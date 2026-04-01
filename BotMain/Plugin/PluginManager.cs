using System.Reflection;
using System.Text.Json;
using BotMain.Core;

namespace BotMain.Plugin;

/// <summary>
/// 插件管理器单例，负责从 plugins 目录扫描并维护 <see cref="PluginInstance"/> 列表
/// </summary>
public sealed class PluginManager
{
    private static readonly PluginManager s_instance = new();
    private static readonly JsonSerializerOptions s_writeOptions = new() { WriteIndented = true };

    /// <summary>插件管理器单例</summary>
    public static PluginManager Instance => s_instance;

    private readonly List<PluginInstance> _plugins = [];
    private string _configPath = string.Empty;

    private PluginManager() { }

    /// <summary>已注册的插件列表（含启用与未启用）</summary>
    public IReadOnlyList<PluginInstance> Plugins => _plugins;

    /// <summary>
    /// 扫描指定目录下的所有 .dll 文件，将其中有且唯一满足条件的插件入口类注册为 <see cref="PluginInstance"/>。
    /// 满足条件：实现 <see cref="IPluginMain"/>、标注 <see cref="PluginEntryAttribute"/>、非抽象非接口。
    /// 注册完成后自动读取同目录下的 config.json，并对标记为启用的插件调用 <see cref="PluginInstance.IsEnabled"/>。
    /// </summary>
    public void LoadAll(string pluginsDir)
    {
        if (!Directory.Exists(pluginsDir))
        {
            Directory.CreateDirectory(pluginsDir);
            BotCore.Logger.Info("[PluginManager] plugins 目录不存在，已自动创建: {0}", pluginsDir);
        }

        _configPath = Path.Combine(pluginsDir, "config.json");

        var dlls = Directory.GetFiles(pluginsDir, "*.dll");
        BotCore.Logger.Info("[PluginManager] 扫描插件目录，共发现 {0} 个 DLL", dlls.Length);

        foreach (var dll in dlls)
            TryLoadPlugin(dll);

        BotCore.Logger.Info("[PluginManager] 插件注册完成，共注册 {0} 个插件", _plugins.Count);

        ApplyConfig();
    }

    /// <summary>
    /// 将所有插件的当前配置写入 plugins/config.json，应在 Bot 关闭前调用。
    /// </summary>
    public void SaveConfig()
    {
        if (string.IsNullOrEmpty(_configPath)) return;

        var config = _plugins.ToDictionary(
            p => p.PluginName,
            p => new PluginConfigEntry { Enabled = p.Settings.Enabled });
        try
        {
            var json = JsonSerializer.Serialize(config, s_writeOptions);
            File.WriteAllText(_configPath, json);
            BotCore.Logger.Info("[PluginManager] 插件配置已保存: {0}", _configPath);
        }
        catch (Exception ex)
        {
            BotCore.Logger.Error("保存插件配置失败: {0}", ex.Message);
        }
    }

    /// <summary>
    /// 启用指定名称的插件。名称不存在时返回 false。
    /// </summary>
    public bool Enable(string pluginName)
    {
        var plugin = FindByName(pluginName);
        if (plugin is null)
        {
            BotCore.Logger.Warning("[PluginManager] 未找到插件: {0}", pluginName);
            return false;
        }
        plugin.IsEnabled = true;
        return true;
    }

    /// <summary>
    /// 停用指定名称的插件。名称不存在时返回 false。
    /// </summary>
    public bool Disable(string pluginName)
    {
        var plugin = FindByName(pluginName);
        if (plugin is null)
        {
            BotCore.Logger.Warning("[PluginManager] 未找到插件: {0}", pluginName);
            return false;
        }
        plugin.IsEnabled = false;
        return true;
    }

    private PluginInstance? FindByName(string pluginName)
        => _plugins.Find(p => string.Equals(p.PluginName, pluginName, StringComparison.OrdinalIgnoreCase));

    /// <summary>由 BotCore.Update 驱动，对所有已启用的插件调用 Update</summary>
    internal void Update()
    {
        foreach (var plugin in _plugins)
            plugin.Update();
    }

    private void ApplyConfig()
    {
        if (!File.Exists(_configPath)) return;

        Dictionary<string, PluginConfigEntry>? config = null;
        try
        {
            var json = File.ReadAllText(_configPath);
            config = JsonSerializer.Deserialize<Dictionary<string, PluginConfigEntry>>(json, BotCore.JsonOptions);
        }
        catch (Exception ex)
        {
            BotCore.Logger.Warning("[PluginManager] 读取插件配置失败，将使用默认状态: {0}", ex.Message);
        }

        if (config is null) return;

        foreach (var plugin in _plugins)
        {
            if (config.TryGetValue(plugin.PluginName, out var entry) && entry.Enabled)
                plugin.IsEnabled = true;
        }
    }

    private void TryLoadPlugin(string path)
    {
        var fileName = Path.GetFileName(path);

        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(path);
        }
        catch (Exception ex)
        {
            BotCore.Logger.Warning("[PluginManager] 加载 DLL 失败 \"{0}\": {1}", fileName, ex.Message);
            return;
        }

        IEnumerable<Type> allTypes;
        try
        {
            allTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // 部分类型依赖缺失时，使用已成功加载的子集继续处理
            BotCore.Logger.Warning("[PluginManager] \"{0}\" 存在部分类型加载失败，将尝试处理可用类型", fileName);
            allTypes = ex.Types.Where(t => t is not null).Cast<Type>();
        }
        catch (Exception ex)
        {
            BotCore.Logger.Warning("[PluginManager] 枚举类型失败 \"{0}\": {1}", fileName, ex.Message);
            return;
        }

        var candidates = allTypes
            .Where(t => !t.IsAbstract
                && !t.IsInterface
                && typeof(IPluginMain).IsAssignableFrom(t)
                && t.IsDefined(typeof(PluginEntryAttribute), inherit: false))
            .ToArray();

        if (candidates.Length == 0)
        {
            BotCore.Logger.Warning("[PluginManager] \"{0}\" 中未找到符合条件的插件入口类，已跳过", fileName);
            return;
        }

        if (candidates.Length > 1)
        {
            BotCore.Logger.Warning(
                "[PluginManager] \"{0}\" 中存在 {1} 个插件入口类，有且唯一时才会加载，已跳过",
                fileName, candidates.Length);
            return;
        }

        var instance = new PluginInstance(candidates[0]);
        _plugins.Add(instance);
        BotCore.Logger.Info("[PluginManager] 已注册插件: {0} ({1})", instance.PluginName, fileName);
    }

    private sealed class PluginConfigEntry
    {
        public bool Enabled { get; set; }
    }
}
