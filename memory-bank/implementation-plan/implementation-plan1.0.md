# 实施计划：AI 文档差异助手 V1.0

> 基于 `prd1.0.md` 和 `tech-stack.md` 制定
> 聚焦 MVP（最小可行产品），完整功能分阶段实现

---

## 阶段一：项目基础架构

### 1.1 创建解决方案和项目结构

**目标**：建立完整的 .NET 10 WPF 项目框架

**步骤 1.1.1**：在 `src/` 目录下创建解决方案文件 `AI.DiffAssistant.sln`

**步骤 1.1.2**：创建以下项目：
- `AI.DiffAssistant.Core`（类库）
- `AI.DiffAssistant.GUI`（WPF 应用程序）
- `AI.DiffAssistant.Shared`（类库）
- `AI.DiffAssistant.Cli`（控制台应用程序）
- `AI.DiffAssistant.Tests`（xUnit 测试项目，含 Mock 服务器依赖）

**步骤 1.1.2.1**：在 `Tests` 项目中添加 NuGet 依赖：
- `WireMock.Net`（用于 Mock OpenAI API）
- `Microsoft.NET.Test.Sdk`、`xunit`、`xunit.runner.visualstudio`

**步骤 1.1.3**：在 `Core`、`Shared` 项目中添加项目引用依赖关系

**步骤 1.1.4**：在 `GUI` 项目中添加对 `Core` 和 `Shared` 的引用

**步骤 1.1.5**：在 `Cli` 项目中添加对 `Core` 和 `Shared` 的引用

**验证方法**：
- 执行 `dotnet restore` 成功，无依赖错误
- 执行 `dotnet build` 成功，所有项目编译通过
- 检查 `bin/Debug/net10.0-windows/` 目录下生成正确程序集

---

### 1.2 配置 AOT 编译环境

**目标**：确保项目支持 Native AOT 编译为独立 .exe

**步骤 1.2.1**：在 `GUI` 项目文件中启用 Windows 桌面支持：
```
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
</PropertyGroup>
```

**步骤 1.2.2**：配置 AOT 编译属性：
```
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>false</SelfContained>
</PropertyGroup>
```

**步骤 1.2.3**：创建 `Directory.Build.props` 文件，统一管理版本和编译设置

**验证方法**：
- 执行 `dotnet publish -c Release -r win-x64 --self-contained false -o publish`
- 检查 `publish/` 目录生成 `.exe` 文件
- 双击 `.exe` 能正常启动（显示空白窗口即可）

---

## 阶段二：配置管理模块

### 2.1 定义配置数据模型

**目标**：创建配置相关的 C# 数据结构

**注意**：V1.0 **仅支持 OpenAI 兼容的 API 格式**，暂不支持 Ollama 等本地模型服务

**步骤 2.1.1**：在 `Shared` 项目中创建 `ApiConfig` 类：
- 字段：`BaseUrl`（string）、`ApiKey`（string）、`Model`（string）

**步骤 2.1.2**：创建 `PromptConfig` 类：
- 字段：`SystemPrompt`（string），设置默认值为 PRD 中指定的默认提示词

**步骤 2.1.3**：创建 `AppSettings` 类：
- 字段：`MaxTokenLimit`（int），默认值 15000
- 字段：`Api`（ApiConfig）、`Prompts`（PromptConfig）

**步骤 2.1.4**：创建配置根类 `AppConfig`，包含 `Api`、`Prompts`、`Settings`

**验证方法**：
- 创建单元测试：实例化 `AppConfig`，验证默认值为 PRD 规范值
- 验证 `System.Text.Json` 序列化/反序列化正确

---

### 2.2 实现配置文件读写

**目标**：实现 JSON 配置文件的热加载和持久化

**步骤 2.2.1**：确定配置文件存储路径：
- **%APPDATA%\AI.DiffAssistant\config.json**（符合 Windows 规范，避免 Program Files 权限问题）
- 开发环境同样使用此路径，可通过环境变量覆盖

**步骤 2.2.2**：创建 `ConfigManager` 类，定义以下方法：
- `LoadConfig()`：从文件读取，反序列化为 `AppConfig`，文件不存在时返回默认配置
- `SaveConfig(AppConfig config)`：序列化并写入文件
- `GetConfigPath()`：返回配置文件完整路径

**步骤 2.2.3**：实现配置目录自动创建：
- 路径：`Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\AI.DiffAssistant"`
- 文件不存在时自动创建目录和默认配置文件

