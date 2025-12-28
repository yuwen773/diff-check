using AI.DiffAssistant.Core.Registry;
using Xunit.Abstractions;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// 注册表管理测试
/// 注意：这些测试会实际修改 HKCU 注册表
/// </summary>
public class RegistryTests
{
    private readonly ITestOutputHelper _output;
    private readonly RegistryManager _registryManager;

    public RegistryTests(ITestOutputHelper output)
    {
        _output = output;
        _registryManager = new RegistryManager();
    }

    [Fact]
    public void IsRegistered_WhenNotRegistered_ReturnsFalse()
    {
        // Arrange - 确保先清理
        CleanupRegistry();

        // Act
        var result = _registryManager.IsRegistered();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RegisterContextMenu_WithValidExePath_ReturnsTrue()
    {
        // Arrange
        CleanupRegistry();
        var exePath = GetType().Assembly.Location;

        // Act
        var result = _registryManager.RegisterContextMenu(exePath);

        // Assert
        Assert.True(result);
        Assert.True(_registryManager.IsRegistered());
    }

    [Fact]
    public void RegisterContextMenu_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert - 空路径会抛出 ArgumentException，然后被外层捕获并重新抛出为 InvalidOperationException
        var ex = Assert.Throws<InvalidOperationException>(() => _registryManager.RegisterContextMenu(""));
        Assert.Contains("可执行文件路径不能为空", ex.Message);
    }

    [Fact]
    public void RegisterContextMenu_WithNonExistentPath_ThrowsFileNotFoundException()
    {
        // Act & Assert - FileNotFoundException 会被捕获并重新抛出为 InvalidOperationException
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _registryManager.RegisterContextMenu(@"C:\NonExistent\Path\app.exe"));
        Assert.Contains("可执行文件不存在", ex.Message);
    }

    [Fact]
    public void UnregisterContextMenu_WhenRegistered_ReturnsTrue()
    {
        // Arrange
        CleanupRegistry();
        var exePath = GetType().Assembly.Location;
        _registryManager.RegisterContextMenu(exePath);

        // Act
        var result = _registryManager.UnregisterContextMenu();

        // Assert
        Assert.True(result);
        Assert.False(_registryManager.IsRegistered());
    }

    [Fact]
    public void UnregisterContextMenu_WhenNotRegistered_ReturnsTrue()
    {
        // Arrange
        CleanupRegistry();

        // Act
        var result = _registryManager.UnregisterContextMenu();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetRegisteredPath_AfterRegistration_ReturnsCorrectPath()
    {
        // Arrange
        CleanupRegistry();
        var exePath = GetType().Assembly.Location;
        _registryManager.RegisterContextMenu(exePath);

        // Act
        var registeredPath = _registryManager.GetRegisteredPath();

        // Assert
        Assert.NotNull(registeredPath);
        Assert.Equal(exePath, registeredPath);
    }

    [Fact]
    public void GetRegisteredPath_WhenNotRegistered_ReturnsNull()
    {
        // Arrange
        CleanupRegistry();

        // Act
        var registeredPath = _registryManager.GetRegisteredPath();

        // Assert
        Assert.Null(registeredPath);
    }

    [Fact]
    public void RoundTrip_RegisterAndUnregister_WorksCorrectly()
    {
        // Arrange
        CleanupRegistry();
        var exePath = GetType().Assembly.Location;

        // Act & Assert - 第一次注册
        Assert.True(_registryManager.RegisterContextMenu(exePath));
        Assert.True(_registryManager.IsRegistered());

        // 注销
        Assert.True(_registryManager.UnregisterContextMenu());
        Assert.False(_registryManager.IsRegistered());

        // 再次注册
        Assert.True(_registryManager.RegisterContextMenu(exePath));
        Assert.True(_registryManager.IsRegistered());

        // 最终清理
        Assert.True(_registryManager.UnregisterContextMenu());
        Assert.False(_registryManager.IsRegistered());
    }

    /// <summary>
    /// 清理测试注册表项
    /// </summary>
    private void CleanupRegistry()
    {
        try
        {
            _registryManager.UnregisterContextMenu();
        }
        catch
        {
            // 忽略清理错误
        }
    }
}
