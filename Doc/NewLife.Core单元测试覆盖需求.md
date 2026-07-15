# NewLife.Core 单元测试覆盖需求

## 概述

本文档跟踪 NewLife.Core 项目各模块的单元测试覆盖情况。  
测试框架：**xUnit 2.9.3**，目标框架：**net8.0;net9.0;net10.0**。  
测试项目：`XUnitTest.Core`，源项目：`NewLife.Core`（23 个命名空间，~200 类型）。  

> 最后更新：2026-07-15（全量重写，298 源文件 ↔ 140+ 测试文件交叉比对）

---

## 覆盖概要

| 指标 | 数值 |
|------|:----:|
| 源文件总数（不含 Stub） | ~270 |
| 测试文件总数（含集成测试） | ~140 |
| 已覆盖（✅） | ~160 |
| 待补（⏳） | ~55 |
| 不适用（接口/枚举/内部） | ~55 |
| 集成测试 | 7 |
| 可测类型覆盖率 | ~75% |

---

## 模块覆盖详情

### Algorithms / Buffers / Caching

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| AverageSampling.cs | AverageDownSamplingTests.cs | ✅ |
| LinearInterpolation.cs | SamplingTests.cs | ✅ |
| PooledByteBufferWriter.cs | SpanReaderWriterTests.cs | ✅ |
| SpanHelper.cs | SpanHelperTests.cs | ✅ |
| SpanReader.cs | SpanReaderTests.cs | ✅ |
| SpanWriter.cs | SpanWriterTests.cs | ✅ |
| Cache.cs | — | ⏳ |
| CacheLock.cs | — | ⏳ |
| CacheProvider.cs | CacheProviderTests.cs | ✅ |
| MemoryCache.cs | MemoryCacheTests.cs, MemoryQueueTests.cs | ✅ |
| QueueEventBus.cs | QueueEventBusTests.cs | ✅ |

### Collections

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| CollectionHelper.cs | CollectionHelperTests.cs | ✅ |
| ConcurrentHashSet.cs | ConcurrentHashSetTests.cs | ✅ |
| NullableDictionary.cs | NullableDictionaryTests.cs | ✅ |
| ObjectPool.cs | ObjectPoolTests.cs | ✅ |
| Pool.cs | PoolTests.cs | ✅ |

### Common

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| DisposeBase.cs | DisposeBaseTests.cs | ✅ |
| MachineInfo.cs | MachineInfoTests.cs | ✅ |
| PinYin.cs | PinYinTests.cs | ✅ |
| Runtime.cs | RuntimeTests.cs | ✅ |
| Setting.cs | — | ⏳ |
| SysConfig.cs | — | ⏳ |
| TimeProvider.cs | — | ⏳ |
| Utility.cs | UtilityTests.cs | ✅ |

### Compression / Configuration

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| TarFile.cs | TarFileTests.cs, TarEntryTests.cs | ✅ |
| SevenZip.cs | — | ⏳ |
| CommandParser.cs | CommandParserTests.cs | ✅ |
| CompositeConfigProvider.cs | CompositeConfigProviderTests.cs | ✅ |
| Config.cs | ConfigProviderTests.cs | 🟡 部分 |
| ConfigHelper.cs | ConfigHelperTests.cs, ConfigHelperPrivateTests.cs | ✅ |
| FileConfigProvider.cs | FileConfigProviderAtomicWriteTests.cs | 🟡 部分 |
| HttpConfigProvider.cs | HttpConfigProviderTests.cs | ✅ |
| IniConfigProvider.cs | IniConfigProviderTests.cs | ✅ |
| JsonConfigProvider.cs | JsonConfigProviderTests.cs | ✅ |
| XmlConfigProvider.cs | XmlConfigProviderTests.cs | ✅ |

