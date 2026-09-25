# WebSocket 迁移设计（消息协议栈接入）

> 状态：**已完成收口**（服务端帧循环替换 → 业务回调演进 → 客户端切协议 → 旧引用清理+发送侧切换；契约大手术中 `WebSocketMessage`、旧 `Handler`/`ToLegacy` 回调已整删）。验证：WS 集成 13/13、Net+Messaging 区域 440/440、全量 2842 通过 0 失败、全 TFM 编译 0 错。
> 关联文档：《消息协议栈》《WebSocket双向通信》。

## 1. 背景与现状

WebSocket 是协议栈迁移的最后一个协议。现状有三个接入点，仍使用旧帧引擎与旧消息类型：

| 接入点 | 位置 | 现状实现 |
|---|---|---|
| 服务端会话 | `Http/WebSocket.cs` | 自持 `Pipe` + `PacketFramer.Pump` 同步泵；逐帧 `new WebSocketMessage().ReadFrame(frame)`（物化 + 原地解掩码）；业务回调 `Handler(WebSocket, WebSocketMessage)` |
| 客户端 | `Net/WebSocketClient.cs`（`: TcpSession`） | `Add<WebSocketCodec>()` 挂旧处理器链 |
| 通用编解码器 | `Net/Handlers/WebSocketCodec.cs` | 旧 `Handler` 链实现——**已随 v12 契约大手术删除**（新实现为 `Messaging/WebSocketCodec`） |

## 2. 已就绪件

- `Messaging/WsMessage`（`: Message`）：承载 `Fin/Type/MaskKey/CloseStatus`，`TryReadCloseStatus` 解析关闭帧
- `Messaging/WebSocketCodec`（`IMessageCodec`）：`IsServer` 角色（服务端收掩码/发无掩码；客户端每帧自动随机掩码）；分片对齐现状（FIN=0 无法定界）
- **帧级交叉验证已通过**（`WebSocketCodecTests`）：
  - 同掩码键下新旧构建**逐字节一致**（0/5/125/126/65535/65536 全边界）
  - 新构建掩码帧 → 旧 `WebSocketMessage.ReadFrame` 解码还原原文；解析字段一致
  - 掩码语义对齐现状：发送侧原地 XOR（破坏性）、接收侧由消费方解码

## 3. 服务端改造（阶段 1：内部替换，外部零变化）

`Http/WebSocket.Process(IPacket)` 帧循环内部替换，**保持同步泵模型**：

```
现状：Pipe → PacketFramer.Pump → 每帧 new WebSocketMessage + ReadFrame → Process(WebSocketMessage)
目标：Pipe → MessagePump.TryRead 循环 → WsMessage（头字段就位）→ 消费侧 Demask → 分发
```

- **掩码解码点**：新增 `WsMessage.Demask()`——整帧快路径对负载原地 XOR（帧内字节独享，与现状 `ReadFrame` 一致）；流式先读满物化再解码。替代旧 `WebSocketMessage` 内的解码
- **同步泵**：用 `MessagePump.TryRead`（不等待版本）在 Socket 回调线程内循环，替代 `PacketFramer.Pump`
- **协议帧处理**照搬：Close 回显状态码、Ping 回 Pong（`TryReadCloseStatus` 已就绪）
- **业务回调过渡**：`Handler` 委托类型 `WebSocketDelegate(WebSocket, WebSocketMessage)` 本阶段**不变**——内部把 `WsMessage` 适配为 `WebSocketMessage` 交付（外部零变化）；接口演进见阶段 2（已加 `MessageHandler` 直达回调）；发送侧构建于阶段 4 切换至新 codec（线格式不变）

## 4. 客户端改造（阶段 3）

`WebSocketClient : TcpSession`——收敛到新栈会话协议模式：

- `Protocol = new WebSocketCodec { IsServer = false }`，替代 `Add<WebSocketCodec>()`
- 接收：消息泵 → `e.Message`（`WsMessage`）；发送：`SendMessage(WsMessage)`（codec 自动每帧随机掩码，替代 WebSocketCodec 的自动 MaskKey）
- **行为差异**：`ReceiveMessageAsync` 不再"解析首个帧后丢弃剩余字节"（现状历史语义）——改为完整消息交付（消息泵事件驱动入队，修复历史"丢后续帧"缺陷，兼容影响已确认为纯改善）
- `WebSocketCodec`（Handler）**保留为兼容面**，不删除

## 5. 破坏性评估与兼容策略

| 面 | 影响 | 策略 |
|---|---|---|
| 线格式 | 无 | 字节级一致已验证 |
| `WebSocket.Handler` | 若直接换 `WsMessage` 则破坏 | 阶段 1 适配交付；阶段 2 新增 `MessageHandler(WsMessageDelegate)` 直达回调并存过渡 |
| `WebSocketClient` 公开 API | 无（内部实现替换） | 签名保持 |
| `WebSocketCodec` | 无 | 保留为兼容面（冻结） |

## 6. 分阶段计划与验收

| 阶段 | 内容 | 验收标准 |
|---|---|---|
| 1 | 服务端帧循环替换 + `WsMessage.Demask` | ✅ **已完成**：`MessagePump.RequireFullFrame` 整帧模式 + `Http/WebSocket.Process(IPacket)` 改写（整帧语义、原地解码、旧类型适配交付）；WS 门禁 47/47、全量 0 失败 |
| 2 | 业务回调接口演进（`WsMessage` 直达） | ✅ **已完成**：`WebSocket.MessageHandler`（`WsMessageDelegate` 直达回调）新增，旧 `Process(WebSocketMessage)` 适配器并存；用例 12 验证直达回调 |
| 3 | 客户端切协议模式 | ✅ **已完成**：`Protocol = WebSocketCodec { IsServer = false }`；`ReceiveMessageAsync` 改内部队列（支持流水线收取）；掩码由协议构建生成（`message.MaskKey ?? MaskKey`）；用例 03/04/05/11 覆盖拉取路径 |
| 4 | 清理旧引用 | ✅ **已完成**：服务端发送侧（`Send`/`SendAllAsync`）经验 `BuildFrame` 切至新 codec 构建，Close 正文构造统一为 `WebSocketCodec.BuildClosePayload`（客户端同用）；`WebSocketCodec`（Handler）保留为管道兼容面并标注迁移指引；过时注释修订；全 TFM 编译 0 错、全量测试 0 失败 |

## 7. 风险与回滚

- **风险**：服务端帧循环改动影响生产 WS——缓解：线格式/解码语义逐字节对齐 + 现有测试作门禁；改动集中在 `Process(IPacket)` 内部
- **回滚**：方法级替换可整体 revert；无数据格式变化
- **分片**：已支持——消费侧自动重组（服务端 `WebSocket` / 客户端 `WebSocketClient` 经 `WebSocketFragment` 累积 FIN=0 序列，末片合并为完整消息后交付）；应用层无感；重组上限 16MB。发送侧仍为整帧（不分片发送）

## 8. 已确认结论

1. 阶段 1 已实施（服务端内部替换，外部零变化）
2. 阶段 2 接口演进：新增 `MessageHandler` 直达回调，不改变旧 `Handler` 签名（并存过渡）
3. 阶段 3 的 `ReceiveMessageAsync` 改为完整消息交付（内部队列），历史"仅首个帧"语义退役；新增用例 11 覆盖大帧拉取

---

（设计稿完）
