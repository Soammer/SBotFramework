using System.Reflection;
using System.Runtime.Loader;
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
    private readonly List<PluginLoadContext> _loadContexts = [];
    private string _configPath = string.Empty;

    private const string c_EntryTypeName = "Plugins.Main.MainBehavior";

    /// <summary>已注册的插件列表（含启用与未启用）</summary>
    public IReadOnlyList<PluginInstance> Plugins => _plugins;

    /// <summary>
    /// 扫描指定目录下所有一级子目录，将每个子目录视为一个插件包，
    /// 使用独立的 <see cref="PluginLoadContext"/> 加载其中的 DLL，
    /// 找到满足条件的入口类后注册为 <see cref="PluginInstance"/>。
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

        var subDirs = Directory.GetDirectories(pluginsDir);
        BotCore.Logger.Info("[PluginManager] 扫描插件目录，共发现 {0} 个子目录", subDirs.Length);

        foreach (var subDir in subDirs)
            TryLoadPluginFromDir(subDir);

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

    private void TryLoadPluginFromDir(string subDir)
    {
        var dirName = Path.GetFileName(subDir);
        var dlls = Directory.GetFiles(subDir, "*.dll");

        if (dlls.Length == 0)
        {
            BotCore.Logger.Warning("[PluginManager] 子目录 \"{0}\" 中没有 DLL 文件，已跳过", dirName);
            return;
        }

        var loadContext = new PluginLoadContext(subDir);

        foreach (var dll in dlls)
        {
            if (TryLoadPlugin(dll, loadContext, dirName))
            {
                _loadContexts.Add(loadContext);
                return;
            }
        }

        BotCore.Logger.Warning("[PluginManager] 子目录 \"{0}\" 中未找到有效的插件入口，已跳过", dirName);
    }

    /// <returns>成功注册插件时返回 true</returns>
    private bool TryLoadPlugin(string path, PluginLoadContext loadContext, string dirName)
    {
        var fileName = Path.GetFileName(path);

        Assembly assembly;
        try
        {
            assembly = loadContext.LoadFromAssemblyPath(path);
        }
        catch (Exception ex)
        {
            BotCore.Logger.Warning("[PluginManager] 加载 DLL 失败 \"{0}\": {1}", fileName, ex.Message);
            return false;
        }

        var entryType = assembly.GetType(c_EntryTypeName);

        // 该 DLL 不含入口类，可能是依赖库，静默跳过
        if (entryType is null)
            return false;

        if (entryType.IsAbstract || entryType.IsInterface)
        {
            BotCore.Logger.Warning("[PluginManager] \"{0}\" 的入口类 {1} 不能是抽象类或接口，已跳过", dirName, c_EntryTypeName);
            return false;
        }

        if (!typeof(IPluginMain).IsAssignableFrom(entryType))
        {
            BotCore.Logger.Warning("[PluginManager] \"{0}\" 的入口类 {1} 未实现 IPluginMain，已跳过", dirName, c_EntryTypeName);
            return false;
        }

        if (!entryType.IsDefined(typeof(PluginEntryAttribute), inherit: false))
        {
            BotCore.Logger.Warning("[PluginManager] \"{0}\" 的入口类 {1} 未标注 PluginEntryAttribute，已跳过", dirName, c_EntryTypeName);
            return false;
        }

        var instance = new PluginInstance(entryType);
        _plugins.Add(instance);
        BotCore.Logger.Info("[PluginManager] 已注册插件: {0} ({1}/{2})", instance.PluginName, dirName, fileName);
        return true;
    }

    /// <summary>
    /// 插件隔离加载上下文。优先从插件目录解析依赖，找不到时回退到默认上下文。
    /// 共享程序集（BotMain、NapPlana.NET 等）不应放入插件目录，回退机制确保类型一致性。
    /// </summary>
    private sealed class PluginLoadContext : AssemblyLoadContext
    {
        private readonly string _pluginDir;

        internal PluginLoadContext(string pluginDir) : base(isCollectible: false)
        {
            _pluginDir = pluginDir;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var path = Path.Combine(_pluginDir, assemblyName.Name + ".dll");
            if (File.Exists(path))
                return LoadFromAssemblyPath(path);

            // 返回 null 回退到默认上下文（主程序已加载的程序集）
            return null;
        }
    }

    private sealed class PluginConfigEntry
    {
        public bool Enabled { get; set; }
    }
}
