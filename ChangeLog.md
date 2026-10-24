# NewLife.Core 版本更新记录

## v12.0.2026.1024 (2026-10-24)

### 破坏性变更（升级须知）
- **数据包所有权模型重写**：`OwnerPacket` 引入引用计数内核，`Slice` 返回具体类型并支持 `using` 释放；`OwnerPacket.Free`、`Rent`/`Return` 等兼容壳删除；三参 `Slice` 保留 `[Obsolete]` 壳（语义改为引用计数共享，`transferOwner` 被忽略），基于旧版编译且使用已删除成员的调用方需随大版本重编译
- **消息与协议契约重建**：`IMessage` 承载方向语义（`Kind`/`Reply`/`OneWay`/`CreateReply`），消息不再池化；帧读写归 `IFrameMessage`（`ReadFrame`/`Build`/`BuildHeader`），旧名桥 `Read`/`ToPacket`/`ToHeaderPacket` 删除；`ISocketRemote.Add<T>()`/`SendFile`/`SendMessages` 删除，协议统一经 `Protocol` 属性设置
- **编解码器单栈化**：`PacketCodec`/`PacketFramer` 与 `NewLife.Net.Handlers` 处理器体系整删，统一为 `MessagePump` 帧泵 + 无状态 `IMessageCodec`；旧 `Pipeline`/`Handler` 全家族同步退役
- **目标框架精简**：删除 netcoreapp3.1、net5.0(-windows)、net7.0(-windows)、net9.0(-windows) 共 7 个 EOL 资产，保留 11 个 TFM（net45/net461/net462/netstandard2.0/netstandard2.1/net6.0(-windows)/net8.0(-windows)/net10.0(-windows)），包体由 14.6 MB 降至 9.8 MB；NuGet 会回退到最近兼容资产，但 net5/3.1 回退 ns2.1 会丢部分 JSON 类型，net7 回退 net6 会丢 `DataMemberResolver`/`ServiceTypeResolver`
- **HTTP 契约调整**：`IHttpHandler.ProcessRequest` 改为 `Task ProcessRequestAsync`；`HttpClient` 收敛为内网 `TinyHttpClient`，删除 RPC、`JsonHost`/`JsonOptions`、`IgnoreServerCertificate`、`Tracer`、`Log` 等超出定位的能力，1xx 与不支持的 Transfer-Encoding 显式抛 `NotSupportedException`
- **数据管道契约对齐 BCL**：读侧结束后再读改为抛 `InvalidOperationException`，新增 `PipeReader.Error`/`IsReaderCompleted`，`Complete()` 先提交已推进数据，`CancelPendingFlush()` 无挂起提交时暂存取消一次；连接故障不再伪装成优雅关闭
- **二进制兼容性修正**：`PooledByteBufferWriter.WriteToStreamAsync` 低 TFM 返回类型统一为 `ValueTask`，`IBinary.ReadBytes(Span<Byte>)` 放宽为全 TFM 暴露，消除 ns2.0 资产在现代宿主的 `MissingMethodException` 与接口实现者跨资产类型加载风险
- **Security 包合并**：`ECDsaHelper`/`ECKey`/`AlgorithmKeyBlob` 移入 `NewLife.Core`（命名空间不变），`NewLife.Security` 降级为类型转发壳，老消费者升级 Core 即可平滑迁移
- **过时类型清理**：分三批删除 `Packet` 类、`IPacketEncoder`、`NewLife.Net.Setting` 别名、`IAsyncEventBus`、`Pipeline`/`Handler` 族及 2026-10 全生态零引用复核后的公开成员；按下游实际引用补回 9 组 v11 兼容成员（三参 `Slice`、`OwnerPacket(Int32)`、`Reflect.GetTypeEx`、`SpanReader.FreeCapacity`、`ObjectPool.AllIdleTime` 等），其余设计级删除不提供兼容壳

### 新增功能
#### 多语言本地化
- **31 种内置语言**：异常、日志、界面提示与配置注释统一按 `CurrentUICulture` 本地化，英文为源文与兜底；内置 269 条运行时消息，`L.T`/`L.TLog` 零改动接入
- **请求语言与配置**：`HttpServer`/`HttpSession` 默认按 `Accept-Language` 选择最高权重且受支持的语言，`RequestCultureScope` 支持请求级作用域，`Setting.Language` 可锁定全局语言；`GetLanguages()` 返回 31 种语言的配置键、自称与英文名，供管理端生成下拉框
- **下游复用与源生成**：多语言源生成器随主包分发，下游声明 `Locales/*.yml` 即可复用与覆盖；语言表 Deflate 压缩、按需解压，单 DLL 且 AOT 安全（31 场景冒烟）
- **元数据本地化**：程序集标题/描述英文化，配置属性注释、`[Description]`/`[DisplayName]` 与 `MachineInfo` 属性文案按语言表翻译

