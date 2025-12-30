using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Parser;

/// <summary>
/// Word (.docx) 文档解析器
/// 提取文档正文文本，忽略页眉、页脚、批注、图片
/// 表格：提取文字，保留单元格分隔符
/// </summary>
public class DocxParser : IFileParser
{
    private const string DocxExtension = ".docx";

    public bool CanParse(string ext) =>
        ext.Equals(DocxExtension, StringComparison.OrdinalIgnoreCase);

    public ParseResult Parse(string filePath)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document.Body;

            if (body == null)
            {
                return ParseResult.Failure("文档正文为空");
            }

            var text = ExtractBodyText(body);
            return ParseResult.Success(text, DocxExtension, text.Length);
        }
        catch (FileNotFoundException)
        {
            return ParseResult.Failure($"文件不存在: {filePath}");
        }
        catch (Exception ex)
        {
            return ParseResult.Failure($"解析 Word 文档失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 提取正文文本，保留段落和表格换行
    /// </summary>
    private static string ExtractBodyText(Body body)
    {
        var sb = new StringBuilder();

        foreach (var element in body.Elements())
        {
            switch (element)
            {
                case Paragraph paragraph:
                    sb.Append(ExtractParagraphText(paragraph));
                    sb.AppendLine();
                    break;

                case Table table:
                    sb.Append(ExtractTableText(table));
                    sb.AppendLine();
                    break;

                // 忽略: SectionProperties, BookmarkStart, BookmarkEnd, Run, etc.
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 提取段落文本
    /// </summary>
    private static string ExtractParagraphText(Paragraph paragraph)
    {
        if (paragraph?.InnerText == null)
            return string.Empty;

        // 使用 InnerText 获取段落文本，换行符会保留
        return paragraph.InnerText;
    }

    /// <summary>
    /// 提取表格文本，保留单元格分隔符
    /// </summary>
    private static string ExtractTableText(Table table)
    {
        var sb = new StringBuilder();

        foreach (var tableRow in table.Elements<TableRow>())
        {
            var rowText = new StringBuilder();
            bool isFirstCell = true;

            foreach (var tableCell in tableRow.Elements<TableCell>())
            {
                if (!isFirstCell)
                    rowText.Append(" | "); // 单元格分隔符

                // 获取单元格内的所有段落文本
                var cellText = new StringBuilder();
                foreach (var paragraph in tableCell.Elements<Paragraph>())
                {
                    if (cellText.Length > 0)
                        cellText.Append(" ");
                    cellText.Append(paragraph.InnerText ?? string.Empty);
                }
                rowText.Append(cellText.ToString().TrimEnd());
                isFirstCell = false;
            }

            sb.AppendLine(rowText.ToString());
        }

        return sb.ToString().TrimEnd();
    }
}
