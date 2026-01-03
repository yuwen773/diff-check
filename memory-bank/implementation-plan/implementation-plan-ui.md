# UI 重构与交互升级 - V1.5 实施计划

> **版本核心目标**: UI 重构与交互升级
>
> - **视觉**: 抛弃原生 WPF 的灰色方块风格，采用现代 Windows 11 Fluent Design（圆角、阴影、微质感）
> - **交互**: 引入侧边栏导航，优化配置页面校验与反馈体验
>
> **基于**: `memory-bank/feature/feature4.0.md`
> **技术栈**: C# + .NET 10 + WPF + WPF-UI 库
>
> **侧边栏 Tab 定义**:
> | Tab | 名称 | 内容 |
> |-----|------|------|
> | 🏠 配置 | AI 服务配置 | Base URL、API Key、Model、System Prompt |
> | ⚙️ 设置 | 系统集成 | 右键菜单注册/注销 |
> | 📁 历史版本 | 版本下载 | 可用稳定版列表与下载（复用 V3.0 功能） |
> | 📄 日志 | 日志设置 | 是否启用日志、日志级别选择 |
> | ℹ️ 关于 | 关于页面 | 版本号、作者、官网、版权、更新渠道 |

---

## 阶段一：环境准备与依赖引入

### 步骤 1：评估现有项目结构

**指令：**
1. 打开 `src/AI.DiffAssistant.GUI/AI.DiffAssistant.GUI.csproj` 文件
2. 确认当前 TargetFramework 版本（应为 `net10.0-windows`）
3. 记录当前 GUI 项目的所有依赖项

**验证：**
- 输出当前 csproj 文件的依赖项列表（不含版本号）
- 确认项目能够正常编译：`dotnet build src/AI.DiffAssistant.GUI/AI.DiffAssistant.GUI.csproj`

---

### 步骤 2：引入 WPF-UI NuGet 包

**指令：**
1. 在 `AI.DiffAssistant.GUI.csproj` 中添加以下包引用：
   ```xml
   <PackageReference Include="WPF-UI" Version="3.0.0" />
   ```
2. 执行 `dotnet restore` 还原依赖

**验证：**
- `dotnet build` 无编译错误
- 在 `obj/project.assets.json` 中确认 `WPF-UI` 包已正确还原

---

### 步骤 3：备份现有 GUI 代码

**指令：**
1. 创建备份目录 `backup/gui-v1.0/`
2. 复制以下文件到备份目录：
   - `src/AI.DiffAssistant.GUI/MainWindow.xaml`
   - `src/AI.DiffAssistant.GUI/MainWindow.xaml.cs`
   - `src/AI.DiffAssistant.GUI/App.xaml`
   - `src/AI.DiffAssistant.GUI/App.xaml.cs`
   - `src/AI.DiffAssistant.GUI/Views/AboutWindow.xaml`
   - `src/AI.DiffAssistant.GUI/Views/AboutWindow.xaml.cs`
   - `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs`
   - `src/AI.DiffAssistant.GUI/Themes/DarkTheme.xaml`
   - `src/AI.DiffAssistant.GUI/Themes/LightTheme.xaml`

**验证：**
- 确认备份目录存在且包含所有 9 个文件
- 每个文件大小 > 0 字节

---

## 阶段二：应用基础样式

### 步骤 4：更新 App.xaml 资源字典

**指令：**
1. 打开 `src/AI.DiffAssistant.GUI/App.xaml`
2. 用以下内容替换整个 `Application.Resources` 内容：

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- 引入 WPF-UI 的主题资源 -->
            <ui:ThemeResources xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml" />
            <ui:XamlControlsResources xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

**验证：**
- `dotnet build` 无 XAML 解析错误
- 应用启动时无资源加载异常

---

### 步骤 5：替换 MainWindow 为 FluentWindow

**指令：**
1. 打开 `src/AI.DiffAssistant.GUI/MainWindow.xaml`
2. 将根元素从 `Window` 替换为 `ui:FluentWindow`
3. 添加 xmlns 声明：`xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`
4. 将 `WindowState="Maximized"` 改为 `WindowStartupLocation="CenterScreen"`
5. 暂时移除所有内部内容，只保留最小框架

