using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace AI.DiffAssistant.GUI;

/// <summary>
/// 系统托盘管理器 - 负责托盘图标的显示、交互和菜单
/// </summary>
public class SystemTrayManager : IDisposable
{
    private readonly MainWindow _mainWindow;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;

    public SystemTrayManager(MainWindow mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
    }

    /// <summary>
    /// 初始化系统托盘
    /// </summary>
    public void Initialize()
    {
        try
        {
            // 确保在 UI 线程上初始化
            if (_mainWindow.Dispatcher.CheckAccess())
            {
                CreateNotifyIcon();
                CreateContextMenu();
                SetupEventHandlers();
            }
            else
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    CreateNotifyIcon();
                    CreateContextMenu();
                    SetupEventHandlers();
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"托盘初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建托盘图标
    /// </summary>
    private void CreateNotifyIcon()
    {
        if (_notifyIcon != null) return;

        _notifyIcon = new NotifyIcon
        {
            Text = "diff-check",
            Icon = GetTrayIcon(),
            Visible = true
        };
    }

    /// <summary>
    /// 获取托盘图标
    /// </summary>
    private Icon GetTrayIcon()
    {
        try
        {
            // 尝试从嵌入资源或文件加载图标
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "AI.DiffAssistant.GUI.Assets.diff-check.ico";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                return new Icon(stream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载托盘图标资源失败: {ex.Message}");
        }

        // 备用方案：从文件加载
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diff-check.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"从文件加载托盘图标失败: {ex.Message}");
        }

        // 最后的备用方案：创建默认图标
        return CreateDefaultIcon();
    }

    /// <summary>
    /// 创建默认图标
    /// </summary>
    private Icon CreateDefaultIcon()
    {
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(System.Drawing.Color.Transparent);
        using var pen = new System.Drawing.Pen(System.Drawing.Color.Blue, 2);
        graphics.DrawEllipse(pen, 2, 2, 12, 12);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>
    /// 创建右键菜单
    /// </summary>
    private void CreateContextMenu()
    {
        _contextMenu = new ContextMenuStrip();

        // 主面板项
        var mainPanelItem = new ToolStripMenuItem("主面板", null, (s, e) => ShowMainWindow())
        {
            Name = "MainPanel"
        };
        _contextMenu.Items.Add(mainPanelItem);

        // 分隔线
        _contextMenu.Items.Add(new ToolStripSeparator());

        // 关于项 - 切换到主窗口的"关于"Tab
        var aboutItem = new ToolStripMenuItem("关于", null, (s, e) => ShowAboutTab())
        {
            Name = "About"
        };
        _contextMenu.Items.Add(aboutItem);

        // 退出项
        var exitItem = new ToolStripMenuItem("退出", null, (s, e) => ExitApplication())
        {
            Name = "Exit"
        };
        _contextMenu.Items.Add(exitItem);

        _notifyIcon!.ContextMenuStrip = _contextMenu;
    }

    /// <summary>
    /// 设置事件处理器
    /// </summary>
    private void SetupEventHandlers()
    {
        // 双击托盘图标显示/隐藏主窗口
        _notifyIcon!.MouseDoubleClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        };
    }

    /// <summary>
    /// 显示主窗口
    /// </summary>
    private void ShowMainWindow()
    {
        RunOnUiThread(() =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[托盘] ShowMainWindow - IsVisible={_mainWindow.IsVisible}, Visibility={_mainWindow.Visibility}");
                _mainWindow.RestoreFromTray();

                // 强制重置窗口状态
                _mainWindow.Visibility = Visibility.Hidden;
                _mainWindow.Show();
                _mainWindow.Hide();

                // 强制创建窗口句柄
                var tempHandle = new WindowInteropHelper(_mainWindow).Handle;
                System.Diagnostics.Debug.WriteLine($"[托盘] 临时句柄: {tempHandle}");

                // 完全重置窗口
                _mainWindow.WindowState = WindowState.Minimized;
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();
                _mainWindow.Activate();
                _mainWindow.Focus();

                var handle = new WindowInteropHelper(_mainWindow).Handle;
                System.Diagnostics.Debug.WriteLine($"[托盘] 最终句柄: {handle}");

                if (handle != IntPtr.Zero)
                {
                    // 确保窗口显示并激活
                    ShowWindow(handle, SW_RESTORE);
                    SetWindowPos(handle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    SetForegroundWindow(handle);
                }

                System.Diagnostics.Debug.WriteLine($"[托盘] 最终 - IsVisible={_mainWindow.IsVisible}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示主窗口失败: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 闪烁窗口和任务栏图标
    /// </summary>
    private void FlashWindow(IntPtr handle, int count)
    {
        try
        {
            var flashInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = handle,
                dwFlags = FLASHW_ALL,
                uCount = (uint)count,
                dwTimeout = 100
            };
            FlashWindowEx(ref flashInfo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"闪烁窗口失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换到主窗口的"关于"页面
    /// </summary>
    private void ShowAboutTab()
    {
        RunOnUiThread(() =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[托盘] ShowAboutTab - IsVisible={_mainWindow.IsVisible}");
                _mainWindow.RestoreFromTray();

                // 强制重置窗口状态
                _mainWindow.Visibility = Visibility.Hidden;
                _mainWindow.Show();
                _mainWindow.Hide();

                _mainWindow.WindowState = WindowState.Minimized;
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();

                // 切换到"关于"Tab
                var tabControl = FindVisualChild<System.Windows.Controls.TabControl>(_mainWindow);
                if (tabControl != null)
                {
                    foreach (var item in tabControl.Items)
                    {
                        if (item is System.Windows.Controls.TabItem tabItem && tabItem.Header?.ToString() == "关于")
                        {
                            tabItem.IsSelected = true;
                            break;
                        }
                    }
                }

                var handle = new WindowInteropHelper(_mainWindow).Handle;
                if (handle != IntPtr.Zero)
                {
                    ShowWindow(handle, SW_RESTORE);
                    SetWindowPos(handle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    SetForegroundWindow(handle);
                }

                _mainWindow.Activate();
                _mainWindow.Focus();

                System.Diagnostics.Debug.WriteLine($"[托盘] About 窗口应已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示关于页面失败: {ex.Message}");
            }
        });
    }

    // Windows API 常量
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private static readonly IntPtr HWND_TOP = new IntPtr(0);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;
    private const int SW_MINIMIZE = 6;
    private const int FLASHW_STOP = 0;
    private const int FLASHW_CAPTION = 0x0001;
    private const int FLASHW_TRAY = 0x0002;
    private const int FLASHW_ALL = FLASHW_CAPTION | FLASHW_TRAY;
    private const int FLASHW_TIMERNOFG = 0x000C;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    /// <summary>
    /// 查找可视子元素
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        RunOnUiThread(() =>
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                }

                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();

                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"退出应用程序失败: {ex.Message}");
            }
        });
    }

    private void RunOnUiThread(Action action)
    {
        // 使用 Dispatcher.Invoke 确保同步执行（关键修复）
        if (_mainWindow.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _mainWindow.Dispatcher.Invoke(action);
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }

            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"托盘资源清理失败: {ex.Message}");
        }
    }
}
