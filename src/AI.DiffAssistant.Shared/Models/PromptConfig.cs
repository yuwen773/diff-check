namespace AI.DiffAssistant.Shared.Models;

/// <summary>
/// 提示词配置
/// </summary>
public class PromptConfig
{
    /// <summary>
    /// 系统提示词
    /// </summary>
    public string SystemPrompt { get; set; } = "你是一个文档对比助手，请对比两份文档，忽略格式差异，重点总结语义上的变化，并用 Markdown 列表输出。";
}
