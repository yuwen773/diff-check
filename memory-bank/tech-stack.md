# 技术栈推荐方案

## 推荐方案：C# + .NET 10 + WPF

### 核心技术选型

| 层级 | 技术选型 | 选择理由 |
|------|----------|----------|
| **运行时** | .NET 10 Windows Desktop | 原生Windows支持，AOT编译可生成独立exe，冷启动<1秒 |
| **GUI框架** | WPF | 现代化UI，XAML声明式UI，数据绑定完善，生态成熟 |
| **HTTP客户端** | System.Net.Http.HttpClient | .NET内置，支持异步，高性能 |
| **JSON处理** | System.Text.Json | .NET内置，性能优秀 |
| **配置存储** | JSON文件 | 轻量，无需额外数据库 |
| **安装打包** | NSIS / Inno Setup | Windows成熟打包工具，生成单文件安装包 |

### 推荐的 NuGet 包

```
# 仅需少量依赖，保持轻量
- 无需额外HTTP库（System.Net.Http已内置）
- 无需额外JSON库（System.Text.Json已内置）
- 可选：Polly（重试/熔断，如需要）
```

### 项目结构建议

```
src/
├── AI.DiffAssistant.Core/       # 核心业务逻辑
│   ├── Config/                  # 配置管理
│   ├── Diff/                    # 差异分析逻辑
│   ├── File/                    # 文件处理（编码检测、截断）
│   └── Registry/                # 注册表操作
├── AI.DiffAssistant.GUI/        # WPF界面
│   ├── Views/                   # 视图
│   ├── ViewModels/              # MVVM ViewModels
│   └── Converters/              # 值转换器
├── AI.DiffAssistant.Shared/     # 共享模型
└── AI.DiffAssistant.Cli/        # CLI入口（静默模式）
```

### 技术优势

1. **原生Windows体验** - 与系统深度集成，注册表、系统通知、文件资源管理器无缝对接
2. **轻量级** - .NET 10 AOT编译后exe约20-50MB
3. **快速启动** - Native AOT编译，冷启动可控制在500ms内
4. **开发效率** - C#语言现代化，WPF + MVVM模式成熟稳定
5. **部署简单** - 单exe文件，无需用户安装.NET运行时
6. **维护成本低** - 微软官方长期支持，文档完善

### 备选方案对比

| 方案 | 优点 | 缺点 |
|------|------|------|
| **C# + .NET 10 + WPF**（推荐） | 原生Windows支持，启动快，生态完善 | 需要Visual Studio开发环境 |
| Python + PyQt/Tkinter | 开发快，AI生态丰富 | 打包大（100MB+），启动慢（约2-3秒） |
| C# + WinForms | 更轻量，兼容性好 | UI过时，布局灵活性差 |
| Electron | 跨平台，UI美观 | 体积极大（100MB+），启动慢，不符合"轻量"定位 |

### 开发环境要求

- IDE: Visual Studio 2022 ( Community版免费 )
- SDK: .NET 10.0 Windows Desktop
- 语言: C# 14

### 构建命令

```bash
# AOT编译为独立exe
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

---
*生成时间: 2025-12-28*
