# 系统架构文档

> diff-check 架构设计说明（内部项目名仍为 AI.DiffAssistant.*）

---

## 1. 项目结构概览

```
src/
├── AI.DiffAssistant.sln              # 解决方案文件
├── AI.DiffAssistant.Core/            # 核心业务逻辑
├── AI.DiffAssistant.GUI/             # WPF 用户界面
├── AI.DiffAssistant.Shared/          # 共享模型
├── AI.DiffAssistant.Cli/             # CLI 入口（静默模式）
└── AI.DiffAssistant.Tests/           # 单元/集成测试
```

---

## 2. 各项目职责说明

### 2.1 AI.DiffAssistant.Shared (共享模型层)

**职责**: 定义跨项目共享的数据模型和配置结构

**核心文件**:
| 文件 | 作用 |
|------|------|
| `Models/AppConfig.cs` | 应用程序配置根模型，包含 API、Prompts、Settings |
| `Models/ApiConfig.cs` | AI API 配置（BaseUrl、ApiKey、Model） |
| `Models/PromptConfig.cs` | 提示词配置（SystemPrompt） |
| `Models/ParseResult.cs` | 单个文件解析结果模型（V1.1 新增） |
| `Models/FileParseResult.cs` | 封装两个文件解析结果（V1.1 新增） |
| `Constants/ConfigConstants.cs` | 配置相关常量（默认值、路径等） |

**依赖**: 无（纯 POCO 类库）

**特点**: 仅包含数据模型和常量，不包含业务逻辑

---

### 2.2 AI.DiffAssistant.Core (核心业务逻辑层)

**职责**: 实现所有业务功能模块，供 GUI 和 CLI 调用

**模块结构**:
| 目录 | 模块 | 职责 |
|------|------|------|
| `Config/` | 配置管理 | 配置文件读写、配置热加载 |
| `Diff/` | 差异分析 | AI 调用、Prompt 组装、结果生成 |
| `File/` | 文件处理 | 编码检测、文件读取、文本截断 |
| `Registry/` | 注册表操作 | 右键菜单注册/注销 |
| `Notification/` | 系统通知 | Toast 通知、消息框 |
| `Util/` | 工具类 | 单实例管理 |

**核心类**:
| 类名 | 作用 |
|------|------|
| `ConfigManager` | 配置加载/保存，支持 FileSystemWatcher 热更新 |
| `AiService` | AI API 调用，连接测试，错误友好提示 |
| `EncodingDetector` | 自动检测 UTF-8/GBK/ASCII 编码 |
| `FileProcessor` | 文件读取、截断逻辑 |
| `DiffAnalyzer` | 调用 AI 生成差异分析 |
| `ResultWriter` | 追加写入 difference.md |
| `RegistryManager` | 注册表操作，右键菜单管理 |
| `NotificationManager` | 系统通知，使用 MessageBox Win32 API |
| `SingleInstanceManager` | Mutex 互斥锁，处理多实例 |

**依赖**: Shared 项目

---

### 2.3 AI.DiffAssistant.GUI (WPF 界面层)

**职责**: 提供配置中心 UI，实现 MVVM 模式

**结构**:
| 目录/文件 | 作用 |
|-----------|------|
| `Views/` | XAML 视图文件 |
| `ViewModels/` | MVVM ViewModels |
| `Converters/` | 值转换器 |
| `Themes/` | 主题资源文件（深色/浅色） |
| `Assets/` | 品牌资源（应用图标、通知图标） |
| `App.xaml` | WPF 应用入口资源字典 |
| `MainWindow.xaml` | 配置中心主窗口 |
| `SystemTrayManager.cs` | 系统托盘管理器（V2.0 新增） |
| `Views/AboutWindow.xaml` | 关于弹窗（V2.0 新增） |

**核心类**:
| 类名 | 作用 |
|------|------|
| `MainViewModel` | 主窗口 ViewModel，实现 INotifyPropertyChanged，包含配置管理、连接测试、右键菜单注册、主题切换等业务逻辑 |
| `MainWindow` | 主窗口视图，处理 UI 事件、密码显示切换、主题切换；集成 SystemTrayManager（V2.0 新增） |
| `SystemTrayManager` | 系统托盘管理器（V2.0 新增）：托盘图标显示、双击交互、右键菜单、资源管理、IDisposable |
| `AboutWindow` | 关于弹窗（V2.0 新增）：显示版本、作者、官网、版权、更新渠道信息 |
| `BoolToStatusConverter` | 布尔值到注册状态文本的转换器 |
| `BoolToColorConverter` | 布尔值到颜色的转换器（已集成/未集成状态颜色） |
| `RelayCommand` | 简单的 ICommand 实现 |

