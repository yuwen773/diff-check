using System.Collections.Immutable;
using System.IO;
using System.Threading;

namespace AI.DiffAssistant.Cli;

/// <summary>
/// 命令行参数解析结果
/// </summary>
public class ParseResult
{
    public bool IsValid { get; init; }
    public bool IsGuiMode { get; init; }
    public bool IsWaitingMode { get; init; } // 是否在等待模式（第一个实例）
    public IImmutableList<string> FilePaths { get; init; } = ImmutableArray<string>.Empty;
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 命令行参数解析器 - 支持多实例参数协调
/// </summary>
public static class ArgsParser
{
    /// <summary>
    /// 全局参数文件名（用于多实例参数收集）
    /// </summary>
    private const string GlobalArgsFileName = "AI.DiffAssistant.args";

    /// <summary>
    /// 协调互斥锁名称
    /// </summary>
    public const string CoordinatorMutexName = @"Global\AI.DiffAssistant.ArgsCoordinator";

    /// <summary>
    /// 解析命令行参数
    /// </summary>
    /// <param name="args">原始命令行参数</param>
    /// <returns>解析结果</returns>
    public static ParseResult Parse(string[] args)
    {
        // 0 个参数：启动 GUI 模式
        if (args.Length == 0)
        {
            return new ParseResult
            {
                IsValid = true,
                IsGuiMode = true
            };
        }

        // 1 个参数：多实例启动，需要协调
        if (args.Length == 1)
        {
            var filePath = args[0];

            // 验证文件存在
            if (!File.Exists(filePath))
            {
                return new ParseResult
                {
                    IsValid = false,
                    ErrorMessage = $"文件不存在: {Path.GetFileName(filePath)}"
                };
            }

            return new ParseResult
            {
                IsValid = true,
                IsGuiMode = false,
                IsWaitingMode = true, // 进入等待模式
                FilePaths = ImmutableArray.Create(filePath)
            };
        }

        // 2 个参数：直接执行静默分析
        if (args.Length == 2)
        {
            var fileA = args[0];
            var fileB = args[1];

            // 验证文件存在
            if (!File.Exists(fileA) || !File.Exists(fileB))
            {
                return new ParseResult
                {
                    IsValid = false,
                    ErrorMessage = "参数中包含不存在的文件"
                };
            }

            return new ParseResult
            {
                IsValid = true,
                IsGuiMode = false,
                FilePaths = ImmutableArray.Create(fileA, fileB)
            };
        }

        // 超过 2 个参数：取前两个
        if (args.Length > 2)
        {
            var fileA = args[0];
            var fileB = args[1];

            if (!File.Exists(fileA) || !File.Exists(fileB))
            {
                return new ParseResult
                {
                    IsValid = false,
                    ErrorMessage = "参数中包含不存在的文件"
                };
            }

            return new ParseResult
            {
                IsValid = true,
                IsGuiMode = false,
                FilePaths = ImmutableArray.Create(fileA, fileB)
            };
        }

        return new ParseResult
        {
            IsValid = false,
            ErrorMessage = "未知参数错误"
        };
    }

    /// <summary>
    /// 获取全局参数文件路径
    /// </summary>
    public static string GetGlobalArgsFilePath()
    {
        var tempDir = Path.GetTempPath();
        return Path.Combine(tempDir, GlobalArgsFileName);
    }

