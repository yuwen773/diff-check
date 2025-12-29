using System.Configuration;
using System.Data;
using System.Windows;

namespace AI.DiffAssistant.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static bool _isDarkTheme = true;

    public static bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme != value)
            {
                _isDarkTheme = value;
                ApplyTheme();
            }
        }
    }

    public static void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    private static void ApplyTheme()
    {
        var themeUri = IsDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

        // 重新加载资源字典
        if (Current?.Resources.MergedDictionaries.Count > 0)
        {
            var dict = Current.Resources.MergedDictionaries[0];
            if (dict.Source?.OriginalString != themeUri)
            {
                dict.Source = new Uri(themeUri, UriKind.Relative);
            }
        }
    }
}
