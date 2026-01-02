namespace AI.DiffAssistant.Shared.Models;

/// <summary>
/// 日志配置
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// 是否启用日志
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 日志文件路径（支持环境变量如 %TEMP%）
    /// </summary>
    public string LogPath { get; set; } = "%TEMP%\\diff-check.log";

    /// <summary>
    /// 日志级别（逗号分隔：Error, Warning）
    /// </summary>
    public string Level { get; set; } = "Error,Warning";

    /// <summary>
    /// 获取解析后的日志路径
    /// </summary>
    public string GetResolvedPath()
    {
        var path = LogPath;
        if (path.Contains("%TEMP%"))
        {
            path = path.Replace("%TEMP%", Path.GetTempPath());
        }
        if (path.Contains("%USERPROFILE%"))
        {
            path = path.Replace("%USERPROFILE%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }
        return path;
    }

    /// <summary>
    /// 获取启用的日志级别集合
    /// </summary>
    public HashSet<string> GetEnabledLevels()
    {
        return Level.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