**验证：**
- 应用能够启动并显示空窗口
- 窗口具有圆角边框效果（目视检查）

---

### 步骤 6：配置主题跟随系统并支持切换

**指令：**
1. 修改 `App.xaml.cs` 中的主题初始化代码：
   ```csharp
   // 默认跟随系统
   Wpf.Ui.Appearance.ApplicationThemeManager.GetSystemTheme();
   ```
2. 添加主题切换方法：
   ```csharp
   public static void SetTheme(Wpf.Ui.Appearance.ApplicationTheme theme)
   {
       Wpf.Ui.Appearance.ApplicationThemeManager.Apply(theme, Wpf.Ui.Appearance.BackgroundType.Mica);
       _isDarkTheme = (theme == Wpf.Ui.Appearance.ApplicationTheme.Dark);
   }
   ```

**验证：**
- 应用启动时主题跟随操作系统设置
- 切换系统主题后重新启动应用，应用主题同步变化

---

## 阶段三：实现侧边栏导航布局

### 步骤 7：创建页面目录结构

**指令：**
1. 在 `src/AI.DiffAssistant.GUI/` 下创建目录 `Views/Pages/`
2. 创建以下空页面文件：
   - `Views/Pages/ConfigPage.xaml` + `ConfigPage.xaml.cs`（AI 配置）
   - `Views/Pages/SettingsPage.xaml` + `SettingsPage.xaml.cs`（系统集成）
   - `Views/Pages/VersionsPage.xaml` + `VersionsPage.xaml.cs`（版本下载，复用 V3.0）
   - `Views/Pages/LogsPage.xaml` + `LogsPage.xaml.cs`（日志设置）
   - `Views/Pages/AboutPage.xaml` + `AboutPage.xaml.cs`（关于页面）

**验证：**
- 5 个目录存在
- 10 个文件已创建（每个页面 2 个文件）

---

### 步骤 8：实现主窗口布局骨架

**指令：**
修改 `MainWindow.xaml`，用以下布局替换内容：

```xml
<ui:FluentWindow x:Class="AI.DiffAssistant.GUI.MainWindow"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        xmlns:pages="clr-namespace:AI.DiffAssistant.GUI.Views.Pages"
        Title="diff-check" Height="650" Width="950"
        WindowStartupLocation="CenterScreen">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto" MinWidth="200" MaxWidth="250"/> <!-- 侧边栏 -->
            <ColumnDefinition Width="*" />                              <!-- 内容区 -->
        </Grid.ColumnDefinitions>

        <!-- 左侧导航栏 -->
        <Border Grid.Column="0" Background="{ui:ThemeResource ApplicationBackgroundBrush}">
            <StackPanel Margin="0,20,0,0">
                <!-- Logo + 应用名称 -->
                <Image Source="/Assets/diff-check.png" Width="48" Height="48" HorizontalAlignment="Center" Margin="0,0,0,8"/>
                <TextBlock Text="diff-check" HorizontalAlignment="Center" FontSize="16" FontWeight="Bold" Margin="0,0,0,20"/>

                <!-- 导航按钮 -->
                <ui:NavigationStore x:Name="NavigationStore" Margin="0,10">
                    <ui:NavigationItem Content="配置" Icon="Settings24" TargetPageType="{x:Type pages:ConfigPage}"/>
                    <ui:NavigationItem Content="设置" Icon="Shield24" TargetPageType="{x:Type pages:SettingsPage}"/>
                    <ui:NavigationItem Content="历史版本" Icon="CloudDownload24" TargetPageType="{x:Type pages:VersionsPage}"/>
                    <ui:NavigationItem Content="日志" Icon="List24" TargetPageType="{x:Type pages:LogsPage}"/>
                    <ui:NavigationItem Content="关于" Icon="Info24" TargetPageType="{x:Type pages:AboutPage}"/>
                </ui:NavigationStore>
            </StackPanel>
        </Border>

        <!-- 右侧内容区 -->
        <Frame Grid.Column="1" NavigationUIVisibility="Hidden" x:Name="RootFrame"/>
    </Grid>
</ui:FluentWindow>
```

