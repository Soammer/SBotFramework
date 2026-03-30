using BotMain.Core;

namespace BotMain.Message;

/// <summary>回复消息数据</summary>
public class ReplyMessageData : MessageDataBase
{
    public string Id = string.Empty;
}

/// <summary>回复消息</summary>
public class ReplyMessage : MessageBase
{
    public override MessageDataType MessageType { get; set; } = MessageDataType.Reply;
    public override MessageDataBase MessageData { get; set; } = new ReplyMessageData();
}
