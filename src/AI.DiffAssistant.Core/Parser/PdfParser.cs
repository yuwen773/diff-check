using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Parser;

/// <summary>
/// PDF 文档解析器
/// 提取文档页面文本，支持加密文档检测和扫描件识别
/// </summary>
public class PdfParser : IFileParser
{
    private const string PdfExtension = ".pdf";
    private const int MinTextLengthForScan = 50;        // 扫描件最小文本长度阈值
    private const long MaxFileSizeForScanBytes = 100 * 1024; // 100KB

    public bool CanParse(string ext) =>
        ext.Equals(PdfExtension, StringComparison.OrdinalIgnoreCase);

    public ParseResult Parse(string filePath)
    {
        try
        {
            // 检查文件大小
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return ParseResult.Failure($"文件不存在: {filePath}");
            }

            // 打开并解析 PDF
            using var document = PdfDocument.Open(filePath);
            var text = ExtractText(document);

            // 扫描件检测：文本量极少但文件较大
            if (text.Length < MinTextLengthForScan && fileInfo.Length > MaxFileSizeForScanBytes)
            {
                return ParseResult.Failure("不支持扫描版 PDF（检测到文本量极少，文件体积较大，可能是纯图片扫描）");
            }

            return ParseResult.Success(text, PdfExtension, text.Length);
        }
        catch (PdfDocumentEncryptedException)
        {
            return ParseResult.Failure("PDF 文档已加密，请解除密码保护后重试");
        }
        catch (FileNotFoundException)
        {
            return ParseResult.Failure($"文件不存在: {filePath}");
        }
        catch (Exception ex) when (ex.Message.Contains("encrypted", StringComparison.OrdinalIgnoreCase) ||
                                   ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return ParseResult.Failure("PDF 文档已加密，请解除密码保护后重试");
        }
        catch (Exception ex)
        {
            return ParseResult.Failure($"解析 PDF 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 提取 PDF 所有页面的文本
    /// </summary>
    private static string ExtractText(PdfDocument document)
    {
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            var pageText = page.Text.Trim();
            if (!string.IsNullOrEmpty(pageText))
            {
                sb.AppendLine(pageText);
            }
        }

        return sb.ToString().TrimEnd();
    }
}