**步骤 2.2.4**：实现配置变更监听（FileSystemWatcher），支持热更新

**验证方法**：
- 编写测试：调用 `SaveConfig` 后删除文件，再调用 `LoadConfig` 验证默认值
- 编写测试：修改配置后保存，重新加载验证值正确
- 手动测试：运行程序，在程序目录创建 `config.json`，验证程序能正确读取

---

### 2.3 实现连接测试功能

**目标**：验证 AI API 连通性

**步骤 2.3.1**：创建 `AiService` 类，依赖注入 `HttpClient`

**步骤 2.3.2**：实现 `TestConnectionAsync(ApiConfig config)` 方法：
- 构建 OpenAI 兼容的 `/chat/completions` 请求
- 使用流式或非流式 API 发送简短测试（如 "Hi"）
- 返回成功/失败状态和友好错误信息

**步骤 2.3.3**：实现超时处理（建议 10 秒超时）

**步骤 2.3.4**：处理常见错误：网络错误、认证错误、限流错误
- 统一返回**友好提示**，**不**显示技术错误码（如 401、429）
- 示例："无法连接到 AI 服务，请检查网络或 API Key"

**步骤 2.3.5**：实现错误映射表，将技术错误转换为友好提示

**验证方法**：
- **Mock 服务器测试**（推荐，不消耗 API 配额）：
  - 使用 WireMock.NET 或自定义 Mock 服务器
  - 模拟 OpenAI API 响应格式
  - 验证请求格式正确
  - 验证错误响应处理
- 真实 API 测试（可选，用于最终验证）：
  - 使用真实 API Key 测试：连接成功，返回有效响应
  - 使用错误 API Key 测试：返回 401 错误
  - 使用无效 URL 测试：返回网络错误
  - 测试超时：配置 1 秒超时，验证超时异常被正确捕获

---

## 阶段三：文件处理模块

### 3.1 实现文件编码检测

**目标**：自动检测文件编码（UTF-8、GBK、ASCII）

**步骤 3.1.1**：创建 `EncodingDetector` 类

**步骤 3.1.2**：实现检测逻辑：
- 优先尝试 UTF-8（带 BOM 和无 BOM）
- 回退到 GBK（中文 Windows 常见）
- 最终回退到 ASCII 或系统默认编码

**步骤 3.1.3**：创建 `FileReader` 类，封装文件读取方法：
- 使用检测到的编码读取文件
- 返回 `(string content, Encoding encoding)` 元组

**步骤 3.1.4**：处理读取异常（文件不存在、无权限、编码不支持）

**验证方法**：
- 创建 UTF-8 编码文件，测试检测正确
- 创建 GBK 编码文件（中文内容），测试检测正确
- 创建 ASCII 文件，测试检测正确
- 混合编码场景验证

---

### 3.2 实现文件截断逻辑

**目标**：对超长文件进行头部截断

**步骤 3.2.1**：在 `FileProcessor` 类中实现 `TruncateIfNeeded(string content, int maxLength)` 方法

**步骤 3.2.2**：逻辑规则：
- 如果 `content.Length <= maxLength`，返回原内容，返回状态为 "完整"
- 如果 `content.Length > maxLength`，截取前 `maxLength` 字符，返回内容和 "已截断" 状态
- 计算截断百分比：`Math.Round((double)maxLength / originalLength * 100, 1)`
- **截断提示不传给 AI**，只在结果 `difference.md` 中标注

**步骤 3.2.3**：定义 `TruncateResult` 类，包含 `Content`、`Status`、`OriginalLength`、`TruncatedLength`、`Percentage`

**验证方法**：
- 测试：5000 字符文件，15000 阈值，返回 "完整"
- 测试：20000 字符文件，15000 阈值，返回 "已截断"、75.0%
- 测试：边界值（正好 15000 字符），返回 "完整"
- 测试：空文件处理

---

### 3.3 实现 AI 差异分析调用

**目标**：调用 AI API 生成差异分析结果

**步骤 3.3.1**：创建 `DiffAnalyzer` 类，依赖注入 `HttpClient` 和 `AppConfig`

**步骤 3.3.2**：实现 `AnalyzeAsync(string fileA, string fileB)` 方法：
- 读取两个文件内容
- 执行编码检测和截断
- 构建请求体（符合 OpenAI Chat Completion API 格式）：
  - `temperature`: 0.7
  - `max_tokens`: 2000（默认）
  - `messages`:
    - system: 配置的 System Prompt
    - user: 包含两个文件名信息的对比请求
