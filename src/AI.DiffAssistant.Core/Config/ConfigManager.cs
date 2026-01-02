using System.Text.Json;
using AI.DiffAssistant.Shared.Constants;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Config;

/// <summary>
/// 配置管理器 - 负责配置文件的加载、保存和热更新监听
/// </summary>
public class ConfigManager
{
    private readonly string _configPath;
    private AppConfig? _config;
    private FileSystemWatcher? _watcher;
    private readonly object _lock = new();

    /// <summary>
    /// 配置变更事件
    /// </summary>
    public event Action? ConfigChanged;

    public ConfigManager()
    {
        _configPath = GetConfigPath();
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    public static string GetConfigPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = System.IO.Path.Combine(appDataPath, ConfigConstants.AppName);
        var configPath = System.IO.Path.Combine(configDir, ConfigConstants.ConfigFileName);

        var legacyPath = GetLegacyConfigPath(appDataPath);
        if (!System.IO.File.Exists(configPath) && System.IO.File.Exists(legacyPath))
        {
            if (!System.IO.Directory.Exists(configDir))
            {
                System.IO.Directory.CreateDirectory(configDir);
            }

            System.IO.File.Copy(legacyPath, configPath, true);
            try
            {
                System.IO.File.Delete(legacyPath);
            }
            catch
            {
                // 忽略清理旧配置失败
            }
        }

        return configPath;
    }

    private static string GetLegacyConfigPath(string appDataPath)
    {
        var legacyDir = System.IO.Path.Combine(appDataPath, ConfigConstants.LegacyAppName);
        return System.IO.Path.Combine(legacyDir, ConfigConstants.ConfigFileName);
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public AppConfig LoadConfig()
    {
        lock (_lock)
        {
            if (_config != null)
                return _config;

            // 确保目录存在
            var directory = System.IO.Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // 如果文件不存在，创建默认配置
            if (!System.IO.File.Exists(_configPath))
            {
                _config = CreateDefaultConfig();
                SaveConfigInternal(_config);
                return _config;
            }

            try
            {
                var json = System.IO.File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? CreateDefaultConfig();
            }
            catch
            {
                _config = CreateDefaultConfig();
            }

            if (_config != null && NormalizeConfig(_config))
            {
                SaveConfigInternal(_config);
            }

            return _config!;
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public void SaveConfig(AppConfig config)
    {
        lock (_lock)
        {
            SaveConfigInternal(config);
        }
    }

    /// <summary>
    /// 启动配置文件监听（热更新支持）
    /// </summary>
    public void StartWatching()
    {
        _watcher?.Dispose();

        var directory = System.IO.Path.GetDirectoryName(_configPath);
        if (string.IsNullOrEmpty(directory) || !System.IO.Directory.Exists(directory))
            return;

        _watcher = new FileSystemWatcher(directory, ConfigConstants.ConfigFileName);
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        _watcher.Changed += OnConfigChanged;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// 停止配置文件监听
    /// </summary>
    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    private void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        // 延迟一点时间确保文件写入完成
        Task.Delay(100).ContinueWith(_ =>
        {
            lock (_lock)
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? _config;
                    ConfigChanged?.Invoke();
                }
                catch
                {
                    // 忽略读取错误
                }
            }
        });
    }

    private void SaveConfigInternal(AppConfig config)
    {
        // 确保目录存在
        var directory = System.IO.Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(config, options);
        System.IO.File.WriteAllText(_configPath, json);

        _config = config;

        // 重新启动文件监听
        StartWatching();
    }

    private static bool NormalizeConfig(AppConfig config)
    {
        var updated = false;

        if (string.Equals(config.Logging.LogPath, "%TEMP%\\AI.DiffAssistant.log", StringComparison.OrdinalIgnoreCase))
        {
            config.Logging.LogPath = "%TEMP%\\diff-check.log";
            updated = true;
        }

        return updated;
    }

    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            Api = new ApiConfig
            {
                BaseUrl = "https://api.openai.com/v1",
                ApiKey = string.Empty,
                Model = "gpt-4o"
            },
            Prompts = new PromptConfig
            {
                SystemPrompt = "你是一个文档对比助手，请对比两份文档，忽略格式差异，重点总结语义上的变化，并用 Markdown 列表输出。"
            },
            Settings = new AppSettings
            {
                MaxTokenLimit = 15000
            }
        };
    }
}