### Data

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| ArrayPacket.cs | ArrayPacketTests.cs | ✅ |
| DbRow.cs / DbTable.cs | DbTableTests.cs | ✅ |
| GeoHash.cs / GeoPoint.cs | GeoHashTests.cs | ✅ |
| IData.cs / IExtend.cs / IFilter.cs / IModel.cs | IExtendTests.cs | ✅ |
| IPacket.cs | IPacketTests.cs | ✅ |
| IPacketEncoder.cs | IPacketEncoderTests.cs | ✅ |
| OwnerPacket.cs | OwnerPacketTests.cs | ✅ |
| Packet.cs（[Obsolete]） | PacketTests.cs | ✅ |
| PageParameter.cs | PageParameterTests.cs | ✅ |
| RingBuffer.cs | RingBufferTests.cs | ✅ |
| Snowflake.cs | SnowflakeTests.cs | ✅ |
| TimePoint.cs | SamplingTests.cs | ✅ |
| IndexRange.cs | PacketTests.cs | ✅ |

### Event / Exceptions / Extension

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| EventArgs.cs | EventArgsTests.cs | ✅ |
| WeakAction.cs | WeakActionTests.cs | ✅ |
| XException.cs | XExceptionTests.cs | ✅ |
| BitHelper.cs | BitHelperTests.cs | ✅ |
| EnumHelper.cs | EnumHelperTests.cs | ✅ |
| ListExtension.cs | ListExtensionTests.cs | ✅ |
| ProcessHelper.cs | ProcessHelperTests.cs | ✅ |
| StringHelper.cs | StringHelperTests.cs | ✅ |
| ConcurrentDictionaryExtensions.cs | — | ⏳ |
| SpeakProvider.cs | — | ⏳ |

### Http

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| HttpBase.cs | HttpBaseTests.cs | ✅ |
| HttpHelper.cs | HttpHelperTests.cs | ✅ |
| HttpRequest.cs | HttpRequestTests.cs | ✅ |
| HttpResponse.cs | HttpResponseTests.cs | ✅ |
| HttpServer.cs | HttpServerTests.cs | ✅ |
| WebSocketMessage.cs | WebSocketMessageTests.cs | ✅ |
| HttpRouter.cs / HttpSession.cs / TinyHttpClient.cs 等 20+ 文件 | — | ⏳ |

### IO

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| CsvDb.cs | CsvDbTests.cs | ✅ |
| CsvFile.cs | CsvFileTests.cs | ✅ |
| EasyClient.cs | EasyClientTests.cs | ✅ |
| ExcelReader.cs | ExcelReaderTests.cs | ✅ |
| ExcelWriter.cs | ExcelWriterTests.cs | ✅ |
| IOHelper.cs | IOHelperTests.cs | ✅ |
| PathHelper.cs | PathHelperTests.cs, PathHelperHashTests.cs | ✅ |

### Log

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| ActionLog.cs | ActionLogTests.cs | ✅ |
| CompositeLog.cs | CompositeLogTests.cs | ✅ |
| ConsoleLog.cs | — | ⏳ |
| ITracer.cs | TracerTests.cs | ✅ |
| ITracerResolver.cs | TracerResolverTests.cs | ✅ |
| LevelLog.cs | LevelLogTests.cs | ✅ |
| LogEventListener.cs | — | ⏳ |
| Logger.cs | LoggerTests.cs | ✅ |
| NetworkLog.cs | NetworkLogTests.cs | ✅ |
| PerfCounter.cs | PerfCounterTests.cs | ✅ |
| TextControlLog.cs | — | ⏳ |
| TextFileLog.cs | TextFileLogTests.cs | ✅ |
| WriteLogEventArgs.cs | WriteLogEventArgsTests.cs | ✅ |
| XTrace.cs | — | ⏳ |

