# 问题追踪

> 记录测试中发现的问题及解决方案

---

## 问题 #001: GUI 中 API Key 配置无法保存

### 问题描述

在 GUI 配置界面中，用户输入 API Key 后：
1. 点击"测试连接"按钮，提示"请输入 API Key"（即使已输入）
2. 点击"保存配置"后提示"配置已保存"
3. 再次打开 GUI，API Key 输入框仍然为空

### 问题根因

**`PasswordBox` 控件未与 ViewModel 进行数据绑定**

在 `MainWindow.xaml:75-79` 中，`PasswordBox` 控件的 `Password` 属性没有绑定到 `MainViewModel.ApiKey` 属性：

```xaml
<PasswordBox Grid.Column="1"
             Name="PasswordBox"
             Height="26"
             Padding="4"
             ToolTip="API 密钥"/>
```

关键问题：
1. `PasswordBox.Password` 不是依赖属性，无法直接进行双向数据绑定
2. ViewModel 的 `ApiKey` 属性值来自 `LoadConfig()` 加载的配置文件
3. 用户在 UI 中输入的内容只在 PasswordBox 内部，不会同步到 ViewModel

### 数据流问题

```
用户输入 → PasswordBox.Password（UI本地）
                    ↓
           未同步到 ViewModel.ApiKey
                    ↓
测试连接: ApiKey 为空 → 提示"请输入 API Key"
保存配置: 保存 ViewModel.ApiKey（空值）→ config.json（空值）
再次打开: 加载 config.json → ApiKey 为空
```

### 影响范围

- API Key 无法通过 GUI 保存和加载
- 测试连接功能失效
- 用户需要通过直接编辑 `config.json` 文件来配置 API Key

### 建议解决方案

#### 方案一：使用 PasswordBinding 附加行为（推荐）

创建附加行为来绑定 PasswordBox 的 Password 属性：

```csharp
// PasswordHelper.cs
public static class PasswordHelper
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordHelper),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPasswordPropertyChanged));

    public static string GetPassword(DependencyObject d) => (string)d.GetValue(PasswordProperty);
    public static void SetPassword(DependencyObject d, string value) => d.SetValue(PasswordProperty, value);

    private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var passwordBox = d as PasswordBox;
        if (passwordBox != null && !passwordBox.Password.Equals(e.NewValue))
        {
            passwordBox.Password = e.NewValue as string;
        }
    }
}
```

在 XAML 中绑定：
```xaml
<PasswordBox local:PasswordHelper.Password="{Binding ApiKey, Mode=TwoWay}"
             PasswordChanged="PasswordBox_PasswordChanged"/>
```

#### 方案二：使用事件处理同步（简单方案）

在代码隐藏中处理密码同步：

```csharp
// MainWindow.xaml.cs
private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
{
    if (sender is PasswordBox passwordBox)
    {
        _viewModel.ApiKey = passwordBox.Password;
    }
}
```

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 修复方案

采用**方案一：附加行为绑定**（WPF 标准做法）

#### 修复文件

1. **新建 `src/AI.DiffAssistant.GUI/PasswordHelper.cs`**
   - 创建 `PasswordHelper` 静态类
   - 实现 `PasswordProperty` 附加属性（带 coerce callback）
   - 实现 `OnPasswordPropertyChanged` 处理方法（ViewModel → PasswordBox）
   - 添加 `PasswordBoxPasswordChangedHandler` 实现反向同步（PasswordBox → ViewModel）

2. **修改 `src/AI.DiffAssistant.GUI/MainWindow.xaml`**
   ```xaml
   <PasswordBox Grid.Column="1"
                Name="PasswordBox"
                Height="26"
                Padding="4"
                ToolTip="API 密钥"
                PasswordChanged="PasswordBox_PasswordChanged"
                local:PasswordHelper.Password="{Binding ApiKey, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
   ```

3. **修改 `src/AI.DiffAssistant.GUI/MainWindow.xaml.cs`**
   ```csharp
   // 新增事件处理方法
   private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
   {
       if (sender is PasswordBox passwordBox)
       {
           // 使用附加行为同步密码到 ViewModel
           PasswordHelper.SetPassword(passwordBox, passwordBox.Password);
       }
   }
   ```

#### 修复后的数据流

```
配置文件(config.json) → LoadConfig() → ViewModel.ApiKey
                                              ↓
                                      附加属性绑定
                                              ↓
                                      PasswordBox.Password
                                              ↑
                                     ____________|
                                     |
                       用户输入 → 事件同步
```

### 验证步骤

1. 重新编译项目：
   ```bash
   dotnet build
   ```

