using BotMain.Logging;

namespace BotMain.Core;

/// <summary>
/// 全局配置，从 appsettings.json 的 BotSettings / FilterSettings 节点加载
/// </summary>
public static class GlobalSettings
{
    private static BotLogLevel s_minLogLevel = BotLogLevel.Info;
    private static bool s_enableLogColor = false;
    private static int s_maxSendDelaySeconds = 5;
    private static int s_sendTimeoutSeconds = 15;

    private static long s_selfId = 0L;

    private static HashSet<long> s_privateList = [];
    private static bool s_privateListIsBlacklist = false;
    private static HashSet<long> s_groupList = [];
    private static bool s_groupListIsBlacklist = false;

    /// <summary>Bot 自身的 UserId，用于屏蔽自身消息</summary>
    public static long SelfId => s_selfId;

    /// <summary>日志最小输出级别，低于此级别的日志将被忽略。默认 Info</summary>
    public static BotLogLevel MinLogLevel => s_minLogLevel;

    /// <summary>是否启用控制台颜色输出。默认 false</summary>
    public static bool EnableLogColor => s_enableLogColor;

    /// <summary>消息发送随机延迟的最大秒数（实际延迟在 1 到此值之间随机）。默认 5</summary>
    public static int MaxSendDelaySeconds => s_maxSendDelaySeconds;

    /// <summary>消息发送接口调用的超时秒数。默认 15</summary>
    public static int SendTimeoutSeconds => s_sendTimeoutSeconds;

    /// <summary>
    /// 判断指定私聊用户是否被允许（接收或发送）。
    /// 白名单模式：仅允许名单内的 uid，名单为空时拒绝所有。
    /// 黑名单模式：名单内的 uid 拒绝，其余允许。
    /// </summary>
    public static bool IsPrivateAllowed(long uid)
    {
        if (s_privateListIsBlacklist)
            return !s_privateList.Contains(uid);
        return s_privateList.Contains(uid);
    }

    /// <summary>
    /// 判断指定群聊是否被允许（接收或发送）。
    /// 白名单模式：仅允许名单内的 gid，名单为空时拒绝所有。
    /// 黑名单模式：名单内的 gid 拒绝，其余允许。
    /// </summary>
    public static bool IsGroupAllowed(long gid)
    {
        if (s_groupListIsBlacklist)
            return !s_groupList.Contains(gid);
        return s_groupList.Contains(gid);
    }

    /// <summary>
    /// 从配置文件加载设置，应在 Bot 启动前调用一次
    /// </summary>
    public static void Load(BotSettingsJson? settings, FilterSettingsJson? filter = null, long selfId = 0L)
    {
        if (selfId != 0L)
            s_selfId = selfId;

        if (settings is not null)
        {
            s_minLogLevel = settings.MinLogLevel;
            s_enableLogColor = settings.EnableLogColor;
            s_maxSendDelaySeconds = Math.Max(1, settings.MaxSendDelaySeconds);
            s_sendTimeoutSeconds = Math.Max(1, settings.SendTimeoutSeconds);
        }

        if (filter is not null)
        {
            s_privateList = [.. filter.PrivateList];
            s_privateListIsBlacklist = filter.PrivateListIsBlacklist;
            s_groupList = [.. filter.GroupList];
            s_groupListIsBlacklist = filter.GroupListIsBlacklist;
        }
    }
}

public class BotSettingsJson
{
    public BotLogLevel MinLogLevel { get; init; } = BotLogLevel.Info;
    public bool EnableLogColor { get; init; } = false;
    public int MaxSendDelaySeconds { get; init; } = 3;
    public int SendTimeoutSeconds { get; init; } = 15;
}

public class FilterSettingsJson
{
    /// <summary>私聊用户 ID 名单</summary>
    public List<long> PrivateList { get; init; } = [];

    /// <summary>私聊名单是否为黑名单。false（默认）= 白名单；true = 黑名单</summary>
    public bool PrivateListIsBlacklist { get; init; } = false;

    /// <summary>群聊 ID 名单</summary>
    public List<long> GroupList { get; init; } = [];

    /// <summary>群聊名单是否为黑名单。false（默认）= 白名单；true = 黑名单</summary>
    public bool GroupListIsBlacklist { get; init; } = false;
}