### Messaging / Model

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| DefaultMessage.cs | DefaultMessageTests.cs | ✅ |
| EventBus.cs | EventBusTests.cs | ✅ |
| EventHub.cs | EventHubTests.cs, EventHubIntegrationTests.cs | ✅ |
| IMessage.cs | MessageDisposeTests.cs | ✅ |
| PacketCodec.cs | CodecIntegrationTests.cs, JsonCodecIntegrationTests.cs | ✅ |
| Actor.cs | ActorTests.cs | ✅ |
| DeferredQueue.cs | DeferredQueueTests.cs | ✅ |
| Host.cs | HostTests.cs | ⛔ 排除编译 |
| IPipeline.cs | PipelineTests.cs | ✅ |
| ObjectContainer.cs | ObjectContainerTests.cs | ✅ |

### Net

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| ISocketRemote.cs | ISocketRemoteTests.cs | ✅ |
| NetClient.cs | NetClientTests.cs | ✅ |
| NetHandlerContext.cs | NetEdgeCaseTests.cs | ✅ |
| NetHelper.cs | NetUriTests.cs | ✅ |
| NetServer.cs | NetServerTests.cs, NetSeverTests.cs | ✅ |
| NetSession.cs | NetSessionTests.cs | ✅ |
| NetUri.cs | NetUriTests.cs | ✅ |
| ReceivedEventArgs.cs | NetAsyncTests.cs | ✅ |
| SessionBase.cs | SessionBaseTests.cs | ✅ |
| TcpConnectionInformation2.cs | TcpConnectionInformation2Tests.cs | ✅ |
| TcpServer.cs | EchoNetServerTests.cs | ✅ |
| TcpSession.cs | TcpSessionTests.cs | ✅ |
| Upgrade.cs | UpgradeTests.cs | ✅ |
| JsonCodec.cs | JsonCodecIntegrationTests.cs | ✅ |
| LengthFieldCodec.cs | HighThroughputCodecTests.cs | ✅ |
| StandardCodec.cs | CodecIntegrationTests.cs | ✅ |
| UdpServer.cs / UdpSession.cs / WebSocketClient.cs 等 | — | ⏳ |

### Reflection / Remoting

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| AssemblyX.cs | AssemblyXTests.cs | ✅ |
| AttributeX.cs | AttributeXTests.cs | ✅ |
| Reflect.cs | ReflectTests.cs | ✅ |
| ScriptEngine.cs | — | ⏳ |
| ApiHttpClient.cs | ApiHttpClientTests.cs, ApiHttpClientRaceTests.cs | ✅ |
| ApiException.cs | ApiHttpClientUnitTests.cs | ✅ |
| ApiHelper.cs | ApiHttpClientTests.cs | ✅ |
| FailoverLoadBalancer.cs | LoadBalancerTests.cs | ✅ |
| IServiceResolver.cs | ConfigServiceResolverTests.cs | ✅ |
| RaceLoadBalancer.cs | LoadBalancerTests.cs | ✅ |
| WeightedRoundRobinLoadBalancer.cs | LoadBalancerTests.cs | ✅ |

### Security

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| Asn1.cs / Asn1Tags.cs | Asn1Tests.cs | ✅ |
| CbcTransform.cs | CbcTransformTests.cs | ✅ |
| Crc16.cs / Crc32.cs | Crc16Tests.cs, Crc32Tests.cs | ✅ |
| DSAHelper.cs | DSAHelperTests.cs | ✅ |
| IPasswordProvider.cs | PasswordProviderTests.cs | ✅ |
| Murmur128.cs | Murmur128Tests.cs | ✅ |
| PKCS7PaddingTransform.cs | — | ⏳ |
| ProtectedKey.cs | ProtectedKeyTests.cs | ✅ |
| Rand.cs | RandTests.cs | ✅ |
| RC4.cs | RC4Tests.cs | ✅ |
| RSAHelper.cs | RSAHelperTests.cs | ✅ |
| SecurityHelper.cs | SecurityHelperTests.cs | ✅ |
| SM4.cs | SM4Tests.cs | ✅ |
| ZerosPaddingTransform.cs | ZerosPaddingTransformTests.cs | ✅ |

