namespace BotMain.Logging;

/// <summary>
/// 日志级别枚举，数值越大代表越严重，MinLogLevel 过滤时使用
/// </summary>
public enum BotLogLevel
{
    Info    = 0,
    Warning = 1,
    Error   = 2
}
