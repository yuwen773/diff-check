using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Parser;

/// <summary>
/// 文件解析路由器
/// 根据文件扩展名分发到对应的解析器
/// </summary>
public class FileParserRouter
{
    private readonly List<IFileParser> _parsers;

    public FileParserRouter()
    {
        // 注册所有可用的解析器
        _parsers = new List<IFileParser>
        {
            new DocxParser(),
            new PdfParser()
        };
    }

    /// <summary>
    /// 解析文件，根据扩展名路由到对应解析器
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>解析结果</returns>
    public ParseResult ParseFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ParseResult.Failure("文件路径不能为空");
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (string.IsNullOrEmpty(ext))
        {
            return ParseResult.Failure("无法识别文件扩展名");
        }

        var parser = _parsers.FirstOrDefault(p => p.CanParse(ext));

        if (parser == null)
        {
            return ParseResult.Failure($"不支持的文件格式: {ext}。支持的格式: .docx, .pdf");
        }

        return parser.Parse(filePath);
    }

    /// <summary>
    /// 解析两个文件，返回 FileParseResult
    /// </summary>
    public FileParseResult ParseFiles(string filePathA, string filePathB)
    {
        var resultA = ParseFile(filePathA);
        var resultB = ParseFile(filePathB);

        if (!resultA.IsSuccess)
        {
            return FileParseResult.Failure(resultA.ErrorMessage);
        }

        if (!resultB.IsSuccess)
        {
            return FileParseResult.Failure(resultB.ErrorMessage);
        }

        return FileParseResult.Success(resultA, resultB, filePathA, filePathB);
    }

    /// <summary>
    /// 检查是否支持指定扩展名
    /// </summary>
    public bool IsSupported(string ext)
    {
        return _parsers.Any(p => p.CanParse(ext));
    }

    /// <summary>
    /// 获取所有支持的扩展名
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        // 使用 CanParse 方法探测支持的扩展名
        var testExtensions = new[] { ".docx", ".pdf", ".txt", ".md", ".json" };
        return testExtensions.Where(ext => IsSupported(ext));
    }
}