**主题系统**:
| 文件 | 作用 |
|------|------|
| `Themes/DarkTheme.xaml` | 深色主题资源字典（背景 #1E1E1E，强调色 #0078D4） |
| `Themes/LightTheme.xaml` | 浅色主题资源字典（背景 #FFFFFF） |
| `App.xaml.cs` | 主题管理器（IsDarkTheme、ToggleTheme、ApplyTheme） |

**品牌资源**:
| 文件 | 作用 |
|------|------|
| `Assets/diff-check.ico` | 应用图标（可执行文件/右键菜单） |
| `Assets/diff-check.png` | Toast 通知图标 |

**设计模式**: MVVM（Model-View-ViewModel）

**依赖**: Core、Shared

---

### 2.4 AI.DiffAssistant.Cli (命令行入口)

**职责**: 处理静默模式启动，接收文件路径参数执行分析

**核心文件**:
| 文件 | 作用 |
|------|------|
| `Program.cs` | CLI 主入口，参数解析，流程编排，错误处理 |
| `ArgsParser.cs` | 命令行参数解析，返回 ParseResult |
| `app.manifest` | Windows 应用程序清单，声明信任级别，启用 Toast 通知 |
| `AI.DiffAssistant.Cli.csproj` | 项目配置，定义为 WinExe 移除控制台窗口 |
| `Assets/diff-check.ico` | 右键菜单与应用图标资源 |
| `Assets/diff-check.png` | Toast 通知图标资源 |

**输出类型说明**:
- `WinExe`: 以 Windows 应用程序运行（无控制台窗口）
- `SubApplication`: 标记为子应用程序，支持 GUI 通知
- `app.manifest`: 包含 `trustInfo` 声明，启用 Windows Toast 通知能力

**启动模式**:
- `0` 参数: 启动 GUI 模式
- `2` 参数: 执行静默分析
- 其他: 显示错误通知并退出

**退出代码**:
| 代码 | 含义 |
|------|------|
| `0` | 成功 |
| `1` | 文件不存在或无效 |
| `2` | AI 调用失败 |
| `3` | 文件写入失败 |
| `4` | 配置错误 |
| `99` | 其他未处理异常 |

**执行流程**:
1. 解析命令行参数
2. 获取互斥锁（单实例检查）
3. 加载配置文件
4. 读取并处理两个文件（编码检测 + 截断）
5. 调用 AI 进行差异分析
6. 追加写入 `difference.md`
7. 显示成功/失败通知

**依赖**: Core、Shared

---

### 2.5 AI.DiffAssistant.Tests (测试项目)

**职责**: 单元测试和集成测试

**测试类型**:
| 类型 | 工具 | 用途 |
|------|------|------|
| 单元测试 | xUnit | 测试配置模型、编码检测、截断逻辑、解析器 |
| Mock 测试 | WireMock.Net | 模拟 OpenAI API 响应 |

**测试文件结构** (V1.1 新增):
| 文件 | 测试内容 |
|------|----------|
| `DocxParserTests.cs` | Word (.docx) 解析器测试：文本提取、表格提取、空文档、中英文/Unicode、特殊字符 |
| `PdfParserTests.cs` | PDF 解析器测试：文件不存在、路径验证、异常处理 |
| `FileParserRouterTests.cs` | 路由器测试：路由逻辑、双文件解析、未知格式、扩展名支持 |
| `NotificationManagerTests.cs` | Toast 通知测试：成功/错误通知、文件路径、多通知场景 |

**覆盖范围**:
- 配置序列化/反序列化
- 编码检测准确性
- 文件截断逻辑
- AI 请求构建正确性
- 结果格式生成
- GUI 完整流程
- CLI 静默模式
- **V1.1 新增**: DocxParser、PdfParser、FileParserRouter、Toast 通知

**测试统计**:
| 项目 | V1.0 测试数 | V1.1 新增 | 总计 |
|------|------------|----------|------|
| 配置模型 | 6 | 0 | 6 |
| 文件处理 | 15 | 0 | 15 |
| 参数解析 | 10 | 0 | 10 |
| 注册表操作 | 4 | 0 | 4 |
| 单实例管理 | 2 | 0 | 2 |
| 结果写入 | 10 | 0 | 10 |
| 通知管理 | 11 | 12 | 23 |
| DocxParser | 0 | 16 | 16 |
| PdfParser | 0 | 9 | 9 |
| FileParserRouter | 0 | 18 | 18 |
| **总计** | **73** | **55** | **128** |

