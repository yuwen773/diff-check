using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AI.DiffAssistant.GUI.Controls;

/// <summary>
/// Toast 通知类型
/// </summary>
public enum ToastType
{
    Success,
    Warning,
    Error
}

/// <summary>
/// Toast 通知模型
/// </summary>
public class ToastNotification
{
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Success;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Action? OnClose { get; set; }
}

/// <summary>
/// ToastView 用户控件
/// </summary>
public partial class ToastView : global::System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(ToastView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ToastTypeProperty =
        DependencyProperty.Register(nameof(ToastType), typeof(ToastType), typeof(ToastView), new PropertyMetadata(ToastType.Success));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(ToastView), new PropertyMetadata("✓"));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ToastType ToastType
    {
        get => (ToastType)GetValue(ToastTypeProperty);
        set
        {
            SetValue(ToastTypeProperty, value);
            UpdateAppearance(value);
        }
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ToastView()
    {
        InitializeComponent();
    }

    private void UpdateAppearance(ToastType type)
    {
        var resources = global::System.Windows.Application.Current.Resources;
        string bgKey;

        switch (type)
        {
            case ToastType.Success:
                bgKey = "ToastSuccessBg";
                Icon = "✓";
                break;
            case ToastType.Warning:
                bgKey = "ToastWarningBg";
                Icon = "!";
                break;
            case ToastType.Error:
                bgKey = "ToastErrorBg";
                Icon = "✕";
                break;
            default:
                bgKey = "ToastSuccessBg";
                Icon = "✓";
                break;
        }

        if (resources[bgKey] is SolidColorBrush brush)
        {
            ToastBorder.Background = brush;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 手动查找父级 StackPanel
        DependencyObject? parent = VisualTreeHelper.GetParent(this);
        while (parent != null)
        {
            if (parent is StackPanel stackPanel)
            {
                var storyboard = FindResource("ToastFadeOut") as Storyboard;
                storyboard?.Begin(this);
                storyboard!.Completed += (s, args) => stackPanel.Children.Remove(this);
                return;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
    }
}
