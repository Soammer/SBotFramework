using System.Text.Json;
using System.Text.Json.Serialization;
using BotMain.Logging;
using BotMain.Net;
using BotMain.Plugin;
using NapPlana.Core.Bot.BotInstance;
using NapPlana.Core.Data.API;
using NapPlana.Core.Data.Event.Message;
using BotMsg = BotMain.Message;
using NapMsg = NapPlana.Core.Data.Message;

namespace BotMain.Core;

/// <summary>
/// Bot 核心单例，持有全局共享的基础服务实例，并驱动每秒一次的 Update 循环
/// </summary>
public static class BotCore
{
    private static readonly BaseLoggerImp s_logger = new();
    /// <summary>全局日志实例，所有模块统一通过此实例输出日志</summary>
    public static BaseLoggerImp Logger => s_logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    /// <summary>全局 JSON 序列化选项，支持大小写不敏感属性名与字符串枚举</summary>
    public static JsonSerializerOptions JsonOptions => s_jsonOptions;

    /// <summary>全局共享随机数实例</summary>
    public static Random Random => Random.Shared;

    private static NetCenter? s_net;
    private static Timer? s_updateTimer;

    /// <summary>
    /// 初始化 BotCore，绑定 Bot 实例与 NetCenter，并启动每秒一次的 Update 循环
    /// </summary>
    public static void Initialize(NapBot bot, NetCenter net)
    {
        s_net = net;
        s_updateTimer = new Timer(_ => Update(), null,
            dueTime: TimeSpan.FromSeconds(1),
            period: TimeSpan.FromSeconds(1));
    }

    #region Update 循环

    /// <summary>由定时器每秒驱动一次，处理 NetCenter 中积压的消息队列并驱动插件 Update</summary>
    private static void Update()
    {
        s_net?.Update();
        PluginManager.Instance.Update();
    }

    #endregion Update 循环

    #region 消息接收事件

    private static readonly ObjectPool<PrivateMessage> s_privateMessagePool = new(() => new PrivateMessage());
    private static readonly ObjectPool<GroupMessage> s_groupMessagePool = new(() => new GroupMessage());

    private static event Action<PrivateMessage>? s_onPrivateMessage;

    private static event Action<GroupMessage>? s_onGroupMessage;

    /// <summary>注册私聊消息处理器</summary>
    public static void RegisterPrivateMessageHandler(Action<PrivateMessage> handler)
        => s_onPrivateMessage += handler;

    /// <summary>反注册私聊消息处理器</summary>
    public static void UnregisterPrivateMessageHandler(Action<PrivateMessage> handler)
        => s_onPrivateMessage -= handler;

    /// <summary>注册群聊消息处理器</summary>
    public static void RegisterGroupMessageHandler(Action<GroupMessage> handler)
        => s_onGroupMessage += handler;

    /// <summary>反注册群聊消息处理器</summary>
    public static void UnregisterGroupMessageHandler(Action<GroupMessage> handler)
        => s_onGroupMessage -= handler;

    /// <summary>处理接收到的私聊消息，由 NetCenter 内部调用</summary>
    internal static void ProcessPrivateMessage(PrivateMessageEvent msg)
    {
        if (!GlobalSettings.IsPrivateAllowed(msg.UserId)) return;

        var pm = s_privateMessagePool.Rent();
        pm.Initialize(msg.RawMessage, msg.UserId, msg.SubType.ToBotSubType());
        s_onPrivateMessage?.Invoke(pm);
        s_privateMessagePool.Return(pm);
    }

    /// <summary>处理接收到的群聊消息，由 NetCenter 内部调用</summary>
    internal static void ProcessGroupMessage(GroupMessageEvent msg)
    {
        if (!GlobalSettings.IsGroupAllowed(msg.GroupId)) return;

        var gm = s_groupMessagePool.Rent();
        gm.Initialize(msg.RawMessage, msg.UserId, msg.GroupId, msg.Sender.Role.ToBotGroupRole());
        s_onGroupMessage?.Invoke(gm);
        s_groupMessagePool.Return(gm);
    }

    #endregion 消息接收事件

    #region 调试方法

#if DEBUG

