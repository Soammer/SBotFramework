namespace BotMain.Logging;

/// <summary>
/// 日志接口，定义三种日志级别的核心输出方法
/// </summary>
public interface ILogger
{
    /// <summary>输出 Info 级别日志（接受已拼接完毕的消息）</summary>
    void LogInfo(string message);

    /// <summary>输出 Warning 级别日志（接受已拼接完毕的消息）</summary>
    void LogWarning(string message);

    /// <summary>输出 Error 级别日志（接受已拼接完毕的消息）</summary>
    void LogError(string message);
}
