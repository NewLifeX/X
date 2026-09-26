# 生态迁移：NewLife.Remoting 适配 v12

> 背景：v12 编解码器单栈化删除了 `NewLife.Net.Handlers` 处理器体系（`Handler`/`IHandlerContext`/`IPipelineHandler`/`StandardCodec`）与 `NewLife.Data.Packet` 类，Remoting 的公共编解码层需要迁移。
> 本文是迁移方案与决策记录，供 Remoting 库本体收口（记忆中的 D4）拍板后执行。

## 1. 精确迁移面

在**干净 HEAD（`56ac9e7`）+ v12 Core 源码**下构建：**20 条错误 / 6 个文件**（净空构建环境：`C:\X\rem-head`，已把 csproj 换成 ProjectReference 本地源码 + 补 net4x `System.Net.Http`/`System.Web`）。

| 文件 | 错误 | 性质 |
|------|------|------|
| `ApiHost.cs` | `using NewLife.Net.Handlers;`、`IPipelineHandler GetMessageCodec() => new StandardCodec{...}` | 契约改名 |
| `IApiHost.cs` | `IPipelineHandler GetMessageCodec();` | 契约改名 |
| `Http/HttpMessage.cs` | 不实现 `IMessage` 的 `Body`/`Kind`/`SetBody`/`BindBody` | 消息契约重建 |
| `Http/HttpCodec.cs` | `: Handler`、2 处 `IHandlerContext` 重写 | 处理器 → 帧协议 |
| `Http/WebSocketClientCodec.cs` | `: Handler`、4 处 `IHandlerContext` | 处理器 → 帧协议 |
| `Http/WebSocketServerCodec.cs` | `: Handler`、3 处 `IHandlerContext`、`NewLife.Http.WebSocketMessageType` 位置变化 | 处理器 → 帧协议 |

**连带改动（无编译错误但必须同步）**：`ApiNetServer.cs:40-43`、`ApiClient.cs:616/628`、`WsClient.cs:275/397`、`XUnitTest/ApiHostTests.cs:76-82`。

> ⚠️ 排错提醒：直接在 `C:\X\NewLife.Remoting` 工作区构建会看到 **360 个错误**——其中 288 个来自工作区里**过期的 09-18 半成品改动**（往 `HttpMessage`/3 个 codec 里加 `ReadFrame`/`Build` 重写，而 v12 已改成 `IMessageCodec`），另 18 个来自临时 csproj 缺 net4x 框架引用。**不要以 360 为工作量依据。**

## 2. 三块工作的定形程度

### 2.1 已定形：消息契约（`HttpMessage`，可照抄 Core 模式）

v12 的消息类自行承担协议字段读写，由 codec 委托调用——现成范本是 `Messaging/WsMessage.cs` + `Messaging/WebSocketCodec.cs`：

- `HttpMessage : Message`（基类已提供 `Kind`/`Reply`/`OneWay`/`Payload`/`SetBody`/`BindBody`，删掉自实现的这几个成员）；
- 新增 `TryParse(ReadOnlySequence<Byte>, out bodyLength, out headerSize, out invalid)`（解析请求行/状态行 + 头 + `Content-Length`，**头部不足返回 false**，长度非法置 `invalid`）；
- 新增 `WriteHeader`（写请求行/状态行与头）；
- `Dispose` 改 `protected override Dispose(Boolean)`（先释放 `Header` 再 `base`）。

### 2.2 已定形：HTTP 帧协议（`HttpCodec : IMessageCodec`）

按 `IMessageCodec` 三成员实现，无状态：`TryParse` 委托 `HttpMessage.TryParse` 产出 `ParseResult{HeaderSize, BodyLength}`（体未到齐用 `IsFrameComplete=false`，让帧泵走流式体）；`Build` 组响应整帧；`BuildHeader` 仅头 + 声明体长。

**唯一没有归宿的是旧代码里的"首包必须像 HTTP 请求"的每连接判定**（`ext["Encoder"] is not HttpEncoder`）——无状态 codec 不能持有该状态，需上移到会话层（见 2.3）。

### 2.3 需要决策：`ApiNetServer` 的"一端口三协议"

旧实现靠 `NetServer.Add(handler)` 挂**每连接处理器链**：

```csharp
Add(new WebSocketServerCodec { Server = "ApiServer", Protocol = "SRMP" });
Add(new HttpCodec { AllowParseHeader = true, JsonHost = json });
Add(Host.GetMessageCodec());      // StandardCodec
```

v12 只有**单一 `SessionBase.Protocol`**（`IMessageCodec`），没有链、没有 per-connection 追加。而 Remoting 的产品描述就是「提供 RPC、HTTP、WebSocket 与 SRMP 统一通信能力」——**一个端口同时接 WS 握手 / HTTP 请求 / SRMP 帧**是它的卖点，不是可有可无的细节。