- 发送请求，获取 AI 返回内容
- 返回分析结果

**步骤 3.3.3**：实现请求重试逻辑（最多 3 次，间隔 1 秒）

**步骤 3.3.4**：实现流式响应处理（逐步读取而非等待完整响应）

**验证方法**：
- **Mock 服务器测试**（推荐）：
  - 使用 WireMock.NET 模拟 /chat/completions 端点
  - 验证请求体格式符合 OpenAI API 规范
  - 验证流式响应解析正确
  - 模拟 401/429 错误，验证错误处理
- 真实 API 测试（可选）：
  - 使用两个简短文本文件测试，返回有效分析结果
  - 使用一个空文件测试，处理空输入
  - 验证重试逻辑：在网络不稳定时能正确重试

---

### 3.4 实现结果输出

**目标**：将分析结果追加写入 `difference.md`

**步骤 3.4.1**：创建 `ResultWriter` 类

**步骤 3.4.2**：实现 `AppendDiffReport(string fileAPath, string fileBPath, string aiResult, bool isTruncated)` 方法

**步骤 3.4.3**：确定输出目录：**第一个文件所在目录**
- `Path.GetDirectoryName(fileAPath)` 获取目录
- 如果两个文件在不同目录，以第一个文件为准

**步骤 3.4.4**：写入格式（严格遵循 PRD）：
```markdown
---
## 📅 对比报告: [File_A] vs [File_B]
> 时间: YYYY-MM-DD HH:mm:ss | 状态: [完整 / 已截断]

[AI 返回的内容]
```

**步骤 3.4.4**：处理文件不存在的场景（自动创建）

**步骤 3.4.5**：处理写入锁定异常（文件被其他程序占用）

**验证方法**：
- 在空目录调用，写入后文件存在且格式正确
- 多次调用，验证内容追加而非覆盖
- 验证时间戳格式正确（YYYY-MM-DD HH:mm:ss）
- 验证状态字段根据截断参数正确显示

---

## 阶段四：Windows 注册表集成

### 4.1 实现右键菜单注册

**目标**：将程序注册到 Windows 右键菜单

**步骤 4.1.1**：创建 `RegistryManager` 类

**步骤 4.1.2**：定义注册表路径常量：
- 主键：`HKCU\Software\Classes\*\shell\AI差异分析`
- 命令：`HKCU\Software\Classes\*\shell\AI差异分析\command`

**步骤 4.1.3**：实现 `RegisterContextMenu(string exePath)` 方法：
- 创建主键，设置显示名称为 "AI 差异分析"
- 创建 `command` 子键，设置默认值为 `"[exePath]" "%1"`
- 设置图标（可选，使用程序图标）

**步骤 4.1.4**：实现 `UnregisterContextMenu()` 方法：
- 删除主键及其所有子键

**步骤 4.1.5**：实现 `IsRegistered()` 方法：
- 检查主键是否存在，返回布尔值

**验证方法**：
- 调用注册方法，打开 regedit 检查键值正确创建
- 右键文件，查看上下文菜单是否显示 "AI 差异分析"
- 调用注销方法，验证菜单项消失
- 测试图标显示正确

---

### 4.2 实现单实例互斥锁

**目标**：处理 Windows 多文件选择时启动多个实例的问题

**步骤 4.2.1**：定义全局互斥锁名称（如 `Global\AI.DiffAssistant.SingleInstance`）

**步骤 4.2.2**：在 `Cli` 项目 Program.cs 中实现：
- 程序启动时尝试获取互斥锁
- 如果获取失败，说明已有实例运行

**步骤 4.2.3**：实现参数传递机制（**方案 B - 简化方案**）：
- 第二个实例检测到互斥锁已存在后，直接退出
- 第一个实例在启动后等待一小段时间（收集可能的第二个实例传递的参数）
- 简化处理：默认只处理第一个实例收到的参数（Windows 选中两个文件时通常会连续启动两次）

**步骤 4.2.4**：设置等待超时（建议 5 秒），超时后强制继续

**验证方法**：
- 启动一个实例，再次双击启动第二个，验证第二个能检测到第一个
- 使用命令行传递两个文件路径，验证正确处理
- 测试参数收集逻辑：Windows 可能分两次传递参数，验证能正确合并

