using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using AI.DiffAssistant.Core.Parser;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// 文件解析路由器单元测试
/// </summary>
public class FileParserRouterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileParserRouter _router;

    public FileParserRouterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DiffAssistant_RouterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _router = new FileParserRouter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region ParseFile Tests

    [Fact]
    public void ParseFile_DocxFile_ReturnsSuccess()
    {
        // Arrange
        var filePath = CreateDocxFile(new[] { "测试文档内容" });

        // Act
        var result = _router.ParseFile(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(".docx", result.SourceFileType);
        Assert.Contains("测试文档内容", result.Content);
    }

    [Fact]
    public void ParseFile_NonExistentFile_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.pdf");

        // Act
        var result = _router.ParseFile(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不存在", result.ErrorMessage);
    }

    [Fact]
    public void ParseFile_UnsupportedFormat_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.xlsx");
        File.WriteAllText(filePath, "test");

        // Act
        var result = _router.ParseFile(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不支持", result.ErrorMessage);
        Assert.Contains(".xlsx", result.ErrorMessage);
    }

    [Fact]
    public void ParseFile_EmptyPath_ReturnsFailure()
    {
        // Arrange
        var filePath = "";

        // Act
        var result = _router.ParseFile(filePath);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ParseFile_NullPath_ReturnsFailure()
    {
        // Arrange
        string? filePath = null;

        // Act
        var result = _router.ParseFile(filePath!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不能为空", result.ErrorMessage);
    }

    [Fact]
    public void ParseFile_FileWithoutExtension_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "testfile");
        File.WriteAllText(filePath, "content");

        // Act
        var result = _router.ParseFile(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("无法识别", result.ErrorMessage);
    }

    #endregion

    #region ParseFiles Tests (Two Files)

    [Fact]
    public void ParseFiles_TwoValidDocxFiles_ReturnsSuccess()
    {
        // Arrange
        var filePathA = CreateDocxFile(new[] { "文档A内容" });
        var filePathB = CreateDocxFile(new[] { "文档B内容" });

        // Act
        var result = _router.ParseFiles(filePathA, filePathB);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.FileA.IsSuccess);
        Assert.True(result.FileB.IsSuccess);
        Assert.Contains("文档A内容", result.FileA.Content);
        Assert.Contains("文档B内容", result.FileB.Content);
    }

    [Fact]
    public void ParseFiles_FirstFileNotFound_ReturnsFailure()
    {
        // Arrange
        var filePathA = Path.Combine(_testDirectory, "nonexistentA.docx");
        var filePathB = CreateDocxFile(new[] { "文档B" });

        // Act
        var result = _router.ParseFiles(filePathA, filePathB);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不存在", result.ErrorMessage);
    }

    [Fact]
    public void ParseFiles_SecondFileNotFound_ReturnsFailure()
    {
        // Arrange
        var filePathA = CreateDocxFile(new[] { "文档A" });
        var filePathB = Path.Combine(_testDirectory, "nonexistentB.docx");

        // Act
        var result = _router.ParseFiles(filePathA, filePathB);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不存在", result.ErrorMessage);
    }

    [Fact]
    public void ParseFiles_DocxAndPdf_ReturnsSuccess()
    {
        // Arrange
        var docxPath = CreateDocxFile(new[] { "Word文档内容" });

        // 创建模拟的 PDF 文件（无效内容测试路由器）
        var pdfPath = Path.Combine(_testDirectory, "test.pdf");
        File.WriteAllText(pdfPath, "not a real pdf");

        // Act
        var result = _router.ParseFiles(docxPath, pdfPath);

        // Assert - docx 成功，pdf 失败（因为不是真正的 PDF）
        Assert.False(result.IsSuccess);
        Assert.Contains("PDF", result.ErrorMessage);
    }

    [Fact]
    public void ParseFiles_UnsupportedFirstFile_ReturnsFailure()
    {
        // Arrange
        var filePathA = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePathA, "content");
        var filePathB = CreateDocxFile(new[] { "文档B" });

        // Act
        var result = _router.ParseFiles(filePathA, filePathB);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不支持", result.ErrorMessage);
        Assert.Contains(".txt", result.ErrorMessage);
    }

    [Fact]
    public void ParseFiles_BothUnsupported_ReturnsFailure()
    {
        // Arrange
        var filePathA = Path.Combine(_testDirectory, "test.txt");
        var filePathB = Path.Combine(_testDirectory, "test.xlsx");
        File.WriteAllText(filePathA, "contentA");
        File.WriteAllText(filePathB, "contentB");

        // Act
        var result = _router.ParseFiles(filePathA, filePathB);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ParseFiles_TotalCharCount_CalculatesCorrectly()
    {
        // Arrange
        var contentA = "内容A测试";
        var contentB = "内容B测试";
        var filePathA = CreateDocxFile(new[] { contentA });
        var filePathB = CreateDocxFile(new[] { contentB });

        // Act
        var result = _router.ParseFiles(filePathA, filePathB);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(contentA.Length + contentB.Length, result.TotalCharCount);
    }

    #endregion

    #region IsSupported Tests

    [Fact]
    public void IsSupported_Docx_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(_router.IsSupported(".docx"));
        Assert.True(_router.IsSupported(".DOCX"));
    }

    [Fact]
    public void IsSupported_Pdf_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(_router.IsSupported(".pdf"));
        Assert.True(_router.IsSupported(".PDF"));
    }

    [Fact]
    public void IsSupported_UnsupportedFormat_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(_router.IsSupported(".txt"));
        Assert.False(_router.IsSupported(".doc"));
        Assert.False(_router.IsSupported(".xlsx"));
        Assert.False(_router.IsSupported(".pptx"));
    }

    #endregion

    #region GetSupportedExtensions Tests

    [Fact]
    public void GetSupportedExtensions_ReturnsDocxAndPdf()
    {
        // Arrange & Act
        var extensions = _router.GetSupportedExtensions().ToList();

        // Assert - 验证返回的扩展名包含 docx 和 pdf（不区分大小写）
        Assert.NotEmpty(extensions);
        Assert.True(extensions.Any(e => e.Equals(".docx", StringComparison.OrdinalIgnoreCase)),
            "Should contain .docx");
        Assert.True(extensions.Any(e => e.Equals(".pdf", StringComparison.OrdinalIgnoreCase)),
            "Should contain .pdf");
    }

    [Fact]
    public void GetSupportedExtensions_ContainsAtLeastTwoExtensions()
    {
        // Arrange & Act
        var extensions = _router.GetSupportedExtensions().ToList();

        // Assert
        Assert.True(extensions.Count >= 2, $"Expected at least 2 extensions, got {extensions.Count}");
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

    #endregion
}