#### NativeAOT 与源生成器
- **AOT 源生成器**：新增 `NewLife.Core.SourceGenerator` 并随主包分发，覆盖 IoC 注册、JSON 成员访问、配置绑定、HTTP 控制器静态分发四条反射通道；未覆盖场景自动回退反射，JIT 下同样免反射/免表达式编译
- **AOT 实测与 CI**：`AotSmoke/` 31 场景 JIT/AOT 双通，分析器与 ILC 告警由 164 条收敛至 0；AOT 冒烟矩阵纳入 CI，覆盖 win-x64/linux-x64/linux-arm64 阻断项与多 RID 观察项
- **动态能力边界**：脚本、插件、DLR、表达式编译等动态能力以 `RequiresDynamicCode`/`RequiresUnreferencedCode` 声明 + 运行期守卫承接，AOT 下抛明确异常；新增 `Reflect` 泛型反射入口，`SystemJson` 支持透传 `JsonSerializerContext`

#### 网络与协议
- **数据管道**：新增 `Pipe`/`PipeReader`/`PipeWriter` 与 `LimitedReader`，对齐 `System.IO.Pipelines` 形态，支持背压水位与读饥饿让位，全 TFM 可用（含 net45）
- **帧泵与协议**：`MessagePump` 头到齐即交付、小帧零拷贝快路径、大帧流式体、`MaxCache` 残余防护；`SequenceReader` 全 11 TFM 垫片；SRMP 服务端并行派发
- **WebSocket**：分片自动重组（RFC 6455 §5.4）、握手异步化进入打开链路、服务端与客户端共用帧解析；新增 `MaxFrameSize` 整帧上限
- **传输增强**：Unix 域套接字（UDS）、`AutoReceive` 接收模式二选一、发送泵异步化与背压感知发送 API、TCP 流式收发下沉会话、`SendFileAsync` 内核零拷贝文件发送
- **UDP**：统一响应匹配并支持请求-响应等待，`ReceiveFromAsync` 省去每包包信息开销，新增 `SocketSetting.ReceiveBufferSize`
- **HTTP**：请求体分块传输、在途请求停滞上限（慢速攻击防护）、`HttpSession` 发送队列、异步处理器契约、极简内网 `TinyHttpClient`
- **网络辅助**：`NetHelper` 新增私网判定、网段匹配、可信代理与客户端 IP 解析（CIDR）

#### 可观测性
- **OpenTelemetry 双向适配**：`OpenTelemetryTracer` 把 NewLife 埋点桥接到标准 `Activity`，`OpenTelemetryCollector` 把第三方 `ActivitySource` 埋点采集进星尘（net6.0+）
- **埋点热路径优化**：消除标识字符串、时间戳装箱与无采集方时的构建器分配

#### 安全与平台
- **ECDSA 合入核心**：`ECDsaHelper`/`ECKey`/`AlgorithmKeyBlob` 迁入核心库，`net45/net461/net462` 走 CNG 分支，其余走跨平台实现
- **移动端**：补齐 `MachineInfo` 的 Android/iOS 平台信息，新增 Android `MobileConsole` 机器信息查看器与 NativeAOT/NDK 构建，移动端冒烟 21 用例

### 性能
- UDP 会话查找改端点键缓存，服务端每包分配降低约 35%；接收环复用 `OwnerPacket` 句柄，轮末回挂接收槽
- 压缩编解码器改为流式进出，消除冗余拷贝；帧层解析前先探测头部，半包时不再构造被丢弃的消息对象
- 多语言表 Deflate 压缩 + 按需构建，单 DLL 体积减少约 34%；目标框架精简使包体由 14.6 MB 降至 9.8 MB
- OpenTelemetry 埋点热路径零分配；配对队列清空后停止周期检查定时器；路径缓存写入改用独立原子计数

