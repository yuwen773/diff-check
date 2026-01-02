using System.IO;
using System.Reflection;
using System.Windows;

namespace AI.DiffAssistant.GUI.Views;

/// <summary>
/// 关于窗口
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        LoadVersionInfo();
    }

    /// <summary>
    /// 加载版本信息
    /// </summary>
    private void LoadVersionInfo()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var buildDate = new FileInfo(assembly.Location).LastWriteTime;

            VersionText.Text = $"版本 {version?.ToString() ?? "1.0.0.0"}\n" +
                              $"构建日期: {buildDate:yyyy-MM-dd}\n" +
                              $"运行时: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}";
        }
        catch (Exception ex)
        {
            VersionText.Text = $"版本信息加载失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 确定按钮点击事件
    /// </summary>
    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
