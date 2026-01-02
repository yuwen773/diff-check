using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using WpfBrushes = System.Windows.Media.Brushes;

namespace AI.DiffAssistant.GUI.Converters;

/// <summary>
/// 布尔值到颜色（前景色）的转换器
/// </summary>
public class BoolToColorConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRegistered)
        {
            return isRegistered ? WpfBrushes.Green : WpfBrushes.Red;
        }
        return WpfBrushes.Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
