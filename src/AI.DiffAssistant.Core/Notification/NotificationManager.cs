using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AI.DiffAssistant.Core.Notification;

/// <summary>
/// Windows Toast 通知管理器
/// </summary>
public static class NotificationManager
{
    private const string AppId = "AI.DiffAssistant";

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONINFORMATION = 0x00000040;
    private const uint MB_ICONERROR = 0x00000010;

    /// <summary>
    /// 初始化通知管理器
    /// </summary>
    public static void Initialize()
    {
        // 通知管理器初始化
    }

    /// <summary>
    /// 显示成功通知
    /// </summary>
    /// <param name="message">通知内容</param>
    public static void ShowSuccess(string message)
    {
        ShowToast("分析完成 (已追加)", message, false);
    }

    /// <summary>
    /// 显示错误通知
    /// </summary>
    /// <param name="error">错误信息</param>
    public static void ShowError(string error)
    {
        ShowToast("分析失败", error, true);
    }

    /// <summary>
    /// 显示 Toast 通知
    /// </summary>
    private static void ShowToast(string title, string content, bool isError)
    {
        try
        {
            var icon = isError ? MB_ICONERROR : MB_ICONINFORMATION;
            MessageBox(IntPtr.Zero, content, title, MB_OK | icon);
        }
        catch (Exception)
        {
            // 通知失败时静默处理，使用控制台输出
            Console.WriteLine($"[{title}] {content}");
        }
    }

    /// <summary>
    /// 注册应用为通知发送者
    /// </summary>
    public static void RegisterAppForNotification()
    {
        // 通知注册在现代 Windows 上自动完成
    }
}
