using System.Windows;
using System.Windows.Controls;
using AI.DiffAssistant.GUI.ViewModels;

namespace AI.DiffAssistant.GUI;

/// <summary>
/// 主窗口
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private TextBox? _visiblePasswordTextBox; // 用于保存显示密码时创建的 TextBox

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        PasswordBox.Password = _viewModel.ApiKey;
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
            _visiblePasswordTextBox = new TextBox
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

            ((Button)sender).Content = "隐藏";
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
                ((Button)sender).Content = "显示";
                _isPasswordVisible = false;
            }
        }
    }
}