**验证：**
- XAML 解析无错误
- 编译成功
- 窗口显示侧边栏和内容区框架
- 5 个导航项全部显示

---

### 步骤 9：实现 ConfigPage（AI 配置页）

**指令：**
在 `ConfigPage.xaml` 中创建 AI 服务配置表单：

```xml
<ui:Page x:Class="AI.DiffAssistant.GUI.Views.Pages.ConfigPage"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="AI 配置">
    <ScrollViewer Padding="20">
        <StackPanel>
            <!-- API 配置卡片 -->
            <ui:Card Header="API 配置" Margin="0,0,0,20">
                <StackPanel>
                    <TextBox x:Name="BaseUrlTextBox" Header="Base URL"
                             PlaceholderText="https://api.openai.com/v1"/>
                    <Grid Margin="0,10,0,0">
                        <PasswordBox x:Name="ApiKeyPasswordBox" Header="API Key"
                                     PlaceholderText="sk-..."/>
                        <ui:Button Content="👀" HorizontalAlignment="Right" VerticalAlignment="Bottom"
                                   Margin="0,0,5,5" Width="30" Height="30"
                                   Click="TogglePasswordVisibility_Click"/>
                    </Grid>
                    <TextBox x:Name="ModelTextBox" Header="Model"
                             PlaceholderText="gpt-4o" Margin="0,10,0,0"/>
                </StackPanel>
            </ui:Card>

            <!-- 提示词配置卡片 -->
            <ui:Card Header="提示词设置" Margin="0,0,0,20">
                <StackPanel>
                    <TextBox x:Name="SystemPromptTextBox"
                             Header="System Prompt"
                             AcceptsReturn="True"
                             MinHeight="200"
                             PlaceholderText="你是一个文档对比助手，请对比两份文档..."/>
                </StackPanel>
            </ui:Card>

            <!-- 状态反馈区域 -->
            <Border x:Name="StatusBanner" CornerRadius="8" Padding="15"
                    Margin="0,20,0,0" Visibility="Collapsed">
                <StackPanel Orientation="Horizontal">
                    <ui:SymbolIcon x:Name="StatusIcon" Margin="0,0,10,0"/>
                    <TextBlock x:Name="StatusText" VerticalAlignment="Center"/>
                </StackPanel>
            </Border>

            <!-- 操作按钮 -->
            <WrapPanel HorizontalAlignment="Right" Margin="0,20,0,0">
                <Grid>
                    <ui:Button Content="测试连接" Icon="PlugConnected24"
                               Margin="0,0,10,0" x:Name="TestConnectionButton"
                               IsEnabled="{Binding IsNotLoading}"/>
                    <ui:ProgressRing x:Name="TestProgress" Width="24" Height="24"
                                     Visibility="Collapsed"/>
                </Grid>
                <Grid>
                    <ui:Button Content="保存配置" Icon="Save24"
                               Margin="0,0,10,0" x:Name="SaveButton"
                               IsEnabled="{Binding IsNotLoading}"/>
                    <ui:ProgressRing x:Name="SaveProgress" Width="24" Height="24"
                                     Visibility="Collapsed"/>
                </Grid>
            </WrapPanel>
        </StackPanel>
    </ScrollViewer>
</ui:Page>
```

**验证：**
- 页面正常显示 API 配置表单
- 密码框旁边有眼睛按钮
- 测试连接和保存按钮可见
- 编译成功

---

### 步骤 10：实现 SettingsPage（系统集成页）

**指令：**
在 `SettingsPage.xaml` 中创建系统集成配置：

