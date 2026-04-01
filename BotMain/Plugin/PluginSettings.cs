namespace BotMain.Plugin;

/// <summary>
/// 插件运行时配置，记录当前插件的状态。由 <see cref="PluginInstance"/> 持有并维护。
/// </summary>
public class PluginSettings
{
    /// <summary>插件是否启用。默认 false。请通过 <see cref="PluginInstance.IsEnabled"/> 修改以触发 Init / DeInit 逻辑</summary>
    public bool Enabled { get; internal set; } = false;
}
