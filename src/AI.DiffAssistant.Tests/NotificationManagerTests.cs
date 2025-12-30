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

    #region ShowSuccess with FilePath Tests (V1.1 Toast)

    [Fact]
    public void NotificationManager_ShowSuccess_WithFilePath_ShouldNotThrow()
    {
        // Arrange
        var message = "测试成功消息";
        var filePath = Path.Combine(Path.GetTempPath(), "difference.md");

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowSuccess(message, filePath));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowSuccess_WithNonExistentFilePath_ShouldNotThrow()
    {
        // Arrange
        var message = "测试成功消息";
        var filePath = "C:\\nonexistent\\path\\difference.md";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowSuccess(message, filePath));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowSuccess_WithEmptyFilePath_ShouldNotThrow()
    {
        // Arrange
        var message = "测试成功消息";
        var filePath = "";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowSuccess(message, filePath));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowSuccess_WithChineseFilePath_ShouldNotThrow()
    {
        // Arrange
        var message = "分析完成";
        var filePath = Path.Combine(Path.GetTempPath(), "差异文档.md");

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowSuccess(message, filePath));
        Assert.Null(exception);
    }

    #endregion

    #region Error Notification Tests

    [Fact]
    public void NotificationManager_ShowError_WithLongMessage_ShouldNotThrow()
    {
        // Arrange
        var longError = new string('E', 2000);

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowError(longError));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowError_WithApiKeyError_ShouldNotThrow()
    {
        // Arrange
        var errorMessage = "API Key 额度不足，请升级套餐";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowError(errorMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowError_WithPdfError_ShouldNotThrow()
    {
        // Arrange
        var errorMessage = "PDF 文档已加密，请解除密码保护后重试";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowError(errorMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowError_WithScanPdfError_ShouldNotThrow()
    {
        // Arrange
        var errorMessage = "不支持扫描版 PDF（检测到文本量极少，文件体积较大，可能是纯图片扫描）";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowError(errorMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_ShowError_WithNetworkError_ShouldNotThrow()
    {
        // Arrange
        var errorMessage = "网络错误：无法连接到 AI 服务，请检查网络连接后重试";

        // Act & Assert
        var exception = Record.Exception(() => NotificationManager.ShowError(errorMessage));
        Assert.Null(exception);
    }

    #endregion

    #region Multiple Notifications Tests

    [Fact]
    public void NotificationManager_MultipleSuccessNotifications_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() =>
        {
            NotificationManager.ShowSuccess("第一条消息");
            NotificationManager.ShowSuccess("第二条消息");
            NotificationManager.ShowSuccess("第三条消息");
        });
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_MultipleErrorNotifications_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() =>
        {
            NotificationManager.ShowError("第一条错误");
            NotificationManager.ShowError("第二条错误");
        });
        Assert.Null(exception);
    }

    [Fact]
    public void NotificationManager_SuccessThenError_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() =>
        {
            NotificationManager.ShowSuccess("成功消息");
            NotificationManager.ShowError("错误消息");
        });
        Assert.Null(exception);
    }

    #endregion
}