```xml
<ui:Page x:Class="AI.DiffAssistant.GUI.Views.Pages.SettingsPage"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="系统设置">
    <ScrollViewer Padding="20">
        <StackPanel>
            <!-- 右键菜单配置卡片 -->
            <ui:Card Header="右键菜单集成" Margin="0,0,0,20">
                <StackPanel>
                    <TextBlock Text="将 diff-check 注册到 Windows 右键菜单，选中两个文件后进行分析。"
                               Foreground="{ui:ThemeResource TextFillColorSecondaryBrush}"
                               TextWrapping="Wrap" Margin="0,0,0,15"/>

                    <!-- 状态显示 -->
                    <WrapPanel VerticalAlignment="Center" Margin="0,0,0,15">
                        <ui:SymbolIcon x:Name="RegisterStatusIcon" Margin="0,0,10,0"/>
                        <TextBlock x:Name="RegisterStatusText" VerticalAlignment="Center"
                                   Text="{Binding IsRegistered, Converter={StaticResource BoolToStatusConverter}}"/>
                    </WrapPanel>

                    <!-- 操作按钮 -->
                    <WrapPanel>
                        <ui:Button Content="添加到右键菜单" Icon="Add24"
                                   Margin="0,0,10,0" x:Name="RegisterButton"
                                   Command="{Binding RegisterCommand}"/>
                        <ui:Button Content="从右键移除" Icon="Delete24"
                                   Margin="0,0,10,0" x:Name="UnregisterButton"
                                   Command="{Binding UnregisterCommand}"/>
                    </WrapPanel>
                </StackPanel>
            </ui:Card>

            <!-- 主题配置卡片 -->
            <ui:Card Header="外观设置" Margin="0,0,0,20">
                <StackPanel>
                    <TextBlock Text="主题模式" FontWeight="Bold" Margin="0,0,0,10"/>
                    <WrapPanel>
                        <ui:RadioButton Content="跟随系统" x:Name="ThemeSystemRadio"
                                        IsChecked="True" GroupName="Theme"
                                        Checked="ThemeRadio_Checked"/>
                        <ui:RadioButton Content="浅色" x:Name="ThemeLightRadio"
                                        GroupName="Theme" Margin="10,0,0,0"
                                        Checked="ThemeRadio_Checked"/>
                        <ui:RadioButton Content="深色" x:Name="ThemeDarkRadio"
                                        GroupName="Theme" Margin="10,0,0,0"
                                        Checked="ThemeRadio_Checked"/>
                    </WrapPanel>
                </StackPanel>
            </ui:Card>

            <!-- 托盘配置卡片 -->
            <ui:Card Header="系统托盘" Margin="0,0,0,20">
                <StackPanel>
                    <TextBlock Text="启动后最小化到系统托盘" VerticalAlignment="Center"/>
                    <ui:CheckBox Content="启动时隐藏主窗口" x:Name="StartMinimizedCheckBox"
                                 IsChecked="{Binding StartMinimized}"/>
                </StackPanel>
            </ui:Card>
        </StackPanel>
    </ScrollViewer>
</ui:Page>
```

**验证：**
- 页面正常显示右键菜单状态
- 注册/注销按钮功能正常
- 主题切换选项可用
- 编译成功

---

### 步骤 11：实现 VersionsPage（版本下载页）

**指令：**
1. 复用 V3.0 已实现的 `MainWindow.xaml` 中的"版本下载"Tab 内容
2. 将其迁移到 `VersionsPage.xaml`
3. 确保以下功能正常：
   - 调用 `ReleaseService` 获取稳定版列表
   - 显示版本号、发布日期、平台、更新说明
   - 提供下载按钮跳转下载页

**验证：**
- 版本列表正确显示
- 刷新按钮可用
- 下载链接可点击跳转

---

### 步骤 12：实现 LogsPage（日志设置页）

**指令：**
在 `LogsPage.xaml` 中创建日志配置：