**依赖**: Core、GUI、Cli、Shared、WireMock.Net

---

## 3. 技术栈

| 层级 | 技术 |
|------|------|
| 运行时 | .NET 10.0 Windows |
| GUI | WPF + WinForms (SystemTray 支持) |
| HTTP | System.Net.Http.HttpClient |
| JSON | System.Text.Json |
| 测试 | xUnit + WireMock.Net |
| 构建 | dotnet publish AOT 编译 |

---

## 4. 数据流

### GUI 模式（配置中心）
```
User Input → MainViewModel
                  ↓
            ConfigManager → config.json
                  ↓
            RegistryManager → HKCR右键菜单
                  ↓
            AiService → Test Connection
```

### CLI 静默模式（右键执行）
```
File Explorer (2 files) → Cli/Program.cs
                            ↓
                      SingleInstanceManager (Mutex)
                            ↓
                      ConfigManager.Load()
                            ↓
                      FileProcessor.Read() → EncodingDetector
                            ↓
                      DiffAnalyzer.Analyze() → AiService → OpenAI API
                            ↓
                      ResultWriter.Append() → difference.md
                            ↓
                      NotificationManager.ShowSuccess() / ShowError()
```

---

## 5. 配置存储

**路径**: `%APPDATA%\diff-check\config.json`

**格式**:
```json
{
  "api": { "baseUrl": "...", "apiKey": "...", "model": "..." },
  "prompts": { "system": "..." },
  "settings": { "maxTokenLimit": 15000 }
}
```

---

## 7. 核心文件详解

### 7.1 Config/ 配置管理模块

| 文件 | 作用 |
|------|------|
| `ConfigManager.cs` | 配置加载/保存，支持 FileSystemWatcher 热更新；提供 `Load()`、`Save()`、`Reload()` 方法 |

### 7.2 Diff/ 差异分析模块

| 文件 | 作用 |
|------|------|
| `AiService.cs` | AI API 调用封装，支持连接测试、错误友好提示；处理 API Key 验证、额度检查 |
| `DiffAnalyzer.cs` | 差异分析编排器，组装 Prompt、调用 AiService、返回分析结果 |
| `ResultWriter.cs` | 追加写入 `difference.md`，支持 Markdown 格式化、状态跟踪 |

### 7.3 File/ 文件处理模块

| 文件 | 作用 |
|------|------|
| `EncodingDetector.cs` | 自动检测文件编码（UTF-8/GBK/ASCII），支持 BOM 识别 |
| `FileProcessor.cs` | 文件读取、截断逻辑；支持纯文本和富文本（.docx/.pdf）解析；提供 `ReadFile()`、`ReadFileAsync()`、`ProcessFile()` 方法 |

**FileProcessor V1.1 增强**:
- 内部集成 `FileParserRouter`，自动路由到对应解析器
- `ReadResult` 新增 `SourceFileType` 和 `IsRichText` 属性
- 富文本格式（.docx/.pdf）使用解析器提取纯文本
- 纯文本格式使用 `EncodingDetector` 检测编码

### 7.4 Parser/ 文件解析模块 (V1.1 新增)

**相关模型**（位于 Shared 项目）:
| 文件 | 作用 |
|------|------|
| `Models/ParseResult.cs` | 单个文件解析结果，包含 Content、SourceFileType、IsSuccess、ErrorMessage、CharCount |
| `Models/FileParseResult.cs` | 封装两个文件的解析结果，提供 IsSuccess、ErrorMessage、TotalCharCount |

**解析器组件**:
| 文件 | 作用 |
|------|------|
| `IFileParser.cs` | 解析器接口，定义 `CanParse()`、`Parse()` 方法（V1.1 新增） |
| `DocxParser.cs` | Word (.docx) 解析器，提取正文、段落、表格文本（V1.1 新增） |
| `PdfParser.cs` | PDF 解析器，提取页面文本，处理加密/扫描件异常（✅ 已实现） |
| `FileParserRouter.cs` | 解析路由器，根据扩展名分发到对应解析器（✅ 已实现） |

**IFileParser 接口设计**:
```csharp
public interface IFileParser
{
    bool CanParse(string ext);           // 检查是否支持指定扩展名
    ParseResult Parse(string filePath);  // 解析文件并返回结果
}
```

