using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AI.DiffAssistant.GUI.Controls;
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
    private WpfTextBox? _visiblePasswordTextBox;
    private SystemTrayManager? _systemTray;
    private bool _allowClose;
    private ToastService? _toastService;

    public MainWindow()
    {
        InitializeComponent();

        // 应用窗口打开动画
        Loaded += OnWindowLoaded;

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // 订阅密码可见性切换事件
        _viewModel.TogglePasswordVisibilityRequested += OnTogglePasswordVisibilityRequested;

        // 在 ViewModel 加载配置前设置密码框的初始值
        InitializePasswordBox();

        // 初始化 Toast 服务
        _toastService = new ToastService(ToastContainer);
        _viewModel.SetToastService(_toastService);

        // 初始化系统托盘
        InitializeSystemTray();
    }

    /// <summary>
    /// 初始化密码框，从配置加载 API Key
    /// </summary>
    private void InitializePasswordBox()
    {
        if (!string.IsNullOrEmpty(_viewModel.ApiKey))
        {
            PasswordBox.Password = _viewModel.ApiKey;
        }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // 窗口打开动画
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200)
        };
        var scaleX = new DoubleAnimation
        {
            From = 0.95,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200)
        };
        var scaleY = new DoubleAnimation
        {
            From = 0.95,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200)
        };

        BeginAnimation(Window.OpacityProperty, fadeIn);
        WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
        WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
    }

    /// <summary>
    /// 标题栏拖动
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
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
    internal void RestoreFromTray()
    {
        BeginAnimation(OpacityProperty, null);
        WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

        Opacity = 1;
        WindowScale.ScaleX = 1;
        WindowScale.ScaleY = 1;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.ApiKey = passwordBox.Password;
        }
    }

    private bool _isPasswordVisible;

    /// <summary>
    /// 密码显示切换按钮点击事件（由 ViewModel 命令触发）
    /// </summary>
    private void OnTogglePasswordVisibilityRequested()
    {
        TogglePasswordVisibilityCore(null);
    }

    /// <summary>
    /// 密码显示切换核心逻辑
    /// </summary>
    private void TogglePasswordVisibilityCore(WpfButton? button)
    {
        // 查找眼睛按钮
        var toggleButton = FindName("TogglePasswordButton") as WpfButton;

        if (!_isPasswordVisible)
        {
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

            toggleButton!.Content = "🔒";
            _isPasswordVisible = true;
        }
        else
        {
            if (_visiblePasswordTextBox != null)
            {
                PasswordBox.Password = _visiblePasswordTextBox.Text;
                var parent = _visiblePasswordTextBox.Parent as Grid;
                parent?.Children.Remove(_visiblePasswordTextBox);
                parent?.Children.Add(PasswordBox);
                _visiblePasswordTextBox = null;
                toggleButton!.Content = "👁";
                _isPasswordVisible = false;
            }
        }
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            // 先取消关闭，再播放动画
            e.Cancel = true;

            // 播放关闭动画后隐藏
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150)
            };
            var scaleX = new DoubleAnimation
            {
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(150)
            };
            var scaleY = new DoubleAnimation
            {
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(150)
            };

            fadeOut.Completed += (s, args) =>
            {
                Hide();
            };

            BeginAnimation(Window.OpacityProperty, fadeOut);
            WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);

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
    /// 请求退出应用
    /// </summary>
    public void RequestExit()
    {
        _allowClose = true;
        Close();
    }

    /// <summary>
    /// Tab 选择改变事件处理
    /// </summary>
    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TabControl tabControl && tabControl.SelectedItem is System.Windows.Controls.TabItem selectedTab)
        {
            // 当选择"版本下载"Tab 时自动刷新
            if (selectedTab.Header?.ToString() == "版本下载")
            {
                // 如果还没有加载过版本列表，则触发刷新
                if (_viewModel.Releases.Count == 0 && !_viewModel.IsReleaseLoading)
                {
                    _ = _viewModel.RefreshReleasesAsync();
                }
            }
        }
    }

    /// <summary>
    /// 超链接导航事件处理 - 确保链接在默认浏览器中打开
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            // 在默认浏览器中打开链接
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开链接失败: {ex.Message}");
            // 可选：显示错误提示
            System.Windows.MessageBox.Show($"无法打开链接: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }
}
