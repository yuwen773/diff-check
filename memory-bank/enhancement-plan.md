# 体验增强计划

> 记录用户体验优化需求和改进方案

---

## 问题 #E01: 右键菜单缺少应用图标

### 问题描述

在 Windows 资源管理器中右键点击文件，选择「AI 差异分析」时，菜单项没有显示应用图标，影响品牌识别和用户体验。

### 问题根因

`RegistryManager.RegisterContextMenu()` 方法已尝试设置图标：

```csharp
// 设置图标（可选，使用程序图标）
mainKey.SetValue("Icon", $"\"{exePath}\"");
```

但存在以下问题：
1. 没有提供独立的图标文件（`.ico` 格式）
2. 直接使用 exe 路径作为图标源可能不生效（Windows 需要 exe 内嵌图标或独立 ico 文件）

### 解决方案

#### 方案一：添加独立图标文件（推荐）

1. **图标要求**：
   - 格式：`.ico`（Windows 图标格式，支持多分辨率）
   - 推荐尺寸：32x32 像素（主要），同时包含 16x16、48x48 多种尺寸
   - 放置位置：`src/AI.DiffAssistant.GUI/Assets/app.ico`

2. **修改 `RegistryManager.cs`**：

```csharp
public bool RegisterContextMenu(string exePath)
{
    // ...

    // 获取图标路径（相对于 exe 所在目录）
    var iconPath = Path.Combine(Path.GetDirectoryName(exePath) ?? "", "app.ico");

    // 如果图标文件存在，设置图标
    if (File.Exists(iconPath))
    {
        mainKey.SetValue("Icon", $"\"{iconPath}\"");
    }
    else
    {
        // 回退：尝试使用 exe 内嵌图标
        mainKey.SetValue("Icon", $"\"{exePath}\"");
    }

    // ...
}
```

#### 方案二：使用 Base64 编码的图标数据

如果不想额外文件，可以将图标数据嵌入到程序集中，通过 API 返回图标句柄。

### 实施步骤

1. [ ] 设计并创建应用图标（32x32 ico，包含 16x32 48 多尺寸）
2. [ ] 将图标文件复制到发布目录（修改发布脚本）
3. [ ] 修改 `RegistryManager.cs` 支持图标路径
4. [ ] 测试右键菜单图标显示

### 图标资源

| 项目 | 要求 |
|------|------|
| 文件名 | `app.ico` |
| 格式 | ICO（支持多分辨率） |
| 推荐尺寸 | 16x16, 32x32, 48x48 |
| 放置位置 | `src/AI.DiffAssistant.GUI/Assets/app.ico` |

---

## 问题 #E02: 多实例启动时 DOS 窗口闪烁和超时问题

### 问题描述

用户选中两个文件，右键选择「AI 差异分析」后：
1. 出现两个 DOS 窗口（两个 CLI 实例）
2. 第一个窗口显示"等待第二个文件超时"错误
3. 第二个窗口正常运行并完成分析

### 问题根因

**Windows 右键菜单多实例启动机制**：
- 当选中两个文件右键点击时，Windows 几乎同时启动两个程序实例
- 每个实例收到一个文件路径（`%1` 参数）

**当前实现的时序问题**：

```
时间线：
T0: 实例 A 启动，收到 fileA
T0: 实例 B 启动，收到 fileB
T1: 实例 A 写入临时文件（仅包含 fileA）
T2: 实例 A 开始等待，检查临时文件 -> 只看到 fileA
T2: 实例 B 读取临时文件 -> 看到 fileA
T3: 实例 B 追加 fileB 到临时文件
T3: 实例 B 检查临时文件 -> 看到 fileA + fileB，触发执行
T4: 实例 A 检查临时文件 -> 只看到 fileA（实例 B 还没追加或已删除）
T5: 实例 A 超时（3秒），显示错误并退出
T6: 实例 B 正常完成分析
```

**核心问题**：
1. 实例 A 和实例 B 并发执行，都认为自己在等待对方
2. 实例 B 完成后删除临时文件，但实例 A 可能已经错过
3. 第一个实例总是超时，因为第二个实例已经准备好所有参数

### 解决方案

#### 方案一：使用文件锁和协调协议（推荐）

使用 `Mutex` + 文件锁实现更好的协调：

