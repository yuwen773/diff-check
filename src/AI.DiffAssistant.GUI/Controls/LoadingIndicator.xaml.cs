using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AI.DiffAssistant.GUI.Controls;

/// <summary>
/// Loading 指示器尺寸
/// </summary>
public enum LoadingSize
{
    Small,
    Normal,
    Large
}

/// <summary>
/// Loading 环形指示器
/// </summary>
public partial class LoadingIndicator : global::System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(LoadingSize), typeof(LoadingIndicator),
            new PropertyMetadata(LoadingSize.Normal, OnSizeChanged));

    public static readonly DependencyProperty RingThicknessProperty =
        DependencyProperty.Register(nameof(RingThickness), typeof(double), typeof(LoadingIndicator),
            new PropertyMetadata(3.0));

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingIndicator),
            new PropertyMetadata(true, OnIsLoadingChanged));

    public LoadingSize Size
    {
        get => (LoadingSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double RingThickness
    {
        get => (double)GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public LoadingIndicator()
    {
        InitializeComponent();
        UpdateSize();
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((LoadingIndicator)d).UpdateSize();
    }

    private void UpdateSize()
    {
        double size = Size switch
        {
            LoadingSize.Small => 16,
            LoadingSize.Normal => 24,
            LoadingSize.Large => 32,
            _ => 24
        };

        MainGrid.Width = size;
        MainGrid.Height = size;
        RingPath1.StrokeThickness = RingThickness;
        RingPath2.StrokeThickness = RingThickness;
    }

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LoadingIndicator)d;
        if (control.IsLoading)
        {
            control.StartAnimation();
        }
        else
        {
            control.StopAnimation();
        }
    }

    private void StartAnimation()
    {
        var rotateAnimation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(1),
            RepeatBehavior = RepeatBehavior.Forever
        };

        RotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
    }

    private void StopAnimation()
    {
        RotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
    }
}