---

## 阶段五：系统通知模块

### 5.1 实现 Toast 通知

**目标**：发送 Windows 系统通知

**步骤 5.1.1**：添加 `Microsoft.Windows.SDK.Contracts` NuGet 包（Windows API Pack）

**步骤 5.1.2**：创建 `NotificationManager` 类

**步骤 5.1.3**：实现 `ShowSuccess(string message)` 方法：
- 标题："分析完成 (已追加)"
- 内容：传入参数
- 设置 `ToastActivated` 回调

**步骤 5.1.4**：实现 `ShowError(string error)` 方法：
- 标题："分析失败"
- 内容：传入错误信息

**步骤 5.1.5**：实现通知点击处理：打开 `difference.md` 文件

**步骤 5.1.6**：注册应用为通知发送者（首次运行调用 `ToastNotificationManager.GetForCurrentUser().AuthorizationChanged`）

**验证方法**：
- 发送成功通知，验证显示正确且可点击
- 发送失败通知，验证显示错误信息
- 点击通知，验证调用系统默认程序打开文件
- 验证通知在 Windows 通知中心正确显示

---

## 阶段六：CLI 入口（静默模式）

### 6.1 实现命令行参数解析

**目标**：解析传入的文件路径参数

**步骤 6.1.1**：在 `Cli` 项目中实现 `ArgsParser` 类

**步骤 6.1.2**：解析规则：
- 接受 0 个参数：启动 GUI 模式
- 接受 1 个参数：错误，提示需要选择两个文件
- 接受 2 个参数：执行静默分析
- 接受超过 2 个参数：错误

**步骤 6.1.3**：验证文件存在性和可读性

**步骤 6.1.4**：输出使用说明（无参数或参数错误时）

**验证方法**：
- 无参数运行：启动 GUI 窗口
- 1 个参数：显示错误通知并退出
- 2 个有效文件路径：执行分析
- 2 个无效文件路径：显示错误通知
- 3 个参数：显示错误通知

---

### 6.2 实现静默执行主流程

**目标**：在后台完成完整的分析流程

**步骤 6.2.1**：在 `Cli` 项目 Program.cs 中实现主流程：
```
1. 加载配置
2. 解析命令行参数
3. 获取互斥锁
4. 收集所有文件路径（处理多实例情况）
5. 读取文件内容
6. 调用 AI 分析
7. 追加写入结果
8. 发送成功通知
```

**步骤 6.2.2**：实现错误处理：
- 文件不存在：发送错误通知，退出码 1
- AI 调用失败：发送错误通知，退出码 2
- 文件写入失败：发送错误通知，退出码 3
- 其他异常：发送通用错误通知，退出码 99

**步骤 6.2.3**：实现日志记录（可选，调试用，写入临时文件）

**验证方法**：
- 选中两个 txt 文件，右键菜单执行，验证通知正确
- 检查 `difference.md` 文件内容正确
- 测试各种错误场景，验证错误通知正确
- 测试关闭通知后的行为

---

## 阶段七：GUI 配置界面

### 7.1 创建主窗口 XAML

**目标**：实现配置中心 UI

**步骤 7.1.1**：设计 MainWindow.xaml 布局（参考 PRD 线框图）

**步骤 7.1.2**：创建以下区域：
- AI 服务设置区（Base URL、API Key、Model Name）
- 提示词设置区（多行文本框）
- 按钮区（测试连接、保存配置）
- 系统集成区（状态显示、添加/移除按钮）

**步骤 7.1.3**：设置窗口属性：
- 标题："AI 文档差异助手 - 配置中心"
- 窗口大小：约 600x500
- 不可调整大小

**步骤 7.1.4**：添加样式和排版（简洁专业风格）

**验证方法**：
- 运行程序，窗口显示正确
- 各输入框可正常输入
- 按钮可点击
- 窗口关闭和最小化正常

---

### 7.2 实现 MVVM 绑定

**目标**：实现 UI 与业务逻辑的数据绑定

**步骤 7.2.1**：创建 `MainViewModel` 类，继承 `INotifyPropertyChanged`

**步骤 7.2.2**：定义绑定属性：
- `BaseUrl`、`ModelName`、`SystemPrompt`（双向绑定）
- `ApiKey`（使用 `PasswordBox` 控件，WPF 原生支持掩码显示）
- `IsRegistered`（只读，单向绑定到 UI）
- `IsTesting`、`IsSaving`（用于按钮禁用状态）

