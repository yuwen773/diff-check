using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AI.DiffAssistant.Core.Config;
using AI.DiffAssistant.Core.Diff;
using AI.DiffAssistant.Core.File;
using AI.DiffAssistant.Core.Logging;
using AI.DiffAssistant.Core.Notification;
using AI.DiffAssistant.Core.Util;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Cli;

/// <summary>
/// 退出代码
/// </summary>
public enum ExitCode
{
    Success = 0,
    FileNotFound = 1,
    AiCallFailed = 2,
    FileWriteFailed = 3,
    ConfigurationError = 4,
    OtherError = 99
}

/// <summary>
/// CLI 主入口类 - 支持静默模式和参数协调
/// </summary>
public class Program
{
    /// <summary>
    /// Windows API: 释放控制台
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    /// <summary>
    /// 日志服务实例
    /// </summary>
    private static LoggingService? _logger;

    /// <summary>
    /// 主入口点
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        // 如果有文件参数，隐藏控制台窗口
        if (args.Length > 0)
        {
            FreeConsole();
        }

        // 初始化日志服务
        InitLogging();

        // 初始化通知管理器（延迟初始化，在知道输出路径后更新）
        NotificationManager.Initialize();

        try
        {
            Log(LoggingService.Levels.Info, $"启动 - 参数数量: {args.Length}");

            // 解析命令行参数
            var parseResult = ArgsParser.Parse(args);

            // GUI 模式：启动 GUI
            if (parseResult.IsValid && parseResult.IsGuiMode)
            {
                Log(LoggingService.Levels.Info, "启动 GUI 模式");
                return await LaunchGuiMode();
            }

            // 参数错误
            if (!parseResult.IsValid)
            {
                var error = parseResult.ErrorMessage ?? "未知错误";
                Log(LoggingService.Levels.Error, $"参数解析失败: {error}");
                ShowErrorNotification($"参数错误: {error}");
                return (int)ExitCode.FileNotFound;
            }

            // 静默模式：执行分析流程
            return await RunSilentModeAsync(parseResult);
        }
        catch (Exception ex)
        {
            Log(LoggingService.Levels.Error, $"异常: {ex.Message}");
            return (int)ExitCode.OtherError;
        }
    }

    /// <summary>
    /// 初始化日志服务
    /// </summary>
    private static void InitLogging()
    {
        try
        {
            var configManager = new ConfigManager();
            var config = configManager.LoadConfig();
            _logger = new LoggingService(config.Logging);
        }
        catch
        {
            // 如果加载配置失败，使用默认日志配置
            _logger = new LoggingService(new LoggingConfig());
        }
    }

    /// <summary>
    /// 写入日志
    /// </summary>
    private static void Log(string level, string message)
    {
        _logger?.Log(level, message);
    }

    /// <summary>
    /// GUI 模式：启动 WPF 配置中心
    /// </summary>
    private static async Task<int> LaunchGuiMode()
    {
        try
        {
            var guiPath = Path.Combine(AppContext.BaseDirectory, "AI.DiffAssistant.GUI.exe");

            if (File.Exists(guiPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = guiPath,
                    UseShellExecute = true
                });
            }
            else
            {
                var altGuiPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..",
                    "AI.DiffAssistant.GUI", "bin", "Release", "net10.0-windows",
                    "AI.DiffAssistant.GUI.exe");

                if (File.Exists(altGuiPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = altGuiPath,
                        UseShellExecute = true
                    });
                }
            }

            await Task.CompletedTask;
            return 0;
        }
        catch (Exception ex)
        {
            Log(LoggingService.Levels.Error, $"启动 GUI 失败: {ex.Message}");
            return (int)ExitCode.OtherError;
        }
    }

    /// <summary>
    /// 静默模式：执行文件对比分析
    /// </summary>
    private static async Task<int> RunSilentModeAsync(ParseResult parseResult)
    {
        string fileA;
        string fileB;

        // 如果只有一个文件（等待模式），使用协调机制收集参数
        if (parseResult.IsWaitingMode && parseResult.FilePaths.Count == 1)
        {
            var firstFile = parseResult.FilePaths[0];
            Log(LoggingService.Levels.Info, $"协调 - 收到第一个文件: {firstFile}");

            // 使用协调机制收集参数
            var collectedArgs = ArgsParser.CoordinateArguments(firstFile);

            // 如果收集到两个文件，使用收集到的路径
            if (collectedArgs.Count >= 2)
            {
                fileA = collectedArgs[0];
                fileB = collectedArgs[1];
                Log(LoggingService.Levels.Info, $"协调 - 收集到两个文件: {fileA}, {fileB}");
            }
            else
            {
                // 只收集到一个文件，检查协调文件状态
                var globalArgsPath = ArgsParser.GetGlobalArgsFilePath();
                if (ArgsParser.ShouldExitWait(globalArgsPath))
                {
                    // 协调文件被删除，说明其他实例可能在执行
                    Log(LoggingService.Levels.Info, "协调 - 协调文件已删除，其他实例可能在执行，退出");
                    return (int)ExitCode.Success;
                }
                else
                {
                    Log(LoggingService.Levels.Warning, $"协调后只有 {collectedArgs.Count} 个文件");
                    ShowErrorNotification("请选择两个文件进行对比分析");
                    return (int)ExitCode.FileNotFound;
                }
            }
        }
        else
        {
            // 直接使用解析的文件路径
            if (parseResult.FilePaths.Count < 2)
            {
                Log(LoggingService.Levels.Error, $"文件数量不足: {parseResult.FilePaths.Count}");
                ShowErrorNotification("请选择两个文件进行对比分析");
                return (int)ExitCode.FileNotFound;
            }

            fileA = parseResult.FilePaths[0];
            fileB = parseResult.FilePaths[1];
            Log(LoggingService.Levels.Info, $"参数 - 直接收到两个文件: {fileA}, {fileB}");
        }

        // 使用单实例互斥锁确保只有一个实例执行
        using var mutex = new SingleInstanceManager();

        if (!mutex.IsFirstInstance)
        {
            Log(LoggingService.Levels.Info, "单例 - 检测到其他实例在运行，退出");
            return (int)ExitCode.Success;
        }

        try
        {
            // 步骤 1: 加载配置
            Log(LoggingService.Levels.Info, "配置 - 加载配置...");
            var configManager = new ConfigManager();
            var config = configManager.LoadConfig();

            // 更新日志服务配置（如果配置已更改）
            if (_logger != null)
            {
                _logger = new LoggingService(config.Logging);
            }

            if (string.IsNullOrWhiteSpace(config.Api.BaseUrl) ||
                string.IsNullOrWhiteSpace(config.Api.ApiKey) ||
                string.IsNullOrWhiteSpace(config.Api.Model))
            {
                Log(LoggingService.Levels.Error, "AI 服务未配置");
                ShowErrorNotification("请先在配置中心设置 API 信息");
                return (int)ExitCode.ConfigurationError;
            }

            // 步骤 2: 读取并处理文件
            Log(LoggingService.Levels.Info, $"文件 - 读取文件 A: {fileA}");
            var processor = new FileProcessor(config.Settings.MaxTokenLimit);

            var (readA, truncateA) = await processor.ProcessFileAsync(fileA);
            bool isTruncatedA = truncateA.IsTruncated;
            var encodingAName = readA.Encoding?.EncodingName ?? "N/A (docx/pdf)";
            Log(LoggingService.Levels.Info, $"文件 - 文件 A 读取完成，编码: {encodingAName}");

            Log(LoggingService.Levels.Info, $"文件 - 读取文件 B: {fileB}");
            var (readB, truncateB) = await processor.ProcessFileAsync(fileB);
            bool isTruncatedB = truncateB.IsTruncated;
            var encodingBName = readB.Encoding?.EncodingName ?? "N/A (docx/pdf)";
            Log(LoggingService.Levels.Info, $"文件 - 文件 B 读取完成，编码: {encodingBName}");

            var isTruncated = isTruncatedA || isTruncatedB;

            // 步骤 3: 调用 AI 分析
            Log(LoggingService.Levels.Info, "AI - 开始调用 AI 服务...");
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            var analyzer = new DiffAnalyzer(httpClient, config);
            var analysisResult = await analyzer.AnalyzeContentAsync(
                readA.FileName, readA.Content, isTruncatedA,
                readB.FileName, readB.Content, isTruncatedB);

            if (!analysisResult.IsSuccess)
            {
                var errorMsg = analysisResult.ErrorMessage ?? "AI 分析失败";
                Log(LoggingService.Levels.Error, $"AI 分析失败: {errorMsg}");
                ShowErrorNotification($"AI 分析失败: {errorMsg}");
                return (int)ExitCode.AiCallFailed;
            }

            Log(LoggingService.Levels.Info, "AI - AI 分析完成");

            // 步骤 4: 追加写入结果
            Log(LoggingService.Levels.Info, "写入 - 写入分析结果...");
            var writer = new ResultWriter();
            var writeResult = await writer.AppendDiffReportAsync(fileA, fileB, analysisResult.Content!, isTruncated);

            if (!writeResult.IsSuccess)
            {
                var errorMsg = writeResult.ErrorMessage ?? "写入失败";
                Log(LoggingService.Levels.Error, $"写入失败: {errorMsg}");
                ShowErrorNotification($"写入失败: {errorMsg}");
                return (int)ExitCode.FileWriteFailed;
            }

            Log(LoggingService.Levels.Info, $"完成 - 结果已写入: {writeResult.OutputPath}");

            // 步骤 5: 显示成功通知
            ShowSuccessNotification(writeResult.OutputPath!);

            return (int)ExitCode.Success;
        }
        catch (Exception ex)
        {
            Log(LoggingService.Levels.Error, $"异常: {ex.Message}");
            ShowErrorNotification($"发生错误: {ex.Message}");
            return (int)ExitCode.OtherError;
        }
    }

    /// <summary>
    /// 显示成功通知
    /// </summary>
    private static void ShowSuccessNotification(string outputPath)
    {
        try
        {
            var fileName = Path.GetFileName(outputPath);
            var message = $"差异分析成功，结果已写入 {fileName}";
            Log(LoggingService.Levels.Info, $"通知 - 成功: {message}");
            // 传递 outputPath 以支持点击通知打开文件
            NotificationManager.ShowSuccess(message, outputPath);
        }
        catch (Exception ex)
        {
            Log(LoggingService.Levels.Error, $"通知 - 成功通知失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示错误通知
    /// </summary>
    private static void ShowErrorNotification(string error)
    {
        try
        {
            Log(LoggingService.Levels.Warning, $"通知 - 错误: {error}");
            NotificationManager.ShowError(error);
        }
        catch (Exception ex)
        {
            Log(LoggingService.Levels.Error, $"通知 - 错误通知失败: {ex.Message}");
        }
    }
}
