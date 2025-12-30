using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AI.DiffAssistant.Core.Notification;

/// <summary>
/// Windows Toast 通知管理器
/// 使用 Windows 10/11 原生 Toast 通知，不抢占焦点
/// </summary>
public static class NotificationManager
{
    private const string AppId = "AI.DiffAssistant";

    /// <summary>
    /// 初始化通知管理器
    /// </summary>
    public static void Initialize()
    {
        // ToastNotificationManagerCompat 自动初始化，无需手动调用
        // 但需要确保 app.manifest 中声明了 windowsToastNotifications 能力
    }

    /// <summary>
    /// 显示成功通知
    /// </summary>
    /// <param name="message">通知内容</param>
    /// <param name="filePathToOpen">点击通知时打开的文件路径（可选）</param>
    public static void ShowSuccess(string message, string? filePathToOpen = null)
    {
        ShowToast("分析完成", message, false, filePathToOpen);
    }

    /// <summary>
    /// 显示错误通知
    /// </summary>
    /// <param name="error">错误信息</param>
    public static void ShowError(string error)
    {
        ShowToast("分析失败", error, true, null);
    }

    /// <summary>
    /// 显示 Toast 通知
    /// </summary>
    private static void ShowToast(string title, string content, bool isError, string? filePathToOpen)
    {
        try
        {
            // 注册 Toast 激活回调（如果提供了文件路径）
            if (filePathToOpen != null)
            {
                ToastNotificationManagerCompat.OnActivated += args =>
                {
                    if (args.Argument == "action=openFile" || args.Argument == "action=openResult")
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(filePathToOpen)
                            {
                                UseShellExecute = true
                            });
                        }
                        catch (Exception)
                        {
                            // 静默处理
                        }
                    }
                };
            }

            var builder = new ToastContentBuilder()
                .AddArgument("action", "openResult")
                .AddText(title);

            if (isError)
            {
                // 错误通知：仅显示错误信息
                builder.AddText(content);
            }
            else
            {
                // 成功通知：显示结果信息和操作按钮
                builder.AddText(content)
                    .AddButton(new ToastButton()
                        .SetContent("打开文件")
                        .AddArgument("action", "openFile"))
                    .AddButton(new ToastButton()
                        .SetContent("打开文件夹")
                        .AddArgument("action", "openFolder"));
            }

            builder.Show();
        }
        catch (Exception)
        {
            // Toast 通知失败时静默处理，使用控制台输出
            Console.WriteLine($"[{title}] {content}");
        }
    }

    /// <summary>
    /// 注册应用为通知发送者（现代 Windows 自动完成）
    /// </summary>
    public static void RegisterAppForNotification()
    {
        // 通知注册在现代 Windows 上自动完成
    }
}
