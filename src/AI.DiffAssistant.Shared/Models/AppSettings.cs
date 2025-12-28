namespace AI.DiffAssistant.Shared.Models;

/// <summary>
/// 应用程序设置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 最大 Token 限制（字符数）
    /// </summary>
    public int MaxTokenLimit { get; set; } = 15000;
}