**步骤 7.2.3**：定义命令：
- `TestConnectionCommand`
- `SaveConfigCommand`
- `RegisterCommand`
- `UnregisterCommand`

**步骤 7.2.4**：实现 `PropertyChanged` 事件触发逻辑

**步骤 7.2.5**：在 XAML 中设置 `DataContext`

**验证方法**：
- 在文本框输入，ViewModel 属性值更新
- 修改 ViewModel 属性，UI 显示更新
- 按钮点击触发对应命令
- 验证 API Key 掩码显示功能

---

### 7.3 绑定业务逻辑

**目标**：连接 UI 与 Core 层的业务逻辑

**步骤 7.3.1**：在 `MainViewModel` 中注入 `ConfigManager`、`AiService`、`RegistryManager`

**步骤 7.3.2**：实现 `TestConnectionCommand` 逻辑：
- 调用 `AiService.TestConnectionAsync`
- 根据结果显示成功/失败消息
- 使用 `MessageBox` 或自定义对话框显示结果

**步骤 7.3.3**：实现 `SaveConfigCommand` 逻辑：
- 验证输入不为空
- 保存配置到文件
- 显示保存成功提示

**步骤 7.3.4**：实现 `RegisterCommand` 和 `UnregisterCommand`：
- 调用 `RegistryManager`
- 更新 `IsRegistered` 状态
- 显示操作结果

**步骤 7.3.5**：窗口加载时读取现有配置

**验证方法**：
- 点击测试连接，验证调用正确 API
- 点击保存，验证配置文件更新
- 点击注册，验证右键菜单出现
- 重启程序，验证配置正确加载

---

## 阶段八：集成测试与构建

### 8.1 功能集成测试

**目标**：端到端测试完整功能

**步骤 8.1.1**：准备测试环境：
- 创建两个测试文本文件（内容略有不同）
- 配置有效的 API Key

**步骤 8.1.2**：执行完整流程测试：
1. 启动 GUI，配置 AI 信息
2. 保存配置
3. 注册右键菜单
4. 选中两个文件，右键选择 "AI 差异分析"
5. 等待通知
6. 检查 `difference.md` 内容

**步骤 8.1.3**：测试边界场景：
- 0 个参数启动（GUI 模式）
- 1 个参数启动
- 3 个参数启动
- 无效配置文件
- AI 服务不可用

**步骤 8.1.4**：记录测试结果和发现的问题

**验证方法**：
- 所有测试步骤按预期执行
- 无未捕获异常
- 通知正确显示
- 结果文件格式正确

---

### 8.2 性能测试

**目标**：验证冷启动 < 1 秒

**步骤 8.2.1**：编译发布版本

**步骤 8.2.2**：测量冷启动时间：
- 结束所有相关进程
- 记录开始时间
- 启动程序
- 记录窗口显示时间
- 计算差值

**步骤 8.2.3**：重复测量 5 次，取平均值

**步骤 8.2.4**：如果超过 1 秒，分析瓶颈并优化

**验证方法**：
- 平均冷启动时间 < 1000ms
- 最慢启动时间 < 1500ms

---

### 8.3 构建包生成

**目标**：生成可分发的安装包

**步骤 8.3.1**：使用 NSIS 或 Inno Setup 创建安装脚本

**步骤 8.3.2**：包含内容：
- 主程序 .exe
- 配置文件模板
- 开始菜单快捷方式
- 卸载程序

**步骤 8.3.3**：测试安装过程：
- 运行安装程序
- 选择安装目录
- 完成安装
- 验证程序可启动
- 测试卸载功能

**验证方法**：
- 安装过程无错误
- 安装后程序功能正常
- 卸载后无残留文件（除用户配置）

---

## 附录：测试检查清单

### 单元测试
- [ ] 配置模型序列化/反序列化
- [ ] 编码检测准确性
- [ ] 文件截断逻辑
- [ ] AI 请求构建正确性
- [ ] 结果格式生成

### 集成测试
- [ ] GUI 完整流程
- [ ] CLI 静默模式
- [ ] 右键菜单注册/注销
- [ ] 通知显示与点击
- [ ] 多实例处理

### 端到端测试
- [ ] 真实 AI API 调用
- [ ] 真实文件对比
- [ ] 性能测试
- [ ] 安装包测试

---

> 计划版本：V1.0
> 生成日期：2025-12-28