```csharp
private static async Task<string[]> WaitForSecondFileAsync(string firstFilePath)
{
    const string globalArgsFileName = "AI.DiffAssistant.args";
    const string lockFileName = "AI.DiffAssistant.args.lock";
    var tempDir = Path.GetTempPath();
    var globalArgsPath = Path.Combine(tempDir, globalArgsFileName);
    var lockPath = Path.Combine(tempDir, lockFileName);

    // 使用互斥锁确保线程安全
    using var mutex = new Mutex(false, @"Global\AI.DiffAssistant.ArgsCoord");

    try
    {
        bool acquired = mutex.WaitOne(500, false);
        if (!acquired)
        {
            // 无法获取互斥锁，直接使用当前文件
            return new[] { firstFilePath };
        }

        try
        {
            if (File.Exists(globalArgsPath))
            {
                // 已有其他实例在协调，读取并追加
                var lines = File.ReadAllLines(globalArgsPath)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                if (lines.Count >= 2)
                {
                    // 已经有两个文件，直接返回
                    var fileB = lines.FirstOrDefault(f => f != firstFilePath);
                    if (fileB != null)
                    {
                        return new[] { firstFilePath, fileB };
                    }
                }

                // 追加当前文件
                lines.Add(firstFilePath);
                File.WriteAllLines(globalArgsPath, lines);

                // 立即检查是否有两个文件
                await Task.Delay(50);
                var updatedLines = File.ReadAllLines(globalArgsPath)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                if (updatedLines.Count >= 2)
                {
                    var secondFile = updatedLines.FirstOrDefault(f => f != firstFilePath);
                    if (secondFile != null)
                    {
                        // 清理临时文件（让其他实例知道已完成）
                        try { File.Delete(globalArgsPath); } catch { }
                        return new[] { firstFilePath, secondFile };
                    }
                }
            }
            else
            {
                // 没有协调文件，创建并等待
                File.WriteAllText(globalArgsPath, firstFilePath);
            }

            // 等待其他实例（最多 2 秒）
            var maxWait = 2000;
            var checkInterval = 50;
            var waited = 0;

            while (waited < maxWait)
            {
                await Task.Delay(checkInterval);
                waited += checkInterval;

                try
                {
                    if (!File.Exists(globalArgsPath))
                    {
                        // 文件被删除，说明其他实例已完成协调
                        return new[] { firstFilePath }; // 交给其他实例处理
                    }

                    var lines = File.ReadAllLines(globalArgsPath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToList();

                    if (lines.Count >= 2)
                    {
                        var secondFile = lines.FirstOrDefault(f => f != firstFilePath);
                        if (secondFile != null)
                        {
                            // 清理并返回
                            try { File.Delete(globalArgsPath); } catch { }
                            return new[] { firstFilePath, secondFile };
                        }
                    }
                }
                catch { }
            }

            // 超时，清理
            try { File.Delete(globalArgsPath); } catch { }
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
    catch (AbandonedMutexException)
    {
        // 互斥锁被放弃，我们可以继续
    }

    return new[] { firstFilePath };
}
```

#### 方案二：使用命名管道进行进程间通信

使用 `NamedPipeServerStream` 和 `NamedPipeClientStream` 进行同步通信：

```csharp
// 第一个实例启动管道服务器
var server = new NamedPipeServerStream("AI.DiffAssistant.Pipe", PipeDirection.InOut, 1);

// 第二个实例连接并发送文件路径
var client = new NamedPipeClientStream(".", "AI.DiffAssistant.Pipe", PipeDirection.Out);
client.Connect(1000);
```

#### 方案三：简化方案 - 竞争执行

让两个实例竞争执行，避免协调：

```csharp
// 两个实例都尝试获取执行权
using var mutex = new SingleInstanceManager();
if (mutex.IsFirstInstance)
{
    // 第一个实例：等待并收集参数
    var args = await WaitForArgumentsAsync();
    if (args.Length >= 2)
    {
        await RunAnalysisAsync(args[0], args[1]);
    }
}
else
{
    // 后续实例：写入自己的参数并退出
    WriteArgumentsToSharedStorage(currentFilePath);
    // 通知第一个实例（通过删除标记文件）
}
```

### 推荐方案

**推荐方案一（文件锁协调）**，因为：
1. 不需要额外的系统 API 学习成本
2. 基于文件操作，可靠性高
3. 可以优雅处理各种边界情况

