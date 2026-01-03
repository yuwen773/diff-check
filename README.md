# AI 文档差异助手 (diff-check)

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

一款原生集成于 Windows 资源管理器的轻量级 AI 文档差异对比工具。通过 AI 能力提供语义级的差异总结，最大化提升办公效率。

## 功能特性

### 核心功能
- **右键集成**: 选中两个文件，右键一键启动 AI 差异分析
- **多格式支持**: 纯文本 (.txt, .md, .cs, .js, .py, .json 等) + 富文本 (.docx, .pdf)
- **智能截断**: 自动处理大文件，保留头部 15,000 字符
- **多编码支持**: 自动检测 UTF-8、GBK、ASCII 编码
- **结果追溯**: 自动追加对比结果到 `difference.md`
- **系统通知**: 任务完成后通过 Windows Toast 通知反馈

### 界面功能
- **系统托盘**: 支持最小化到托盘、双击显示/隐藏窗口、右键菜单管理
- **明暗主题**: 一键切换亮色/暗黑主题
- **版本下载**: 在设置中查看 GitHub 稳定版发布列表并跳转下载
- **关于页面**: 显示版本号、作者、官网、版权、更新渠道

### 配置功能
- **AI 服务配置**: 支持 OpenAI 兼容接口及本地 Ollama 服务（还未支持，后续版本将会支持）
- **连接测试**: 内置 API 连接测试功能
- **自定义提示词**: 可自定义 AI 提示词模板
- **日志配置**: 支持日志启用/禁用及日志级别配置

## 技术栈

| 层级 | 技术 |
|------|------|
| 运行时 | .NET 10.0 Windows (AOT 编译) |
| GUI 框架 | WPF (MVVM 模式) + WinForms (托盘支持) |
| HTTP 客户端 | System.Net.Http.HttpClient |
| JSON 处理 | System.Text.Json |
| 测试框架 | xUnit + WireMock.Net |
| 富文本解析 | DocumentFormat.OpenXml (.docx) + PdfPig (.pdf) |
| 通知 | Microsoft.Toolkit.Uwp.Notifications |

## 项目结构

```
src/
├── AI.DiffAssistant.Core/       # 核心业务逻辑
│   ├── Config/                  # 配置管理
│   ├── Diff/                    # 差异分析
│   ├── File/                    # 文件处理
│   ├── Parser/                  # 文件解析器
│   ├── Registry/                # 注册表操作
│   ├── Notification/            # 系统通知
│   └── Release/                 # 版本下载
├── AI.DiffAssistant.GUI/        # WPF 界面
│   ├── Views/                   # 视图
│   ├── ViewModels/              # MVVM ViewModels
│   ├── Converters/              # 值转换器
│   ├── Themes/                  # 主题资源
│   └── Assets/                  # 品牌资源
├── AI.DiffAssistant.Shared/     # 共享模型
├── AI.DiffAssistant.Cli/        # CLI 入口 (静默模式)
└── AI.DiffAssistant.Tests/      # 单元/集成测试
```

## 快速开始

### 环境要求

- **IDE**: Visual Studio 2022
- **SDK**: .NET 10.0 Windows Desktop SDK
- **系统**: Windows 10 Version 1809 (Build 17763) 或更高版本

### 构建命令

```bash
# 克隆项目
git clone https://github.com/yuwen773/diff-check.git
cd diff-check

# 构建 Release 版本
dotnet build -c Release

# AOT 编译为独立 exe
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

构建完成后，可执行文件位于 `publish/` 目录：
- `diff-check.exe` - GUI 配置中心
- `diff-check-cli.exe` - CLI 静默模式（右键菜单调用）

### 使用方法

#### 1. 配置与集成

1. 运行 `diff-check.exe` 打开配置中心
2. 填写 AI 服务信息（Base URL、API Key、Model Name）
3. 自定义 System Prompt（可选）
4. 点击「测试连接」验证配置
5. 点击「添加到右键菜单」完成系统集成

#### 2. 使用场景

1. 在 Windows 资源管理器中按住 `Ctrl` 选中两个文件
2. 右键选择「AI 差异分析」
3. 软件在后台自动完成对比
4. 结果自动追加到文件目录下的 `difference.md`
5. 点击系统通知可快速打开结果文件

### 配置说明

配置文件位于 `%APPDATA%\diff-check\config.json`：

```json
{
  "api": {
    "baseUrl": "https://api.openai.com/v1",
    "apiKey": "your-api-key",
    "model": "gpt-4o"
  },
  "prompts": {
    "system": "你是一个文档对比助手，请对比两份文档，忽略格式差异，重点总结语义上的变化，并用 Markdown 列表输出。"
  },
  "settings": {
    "maxTokenLimit": 15000
  },
  "logging": {
    "enabled": true,
    "logPath": "diff-check.log",
    "level": "Error,Warning"
  }
}
```

## 支持的文件格式

| 类型 | 格式 |
|------|------|
| 纯文本 | .txt, .md, .cs, .js, .py, .json, .xml, .html, .css 等 |
| 富文本 | .docx, .pdf |

## 测试

```bash
# 运行所有测试
dotnet test src/AI.DiffAssistant.Tests

# 运行并显示详细输出
dotnet test src/AI.DiffAssistant.Tests -v normal
```

当前测试覆盖：配置模型、文件处理、编码检测、参数解析、注册表操作、通知管理、富文本解析等。

## 性能要求

- 冷启动 < 1 秒
- 单文件超过 15,000 字符时自动截断（保留头部）
- 后台运行时内存占用低

## 架构设计

详细架构设计请参考 [memory-bank/architecture.md](memory-bank/architecture.md)。

## License

MIT License - 详见 [LICENSE](LICENSE) 文件。

## 作者

- 作者: yuwen773
- 项目: https://github.com/yuwen773/diff-check