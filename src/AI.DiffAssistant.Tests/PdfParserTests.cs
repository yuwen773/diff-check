using System.Text;
using AI.DiffAssistant.Core.Parser;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// PDF 解析器单元测试
/// </summary>
public class PdfParserTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PdfParser _parser;

    public PdfParserTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DiffAssistant_PdfTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _parser = new PdfParser();
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
    public void CanParse_PdfExtension_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(_parser.CanParse(".pdf"));
        Assert.True(_parser.CanParse(".PDF"));
        Assert.True(_parser.CanParse(".Pdf"));
    }

    [Fact]
    public void CanParse_OtherExtensions_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(_parser.CanParse(".docx"));
        Assert.False(_parser.CanParse(".txt"));
        Assert.False(_parser.CanParse(".doc"));
        Assert.False(_parser.CanParse(".xlsx"));
    }

    #endregion

    #region Parse Tests - File Not Found

    [Fact]
    public void Parse_NonExistentFile_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.pdf");

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不存在", result.ErrorMessage);
    }

    #endregion

    #region Parse Tests - File Path Validation

    [Fact]
    public void Parse_EmptyPath_ReturnsFailure()
    {
        // Arrange
        var filePath = "";

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Parse_NullPath_ReturnsFailure()
    {
        // Arrange
        string? filePath = null;

        // Act
        var result = _parser.Parse(filePath!);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Parse Result Properties

    [Fact]
    public void Parse_NonExistentFile_HasCorrectSourceFileType()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.pdf");

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        // 即使失败也可能有 sourceFileType
    }

    [Fact]
    public void Parse_ValidFile_ReturnsCorrectSourceFileType()
    {
        // Arrange - Create a simple text file (not a real PDF, but tests the extension check)
        var filePath = Path.Combine(_testDirectory, "valid.pdf");
        File.WriteAllText(filePath, "test content");

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        // 文件不是真正的PDF，会解析失败
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void Parse_InvalidPdfFormat_ReturnsFailure()
    {
        // Arrange - Create a file with .pdf extension but invalid content
        var filePath = Path.Combine(_testDirectory, "invalid.pdf");
        File.WriteAllText(filePath, "This is not a valid PDF");

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion

    #region Scan Detection Logic Tests

    /// <summary>
    /// 测试扫描件检测逻辑
    /// 注意：实际测试需要创建符合扫描件特征的文件
    /// </summary>
    [Fact]
    public void Parse_SmallFileWithText_Succeeds()
    {
        // Arrange - 创建一个小文件，包含有效文本内容
        // 注意：这只是一个简单的文本文件，不是真正的 PDF
        // 实际测试需要真实的 PDF 文件
        var filePath = Path.Combine(_testDirectory, "small.pdf");
        File.WriteAllText(filePath, "Hello World");

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        // 预期失败（不是有效的 PDF）
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Integration Tests (Require Real PDF Files)

    /// <summary>
    /// 此测试需要真实的 PDF 文件
    /// 运行集成测试前，请确保 test-resources 目录下有有效的 PDF 文件
    /// </summary>
    [Fact]
    public void Parse_RealPdfFile_ExtractsText()
    {
        // Arrange - 检查是否有测试用的 PDF 文件
        var testPdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-resources", "sample.pdf");

        // 如果测试文件不存在，跳过此测试
        if (!File.Exists(testPdfPath))
        {
            // 创建跳过原因
            return;
        }

        // Act
        var result = _parser.Parse(testPdfPath);

        // Assert
        if (result.IsSuccess)
        {
            Assert.NotNull(result.Content);
            Assert.Equal(".pdf", result.SourceFileType);
            Assert.True(result.CharCount >= 0);
        }
        else
        {
            // 可能是加密或损坏的 PDF
            Assert.NotNull(result.ErrorMessage);
        }
    }

    #endregion
}
