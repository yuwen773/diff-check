using System.Text;
using AI.DiffAssistant.Core.Parser;

namespace AI.DiffAssistant.Core.File;

/// <summary>
/// 文件读取结果
/// </summary>
public class ReadResult
{
    /// <summary>
    /// 文件内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 检测到的编码
    /// </summary>
    public Encoding? Encoding { get; init; } = Encoding.UTF8;

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(FilePath);

    /// <summary>
    /// 源文件类型（扩展名）
    /// </summary>
    public string? SourceFileType { get; init; }

    /// <summary>
    /// 是否为富文本格式解析
    /// </summary>
    public bool IsRichText => SourceFileType is ".docx" or ".pdf";
}

/// <summary>
/// 截断结果
/// </summary>
public class TruncateResult
{
    /// <summary>
    /// 处理后的内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 状态：完整 / 已截断
    /// </summary>
    public string Status { get; init; } = "完整";

    /// <summary>
    /// 原始长度
    /// </summary>
    public int OriginalLength { get; init; }

    /// <summary>
    /// 截断后长度
    /// </summary>
    public int TruncatedLength { get; init; }

    /// <summary>
    /// 保留百分比
    /// </summary>
    public double Percentage { get; init; }

    /// <summary>
    /// 是否被截断
    /// </summary>
    public bool IsTruncated => Status == "已截断";
}

/// <summary>
/// 文件处理器：读取、编码检测、截断
/// </summary>
public class FileProcessor
{
    private readonly int _maxLength;
    private readonly FileParserRouter _router;

    /// <summary>
    /// 支持的富文本格式
    /// </summary>
    private static readonly HashSet<string> RichTextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx",
        ".pdf"
    };

    /// <summary>
    /// 初始化文件处理器
    /// </summary>
    /// <param name="maxLength">最大字符数阈值</param>
    public FileProcessor(int maxLength = 15000)
    {
        _maxLength = maxLength;
        _router = new FileParserRouter();
    }

    /// <summary>
    /// 检查是否为富文本格式
    /// </summary>
    private static bool IsRichTextFile(string ext) => RichTextExtensions.Contains(ext);

    /// <summary>
    /// 读取文件内容并检测编码
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>读取结果</returns>
    public ReadResult ReadFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        if (!System.IO.File.Exists(filePath))
        {
            throw new System.IO.FileNotFoundException($"文件不存在: {filePath}", filePath);
        }

        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

        // 富文本格式使用解析器
        if (IsRichTextFile(ext))
        {
            var result = _router.ParseFile(filePath);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.ErrorMessage);
            }

            return new ReadResult
            {
                Content = result.Content,
                Encoding = null,
                FilePath = filePath,
                SourceFileType = result.SourceFileType
            };
        }

        // 纯文本格式使用编码检测
        var encoding = EncodingDetector.Detect(filePath);
        string content = System.IO.File.ReadAllText(filePath, encoding);

        return new ReadResult
        {
            Content = content,
            Encoding = encoding,
            FilePath = filePath,
            SourceFileType = ext
        };
    }

    /// <summary>
    /// 读取文件内容（异步）
    /// </summary>
    public async Task<ReadResult> ReadFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        if (!System.IO.File.Exists(filePath))
        {
            throw new System.IO.FileNotFoundException($"文件不存在: {filePath}", filePath);
        }

        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

        // 富文本格式使用解析器
        if (IsRichTextFile(ext))
        {
            var result = _router.ParseFile(filePath);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.ErrorMessage);
            }

            return new ReadResult
            {
                Content = result.Content,
                Encoding = null,
                FilePath = filePath,
                SourceFileType = result.SourceFileType
            };
        }

        // 纯文本格式使用编码检测
        var encoding = EncodingDetector.Detect(filePath);

        // 异步读取内容
        using var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, bufferSize: 4096, useAsync: true);
        using var reader = new System.IO.StreamReader(stream, encoding);
        string content = await reader.ReadToEndAsync();

        return new ReadResult
        {
            Content = content,
            Encoding = encoding,
            FilePath = filePath,
            SourceFileType = ext
        };
    }

    /// <summary>
    /// 截断文件内容（如果超出阈值）
    /// </summary>
    /// <param name="content">原始内容</param>
    /// <returns>截断结果</returns>
    public TruncateResult TruncateIfNeeded(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        int originalLength = content.Length;

        if (originalLength <= _maxLength)
        {
            return new TruncateResult
            {
                Content = content,
                Status = "完整",
                OriginalLength = originalLength,
                TruncatedLength = originalLength,
                Percentage = 100.0
            };
        }

        // 截取前 maxLength 字符
        string truncated = content[.._maxLength];
        double percentage = Math.Round((double)_maxLength / originalLength * 100, 1);

        return new TruncateResult
        {
            Content = truncated,
            Status = "已截断",
            OriginalLength = originalLength,
            TruncatedLength = _maxLength,
            Percentage = percentage
        };
    }

    /// <summary>
    /// 处理文件：读取并截断
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>元组：(读取结果, 截断结果)</returns>
    public (ReadResult ReadResult, TruncateResult TruncateResult) ProcessFile(string filePath)
    {
        var readResult = ReadFile(filePath);
        var truncateResult = TruncateIfNeeded(readResult.Content);
        return (readResult, truncateResult);
    }

    /// <summary>
    /// 处理文件（异步）
    /// </summary>
    public async Task<(ReadResult ReadResult, TruncateResult TruncateResult)> ProcessFileAsync(string filePath)
    {
        var readResult = await ReadFileAsync(filePath);
        var truncateResult = TruncateIfNeeded(readResult.Content);
        return (readResult, truncateResult);
    }
}