再加一条硬约束：Core `Messaging/WebSocketCodec` 的注释明确写着——

> **不要直接用作 `SessionBase.Protocol`**：本 codec 不做掩码解码，会话层也没有解码时机……WebSocket 通道请走 `Http/WebSocket`（服务端）与 `WebSocketClient`（客户端），两者在交付前完成解码与分片重组。

即 **v12 把 WS 握手与掩码解码收进了 Net/Http 层**（"握手异步化进入打开链路"），Remoting 自己那套 WS codec 在 v12 里没有位置。

## 3. 三个可选实现（需要拍板）

| 方案 | 做法 | 优点 | 代价 |
|------|------|------|------|
| **A（推荐）** | Remoting 只保留 SRMP 作为 `Protocol`；HTTP/WS 交给 Core 的 `HttpServer` + `WebSocketHandler`（同端口监听 + 101 升级） | 与 v12 单栈化方向一致，删除自研 WS codec 与握手逻辑，长期维护成本最低 | `WebSocketClientCodec`/`WebSocketServerCodec`/`HttpCodec` 三个公开类删除（破坏性） |

> **A 的可行性已核实（2026-09-27）**：Core 自带 `Http/HttpServer.cs`（`HttpServer : NetServer, IHttpHost`）与 `Http/WebSocketHandler.cs`（`WebSocketHandler : IHttpHandler`），握手在 `Http/WebSocket.cs` 内完成（校验 `Upgrade`/`Connection` 令牌 → 回 `101 SwitchingProtocols`）——**同一监听上 HTTP 与 WS 升级是 Core 原生能力**，无需 Remoting 自研。
> 仍需回答的一点：同端口上的**裸 SRMP 帧**是否必须保留（Core 的 `HttpServer` 按 HTTP 解析，非 HTTP 首字节不会自动落回 SRMP）。若"一个端口三协议"必须保留，A 需要 Core 侧给一个"非 HTTP 首字节回落"的钩子，或退到方案 C。
| B | Remoting 自研 `ApiProtocolCodec : IMessageCodec`：首帧嗅探 WS 升级 / HTTP 请求 / SRMP，命中后自行切换会话协议 | 保住"一端口三协议"与现有公开类型 | 与 v12"握手进打开链路"的设计相悖，等于把刚删掉的协商逻辑再写一遍；codec 需要会话引用（不再无状态可共享） |
| C | 拆分端口：SRMP 走 `ApiNetServer`（`Protocol = SrmpCodec`），HTTP/WS 走 Core 的 `ApiHttpServer`/`HttpServer` | 改动最小、语义最清晰 | 产品行为变化（同一端口不再三种协议），需文档与用户告知 |

**我的推荐是 A**，理由是它顺着 v12 已经做出的方向（协议区分交给传输类型 `NetUri.Type` 与 Net 层握手），而不是逆着它再实现一遍嗅探；B 之所以不推荐，是因为它会把 v12 刚统一的"握手/掩码归 Net 层"重新分裂到 Remoting。

## 4. 执行顺序（无论选哪个方案）

1. `HttpMessage : Message` + `TryParse`/`WriteHeader`（照 `WsMessage` 模式，2.1）
2. `HttpCodec : IMessageCodec`（2.2）；WS 相关按选定方案删除或改写
3. `IApiHost.GetMessageCodec()` 返回类型 `IPipelineHandler` → `IMessageCodec`；`ApiHost` 的 `new StandardCodec{Timeout,UserPacket}` → `new SrmpCodec()`（`Timeout` 已由消息层 `ApiHost.Timeout` 承担）
4. 装配点：`ApiNetServer.Init`、`ApiClient.OnCreate`（`client.Add(...)` → `client.Protocol = ...`；`client.Pipeline?.Clear()` 删除）、`WsClient` 两处 `codec.Write(context, msg)` → `codec.Build(msg)`
5. 测试：`ApiHostTests.GetMessageCodec_ReturnsCodec`（断言 `StandardCodec` → `SrmpCodec`）、`HttpMessageTests`、codec 相关用例
6. 验证：6 个 TFM 全 0 错误 + Remoting 全量测试（基线 658 通过）

## 5. 环境

- 已备好干净工作树 `C:\X\rem-head`（detached HEAD `56ac9e7`，csproj 已指向本地 Core 源码 + net4x 框架引用），可直接开工；**勿**在 `C:\X\NewLife.Remoting` 工作区直接改（混有他人未提交的鉴权特性 WIP 与过期的 09-18 半成品）。

（完）
