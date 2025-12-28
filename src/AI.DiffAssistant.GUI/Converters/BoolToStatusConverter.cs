using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace AI.DiffAssistant.GUI.Converters;

/// <summary>
/// 布尔值到注册状态文本的转换器
/// </summary>
public class BoolToStatusConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRegistered)
        {
            return isRegistered ? "已集成" : "未集成";
        }
        return "未集成";
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
