namespace AI.DiffAssistant.Shared.Models;

/// <summary>
/// 封装两个文件解析结果的对比较果
/// </summary>
public class FileParseResult
{
    /// <summary>
    /// 第一个文件的解析结果
    /// </summary>
    public ParseResult FileA { get; set; } = new();

    /// <summary>
    /// 第二个文件的解析结果
    /// </summary>
    public ParseResult FileB { get; set; } = new();

    /// <summary>
    /// 是否两个文件都解析成功
    /// </summary>
    public bool IsSuccess => FileA.IsSuccess && FileB.IsSuccess;

    /// <summary>
    /// 任意一个文件解析失败时的错误信息
    /// </summary>
    public string ErrorMessage
    {
        get
        {
            if (FileA.IsSuccess && FileB.IsSuccess)
                return string.Empty;
            if (!FileA.IsSuccess)
                return FileA.ErrorMessage;
            if (!FileB.IsSuccess)
                return FileB.ErrorMessage;
            return string.Empty;
        }
    }

    /// <summary>
    /// 两个文件总字符数
    /// </summary>
    public int TotalCharCount => FileA.CharCount + FileB.CharCount;

    /// <summary>
    /// 第一个文件路径
    /// </summary>
    public string FileAPath { get; set; } = string.Empty;

    /// <summary>
    /// 第二个文件路径
    /// </summary>
    public string FileBPath { get; set; } = string.Empty;

    /// <summary>
    /// 创建对比较果（两个文件都成功）
    /// </summary>
    public static FileParseResult Success(ParseResult fileA, ParseResult fileB, string fileAPath, string fileBPath)
    {
        return new FileParseResult
        {
            FileA = fileA,
            FileB = fileB,
            FileAPath = fileAPath,
            FileBPath = fileBPath
        };
    }

    /// <summary>
    /// 创建失败结果（任一文件解析失败）
    /// </summary>
    public static FileParseResult Failure(string errorMessage)
    {
        return new FileParseResult
        {
            FileA = ParseResult.Failure(errorMessage),
            FileB = ParseResult.Failure(errorMessage)
        };
    }
}
