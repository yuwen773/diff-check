namespace AI.DiffAssistant.Shared.Models;

/// <summary>
/// AI API 配置
/// </summary>
public class ApiConfig
{
    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = "gpt-4o";
}
