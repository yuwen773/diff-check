using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AI.DiffAssistant.Core.File;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Diff;

/// <summary>
/// 差异分析结果
/// </summary>
public class DiffAnalysisResult
{
    /// <summary>
    /// 分析是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// AI 返回的分析内容
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 是否重试后成功
    /// </summary>
    public bool WasRetried { get; init; }

    public static DiffAnalysisResult Success(string content, bool wasRetried = false) =>
        new() { IsSuccess = true, Content = content, WasRetried = wasRetried };

    public static DiffAnalysisResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// AI 差异分析器
/// </summary>
public class DiffAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// 最大重试次数
    /// </summary>
    private const int MaxRetries = 3;

    /// <summary>
    /// 重试间隔（毫秒）
    /// </summary>
    private const int RetryIntervalMs = 1000;

    /// <summary>
    /// 默认最大 Tokens
    /// </summary>
    private const int DefaultMaxTokens = 2000;

    /// <summary>
    /// 请求超时（秒）
    /// </summary>
    private const int RequestTimeoutSeconds = 60;

    public DiffAnalyzer(HttpClient httpClient, AppConfig config)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// 分析两个文件的差异
    /// </summary>
    /// <param name="fileA">文件 A 路径</param>
    /// <param name="fileB">文件 B 路径</param>
    /// <returns>分析结果</returns>
    public async Task<DiffAnalysisResult> AnalyzeAsync(string fileA, string fileB)
    {
        // 读取并处理两个文件
        var processor = new FileProcessor(_config.Settings.MaxTokenLimit);
        var (resultA, truncateA) = await processor.ProcessFileAsync(fileA);
        var (resultB, truncateB) = await processor.ProcessFileAsync(fileB);

        // 构建用户提示词
        var userPrompt = BuildUserPrompt(resultA, truncateA, resultB, truncateB);

        // 发送请求并返回结果
        return await SendAnalysisRequestAsync(_config.Api, _config.Prompts.SystemPrompt, userPrompt);
    }

    /// <summary>
    /// 分析两个已处理的内容
    /// </summary>
    public async Task<DiffAnalysisResult> AnalyzeContentAsync(
        string fileAName, string contentA, bool isTruncatedA,
        string fileBName, string contentB, bool isTruncatedB)
    {
        var truncateA = new TruncateResult
        {
            Status = isTruncatedA ? "已截断" : "完整"
        };
        var truncateB = new TruncateResult
        {
            Status = isTruncatedB ? "已截断" : "完整"
        };

        var resultA = new ReadResult { Content = contentA, FilePath = fileAName };
        var resultB = new ReadResult { Content = contentB, FilePath = fileBName };

        var userPrompt = BuildUserPrompt(resultA, truncateA, resultB, truncateB);
        return await SendAnalysisRequestAsync(_config.Api, _config.Prompts.SystemPrompt, userPrompt);
    }