2. 运行 GUI，测试：
   - 输入 API Key
   - 点击"测试连接"（应能正常验证）
   - 点击"保存配置"
   - 关闭并重新打开 GUI
   - 验证 API Key 是否保留

### 发现日期

2025-12-28

### 修复日期

2025-12-28

---

## 问题 #002: 密码显示/隐藏切换功能失效

### 问题描述

在 GUI 配置界面中，点击"显示"按钮后，可以显示明文密码。但点击"隐藏"按钮后，无法将 TextBox 恢复为 PasswordBox，密码仍然可见。

### 问题根因

**`PasswordBox` 被移除后无法通过 Parent 查找**

在 `MainWindow.xaml.cs` 第 58-70 行中：

```csharp
else
{
    // 将 TextBox 内容复制到 PasswordBox 并隐藏 TextBox
    var textBox = (PasswordBox.Parent as Grid)?.Children[Grid.GetColumn(PasswordBox)] as TextBox;
    // ...
}
```

关键问题：
1. 当点击"显示"按钮后，`PasswordBox` 被从视觉树中移除（第 52 行 `parent?.Children.Remove(PasswordBox)`）
2. 当点击"隐藏"按钮时，代码尝试通过 `PasswordBox.Parent` 查找 TextBox
3. 但此时 `PasswordBox.Parent` 已经是 `null`（因为已被移除）
4. 所以 `textBox` 为 `null`，隐藏逻辑无法执行

### 影响范围

- 用户无法在显示密码后重新隐藏密码
- 界面显示不一致

### 修复方案

使用字段 `_visiblePasswordTextBox` 保存显示密码时创建的 TextBox 引用：

```csharp
private TextBox? _visiblePasswordTextBox; // 用于保存显示密码时创建的 TextBox

private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
{
    if (PasswordBox.Visibility == Visibility.Visible)
    {
        // 显示密码：创建 TextBox
        _visiblePasswordTextBox = new TextBox
        {
            Text = PasswordBox.Password,
            // ... 其他属性设置
        };
        // 替换 PasswordBox 为 TextBox
        // ...
        ((Button)sender).Content = "隐藏";
    }
    else
    {
        // 隐藏密码：使用保存的引用
        if (_visiblePasswordTextBox != null)
        {
            PasswordBox.Password = _visiblePasswordTextBox.Text;
            // 替换回 PasswordBox
            _visiblePasswordTextBox = null;
            ((Button)sender).Content = "显示";
        }
    }
}
```

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 发现日期

2025-12-28

### 修复日期

2025-12-28

---

## 问题 #003: 右键菜单调用 CLI 而非 GUI 执行分析

### 问题描述

用户通过 Ctrl 选中两个文件，右键选择「AI 差异分析」后，弹出的是配置中心 GUI 窗口，而不是直接执行差异分析生成 `difference.md` 文件。

### 问题根因

**`RegisterContextMenu` 方法注册了错误的可执行文件路径**

在 `MainViewModel.cs:225-242` 的 `RegisterContextMenu` 方法中：

```csharp
private void RegisterContextMenu()
{
    var exePath = GetExecutablePath();  // 获取的是 GUI 可执行文件路径
    _registryManager.RegisterContextMenu(exePath);
}
```

关键问题：
1. `GetExecutablePath()` 返回的是当前 GUI 进程的路径 (`AI.DiffAssistant.GUI.exe`)
2. 右键菜单应该调用 CLI 可执行文件 (`AI.DiffAssistant.Cli.exe`) 执行静默分析
3. GUI 只用于配置中心，不应作为右键菜单的入口

### 影响范围

- 右键菜单无法执行差异分析
- 每次右键都会弹出配置中心窗口，造成用户体验困扰

### 修复方案

创建新方法 `GetCliExecutablePath()` 获取 CLI 可执行文件路径：

```csharp
/// <summary>
/// 获取 CLI 可执行文件路径
/// </summary>
private static string GetCliExecutablePath()
{
    var currentPath = GetExecutablePath();
    var directory = Path.GetDirectoryName(currentPath);
    var cliPath = Path.Combine(directory ?? "", "AI.DiffAssistant.Cli.exe");

    // 如果 CLI 不存在（开发环境），使用当前路径
    if (!System.IO.File.Exists(cliPath))
        return currentPath;

    return cliPath;
}
```

修改 `RegisterContextMenu` 方法：

```csharp
private void RegisterContextMenu()
{
    try
    {
        // 获取 CLI 可执行文件路径（右击分析需要调用 CLI 静默模式）
        var exePath = GetCliExecutablePath();

        // 注册右键菜单
        _registryManager.RegisterContextMenu(exePath);
        // ...
    }
    // ...
}
```

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 修复文件