    /// <summary>
    /// 协调并收集参数 - 使用互斥锁 + 文件锁
    /// </summary>
    /// <param name="currentFilePath">当前文件路径</param>
    /// <returns>收集到的文件路径列表</returns>
    public static List<string> CoordinateArguments(string currentFilePath)
    {
        var result = new List<string> { currentFilePath };
        var globalArgsPath = GetGlobalArgsFilePath();

        using var coordinatorMutex = new Mutex(false, CoordinatorMutexName);

        try
        {
            bool mutexAcquired = false;
            try
            {
                mutexAcquired = coordinatorMutex.WaitOne(500, false);
            }
            catch (AbandonedMutexException)
            {
                mutexAcquired = true;
            }

            if (!mutexAcquired)
            {
                // 无法获取互斥锁，直接返回
                return result;
            }

            try
            {
                if (File.Exists(globalArgsPath))
                {
                    // 已有协调文件，读取并合并
                    var existingLines = ReadGlobalArgsFile(globalArgsPath);
                    foreach (var line in existingLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !result.Contains(line))
                        {
                            result.Add(line);
                        }
                    }

                    // 如果已有两个或更多文件，说明协调已完成
                    // 清理文件并返回（让其他实例知道已完成）
                    try { File.Delete(globalArgsPath); } catch { }
                }
                else
                {
                    // 创建协调文件，标记开始协调
                    WriteGlobalArgsFile(globalArgsPath, new[] { currentFilePath });

                    // 释放互斥锁，让其他实例可以写入
                    coordinatorMutex.ReleaseMutex();
                    coordinatorMutex.Dispose();

                    // 等待其他实例（最多 2 秒）
                    result = WaitForSecondArgument(currentFilePath, globalArgsPath);
                }
            }
            finally
            {
                if (mutexAcquired)
                {
                    try { coordinatorMutex.ReleaseMutex(); } catch { }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 无法使用互斥锁，直接返回
        }
        catch
        {
            // 其他错误，返回当前参数
        }

        return result;
    }

    /// <summary>
    /// 等待并收集第二个参数
    /// </summary>
    private static List<string> WaitForSecondArgument(string currentFilePath, string globalArgsPath)
    {
        var result = new List<string> { currentFilePath };
        var maxWait = 2000; // 最多等待 2 秒
        var checkInterval = 50;
        var waited = 0;

        while (waited < maxWait)
        {
            Thread.Sleep(checkInterval);
            waited += checkInterval;

            try
            {
                // 检查协调文件是否还存在（不存在说明已完成协调）
                if (!File.Exists(globalArgsPath))
                {
                    // 协调已完成，退出
                    return result;
                }

                var lines = ReadGlobalArgsFile(globalArgsPath);
                if (lines.Count >= 2)
                {
                    // 找到第二个文件
                    var secondFile = lines.FirstOrDefault(f => f != currentFilePath);
                    if (secondFile != null)
                    {
                        result.Add(secondFile);

                        // 标记为执行者，清理文件（通知其他等待实例）
                        try { File.Delete(globalArgsPath); } catch { }
                        return result;
                    }
                }
            }
            catch { }
        }

        // 超时，清理
        try { File.Delete(globalArgsPath); } catch { }
        return result;
    }

    /// <summary>
    /// 读取全局参数文件（带重试）
    /// </summary>
    private static List<string> ReadGlobalArgsFile(string path)
    {
        var lines = new List<string>();
        int retries = 3;

        while (retries > 0)
        {
            try
            {
                lines = File.ReadAllLines(path)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();
                break;
            }
            catch (IOException) when (retries-- > 0)
            {
                Thread.Sleep(10); // 短暂等待后重试
            }
        }

        return lines;
    }

    /// <summary>
    /// 写入全局参数文件（带重试）
    /// </summary>
    private static void WriteGlobalArgsFile(string path, string[] lines)
    {
        int retries = 3;

        while (retries > 0)
        {
            try
            {
                File.WriteAllLines(path, lines);
                break;
            }
            catch (IOException) when (retries-- > 0)
            {
                Thread.Sleep(10);
            }
        }
    }

    /// <summary>
    /// 检查是否应该退出等待（检测到协调完成信号）
    /// </summary>
    public static bool ShouldExitWait(string globalArgsPath)
    {
        try
        {
            return !File.Exists(globalArgsPath);
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// 获取使用说明
    /// </summary>
    public static string GetUsage()
    {
        return @"AI 文档差异助手 - 命令行模式

用法:
  AI.DiffAssistant.Cli.exe              - 启动 GUI 配置中心
  AI.DiffAssistant.Cli.exe fileA fileB - 对比两个文件并生成分析报告

参数:
  fileA - 第一个文件路径
  fileB - 第二个文件路径

示例:
  AI.DiffAssistant.Cli.exe ""C:\docs\version1.txt"" ""C:\docs\version2.txt""
";
    }
}
