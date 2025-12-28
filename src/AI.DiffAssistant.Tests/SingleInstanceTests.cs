using AI.DiffAssistant.Core.Util;
using System.Threading;
using Xunit.Abstractions;

namespace AI.DiffAssistant.Tests;

/// <summary>
/// 单实例管理器测试
/// </summary>
public class SingleInstanceTests
{
    private readonly ITestOutputHelper _output;

    public SingleInstanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_FirstInstance_OwnsMutex()
    {
        // Arrange & Act
        using var manager = new SingleInstanceManager(1000);

        // Assert
        Assert.True(manager.IsFirstInstance);
    }

    [Fact]
    public void Constructor_SecondInstance_DoesNotOwnMutex()
    {
        // Arrange
        using var first = new SingleInstanceManager(1000);
        Assert.True(first.IsFirstInstance);

        // Act - 尝试获取同一个互斥锁
        using var second = new SingleInstanceManager(1000);

        // Assert
        Assert.False(second.IsFirstInstance);
    }

    [Fact]
    public void Dispose_AllowsNewInstanceToAcquireMutex()
    {
        // Arrange
        using var first = new SingleInstanceManager(1000);
        Assert.True(first.IsFirstInstance);

        // 释放并处置
        first.Dispose();

        // Act - 新的实例应该能获取锁
        using var second = new SingleInstanceManager(1000);

        // Assert
        Assert.True(second.IsFirstInstance);
    }

    [Fact]
    public void Release_AllowsNewInstanceToAcquireMutex()
    {
        // Arrange
        using var first = new SingleInstanceManager(1000);
        Assert.True(first.IsFirstInstance);

        // 释放互斥锁
        first.Release();
        first.Dispose();

        // 短暂等待确保系统释放互斥锁
        Thread.Sleep(50);

        // Act - 新的实例应该能获取锁
        using var second = new SingleInstanceManager(1000);

        // Assert
        Assert.True(second.IsFirstInstance);
    }

    [Fact]
    public void GetMutexName_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var mutexName = SingleInstanceManager.GetMutexName();

        // Assert
        Assert.Equal(@"Global\AI.DiffAssistant.SingleInstance", mutexName);
    }

    [Fact]
    public void RunWithLock_FirstInstance_ExecutesAction()
    {
        // Arrange
        var executed = false;
        var receivedArgs = Array.Empty<string>();

        // Act
        var result = SingleInstanceManager.RunWithLock(
            new[] { "arg1", "arg2" },
            (args) =>
            {
                executed = true;
                receivedArgs = args;
            });

        // Assert
        Assert.True(result);
        Assert.True(executed);
        Assert.Equal(new[] { "arg1", "arg2" }, receivedArgs);
    }

    [Fact]
    public void RunWithLock_OtherInstance_ExecutesOtherAction()
    {
        // Arrange
        using var first = new SingleInstanceManager(1000);
        Assert.True(first.IsFirstInstance);

        var otherExecuted = false;
        var receivedArgs = Array.Empty<string>();

        // Act
        var result = SingleInstanceManager.RunWithLock(
            new[] { "arg1", "arg2" },
            (_) => { },
            (args) =>
            {
                otherExecuted = true;
                receivedArgs = args;
            });

        // Assert
        Assert.False(result);
        Assert.True(otherExecuted);
        Assert.Equal(new[] { "arg1", "arg2" }, receivedArgs);
    }

    [Fact]
    public void MultipleInstances_AllExceptFirstReturnFalse()
    {
        // Arrange
        using var first = new SingleInstanceManager(1000);
        Assert.True(first.IsFirstInstance);

        // Act & Assert - 创建多个后续实例
        for (int i = 0; i < 5; i++)
        {
            using var instance = new SingleInstanceManager(1000);
            Assert.False(instance.IsFirstInstance);
        }
    }

    [Fact]
    public void Timeout_Expired_StillReturnsOwnershipStatus()
    {
        // Arrange - 创建一个会长时间持有的互斥锁
        using var holder = new SingleInstanceManager(10000);
        Assert.True(holder.IsFirstInstance);

        // Act - 尝试获取（会超时）
        using var waiter = new SingleInstanceManager(100);

        // Assert
        Assert.False(waiter.IsFirstInstance);
    }

    [Fact]
    public void Constructor_DisposesGracefully()
    {
        // Arrange & Act
        var manager = new SingleInstanceManager(1000);
        manager.Dispose();
        manager.Dispose(); // 双重释放不应抛出异常

        // Assert - 没有异常即为通过
    }

    [Fact]
    public void Release_WithoutOwnership_DoesNotThrow()
    {
        // Arrange
        using var first = new SingleInstanceManager(1000);
        Assert.True(first.IsFirstInstance);

        using var second = new SingleInstanceManager(1000);
        Assert.False(second.IsFirstInstance);

        // Act & Assert - 后续实例释放不应抛出异常
        second.Release();
    }
}