**DocxParser 实现特性**:
- 使用 `DocumentFormat.OpenXml` 库解析 `.docx` 文件
- 提取正文段落文本，保留换行符
- 提取表格文字，使用 `|` 作为单元格分隔符
- 忽略页眉、页脚、批注、图片、样式信息

**PdfParser 实现特性**:
- 使用 `PdfPig` (UglyToad.PdfPig) 库解析 `.pdf` 文件
- 按页顺序提取所有页面文本
- 加密文档检测：捕获 `PdfDocumentEncryptedException`
- 扫描件检测：若 `text.Length < 50 && fileSize > 100KB` → 返回错误
- 异常信息友好提示，支持中文错误描述

**FileParserRouter 实现特性**:
- 路由器模式：自动根据扩展名分发到对应解析器
- 内部注册 DocxParser 和 PdfParser
- 提供 `ParseFile()` 单文件解析和 `ParseFiles()` 双文件解析
- 不支持格式返回友好错误信息
- `GetSupportedExtensions()` 使用 `IsSupported()` 探测扩展名，而非反射获取常量字段

### 7.5 Registry/ 注册表模块

| 文件 | 作用 |
|------|------|
| `RegistryManager.cs` | 右键菜单注册/注销；创建 `HKCU\Software\Classes\*\shell\diff-check` 键 |

### 7.6 Notification/ 通知模块

| 文件 | 作用 |
|------|------|
| `NotificationManager.cs` | Windows Toast 通知，使用 `Microsoft.Toolkit.Uwp.Notifications` 库 |

**NotificationManager V1.1 增强**:
- 使用 `ToastContentBuilder` 构建原生 Windows Toast 通知
- AppId 更新为 `diff-check`
- `ShowSuccess(message, filePathToOpen)`: 成功通知带操作按钮
  - 标题: "分析完成"
  - 按钮: [打开文件]、[打开文件夹]
  - 点击通知或按钮可打开 difference.md
- `ShowError(error)`: 错误通知
  - 标题: "分析失败"
  - 正文: 显示具体错误原因
- 不抢占键盘焦点，仅在右下角滑入显示
- 自动收入通知中心，不强制用户关闭

**技术要求**:
- TargetFramework: `net10.0-windows10.0.17763.0`
- Windows 10 Build 17763（Version 1809）或更高版本
- 依赖 `Microsoft.Toolkit.Uwp.Notifications` 7.1.3 |

**发布产物命名**:
- GUI 可执行文件：`diff-check.exe`
- CLI 可执行文件：`diff-check-cli.exe`

### 7.7 Util/ 工具类模块

| 文件 | 作用 |
|------|------|
| `SingleInstanceManager.cs` | Mutex 互斥锁，处理 Windows 多文件选择可能触发的多实例启动 |

---

## 8. V1.1 架构变更

### 新增模块

**Shared 项目新增模型**:
```
src/AI.DiffAssistant.Shared/Models/
├── ParseResult.cs        # 文件解析结果模型 ✅ 已实现
└── FileParseResult.cs    # 双文件解析结果封装 ✅ 已实现
```

**Core 项目新增解析器**:
```
src/AI.DiffAssistant.Core/Parser/
├── IFileParser.cs        # 解析器接口 ✅ 已实现
├── DocxParser.cs         # Word 解析器 ✅ 已实现
├── PdfParser.cs          # PDF 解析器 ✅ 已实现
└── FileParserRouter.cs   # 解析路由器 ✅ 已实现
```

### 依赖变更

| 项目 | 新增依赖 | 用途 |
|------|----------|------|
| Core | `DocumentFormat.OpenXml` 3.0.2 | Word 文档解析 |
| Core | `PdfPig` 0.1.8 | PDF 文档解析 |
| Core | `Microsoft.Toolkit.Uwp.Notifications` 7.1.3 | Windows Toast 通知 |

### TargetFramework 变更

**V1.0**: `net10.0-windows`
**V1.1**: `net10.0-windows10.0.17763.0`

变更原因：`Microsoft.Toolkit.Uwp.Notifications` 需要 Windows SDK 10.0.17763.0 或更高版本以支持 Toast 通知 API。

**兼容性**:
- Windows 10 Version 1809 (Build 17763) 及更高版本完全支持
- Windows 7/8.x 不支持 Toast 通知（但程序可运行，仅不显示通知）

---

## 9. V2.0 架构变更 - 系统托盘与后台常驻

### 实施日期
2026-01-02

