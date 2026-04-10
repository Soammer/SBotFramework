using BotMain.Core;
using BotMain.Plugin;

namespace BotMain;

/// <summary>控制台指令调试器，负责读取并分发控制台输入的指令</summary>
internal static class ConsoleDebugger
{
    /// <summary>启动主循环，阻塞直到输入 exit 指令</summary>
    internal static void Run()
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (string.Equals(input.Trim(), ConsoleConstant.CmdExit, StringComparison.OrdinalIgnoreCase))
                break;

            Dispatch(ParseTokens(input));
        }
    }

    #region 指令分发

    private static void Dispatch(string[] tokens)
    {
        if (tokens.Length == 0)
            return;

        var command = tokens[0].ToLowerInvariant();

        switch (command)
        {
            case ConsoleConstant.CmdSendPrivate:
                ExecuteSendPrivate(tokens);
                break;
            case ConsoleConstant.CmdSendGroup:
                ExecuteSendGroup(tokens);
                break;
            case ConsoleConstant.CmdHotReload:
                ExecuteHotReload(tokens);
                break;
            case ConsoleConstant.CmdPlugin:
                ExecutePlugin(tokens);
                break;
            case ConsoleConstant.CmdDebug:
                PluginManager.Instance.SendDebugCommand(tokens);
                break;
            case ConsoleConstant.CmdHelp:
                ExecuteHelp(tokens);
                break;
            default:
                Console.WriteLine(ConsoleConstant.MsgUnknown);
                break;
        }
    }

    // sendprivate <msg> <uid> [sendImmediately]
    private static void ExecuteSendPrivate(string[] tokens)
    {
        if ((tokens.Length != 3 && tokens.Length != 4)
            || !long.TryParse(tokens[2], out var uid)
            || (tokens.Length == 4 && !bool.TryParse(tokens[3], out _)))
        {
            Console.WriteLine(ConsoleConstant.MsgBadFormat);
            return;
        }
        var sendImmediately = tokens.Length == 4 && bool.Parse(tokens[3]);
        BotCore.SendPrivateMessage(tokens[1], uid, sendImmediately);
    }

    // sendgroup <msg> <gid> [sendImmediately]
    private static void ExecuteSendGroup(string[] tokens)
    {
        if ((tokens.Length != 3 && tokens.Length != 4)
            || !long.TryParse(tokens[2], out var gid)
            || (tokens.Length == 4 && !bool.TryParse(tokens[3], out _)))
        {
            Console.WriteLine(ConsoleConstant.MsgBadFormat);
            return;
        }
        var sendImmediately = tokens.Length == 4 && bool.Parse(tokens[3]);
        BotCore.SendGroupMessage(tokens[1], gid, sendImmediately);
    }

    // hotreload
    private static void ExecuteHotReload(string[] tokens)
    {
        if (tokens.Length != 1)
        {
            Console.WriteLine(ConsoleConstant.MsgBadFormat);
            return;
        }
        if (BotEntry.ReloadConfig())
            BotCore.Logger.Info("[ConsoleDebugger] 配置文件已热重载");
    }

    // plugin list
    // plugin <name> --d
    // plugin <name> --v
    // plugin <name> enable
    // plugin <name> disable
    private static void ExecutePlugin(string[] tokens)
    {
        if (tokens.Length < 2)
        {
            Console.WriteLine(ConsoleConstant.MsgBadFormat);
            return;
        }

        var manager = PluginManager.Instance;

        if (string.Equals(tokens[1], ConsoleConstant.CmdPluginList, StringComparison.OrdinalIgnoreCase))
        {
            var plugins = manager.Plugins;
            if (plugins.Count == 0)
            {
                Console.WriteLine("（无已注册插件）");
                return;
            }
            foreach (var p in plugins)
                Console.WriteLine("[{0}] {1}", p.IsEnabled ? "启用" : "停用", p.PluginName);
            return;
        }

        if (tokens.Length < 3)
        {
            Console.WriteLine(ConsoleConstant.MsgBadFormat);
            return;
        }

        var name = tokens[1];
        var sub = tokens[2];

        switch (sub)
        {
            case ConsoleConstant.CmdPluginDesc:
            {
                var p = manager.Plugins.FirstOrDefault(
                    x => string.Equals(x.PluginName, name, StringComparison.OrdinalIgnoreCase));
                if (p is null) { Console.WriteLine("未找到插件: {0}", name); return; }
                Console.WriteLine(string.IsNullOrEmpty(p.PluginDes) ? "（无描述）" : p.PluginDes);
                break;
            }
            case ConsoleConstant.CmdPluginVer:
            {
                var p = manager.Plugins.FirstOrDefault(
                    x => string.Equals(x.PluginName, name, StringComparison.OrdinalIgnoreCase));
                if (p is null) { Console.WriteLine("未找到插件: {0}", name); return; }
                Console.WriteLine(string.IsNullOrEmpty(p.PluginVersion) ? "（无版本信息）" : p.PluginVersion);
                break;
            }
            case ConsoleConstant.CmdPluginEnable:
                manager.Enable(name);
                break;
            case ConsoleConstant.CmdPluginDisable:
                manager.Disable(name);
                break;
            default:
                Console.WriteLine(ConsoleConstant.MsgBadFormat);
                break;
        }
    }

    // help
    // help <CommandName>
    private static void ExecuteHelp(string[] tokens)
    {
        if (tokens.Length == 1)
        {
            Console.WriteLine("可用指令：");
            foreach (var (cmd, brief) in ConsoleConstant.CommandBriefs)
                Console.WriteLine("  {0,-16}{1}", cmd, brief);
            return;
        }

        if (tokens.Length == 2)
        {
            if (ConsoleConstant.CommandDetails.TryGetValue(tokens[1], out var detail))
                Console.Write(detail);
            else
                Console.WriteLine("未知的指令: {0}", tokens[1]);
            return;
        }

        Console.WriteLine(ConsoleConstant.MsgBadFormat);
    }

    #endregion 指令分发

    #region 指令解析

    /// <summary>
    /// 将输入字符串按空格拆分为 token 列表，双引号包围的部分视为单个 token（内容不含引号）
    /// </summary>
    private static string[] ParseTokens(string input)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < input.Length)
        {
            // 跳过空格
            while (i < input.Length && input[i] == ' ')
                i++;

            if (i >= input.Length)
                break;

            if (input[i] == '"')
            {
                // 带引号的 token：读取到下一个 " 为止
                i++;
                int start = i;
                while (i < input.Length && input[i] != '"')
                    i++;
                tokens.Add(input[start..i]);
                if (i < input.Length)
                    i++; // 跳过闭合 "
            }
            else
            {
                // 普通 token：读取到下一个空格为止
                int start = i;
                while (i < input.Length && input[i] != ' ')
                    i++;
                tokens.Add(input[start..i]);
            }
        }
        return [.. tokens];
    }

    #endregion 指令解析
}
