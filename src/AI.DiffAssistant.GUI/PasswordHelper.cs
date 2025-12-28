using System.Windows;
using System.Windows.Controls;

namespace AI.DiffAssistant.GUI;

/// <summary>
/// PasswordBox 附加行为帮助类
/// 用于实现 PasswordBox.Password 属性的双向数据绑定
/// </summary>
public static class PasswordHelper
{
    /// <summary>
    /// 密码附加属性
    /// </summary>
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordHelper),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPasswordPropertyChanged,
                OnPasswordPropertyCoerce));

    /// <summary>
    /// 获取密码
    /// </summary>
    public static string GetPassword(DependencyObject d) => (string)d.GetValue(PasswordProperty);

    /// <summary>
    /// 设置密码
    /// </summary>
    public static void SetPassword(DependencyObject d, string value) => d.SetValue(PasswordProperty, value);

    /// <summary>
    /// 密码属性变更处理（ViewModel → PasswordBox）
    /// </summary>
    private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordBox passwordBox)
        {
            var newPassword = e.NewValue as string ?? string.Empty;

            // 只有当值不同时才更新，避免死循环
            if (!passwordBox.Password.Equals(newPassword))
            {
                passwordBox.Password = newPassword;
            }
        }
    }

    /// <summary>
    /// 强制值转换处理（PasswordBox → ViewModel）
    /// </summary>
    private static object OnPasswordPropertyCoerce(DependencyObject d, object baseValue)
    {
        // 这里不需要做额外的强制转换
        // 反向同步通过 PasswordBox.PasswordChanged 事件处理
        return baseValue;
    }

    /// <summary>
    /// 内部使用的附加属性，用于标记是否已绑定事件
    /// </summary>
    private static readonly DependencyProperty IsBoundProperty =
        DependencyProperty.RegisterAttached(
            "IsBound",
            typeof(bool),
            typeof(PasswordHelper),
            new PropertyMetadata(false));

    private static bool GetIsBound(DependencyObject d) => (bool)d.GetValue(IsBoundProperty);
    private static void SetIsBound(DependencyObject d, bool value) => d.SetValue(IsBoundProperty, value);

    /// <summary>
    /// 附加事件处理器，当 PasswordBox 的 Password 改变时同步到附加属性
    /// </summary>
    public static readonly RoutedEventHandler PasswordBoxPasswordChangedHandler = (sender, e) =>
    {
        if (sender is PasswordBox passwordBox)
        {
            // 标记已绑定，避免重复绑定
            if (!GetIsBound(passwordBox))
            {
                SetIsBound(passwordBox, true);
                passwordBox.PasswordChanged += PasswordBoxPasswordChangedHandler;
            }

            // 将 PasswordBox.Password 的值同步到附加属性
            // 这会触发 ViewModel 的 ApiKey 属性更新
            SetPassword(passwordBox, passwordBox.Password);
        }
    };
}