```xml
<ui:Page x:Class="AI.DiffAssistant.GUI.Views.Pages.LogsPage"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="日志设置">
    <ScrollViewer Padding="20">
        <StackPanel>
            <!-- 日志启用配置 -->
            <ui:Card Header="日志记录" Margin="0,0,0,20">
                <StackPanel>
                    <ui:CheckBox Content="启用日志记录" x:Name="EnableLoggingCheckBox"
                                 IsChecked="{Binding IsLoggingEnabled}"/>
                    <TextBlock Text="日志文件位置: %TEMP%\diff-check.log"
                               Foreground="{ui:ThemeResource TextFillColorSecondaryBrush}"
                               Margin="0,10,0,0"/>
                </StackPanel>
            </ui:Card>

            <!-- 日志级别配置 -->
            <ui:Card Header="日志级别" Margin="0,0,0,20">
                <StackPanel>
                    <TextBlock Text="选择日志记录级别（低级别包含高级别）"
                               Foreground="{ui:ThemeResource TextFillColorSecondaryBrush}"
                               Margin="0,0,0,10"/>

                    <ui:ComboBox x:Name="LogLevelComboBox" SelectedIndex="2">
                        <ui:ComboBoxItem Content="Error - 仅错误"/>
                        <ui:ComboBoxItem Content="Warning - 警告和错误"/>
                        <ui:ComboBoxItem Content="Info - 所有信息（默认）"/>
                        <ui:ComboBoxItem Content="Debug - 调试信息"/>
                    </ui:ComboBox>
                </StackPanel>
            </ui:Card>

            <!-- 查看日志按钮 -->
            <ui:Button Content="打开日志文件" Icon="Document24"
                       HorizontalAlignment="Left" x:Name="OpenLogButton"
                       Click="OpenLogButton_Click"/>
        </StackPanel>
    </ScrollViewer>
</ui:Page>
```

**验证：**
- 日志启用开关可切换
- 日志级别下拉框可用
- 打开日志按钮可正常打开文件

---

### 步骤 13：实现 AboutPage（关于页面）

**指令：**
在 `AboutPage.xaml` 中创建关于页面（Fluent Design 风格）：

```xml
<ui:Page x:Class="AI.DiffAssistant.GUI.Views.Pages.AboutPage"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="关于">
    <Grid Margin="20">
        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" MaxWidth="500">
            <!-- Logo -->
            <Image Source="/Assets/diff-check.png" Width="96" Height="96"
                   HorizontalAlignment="Center" Margin="0,0,0,20"/>

            <!-- 应用名称和版本 -->
            <TextBlock Text="diff-check" FontSize="28" FontWeight="Bold"
                       HorizontalAlignment="Center" Margin="0,0,0,10"/>
            <TextBlock x:Name="VersionText" FontSize="14"
                       Foreground="{ui:ThemeResource TextFillColorSecondaryBrush}"
                       HorizontalAlignment="Center" Margin="0,0,0,30"/>

            <!-- 信息卡片 -->
            <ui:Card Margin="0,0,0,20">
                <StackPanel>
                    <Grid Margin="0,0,0,10">
                        <TextBlock Text="作者" FontWeight="Bold" VerticalAlignment="Center"/>
                        <TextBlock Text="Your Name" HorizontalAlignment="Right"
                                   VerticalAlignment="Center"/>
                    </Grid>
                    <ui:Separator Margin="0,5,0,10"/>

                    <Grid Margin="0,0,0,10">
                        <TextBlock Text="官网" FontWeight="Bold" VerticalAlignment="Center"/>
                        <ui:HyperlinkButton Content="https://github.com/yourname/diff-check"
                                            HorizontalAlignment="Right"
                                            NavigateUri="https://github.com/yourname/diff-check"/>
                    </Grid>
                    <ui:Separator Margin="0,5,0,10"/>

                    <Grid Margin="0,0,0,10">
                        <TextBlock Text="版权" FontWeight="Bold" VerticalAlignment="Center"/>
                        <TextBlock Text="© 2025 Your Name" HorizontalAlignment="Right"
                                   VerticalAlignment="Center"/>
                    </Grid>
                    <ui:Separator Margin="0,5,0,10"/>

                    <Grid>
                        <TextBlock Text="更新渠道" FontWeight="Bold" VerticalAlignment="Center"/>
                        <TextBlock Text="GitHub Releases" HorizontalAlignment="Right"
                                   VerticalAlignment="Center"/>
                    </Grid>
                </StackPanel>
            </ui:Card>
        </StackPanel>
    </Grid>
</ui:Page>
```

**验证：**
- 页面正常显示 Logo
- 版本号、作者、官网、版权、更新渠道全部显示
- 官网链接可点击跳转