### Bug 修复
- **[fix]** HTTP：参数化路由 `{param}` 段对齐（不再全部 404）、请求体按 `Content-Length` 截断、拒绝不支持的 `Transfer-Encoding`、相对 Uri 构建头部不再抛异常（#186）
- **[fix]** WebSocket：握手校验失败不再静默接管连接、握手头按令牌列表解析、入站帧补齐 RFC 6455 校验（RSV/保留 opcode/掩码方向/控制帧长度）
- **[fix]** 数据包：构造拒绝越界偏移、链式追加环检测、空首节点扩展头部不再产生负偏移
- **[fix]** 管道：`WriteTo` 跨段解码保持解码器状态（多字节字符不再被段边界解成替换字符）、背压改按未检查记账、`SpanReader` 流式扩容池缓冲可归还、入站管道关闭释放读侧残余
- **[fix]** 会话：UDP 客户端模式关闭释放套接字、UDP 体绑定与并发信号量释放、接受环递归深度限制、HTTP 会话整包判空并清理悬空引用
- **[fix]** 协议：长度字段非法值按损坏帧上报、帧泵区分已定界待整帧、压缩协议限制解压上限防解压炸弹
- **[fix]** 安全：`SecurityHelper` 旧框架分支释放哈希算法实例与 `ICryptoTransform`
- **[fix]** 其他：SSL 会话断线感知归一、`NetClient` 重连释放旧实例、升级检查遍历全部镜像地址、控制器仅暴露公开方法、网络变化只清自身缓存；补齐捷克/波兰/德/丹麦/意等语言漏译

### 测试与质量
- RISC-V 与 s390x 全量均 3573 通过 / 5 跳过 / 0 失败，新增 riscv64/s390x 指令集矩阵与多 RID CI
- 新增 AOT 冒烟工程、源生成器单测工程、包消费者冒烟与 API 兼容门禁；NetLoadTest 提供吞吐/延迟/分配基线
- 移动端 Android/iOS 冒烟 21 用例；修复测试端口抢占、SpanWriter 零分配断言、配置超时用例在满载下的假失败

### 文档与协作
- README 多语言化并默认英文，补充 AOT、TFM、locales 徽章与生态版图
- 新增/更新《AOT兼容性分析》《多语言本地化》《数据管道Pipe》《消息协议栈》《网络缓冲所有权架构》等文档，功能清单与架构设计同步到 v12 现状

---

## v11.19.2026.0901 (2026-09-01)

### 新增功能
- **向量存储接口**：新增 `IVectorStore`/`VectorRecord` 向量存储接口及数据结构，对标 Microsoft.Extensions.VectorData 做非泛型简化，支持 Top-K 相似度检索，为 AI 应用提供向量检索基础设施
- **Murmur128 哈希增强**：增强 Murmur128 哈希算法实现并补充完整单元测试覆盖
- **GeoPoint 功能增强**：增强 GeoPoint 地理位置处理能力并补充完整单元测试

### 配置与日志
- **SysConfig 注解完善**：为 SysConfig 属性补充 Description 注解并优化命名
- **日志文件命名统一**：日志文件命名统一为下划线分隔，移除 UTC 时间校正

### Bug 修复
- **[fix]** 修复 DSAHelper 对 PgenCounter/Counter 节点解析兼容性及标签不对称问题
- **[fix]** 修复 Crc16/Crc32 边界校验绕过、索引越界、Modbus 偏移与大流计数回绕
- **[fix]** 修复 PerfCounter 采样间隔异常配置除零与数组越界（Interval=0 或 Duration<Interval 时）
- **[fix]** RaceLoadBalancer 自定义探测委托异常击穿竞速调用兜底；XmlGeneral Decimal 经 Double 中转丢精度修正
- **[fix]** JwtBuilder 算法注册表并发安全、签名失败出参消息补齐；WebClientX 并发创建 HttpClient 泄漏防护
- **[fix]** PinYin 对 GB2312 未收录汉字编码越界崩溃补长度防护；SplitByFixedSize 补负偏移防护
- **[fix]** NetServer 随机端口落入系统保留段导致 UDP 绑定失败时清理并重新分配端口（有界重试 3 次）
- **[fix]** 修复 CacheLock 零超时空闲锁无法获取、MemoryCache 持久化过期竞态、CommandParser/JsonConfigProvider 越界、HttpConfigProvider 字符串锁失效、FileConfigProvider 非 Windows 原子写入崩溃
- **[fix]** ProcessHelper.Execute returnError 未重定向 stderr 导致死功能；BitHelper Byte 重载负位置破坏数据
- **[fix]** 修复 PKCS7 多块拷贝偏移、Rand 取绝对值溢出、Asn1 空缓冲越界

