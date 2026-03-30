using BotMain.Core;

namespace BotMain.Logging;

/// <summary>
/// 基础日志实现，直接向控制台输出，支持多种重载以避免调用侧的字符串拼接 GC。
/// 输出行为受 <see cref="GlobalSettings.MinLogLevel"/> 和
/// <see cref="GlobalSettings.EnableLogColor"/> 控制。
/// </summary>
public class BaseLoggerImp : ILogger
{
    private const string c_InfoPrefix = "[INFO]   ";
    private const string c_WarningPrefix = "[WARNING]";
    private const string c_ErrorPrefix = "[ERROR]  ";

    // 控制台颜色操作需要加锁，避免多线程交错
    private static readonly object s_consoleLock = new();

    #region ILogger 实现

    public void LogInfo(string message)
    {
        if (GlobalSettings.MinLogLevel > BotLogLevel.Info) return;
        lock (s_consoleLock)
        {
            if (GlobalSettings.EnableLogColor) Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{c_InfoPrefix} {message}");
            if (GlobalSettings.EnableLogColor) Console.ResetColor();
        }
    }

    public void LogWarning(string message)
    {
        if (GlobalSettings.MinLogLevel > BotLogLevel.Warning) return;
        lock (s_consoleLock)
        {
            if (GlobalSettings.EnableLogColor) Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{c_WarningPrefix} {message}");
            if (GlobalSettings.EnableLogColor) Console.ResetColor();
        }
    }

    public void LogError(string message)
    {
        // Error 始终输出，不受 MinLogLevel 过滤
        lock (s_consoleLock)
        {
            if (GlobalSettings.EnableLogColor) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{c_ErrorPrefix} {message}");
            if (GlobalSettings.EnableLogColor) Console.ResetColor();
        }
    }

    #endregion ILogger 实现

    #region Info 对外方法

    /// <summary>直接输出 Info 消息</summary>
    public void Info(string message)
    {
        LogInfo(message);
    }

    /// <summary>格式化后输出 Info 消息，泛型参数避免值类型在调用侧装箱</summary>
    public void Info<T>(string format, T arg)
    {
        LogInfo(string.Format(format, arg));
    }

    /// <summary>格式化后输出 Info 消息，支持两个泛型参数</summary>
    public void Info<T1, T2>(string format, T1 arg1, T2 arg2)
    {
        LogInfo(string.Format(format, arg1, arg2));
    }

    /// <summary>格式化后输出 Info 消息，支持三个泛型参数</summary>
    public void Info<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
    {
        LogInfo(string.Format(format, arg1, arg2, arg3));
    }

    /// <summary>格式化后输出 Info 消息，支持任意数量参数（产生数组分配，作为兜底重载）</summary>
    public void Info(string format, params object[] args)
    {
        LogInfo(string.Format(format, args));
    }

    #endregion Info 对外方法

    #region Warning 对外方法

    /// <summary>直接输出 Warning 消息</summary>
    public void Warning(string message)
    {
        LogWarning(message);
    }

    /// <summary>格式化后输出 Warning 消息，泛型参数避免值类型在调用侧装箱</summary>
    public void Warning<T>(string format, T arg)
    {
        LogWarning(string.Format(format, arg));
    }

    /// <summary>格式化后输出 Warning 消息，支持两个泛型参数</summary>
    public void Warning<T1, T2>(string format, T1 arg1, T2 arg2)
    {
        LogWarning(string.Format(format, arg1, arg2));
    }

    /// <summary>格式化后输出 Warning 消息，支持三个泛型参数</summary>
    public void Warning<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
    {
        LogWarning(string.Format(format, arg1, arg2, arg3));
    }

    /// <summary>格式化后输出 Warning 消息，支持任意数量参数（产生数组分配，作为兜底重载）</summary>
    public void Warning(string format, params object[] args)
    {
        LogWarning(string.Format(format, args));
    }

    #endregion Warning 对外方法

    #region Error 对外方法

    /// <summary>直接输出 Error 消息</summary>
    public void Error(string message)
    {
        LogError(message);
    }

    /// <summary>格式化后输出 Error 消息，泛型参数避免值类型在调用侧装箱</summary>
    public void Error<T>(string format, T arg)
    {
        LogError(string.Format(format, arg));
    }

    /// <summary>格式化后输出 Error 消息，支持两个泛型参数</summary>
    public void Error<T1, T2>(string format, T1 arg1, T2 arg2)
    {
        LogError(string.Format(format, arg1, arg2));
    }

    /// <summary>格式化后输出 Error 消息，支持三个泛型参数</summary>
    public void Error<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
    {
        LogError(string.Format(format, arg1, arg2, arg3));
    }

    /// <summary>格式化后输出 Error 消息，支持任意数量参数（产生数组分配，作为兜底重载）</summary>
    public void Error(string format, params object[] args)
    {
        LogError(string.Format(format, args));
    }

    #endregion Error 对外方法
}