### 新增功能
**系统托盘与后台常驻**：软件启动后在右下角系统托盘区显示图标，支持双击显示/隐藏主窗口，右键菜单提供主面板/关于/退出选项。关闭按钮行为改为最小化到托盘而非退出应用程序。

### 新增文件

**GUI 项目新增文件**:
```
src/AI.DiffAssistant.GUI/
├── SystemTrayManager.cs          # 系统托盘管理器 ✅ 已实现
└── Views/
    ├── AboutWindow.xaml           # 关于弹窗 XAML ✅ 已实现
    └── AboutWindow.xaml.cs        # 关于弹窗代码 ✅ 已实现
```

### 依赖变更

| 项目 | 变更 | 原因 |
|------|------|------|
| GUI | 新增 `<UseWindowsForms>true</UseWindowsForms>` | 引入 NotifyIcon 支持系统托盘 |

### 核心实现说明

#### SystemTrayManager.cs
**职责**: 系统托盘管理

**核心功能**:
- 托盘图标初始化与显示（使用 `NotifyIcon`）
- 双击托盘图标切换主窗口显示/隐藏状态
- 右键菜单管理（主面板/关于/退出）
- 图标资源加载与容错机制
- 完整的 IDisposable 实现确保资源释放

**关键方法**:
| 方法 | 作用 |
|------|------|
| `Initialize()` | 初始化托盘图标和菜单 |
| `ToggleMainWindow()` | 切换主窗口显示状态 |
| `ShowAboutWindow()` | 显示关于弹窗 |
| `ExitApplication()` | 退出应用程序 |
| `GetTrayIcon()` | 加载托盘图标资源，支持多种加载方式 |
| `CreateDefaultIcon()` | 创建默认图标作为兜底方案 |

**容错机制**:
- 图标资源加载失败时自动创建默认图标
- 托盘初始化失败不影响程序主功能运行
- 所有异常都有日志记录和异常捕获

#### AboutWindow.xaml / AboutWindow.xaml.cs
**职责**: 关于弹窗显示

**显示信息**:
- 版本号与构建日期
- 作者信息
- 官网链接
- 版权声明
- 更新渠道说明

**设计特点**:
- 统一的 UI 风格（深色/浅色主题适配）
- 居中显示，无任务栏显示
- 简单的确认按钮交互

#### MainWindow.xaml.cs 增强
**新增功能**:
- 集成 `SystemTrayManager`
- 重写 `OnClosing()` 方法：关闭按钮最小化到托盘
- 重写 `OnClosed()` 方法：托盘资源清理

**关键变更**:
```csharp
protected override void OnClosing(CancelEventArgs e)
{
    e.Cancel = true;  // 取消关闭
    Hide();           // 隐藏到托盘
}
```

#### App.xaml.cs 增强
**新增功能**:
- 单实例运行逻辑（使用 Mutex）
- 重复启动时激活现有窗口
- Windows API 调用实现窗口激活

**关键实现**:
- `MutexName = "AI.DiffAssistant.SingleInstance.Mutex"`
- `ActivateExistingInstance()`: 查找并激活现有窗口
- `SetForegroundWindow()` + `ShowWindow()`: Windows API 调用

### 命名空间冲突解决

**问题**: WPF 与 WinForms 同时启用时存在命名空间歧义

**解决方案**: 使用 using 别名明确指定类型

| 原始类型 | 歧义 | 解决方案 |
|----------|------|----------|
| `Application` | WPF vs WinForms | `WpfApplication = System.Windows.Application` |
| `TextBox` | WPF vs WinForms | `WpfTextBox = System.Windows.Controls.TextBox` |
| `Button` | WPF vs WinForms | `WpfButton = System.Windows.Controls.Button` |
| `Brushes` | WPF vs WinForms | `WpfBrushes = System.Windows.Media.Brushes` |
| `MessageBox` | WPF vs WinForms | `WpfMessageBox = System.Windows.MessageBox` |

### 构建验证
- ✅ 项目构建成功，0 错误
- ⚠️ 仅 3 个警告（未使用字段和空引用检查）

### 性能影响
- 启动时间: 无明显影响（<1秒）
- 内存占用: 最小化托盘后内存占用显著降低
- CPU 使用: 后台运行时 CPU 占用接近 0

---

## 10. V1.1 集成测试验证 ✅

**验证日期**: 2025-12-30

### 10.1 测试执行结果