### 测试与质量
- 补充 Murmur128、GeoPoint、PinYin 越界防护等回归测试

---

## v11.18.2026.0801 (2026-08-01)

### HTTP 服务增强
- **HTTP 中间件机制**：引入中间件管道，支持 CORS 跨域与统一错误处理
- **参数化路由**：支持 `{param}` 参数化路由与多方法注册，完善路由体系
- **Controller 注入增强**：支持 `IHttpContext`/DI 容器多方式注入，支持作用域内动态注册服务实例（`IServiceRegistry`）
- **静态文件服务**：支持内嵌资源作为 HTTP 静态文件服务
- **参数绑定重构**：抽取 `ParameterBinder`，统一 MIME 类型处理，`FormFile` 保存原子化，优化 GET/DELETE 参数拼接
- **TokenHttpFilter 并发安全**：防止重复申请刷新令牌

### 连接池与对象池
- **连接池架构升级**：支持池满阻塞与生命周期回收，`Max`/`Min` 语义对齐主流
- **ObjectPool 信号量限流**：重构信号量限流，健壮性增强

### 网络层优化
- **WebSocket 掩码与心跳**：完善掩码与心跳机制实现
- **ProcessHelper 增强**：宿主进程识别与文档说明完善

### 工具类
- **24 位无符号整数读写**：新增 UInt24 读写方法
- **GeoPoint 值类型优化**：改为 `readonly record struct`，优化为值类型
- **接口清理**：移除过时的 `GetEventBus`、`IncrementWithTtl`/`DecrementWithTtl` 接口

### Bug 修复
- **[fix]** 完成 `MachineInfo` 分部类迁移，消除 70 个编译错误（重复定义与缺失 `partial` 修饰符）；修复 Windows 路径 `LoadHardwareFromRegistry` 的平台标注（消除 22 个 CA1416 警告）
- **[fix]** `PKCS7PaddingTransform` 整倍块长明文缺失完整填充块，与 .NET/Java 标准填充互通；`SM4.GenerateKey` 误设 IV、`ProtectedKey` Hex 分支误用 Base64、`Crc16/Crc32` 偏移量校验错误
- **[fix]** JSON 数组/列表解析丢弃 null 元素导致位置错位；`JsonComposite.Write` 误调用 `Read` 方法
- **[fix]** `MemoryCache` 持久化过期时间截断，已过期条目加载后复活为永不过期（持久化格式升级至 v2）
- **[fix]** `IReflect` JsonIgnore 条件语义反转、静态属性字典并发写、数组深拷贝越界
- **[fix]** `Cron` 步进值为 0 时死循环挂死；XML/配置空文件解析死循环
- **[fix]** `XmlConfig` 字符串锁对象失效导致并发保存冲突；`Actor` Stop 后 `Tell` 崩溃
- **[fix]** `WebSocket` Ping 回显使用已释放缓冲；`SessionBase`/`UdpSession` 池化对象泄漏；`HttpServer` 路径缓存并发安全
- **[fix]** `CacheLock` 分布式锁引入持有者令牌，避免锁超时被接管后误删新持有者的锁
- **[fix]** `ConsoleLog`/`NetworkLog` 异步写日志丢失唤醒竞态，日志永久滞留队列
- **[fix]** `HttpRouter` 可选参数 `{param?}` 死代码；`ApiHttpClient` 释放节点 HttpClient；`ObjectPool` 创建失败无限循环等 40+ 处缺陷
- **[fix]** `ObjectContainer` 新增线程安全查询 `ContainsService`/`FindServices`，消除 `ServiceProvider.GetService` 与 `ModelExtension.GetServices` 无锁遍历在启动期与并发注册抛集合修改异常
- **[fix]** `Asn1` 补 `0x83` 三字节长格式支持（长度超过 65535 的 TLV 原解析错位），`0x80` BER 不定长由静默错解改为显式抛错
- **[fix]** `Rand.NextBytes` 在 net45/netstandard2.0 改用加密安全随机数生成器，与 `Next()` 保持一致（原 `new Random()` 非加密安全）
- **[fix]** `ReadStringUni` 按实际长度读取字符串；`BitConverter` 越界与 `PathHelper` 写入偏移问题
- **[fix]** `ToDictionary`/`ApiHelper` 错误转换数组和列表；流读取不完整及 `ToBase64` 返回值问题
- **[fix]** 优化 `DeferredQueue` 并发安全与性能、`ArrayPacket` 解包数据安全性、`BinaryGeneral`/`Normal` 内存池归还与解码逻辑

