using System.Reflection;
using BotMain.Core;

namespace BotMain.Plugin;

/// <summary>
/// 插件实例包装，持有插件入口类型、运行时实例与配置。
/// 通过 <see cref="IsEnabled"/> 控制插件的启用与停用。
/// </summary>
public sealed class PluginInstance
{
    private readonly Type _entryType;
    private IPluginMain? _main;

    /// <summary>插件运行时配置，记录当前状态</summary>
    public PluginSettings Settings { get; } = new();

    /// <summary>当前活跃的插件实例；未启用时为 null</summary>
    public IPluginMain? Main => _main;

    /// <summary>插件入口类型的完全限定名</summary>
    public string TypeName => _entryType.FullName ?? _entryType.Name;

    /// <summary>插件名称（来自 <see cref="PluginEntryAttribute.PluginName"/>）</summary>
    public string PluginName { get; }

    /// <summary>插件描述（来自 <see cref="PluginEntryAttribute.PluginDes"/>）</summary>
    public string PluginDes { get; }

    /// <summary>插件版本（来自 <see cref="PluginEntryAttribute.PluginVersion"/>）</summary>
    public string PluginVersion { get; }

    internal PluginInstance(Type entryType)
    {
        _entryType = entryType;
        var attr = entryType.GetCustomAttribute<PluginEntryAttribute>()!;
        PluginName = attr.PluginName;
        PluginDes = attr.PluginDes;
        PluginVersion = attr.PluginVersion;
    }

    /// <summary>
    /// 获取或设置插件启用状态。
    /// 设为 true 时实例化并调用 <see cref="IPluginMain.Init"/>；
    /// 设为 false 时调用 <see cref="IPluginMain.DeInit"/> 并回收实例。
    /// </summary>
    public bool IsEnabled
    {
        get => Settings.Enabled;
        set
        {
            if (Settings.Enabled == value) return;

            if (value)
                Enable();
            else
                Disable();
        }
    }

    private void Enable()
    {
        try
        {
            _main = (IPluginMain)Activator.CreateInstance(_entryType)!;
            _main.Init();
            Settings.Enabled = true;
            BotCore.Logger.Info("[PluginManager] 插件已启用: {0}", PluginName);
        }
        catch (Exception ex)
        {
            _main = null;
            BotCore.Logger.Error("启用插件 \"{0}\" 失败: {1}", PluginName, ex.Message);
        }
    }

    private void Disable()
    {
        Settings.Enabled = false;
        try
        {
            _main?.DeInit();
        }
        catch (Exception ex)
        {
            BotCore.Logger.Error("停用插件 \"{0}\" 时 DeInit 抛出异常: {1}", PluginName, ex.Message);
        }
        finally
        {
            _main = null;
            BotCore.Logger.Info("[PluginManager] 插件已停用: {0}", PluginName);
        }
    }

    /// <summary>由 PluginManager 每次 Update 时调用，仅在启用状态下驱动插件</summary>
    internal void Update()
    {
        if (!Settings.Enabled) return;
        try
        {
            _main?.Update();
        }
        catch (Exception ex)
        {
            BotCore.Logger.Error("插件 \"{0}\" Update 抛出异常: {1}", PluginName, ex.Message);
        }
    }
}
