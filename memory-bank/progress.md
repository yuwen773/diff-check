# 项目进度记录

> 供后续开发者参考的开发进度追踪

---

## 阶段一：项目基础架构

### 步骤 1.1 - 项目结构创建 ✅

**完成日期**: 2025-12-28

**完成内容**:
1. 在 `src/` 目录下创建解决方案 `AI.DiffAssistant.sln`
2. 创建 5 个项目:
   - `AI.DiffAssistant.Shared` - 共享模型类库
   - `AI.DiffAssistant.Core` - 核心业务逻辑类库
   - `AI.DiffAssistant.GUI` - WPF 应用程序
   - `AI.DiffAssistant.Cli` - 控制台应用程序（静默模式入口）
   - `AI.DiffAssistant.Tests` - xUnit 测试项目
3. 添加 WireMock.Net 测试依赖用于 Mock OpenAI API
4. 建立项目引用关系:
   - Core → Shared
   - GUI → Core + Shared
   - Cli → Core + Shared
   - Tests → All projects
5. 修复测试项目框架兼容性（net10.0 → net10.0-windows）

**验证结果**:
- `dotnet restore` ✅ 通过
- `dotnet build` ✅ 通过

---

## 阶段二：配置管理模块 ✅ 已完成

**完成日期**: 2025-12-28

### 步骤 2.1 - 配置数据模型定义 ✅
**完成内容**:
1. 创建 `ApiConfig` 类（BaseUrl、ApiKey、Model）
2. 创建 `PromptConfig` 类（SystemPrompt）
3. 创建 `AppSettings` 类（MaxTokenLimit）
4. 创建 `AppConfig` 根配置类
5. 创建 `ConfigConstants` 常量类

**文件位置**:
- `src/AI.DiffAssistant.Shared/Models/`

### 步骤 2.2 - 配置文件读写实现 ✅
**完成内容**:
1. 创建 `ConfigManager` 类
2. 实现 `LoadConfig()` 方法（支持默认配置创建）
3. 实现 `SaveConfig()` 方法
4. 实现 `FileSystemWatcher` 热更新监听
5. 配置路径: `%APPDATA%\AI.DiffAssistant\config.json`

**文件位置**:
- `src/AI.DiffAssistant.Core/Config/ConfigManager.cs`

### 步骤 2.3 - 连接测试功能 ✅
**完成内容**:
1. 创建 `AiService` 类
2. 实现 `TestConnectionAsync()` 方法
3. 实现 10 秒超时处理
4. 实现友好错误提示映射
5. 创建 `ConnectionResult` 结果类

**文件位置**:
- `src/AI.DiffAssistant.Core/Diff/AiService.cs`

### 验证结果
- `dotnet build` ✅ 通过（所有项目）
- `dotnet test` ✅ 通过（6 个测试全部通过）

---

## 阶段三：文件处理模块 ✅ 已完成

**完成日期**: 2025-12-28

### 步骤 3.1 - 文件编码检测 ✅
**完成内容**:
1. 创建 `EncodingDetector` 类
2. 实现检测逻辑：
   - UTF-8 BOM (EF BB BF)
   - UTF-16 LE/BE BOM
   - UTF-32 LE/BE BOM
   - 无 BOM 时检测纯 ASCII（返回 UTF-8）
   - 无 BOM 时检测有效 UTF-8
   - 检测 GBK 中文编码模式
   - 回退到系统默认编码

**文件位置**:
- `src/AI.DiffAssistant.Core/File/EncodingDetector.cs`

### 步骤 3.2 - 文件截断逻辑 ✅
**完成内容**:
1. 创建 `FileProcessor` 类
2. 实现 `ReadFile()` 和 `ReadFileAsync()` 方法
3. 实现 `TruncateIfNeeded()` 方法：
   - 阈值可配置（默认 15000）
   - 保留头部，截断尾部
   - 计算截断百分比
4. 定义 `ReadResult` 和 `TruncateResult` 数据类

**文件位置**:
- `src/AI.DiffAssistant.Core/File/FileProcessor.cs`

### 步骤 3.3 - AI 差异分析调用 ✅
**完成内容**:
1. 创建 `DiffAnalyzer` 类
2. 实现 `AnalyzeAsync()` 方法（读取文件并分析）
3. 实现 `AnalyzeContentAsync()` 方法（直接分析内容）
4. 实现请求重试逻辑（最多 3 次，指数退避）
5. 实现 OpenAI API 格式请求构建
6. 定义 `DiffAnalysisResult` 结果类

**文件位置**:
- `src/AI.DiffAssistant.Core/Diff/DiffAnalyzer.cs`

