using System.IO;

namespace AI.DiffAssistant.Core.Registry;

/// <summary>
/// 注册表管理器 - 处理 Windows 右键菜单注册
/// </summary>
public class RegistryManager
{
    /// <summary>
    /// 注册表主键路径
    /// </summary>
    public const string RegRoot = @"Software\Classes\*\shell\diff-check";

    /// <summary>
    /// 旧版注册表主键路径（用于迁移/清理）
    /// </summary>
    private const string LegacyRegRoot = @"Software\Classes\*\shell\AI差异分析";

    /// <summary>
    /// 命令子键路径
    /// </summary>
    public const string CommandKey = @"\command";

    private readonly string _registryPath;

    /// <summary>
    /// 初始化注册表管理器
    /// </summary>
    public RegistryManager()
    {
        _registryPath = RegRoot;
    }

    /// <summary>
    /// 检查是否已注册
    /// </summary>
    /// <returns>是否已注册右键菜单</returns>
    public bool IsRegistered()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_registryPath);
            return key != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 图标文件名
    /// </summary>
    private const string IconFileName = "diff-check.ico";

    /// <summary>
    /// 右键菜单显示名称
    /// </summary>
    private const string DisplayName = "diff-check";

    /// <summary>
    /// 注册右键菜单
    /// </summary>
    /// <param name="exePath">程序完整路径</param>
    /// <returns>是否注册成功</returns>
    public bool RegisterContextMenu(string exePath)
    {
        try
        {
            // 验证可执行文件路径
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("可执行文件路径不能为空", nameof(exePath));

            if (!System.IO.File.Exists(exePath))
                throw new FileNotFoundException("可执行文件不存在", exePath);

            CleanupLegacyRegistration();

            // 创建主键
            using var mainKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_registryPath);

            // 设置显示名称
            mainKey.SetValue("", DisplayName);

            // 设置图标
            var iconPath = SetIcon(exePath, mainKey);

            // 创建 command 子键
            using var commandKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_registryPath + CommandKey);

            // 设置命令值："[exePath]" "%1"
            // %1 表示被选中的文件路径
            commandKey.SetValue("", $"\"{exePath}\" \"%1\"");

            // 设置 NoWorkingDirectory，避免工作目录问题
            commandKey.SetValue("NoWorkingDirectory", "");

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("没有权限修改注册表，请确保以管理员身份运行");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"注册右键菜单失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 设置图标路径
    /// </summary>
    private static string SetIcon(string exePath, Microsoft.Win32.RegistryKey mainKey)
    {
        // 尝试从 exe 所在目录获取图标
        var exeDirectory = Path.GetDirectoryName(exePath);
        var iconPath = exeDirectory != null ? Path.Combine(exeDirectory, IconFileName) : null;

        if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
        {
            // 使用独立图标文件
            mainKey.SetValue("Icon", $"\"{iconPath}\"");
            return iconPath;
        }

        // 回退：尝试使用 exe 内嵌图标
        mainKey.SetValue("Icon", $"\"{exePath}\"");
        return exePath;
    }

    /// <summary>
    /// 注销右键菜单
    /// </summary>
    /// <returns>是否注销成功</returns>
    public bool UnregisterContextMenu()
    {
        try
        {
            // 删除主键（包含所有子键）
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(_registryPath, false);
            CleanupLegacyRegistration();
            return true;
        }
        catch (ArgumentException)
        {
            // 键不存在，视为成功
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("没有权限修改注册表，请确保以管理员身份运行");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"注销右键菜单失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取注册的命令路径
    /// </summary>
    /// <returns>注册的可执行文件路径，不存在返回 null</returns>
    public string? GetRegisteredPath()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_registryPath + CommandKey);
            if (key == null)
                return null;

            var value = key.GetValue("") as string;
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // 解析路径格式: "[exePath]" "%1"
            // 提取第一个引号内的路径
            var match = System.Text.RegularExpressions.Regex.Match(value, @"^""([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void CleanupLegacyRegistration()
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(LegacyRegRoot, false);
        }
        catch (ArgumentException)
        {
            // 旧键不存在，忽略
        }
        catch
        {
            // 忽略清理失败
        }
    }
}