    /// <summary>以 Info 日志输出私聊发送参数，不调用 NetCenter 或封装消息</summary>
    public static void DebugPrivateMessage(string msg, long uid, bool sendImmediately = false)
        => Logger.Info("[Debug] 私聊 uid={0} sendImmediately={1} msg={2}", uid, sendImmediately, msg);

    /// <summary>以 Info 日志输出群聊发送参数，不调用 NetCenter 或封装消息</summary>
    public static void DebugGroupMessage(string msg, long gid, bool sendImmediately = false)
        => Logger.Info("[Debug] 群聊 gid={0} sendImmediately={1} msg={2}", gid, sendImmediately, msg);

#endif

    #endregion 调试方法

    #region 消息发送公开接口

    /// <summary>发送私聊文本消息</summary>
    /// <param name="sendImmediately">true 立即发送；false 加入 NetCenter 发送队列</param>
    public static void SendPrivateMessage(string msg, long uid, bool sendImmediately = false)
    {
        if (!GlobalSettings.IsPrivateAllowed(uid)) return;
        var textMsg = new NapMsg.TextMessage();
        ((NapMsg.TextMessageData)textMsg.MessageData).Text = msg;
        SendPrivateCore([textMsg], uid, sendImmediately);
    }

    /// <summary>发送私聊消息</summary>
    /// <param name="sendImmediately">true 立即发送；false 加入 NetCenter 发送队列</param>
    public static void SendPrivateMessage(BotMsg.MessageBase msg, long uid, bool sendImmediately = false)
    {
        if (!GlobalSettings.IsPrivateAllowed(uid)) return;
        SendPrivateCore([msg.ToNapMessage()], uid, sendImmediately);
    }

    /// <summary>发送私聊消息链</summary>
    /// <param name="sendImmediately">true 立即发送；false 加入 NetCenter 发送队列</param>
    public static void SendPrivateMessage(List<BotMsg.MessageBase> msgs, long uid, bool sendImmediately = false)
    {
        if (!GlobalSettings.IsPrivateAllowed(uid)) return;
        SendPrivateCore(msgs.ConvertAll(m => m.ToNapMessage()), uid, sendImmediately);
    }

    private static void SendPrivateCore(List<NapMsg.MessageBase> messages, long uid, bool sendImmediately)
    {
        var send = new PrivateMessageSend { UserId = uid.ToString(), Message = messages };
        if (sendImmediately)
            s_net?.SendPrivateMessage(send);
        else
            s_net?.PushPrivateMessage(send);
    }

    /// <summary>发送群聊文本消息</summary>
    /// <param name="sendImmediately">true 立即发送；false 加入 NetCenter 发送队列</param>
    public static void SendGroupMessage(string msg, long gid, bool sendImmediately = false)
    {
        if (!GlobalSettings.IsGroupAllowed(gid)) return;
        var textMsg = new NapMsg.TextMessage();
        ((NapMsg.TextMessageData)textMsg.MessageData).Text = msg;
        SendGroupCore([textMsg], gid, sendImmediately);
    }

    /// <summary>发送群聊消息</summary>
    /// <param name="sendImmediately">true 立即发送；false 加入 NetCenter 发送队列</param>
    public static void SendGroupMessage(BotMsg.MessageBase msg, long gid, bool sendImmediately = false)
    {
        if (!GlobalSettings.IsGroupAllowed(gid)) return;
        SendGroupCore([msg.ToNapMessage()], gid, sendImmediately);
    }

    /// <summary>发送群聊消息链</summary>
    /// <param name="sendImmediately">true 立即发送；false 加入 NetCenter 发送队列</param>
    public static void SendGroupMessage(List<BotMsg.MessageBase> msgs, long gid, bool sendImmediately = false)
    {
        if (!GlobalSettings.IsGroupAllowed(gid)) return;
        SendGroupCore(msgs.ConvertAll(m => m.ToNapMessage()), gid, sendImmediately);
    }

    private static void SendGroupCore(List<NapMsg.MessageBase> messages, long gid, bool sendImmediately)
    {
        var send = new GroupMessageSend { GroupId = gid.ToString(), Message = messages };
        if (sendImmediately)
            s_net?.SendGroupMessage(send);
        else
            s_net?.PushGroupMessage(send);
    }

    #endregion 消息发送公开接口
}