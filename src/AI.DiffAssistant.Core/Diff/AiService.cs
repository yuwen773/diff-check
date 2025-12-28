using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Diff;

/// <summary>
/// AI 服务调用类
/// </summary>
public class AiService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 测试 AI API 连接
    /// </summary>
    /// <param name="config">API 配置</param>
    /// <returns>连接结果</returns>
    public async Task<ConnectionResult> TestConnectionAsync(ApiConfig config)
    {
        try
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(config.BaseUrl))
                return ConnectionResult.Failure("Base URL 不能为空");

            if (string.IsNullOrWhiteSpace(config.ApiKey))
                return ConnectionResult.Failure("API Key 不能为空");

            if (string.IsNullOrWhiteSpace(config.Model))
                return ConnectionResult.Failure("模型名称不能为空");

            // 构建请求 URL
            var baseUrl = config.BaseUrl.TrimEnd('/');
            var chatUrl = $"{baseUrl}/chat/completions";

            // 构建请求体
            var requestBody = new
            {
                model = config.Model,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant. Respond with 'OK' only." },
                    new { role = "user", content = "Hi" }
                },
                max_tokens = 10,
                temperature = 0
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 设置请求头
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.ApiKey);

            // 发送请求（10秒超时）
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.PostAsync(chatUrl, content, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return ConnectionResult.Success();
            }

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => ConnectionResult.Failure("API Key 无效，请检查配置"),
                System.Net.HttpStatusCode.TooManyRequests => ConnectionResult.Failure("请求频率超限，请稍后再试"),
                System.Net.HttpStatusCode.BadRequest => ConnectionResult.Failure("请求参数错误，请检查模型名称和配置"),
                _ => ConnectionResult.Failure($"服务器返回错误: {(int)response.StatusCode}")
            };
        }
        catch (TaskCanceledException) when (TimeoutWasSet())
        {
            return ConnectionResult.Failure("连接超时，请检查网络或 API 地址是否正确");
        }
        catch (HttpRequestException ex)
        {
            return ConnectionResult.Failure($"网络错误: {GetFriendlyErrorMessage(ex)}");
        }
        catch (Exception ex)
        {
            return ConnectionResult.Failure($"发生错误: {ex.Message}");
        }
    }

    private static bool TimeoutWasSet()
    {
        // 简化的超时检测
        return true;
    }

    private static string GetFriendlyErrorMessage(HttpRequestException ex)
    {
        return ex.Message.Contains("No such host") || ex.Message.Contains("dns")
            ? "无法解析服务器地址，请检查 Base URL"
            : "无法连接到服务器";
    }
}

/// <summary>
/// 连接测试结果
/// </summary>
public class ConnectionResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ConnectionResult Success() => new() { IsSuccess = true };
    public static ConnectionResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
}
