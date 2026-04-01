namespace BotMain.Plugin;

/// <summary>
/// 标记一个类为插件入口，该类必须实现 <see cref="IPluginMain"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginEntryAttribute(string pluginName, string pluginDes = "", string pluginVersion = "") : Attribute
{
    /// <summary>插件名称，用于控制台指令寻址</summary>
    public string PluginName { get; } = pluginName;

    /// <summary>插件描述</summary>
    public string PluginDes { get; } = pluginDes;

    /// <summary>插件版本</summary>
    public string PluginVersion { get; } = pluginVersion;
}
