using System.Text.Json;
using BotMain.Core;
using BotMain.Net;
using BotMain.Plugin;
using NapPlana.Core.Bot;
using NapPlana.Core.Bot.BotInstance;
using NapPlana.Core.Data;
using NapPlana.Core.Event.Handler;

namespace BotMain;

public class BotEntry
{
    private static string s_configPath = string.Empty;

    /// <summary>
    /// 热重载配置文件，重新读取并应用 GlobalSettings。
    /// 返回 true 表示成功，false 表示失败（失败时 GlobalSettings 保持原值不变）。
    /// </summary>
    internal static bool ReloadConfig()
    {
        try
        {
            var json = File.ReadAllText(s_configPath);
            var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettingsJson>(json, BotCore.JsonOptions)
                ?? throw new InvalidOperationException("配置文件内容为空");
            GlobalSettings.Load(settings.BotSettings, settings.FilterSettings);
            return true;
        }
        catch (Exception ex)
        {
            BotCore.Logger.Error("热重载配置文件失败: {0}", ex.Message);
            return false;
        }
    }

    public static async Task Main(string[] args)
    {
        // ── 读取配置 ──────────────────────────────────────────────────────────
        AppSettingsJson appSettings;
        try
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "config", "appsettings.json");
            var json = await File.ReadAllTextAsync(configPath);
            appSettings = JsonSerializer.Deserialize<AppSettingsJson>(json, BotCore.JsonOptions)
                ?? throw new InvalidOperationException("配置文件内容为空");
        }
        catch (Exception ex)
        {
            // 此时 GlobalSettings 尚未加载，直接用 Logger 默认设置输出
            BotCore.Logger.Error("读取配置文件失败: {0}", ex.Message);
            return;
        }

        // GlobalSettings 加载必须在任何日志输出前完成，以便后续日志遵守设置
        GlobalSettings.Load(appSettings.BotSettings, appSettings.FilterSettings);

        var ip = appSettings.NapCatConfig?.IP ?? "127.0.0.1";
        var port = appSettings.NapCatConfig?.Port ?? 6100;
        var token = appSettings.NapCatConfig?.Token;
        var selfId = appSettings.BotConfig?.SelfId ?? 0L;

        // ── 构建 Bot ──────────────────────────────────────────────────────────
        NapBot bot;
        try
        {
            bot = PlanaBotFactory
                .Create()
                .SetSelfId(selfId)
                .SetConnectionType(BotConnectionType.WebSocketClient)
                .SetIp(ip)
                .SetPort(port)
                .SetToken(token)
                .Build();
        }
        catch (Exception ex)
        {
            BotCore.Logger.Error("Bot构建失败: {0}", ex.Message);
            return;
        }

        // ── 初始化 NetCenter 与 BotCore ───────────────────────────────────────
        var netCenter = new NetCenter(bot);
        BotCore.Initialize(bot, netCenter);

        // ── 注册日志转发 ──────────────────────────────────────────────────────
        BotEventHandler.OnLogReceived += (level, message) =>
        {
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    BotCore.Logger.Info("[NapBot] {0}", message);
                    break;

                case LogLevel.Warning:
                    BotCore.Logger.Warning("[NapBot] {0}", message);
                    break;

                case LogLevel.Error:
                    BotCore.Logger.Error("[NapBot] {0}", message);
                    break;
            }
        };

        // ── 注册事件 ──────────────────────────────────────────────────────────
        BotEventHandler.OnBotConnected += () =>
        {
            BotCore.Logger.Info("=== Bot已连接到NapCat服务器 ===");
        };

        // ── 启动 Bot ──────────────────────────────────────────────────────────
        BotCore.Logger.Info("=== BotMain 启动中 ===");
        BotCore.Logger.Info("输入 \"Exit\" 并回车以停止Bot");

        try
        {
            await bot.StartAsync();
        }
        catch (Exception ex)
        {
            BotCore.Logger.Error("Bot启动失败: {0}", ex.Message);
            return;
        }

        // ── 加载插件 ──────────────────────────────────────────────────────────
        var pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");
        PluginManager.Instance.LoadAll(pluginsDir);

        // ── 主循环 ────────────────────────────────────────────────────────────
        s_configPath = Path.Combine(AppContext.BaseDirectory, "config", "appsettings.json");
        ConsoleDebugger.Run();

        // ── 停止 Bot ──────────────────────────────────────────────────────────
        PluginManager.Instance.SaveConfig();
        BotCore.Logger.Info("=== 正在停止Bot... ===");
        await bot.StopAsync();
        BotCore.Logger.Info("=== BotMain 已退出 ===");
    }
}

internal class AppSettingsJson
{
    public NapCatConfigJson? NapCatConfig { get; init; }
    public BotConfigJson? BotConfig { get; init; }
    public BotSettingsJson? BotSettings { get; init; }
    public FilterSettingsJson? FilterSettings { get; init; }
}

internal class NapCatConfigJson
{
    public string IP { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 6100;
    public string? Token { get; init; }
}

internal class BotConfigJson
{
    public long SelfId { get; init; }
}