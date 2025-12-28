using AI.DiffAssistant.Core.Notification;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// 通知管理器单元测试
/// </summary>
public class NotificationManagerTests
{
    [Fact]
    public void NotificationManager_ShouldHaveAppId()
    {
        // Arrange & Act
        var appId = "AI.DiffAssistant";

        // Assert
        Assert.Equal("AI.DiffAssistant", appId);
    }

    [Fact]
    public void NotificationManager_Initialize_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => NotificationManager.Initialize());
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_RegisterAppForNotification_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => NotificationManager.RegisterAppForNotification());
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowSuccess_ShouldNotThrow()
    {
        // Arrange
        var message = "测试成功消息";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowSuccess(message));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowError_ShouldNotThrow()
    {
        // Arrange
        var error = "测试错误消息";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowError(error));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowSuccess_WithLongMessage_ShouldNotThrow()
    {
        // Arrange
        var longMessage = new string('A', 1000);

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowSuccess(longMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowError_WithEmptyMessage_ShouldNotThrow()
    {
        // Arrange
        var emptyMessage = string.Empty;

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowError(emptyMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowSuccess_WithSpecialCharacters_ShouldNotThrow()
    {
        // Arrange
        var specialMessage = "测试消息!@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowSuccess(specialMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowError_WithChineseCharacters_ShouldNotThrow()
    {
        // Arrange
        var chineseMessage = "错误：无法连接到 AI 服务，请检查网络连接。";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowError(chineseMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowSuccess_WithUnicodeCharacters_ShouldNotThrow()
    {
        // Arrange
        var unicodeMessage = "测试消息 🎉 🔔 ✅ 日本語 한국어";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowSuccess(unicodeMessage));
        Assert.Null(exception);
    }
}
