namespace AI.DiffAssistant.Shared.Constants;

/// <summary>
/// 配置相关常量
/// </summary>
public static class ConfigConstants
{
    /// <summary>
    /// 应用程序名称
    /// </summary>
    public const string AppName = "diff-check";

    /// <summary>
    /// 旧版应用程序名称（用于迁移配置）
    /// </summary>
    public const string LegacyAppName = "AI.DiffAssistant";

    /// <summary>
    /// 配置文件名
    /// </summary>
    public const string ConfigFileName = "config.json";

    /// <summary>
    /// 默认配置文件内容
    /// </summary>
    public const string DefaultConfigJson = """
        {
          "api": {
            "baseUrl": "https://api.openai.com/v1",
            "apiKey": "",
            "model": "gpt-4o"
          },
          "prompts": {
            "system": "你是一个文档对比助手，请对比两份文档，忽略格式差异，重点总结语义上的变化，并用 Markdown 列表输出。"
          },
          "settings": {
            "maxTokenLimit": 15000
          }
        }
        """;
}