    /// <summary>
    /// 构建用户提示词
    /// </summary>
    private string BuildUserPrompt(ReadResult fileA, TruncateResult truncateA, ReadResult fileB, TruncateResult truncateB)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"请对比以下两份文档的差异：");
        sb.AppendLine();
        sb.AppendLine($"**文件 A: {fileA.FileName}** (编码: {fileA.Encoding.EncodingName})");
        if (truncateA.IsTruncated)
        {
            sb.AppendLine($"[注意：内容已截断，仅保留前 {truncateA.Percentage}%]");
        }
        sb.AppendLine("---");
        sb.AppendLine(fileA.Content);
        sb.AppendLine();
        sb.AppendLine($"**文件 B: {fileB.FileName}** (编码: {fileB.Encoding.EncodingName})");
        if (truncateB.IsTruncated)
        {
            sb.AppendLine($"[注意：内容已截断，仅保留前 {truncateB.Percentage}%]");
        }
        sb.AppendLine("---");
        sb.AppendLine(fileB.Content);

        return sb.ToString();
    }

    /// <summary>
    /// 发送分析请求（带重试逻辑）
    /// </summary>
    private async Task<DiffAnalysisResult> SendAnalysisRequestAsync(
        ApiConfig apiConfig, string systemPrompt, string userPrompt)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < MaxRetries)
        {
            attempt++;

            try
            {
                var result = await SendRequestAsync(apiConfig, systemPrompt, userPrompt);
                if (result.IsSuccess)
                {
                    return result;
                }

                // 如果是客户端错误（401、400 等），不再重试
                if (!IsRetryableError(result.ErrorMessage))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (!IsRetryableException(ex))
                {
                    throw;
                }
            }

            // 重试前等待
            if (attempt < MaxRetries)
            {
                await Task.Delay(RetryIntervalMs * attempt); // 指数退避
            }
        }

        return DiffAnalysisResult.Failure(
            lastException != null
                ? $"分析失败，已重试 {MaxRetries} 次: {lastException.Message}"
                : "分析失败，请稍后重试");
    }

    /// <summary>
    /// 发送单个请求
    /// </summary>
    private async Task<DiffAnalysisResult> SendRequestAsync(
        ApiConfig apiConfig, string systemPrompt, string userPrompt)
    {
        // 验证配置
        if (string.IsNullOrWhiteSpace(apiConfig.BaseUrl))
            return DiffAnalysisResult.Failure("AI 服务 Base URL 未配置");

        if (string.IsNullOrWhiteSpace(apiConfig.ApiKey))
            return DiffAnalysisResult.Failure("AI 服务 API Key 未配置");

        if (string.IsNullOrWhiteSpace(apiConfig.Model))
            return DiffAnalysisResult.Failure("AI 服务模型名称未配置");

        // 构建请求 URL
        var baseUrl = apiConfig.BaseUrl.TrimEnd('/');
        var chatUrl = $"{baseUrl}/chat/completions";

        // 构建请求体
        var requestBody = new
        {
            model = apiConfig.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = DefaultMaxTokens,
            temperature = 0.7
        };

        var jsonContent = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // 设置请求头
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiConfig.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // 发送请求
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds));

        try
        {
            var response = await _httpClient.PostAsync(chatUrl, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync(response);
            }

            // 解析响应
            var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
            var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, JsonOptions);

            if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
            {
                return DiffAnalysisResult.Failure("AI 返回了空的响应");
            }

            var contentResult = chatResponse.Choices[0].Message?.Content ?? string.Empty;
            return DiffAnalysisResult.Success(contentResult);
        }
        catch (TaskCanceledException)
        {
            return DiffAnalysisResult.Failure("AI 分析超时，请稍后重试");
        }
        catch (HttpRequestException ex)
        {
            return DiffAnalysisResult.Failure($"网络错误: {GetFriendlyErrorMessage(ex)}");
        }
    }

    /// <summary>
    /// 处理错误响应
    /// </summary>
    private async Task<DiffAnalysisResult> HandleErrorResponseAsync(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        var content = await response.Content.ReadAsStringAsync();

        // 尝试解析错误信息
        var errorMessage = TryParseErrorMessage(content) ?? GetDefaultErrorMessage(statusCode);

        return statusCode switch
        {
            401 => DiffAnalysisResult.Failure("API Key 无效，请检查配置"),
            429 => DiffAnalysisResult.Failure("请求频率超限，请稍后再试"),
            400 => DiffAnalysisResult.Failure($"请求参数错误: {errorMessage}"),
            _ => DiffAnalysisResult.Failure(errorMessage)
        };
    }

    /// <summary>
    /// 尝试从响应中解析错误信息
    /// </summary>
    private static string? TryParseErrorMessage(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    return message.GetString();
                }
            }

            if (root.TryGetProperty("error_message", out var errorMsg))
            {
                return errorMsg.GetString();
            }
        }
        catch
        {
            // 解析失败，返回 null
        }

        return null;
    }

    private static string GetDefaultErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            500 => "AI 服务内部错误，请稍后重试",
            502 => "AI 服务暂时不可用，请稍后重试",
            503 => "AI 服务繁忙，请稍后重试",
            _ => $"服务器返回错误: {statusCode}"
        };
    }

    private static string GetFriendlyErrorMessage(HttpRequestException ex)
    {
        return ex.Message.Contains("No such host") || ex.Message.Contains("dns")
            ? "无法解析服务器地址，请检查 Base URL"
            : "无法连接到 AI 服务";
    }

    /// <summary>
    /// 判断是否应该重试
    /// </summary>
    private static bool IsRetryableError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return false;

        // 401、400 等客户端错误不重试
        if (errorMessage.Contains("401") || errorMessage.Contains("API Key"))
            return false;
        if (errorMessage.Contains("400") || errorMessage.Contains("参数错误"))
            return false;

        // 超时、网络错误、服务端错误可以重试
        return true;
    }

    private static bool IsRetryableException(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException;
    }
}

/// <summary>
/// OpenAI Chat Completion 响应（简化版）
/// </summary>
public class ChatCompletionResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? Model { get; set; }
    public List<Choice>? Choices { get; set; }
    public Usage? Usage { get; set; }
}

public class Choice
{
    public int Index { get; set; }
    public Message? Message { get; set; }
    public string? FinishReason { get; set; }
}

public class Message
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
