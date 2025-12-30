namespace AI.DiffAssistant.Shared.Models;

/// <summary>
/// 表示单个文件解析的结果
/// </summary>
public class ParseResult
{
    /// <summary>
    /// 解析后的纯文本内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 源文件类型扩展名（如 ".pdf", ".docx"）
    /// </summary>
    public string SourceFileType { get; set; } = string.Empty;

    /// <summary>
    /// 是否解析成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（解析失败时填充）
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 解析后内容的字符数
    /// </summary>
    public int CharCount { get; set; }

    /// <summary>
    /// 创建成功的解析结果
    /// </summary>
    public static ParseResult Success(string content, string sourceFileType, int charCount)
    {
        return new ParseResult
        {
            Content = content,
            SourceFileType = sourceFileType,
            IsSuccess = true,
            CharCount = charCount
        };
    }

    /// <summary>
    /// 创建失败的解析结果
    /// </summary>
    public static ParseResult Failure(string errorMessage)
    {
        return new ParseResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
