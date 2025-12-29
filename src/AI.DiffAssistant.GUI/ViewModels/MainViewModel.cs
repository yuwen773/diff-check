using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AI.DiffAssistant.Core.Config;
using AI.DiffAssistant.Core.Diff;
using AI.DiffAssistant.Core.Logging;
using AI.DiffAssistant.Core.Registry;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.GUI.ViewModels;

/// <summary>
/// 主窗口 ViewModel，实现 INotifyPropertyChanged
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ConfigManager _configManager;
    private readonly AiService _aiService;
    private readonly RegistryManager _registryManager;
    private readonly HttpClient _httpClient;

    // 绑定属性
    private string _baseUrl = string.Empty;
    private string _apiKey = string.Empty;
    private string _modelName = string.Empty;
    private string _systemPrompt = string.Empty;
    private bool _isRegistered;
    private bool _isTesting;
    private bool _isSaving;

    // 日志配置属性
    private bool _loggingEnabled = true;
    private string _logPath = string.Empty;
    private bool _logErrorEnabled = true;
    private bool _logWarningEnabled = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        _httpClient = new HttpClient();
        _configManager = new ConfigManager();
        _aiService = new AiService(_httpClient);
        _registryManager = new RegistryManager();

        // 初始化命令
        TestConnectionCommand = new RelayCommand(async _ => await TestConnectionAsync(), _ => IsNotTesting);
        SaveConfigCommand = new RelayCommand(_ => SaveConfig(), _ => IsNotSaving);
        RegisterCommand = new RelayCommand(_ => RegisterContextMenu(), _ => NotRegistered && !IsRegistered);
        UnregisterCommand = new RelayCommand(_ => UnregisterContextMenu(), _ => IsRegistered);
        OpenLogFileCommand = new RelayCommand(_ => OpenLogFile());
        ClearLogFileCommand = new RelayCommand(_ => ClearLogFile());
        BrowseLogPathCommand = new RelayCommand(_ => BrowseLogPath());
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());

        // 加载现有配置
        LoadConfig();
        RefreshRegistrationStatus();
    }

    #region 绑定属性

    public string BaseUrl
    {
        get => _baseUrl;
        set => SetProperty(ref _baseUrl, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public string ModelName
    {
        get => _modelName;
        set => SetProperty(ref _modelName, value);
    }

    public string SystemPrompt
    {
        get => _systemPrompt;
        set => SetProperty(ref _systemPrompt, value);
    }

    public bool IsRegistered
    {
        get => _isRegistered;
        set => SetProperty(ref _isRegistered, value);
    }

    public bool IsNotTesting => !IsTesting;
    public bool IsNotSaving => !IsSaving;
    public bool NotRegistered => !IsRegistered;

    public bool IsTesting
    {
        get => _isTesting;
        set
        {
            if (SetProperty(ref _isTesting, value))
            {
                OnPropertyChanged(nameof(IsNotTesting));
            }
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            if (SetProperty(ref _isSaving, value))
            {
                OnPropertyChanged(nameof(IsNotSaving));
            }
        }
    }

    #endregion

    #region 日志配置属性

    public bool LoggingEnabled
    {
        get => _loggingEnabled;
        set => SetProperty(ref _loggingEnabled, value);
    }

    public string LogPath
    {
        get => _logPath;
        set => SetProperty(ref _logPath, value);
    }

    public bool LogErrorEnabled
    {
        get => _logErrorEnabled;
        set => SetProperty(ref _logErrorEnabled, value);
    }

    public bool LogWarningEnabled
    {
        get => _logWarningEnabled;
        set => SetProperty(ref _logWarningEnabled, value);
    }

    #endregion

    #region 日志命令

    public ICommand OpenLogFileCommand { get; }
    public ICommand ClearLogFileCommand { get; }
    public ICommand BrowseLogPathCommand { get; }

    #endregion

    #region 命令

    public ICommand TestConnectionCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand UnregisterCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    #endregion

    #region 业务逻辑

    private void ToggleTheme()
    {
        App.ToggleTheme();
    }

    private void LoadConfig()
    {
        try
        {
            var config = _configManager.LoadConfig();
            BaseUrl = config.Api.BaseUrl;
            ApiKey = config.Api.ApiKey;
            ModelName = config.Api.Model;
            SystemPrompt = config.Prompts.SystemPrompt;

            // 加载日志配置
            LoggingEnabled = config.Logging.Enabled;
            LogPath = config.Logging.LogPath;
            var levels = config.Logging.GetEnabledLevels();
            LogErrorEnabled = levels.Contains("Error");
            LogWarningEnabled = levels.Contains("Warning");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            MessageBox.Show("请输入 Base URL", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            MessageBox.Show("请输入 API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelName))
        {
            MessageBox.Show("请输入模型名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsTesting = true;
        try
        {
            var config = new ApiConfig
            {
                BaseUrl = BaseUrl,
                ApiKey = ApiKey,
                Model = ModelName
            };

            var result = await _aiService.TestConnectionAsync(config);

            if (result.IsSuccess)
            {
                MessageBox.Show("连接成功！", "测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"连接失败: {result.ErrorMessage}", "测试结果", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsTesting = false;
        }
    }

    private void SaveConfig()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            MessageBox.Show("Base URL 不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelName))
        {
            MessageBox.Show("模型名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsSaving = true;
        try
        {
            var config = _configManager.LoadConfig();
            config.Api.BaseUrl = BaseUrl;
            config.Api.ApiKey = ApiKey;
            config.Api.Model = ModelName;
            config.Prompts.SystemPrompt = SystemPrompt;

            // 保存日志配置
            config.Logging.Enabled = LoggingEnabled;
            config.Logging.LogPath = LogPath;
            var levels = new List<string>();
            if (LogErrorEnabled) levels.Add("Error");
            if (LogWarningEnabled) levels.Add("Warning");
            config.Logging.Level = string.Join(",", levels);

            _configManager.SaveConfig(config);

            MessageBox.Show("配置已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void OpenLogFile()
    {
        try
        {
            var logger = new LoggingService(new LoggingConfig
            {
                Enabled = LoggingEnabled,
                LogPath = LogPath
            });
            var path = logger.GetLogPath();
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("日志文件不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开日志文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearLogFile()
    {
        try
        {
            var logger = new LoggingService(new LoggingConfig
            {
                Enabled = LoggingEnabled,
                LogPath = LogPath
            });
            logger.Clear();
            MessageBox.Show("日志已清除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"清除日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseLogPath()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "选择日志文件位置",
            Filter = "日志文件 (*.log)|*.log|所有文件 (*.*)|*.*",
            FileName = string.IsNullOrEmpty(LogPath) ? "AI.DiffAssistant.log" : Path.GetFileName(LogPath),
            InitialDirectory = string.IsNullOrEmpty(LogPath)
                ? Path.GetTempPath()
                : Path.GetDirectoryName(LogPath)
        };

        if (dialog.ShowDialog() == true)
        {
            LogPath = dialog.FileName;
        }
    }

    private void RegisterContextMenu()
    {
        try
        {
            // 获取 CLI 可执行文件路径（右击分析需要调用 CLI 静默模式）
            var exePath = GetCliExecutablePath();

            // 注册右键菜单
            _registryManager.RegisterContextMenu(exePath);

            RefreshRegistrationStatus();
            MessageBox.Show("已成功添加到右键菜单！\n提示: 按住 Ctrl 选中两个文件即可使用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"注册失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UnregisterContextMenu()
    {
        try
        {
            _registryManager.UnregisterContextMenu();

            RefreshRegistrationStatus();
            MessageBox.Show("已从右键菜单移除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"移除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshRegistrationStatus()
    {
        IsRegistered = _registryManager.IsRegistered();
        OnPropertyChanged(nameof(NotRegistered));
    }

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

    private static string GetExecutablePath()
    {
        return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
               ?? Environment.ProcessPath
               ?? throw new InvalidOperationException("无法获取程序路径");
    }

    #endregion

    #region INotifyPropertyChanged 实现

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

/// <summary>
/// 简单命令实现
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