- `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs`
  - 添加 `using System.IO;` using 语句
  - 添加 `GetCliExecutablePath()` 方法
  - 修改 `RegisterContextMenu()` 方法调用 `GetCliExecutablePath()`

### 发现日期

2025-12-28

### 修复日期

2025-12-28

---

## 问题 #004: Windows 右键菜单多文件选择参数收集问题

### 问题描述

用户通过 Ctrl 选中两个文件，右键选择「AI 差异分析」后，弹出错误提示「当前收到 1 个文件路径，请选择 2 个文件进行对比分析」。

### 问题根因

**Windows 右键菜单分多次启动程序实例传递参数**

当用户在文件资源管理器中选中多个文件并右键点击时，Windows 可能不会一次性传递所有文件路径，而是：
1. 启动第一个程序实例，传递第一个文件的路径 (`%1`)
2. 启动第二个程序实例，传递第二个文件的路径 (`%1`)

原代码 `ArgsParser` 的问题：
```csharp
// 1 个参数：错误，需要选择两个文件
if (args.Length == 1)
{
    return new ParseResult
    {
        IsValid = false,  // 直接返回错误！
        ErrorMessage = "请选择两个文件进行对比分析"
    };
}
```

关键问题：
1. `ArgsParser` 将 1 个参数视为错误
2. 没有实现多实例参数收集机制
3. 单实例互斥锁 (`SingleInstanceManager`) 没有处理参数共享

### 影响范围

- 选中两个文件执行右键分析时，总是报错
- 用户体验严重受损，无法正常使用右键菜单功能

### 修复方案

#### 1. 修改 `ArgsParser.cs`

支持 1 个参数的收集模式，使用全局临时文件共享参数：

```csharp
// 1 个参数：可能是多实例启动，需要收集参数
if (args.Length == 1)
{
    var filePath = args[0];
    // 验证文件存在
    if (!File.Exists(filePath))
        return 错误结果;

    // 尝试收集或创建全局参数
    var globalArgsPath = GetGlobalArgsFilePath();
    var collectedArgs = CollectArguments(filePath, globalArgsPath);

    if (collectedArgs.Count >= 2)
        return 成功结果（包含两个文件）;

    return 仅包含一个文件的结果;
}
```

#### 2. 修改 `Program.cs`

添加 `WaitForSecondFileAsync` 方法，等待并收集第二个文件：

```csharp
private static async Task<string[]> WaitForSecondFileAsync(string firstFilePath)
{
    const string globalArgsFileName = "AI.DiffAssistant.args";
    var tempDir = Path.GetTempPath();
    var globalArgsPath = Path.Combine(tempDir, globalArgsFileName);

    // 写入第一个文件路径
    File.WriteAllText(globalArgsPath, firstFilePath);

    // 等待最多 3 秒，让其他实例有时间写入
    var maxWait = 3000;
    while (waited < maxWait)
    {
        await Task.Delay(100);
        // 检查是否有其他文件路径
        var lines = File.ReadAllLines(globalArgsPath)
            .Where(line => line != firstFilePath)
            .ToList();

        if (lines.Count > 0)
        {
            // 找到第二个文件
            var secondFile = lines.First();
            File.Delete(globalArgsPath);
            return new[] { firstFilePath, secondFile };
        }
    }

    // 超时
    File.Delete(globalArgsPath);
    return new[] { firstFilePath };
}
```

在 `RunSilentModeAsync` 中调用：

```csharp
private static async Task<int> RunSilentModeAsync(string[] filePaths)
{
    // 等待并收集第二个文件（如果只有一个）
    string[] validFilePaths = filePaths;
    if (filePaths.Length == 1)
    {
        validFilePaths = await WaitForSecondFileAsync(filePaths[0]);
    }

    if (validFilePaths.Length < 2)
    {
        // 显示错误通知
        return (int)ExitCode.FileNotFound;
    }

    // 继续执行分析...
}
```

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 修复文件

- `src/AI.DiffAssistant.Cli/ArgsParser.cs`
  - 添加 `using System.IO;` 和 `using System.Threading;`
  - 修改 `Parse()` 方法支持 1 个参数
  - 添加 `GetGlobalArgsFilePath()` 方法
  - 添加 `CollectArguments()` 方法处理参数收集

- `src/AI.DiffAssistant.Cli/Program.cs`
  - 添加 `using System.Threading;`
  - 添加 `WaitForSecondFileAsync()` 方法
  - 修改 `RunSilentModeAsync()` 方法调用等待逻辑

### 发现日期

2025-12-28

