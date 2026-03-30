using BotMain.Core;

namespace BotMain.Message;

/// <summary>消息基类</summary>
public class MessageBase
{
    public virtual MessageDataType MessageType { get; set; }
    public virtual MessageDataBase MessageData { get; set; } = new();
}

/// <summary>所有消息数据基类</summary>
public class MessageDataBase { }
