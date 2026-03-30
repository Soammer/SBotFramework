namespace BotMain.Core;

/// <summary>
/// 经过提取的私聊消息，供业务逻辑使用。
/// 警告：不要在消息处理完毕后持有此对象的引用，对象将回池并被覆写。
/// </summary>
public class PrivateMessage
{
    private string _message = string.Empty;
    private long _uid;
    private PrivateMessageSubType _subType;

    public string Message => _message;
    public long Uid => _uid;
    public PrivateMessageSubType SubType => _subType;

    internal PrivateMessage() { }

    public PrivateMessage(string message, long uid, PrivateMessageSubType subType)
    {
        _message = message;
        _uid = uid;
        _subType = subType;
    }

    internal void Initialize(string message, long uid, PrivateMessageSubType subType)
    {
        _message = message;
        _uid = uid;
        _subType = subType;
    }
}