---

## 阶段四：实现异步交互与状态反馈

### 步骤 14：添加 ViewModel 异步支持

**指令：**
1. 打开 `MainViewModel.cs`
2. 添加以下异步方法占位实现：
   ```csharp
   private bool _isLoading;
   public bool IsLoading
   {
       get => _isLoading;
       set => SetProperty(ref _isLoading, value);
   }

   public bool IsNotLoading => !IsLoading;

   public async Task TestConnectionAsync()
   {
       IsLoading = true;
       try
       {
           var result = await _aiService.TestConnectionAsync(_config.Api);
           if (result.IsSuccess)
               ShowStatus("连接成功！", true);
           else
               ShowStatus(result.ErrorMessage, false);
       }
       finally
       {
           IsLoading = false;
       }
   }

   public async Task SaveConfigAsync()
   {
       IsLoading = true;
       try
       {
           // 保存配置逻辑
           ShowStatus("保存成功！", true);
       }
       catch (Exception ex)
       {
           ShowStatus($"保存失败: {ex.Message}", false);
       }
       finally
       {
           IsLoading = false;
       }
   }

   private void ShowStatus(string message, bool isSuccess)
   {
       StatusMessage = message;
       SaveSuccess = isSuccess;
       // 触发状态更新
   }
   ```

**验证：**
- `dotnet build` 无编译错误
- `IsLoading` 和 `IsNotLoading` 属性正确实现

---

### 步骤 15：实现密码显隐切换

**指令：**
1. 在 `ConfigPage.xaml.cs` 中添加点击事件：
   ```csharp
   private bool _isPasswordVisible = false;

   private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
   {
       _isPasswordVisible = !_isPasswordVisible;
       ApiKeyPasswordBox.Password = _isPasswordVisible ?
           ApiKeyPasswordBox.Password : "";
       // 注意：实际实现需要使用 TextBox 作为中转
   }
   ```

**验证：**
- 点击眼睛按钮时密码显示/隐藏切换正常
- 按钮位置正确对齐密码框

---

### 步骤 16：实现无弹窗保存反馈

**指令：**
1. 在 `ConfigPage.xaml` 中添加状态区域（已包含在步骤 9）
2. 在 ViewModel 中添加状态方法：
   ```csharp
   public void ShowStatus(string message, bool isSuccess)
   {
       StatusBannerVisibility = Visibility.Visible;
       StatusMessage = message;
       StatusIcon.Symbol = isSuccess ? Symbol.Checkmark : Symbol.Error;
       StatusBannerBackground = isSuccess ? Green : Red;

       // 3 秒后自动消失
       _ = Task.Delay(3000).ContinueWith(_ => {
           StatusBannerVisibility = Visibility.Collapsed;
       });
   }
   ```

**验证：**
- 保存成功后显示绿色状态条
- 保存失败后显示红色状态条
- 状态条在 3 秒后自动消失

---

### 步骤 17：实现输入校验

**指令：**
在 `MainViewModel` 中添加校验逻辑：

```csharp
private string ValidateConfig()
{
    if (string.IsNullOrWhiteSpace(Config?.Api?.BaseUrl))
        return "Base URL 不能为空";
    if (string.IsNullOrWhiteSpace(Config?.Api?.ApiKey))
        return "API Key 不能为空";
    if (string.IsNullOrWhiteSpace(Config?.Api?.Model))
        return "Model 不能为空";
    if (!Config.Api.BaseUrl.StartsWith("http://") &&
        !Config.Api.BaseUrl.StartsWith("https://"))
        return "Base URL 必须以 http:// 或 https:// 开头";
    return null; // 无错误
}
```

**验证：**
- 空值输入显示红色提示
- 无效 URL 显示红色提示
- 校验失败时保存按钮禁用

---

## 阶段五：迁移业务逻辑

### 步骤 18：迁移 MainViewModel 业务逻辑

