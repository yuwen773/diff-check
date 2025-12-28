using System.Text;
using AI.DiffAssistant.Core.File;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// 文件处理模块单元测试
/// </summary>
public class FileProcessingTests : IDisposable
{
    private readonly string _testDirectory;

    public FileProcessingTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DiffAssistant_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region EncodingDetector Tests

    [Fact]
    public void Detect_Utf8WithBom_ReturnsUtf8()
    {
        // Arrange
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = Encoding.UTF8.GetBytes("Hello World");
        var data = bom.Concat(content).ToArray();
        var filePath = Path.Combine(_testDirectory, "utf8_bom.txt");
        File.WriteAllBytes(filePath, data);

        // Act
        var encoding = EncodingDetector.Detect(filePath);

        // Assert
        Assert.Equal(Encoding.UTF8, encoding);
    }

    [Fact]
    public void Detect_Utf8WithoutBom_ReturnsUtf8()
    {
        // Arrange
        var content = "Hello World - ASCII only";
        var filePath = Path.Combine(_testDirectory, "utf8_nobom.txt");
        File.WriteAllText(filePath, content, new UTF8Encoding(false));

        // Act
        var encoding = EncodingDetector.Detect(filePath);

        // Assert
        Assert.Equal(Encoding.UTF8, encoding);
    }

    [Fact]
    public void Detect_Ascii_ReturnsUtf8()
    {
        // Arrange - 纯 ASCII 内容使用 UTF-8 编码（无 BOM）写入
        var content = "Simple ASCII text 12345 !@#$%";
        var filePath = Path.Combine(_testDirectory, "ascii.txt");
        // 使用 UTF-8（无 BOM）写入纯 ASCII 内容
        File.WriteAllText(filePath, content, new UTF8Encoding(false));

        // Act
        var encoding = EncodingDetector.Detect(filePath);

        // Assert - 纯 ASCII 内容检测为 UTF-8（因为纯 ASCII 也是有效的 UTF-8）
        Assert.Equal(Encoding.UTF8, encoding);
    }

    [Fact]
    public void Detect_EmptyFile_ReturnsDefault()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "empty.txt");
        File.WriteAllText(filePath, "");

        // Act
        var encoding = EncodingDetector.Detect(filePath);

        // Assert
        Assert.Equal(Encoding.Default, encoding);
    }

    [Fact]
    public void Detect_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => EncodingDetector.Detect(filePath));
    }

    #endregion

    #region FileProcessor Tests

    [Fact]
    public void ReadFile_ValidFile_ReturnsCorrectContent()
    {
        // Arrange
        var content = "测试文件内容";
        var filePath = Path.Combine(_testDirectory, "test_read.txt");
        File.WriteAllText(filePath, content, Encoding.UTF8);

        var processor = new FileProcessor();

        // Act
        var result = processor.ReadFile(filePath);

        // Assert
        Assert.Equal(content, result.Content);
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal("test_read.txt", result.FileName);
        Assert.Equal(Encoding.UTF8, result.Encoding);
    }

    [Fact]
    public void ReadFile_GbkEncoding_HandlesGbkLikeContent()
    {
        // Arrange - 手动创建包含中文模式字节的文件
        // "中文" 的 GBK 编码是: 0xD6 0xD0 0xCE 0xC4
        byte[] gbkChineseBytes = new byte[] { 0xD6, 0xD0, 0xCE, 0xC4 };
        byte[] otherBytes = System.Text.Encoding.ASCII.GetBytes("Test content: ");
        byte[] fullContent = otherBytes.Concat(gbkChineseBytes).Concat(otherBytes).ToArray();

        var filePath = Path.Combine(_testDirectory, "gbk_test.bin");
        File.WriteAllBytes(filePath, fullContent);

        var processor = new FileProcessor();

        // Act
        var result = processor.ReadFile(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Encoding);
    }

    [Fact]
    public void ReadFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var processor = new FileProcessor();

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => processor.ReadFile("nonexistent.txt"));
    }

    [Fact]
    public void ReadFile_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var processor = new FileProcessor();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => processor.ReadFile(""));
    }

    [Fact]
    public async Task ReadFileAsync_ValidFile_ReturnsCorrectContent()
    {
        // Arrange
        var content = "异步读取测试内容";
        var filePath = Path.Combine(_testDirectory, "async_test.txt");
        File.WriteAllText(filePath, content, Encoding.UTF8);

        var processor = new FileProcessor();

        // Act
        var result = await processor.ReadFileAsync(filePath);

        // Assert
        Assert.Equal(content, result.Content);
        Assert.Equal(filePath, result.FilePath);
    }

    #endregion

    #region Truncate Tests

    [Fact]
    public void TruncateIfNeeded_UnderLimit_ReturnsFullContent()
    {
        // Arrange
        var content = new string('A', 5000);
        var processor = new FileProcessor(15000);

        // Act
        var result = processor.TruncateIfNeeded(content);

        // Assert
        Assert.Equal("完整", result.Status);
        Assert.Equal(5000, result.OriginalLength);
        Assert.Equal(5000, result.TruncatedLength);
        Assert.Equal(100.0, result.Percentage);
        Assert.False(result.IsTruncated);
        Assert.Equal(content, result.Content);
    }

    [Fact]
    public void TruncateIfNeeded_OverLimit_TruncatesCorrectly()
    {
        // Arrange
        var content = new string('A', 20000);
        var processor = new FileProcessor(15000);

        // Act
        var result = processor.TruncateIfNeeded(content);

        // Assert
        Assert.Equal("已截断", result.Status);
        Assert.Equal(20000, result.OriginalLength);
        Assert.Equal(15000, result.TruncatedLength);
        Assert.Equal(75.0, result.Percentage);
        Assert.True(result.IsTruncated);
        Assert.Equal(15000, result.Content.Length);
    }

    [Fact]
    public void TruncateIfNeeded_ExactLimit_ReturnsFullContent()
    {
        // Arrange
        var content = new string('A', 15000);
        var processor = new FileProcessor(15000);

        // Act
        var result = processor.TruncateIfNeeded(content);

        // Assert
        Assert.Equal("完整", result.Status);
        Assert.Equal(100.0, result.Percentage);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public void TruncateIfNeeded_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var content = "";
        var processor = new FileProcessor(15000);

        // Act
        var result = processor.TruncateIfNeeded(content);

        // Assert
        Assert.Equal("完整", result.Status);
        Assert.Equal(0, result.OriginalLength);
        Assert.Equal("", result.Content);
    }

    [Fact]
    public void TruncateIfNeeded_NullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var processor = new FileProcessor(15000);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => processor.TruncateIfNeeded(null!));
    }

    [Fact]
    public void ProcessFile_CombinesReadAndTruncate()
    {
        // Arrange
        var content = new string('X', 20000);
        var filePath = Path.Combine(_testDirectory, "process_test.txt");
        File.WriteAllText(filePath, content, Encoding.UTF8);

        var processor = new FileProcessor(15000);

        // Act
        var (readResult, truncateResult) = processor.ProcessFile(filePath);

        // Assert
        Assert.Equal(content, readResult.Content);
        Assert.Equal(filePath, readResult.FilePath);
        Assert.True(truncateResult.IsTruncated);
        Assert.Equal("已截断", truncateResult.Status);
        Assert.Equal(15000, truncateResult.TruncatedLength);
    }

    #endregion
}
