using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AI.DiffAssistant.GUI.Controls;

/// <summary>
/// Toast 通知服务
/// </summary>
public class ToastService
{
    private readonly StackPanel _toastContainer;
    private readonly Dictionary<ToastType, double> _autoDismissTimes = new()
    {
        { ToastType.Success, 3000 },
        { ToastType.Warning, 4000 },
        { ToastType.Error, 5000 }
    };

    public ToastService(StackPanel toastContainer)
    {
        _toastContainer = toastContainer;
    }

    /// <summary>
    /// 显示成功提示
    /// </summary>
    public void ShowSuccess(string message)
    {
        Show(message, ToastType.Success);
    }

    /// <summary>
    /// 显示警告提示
    /// </summary>
    public void ShowWarning(string message)
    {
        Show(message, ToastType.Warning);
    }

    /// <summary>
    /// 显示错误提示
    /// </summary>
    public void ShowError(string message)
    {
        Show(message, ToastType.Error);
    }

    /// <summary>
    /// 显示 Toast 通知
    /// </summary>
    public void Show(string message, ToastType type = ToastType.Success, int? durationMs = null)
    {
        global::System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var toast = new ToastView
            {
                Message = message,
                ToastType = type,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // 滑入动画
            var slideIn = new ThicknessAnimation
            {
                From = new Thickness(400, 0, -400, 0),
                To = new Thickness(0, 0, 0, 8),
                Duration = TimeSpan.FromMilliseconds(250),
                DecelerationRatio = 0.7
            };
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            toast.BeginAnimation(FrameworkElement.MarginProperty, slideIn);
            toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            _toastContainer.Children.Add(toast);

            // 自动消失
            var dismissTime = durationMs ?? _autoDismissTimes[type];
            var dismissTimer = new System.Threading.Timer(_ =>
            {
                global::System.Windows.Application.Current.Dispatcher.Invoke(() => DismissToast(toast));
            }, null, (int)dismissTime, Timeout.Infinite);

            toast.Loaded += (s, e) =>
            {
                // 确保动画结束后位置正确
            };
        });
    }

    private void DismissToast(ToastView toast)
    {
        if (_toastContainer.Children.Contains(toast))
        {
            var slideOut = new ThicknessAnimation
            {
                To = new Thickness(400, 0, -400, 0),
                Duration = TimeSpan.FromMilliseconds(200),
                AccelerationRatio = 0.3
            };
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150)
            };

            toast.BeginAnimation(FrameworkElement.MarginProperty, slideOut);
            toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            fadeOut.Completed += (s, e) =>
            {
                _toastContainer.Children.Remove(toast);
            };
        }
    }

    /// <summary>
    /// 清除所有 Toast
    /// </summary>
    public void ClearAll()
    {
        global::System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _toastContainer.Children.Clear();
        });
    }
}
