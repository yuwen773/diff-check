using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AI.DiffAssistant.GUI.ViewModels;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfButton = System.Windows.Controls.Button;

namespace AI.DiffAssistant.GUI;

/// <summary>
/// 主窗口
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private WpfTextBox? _visiblePasswordTextBox; // 用于保存显示密码时创建的 TextBox
    private SystemTrayManager? _systemTray;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        PasswordBox.Password = _viewModel.ApiKey;

        // 初始化系统托盘
        InitializeSystemTray();
    }

    /// <summary>
    /// 初始化系统托盘
    /// </summary>
    private void InitializeSystemTray()
    {
        try
        {
            _systemTray = new SystemTrayManager(this);
            _systemTray.Initialize();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"系统托盘初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 密码输入变更事件 - 同步到 ViewModel
    /// </summary>
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.ApiKey = passwordBox.Password;
        }
    }

    private bool _isPasswordVisible; // 跟踪密码是否处于显示状态

    /// <summary>
    /// 密码显示切换按钮点击事件
    /// </summary>
    private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (!_isPasswordVisible)
        {
            // 将 PasswordBox 内容复制到 TextBox 并隐藏 PasswordBox
            _visiblePasswordTextBox = new WpfTextBox
            {
                Text = PasswordBox.Password,
                Height = PasswordBox.Height,
                Width = PasswordBox.Width,
                Padding = PasswordBox.Padding,
                VerticalAlignment = PasswordBox.VerticalAlignment
            };

            var parent = PasswordBox.Parent as Grid;
            var index = Grid.GetColumn(PasswordBox);
            parent?.Children.Remove(PasswordBox);
            parent?.Children.Add(_visiblePasswordTextBox);
            Grid.SetColumn(_visiblePasswordTextBox, index);

            ((WpfButton)sender).Content = "隐藏";
            _isPasswordVisible = true;
        }
        else
        {
            // 将 TextBox 内容复制到 PasswordBox 并隐藏 TextBox
            if (_visiblePasswordTextBox != null)
            {
                PasswordBox.Password = _visiblePasswordTextBox.Text;
                var parent = _visiblePasswordTextBox.Parent as Grid;
                parent?.Children.Remove(_visiblePasswordTextBox);
                parent?.Children.Add(PasswordBox);
                _visiblePasswordTextBox = null;
                ((WpfButton)sender).Content = "显示";
                _isPasswordVisible = false;
            }
        }
    }

    /// <summary>
    /// 窗口关闭事件 - 最小化到托盘而不是退出
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            // 取消关闭操作，改为最小化到托盘
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    /// <summary>
    /// 窗口已关闭 - 清理资源
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _systemTray?.Dispose();
        base.OnClosed(e);
    }

    /// <summary>
    /// 请求退出应用（绕过最小化到托盘）
    /// </summary>
    public void RequestExit()
    {
        _allowClose = true;
        Close();
    }
}
