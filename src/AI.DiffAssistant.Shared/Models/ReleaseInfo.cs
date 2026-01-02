using System;

namespace AI.DiffAssistant.Shared.Models;

/// <summary>
/// 可下载版本信息
/// </summary>
public class ReleaseInfo
{
    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 发布日期
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// 适用平台
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// 更新说明摘要
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 下载地址
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;
}
