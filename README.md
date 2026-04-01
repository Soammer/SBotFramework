# BotMain

基于 .NET 9 的 QQ Bot 框架，通过 WebSocket 连接 [NapCat](https://github.com/NapNeko/NapCatQQ) 实现消息收发。

> 本项目的框架设计继承自 [喵喵的游戏框架：QQBot](https://github.com/Soammer/QQBot.git)（本人的私有库），并由 ClaudeCode 翻新代码。

---

## 环境要求

- .NET 9 SDK
- 已运行并配置好的 NapCat 实例（需开启 WebSocket 服务）

---

## 快速开始

### 1. 克隆仓库

```bash
git clone <repo-url>
cd BotMain
git submodule update --init
```

### 2. 配置

首次构建后，在输出目录下会自动生成 `config/appsettings.json`，按需修改：

```json
{
  "NapCatConfig": {
    "IP": "127.0.0.1",
    "Port": 6100,
    "Token": ""
  },
  "BotConfig": {
    "SelfId": 0
  },
  "BotSettings": {
    "MinLogLevel": "Info",
    "EnableLogColor": false,
    "MaxSendDelaySeconds": 5,
    "SendTimeoutSeconds": 15
  },
  "FilterSettings": {
    "PrivateList": [],
    "PrivateListIsBlacklist": false,
    "GroupList": [],
    "GroupListIsBlacklist": false
  }
}
```

| 字段 | 说明 | 默认值 |
|------|------|--------|
| `NapCatConfig.IP` | NapCat WebSocket 服务地址 | `127.0.0.1` |
| `NapCatConfig.Port` | NapCat WebSocket 服务端口 | `6100` |
| `NapCatConfig.Token` | 鉴权 Token（NapCat 未设置时留空） | 空 |
| `BotConfig.SelfId` | Bot 的 QQ 号 | `0` |
| `BotSettings.MinLogLevel` | 日志最低输出级别（`Debug` / `Info` / `Warning` / `Error`） | `Info` |
| `BotSettings.EnableLogColor` | 控制台日志是否着色 | `false` |
| `BotSettings.MaxSendDelaySeconds` | 消息发送随机延迟上限（秒），实际延迟在 \[1, 此值\] 之间随机 | `5` |
| `BotSettings.SendTimeoutSeconds` | 消息发送超时时长（秒） | `15` |
| `FilterSettings.PrivateList` | 私聊用户 ID 名单（QQ 号数组） | `[]` |
| `FilterSettings.PrivateListIsBlacklist` | `false` = 白名单；`true` = 黑名单 | `false` |
| `FilterSettings.GroupList` | 群聊 ID 名单（群号数组） | `[]` |
| `FilterSettings.GroupListIsBlacklist` | `false` = 白名单；`true` = 黑名单 | `false` |

**名单行为说明：**
- **白名单**（默认）：名单为空时放行所有；名单非空时仅处理/发送名单内的私聊用户或群
- **黑名单**：拦截名单内的私聊用户或群，其余放行
- 过滤作用于接收（`ProcessPrivateMessage` / `ProcessGroupMessage`）与发送（`SendPrivateMessage` / `SendGroupMessage`）双侧

### 3. 构建与运行

```bash
# 构建
dotnet build BotMain/BotMain.csproj

# 运行
dotnet run --project BotMain/BotMain.csproj
```

启动后 Bot 将自动连接 NapCat。在控制台输入 `Exit` 并回车以停止。

---

## 控制台指令

Bot 运行期间可在控制台输入指令进行调试。指令按空格拆分参数，含空格的参数用双引号括起。

### 语法说明

```
指令名 参数1 "含空格的参数" 参数3
```

### 内置指令

| 指令 | 语法 | 说明 |
|------|------|------|
| `Exit` | `Exit` | 停止 Bot |
| `SendPrivate` | `SendPrivate <msg> <uid> [sendImmediately]` | 向指定 QQ 发送私聊消息 |
| `SendGroup` | `SendGroup <msg> <gid> [sendImmediately]` | 向指定群发送群聊消息 |
| `HotReload` | `HotReload` | 重新读取 `config/appsettings.json` 并应用到运行时配置 |
| `Plugin list` | `Plugin list` | 列出所有已注册插件及启用状态 |
| `Plugin <name> --d` | `Plugin <name> --d` | 显示指定插件的描述 |
| `Plugin <name> --v` | `Plugin <name> --v` | 显示指定插件的版本 |
| `Plugin <name> enable` | `Plugin <name> enable` | 启用指定插件（调用 Init） |
| `Plugin <name> disable` | `Plugin <name> disable` | 停用指定插件（调用 DeInit） |

- `sendImmediately`：`true` 立即发送，`false`（默认）加入延迟队列
- `HotReload` 失败时保持原有配置不变，错误信息会输出到日志
- Plugin 指令中 `<name>` 为 `[PluginEntry]` 中声明的 `PluginName`，大小写不敏感

**示例：**

```
SendPrivate "你好" 123456789 true
SendGroup 早安 987654321
```

---

## 开发接入

### 接收消息

通过 `BotCore` 注册处理器，在 Bot 收到消息时回调。

```csharp
// 注册私聊消息处理器
BotCore.RegisterPrivateMessageHandler(OnPrivateMessage);

// 注册群聊消息处理器
BotCore.RegisterGroupMessageHandler(OnGroupMessage);

void OnPrivateMessage(PrivateMessage msg)
{
    // msg.Message  —— 消息文本
    // msg.Uid      —— 发送者 QQ 号
    // msg.SubType  —— Friend / Temporary / Other
}

void OnGroupMessage(GroupMessage msg)
{
    // msg.Message   —— 消息文本
    // msg.Uid       —— 发送者 QQ 号
    // msg.Gid       —— 群号
    // msg.RoleLevel —— Owner / Admin / Member
}
```

> **注意**：`PrivateMessage` 与 `GroupMessage` 对象由内部对象池管理，处理器返回后对象将被回收复用。**不要在处理器外持有这两个对象的引用。**

反注册：

```csharp
BotCore.UnregisterPrivateMessageHandler(OnPrivateMessage);
BotCore.UnregisterGroupMessageHandler(OnGroupMessage);
```

---

### 插件开发

Bot 支持通过插件扩展功能。插件以独立 DLL 的形式放置在输出目录的 `plugins/` 文件夹中（每次构建会自动创建该文件夹）。Bot 启动后自动扫描加载，关闭时自动保存配置。

#### 实现插件入口

插件入口类需：

1. 实现 `BotMain.Plugin.IPluginMain` 接口
2. 标注 `[PluginEntry(pluginName, pluginDes, pluginVersion)]` Attribute

```csharp
using BotMain.Plugin;

[PluginEntry("MyPlugin", pluginDes: "示例插件", pluginVersion: "1.0.0")]
public class MyPlugin : IPluginMain
{
    public void Init()
    {
        // 注册事件、初始化资源
        BotCore.RegisterGroupMessageHandler(OnGroupMessage);
    }

    public void DeInit()
    {
        // 反注册事件、释放资源
        BotCore.UnregisterGroupMessageHandler(OnGroupMessage);
    }

    public void Update()
    {
        // 随 BotCore 每秒 Update 一次
    }

    public void Reload()
    {
        // 热重载时重新读取插件自身配置
    }

    private void OnGroupMessage(GroupMessage msg) { }
}
```

#### PluginEntryAttribute 参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `pluginName` | `string` | 是 | 插件名称，用于控制台指令寻址及 config.json key |
| `pluginDes` | `string` | 否 | 插件描述，可通过 `Plugin <name> --d` 查看 |
| `pluginVersion` | `string` | 否 | 插件版本，可通过 `Plugin <name> --v` 查看 |

#### IPluginMain 方法说明

| 方法 | 调用时机 |
|------|----------|
| `Init()` | 插件被启用时（`IsEnabled = true`） |
| `DeInit()` | 插件被停用时（`IsEnabled = false`） |
| `Update()` | 每秒一次（随 BotCore Update，仅启用状态下调用） |
| `Reload()` | 执行 `HotReload` 指令时 |

#### 插件生命周期与配置持久化

- 插件注册后默认**停用**，需通过 `Plugin <name> enable` 或修改 `plugins/config.json` 启用
- `plugins/config.json` 在 Bot 启动时自动读取，关闭时自动写回，格式如下：

```json
{
  "MyPlugin": {
    "Enabled": true
  }
}
```

- 运行时也可通过 `PluginManager.Instance` 编程控制：

```csharp
PluginManager.Instance.Enable("MyPlugin");
PluginManager.Instance.Disable("MyPlugin");

// 遍历所有插件
foreach (var plugin in PluginManager.Instance.Plugins)
{
    Console.WriteLine($"{plugin.PluginName} - 启用: {plugin.IsEnabled}");
}
```

---

### 发送消息

`BotCore` 提供三种形式的发送接口，私聊与群聊各有对应重载。`sendImmediately` 参数可选，默认 `false`（加入延迟队列）。

#### 发送纯文本

```csharp
BotCore.SendPrivateMessage("你好", uid);
BotCore.SendGroupMessage("大家好", gid, sendImmediately: true);
```

#### 发送单条消息对象

```csharp
var msg = new TextMessage();
((TextMessageData)msg.MessageData).Text = "你好";

BotCore.SendPrivateMessage(msg, uid);
```

#### 发送消息链

```csharp
var chain = new List<MessageBase>
{
    new ReplyMessage { MessageData = new ReplyMessageData { Id = "123456" } },
    new TextMessage { MessageData = new TextMessageData { Text = "回复内容" } },
};

BotCore.SendGroupMessage(chain, gid);
```

---

### 消息类型

所有消息类型位于 `BotMain.Message` 命名空间。通过设置对应 `MessageData` 的字段构造消息。

| 类型 | Data 类 | 主要字段 |
|------|---------|---------|
| `TextMessage` | `TextMessageData` | `Text`（消息文本） |
| `AtMessage` | `AtMessageData` | `Qq`（目标 QQ 号或 `"all"`） |
| `ReplyMessage` | `ReplyMessageData` | `Id`（被回复的消息 ID） |
| `ImageMessage` | `ImageMessageData` | `File`（文件路径/URL/Base64）、`Url`、`Name` 等 |

**构造示例：**

```csharp
// @ 某人
var at = new AtMessage();
((AtMessageData)at.MessageData).Qq = "123456789";

// 回复消息
var reply = new ReplyMessage();
((ReplyMessageData)reply.MessageData).Id = "消息ID";

// 发送图片（本地路径）
var image = new ImageMessage();
((ImageMessageData)image.MessageData).File = "file:///C:/image.png";
```
