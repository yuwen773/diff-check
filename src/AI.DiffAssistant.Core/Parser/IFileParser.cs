using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Parser;

/// <summary>
/// 文件解析器接口
/// </summary>
public interface IFileParser
{
    /// <summary>
    /// 检查是否支持解析指定扩展名的文件
    /// </summary>
    /// <param name="ext">文件扩展名（如 ".docx"）</param>
    /// <returns>是否支持</returns>
    bool CanParse(string ext);

    /// <summary>
    /// 解析文件并返回结果
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>解析结果</returns>
    ParseResult Parse(string filePath);
}