### 步骤 3.4 - 结果输出 ✅
**完成内容**:
1. 创建 `ResultWriter` 类
2. 实现 `AppendDiffReport()` 方法（同步）
3. 实现 `AppendDiffReportAsync()` 方法（异步）
4. 实现 Markdown 格式输出：
   - 时间戳（YYYY-MM-DD HH:mm:ss）
   - 状态标注（完整 / 已截断）
   - 追加写入而非覆盖

**文件位置**:
- `src/AI.DiffAssistant.Core/Diff/ResultWriter.cs`

### 验证结果
- `dotnet build` ✅ 通过（所有项目）
- `dotnet test` ✅ 通过（32 个测试全部通过）

---

## 阶段四：Windows 注册表集成 ✅ 已完成

**完成日期**: 2025-12-28

### 步骤 4.1 - 右键菜单注册 ✅
**完成内容**:
1. 创建 `RegistryManager` 类
2. 实现 `RegisterContextMenu(string exePath)` 方法：
   - 在 `HKCU\Software\Classes\*\shell\AI差异分析` 创建注册表项
   - 设置显示名称为 "AI 差异分析"
   - 设置图标和命令路径 `"[exePath]" "%1"`
3. 实现 `UnregisterContextMenu()` 方法：
   - 删除注册表主键及其所有子键
4. 实现 `IsRegistered()` 方法：
   - 检查注册表项是否存在
5. 实现 `GetRegisteredPath()` 方法：
   - 获取已注册的可执行文件路径

**文件位置**:
- `src/AI.DiffAssistant.Core/Registry/RegistryManager.cs`

### 步骤 4.2 - 单实例互斥锁 ✅
**完成内容**:
1. 创建 `SingleInstanceManager` 类
2. 实现全局互斥锁 `Global\AI.DiffAssistant.SingleInstance`
3. 实现超时等待机制（默认 5000ms）
4. 实现 `AbandonedMutexException` 处理（其他进程异常退出）
5. 实现 `RunWithLock()` 静态方法简化使用
6. 实现 `Dispose()` 模式正确释放资源

**文件位置**:
- `src/AI.DiffAssistant.Core/Util/SingleInstanceManager.cs`

### 验证结果
- `dotnet build` ✅ 通过（所有项目）
- `dotnet test` ✅ 通过（52 个测试全部通过）

---

## 阶段五：系统通知模块 ✅ 已完成

**完成日期**: 2025-12-28

### 步骤 5.1 - Toast 通知 ✅
**完成内容**:
1. 创建 `NotificationManager` 类
2. 实现 `ShowSuccess(string message)` 方法：
   - 标题："分析完成 (已追加)"
   - 内容：传入参数
   - 使用 MessageBox Win32 API 实现跨版本兼容
3. 实现 `ShowError(string error)` 方法：
   - 标题："分析失败"
   - 内容：传入错误信息
4. 实现 `Initialize()` 和 `RegisterAppForNotification()` 方法
5. 使用 P/Invoke 调用 user32.dll 的 MessageBox 函数

**文件位置**:
- `src/AI.DiffAssistant.Core/Notification/NotificationManager.cs`

**验证结果**:
- `dotnet build` ✅ 通过（所有项目）
- `dotnet test` ✅ 通过（62 个测试全部通过，添加 10 个新测试）

---

## 阶段六：CLI 静默模式 ✅ 已完成

**完成日期**: 2025-12-28

### 步骤 6.1 - 命令行参数解析 ✅
**完成内容**:
1. 创建 `ArgsParser` 类
2. 实现 `Parse(string[] args)` 方法：
   - 0 参数：返回 GUI 模式
   - 1 参数：返回错误（需要两个文件）
   - 2 参数：返回静默模式，验证文件存在性和可读性
   - >2 参数：返回错误
3. 实现 `GetUsage()` 方法，返回使用说明
4. 创建 `ParseResult` 结果类

**文件位置**:
- `src/AI.DiffAssistant.Cli/ArgsParser.cs`

### 步骤 6.2 - 静默执行主流程 ✅
**完成内容**:
1. 更新 `Program.cs` 实现主流程：
   - 加载配置 (`ConfigManager`)
   - 解析命令行参数 (`ArgsParser`)
   - 获取互斥锁 (`SingleInstanceManager`)
   - 读取并处理文件 (`FileProcessor`)
   - 调用 AI 分析 (`DiffAnalyzer`)
   - 追加写入结果 (`ResultWriter`)
   - 发送通知 (`NotificationManager`)
