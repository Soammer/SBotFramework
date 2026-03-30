using System.Collections.Concurrent;
using BotMain.Core;
using NapPlana.Core.Bot.BotInstance;
using NapPlana.Core.Data.API;
using NapPlana.Core.Data.Event.Message;
using NapPlana.Core.Event.Handler;

namespace BotMain.Net;

/// <summary>
/// 网络中心，统一管理消息的接收与发送，所有消息均经过队列缓冲后批量处理
/// </summary>
public sealed class NetCenter
{
    private readonly NapBot _bot;

    private readonly MessageQueue<PrivateMessageEvent> _receivePrivate;
    private readonly MessageQueue<GroupMessageEvent> _receiveGroup;
    private readonly MessageQueue<PrivateMessageSend> _sendPrivate;
    private readonly MessageQueue<GroupMessageSend> _sendGroup;

    /// <summary>私聊消息到达通知，由 BotCore.ProcessPrivateMessage 调用</summary>
    public Action<PrivateMessageEvent>? OnPrivateMessageReceived;

    /// <summary>群聊消息到达通知，由 BotCore.ProcessGroupMessage 调用</summary>
    public Action<GroupMessageEvent>? OnGroupMessageReceived;

    /// <param name="bot">NapBot 实例，用于实际发送消息</param>
    /// <param name="minBatchSize">队列一次性最小处理数量；低于此值时立即处理，达到时等待下次 Update 批量处理</param>
    /// <param name="cacheThreshold">队列缓存阈值；超出时输出警告</param>
    public NetCenter(NapBot bot, int minBatchSize = 5, int cacheThreshold = 100)
    {
        _bot = bot;

        _receivePrivate = new MessageQueue<PrivateMessageEvent>(
            "接收-私聊", minBatchSize, cacheThreshold,
            msg => BotCore.ProcessPrivateMessage(msg));

        _receiveGroup = new MessageQueue<GroupMessageEvent>(
            "接收-群聊", minBatchSize, cacheThreshold,
            msg => BotCore.ProcessGroupMessage(msg));

        _sendPrivate = new MessageQueue<PrivateMessageSend>(
            "发送-私聊", minBatchSize, cacheThreshold,
            msg => SendPrivateMessage(msg));

        _sendGroup = new MessageQueue<GroupMessageSend>(
            "发送-群聊", minBatchSize, cacheThreshold,
            msg => SendGroupMessage(msg));

        BotEventHandler.OnPrivateMessageReceived += ev => _receivePrivate.Enqueue(ev);
        BotEventHandler.OnGroupMessageReceived += ev => _receiveGroup.Enqueue(ev);
    }

    #region 入队接口

    /// <summary>将私聊消息加入发送队列</summary>
    public void PushPrivateMessage(PrivateMessageSend msg)
    {
        _sendPrivate.Enqueue(msg);
    }

    /// <summary>将群聊消息加入发送队列</summary>
    public void PushGroupMessage(GroupMessageSend msg)
    {
        _sendGroup.Enqueue(msg);
    }

    #endregion 入队接口

    #region 发送实现

    /// <summary>在随机延迟后发送私聊消息，延迟范围为 [1, MaxSendDelaySeconds] 秒</summary>
    internal void SendPrivateMessage(PrivateMessageSend msg)
    {
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(BotCore.Random.Next(1, GlobalSettings.MaxSendDelaySeconds + 1)));
            await _bot.SendPrivateMessageAsync(msg, GlobalSettings.SendTimeoutSeconds);
        });
    }

    /// <summary>在随机延迟后发送群聊消息，延迟范围为 [1, MaxSendDelaySeconds] 秒</summary>
    internal void SendGroupMessage(GroupMessageSend msg)
    {
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(BotCore.Random.Next(1, GlobalSettings.MaxSendDelaySeconds + 1)));
            await _bot.SendGroupMessageAsync(msg, GlobalSettings.SendTimeoutSeconds);
        });
    }

    #endregion 发送实现

    #region Update

    /// <summary>每次 BotCore.Update 时调用，批量处理积压消息</summary>
    internal void Update()
    {
        _receivePrivate.Update();
        _receiveGroup.Update();
        _sendPrivate.Update();
        _sendGroup.Update();
    }

    #endregion Update
}

/// <summary>
/// 消息缓冲队列。
/// 入队时：若当前积压量 小于 minBatchSize 则立即处理；否则等待下次 Update。
/// Update 时：若积压量 大于等于 minBatchSize 则一次性处理 minBatchSize 条。
/// 任何时刻积压量超过 cacheThreshold 时输出警告。
/// </summary>
internal sealed class MessageQueue<T>(string name, int minBatchSize, int cacheThreshold, Action<T> process)
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly int _minBatchSize = minBatchSize;
    private readonly int _cacheThreshold = cacheThreshold;
    private readonly string _name = name;
    private readonly Action<T> _process = process;

    #region 队列操作

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);

        var count = _queue.Count;
        if (count > _cacheThreshold)
            BotCore.Logger.Warning("[NetCenter] 队列\"{0}\"超出缓存阈值 ({1}/{2})", _name, count, _cacheThreshold);

        if (count < _minBatchSize)
            ProcessBatch(_minBatchSize);
        // else: 积压足够多，等待 Update 批量处理
    }

    public void Update()
    {
        if (_queue.Count >= _minBatchSize)
            ProcessBatch(_minBatchSize);
    }

    private void ProcessBatch(int maxCount)
    {
        var processed = 0;
        while (processed < maxCount && _queue.TryDequeue(out var item))
        {
            _process(item);
            processed++;
        }
    }

    #endregion 队列操作
}