### 测试与质量
- 全量测试 2558 项全部通过（含 MachineInfo 39 项、PKCS7 9 项）
- 测试代码迁移新 API（`ArrayPacket`/`JsonOptions`/`SearchAsync`），消除 CS0618 与 xUnit 分析器警告
- 新增 `Rand.NextBytes`、`Asn1` 长格式（0x83/0x80）测试；修复 `SnowflakeTests` 全局 WorkerId 测试随机到 0 的偶发失败（0 表示未设置，应排除）
- 新增 `ICache`/`IProducerConsumer`/`TimeProvider`/`PluginManager` 契约测试，补充 `XTrace`/`Config`/`JsonReader`/`Cache`、`XmlParser`/`TypeConverter`、`PluginHelper`、`ConsoleLog`、HTTP 中间件等核心类型测试
- 创建需求文档/功能清单/架构设计三件套文档，功能清单测试覆盖 55/74

## v11.17.2026.0701 (2026-07-01)

### JSON 增强
- **IDictionarySource 序列化支持**：System.Text.Json 支持 `IDictionarySource` 接口序列化及多种时间格式
- **DateTime ISO 8601 优化**：优化 `DateTime` 序列化为 ISO 8601 格式，增强字符串转义与注释处理

### 配置系统
- **原子写入**：`XmlConfig` 与 `JsonConfig` 支持原子写入，避免文件损坏
- **并发安全增强**：`FileConfigProvider` 原子写入增强多线程安全性，提升文件配置写入兼容性

### 网络优化
- **HttpContent 埋点增强**：优化 HTTP 内容埋点标签提取逻辑，提升链路追踪健壮性

### Bug 修复
- **[fix]** 修复 `SystemJson.Parse` 解析对象类型错误
- **[fix]** 修复数组序列化时错误忽略 null 元素

### 测试与质量
- **测试并发控制**：统一测试并发控制，移除 `xunit.runner.ci.json` 简化配置

---

## v11.16.2026.0601 (2026-06-01)

### 事件总线增强
- **EventHub 全面异步化**：重构 EventHub 分发机制，统一异步入口为 `OnReceiveAsync`，支持委托订阅与扩展，提升事件处理的并发能力与可扩展性
- **EventBus 异步广播**：EventBus 支持异步事件接收与广播，与 EventHub 异步架构对齐

### JSON 增强
- **自定义序列化选项**：支持传入自定义 `JsonSerializerOptions`，精细控制序列化行为

### 网络层优化
- **WebSocket 粘包分包**：WebSocket 完整支持粘包/分包处理，增强健壮性并通过高吞吐性能验证
- **HTTP 参数默认值**：HTTP 服务支持参数默认值绑定，优化参数解析健壮性
- **集成测试全面增强**：覆盖 TCP/UDP/WebSocket 多场景的高吞吐集成测试体系

### 多框架兼容性
- **ValueTask 统一**：统一异步接口为 `ValueTask`，移除多余条件编译，简化跨框架代码
- **IAsyncEnumerable / IAsyncDisposable 兼容**：兼容旧版 .NET 的异步枚举与异步释放接口
- **类型转发增强**：高版本框架补充类型转发消除类型冲突；支持 `CsvFile` 等异步接口类型转发

### Bug 修复
- **[fix]** 修复 `UdpClient` 并发绑定竞态条件，优化关闭处理逻辑

---

## v11.15.2026.0501 (2026-05-01)

### 序列化增强
- **SpanWriter/SpanReader WriteValue/ReadValue**：新增对所有基础类型的通用 `WriteValue`/`ReadValue` 方法，完整单元测试覆盖，零分配读写更易用
- **字符串截断方法统一命名**：重构字符串截断扩展方法，统一命名规范，提升 API 一致性

### JSON 增强
- **PropertyNaming 统一命名策略**：引入 `PropertyNaming` 枚举，支持驼峰、下划线、帕斯卡等多种 JSON 属性命名策略，可全局配置
- **Json 命名策略序列化增强**：增强序列化/反序列化对命名策略的完整应用，覆盖更多边界场景
- **ExtendableConverter 属性忽略增强**：增强 `ExtendableConverter` 对 `ShouldIgnore` 忽略条件的支持，灵活控制字段序列化