### 修复日期

2025-12-28

---

## 问题 #005: 静默模式无反馈导致用户无法了解处理状态

### 问题描述

用户通过右键菜单执行「AI 差异分析」后：
1. 没有 DOS 窗口显示进度
2. 没有错误通知弹出
3. 成功时虽有通知但用户可能错过
4. 失败时完全无反馈，用户不知道发生了什么

### 问题根因

**过度静默导致用户失去所有反馈**

在优化 #E02（隐藏 DOS 窗口）时，将所有输出都移除了：
```csharp
catch (Exception ex)
{
    return (int)ExitCode.OtherError;  // 静默失败，无任何反馈
}
```

关键问题：
1. `FreeConsole()` 后控制台完全不可见
2. 所有错误被静默处理
3. 没有日志文件记录执行情况
4. 成功通知可能因其他原因失败而错过

### 影响范围

- 用户无法知道程序是否正在运行
- 用户无法知道处理到哪个步骤
- 失败时完全无反馈，用户体验极差
- 无法进行问题排查

### 修复方案

#### 1. 添加日志文件

```csharp
private static string LogFilePath => Path.Combine(Path.GetTempPath(), "AI.DiffAssistant.log");

private static void Log(string message)
{
    try
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        File.AppendAllText(LogFilePath, $"[{timestamp}] {message}\n");
    }
    catch { }
}
```

#### 2. 关键步骤记录日志

```csharp
Log("[启动] 参数数量: 2");
Log($"[参数] 收到文件: {fileA}, {fileB}");
Log("[配置] 加载配置...");
Log("[文件] 读取文件...");
Log("[AI] 开始调用 AI 服务...");
Log("[写入] 写入分析结果...");
Log($"[完成] 结果已写入: {outputPath}");
```

#### 3. 关键错误发送通知

```csharp
if (string.IsNullOrWhiteSpace(config.Api.BaseUrl) ||
    string.IsNullOrWhiteSpace(config.Api.ApiKey))
{
    Log("[错误] AI 服务未配置");
    ShowErrorNotification("请先在配置中心设置 API 信息");
    return (int)ExitCode.ConfigurationError;
}
```

#### 4. 通知消息改进

- 成功通知：`差异分析成功，结果已写入 difference.md`
- 错误通知：根据不同错误显示不同消息
- 配置未设置：`请先在配置中心设置 API 信息`
- AI 分析失败：`AI 分析失败: {具体错误}`
- 写入失败：`写入结果失败: {具体错误}`

### 日志文件位置

- 路径：`%TEMP%\AI.DiffAssistant.log`
- 大小限制：最大 100KB，超出自动清理
- 格式：`[时间戳] [级别] 消息`

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 修复文件

- `src/AI.DiffAssistant.Cli/Program.cs`
  - 添加 `LogFilePath` 和日志写入方法
  - 在关键步骤添加日志记录
  - 关键错误发送 Toast 通知
  - 改进通知消息内容

### 发现日期

2025-12-28

### 修复日期

2025-12-28

---

## 问题 #006: Toast 通知不弹出

### 问题描述

差异分析完成后，没有弹出 Windows Toast 通知，用户无法得知分析已完成。

### 问题根因

**`app.manifest` 缺少 `windowsToastNotifications` 能力声明**

`Microsoft.Toolkit.Uwp.Notifications` 库需要在应用程序清单中声明 Toast 通知能力才能正常工作。

原 `app.manifest` 配置：
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

缺少 `windowsToastNotifications` 声明，导致 Toast 通知被系统阻止。

### 影响范围

- 分析成功时无成功通知
- 分析失败时无错误通知
- 用户无法得知处理结果

### 修复方案

更新 `src/AI.DiffAssistant.Cli/app.manifest`，添加 Toast 通知能力：

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

  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <!-- 启用 Windows Toast 通知 -->
      <windowsToastNotifications xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">
        <enabled>true</enabled>
      </windowsToastNotifications>
    </windowsSettings>
  </application>
