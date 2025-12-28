using System.IO;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Logging;

/// <summary>
/// 日志服务
/// </summary>
public class LoggingService
{
    private readonly LoggingConfig _config;
    private readonly string _resolvedPath;
    private readonly HashSet<string> _enabledLevels;
    private readonly object _lock = new();

    /// <summary>
    /// 日志级别常量
    /// </summary>
    public static class Levels
    {
        public const string Error = "Error";
        public const string Warning = "Warning";
        public const string Info = "Info";
        public const string Debug = "Debug";
    }

    /// <summary>
    /// 初始化日志服务
    /// </summary>
    public LoggingService(LoggingConfig config)
    {
        _config = config;
        _resolvedPath = config.GetResolvedPath();
        _enabledLevels = config.GetEnabledLevels();

        // 确保日志目录存在
        EnsureDirectoryExists();
    }

    /// <summary>
    /// 确保日志目录存在
    /// </summary>
    private void EnsureDirectoryExists()
    {
        try
        {
            var directory = Path.GetDirectoryName(_resolvedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch
        {
            // 忽略目录创建失败
        }
    }

    /// <summary>
    /// 写入日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    public void Log(string level, string message)
    {
        if (!_config.Enabled) return;
        if (!_enabledLevels.Contains(level)) return;

        try
        {
            lock (_lock)
            {
                // 检查文件大小，超过 100KB 则清空
                CheckFileSize();

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] [{level}] {message}";

                System.IO.File.AppendAllText(_resolvedPath, logLine + Environment.NewLine);
            }
        }
        catch
        {
            // 忽略写入失败
        }
    }

    /// <summary>
    /// 检查并限制日志文件大小
    /// </summary>
    private void CheckFileSize()
    {
        try
        {
            if (System.IO.File.Exists(_resolvedPath))
            {
                var fileInfo = new FileInfo(_resolvedPath);
                if (fileInfo.Length > 100 * 1024) // 100KB
                {
                    System.IO.File.Delete(_resolvedPath);
                }
            }
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 获取日志文件路径
    /// </summary>
    public string GetLogPath() => _resolvedPath;

    /// <summary>
    /// 清除日志文件
    /// </summary>
    public void Clear()
    {
        try
        {
            lock (_lock)
            {
                if (System.IO.File.Exists(_resolvedPath))
                {
                    System.IO.File.Delete(_resolvedPath);
                }
            }
        }
        catch
        {
            // 忽略清除失败
        }
    }

    /// <summary>
    /// 获取日志文件内容
    /// </summary>
    public string? GetLogContent()
    {
        try
        {
            if (System.IO.File.Exists(_resolvedPath))
            {
                return System.IO.File.ReadAllText(_resolvedPath);
            }
        }
        catch
        {
            // 忽略读取失败
        }
        return null;
    }

    /// <summary>
    /// 日志文件是否存在
    /// </summary>
    public bool LogFileExists() => System.IO.File.Exists(_resolvedPath);
}