### 网络与 HTTP
- **集成测试增强**：统一集成测试客户端，增强用例覆盖；优化 `ApiHttpClient` 参数传递逻辑

### Bug 修复
- **[fix]** 修复 `AssemblyResolve` 事件递归触发配置初始化，导致栈溢出的问题
- **[fix]** 修复对象 `Copy` 时，目标 `string?` 字段为 null 导致的空指针异常
- **[chore]** 程序集解析异常时写入 Trace 调试信息，便于排查加载失败问题

---

## v11.14.2026.0402 (2026-04-02)

### 序列化增强
- **SpanWriter 流模式**：SpanWriter 支持 Stream 写入，OwnerPacket/SpanSerializer 支持 Stream 零拷贝双路径，优化大数据序列化
- **ISpanSerializable 增强**：增强 `ISpanSerializable` 支持，编码器可直接处理 `ISpanSerializable` 接口对象，提升序列化性能
- **DbTable Span 序列化**：支持 DbTable 的 Span 序列化与反序列化
- **长度前缀读写**：支持长度前缀数据/字符串读写，优化 `WriteEncodedInt` 空间分配
- **序列化配置重构**：重构序列化配置，新增 `Apply` 方法支持配置复用
- **高级二进制序列化**：零拷贝能力增强

### JSON 增强
- **JsonElement 类型转换**：支持 `JsonElement` 的类型转换扩展
- **ExtendableConverter 重构**：重构并增强 `ExtendableConverter`，提升兼容性
- **属性名映射优化**：优化 JSON 属性名映射，支持多特性叠加与命名策略
- **成员忽略特性**：剔除标记了 `IgnoreDataMember`/`XmlIgnore` 特性的属性，与 .NET 标准行为对齐

### 网络层优化
- **LengthFieldCodec 增强**：功能增强，测试覆盖完善
- **IPacket 多包链**：优化 IPacket 多包链操作性能与接口规范
- **SplitDataCodec 修复**：修复发送数据时未追加分割字节的 Bug

### 对象池增强
- **异步借出**：引入对象池 `GetAsync` 能力，支持高并发场景下的异步资源等待

### 反射与性能
- **Reflect 全面优化**：反射性能全面优化，缓存委托路径，热点路径零反射
- **内存池优化**：数组初始化现代化，使用内存池减少 GC 压力
- **TarFile 性能**：Span 化头部读写与补零复用，提升压缩处理性能

### 配置与服务
- **IServiceResolver**：新增服务解析器 `IServiceResolver` 接口及配置实现，支持多源服务发现

### 工具类
- **DefaultUserAgent**：支持对非 ASCII 字符进行 URL 编码，避免请求头异常
- **深拷贝优化**：调整数组、字典的深拷贝逻辑
- **StringHelper.TrimStart 标记过期**：标记 `TrimStart(String, String)` 为过时，迁移至标准 API；优化 `PathHelper` 路径分隔符处理

### Bug 修复
- **[fix]** 在 SqlServer 批量写入时，`fields` 可能为空数组导致异常

---

## v11.12.2026.0301 (2026-03-01)

### 序列化增强
- **SpanSerializer**：新增高性能 Span 二进制序列化器，支持零分配读写，性能全面优于传统方式
- **SpanReader/SpanWriter**：新增对 `byte[]` 的直接构造支持
- **字符串读取优化**：优化字符串读取时的内存分配策略，减少堆分配

### IPacket 重构与性能优化
- **OwnerPacket**：重构为 `sealed` 类，优化内存管理与生命周期
- **切片性能**：优化 IPacket 切片逻辑，减少装箱，完善测试
- **PacketHelper.AsPacket**：新增快捷转换方法，简化 Packet 操作
- **释放机制**：完善 IPacket 与 IMessage 的释放文档与测试

### 网络层性能优化
- **SendMessageAsync**：重构为非异步路径，降低 Task 分配开销
- **PooledValueTaskSource**：增强池化 ValueTask 源，提升并发吞吐
- **编解码器池化**：对编解码器与事件参数进行池化，Echo 场景性能大幅提升
- **NetServer 实测**：压测跑出 23.4 Gbps 带宽、1.4 亿 pkt/s 吞吐