</assembly>
```

### 技术说明

- `windowsToastNotifications` 是 Windows 10/11 原生 Toast 通知所需的能力声明
- 需要 `net10.0-windows10.0.17763.0` 或更高版本的 TargetFramework
- 需要 Windows 10 Version 1809 (Build 17763) 或更高版本

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 修复文件

- `src/AI.DiffAssistant.Cli/app.manifest` - 添加 windowsToastNotifications 能力声明

### 发现日期

2025-12-30

### 修复日期

2025-12-30

---

## 问题 #008: 版本下载刷新返回 401 Unauthorized

### 问题描述

在“版本下载”页点击“刷新”，提示：
`加载版本列表失败: Response status code does not indicate success: 401 (Unauthorized)`

### 问题根因

**Release 请求复用了 AI HttpClient，携带了 OpenAI 的 Authorization 头**

`MainViewModel` 中 `AiService` 与 `ReleaseService` 共用同一个 `HttpClient`。  
`AiService` 会设置 `DefaultRequestHeaders.Authorization`（Bearer API Key），导致 GitHub Releases API 收到无效 Authorization，返回 401。

### 影响范围

- 版本下载列表无法加载
- 用户无法获取稳定版下载信息

### 修复方案

1. 将 ReleaseService 改为独立的 `HttpClient` 实例，避免继承 AI 请求头。
2. Release 请求始终设置 `User-Agent`，确保 GitHub API 正常响应。
3. ReleaseService 构造时清除 `Authorization`，请求级别强制为空，避免残留头影响。

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 修复文件

- `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs`
  - 拆分 `_aiHttpClient` 与 `_releaseHttpClient`
- `src/AI.DiffAssistant.Core/Release/ReleaseService.cs`
  - 请求中始终设置 `User-Agent`
  - 清理 `Authorization` 头

### 发现日期

2026-01-02

### 修复日期

2026-01-02

---

## 问题 #009: 右键菜单执行 GUI 而非 CLI

### 问题描述

Ctrl 选中两个文件后右键“diff-check”，弹出的不是差异分析，而是配置中心 GUI。

### 问题根因

**注册表命令使用了 GUI 可执行文件**

当 `diff-check-cli.exe` 不在 GUI 同目录（例如 GUI 发布在 `publish/gui`，CLI 发布在 `publish/cli`），  
`GetCliExecutablePath()` 会回退为当前 GUI 路径，导致注册表命令指向 GUI。

### 影响范围

- 右键菜单无法执行静默分析
- 每次右键都会弹出配置中心

### 修复方案

1. 查找 CLI 路径：优先同目录，其次 `..\cli\diff-check-cli.exe`。
2. 若仍找不到 CLI，阻止注册并提示用户放置 CLI 到指定位置。
3. 启动时检测注册路径是否为 CLI，若不是则自动重注册。

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 修复文件

- `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs`
  - `GetCliExecutablePath()` 增加 `publish/cli` 搜索路径
  - CLI 缺失时抛出错误提示，避免错误注册
  - `RefreshRegistrationStatus()` 自动修复注册路径

### 发现日期

2026-01-02

### 修复日期

2026-01-02

---

## 问题 #007: ToastContentBuilder.Show 方法找不到

### 问题描述

差异分析完成后，Toast 通知失败，错误日志显示：
```
Method not found: 'Void Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder.Show()'.
Object reference not set to an instance of an object.
```

### 问题根因

**1. CLI 项目未直接引用 NuGet 包**

CLI 项目 (`AI.DiffAssistant.Cli.csproj`) 仅通过项目引用依赖 Core 项目：
```xml
<ProjectReference Include="..\AI.DiffAssistant.Core\AI.DiffAssistant.Core.csproj" />
```

Core 项目中声明的 `Microsoft.Toolkit.Uwp.Notifications` 包依赖未正确传递到 CLI 项目的发布输出目录。

**2. 旧版本 DLL 缓存**

发布文件夹中可能包含旧版本的 `Microsoft.Toolkit.Uwp.Notifications.dll`，导致方法签名不匹配。

### 影响范围

- 分析成功时无成功通知
- 分析失败时错误通知也失败
- 用户无法得知处理结果

### 修复方案

**1. 在 CLI 项目中添加直接 NuGet 引用**

修改 `src/AI.DiffAssistant.Cli/AI.DiffAssistant.Cli.csproj`：
```xml
<!-- 确保通知库的 DLL 被复制到输出目录 -->
<ItemGroup>
  <PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.1.3" />
</ItemGroup>
```

**2. 清理并重新发布**

```bash
# 删除旧的发布文件夹
dotnet clean
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

### 技术说明

- `Microsoft.Toolkit.Uwp.Notifications` 7.1.3 的 `ToastContentBuilder.Show()` 方法返回类型为 `void`
- 确保所有依赖项目的包引用正确传递是 .NET 发布的关键
- 清理 NuGet 缓存和发布文件夹可以解决 DLL 版本冲突

### 状态

- [x] 问题已确认
- [x] 修复中
- [x] 已完成

### 修复文件

- `src/AI.DiffAssistant.Cli/AI.DiffAssistant.Cli.csproj` - 添加直接 NuGet 引用

### 发现日期

2025-12-30

### 修复日期

2025-12-30
