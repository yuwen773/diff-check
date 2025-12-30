using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using AI.DiffAssistant.Core.Parser;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// Word (.docx) 解析器单元测试
/// </summary>
public class DocxParserTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly DocxParser _parser;

    public DocxParserTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DiffAssistant_DocxTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _parser = new DocxParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region CanParse Tests

    [Fact]
    public void CanParse_DocxExtension_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(_parser.CanParse(".docx"));
        Assert.True(_parser.CanParse(".DOCX"));
        Assert.True(_parser.CanParse(".Docx"));
    }

    [Fact]
    public void CanParse_OtherExtensions_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(_parser.CanParse(".pdf"));
        Assert.False(_parser.CanParse(".txt"));
        Assert.False(_parser.CanParse(".doc"));
        Assert.False(_parser.CanParse(".xlsx"));
    }

    #endregion

    #region Parse Tests - Text Content

    [Fact]
    public void Parse_SimpleText_ReturnsExtractedContent()
    {
        // Arrange
        var filePath = CreateDocxFile(new[] { "这是第一段", "这是第二段" });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("这是第一段", result.Content);
        Assert.Contains("这是第二段", result.Content);
        Assert.Equal(".docx", result.SourceFileType);
    }

    [Fact]
    public void Parse_MultipleParagraphs_PreservesLineBreaks()
    {
        // Arrange
        var paragraphs = new[] { "段落一", "段落二", "段落三" };
        var filePath = CreateDocxFile(paragraphs);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("段落一", result.Content);
        Assert.Contains("段落二", result.Content);
        Assert.Contains("段落三", result.Content);
    }

    [Fact]
    public void Parse_EnglishContent_ReturnsCorrectText()
    {
        // Arrange
        var filePath = CreateDocxFile(new[] { "Hello World", "This is a test document" });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Hello World", result.Content);
        Assert.Contains("test document", result.Content);
    }

    [Fact]
    public void Parse_EmptyDocument_ReturnsEmptyContent()
    {
        // Arrange
        var filePath = CreateDocxFile(Array.Empty<string>());

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("", result.Content);
        Assert.Equal(0, result.CharCount);
    }

    [Fact]
    public void Parse_NonExistentFile_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.docx");

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不存在", result.ErrorMessage);
    }

    #endregion

    #region Parse Tests - Table Content

    [Fact]
    public void Parse_TableContent_ExtractsCellText()
    {
        // Arrange
        var filePath = CreateDocxWithTable(new[]
        {
            new[] { "姓名", "年龄", "城市" },
            new[] { "张三", "25", "北京" },
            new[] { "李四", "30", "上海" }
        });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("姓名", result.Content);
        Assert.Contains("张三", result.Content);
        Assert.Contains("北京", result.Content);
    }

    [Fact]
    public void Parse_TableContent_UsesPipeSeparator()
    {
        // Arrange
        var filePath = CreateDocxWithTable(new[]
        {
            new[] { "A", "B", "C" }
        });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(" | ", result.Content);
    }

    [Fact]
    public void Parse_MixedParagraphsAndTables_ExtractsAllContent()
    {
        // Arrange
        var filePath = CreateDocxFileWithMixedContent(
            new[] { "标题段落" },
            new[] { new[] { "单元格1", "单元格2" } }
        );

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("标题段落", result.Content);
        Assert.Contains("单元格1", result.Content);
        Assert.Contains("单元格2", result.Content);
    }

    #endregion

    #region Parse Tests - Special Characters

    [Fact]
    public void Parse_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var filePath = CreateDocxFile(new[] { "测试!@#$%^&*()_+-=[]{}|;':\",./<>?" });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("测试", result.Content);
    }

    [Fact]
    public void Parse_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var filePath = CreateDocxFile(new[] { "日本語テスト 한국어 🎉" });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("日本語", result.Content);
        Assert.Contains("한국어", result.Content);
    }

    [Fact]
    public void Parse_ChineseCharacters_HandlesCorrectly()
    {
        // Arrange
        var filePath = CreateDocxFile(new[] { "这是中文测试内容，包含标点符号。", "第二行内容。" });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("这是中文测试内容", result.Content);
    }

    #endregion

    #region Parse Result Properties

    [Fact]
    public void Parse_ValidFile_ReturnsCorrectSourceFileType()
    {
        // Arrange
        var filePath = CreateDocxFile(new[] { "测试内容" });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.Equal(".docx", result.SourceFileType);
    }

    [Fact]
    public void Parse_ValidFile_ReturnsCorrectCharCount()
    {
        // Arrange
        var content = "测试内容123";
        var filePath = CreateDocxFile(new[] { content });

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.Equal(content.Length, result.CharCount);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 创建简单的 .docx 文件
    /// </summary>
    private string CreateDocxFile(string[] paragraphs)
    {
        var filePath = Path.Combine(_testDirectory, $"{Guid.NewGuid():N}.docx");

        using (var wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            foreach (var paraText in paragraphs)
            {
                var para = new Paragraph(
                    new Run(
                        new Text(paraText)
                    )
                );
                body.Append(para);
            }

            mainPart.Document.Append(body);
        }

        return filePath;
    }

    /// <summary>
    /// 创建包含表格的 .docx 文件
    /// </summary>
    private string CreateDocxWithTable(string[][] tableData)
    {
        var filePath = Path.Combine(_testDirectory, $"{Guid.NewGuid():N}.docx");

        using (var wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            var table = new Table();

            foreach (var rowData in tableData)
            {
                var tableRow = new TableRow();

                foreach (var cellText in rowData)
                {
                    var tableCell = new TableCell(
                        new Paragraph(
                            new Run(
                                new Text(cellText)
                            )
                        )
                    );
                    tableRow.Append(tableCell);
                }

                table.Append(tableRow);
            }

            body.Append(table);
            mainPart.Document.Append(body);
        }

        return filePath;
    }

    /// <summary>
    /// 创建包含段落和表格的 .docx 文件
    /// </summary>
    private string CreateDocxFileWithMixedContent(string[] paragraphs, string[][] tableData)
    {
        var filePath = Path.Combine(_testDirectory, $"{Guid.NewGuid():N}.docx");

        using (var wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // 添加段落
            foreach (var paraText in paragraphs)
            {
                var para = new Paragraph(
                    new Run(
                        new Text(paraText)
                    )
                );
                body.Append(para);
            }

            // 添加表格
            var table = new Table();

            foreach (var rowData in tableData)
            {
                var tableRow = new TableRow();

                foreach (var cellText in rowData)
                {
                    var tableCell = new TableCell(
                        new Paragraph(
                            new Run(
                                new Text(cellText)
                            )
                        )
                    );
                    tableRow.Append(tableCell);
                }

                table.Append(tableRow);
            }

            body.Append(table);
            mainPart.Document.Append(body);
        }

        return filePath;
    }

    #endregion
}