**进一步优化 - 隐藏 DOS 窗口**：

使用 Windows API 隐藏控制台窗口：

```csharp
[DllImport("kernel32.dll", SetLastError = true)]
private static extern bool FreeConsole();

[DllImport("kernel32.dll", SetLastError = true)]
private static extern bool AttachConsole(int dwProcessId);

// 在 Program.Main 开头调用
if (args.Length > 0 && !IsDebuggerAttached())
{
    FreeConsole(); // 分离控制台，运行在后台
}
```

或者将 CLI 项目改为 Windows Application：

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <SubApplication>false</SubApplication>
  <!-- 其他属性 -->
</PropertyGroup>
```

### 实施步骤

1. [ ] 优化 `ArgsParser.cs` 和 `Program.cs` 的参数收集逻辑
2. [ ] 添加 `IsDebuggerAttached()` 检测和 `FreeConsole()` 调用
3. [ ] 或者将 CLI 项目类型改为 Windows Application
4. [ ] 测试多实例启动流程
5. [ ] 验证只有一个 DOS 窗口闪烁

---

## 问题 #E03: 日志配置功能

### 问题描述

当前日志功能存在以下问题：
1. 日志文件路径固定为 `%TEMP%\AI.DiffAssistant.log`，用户无法自定义
2. 日志级别只有单一级别，无法按需调整
3. GUI 配置中心没有日志配置界面
4. 用户无法查看和管理日志

### 需求确认

| 配置项 | 要求 |
|--------|------|
| 配置界面 | GUI 配置中心添加「日志设置」Tab |
| 日志级别 | Error + Warning（仅错误和警告） |
| 日志路径 | 可自定义路径 |

### 解决方案

#### 1. 扩展配置模型

修改 `Config.cs` 添加日志配置：

```csharp
public class LoggingConfig
{
    /// <summary>是否启用日志</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>日志文件路径</summary>
    public string LogPath { get; set; } = "%TEMP%\\AI.DiffAssistant.log";

    /// <summary>日志级别: Error, Warning</summary>
    public string Level { get; set; } = "Error,Warning";
}
```

#### 2. 添加日志服务

创建 `LoggingService.cs`：

```csharp
public class LoggingService
{
    private readonly LoggingConfig _config;
    private string _logPath;
    private readonly HashSet<string> _enabledLevels;

    public void Log(string level, string message)
    {
        if (!_config.Enabled) return;
        if (!_enabledLevels.Contains(level)) return;

        // 写入日志文件
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        File.AppendAllText(_logPath, $"[{timestamp}] [{level}] {message}\n");
    }
}
```

#### 3. GUI 配置中心添加日志设置 Tab

- 复选框：启用日志
- 文本框：日志文件路径（支持浏览按钮）
- 复选框组：选择日志级别（Error, Warning）
- 按钮：打开日志文件、清除日志

#### 4. 修改 CLI 使用日志服务

替换现有的 `Log()` 方法，使用 `LoggingService`：

```csharp
private static LoggingService _logger;

static void Main()
{
    _logger = new LoggingService(config.Logging);
    _logger.Log("Info", "程序启动");
}
```

### 实施步骤

1. [ ] 扩展 `Config.cs` 添加 `LoggingConfig` 模型
2. [ ] 创建 `LoggingService.cs` 日志服务
3. [ ] 修改 `Program.cs` 使用日志服务
4. [ ] GUI 添加「日志设置」Tab
5. [ ] 支持日志路径浏览和验证
6. [ ] 测试日志功能

### 配置示例

```json
{
  "api": { ... },
  "logging": {
    "enabled": true,
    "logPath": "C:\\Users\\xxx\\AppData\\Local\\Temp\\AI.DiffAssistant.log",
    "level": "Error,Warning"
  }
}
```

---

## 问题 #E04: GUI 界面不够现代化

### 问题描述

当前 GUI 配置中心界面较为朴素，存在以下问题：
1. 界面风格单一，缺少主题切换功能
2. 控件样式陈旧，缺乏现代感
3. 缺少动画效果和视觉反馈

### 需求确认

| 配置项 | 要求 |
|--------|------|
| 主题切换 | 深色/浅色主题一键切换 |
| 控件样式 | 现代化设计，圆角、阴影、悬停效果 |
| 视觉反馈 | 按钮点击动画、状态指示 |

### 解决方案

#### 1. 添加主题支持

**新建文件：** `src/AI.DiffAssistant.GUI/Themes/LightTheme.xaml` 和 `DarkTheme.xaml`

```xml
<!-- DarkTheme.xaml 示例 -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <!-- 背景色 -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="#1E1E1E"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="#2D2D2D"/>
    <SolidColorBrush x:Key="CardBrush" Color="#3D3D3D"/>

    <!-- 文字颜色 -->
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="#B0B0B0"/>

    <!-- 强调色 -->
    <SolidColorBrush x:Key="AccentBrush" Color="#0078D4"/>

    <!-- 按钮样式 -->
    <Style TargetType="Button">
        <Setter Property="Background" Value="{StaticResource AccentBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="CornerRadius" Value="6"/>
    </Style>
