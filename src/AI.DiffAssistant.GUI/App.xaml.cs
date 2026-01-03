using System;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using WpfApplication = System.Windows.Application;

namespace AI.DiffAssistant.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication
{
    private const string MutexName = "AI.DiffAssistant.SingleInstance.Mutex";
    private Mutex? _mutex;
    private static bool _isDarkTheme = false;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 检测系统主题并应用 (必须在窗口创建前)
        DetectAndApplySystemTheme();

        // 检查是否已有实例在运行
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // 已有实例，激活现有窗口并退出
            ActivateExistingInstance();
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    /// <summary>
    /// 检测系统主题并应用
    /// </summary>
    private static void DetectAndApplySystemTheme()
    {
        try
        {
            // 检查 Windows 注册表中的主题设置
            // 使用 64位和32位视图
            using var key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using var key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

            object? appsUseLightTheme = key32.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("AppsUseLightTheme")
                                ?? key64.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("AppsUseLightTheme");

            if (appsUseLightTheme != null)
            {
                // AppsUseLightTheme 为 1 表示浅色主题，0 表示深色主题
                int themeValue = Convert.ToInt32(appsUseLightTheme);
                _isDarkTheme = themeValue == 0;
                ApplyTheme();
            }
            else
            {
                // 默认使用浅色主题
                _isDarkTheme = false;
                ApplyTheme();
            }
        }
        catch
        {
            // 如果检测失败，使用浅色主题
            _isDarkTheme = false;
            ApplyTheme();
        }
    }

    /// <summary>
    /// 激活现有实例
    /// </summary>
    private void ActivateExistingInstance()
    {
        try
        {
            // 查找现有窗口并激活
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var processes = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (var process in processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    // 尝试激活窗口
                    var hWnd = process.MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        // 显示窗口
                        ShowWindow(hWnd, SW_RESTORE);
                        SetForegroundWindow(hWnd);
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"激活现有实例失败: {ex.Message}");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    // Windows API 调用
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    public static bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme != value)
            {
                _isDarkTheme = value;
                ApplyTheme();
            }
        }
    }

    public static void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    private static void ApplyTheme()
    {
        // 资源合并顺序: LightTheme[0], DarkTheme[1], AcrylicBrushes[2], Animations[3], CommonControls[4]
        var themeUri = IsDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

        if (Current?.Resources.MergedDictionaries.Count > 0)
        {
            // 主题资源在索引 0 和 1
            foreach (var dict in Current.Resources.MergedDictionaries.Take(2))
            {
                if (dict.Source?.OriginalString.Contains("Theme") == true)
                {
                    dict.Source = new Uri(themeUri, UriKind.Relative);
                }
            }
        }
    }
}
