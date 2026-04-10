namespace BotMain.Plugin;

/// <summary>
/// 插件主入口接口，配合 <see cref="PluginEntryAttribute"/> 标记的类实现
/// </summary>
public interface IPluginMain
{
    /// <summary>插件加载时调用，用于初始化资源、注册事件等</summary>
    /// <param name="pluginId">插件唯一 ID，可通过 <see cref="PluginManager.GetPluginById"/> / <see cref="PluginManager.GetPluginDir"/> 查询实例与路径</param>
    void Init(int pluginId);

    /// <summary>插件卸载时调用，用于释放资源、反注册事件等</summary>
    void DeInit();

    /// <summary>每次 BotCore Update 时调用</summary>
    void Update();

    /// <summary>配置热重载时调用，用于重新读取插件自身的配置</summary>
    void Reload();

    /// <summary>控制台输入 debug 指令时调用，tokens[0] 固定为 "debug"</summary>
    void ReceiveDebugCommand(string[] tokens);
}
