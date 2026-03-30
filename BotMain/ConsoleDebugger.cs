using BotMain.Core;

namespace BotMain;

/// <summary>控制台指令调试器，负责读取并分发控制台输入的指令</summary>
internal static class ConsoleDebugger
{
    private const string c_ExitCommand = "Exit";
    private const string c_MsgUnknown = "未知的指令";
    private const string c_MsgBadFormat = "错误的格式或语法";

    /// <summary>启动主循环，阻塞直到输入 Exit 指令</summary>
    internal static void Run()
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (string.Equals(input.Trim(), c_ExitCommand, StringComparison.OrdinalIgnoreCase))
                break;

            Dispatch(ParseTokens(input));
        }
    }

    #region 指令分发

    private static void Dispatch(string[] tokens)
    {
        if (tokens.Length == 0)
            return;

        switch (tokens[0])
        {
            case "SendPrivate":
                ExecuteSendPrivate(tokens);
                break;
            case "SendGroup":
                ExecuteSendGroup(tokens);
                break;
            default:
                Console.WriteLine(c_MsgUnknown);
                break;
        }
    }

    // SendPrivate <msg> <uid> [sendImmediately]
    private static void ExecuteSendPrivate(string[] tokens)
    {
        if ((tokens.Length != 3 && tokens.Length != 4)
            || !long.TryParse(tokens[2], out var uid)
            || (tokens.Length == 4 && !bool.TryParse(tokens[3], out _)))
        {
            Console.WriteLine(c_MsgBadFormat);
            return;
        }
        var sendImmediately = tokens.Length == 4 && bool.Parse(tokens[3]);
        BotCore.SendPrivateMessage(tokens[1], uid, sendImmediately);
    }

    // SendGroup <msg> <gid> [sendImmediately]
    private static void ExecuteSendGroup(string[] tokens)
    {
        if ((tokens.Length != 3 && tokens.Length != 4)
            || !long.TryParse(tokens[2], out var gid)
            || (tokens.Length == 4 && !bool.TryParse(tokens[3], out _)))
        {
            Console.WriteLine(c_MsgBadFormat);
            return;
        }
        var sendImmediately = tokens.Length == 4 && bool.Parse(tokens[3]);
        BotCore.SendGroupMessage(tokens[1], gid, sendImmediately);
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
