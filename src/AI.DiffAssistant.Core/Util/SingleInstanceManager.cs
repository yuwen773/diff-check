using System.Diagnostics;

namespace AI.DiffAssistant.Core.Util;

/// <summary>
/// 单实例管理器 - 使用 Mutex 确保程序单实例运行
/// 处理 Windows 多文件选择时启动多个实例的问题
/// </summary>
public class SingleInstanceManager : IDisposable
{
    /// <summary>
    /// 全局互斥锁名称
    /// </summary>
    public const string MutexName = @"Global\AI.DiffAssistant.SingleInstance";

    private readonly Mutex _mutex;
    private bool _ownsMutex;
    private bool _disposed;

    /// <summary>
    /// 是否成功获取互斥锁（是否为第一个实例）
    /// </summary>
    public bool IsFirstInstance => _ownsMutex;

    /// <summary>
    /// 初始化单实例管理器
    /// </summary>
    /// <param name="timeout">获取锁超时时间（毫秒），默认 5000ms</param>
    public SingleInstanceManager(int timeout = 5000)
    {
        bool createdNew;
        try
        {
            _mutex = new Mutex(
                false, // 初始状态为非 signaled
                MutexName,
                out createdNew);

            _ownsMutex = createdNew;

            if (!_ownsMutex)
            {
                // 等待获取锁（处理多实例同时启动情况）
                try
                {
                    if (!_mutex.WaitOne(timeout, false))
                    {
                        Debug.WriteLine("获取互斥锁超时，放弃等待");
                    }
                }
                catch (AbandonedMutexException)
                {
                    // 其他实例异常终止，我们获得了锁
                    _ownsMutex = true;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 无法创建全局互斥锁，尝试使用局部名称
            _mutex = new Mutex(false, MutexName + Process.GetCurrentProcess().Id, out createdNew);
            _ownsMutex = createdNew;
        }
    }

    /// <summary>
    /// 释放互斥锁
    /// </summary>
    public void Release()
    {
        if (_ownsMutex && !_disposed)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ObjectDisposedException)
            {
                // 已经释放，忽略
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放互斥锁失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 尝试优雅地传递焦点到已运行的实例（跨进程通信）
    /// 注意：这是一个简化实现，完整实现需要使用命名管道或内存映射文件
    /// </summary>
    /// <param name="args">要传递的参数</param>
    /// <returns>是否成功传递给已运行的实例</returns>
    public bool TryPassArgumentsToFirstInstance(string[] args)
    {
        if (IsFirstInstance)
            return false;

        // TODO: 实现完整的参数传递机制
        // 方案：
        // 1. 命名管道服务器（第一个实例）
        // 2. 命名管道客户端（后续实例）
        // 3. 或使用内存映射文件共享参数

        Debug.WriteLine("当前为后续实例，参数传递需要完整实现");
        return false;
    }

    /// <summary>
    /// 静默模式入口点 - 确保单实例执行
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <param name="actionOnFirstInstance">第一个实例要执行的操作</param>
    /// <param name="actionOnOtherInstance">后续实例要执行的操作</param>
    /// <returns>是否成功执行</returns>
    public static bool RunWithLock(string[] args, Action<string[]> actionOnFirstInstance, Action<string[]>? actionOnOtherInstance = null)
    {
        using var manager = new SingleInstanceManager();

        if (manager.IsFirstInstance)
        {
            actionOnFirstInstance(args);
            manager.Release();
            return true;
        }
        else
        {
            actionOnOtherInstance?.Invoke(args);
            return false;
        }
    }

    /// <summary>
    /// 获取当前实例的互斥锁名称
    /// </summary>
    public static string GetMutexName() => MutexName;

    public void Dispose()
    {
        if (!_disposed)
        {
            Release();
            _mutex.Dispose();
            _disposed = true;
        }
    }
}
