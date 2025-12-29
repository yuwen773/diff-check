# 系统架构文档

> AI Document Difference Assistant 架构设计说明

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
| `App.xaml` | WPF 应用入口资源字典 |
| `MainWindow.xaml` | 配置中心主窗口 |

**核心类**:
| 类名 | 作用 |
|------|------|
| `MainViewModel` | 主窗口 ViewModel，实现 INotifyPropertyChanged，包含配置管理、连接测试、右键菜单注册、主题切换等业务逻辑 |
| `MainWindow` | 主窗口视图，处理 UI 事件、密码显示切换、主题切换 |
| `BoolToStatusConverter` | 布尔值到注册状态文本的转换器 |
| `BoolToColorConverter` | 布尔值到颜色的转换器（已集成/未集成状态颜色） |
| `RelayCommand` | 简单的 ICommand 实现 |

**主题系统**:
| 文件 | 作用 |
|------|------|
| `Themes/DarkTheme.xaml` | 深色主题资源字典（背景 #1E1E1E，强调色 #0078D4） |
| `Themes/LightTheme.xaml` | 浅色主题资源字典（背景 #FFFFFF） |
| `App.xaml.cs` | 主题管理器（IsDarkTheme、ToggleTheme、ApplyTheme） |

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
| 单元测试 | xUnit | 测试配置模型、编码检测、截断逻辑 |
| Mock 测试 | WireMock.Net | 模拟 OpenAI API 响应 |

**覆盖范围**:
- 配置序列化/反序列化
- 编码检测准确性
- 文件截断逻辑
- AI 请求构建正确性
- 结果格式生成
- GUI 完整流程
- CLI 静默模式

**依赖**: Core、GUI、Cli、Shared、WireMock.Net

---

## 3. 技术栈

| 层级 | 技术 |
|------|------|
| 运行时 | .NET 10.0 Windows |
| GUI | WPF |
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

**路径**: `%APPDATA%\AI.DiffAssistant\config.json`

**格式**:
```json
{
  "api": { "baseUrl": "...", "apiKey": "...", "model": "..." },
  "prompts": { "system": "..." },
  "settings": { "maxTokenLimit": 15000 }
}
```

---

## 6. Windows 集成

### 右键菜单注册表路径
```
HKCU\Software\Classes\*\shell\AI差异分析\command
```

### 命令格式
```
"[exePath]" "%1"
```

### 单实例互斥锁
```
Global\AI.DiffAssistant.SingleInstance
```

---
