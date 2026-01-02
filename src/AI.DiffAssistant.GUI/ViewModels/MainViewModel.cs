using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AI.DiffAssistant.Core.Config;
using AI.DiffAssistant.Core.Diff;
using AI.DiffAssistant.Core.Release;
using AI.DiffAssistant.Core.Logging;
using AI.DiffAssistant.Core.Registry;
using AI.DiffAssistant.Shared.Models;
using WpfMessageBox = System.Windows.MessageBox;

namespace AI.DiffAssistant.GUI.ViewModels;

/// <summary>
/// 主窗口 ViewModel，实现 INotifyPropertyChanged
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ConfigManager _configManager;
    private readonly AiService _aiService;
    private readonly RegistryManager _registryManager;
    private readonly ReleaseService _releaseService;
    private readonly HttpClient _aiHttpClient;
    private readonly HttpClient _releaseHttpClient;

    // 绑定属性
    private string _baseUrl = string.Empty;
    private string _apiKey = string.Empty;
    private string _modelName = string.Empty;
    private string _systemPrompt = string.Empty;
    private bool _isRegistered;
    private bool _isTesting;
    private bool _isSaving;
    private bool _isReleaseLoading;
    private string _releaseLoadError = string.Empty;

    private ObservableCollection<ReleaseInfo> _releases = new();

    // 日志配置属性
    private bool _loggingEnabled = true;
    private string _logPath = string.Empty;
    private bool _logErrorEnabled = true;
    private bool _logWarningEnabled = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        _aiHttpClient = new HttpClient();
        _releaseHttpClient = new HttpClient();
        _configManager = new ConfigManager();
        _aiService = new AiService(_aiHttpClient);
        _registryManager = new RegistryManager();
        _releaseService = new ReleaseService(_releaseHttpClient);

        // 初始化命令
        TestConnectionCommand = new RelayCommand(async _ => await TestConnectionAsync(), _ => IsNotTesting);
        SaveConfigCommand = new RelayCommand(_ => SaveConfig(), _ => IsNotSaving);
        RegisterCommand = new RelayCommand(_ => RegisterContextMenu(), _ => NotRegistered && !IsRegistered);
        UnregisterCommand = new RelayCommand(_ => UnregisterContextMenu(), _ => IsRegistered);
        OpenLogFileCommand = new RelayCommand(_ => OpenLogFile());
        ClearLogFileCommand = new RelayCommand(_ => ClearLogFile());
        BrowseLogPathCommand = new RelayCommand(_ => BrowseLogPath());
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        RefreshReleaseCommand = new RelayCommand(async _ => await RefreshReleasesAsync(), _ => IsNotReleaseLoading);
        DownloadReleaseCommand = new RelayCommand(DownloadRelease, param => param is ReleaseInfo);

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
    public bool IsNotReleaseLoading => !IsReleaseLoading;
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

    public bool IsReleaseLoading
    {
        get => _isReleaseLoading;
        set
        {
            if (SetProperty(ref _isReleaseLoading, value))
            {
                OnPropertyChanged(nameof(IsNotReleaseLoading));
            }
        }
    }

    public string ReleaseLoadError
    {
        get => _releaseLoadError;
        set => SetProperty(ref _releaseLoadError, value);
    }

    public ObservableCollection<ReleaseInfo> Releases
    {
        get => _releases;
        set => SetProperty(ref _releases, value);
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
    public ICommand RefreshReleaseCommand { get; }
    public ICommand DownloadReleaseCommand { get; }

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
            WpfMessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            WpfMessageBox.Show("请输入 Base URL", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            WpfMessageBox.Show("请输入 API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelName))
        {
            WpfMessageBox.Show("请输入模型名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                WpfMessageBox.Show("连接成功！", "测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                WpfMessageBox.Show($"连接失败: {result.ErrorMessage}", "测试结果", MessageBoxButton.OK, MessageBoxImage.Error);
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
            WpfMessageBox.Show("Base URL 不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelName))
        {
            WpfMessageBox.Show("模型名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            WpfMessageBox.Show("配置已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                WpfMessageBox.Show("日志文件不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"打开日志文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            WpfMessageBox.Show("日志已清除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"清除日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseLogPath()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "选择日志文件位置",
            Filter = "日志文件 (*.log)|*.log|所有文件 (*.*)|*.*",
            FileName = string.IsNullOrEmpty(LogPath) ? "diff-check.log" : Path.GetFileName(LogPath),
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
            WpfMessageBox.Show("已成功添加到右键菜单！\n提示: 按住 Ctrl 选中两个文件即可使用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"注册失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UnregisterContextMenu()
    {
        try
        {
            _registryManager.UnregisterContextMenu();

            RefreshRegistrationStatus();
            WpfMessageBox.Show("已从右键菜单移除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"移除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshRegistrationStatus()
    {
        if (!_registryManager.IsRegistered())
        {
            IsRegistered = false;
            OnPropertyChanged(nameof(NotRegistered));
            return;
        }

        var registeredPath = _registryManager.GetRegisteredPath();
        if (string.IsNullOrWhiteSpace(registeredPath))
        {
            IsRegistered = false;
            OnPropertyChanged(nameof(NotRegistered));
            return;
        }

        if (IsCliExecutable(registeredPath))
        {
            IsRegistered = true;
            OnPropertyChanged(nameof(NotRegistered));
            return;
        }

        try
        {
            var cliPath = GetCliExecutablePath();
            _registryManager.RegisterContextMenu(cliPath);
            IsRegistered = true;
        }
        catch
        {
            IsRegistered = false;
        }

        OnPropertyChanged(nameof(NotRegistered));
    }

    private async Task RefreshReleasesAsync()
    {
        IsReleaseLoading = true;
        ReleaseLoadError = "加载中...";

        try
        {
            var releases = await _releaseService.GetStableReleasesAsync("yuwen773", "diff-check");

            Releases = new ObservableCollection<ReleaseInfo>(releases);
            ReleaseLoadError = Releases.Count == 0 ? "暂无可用稳定版" : string.Empty;
        }
        catch (Exception ex)
        {
            ReleaseLoadError = $"加载版本列表失败: {ex.Message}";
            Releases.Clear();
        }
        finally
        {
            IsReleaseLoading = false;
        }
    }

    private void DownloadRelease(object? parameter)
    {
        if (parameter is not ReleaseInfo release)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(release.DownloadUrl))
        {
            WpfMessageBox.Show("下载地址为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = release.DownloadUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"打开下载链接失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 获取 CLI 可执行文件路径
    /// </summary>
    private static string GetCliExecutablePath()
    {
        var currentPath = GetExecutablePath();
        var directory = Path.GetDirectoryName(currentPath);
        var cliPath = Path.Combine(directory ?? "", "diff-check-cli.exe");

        if (File.Exists(cliPath))
        {
            return cliPath;
        }

        if (!string.IsNullOrWhiteSpace(directory))
        {
            var siblingCliPath = Path.GetFullPath(Path.Combine(directory, "..", "cli", "diff-check-cli.exe"));
            if (File.Exists(siblingCliPath))
            {
                return siblingCliPath;
            }
        }

        throw new FileNotFoundException("未找到 diff-check-cli.exe，请将其与 GUI 放在同一目录或 publish/cli 目录。");
    }

    private static bool IsCliExecutable(string path)
    {
        return path.EndsWith("diff-check-cli.exe", StringComparison.OrdinalIgnoreCase);
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
