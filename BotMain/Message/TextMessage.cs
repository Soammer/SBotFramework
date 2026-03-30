using BotMain.Core;

namespace BotMain.Message;

/// <summary>文本消息数据</summary>
public class TextMessageData : MessageDataBase
{
    public string Text = string.Empty;
}

/// <summary>文本消息</summary>
public class TextMessage : MessageBase
{
    public override MessageDataType MessageType { get; set; } = MessageDataType.Text;
    public override MessageDataBase MessageData { get; set; } = new TextMessageData();
}
