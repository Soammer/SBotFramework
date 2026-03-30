using BotMain.Logging;

namespace BotMain.Core;

/// <summary>
/// 全局配置，从 appsettings.json 的 BotSettings 节点加载
/// </summary>
public static class GlobalSettings
{
    private static BotLogLevel s_minLogLevel = BotLogLevel.Info;
    private static bool s_enableLogColor = false;
    private static int s_maxSendDelaySeconds = 5;
    private static int s_sendTimeoutSeconds = 15;

    /// <summary>日志最小输出级别，低于此级别的日志将被忽略。默认 Info</summary>
    public static BotLogLevel MinLogLevel => s_minLogLevel;

    /// <summary>是否启用控制台颜色输出。默认 false</summary>
    public static bool EnableLogColor => s_enableLogColor;

    /// <summary>消息发送随机延迟的最大秒数（实际延迟在 1 到此值之间随机）。默认 5</summary>
    public static int MaxSendDelaySeconds => s_maxSendDelaySeconds;

    /// <summary>消息发送接口调用的超时秒数。默认 15</summary>
    public static int SendTimeoutSeconds => s_sendTimeoutSeconds;

    /// <summary>
    /// 从配置文件加载设置，应在 Bot 启动前调用一次
    /// </summary>
    public static void Load(BotSettingsJson? settings)
    {
        if (settings is null) return;
        s_minLogLevel = settings.MinLogLevel;
        s_enableLogColor = settings.EnableLogColor;
        s_maxSendDelaySeconds = Math.Max(1, settings.MaxSendDelaySeconds);
        s_sendTimeoutSeconds = Math.Max(1, settings.SendTimeoutSeconds);
    }
}

public class BotSettingsJson
{
    public BotLogLevel MinLogLevel { get; init; } = BotLogLevel.Info;
    public bool EnableLogColor { get; init; } = false;
    public int MaxSendDelaySeconds { get; init; } = 3;
    public int SendTimeoutSeconds { get; init; } = 15;
}