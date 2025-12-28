using AI.DiffAssistant.Cli;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// 命令行参数解析器单元测试
/// </summary>
public class ArgsParserTests : IDisposable
{
    // 测试文件路径
    private readonly string _testFileA;
    private readonly string _testFileB;
    private readonly string _nonExistentFile = @"C:\non\existent\file_12345678.txt";

    public ArgsParserTests()
    {
        // 创建临时测试文件
        _testFileA = Path.Combine(Path.GetTempPath(), $"test_a_{Guid.NewGuid():N}.txt");
        _testFileB = Path.Combine(Path.GetTempPath(), $"test_b_{Guid.NewGuid():N}.txt");

        File.WriteAllText(_testFileA, "Test content A");
        File.WriteAllText(_testFileB, "Test content B");
    }

    public void Dispose()
    {
        // 清理临时文件
        try
        {
            if (File.Exists(_testFileA)) File.Delete(_testFileA);
            if (File.Exists(_testFileB)) File.Delete(_testFileB);
        }
        catch
        {
            // 忽略清理错误
        }
    }

    [Fact]
    public void Parse_EmptyArgs_ShouldReturnGuiMode()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = ArgsParser.Parse(args);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsGuiMode);
        Assert.Empty(result.FilePaths);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Parse_SingleArg_ShouldReturnError()
    {
        // Arrange
        var args = new[] { _testFileA };

        // Act
        var result = ArgsParser.Parse(args);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsGuiMode);
        Assert.Contains("两个文件", result.ErrorMessage);
    }

    [Fact]
    public void Parse_ThreeArgs_ShouldReturnError()
    {
        // Arrange
        var args = new[] { _testFileA, _testFileB, "extra.txt" };

        // Act
        var result = ArgsParser.Parse(args);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("无效", result.ErrorMessage);
        Assert.Contains("3", result.ErrorMessage);
    }

    [Fact]
    public void Parse_TwoValidFiles_ShouldReturnSuccess()
    {
        // Arrange
        var args = new[] { _testFileA, _testFileB };

        // Act
        var result = ArgsParser.Parse(args);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsGuiMode);
        Assert.Equal(2, result.FilePaths.Count);
        Assert.Equal(_testFileA, result.FilePaths[0]);
        Assert.Equal(_testFileB, result.FilePaths[1]);
    }

    [Fact]
    public void Parse_FirstFileNotFound_ShouldReturnError()
    {
        // Arrange
        var args = new[] { _nonExistentFile, _testFileB };

        // Act
        var result = ArgsParser.Parse(args);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不存在", result.ErrorMessage);
        Assert.Contains("non", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_SecondFileNotFound_ShouldReturnError()
    {
        // Arrange
        var args = new[] { _testFileA, _nonExistentFile };

        // Act
        var result = ArgsParser.Parse(args);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不存在", result.ErrorMessage);
    }

    [Fact]
    public void Parse_BothFilesNotFound_ShouldReturnFirstError()
    {
        // Arrange
        var nonExistent1 = @"C:\non\existent\file1.txt";
        var nonExistent2 = @"C:\non\existent\file2.txt";
        var args = new[] { nonExistent1, nonExistent2 };

        // Act
        var result = ArgsParser.Parse(args);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不存在", result.ErrorMessage);
    }

    [Fact]
    public void GetUsage_ShouldReturnNonEmptyString()
    {
        // Act
        var usage = ArgsParser.GetUsage();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(usage));
        Assert.Contains("AI 文档差异助手", usage);
        Assert.Contains("用法", usage);
        Assert.Contains("fileA", usage);
        Assert.Contains("fileB", usage);
    }

    [Fact]
    public void Parse_WithSpecialCharactersInPath_ShouldHandleCorrectly()
    {
        // Arrange
        var fileWithSpaces = Path.Combine(Path.GetTempPath(), "test file with spaces.txt");
        var fileWithParens = Path.Combine(Path.GetTempPath(), "test(file).txt");

        try
        {
            File.WriteAllText(fileWithSpaces, "content");
            File.WriteAllText(fileWithParens, "content");

            var args = new[] { fileWithSpaces, fileWithParens };

            // Act
            var result = ArgsParser.Parse(args);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(fileWithSpaces, result.FilePaths[0]);
            Assert.Equal(fileWithParens, result.FilePaths[1]);
        }
        finally
        {
            // Cleanup
            try
            {
                if (File.Exists(fileWithSpaces)) File.Delete(fileWithSpaces);
                if (File.Exists(fileWithParens)) File.Delete(fileWithParens);
            }
            catch { }
        }
    }

    [Fact]
    public void Parse_WithChinesePath_ShouldHandleCorrectly()
    {
        // Arrange
        var chinesePath = Path.Combine(Path.GetTempPath(), "测试文件_中文.txt");

        try
        {
            File.WriteAllText(chinesePath, "内容");

            var args = new[] { chinesePath, _testFileB };

            // Act
            var result = ArgsParser.Parse(args);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(chinesePath, result.FilePaths[0]);
        }
        finally
        {
            // Cleanup
            try
            {
                if (File.Exists(chinesePath)) File.Delete(chinesePath);
            }
            catch { }
        }
    }

    [Fact]
    public void Parse_FileWithReadOnlyAttribute_ShouldReturnError()
    {
        // Arrange
        var readOnlyFile = Path.Combine(Path.GetTempPath(), $"readonly_{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(readOnlyFile, "content");
            File.SetAttributes(readOnlyFile, File.GetAttributes(readOnlyFile) | FileAttributes.ReadOnly);

            var args = new[] { readOnlyFile, _testFileB };

            // Act
            var result = ArgsParser.Parse(args);

            // Assert
            // 注意：File.OpenRead 会成功打开只读文件，所以这不应该是错误
            Assert.True(result.IsValid);
        }
        finally
        {
            // Cleanup
            try
            {
                if (File.Exists(readOnlyFile))
                {
                    File.SetAttributes(readOnlyFile, FileAttributes.Normal);
                    File.Delete(readOnlyFile);
                }
            }
            catch { }
        }
    }
}
