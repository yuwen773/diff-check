using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using AI.DiffAssistant.GUI.Views;

namespace AI.DiffAssistant.GUI;

/// <summary>
/// 系统托盘管理器 - 负责托盘图标的显示、交互和菜单
/// </summary>
public class SystemTrayManager : IDisposable
{
    private readonly MainWindow _mainWindow;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private AboutWindow? _aboutWindow;

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
            CreateNotifyIcon();
            CreateContextMenu();
            SetupEventHandlers();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"托盘初始化失败: {ex.Message}");
            // 即使托盘初始化失败，程序也应能正常运行
        }
    }

    /// <summary>
    /// 创建托盘图标
    /// </summary>
    private void CreateNotifyIcon()
    {
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
        graphics.Clear(Color.Transparent);
        using var pen = new Pen(Color.Blue, 2);
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

        // 关于项
        var aboutItem = new ToolStripMenuItem("关于", null, (s, e) => ShowAboutWindow())
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
                ToggleMainWindow();
            }
        };
    }

    /// <summary>
    /// 显示主窗口
    /// </summary>
    private void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        _mainWindow.Focus();
    }

    /// <summary>
    /// 隐藏主窗口
    /// </summary>
    private void HideMainWindow()
    {
        _mainWindow.Hide();
    }

    /// <summary>
    /// 切换主窗口显示状态
    /// </summary>
    private void ToggleMainWindow()
    {
        if (_mainWindow.IsVisible)
        {
            HideMainWindow();
        }
        else
        {
            ShowMainWindow();
        }
    }

    /// <summary>
    /// 显示关于窗口
    /// </summary>
    private void ShowAboutWindow()
    {
        try
        {
            if (_aboutWindow == null || !_aboutWindow.IsVisible)
            {
                _aboutWindow = new AboutWindow();
                _aboutWindow.ShowDialog();
            }
            else
            {
                _aboutWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示关于窗口失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        try
        {
            _mainWindow.Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"退出应用程序失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
            _aboutWindow?.Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"托盘资源清理失败: {ex.Message}");
        }
    }
}