| 测试类型 | 测试项 | 结果 |
|----------|--------|------|
| 单元测试 | 128 个测试全部通过 | ✅ |
| 构建 | dotnet build | ✅ 0 错误 |
| 发布 | AOT 编译 | ✅ 成功 |
| 集成测试 | txt + txt 对比 | ✅ difference.md 正确生成 |
| 集成测试 | CLI 静默模式 | ✅ 无 DOS 窗口 |
| 集成测试 | Toast 通知 | ✅ 正常显示 |

### 9.2 文件清单验证

**Shared 项目新增模型**:
```
src/AI.DiffAssistant.Shared/Models/
├── ParseResult.cs        # 文件解析结果模型 ✅ 已实现
└── FileParseResult.cs    # 双文件解析结果封装 ✅ 已实现
```

**Core 项目新增解析器**:
```
src/AI.DiffAssistant.Core/Parser/
├── IFileParser.cs        # 解析器接口 ✅ 已实现
├── DocxParser.cs         # Word 解析器 ✅ 已实现
├── PdfParser.cs          # PDF 解析器 ✅ 已实现
└── FileParserRouter.cs   # 解析路由器 ✅ 已实现
```

**CLI 项目新增文件**:
```
src/AI.DiffAssistant.Cli/
├── app.manifest          # 应用程序清单 ✅ 已实现
└── Program.cs            # 集成 Toast 回调 ✅ 已实现
```

---

## 11. 注意事项

### 11.1 单实例处理机制

Windows 文件资源管理器多选文件时可能触发多个实例启动。程序使用以下机制协调：

1. **互斥锁**: `Global\AI.DiffAssistant.SingleInstance` 确保单实例运行
2. **参数协调**: `ArgsParser.CoordinateArguments()` 使用临时文件和互斥锁收集多实例参数
3. **等待模式**: 单参数时 `IsWaitingMode=true`，等待第二个参数后执行

### 11.2 文件截断策略

- **阈值**: 15,000 字符（可配置）
- **保留内容**: 文件头部（前 15,000 字符）
- **状态标记**: 截断后在结果中显示保留百分比

### 11.3 编码检测优先级

1. UTF-8 with BOM
2. UTF-8 without BOM
3. GBK
4. ASCII（回退）

### 11.4 系统托盘支持（V2.0 新增）

**托盘交互行为**:
- 启动后自动在系统托盘区显示图标
- 双击托盘图标：显示/隐藏主窗口
- 右键菜单：主面板/关于/退出
- 关闭按钮：最小化到托盘（不退出进程）
- 托盘提示文本：`"diff-check"`

**托盘图标资源**:
- 优先加载：`Assets/diff-check.ico`
- 备用加载：输出目录 `diff-check.ico`
- 默认兜底：程序生成的默认图标

**单实例运行**:
- 使用 Mutex 确保只有一个实例运行
- 重复启动时激活现有窗口
- 使用 Windows API 实现窗口激活

### 11.5 FileParserRouter 扩展名获取方式变更

**变更日期**: 2025-12-30

**问题描述**:
`GetSupportedExtensions()` 方法使用反射获取解析器的私有常量字段，但 `const` 字段是静态的，无法通过 `BindingFlags.Instance` 获取。

**修复方案**:
```csharp
// 修复前（反射方式，无法获取 const 字段）
public IEnumerable<string> GetSupportedExtensions()
{
    return _parsers.SelectMany(p => p.GetType()
        .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(f => f.Name.EndsWith("Extension"))
        .Select(f => (string)f.GetValue(p)));
}

// 修复后（IsSupported 探测方式）
public IEnumerable<string> GetSupportedExtensions()
{
    var testExtensions = new[] { ".docx", ".pdf", ".txt", ".md", ".json" };
    return testExtensions.Where(ext => IsSupported(ext));
}
```

**优点**:
- 避免使用反射，更简洁可靠
- 扩展名列表可动态扩展，无需修改解析器
- 性能更优（O(n) vs O(n*反射)）

---

### 11.6 app.manifest 应用程序清单

**文件路径**: `src/AI.DiffAssistant.Cli/app.manifest`

**作用**:
- 声明应用程序的信任级别（`asInvoker` - 继承调用者权限）
- 启用 Windows Toast 通知能力
- 确保程序以 GUI 应用程序运行而非控制台应用

**关键配置**:
```xml
<trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">
  <security>
    <requestedPrivileges>
      <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
    </requestedPrivileges>
  </security>
</trustInfo>
```

**配置说明**:
| 配置项 | 值 | 含义 |
|--------|-----|------|
| `level` | `asInvoker` | 继承 Explorer 的权限级别，无需管理员权限 |
| `uiAccess` | `false` | 不需要提升权限访问 UI 元素 |

---
