using System.Text.Json;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// 配置模型单元测试
/// </summary>
public class ConfigModelTests
{
    [Fact]
    public void AppConfig_DefaultValues_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var config = new AppConfig();

        // Assert
        Assert.NotNull(config.Api);
        Assert.NotNull(config.Prompts);
        Assert.NotNull(config.Settings);
    }

    [Fact]
    public void ApiConfig_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var apiConfig = new ApiConfig();

        // Assert
        Assert.Equal("https://api.openai.com/v1", apiConfig.BaseUrl);
        Assert.Equal("gpt-4o", apiConfig.Model);
        Assert.Equal(string.Empty, apiConfig.ApiKey);
    }

    [Fact]
    public void PromptConfig_DefaultSystemPrompt_ShouldNotBeEmpty()
    {
        // Arrange & Act
        var promptConfig = new PromptConfig();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(promptConfig.SystemPrompt));
        Assert.Contains("文档对比助手", promptConfig.SystemPrompt);
    }

    [Fact]
    public void AppSettings_DefaultMaxTokenLimit_ShouldBe15000()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal(15000, settings.MaxTokenLimit);
    }

    [Fact]
    public void AppConfig_Serialization_ShouldPreserveValues()
    {
        // Arrange
        var config = new AppConfig
        {
            Api = new ApiConfig
            {
                BaseUrl = "https://custom.api.com/v1",
                ApiKey = "test-key-123",
                Model = "gpt-3.5-turbo"
            },
            Prompts = new PromptConfig
            {
                SystemPrompt = "Custom prompt"
            },
            Settings = new AppSettings
            {
                MaxTokenLimit = 20000
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("https://custom.api.com/v1", deserialized.Api.BaseUrl);
        Assert.Equal("test-key-123", deserialized.Api.ApiKey);
        Assert.Equal("gpt-3.5-turbo", deserialized.Api.Model);
        Assert.Equal("Custom prompt", deserialized.Prompts.SystemPrompt);
        Assert.Equal(20000, deserialized.Settings.MaxTokenLimit);
    }

    [Fact]
    public void AppConfig_Deserialization_FromDefaultJson_ShouldMatch()
    {
        // Arrange
        var defaultJson = """
            {
              "api": {
                "baseUrl": "https://api.openai.com/v1",
                "apiKey": "",
                "model": "gpt-4o"
              },
              "prompts": {
                "system": "你是一个文档对比助手，请对比两份文档，忽略格式差异，重点总结语义上的变化，并用 Markdown 列表输出。"
              },
              "settings": {
                "maxTokenLimit": 15000
              }
            }
            """;

        // Act
        var config = JsonSerializer.Deserialize<AppConfig>(defaultJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://api.openai.com/v1", config!.Api.BaseUrl);
        Assert.Equal("gpt-4o", config.Api.Model);
        Assert.Equal(15000, config.Settings.MaxTokenLimit);
    }
}