### Serialization

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| Binary.cs | BinaryTests.cs | ✅ |
| ExtendableConverter.cs | ExtendableConverterTests.cs | ✅ |
| LocalTimeConverter.cs | LocalTimeConverterTests.cs | ✅ |
| SafeInt64Converter.cs | SafeInt64ConverterTests.cs | ✅ |
| SerialHelper.cs | SerialHelperTests.cs | ✅ |
| SpanSerializer.cs | SpanSerializerTests.cs | ✅ |
| JsonParser.cs | JsonParserTests.cs | ✅ |
| JsonWriter.cs | JsonWriterTests.cs | ✅ |
| JsonReader.cs / Json.cs / JsonConverter.cs 等 | — | ⏳ |
| Xml/ 子目录全部 | — | ⏳ |

### Threading / Web / Xml / Windows

| 源文件 | 测试文件 | 状态 |
|--------|----------|:----:|
| Cron.cs | CronTests.cs | ✅ |
| ThreadPoolX.cs | ThreadPoolXTests.cs | ✅ |
| TimerScheduler.cs | TimerSchedulerTests.cs | ✅ |
| TimerX.cs | TimerXTests.cs | ✅ |
| JwtBuilder.cs | JwtBuilderTests.cs | ✅ |
| Link.cs | LinkTests.cs | ✅ |
| TokenModel.cs | TokenModelTests.cs | ✅ |
| TokenProvider.cs | TokenProviderTests.cs | ✅ |
| UriInfo.cs | UriInfoTests.cs | ✅ |
| WebClientX.cs | WebClientTests.cs | ✅ |
| PluginHelper.cs | — | ⏳ |
| XmlHelper.cs | XmlHelperTests.cs | ✅ |
| XmlConfig.cs | — | ⏳ |
| Windows 全部 | — | ⏳（平台相关） |

---

## 集成测试

| 测试文件 | 覆盖范围 |
|----------|---------|
| CodecIntegrationTests.cs | StandardCodec |
| EchoServerIntegrationTests.cs | NetServer 回显 |
| HighThroughputCodecTests.cs | LengthFieldCodec |
| HttpServerIntegrationTests.cs | HttpServer |
| JsonCodecIntegrationTests.cs | JsonCodec |
| NetworkServerIntegrationTests.cs | NetServer |
| WebSocketIntegrationTests.cs | WebSocket |

---

## 排除编译的测试

| 测试 | 原因 |
|------|------|
| `Applications/**` | 应用层测试，依赖外部 |
| `Model/HostTests.cs` | Host 重构后需重写 |
| `Remoting/**`（部分排除，5 个文件选择性包含） | 排除 ApiDownTests.cs 等网络依赖测试 |

> 注：`Expressions/**`、`Yun/**` 及 `BloomFilterTests.cs`、`RedisTest.cs` 等孤立的测试文件已于 2026-07-15 清理删除。

---

## 待补充覆盖（按优先级）

| 优先级 | 源文件 | 理由 |
|:------:|--------|------|
| 🔴 高 | Log/XTrace.cs | 全局日志入口 |
| 🔴 高 | Configuration/Config.cs | 配置基类 |
| 🔴 高 | Serialization/JsonReader.cs | JSON 读取核心 |
| 🔴 高 | Caching/Cache.cs | 缓存基类 |
| 🟡 中 | Configuration/FileConfigProvider.cs | 仅原子写入测试 |
| 🟡 中 | Log/ConsoleLog.cs | 控制台日志 |
| 🟡 中 | Http/TinyHttpClient.cs | 极简 HTTP 客户端 |
| 🟡 中 | Http/HttpRouter.cs | HTTP 路由核心 |
| 🟡 中 | Reflection/ScriptEngine.cs | 动态编译引擎 |
| 🟢 低 | Common/TimeProvider.cs | 时间提供者 |
| 🟢 低 | Compression/SevenZip.cs | 7z 压缩 |
| 🟢 低 | Net/UdpServer.cs | UDP 网络 |