</ResourceDictionary>
```

#### 2. 修改 App.xaml 集成主题

**修改文件：** `src/AI.DiffAssistant.GUI/App.xaml`

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Themes/DarkTheme.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

#### 3. 优化主窗口 UI

**修改文件：** `src/AI.DiffAssistant.GUI/MainWindow.xaml`

改进项：
- 添加主题切换按钮（标题栏）
- 优化 TabControl 样式（圆角、选中效果）
- 优化 TextBox/PasswordBox（边框、焦点效果）
- 添加窗口阴影效果

#### 4. ViewModel 主题切换逻辑

**修改文件：** `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs`

```csharp
public bool IsDarkTheme { get; set; } = true;
public ICommand ToggleThemeCommand { get; }

private void ToggleTheme()
{
    IsDarkTheme = !IsDarkTheme;
    var theme = IsDarkTheme ? "DarkTheme" : "LightTheme";
    // 应用主题资源
}
```

### 涉及文件清单

| 文件 | 操作 | 说明 |
|------|------|------|
| `src/AI.DiffAssistant.GUI/Themes/LightTheme.xaml` | 新建 | 浅色主题资源 |
| `src/AI.DiffAssistant.GUI/Themes/DarkTheme.xaml` | 新建 | 深色主题资源 |
| `src/AI.DiffAssistant.GUI/App.xaml` | 修改 | 添加主题引用 |
| `src/AI.DiffAssistant.GUI/MainWindow.xaml` | 修改 | UI样式优化 |
| `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs` | 修改 | 主题切换逻辑 |

---

## 问题 #E05: 富文本文件不支持

### 问题描述

当前仅支持纯文本文件（.txt, .md, .cs 等），不支持 PDF 和 Word 文档。

### 需求确认

| 文件类型 | 优先级 | 说明 |
|----------|--------|------|
| PDF (.pdf) | P0 | 使用 PdfPig 解析 |
| Word (.docx) | P0 | 使用 DocumentFormat.OpenXml 解析 |

### 解决方案

#### 1. 添加 NuGet 依赖

**修改文件：** `src/AI.DiffAssistant.Core/AI.DiffAssistant.Core.csproj`

```xml
<PackageReference Include="PdfPig" Version="0.1.8" />
<PackageReference Include="DocumentFormat.OpenXml" Version="3.0.2" />
```

#### 2. 创建富文本处理器

**新建文件：** `src/AI.DiffAssistant.Core/File/RichTextProcessor.cs`

```csharp
public static class RichTextProcessor
{
    public static async Task<string> ExtractTextAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => await ExtractPdfTextAsync(filePath),
            ".docx" or ".doc" => await ExtractDocxTextAsync(filePath),
            _ => throw new NotSupportedException($"不支持: {ext}")
        };
    }

    private static async Task<string> ExtractPdfTextAsync(string path)
    {
        using var doc = PdfDocument.Open(path);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static async Task<string> ExtractDocxTextAsync(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        return doc.MainDocumentPart.Document.Body.InnerText;
    }
}
```

#### 3. 修改 FileProcessor 集成富文本

**修改文件：** `src/AI.DiffAssistant.Core/File/FileProcessor.cs`

```csharp
public async Task<FileProcessResult> ProcessFileAsync(string path)
{
    var ext = Path.GetExtension(path).ToLowerInvariant();

    string content;
    Encoding encoding = Encoding.UTF8;

    if (IsRichTextFile(ext))
    {
        content = await RichTextProcessor.ExtractTextAsync(path);
    }
    else
    {
        (content, encoding) = await ReadTextFileAsync(path);
    }

    var (truncated, length) = TruncateIfNeeded(ref content);
    return new FileProcessResult { Content = content, IsTruncated = truncated };
}

