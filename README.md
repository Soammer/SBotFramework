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

- `sendImmediately`：`true` 立即发送，`false`（默认）加入延迟队列

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
