using BotMain.Core;

namespace BotMain.Message;

/// <summary>@消息数据</summary>
public class AtMessageData : MessageDataBase
{
    /// <summary>QQ号或"all"</summary>
    public string Qq = string.Empty;
}

/// <summary>@消息</summary>
public class AtMessage : MessageBase
{
    public override MessageDataType MessageType { get; set; } = MessageDataType.At;
    public override MessageDataBase MessageData { get; set; } = new AtMessageData();
}