### 配置系统热加载重构
- **事件驱动**：FileConfigProvider 从低效轮询改为 FileSystemWatcher 事件驱动热更新
- **双重保险**：Watcher 可用时定时器周期延长为 60 秒作为兜底，保持 Period 属性向后兼容

### IO 工具增强
- **IOHelper**：新增异步精确读取（`ReadExactlyAsync`）与最少读取（`ReadAtLeastAsync`）方法及测试
- **MaxSafeArraySize**：新增安全最大数组尺寸常量

### 其他优化
- **DeferredQueue**：支持运行时动态修改处理周期
- **MemoryCache**：`Remove` 单键路径实现零分配优化
- **文件操作**：增强异常处理与哈希校验链路追踪

### 性能基准测试
- 新增 Benchmark 基准测试项目（net10.0），覆盖 IPacket、MemoryCache、TCP Echo、NetServer 等核心场景
- 发布 IPacket、NetServer、MemoryCache 等性能测试报告

### 文档与 Copilot 指令
- 新增 NewLife.Net 开发规范文档与 Copilot 协作指令
- 新增 AI 开发流程指令及自治批处理规范
- 优化 Copilot 指令文档结构与基准测试规范

---

## v11.11.2026.0201 (2026-02-01)

### 核心功能

#### ApiHttpClient 增强
- **多服务竞速与负载均衡**：支持多地址竞速下载，自动选择最快节点
- **哈希校验**：支持文件下载时的哈希校验功能
- **负载均衡器重构**：新增 `RaceLoadBalancer`、`PeerEndpointSelector` 等负载均衡组件
- **节点管理**：自动屏蔽失败节点，支持可配置的启动延迟步长
- **性能追踪**：为竞速调度和下载过程增加 Tracer 埋点

#### 依赖注入（DI）增强
- **ObjectContainer 优化**：增强文档与核心功能，支持延迟 DI 集成
- **Token 模型扩展**：引入 `IToken` 接口，提升令牌模型的扩展性
- **TokenModel 扩展**：新增 Scope 属性支持，改为 partial 类提高扩展容错

### 网络层优化
- **NetClient 重构**：完善文档与注释，补充单元测试
- **会话/服务器增强**：增强 Net 会话与服务器注释及单元测试
- **消息处理统一**：统一 `IMessage` 处理逻辑，简化消息提取代码，支持原始内容追踪
- **UdpServer 修复**：修正监听 0 端口后没有自动回填到 NetServer 的问题
- **并发性能优化**：优化消息队列并发性能，完善编码器注释

### 工具类新增与增强
- **UriInfo 增强**：新增 `ToUri` 方法，增强对 IPv6 及多格式 URL 的解析支持
- **Span 扩展**：新增 `Span<byte> Trim` 扩展方法及单元测试
- **配置支持**：`Config<T>` 支持泛型字典（修复 [#172](https://github.com/NewLifeX/X/issues/172)）
- **序列化优化**：如果属性定义了 `DataObjectFieldAttribute`，让它们排在前面

### 文档与协作
- **使用手册**：为主要功能模块编写使用手册（ApiHttpClient、网络库等）
- **Copilot 指令优化**：
  - 新增 Markdown 文档规范
  - 完善主动优化原则及细化规范
  - 新增防御性注释与日志规范
  - 新增集合表达式与 Null 条件运算符指引
  - 优化 XCode/Cube 指令读取与 XML 检测策略
  - 自动分发 Copilot 指令文件及 MSBuild 支持

### 测试与质量
- **单元测试大幅增强**：为网络库核心功能、UriInfo、PeerEndpointSelector、ApiHttpClient 等新增大量单元测试
- **测试健壮性提升**：优化测试用例健壮性与时区兼容性，提升 CI 兼容性

### 其他优化
- **日志优化**：简化线程池和长任务在日志里的名字，让应用启动时的日志更整齐
- **AddServer 改进**：添加服务地址时返回服务节点

---

## 历史版本

### v11.10.2026.0101 (2026-01-01)
- 重构 IEventBus，支持 EventHub
- ApiHttpClient 支持文件下载

---

**说明**：
- 正式版发布周期：每月月初
- 测试版发布周期：提交代码到 GitHub 时自动发布
- 版本号格式：正式版 `{主版本}.{子版本}.{年}.{月日}`，测试版 `{主版本}.{子版本}.{年}.{月日}-beta{时分}`