**指令：**
1. 打开备份目录中的原 `MainViewModel.cs`
2. 逐个迁移以下方法到新文件：
   - `LoadConfig()` - 加载配置
   - `SaveConfig()` - 保存配置（改为异步）
   - `TestConnection()` - 测试连接（改为异步）
   - `RegisterContextMenu()` - 注册右键菜单
   - `UnregisterContextMenu()` - 注销右键菜单

**验证：**
- 配置加载正常
- 保存后配置正确写入文件
- 右键菜单注册/注销功能正常

---

### 步骤 19：迁移 MainWindow 事件处理

**指令：**
1. 打开备份目录中的原 `MainWindow.xaml.cs`
2. 迁移以下功能到新文件：
   - 密码显示切换逻辑
   - 主题切换逻辑
   - 托盘管理器集成（`SystemTrayManager`）
   - 窗口关闭/关闭事件处理

**验证：**
- 密码显示切换功能正常
- 主题切换功能正常
- 托盘图标显示正常
- 双击托盘显示/隐藏窗口正常

---

## 阶段六：清理与优化

### 步骤 20：移除旧主题文件

**指令：**
1. 确认 `WPF-UI` 主题系统正常工作后
2. 删除以下文件：
   - `Themes/DarkTheme.xaml`
   - `Themes/LightTheme.xaml`
3. 删除 `App.xaml.cs` 中的旧主题管理相关代码

**验证：**
- 主题切换仍然正常工作
- 深色/浅色/跟随系统主题正确应用

---

### 步骤 21：更新项目引用

**指令：**
1. 检查 `AI.DiffAssistant.GUI.csproj` 中是否还有旧的 `UseWPF` 或 `UseWindowsForms` 引用
2. 确保以下配置存在：
   ```xml
   <UseWPF>true</UseWPF>
   <UseWindowsForms>true</UseWindowsForms>
   ```

**验证：**
- `dotnet build` 无警告
- 项目能够正常引用 `System.Windows.Forms`

---

### 步骤 22：更新图标资源

**指令：**
1. 检查 `Assets/` 目录下的图标文件
2. 确认 `WPF-UI` 所需的图标资源已正确设置
3. 测试导航按钮图标显示正常

**验证：**
- 所有导航图标正常显示
- 无缺失资源警告

---

## 阶段七：全面测试

### 步骤 23：编译测试

**指令：**
执行以下命令进行编译验证：
```bash
dotnet clean
dotnet build -c Release
```

**验证：**
- 0 个编译错误
- 最多 10 个警告（可接受）

---

### 步骤 24：功能测试

**指令：**
创建测试用例并逐一验证：

| 测试项 | 预期结果 | 通过 |
|--------|----------|------|
| 启动应用 | 窗口显示，侧边栏 5 个 Tab 可见 | ☐ |
| 导航切换 | 点击 Tab 切换内容区 | ☐ |
| 配置页测试连接 | 按钮禁用，显示加载动画，完成后显示状态条 | ☐ |
| 配置页保存配置 | 按钮禁用，显示加载动画，完成后显示状态条 | ☐ |
| 配置页密码显隐 | 点击眼睛切换密码可见性 | ☐ |
| 配置页输入校验 | 无效输入显示错误提示 | ☐ |
| 设置页注册/注销 | 右键菜单正确添加/移除 | ☐ |
| 设置页主题切换 | 跟随系统/浅色/深色切换正常 | ☐ |
| 历史版本页 | 版本列表正确显示 | ☐ |
| 日志设置页 | 日志级别可切换，日志文件可打开 | ☐ |
| 关于页面 | 显示版本、作者、官网、版权、更新渠道 | ☐ |
| 主题跟随系统 | 系统主题切换后应用主题同步 | ☐ |
| 托盘图标 | 右下角显示托盘图标 | ☐ |
| 双击托盘 | 显示/隐藏主窗口 | ☐ |

---

### 步骤 25：集成测试

**指令：**
1. 右键选择两个文件执行分析
2. 验证 `difference.md` 正确生成
3. 验证 Toast 通知正常显示

**验证：**
- 差异分析功能正常
- 结果文件正确生成
- 通知正常显示

---

## 阶段八：文档更新

### 步骤 26：更新架构文档

