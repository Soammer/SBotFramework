namespace BotMain.Plugin;

/// <summary>
/// 插件主入口接口，配合 <see cref="PluginEntryAttribute"/> 标记的类实现
/// </summary>
public interface IPluginMain
{
    /// <summary>插件加载时调用，用于初始化资源、注册事件等</summary>
    void Init();

    /// <summary>插件卸载时调用，用于释放资源、反注册事件等</summary>
    void DeInit();

    /// <summary>每次 BotCore Update 时调用</summary>
    void Update();

    /// <summary>配置热重载时调用，用于重新读取插件自身的配置</summary>
    void Reload();
}
