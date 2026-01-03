# 产品需求文档：diff-check - V1.5

> **版本核心目标**: **UI 重构与交互升级**。
>
> - **视觉**: 抛弃原生 WPF 的灰色方块风格，采用现代 Windows 11 Fluent Design（圆角、阴影、微质感）。
> - **交互**: 引入“仪表盘”概念，可视化后台任务历史；优化配置页面的校验与反馈体验。

## 1. 视觉风格定义 (Visual Style Guide)

- **设计语言**: **Fluent Design** (Windows 11 风格)。

- **关键元素**:
  - **圆角 (Corner Radius)**: 窗口和按钮采用 4px - 8px 圆角。
  - **字体**: `Segoe UI Variable` (Win11) 或 `Segoe UI` (Win10)。

## 2. 新增用户故事 (User Stories)

### 模块 A: 主界面重构 (Main Window Overhaul)

我们不再使用单一的“设置窗口”，而是将其重构为**“侧边栏 + 内容区”**的现代布局。

#### **US-09: 现代导航主界面**

- **价值**: 提供清晰的功能分区，扩展性更强，视觉更专业。
- **业务规则**:
  1.  **布局结构**:
      - **左侧导航栏 (Sidebar)**: 包含 Logo、以及多个 Tab 按钮 —— `🏠 配置` 和 `⚙️ 设置 ` 和`📁 历史版本`和`📄 日志 `。
      - **右侧内容区**: 根据左侧选择动态切换。
  2.  **窗口样式**:
      - 移除标准 Windows 标题栏，使用**自定义标题栏**（将内容延伸到顶部，右上角保留最小化/关闭按钮）。
      - 窗口添加微阴影 (Drop Shadow)。
- **ASCII 线框图 (New Layout)**:

  ```text
  +---------------------------------------------------------------+
  |  Diff Check                                         [ _ ] [ X ] | <-- 自定义标题栏
  +-----------+---------------------------------------------------+
  | [图标]    |                                                   |
  |           |  [ 🏠 系统配置 ]                                  |
  | 🏠 配置   |                                                   |
  |           |   Base URL：                                      |
  | ⚙️ 设置   |  ---------------------------------------------    |
  |           |
  | 📁 历史版本 | API Key:                                  👀    |
  |           |  ---------------------------------------------    |
  | 📄 日志   |  Model :
  |           |  ---------------------------------------------    |
  |           |                                                   |
  |           |  提示词设置 (System Prompt)                        |
  |           |                                                   |
  |           |  |  文本域                                  |      |
  |           |  |                                          |     |
  |           |  |                                          |     |
  |           |                                                   |
  +-----------+---------------------------------------------------+
  ```

#### **US-10: 异步加载与防卡顿 (Async UI)**

- **价值**: 彻底消除 UI “假死”现象。
- **技术要求**:
  - **测试连接** 和 **保存配置** 必须在后台线程执行。
  - 执行期间，主界面按钮置为 Disable（灰色），并显示加载动画 (Progress Ring)。

---

## 3. 技术实现方案 (WPF UI Library)

为了快速实现 V1.5 的现代外观，**强烈建议引入 UI 库**，而不是手写 XAML 样式。

### 3.1 推荐库：WPF UI (或者 HandyControl)

- **库名**: `WPF-UI` (NuGet: `WPF-UI`)
- **理由**:
  - 专为 Windows 11 风格设计。
  - 提供了现成的 `Card`, `Button`, `Navigation`, `SymbolIcon` 等控件。
  - 支持 MVVM，代码结构更清晰。
  - 体积适中，打包后增加约 2-3MB。

### 3.2 关键代码变更示例

**1. 引入样式 (App.xaml)**:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
             <!-- 引入 WPF UI 的主题 -->
            <ui:ThemeResources />
            <ui:XamlControlsResources />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

**2. 使用现代窗口 (MainWindow.xaml)**:

```xml
<!-- 使用 UI 库的 Window，自带圆角和 Mica 效果 -->
<ui:FluentWindow x:Class="AIDiff.MainWindow"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="AI Diff Assistant" Height="500" Width="700"
        WindowStartupLocation="CenterScreen">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto" /> <!-- 导航栏 -->
            <ColumnDefinition Width="*" />    <!-- 内容区 -->
        </Grid.ColumnDefinitions>

        <!-- 导航控件 -->
        <ui:NavigationStore Grid.Column="0">
            <ui:NavigationItem Icon="Home24" TargetPageType="{x:Type pages:DashboardPage}" Content="概览" />
            <ui:NavigationItem Icon="Settings24" TargetPageType="{x:Type pages:SettingsPage}" Content="设置" />
        </ui:NavigationStore>

        <!-- 页面容器 -->
        <Frame Grid.Column="1" NavigationUIVisibility="Hidden" />
    </Grid>
</ui:FluentWindow>
```

---

## 4. 开发优先级 (Roadmap)

1.  **Step 1 (基础 UI)**: 引入 `WPF-UI` NuGet 包，将现有的 MainWindow 替换为 `FluentWindow`，应用基础样式。
2.  **Step 2 (交互优化)**: 在设置页实现“密码显隐”、“无弹窗保存”、“输入校验”。

---
