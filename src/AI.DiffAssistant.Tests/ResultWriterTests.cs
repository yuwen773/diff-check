using AI.DiffAssistant.Core.Diff;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// 结果写入器单元测试
/// </summary>
public class ResultWriterTests : IDisposable
{
    private readonly string _testDirectory;

    public ResultWriterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DiffAssistant_Writer_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void AppendDiffReport_NewFile_CreatesFileWithContent()
    {
        // Arrange
        var fileAPath = Path.Combine(_testDirectory, "fileA.txt");
        var fileBPath = Path.Combine(_testDirectory, "fileB.txt");
        File.WriteAllText(fileAPath, "Content A");
        File.WriteAllText(fileBPath, "Content B");

        var writer = new ResultWriter();
        var aiResult = "## 主要差异\n- 第一处差异\n- 第二处差异";

        // Act
        var result = writer.AppendDiffReport(fileAPath, fileBPath, aiResult, false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.OutputPath);
        Assert.True(File.Exists(result.OutputPath));

        var content = File.ReadAllText(result.OutputPath);
        Assert.Contains("## 📅 对比报告:", content);
        Assert.Contains("fileA.txt vs fileB.txt", content);
        Assert.Contains("状态: 完整", content);
        Assert.Contains("## 主要差异", content);
        Assert.Contains("第一处差异", content);
    }

    [Fact]
    public void AppendDiffReport_TruncatedContent_ShowsTruncatedStatus()
    {
        // Arrange
        var fileAPath = Path.Combine(_testDirectory, "fileA.txt");
        var fileBPath = Path.Combine(_testDirectory, "fileB.txt");
        File.WriteAllText(fileAPath, "Content A");
        File.WriteAllText(fileBPath, "Content B");

        var writer = new ResultWriter();
        var aiResult = "分析结果";

        // Act
        var result = writer.AppendDiffReport(fileAPath, fileBPath, aiResult, true);

        // Assert
        Assert.True(result.IsSuccess);
        var content = File.ReadAllText(result.OutputPath);
        Assert.Contains("状态: 已截断", content);
    }

    [Fact]
    public void AppendDiffReport_MultipleCalls_AppendsContent()
    {
        // Arrange
        var fileAPath = Path.Combine(_testDirectory, "fileA.txt");
        var fileBPath = Path.Combine(_testDirectory, "fileB.txt");
        File.WriteAllText(fileAPath, "Content A");
        File.WriteAllText(fileBPath, "Content B");

        var writer = new ResultWriter();

        // Act
        var result1 = writer.AppendDiffReport(fileAPath, fileBPath, "第一次分析结果", false);
        var result2 = writer.AppendDiffReport(fileAPath, fileBPath, "第二次分析结果", false);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(result1.OutputPath, result2.OutputPath);

        var content = File.ReadAllText(result1.OutputPath!);
        Assert.Contains("第一次分析结果", content);
        Assert.Contains("第二次分析结果", content);
        Assert.Contains("---", content);
    }

    [Fact]
    public void AppendDiffReport_EmptyPath_ReturnsFailure()
    {
        // Arrange
        var writer = new ResultWriter();

        // Act
        var result = writer.AppendDiffReport("", "fileB.txt", "result");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不能为空", result.ErrorMessage);
    }

    [Fact]
    public void AppendDiffReport_NullAiResult_ReturnsFailure()
    {
        // Arrange
        var fileAPath = Path.Combine(_testDirectory, "fileA.txt");
        var fileBPath = Path.Combine(_testDirectory, "fileB.txt");
        File.WriteAllText(fileAPath, "A");
        File.WriteAllText(fileBPath, "B");

        var writer = new ResultWriter();

        // Act
        var result = writer.AppendDiffReport(fileAPath, fileBPath, null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不能为空", result.ErrorMessage);
    }

    [Fact]
    public void AppendDiffReport_TimestampFormat_IsCorrect()
    {
        // Arrange
        var fileAPath = Path.Combine(_testDirectory, "fileA.txt");
        var fileBPath = Path.Combine(_testDirectory, "fileB.txt");
        File.WriteAllText(fileAPath, "A");
        File.WriteAllText(fileBPath, "B");

        var writer = new ResultWriter();

        // Act
        var result = writer.AppendDiffReport(fileAPath, fileBPath, "result", false);

        // Assert
        Assert.True(result.IsSuccess);
        var content = File.ReadAllText(result.OutputPath!);

        // 验证时间戳格式 yyyy-MM-dd HH:mm:ss
        Assert.Matches(@"> 时间: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", content);
    }

    [Fact]
    public void AppendDiffReport_UsesFirstFileDirectory()
    {
        // Arrange
        var dir1 = Path.Combine(_testDirectory, "dir1");
        var dir2 = Path.Combine(_testDirectory, "dir2");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);

        var fileAPath = Path.Combine(dir1, "fileA.txt");
        var fileBPath = Path.Combine(dir2, "fileB.txt");
        File.WriteAllText(fileAPath, "A");
        File.WriteAllText(fileBPath, "B");

        var writer = new ResultWriter();

        // Act
        var result = writer.AppendDiffReport(fileAPath, fileBPath, "result", false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(dir1, Path.GetDirectoryName(result.OutputPath));
    }

    [Fact]
    public async Task AppendDiffReportAsync_WritesCorrectly()
    {
        // Arrange
        var fileAPath = Path.Combine(_testDirectory, "fileA.txt");
        var fileBPath = Path.Combine(_testDirectory, "fileB.txt");
        File.WriteAllText(fileAPath, "Content A");
        File.WriteAllText(fileBPath, "Content B");

        var writer = new ResultWriter();

        // Act
        var result = await writer.AppendDiffReportAsync(fileAPath, fileBPath, "异步写入结果", false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.OutputPath));
        var content = File.ReadAllText(result.OutputPath!);
        Assert.Contains("异步写入结果", content);
    }

    [Fact]
    public void GetDefaultOutputPath_ReturnsCorrectPath()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");

        // Act
        var outputPath = ResultWriter.GetDefaultOutputPath(filePath);

        // Assert
        Assert.Equal(Path.Combine(_testDirectory, "difference.md"), outputPath);
    }

    [Fact]
    public void GetDefaultOutputPath_EmptyPath_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ResultWriter.GetDefaultOutputPath(""));
    }
}
