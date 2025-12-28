namespace AI.DiffAssistant.Core.Diff;

/// <summary>
/// 结果写入结果
/// </summary>
public class WriteResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 输出文件路径
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static WriteResult Success(string outputPath) =>
        new() { IsSuccess = true, OutputPath = outputPath };

    public static WriteResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// 差异分析结果写入器
/// </summary>
public class ResultWriter
{
    /// <summary>
    /// 默认输出文件名
    /// </summary>
    public const string DefaultOutputFileName = "difference.md";

    /// <summary>
    /// 写入差异报告到文件
    /// </summary>
    /// <param name="fileAPath">文件 A 路径</param>
    /// <param name="fileBPath">文件 B 路径</param>
    /// <param name="aiResult">AI 分析结果</param>
    /// <param name="isTruncated">内容是否被截断</param>
    /// <returns>写入结果</returns>
    public WriteResult AppendDiffReport(string fileAPath, string fileBPath, string aiResult, bool isTruncated = false)
    {
        if (string.IsNullOrWhiteSpace(fileAPath))
            return WriteResult.Failure("文件 A 路径不能为空");

        if (string.IsNullOrWhiteSpace(fileBPath))
            return WriteResult.Failure("文件 B 路径不能为空");

        if (string.IsNullOrWhiteSpace(aiResult))
            return WriteResult.Failure("AI 分析结果不能为空");

        try
        {
            // 确定输出目录（第一个文件所在目录）
            var outputDir = System.IO.Path.GetDirectoryName(fileAPath);
            if (string.IsNullOrEmpty(outputDir))
            {
                return WriteResult.Failure("无法确定输出目录");
            }

            // 如果两个文件在不同目录，以第一个文件为准
            var fileBDir = System.IO.Path.GetDirectoryName(fileBPath);
            if (!string.IsNullOrEmpty(fileBDir) && !System.IO.Directory.Exists(outputDir))
            {
                outputDir = fileBDir;
            }

            var outputPath = System.IO.Path.Combine(outputDir, DefaultOutputFileName);

            // 生成报告内容
            var report = GenerateReport(fileAPath, fileBPath, aiResult, isTruncated);

            // 追加写入文件
            AppendToFile(outputPath, report);

            return WriteResult.Success(outputPath);
        }
        catch (UnauthorizedAccessException)
        {
            return WriteResult.Failure("没有写入权限，请检查文件是否被其他程序占用");
        }
        catch (IOException ex)
        {
            return WriteResult.Failure($"写入文件失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return WriteResult.Failure($"发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 异步写入差异报告
    /// </summary>
    public async Task<WriteResult> AppendDiffReportAsync(
        string fileAPath, string fileBPath, string aiResult, bool isTruncated = false)
    {
        if (string.IsNullOrWhiteSpace(fileAPath))
            return WriteResult.Failure("文件 A 路径不能为空");

        if (string.IsNullOrWhiteSpace(fileBPath))
            return WriteResult.Failure("文件 B 路径不能为空");

        if (string.IsNullOrWhiteSpace(aiResult))
            return WriteResult.Failure("AI 分析结果不能为空");

        try
        {
            // 确定输出目录
            var outputDir = System.IO.Path.GetDirectoryName(fileAPath);
            if (string.IsNullOrEmpty(outputDir))
            {
                return WriteResult.Failure("无法确定输出目录");
            }

            var fileBDir = System.IO.Path.GetDirectoryName(fileBPath);
            if (!string.IsNullOrEmpty(fileBDir) && !System.IO.Directory.Exists(outputDir))
            {
                outputDir = fileBDir;
            }

            var outputPath = System.IO.Path.Combine(outputDir, DefaultOutputFileName);

            // 生成报告内容
            var report = GenerateReport(fileAPath, fileBPath, aiResult, isTruncated);

            // 异步追加写入
            await AppendToFileAsync(outputPath, report);

            return WriteResult.Success(outputPath);
        }
        catch (UnauthorizedAccessException)
        {
            return WriteResult.Failure("没有写入权限，请检查文件是否被其他程序占用");
        }
        catch (IOException ex)
        {
            return WriteResult.Failure($"写入文件失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return WriteResult.Failure($"发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 生成报告内容
    /// </summary>
    private static string GenerateReport(string fileAPath, string fileBPath, string aiResult, bool isTruncated)
    {
        var fileAName = System.IO.Path.GetFileName(fileAPath);
        var fileBName = System.IO.Path.GetFileName(fileBPath);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var status = isTruncated ? "已截断" : "完整";

        // 构建 Markdown 格式的报告
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"## 📅 对比报告: {fileAName} vs {fileBName}");
        sb.AppendLine($"> 时间: {timestamp} | 状态: {status}");
        sb.AppendLine();
        sb.AppendLine(aiResult.Trim());
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// 追加写入文件（同步）
    /// </summary>
    private static void AppendToFile(string filePath, string content)
    {
        // 确保目录存在
        var directory = System.IO.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // 追加写入
        System.IO.File.AppendAllText(filePath, content);
    }

    /// <summary>
    /// 追加写入文件（异步）
    /// </summary>
    private static async Task AppendToFileAsync(string filePath, string content)
    {
        // 确保目录存在
        var directory = System.IO.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // 异步追加写入
        await System.IO.File.AppendAllTextAsync(filePath, content);
    }

    /// <summary>
    /// 获取默认输出路径
    /// </summary>
    public static string GetDefaultOutputPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        var directory = System.IO.Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentException("无法确定输出目录", nameof(filePath));
        }

        return System.IO.Path.Combine(directory, DefaultOutputFileName);
    }
}
