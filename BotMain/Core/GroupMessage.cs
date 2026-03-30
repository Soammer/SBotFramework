namespace BotMain.Core;

/// <summary>
/// 经过提取的群聊消息，供业务逻辑使用。
/// 警告：不要在消息处理完毕后持有此对象的引用，对象将回池并被覆写。
/// </summary>
public class GroupMessage
{
    private string _message = string.Empty;
    private long _uid;
    private long _gid;
    private GroupRole _roleLevel;

    public string Message => _message;
    public long Uid => _uid;
    public long Gid => _gid;
    public GroupRole RoleLevel => _roleLevel;

    internal GroupMessage() { }

    public GroupMessage(string message, long uid, long gid, GroupRole roleLevel)
    {
        _message = message;
        _uid = uid;
        _gid = gid;
        _roleLevel = roleLevel;
    }

    internal void Initialize(string message, long uid, long gid, GroupRole roleLevel)
    {
        _message = message;
        _uid = uid;
        _gid = gid;
        _roleLevel = roleLevel;
    }
}
