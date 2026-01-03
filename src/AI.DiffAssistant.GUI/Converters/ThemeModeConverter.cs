using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace AI.DiffAssistant.GUI.Converters;

/// <summary>
/// 主题模式转换器 - 用于 RadioButton 绑定
/// </summary>
public class ThemeModeConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string currentMode && parameter is string paramMode)
        {
            return currentMode.Equals(paramMode, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string paramMode)
        {
            return paramMode;
        }
        return "System";
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
