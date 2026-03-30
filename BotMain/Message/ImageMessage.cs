using BotMain.Core;

namespace BotMain.Message;

/// <summary>图片消息数据</summary>
public class ImageMessageData : MessageDataBase
{
    public string? Name;
    public string? Summary;
    public string File = string.Empty;
    public string? SubType;
    public string? FileId;
    public string? Url;
    public string? Path;
    public string? FileSize;
    public string? FileUnique;
}

/// <summary>图片消息</summary>
public class ImageMessage : MessageBase
{
    public override MessageDataType MessageType { get; set; } = MessageDataType.Image;
    public override MessageDataBase MessageData { get; set; } = new ImageMessageData();
}
