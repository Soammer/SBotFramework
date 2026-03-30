namespace BotMain.Core;

/// <summary>私聊消息子类型</summary>
public enum PrivateMessageSubType
{
    None,
    Friend,
    Temporary,
    Other
}

/// <summary>群成员角色</summary>
public enum GroupRole
{
    None,
    Owner,
    Admin,
    Member
}

/// <summary>消息数据类型</summary>
public enum MessageDataType
{
    None,
    Text,
    Image,
    Face,
    At,
    Audio,
    Record,
    Video,
    Rps,
    Contact,
    Dice,
    Music,
    Reply,
    Forward,
    Node,
    Json,
    MFace,
    File
}