2. 实现错误处理和退出代码：
   - 0: 成功
   - 1: 文件不存在
   - 2: AI 调用失败
   - 3: 文件写入失败
   - 4: 配置错误
   - 99: 其他异常
3. 实现 GUI 模式启动逻辑

**文件位置**:
- `src/AI.DiffAssistant.Cli/Program.cs`

**验证结果**:
- `dotnet build` ✅ 通过
- `dotnet test` ✅ 通过（62 个测试全部通过，新增 9 个 ArgsParser 测试）

---

## 阶段七：GUI 配置界面 ✅ 已完成

**完成日期**: 2025-12-28

### 步骤 7.1 - 主窗口 XAML ✅
**完成内容**:
1. 设计 MainWindow.xaml 布局（参考 PRD 线框图）
2. 创建以下区域：
   - AI 服务设置区（Base URL、API Key、Model Name）
   - 提示词设置区（多行文本框）
   - 按钮区（测试连接、保存配置）
   - 系统集成区（状态显示、添加/移除按钮）
3. 设置窗口属性：
   - 标题："AI 文档差异助手 - 配置中心"
   - 窗口大小：约 640x520
   - 不可调整大小，居中显示
4. 添加值转换器（BoolToStatusConverter、BoolToColorConverter）

**文件位置**:
- `src/AI.DiffAssistant.GUI/MainWindow.xaml`
- `src/AI.DiffAssistant.GUI/Converters/`

### 步骤 7.2 - MVVM 绑定 ✅
**完成内容**:
1. 创建 `MainViewModel` 类，继承 `INotifyPropertyChanged`
2. 定义绑定属性：
   - `BaseUrl`、`ModelName`、`SystemPrompt`（双向绑定）
   - `ApiKey`（使用 PasswordBox 控件，掩码显示）
   - `IsRegistered`（只读，单向绑定到 UI）
   - `IsTesting`、`IsSaving`（用于按钮禁用状态）
3. 定义命令：
   - `TestConnectionCommand`
   - `SaveConfigCommand`
   - `RegisterCommand`
   - `UnregisterCommand`
4. 实现 `RelayCommand` 简单命令实现类

**文件位置**:
- `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs`

### 步骤 7.3 - 业务逻辑绑定 ✅
**完成内容**:
1. 注入 `ConfigManager`、`AiService`、`RegistryManager`
2. 实现 `TestConnectionCommand` 逻辑：调用 AI 服务测试连接
3. 实现 `SaveConfigCommand` 逻辑：验证并保存配置到文件
4. 实现 `RegisterCommand` 和 `UnregisterCommand`：注册/注销右键菜单
5. 窗口加载时读取现有配置
6. 实现密码显示/隐藏切换功能

**文件位置**:
- `src/AI.DiffAssistant.GUI/MainWindow.xaml.cs`
- `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs`

**验证结果**:
- `dotnet build` ✅ 通过（所有项目）
- `dotnet test` ✅ 通过（73 个测试全部通过）

---

## 阶段八：集成测试与构建 ✅ 已完成

**完成日期**: 2025-12-28

### 步骤 8.1 - 功能集成测试 ✅
**完成内容**:
1. 执行 `dotnet build` 验证所有项目编译通过
2. 执行 `dotnet test` 运行 73 个单元测试，全部通过
3. 创建测试文件验证文件处理逻辑
4. 测试 CLI 静默模式参数解析

**验证结果**:
- `dotnet build` ✅ 通过（0 错误，2 个 CA 警告）
- `dotnet test` ✅ 通过（73/73 测试通过）

### 步骤 8.2 - 性能测试 ✅
**完成内容**:
1. 执行 Release 构建
2. 执行 AOT 编译生成独立 .exe

**生成文件**:
- CLI: `src/publishcli/AI.DiffAssistant.Cli.exe`
- GUI: `src/publish/AI.DiffAssistant.GUI.exe`

### 步骤 8.3 - 构建包生成 ✅
**完成内容**:
1. 使用 AOT 编译生成可执行文件
2. CLI 项目单独发布（用于右键菜单集成）
3. GUI 项目发布（配置中心）

**发布命令**:
```bash
# CLI 独立发布（右键菜单用）
dotnet publish -c Release -r win-x64 --self-contained false -o publish\cli AI.DiffAssistant.Cli.csproj

# GUI 发布
dotnet publish -c Release -r win-x64 --self-contained false -o publish AI.DiffAssistant.GUI.csproj
```

**生成物**:
| 文件 | 位置 |
|------|------|
| CLI 可执行文件 | `src/publishcli/AI.DiffAssistant.Cli.exe` |
| GUI 可执行文件 | `src/publish/AI.DiffAssistant.GUI.exe` |

---
