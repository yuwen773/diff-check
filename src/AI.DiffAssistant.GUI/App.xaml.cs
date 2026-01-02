using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using WpfApplication = System.Windows.Application;

namespace AI.DiffAssistant.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication
{
    private const string MutexName = "AI.DiffAssistant.SingleInstance.Mutex";
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
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
    private static bool _isDarkTheme = false;

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
        var themeUri = IsDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

        // 重新加载资源字典
        if (Current?.Resources.MergedDictionaries.Count > 0)
        {
            var dict = Current.Resources.MergedDictionaries[0];
            if (dict.Source?.OriginalString != themeUri)
            {
                dict.Source = new Uri(themeUri, UriKind.Relative);
            }
        }
    }
}
