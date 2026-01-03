using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace AI.DiffAssistant.GUI.Controls;

/// <summary>
/// Loading 遮罩组件（阻塞式）
/// </summary>
public partial class LoadingOverlay : global::System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingOverlay),
            new PropertyMetadata(false, OnIsLoadingChanged));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay),
            new PropertyMetadata("加载中..."));

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(LoadingSize), typeof(LoadingOverlay),
            new PropertyMetadata(LoadingSize.Normal));

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public LoadingSize Size
    {
        get => (LoadingSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public LoadingOverlay()
    {
        InitializeComponent();
    }

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LoadingOverlay)d;
        control.UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (IsLoading)
        {
            OverlayGrid.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(150)
            };
            OverlayGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
        else
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(120)
            };
            fadeOut.Completed += (s, e) => OverlayGrid.Visibility = Visibility.Collapsed;
            OverlayGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}
