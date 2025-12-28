namespace AI.DiffAssistant.Shared.Models;

/// <summary>
/// 应用程序根配置
/// </summary>
public class AppConfig
{
    /// <summary>
    /// API 配置
    /// </summary>
    public ApiConfig Api { get; set; } = new();

    /// <summary>
    /// 提示词配置
    /// </summary>
    public PromptConfig Prompts { get; set; } = new();

    /// <summary>
    /// 应用程序设置
    /// </summary>
    public AppSettings Settings { get; set; } = new();

    /// <summary>
    /// 日志配置
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();
}