**指令：**
1. 打开 `memory-bank/architecture.md`
2. 添加 V1.5 架构变更章节：
   - 新增 WPF-UI 依赖
   - 新增页面结构（ConfigPage、SettingsPage、VersionsPage、LogsPage、AboutPage）
   - 移除旧主题文件
   - 侧边栏导航布局说明

**验证：**
- 架构文档已更新
- 新增内容与实际实现一致

---

### 步骤 27：更新 CLAUDE.md

**指令：**
更新 `CLAUDE.md` 中的 GUI 部分，反映新的 UI 库和布局：

**验证：**
- CLAUDE.md 已更新
- 新开发者能够了解 UI 架构

---

## 附录 A：文件变更清单

### 新增文件

| 文件路径 | 用途 |
|----------|------|
| `GUI/Views/Pages/ConfigPage.xaml` | AI 配置页 |
| `GUI/Views/Pages/ConfigPage.xaml.cs` | AI 配置页逻辑 |
| `GUI/Views/Pages/SettingsPage.xaml` | 系统设置页 |
| `GUI/Views/Pages/SettingsPage.xaml.cs` | 系统设置页逻辑 |
| `GUI/Views/Pages/VersionsPage.xaml` | 版本下载页（复用 V3.0） |
| `GUI/Views/Pages/VersionsPage.xaml.cs` | 版本下载页逻辑 |
| `GUI/Views/Pages/LogsPage.xaml` | 日志设置页 |
| `GUI/Views/Pages/LogsPage.xaml.cs` | 日志设置页逻辑 |
| `GUI/Views/Pages/AboutPage.xaml` | 关于页面（Fluent Design） |
| `GUI/Views/Pages/AboutPage.xaml.cs` | 关于页面逻辑 |

### 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `GUI/AI.DiffAssistant.GUI.csproj` | 添加 WPF-UI 依赖 |
| `GUI/App.xaml` | 引入 WPF-UI 资源 |
| `GUI/App.xaml.cs` | 添加主题初始化（跟随系统） |
| `GUI/MainWindow.xaml` | 替换为 FluentWindow + 侧边栏布局 + 5 个导航项 |
| `GUI/MainWindow.xaml.cs` | 导航集成 |
| `GUI/ViewModels/MainViewModel.cs` | 异步方法 + 状态属性 + 主题切换 |
| `GUI/Views/AboutWindow.xaml` | 删除（由 AboutPage 替代） |
| `GUI/Views/AboutWindow.xaml.cs` | 删除（由 AboutPage 替代） |

### 删除文件

| 文件路径 | 删除原因 |
|----------|----------|
| `GUI/Themes/DarkTheme.xaml` | 由 WPF-UI 替代 |
| `GUI/Themes/LightTheme.xaml` | 由 WPF-UI 替代 |
| `GUI/Views/AboutWindow.xaml` | 由 AboutPage 替代 |
| `GUI/Views/AboutWindow.xaml.cs` | 由 AboutPage 替代 |

---

## 附录 B：依赖版本

| 包名 | 版本 | 用途 |
|------|------|------|
| WPF-UI | 3.0.0 | Windows 11 风格 UI 库 |
| Microsoft.Toolkit.Uwp.Notifications | 7.1.3 | Toast 通知 |

---

## 附录 C：Tab 功能对照表

| 导航图标 | Tab 名称 | 对应原功能 | 主要组件 |
|----------|----------|------------|----------|
| ⚙️ 配置 | AI 服务配置 | 原 MainWindow 上半部分 | BaseUrl、ApiKey、Model、SystemPrompt |
| 🛡️ 设置 | 系统集成 | 原 MainWindow 右键菜单部分 | 注册/注销、主题切换、托盘设置 |
| 📥 历史版本 | 版本下载 | V3.0 版本下载功能 | ReleaseService、版本列表 |
| 📋 日志 | 日志设置 | 新增功能 | 日志开关、级别选择 |
| ℹ️ 关于 | 关于页面 | 原 AboutWindow | 版本、作者、官网、版权 |

---

> 实施计划版本: 4.0
> 生成时间: 2026-01-02
> 基于 feature4.0.md 生成
