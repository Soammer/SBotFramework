namespace BotMain;

/// <summary>控制台指令的字符串常量与显示文本</summary>
internal static class ConsoleConstant
{
    #region 消息文本

    internal const string MsgUnknown   = "未知的指令";
    internal const string MsgBadFormat = "错误的格式或语法";

    #endregion 消息文本

    #region 指令名

    internal const string CmdExit        = "exit";
    internal const string CmdHelp        = "help";
    internal const string CmdSendPrivate = "sendprivate";
    internal const string CmdSendGroup   = "sendgroup";
    internal const string CmdHotReload   = "hotreload";
    internal const string CmdPlugin      = "plugin";
    internal const string CmdDebug       = "debug";

    #endregion 指令名

    #region plugin 子指令名

    internal const string CmdPluginList    = "list";
    internal const string CmdPluginDesc    = "--d";
    internal const string CmdPluginVer     = "--v";
    internal const string CmdPluginEnable  = "enable";
    internal const string CmdPluginDisable = "disable";

    #endregion plugin 子指令名

    #region help 显示文本

    /// <summary>所有指令的简短说明，用于 help 列表</summary>
    internal static readonly (string Command, string Brief)[] CommandBriefs =
    [
        (CmdExit,        "停止 Bot"),
        (CmdHelp,        "列出所有指令，或查看某指令的详细用法"),
        (CmdSendPrivate, "向指定 QQ 发送私聊消息"),
        (CmdSendGroup,   "向指定群发送群聊消息"),
        (CmdHotReload,   "重新读取配置文件并应用到运行时"),
        (CmdPlugin,      "管理插件（list / enable / disable / --d / --v）"),
        (CmdDebug,       "向所有已启用插件广播调试指令"),
    ];

    /// <summary>各指令的详细用法，键为小写指令名</summary>
    internal static readonly Dictionary<string, string> CommandDetails = new(StringComparer.OrdinalIgnoreCase)
    {
        [CmdExit] = """
            语法：exit
            停止 Bot 并退出程序。
            """,

        [CmdHelp] = """
            语法：help
                   help <CommandName>
            不带参数时列出所有可用指令及简短说明；带指令名时显示该指令的详细用法。
            """,

        [CmdSendPrivate] = """
            语法：sendprivate <msg> <uid> [sendImmediately]
            向指定 QQ 号（uid）发送私聊消息。
              msg             消息文本，含空格时用双引号括起
              uid             目标 QQ 号（长整数）
              sendImmediately true = 立即发送；false（默认）= 加入延迟队列
            """,

        [CmdSendGroup] = """
            语法：sendgroup <msg> <gid> [sendImmediately]
            向指定群号（gid）发送群聊消息。
              msg             消息文本，含空格时用双引号括起
              gid             目标群号（长整数）
              sendImmediately true = 立即发送；false（默认）= 加入延迟队列
            """,

        [CmdHotReload] = """
            语法：hotreload
            重新读取 config/appsettings.json 并应用到 GlobalSettings。
            失败时保持原有配置不变，错误信息输出到日志。
            """,

        [CmdPlugin] = """
            语法：
              plugin list              列出所有已注册插件及启用状态
              plugin <name> --d        显示指定插件的描述
              plugin <name> --v        显示指定插件的版本
              plugin <name> enable     启用指定插件（调用 Init）
              plugin <name> disable    停用指定插件（调用 DeInit）
            <name> 为插件 PluginEntryAttribute 中声明的 PluginName，大小写不敏感。
            """,

        [CmdDebug] = """
            语法：debug [args...]
            将完整的 tokens（含 "debug" 本身作为 tokens[0]）广播给所有已启用插件的 ReceiveDebugCommand 方法。
            参数数量与含义由各插件自行定义。
            """,
    };

    #endregion help 显示文本
}
