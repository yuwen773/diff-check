# AI 文档差异助手 (AI Diff Assistant)

一款原生集成于 Windows 资源管理器的轻量级效率工具，通过 AI 能力提供语义级的文档差异对比。

## 功能特性

- **右键集成**: 选中两个文件，右键一键启动分析
- **智能截断**: 自动处理大文件，保留头部内容
- **多编码支持**: 自动检测 UTF-8、GBK、ASCII 编码
- **结果追溯**: 自动追加对比结果到 `difference.md`
- **系统通知**: 任务完成后通过 Toast 通知反馈
- **灵活配置**: 支持 OpenAI 兼容接口及本地 Ollama 服务

## 技术栈

| 层级 | 技术 |
|------|------|
| 运行时 | .NET 8 Windows Desktop (AOT 编译) |
| GUI 框架 | WPF (MVVM 模式) |
| HTTP 客户端 | System.Net.Http.HttpClient |
| JSON 处理 | System.Text.Json |

## 项目结构

```
src/
├── AI.DiffAssistant.Core/       # 核心业务逻辑
├── AI.DiffAssistant.GUI/        # WPF 界面 (Views, ViewModels)
├── AI.DiffAssistant.Shared/     # 共享模型
└── AI.DiffAssistant.Cli/        # CLI 入口 (静默模式)
```

## 构建要求

- **IDE**: Visual Studio 2022
- **SDK**: .NET 8.0 Windows Desktop SDK
- **语言**: C# 12

## 构建命令

```bash
# AOT 编译为独立 exe
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

构建完成后，exe 文件位于 `publish/` 目录。

## 使用方法

### 1. 配置与集成

1. 运行配置中心，填写 AI 服务信息（Base URL、API Key、Model Name）
2. 自定义 System Prompt（可选）
3. 点击「测试连接」验证配置
4. 点击「添加到右键菜单」完成系统集成

### 2. 使用场景

1. 在 Windows 资源管理器中按住 `Ctrl` 选中两个文件
2. 右键选择「AI 差异分析」
3. 软件在后台自动完成对比
4. 结果自动追加到文件目录下的 `difference.md`
5. 点击系统通知可快速打开结果文件

## 配置说明

配置文件 `config.json` 格式：

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
  }
}
```

## 支持的文件格式

仅支持文本文件：`.txt`、`.md`、`.cs`、`.js`、`.py`、`.json` 等

## 性能要求

- 冷启动 < 1 秒
- 单文件超过 15,000 字符时自动截断（保留头部）