private bool IsRichTextFile(string ext) =>
    ext is ".pdf" or ".docx" or ".doc";
```

### 涉及文件清单

| 文件 | 操作 | 说明 |
|------|------|------|
| `src/AI.DiffAssistant.Core/AI.DiffAssistant.Core.csproj` | 修改 | 添加 NuGet 依赖 |
| `src/AI.DiffAssistant.Core/File/RichTextProcessor.cs` | 新建 | PDF/Word 文本提取 |
| `src/AI.DiffAssistant.Core/File/FileProcessor.cs` | 修改 | 集成富文本处理 |
| `src/AI.DiffAssistant.Shared/Constants/ConfigConstants.cs` | 修改 | 添加文件扩展名 |

---

## 问题 #E06: 分析完成后的提示使用 MessageBox 而非 Toast 通知

### 问题描述

当前使用 `MessageBox` 弹出提示框，用户体验不佳。期望使用 Win10/11 原生 Toast 通知（右下角滑入）。

### 需求确认

| 功能 | 要求 |
|------|------|
| 通知样式 | Win10/11 原生 Toast，右下角滑入 |
| 点击行为 | 点击打开 difference.md |
| 图标 | 显示应用图标 |

### 解决方案

#### 1. 添加 Toast 通知 NuGet 包

**修改文件：** `src/AI.DiffAssistant.Core/AI.DiffAssistant.Core.csproj`

```xml
<PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.1.3" />
```

#### 2. 重写 NotificationManager

**修改文件：** `src/AI.DiffAssistant.Core/Notification/NotificationManager.cs`

```csharp
using Microsoft.Toolkit.Uwp.Notifications;

public static class NotificationManager
{
    public static void ShowSuccess(string message)
    {
        new ToastContentBuilder()
            .AddArgument("action", "openResult")
            .AddText("分析完成")
            .AddText(message)
            .Show();
    }

    public static void ShowError(string error)
    {
        new ToastContentBuilder()
            .AddText("分析失败")
            .AddText(error)
            .Show();
    }
}

// 在 Program.cs 中注册点击回调
ToastNotificationManagerCompat.OnActivated += args =>
{
    if (args.Argument == "action=openResult")
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "difference.md");
        if (File.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
};
```

#### 3. 创建应用清单（消除 DOS 窗口）

**修改文件：** `src/AI.DiffAssistant.Cli/AI.DiffAssistant.Cli.csproj`

```xml
<PropertyGroup>
    <OutputType>WinExe</OutputType>
</PropertyGroup>
```

**新建文件：** `src/AI.DiffAssistant.Cli/app.manifest`

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v3">
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">
    <security>
      <requestedPrivileges>
        <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
```

### 涉及文件清单

| 文件 | 操作 | 说明 |
|------|------|------|
| `src/AI.DiffAssistant.Core/AI.DiffAssistant.Core.csproj` | 修改 | 添加 Toast NuGet 包 |
| `src/AI.DiffAssistant.Core/Notification/NotificationManager.cs` | 重写 | Toast 通知实现 |
| `src/AI.DiffAssistant.Cli/AI.DiffAssistant.Cli.csproj` | 修改 | 改为 WinExe |
| `src/AI.DiffAssistant.Cli/app.manifest` | 新建 | 应用清单 |
| `src/AI.DiffAssistant.GUI/Assets/app.ico` | 新建 | 应用图标（用于托盘） |

---

## 优先级排序（更新）

| 优先级 | 问题 | 影响 | 状态 |
|--------|------|------|------|
| P0 | #E04 GUI 界面现代化 | 用户体验 | ✅ 已完成 |
| P1 | #E05 富文本文件支持 | 功能扩展 | 待实施 |
| P2 | #E06 Toast 通知 | 用户体验 | 待实施 |
| P3 | #E03 日志配置功能 | 已完成 | - |
| P4 | #E02 DOS 窗口闪烁 | 已完成 | - |
| P5 | #E01 右键菜单图标 | 已完成 | - |

---

> 记录时间：2025-12-29
