---
title: NewLife.Core AI 功能索引
version: "11.10"
updated: "2026-03-19"
description: NewLife.Core 核心库全部功能模块的结构化索引，供 AI 工具和 MCP Server 检索使用
---

# NewLife.Core 功能索引

本文档为 NewLife.Core 核心库的结构化功能索引，覆盖全部 26 个命名空间和 200+ 公共类型。
适用于 AI 编程助手检索、MCP Server 知识索引构建、以及开发者快速定位功能。

NuGet 包：`NewLife.Core`
源码仓库：<https://github.com/NewLifeX/X>
文档站点：<https://newlifex.com/core>

---

## 模块总览

| 命名空间 | 模块 | 一句话描述 | 核心类型 |
|----------|------|-----------|---------|
| NewLife | 基础扩展 | 类型转换、字符串操作、进程管理等基础工具 | Utility, StringHelper, DisposeBase |
| NewLife.Caching | 缓存系统 | 统一缓存接口，内存缓存和 Redis 客户端 | ICache, MemoryCache, Redis |
| NewLife.Collections | 集合与池化 | 对象池、线程安全集合、布隆过滤器 | Pool\<T\>, ObjectPool\<T\>, DictionaryCache |
| NewLife.Configuration | 配置系统 | 多源配置框架，支持 JSON/XML/INI/HTTP/Apollo | IConfigProvider, Config\<T\>, HttpConfigProvider |
| NewLife.Data | 数据结构 | 数据包接口、分页、雪花ID、地理哈希 | IPacket, PageParameter, Snowflake, GeoHash |
| NewLife.Http | HTTP 与 WebSocket | 轻量级 HTTP 服务器/客户端和 WebSocket | HttpServer, WebSocketClient, TinyHttpClient |
| NewLife.IO | 文件与存储 | CSV/Excel 读写、对象存储、路径工具 | CsvFile, ExcelReader, OssClient |
| NewLife.Log | 日志与追踪 | 多输出日志框架，分布式链路追踪 APM | ILog, ITracer, DefaultTracer, XTrace |
| NewLife.Model | 应用框架 | 依赖注入、插件管理、管道模型、Actor 并发 | ObjectContainer, Host, Actor, IPipeline |
| NewLife.Net | 网络库 | 高性能 TCP/UDP 服务端/客户端，编解码管道 | NetServer, TcpServer, StandardCodec |
| NewLife.Reflection | 反射扩展 | 高性能反射、程序集工具、脚本引擎 | IReflect, AssemblyX, ScriptEngine |
| NewLife.Remoting | RPC 框架 | API 客户端/服务端，负载均衡，故障转移 | ApiClient, ApiServer, ApiHttpClient |
| NewLife.Security | 安全加密 | RSA/AES/SM4 加解密，哈希校验，证书操作 | SecurityHelper, RSAHelper, SM4, Crc32 |
| NewLife.Serialization | 序列化 | JSON/XML/Binary 多格式序列化，高性能 Span 序列化 | Json, Binary, Xml, SpanSerializer |
| NewLife.Threading | 定时调度 | 高精度定时器，Cron 表达式调度 | TimerX, Cron, ThreadPoolX |
| NewLife.Algorithms | 数据算法 | 时序降采样、插值算法 | LTTBSampling, LinearInterpolation |
| NewLife.Buffers | 高性能缓冲区 | 池化写入器，零分配 Span 读写器 | PooledByteBufferWriter, SpanWriter, SpanReader |
| NewLife.Compression | 压缩归档 | Tar/7Zip 压缩解压 | TarFile, SevenZip |
| NewLife.Messaging | 消息与事件 | 消息接口，事件总线，事件中心 | IMessage, IEventBus, EventHub |
| NewLife.Web | Web 工具 | JWT 令牌、OAuth 客户端/服务端、令牌提供者 | JwtBuilder, OAuthClient, TokenProvider |
| NewLife.Windows | Windows 特定 | 电源状态、语音识别、控制台扩展 | PowerStatus, SpeechRecognition |
| NewLife.Xml | XML 配置 | XML 配置文件基类 | XmlConfig\<T\>, XmlHelper |
| NewLife.Json | JSON 配置 | JSON 配置文件基类 | JsonConfig |
| NewLife.Expressions | 表达式计算 | 逆波兰表达式、数学表达式求值 | RpnExpression, MathExpression |
| NewLife.Yun | 云服务 | 百度/高德/腾讯地图 API、阿里云 OSS | BaiduMap, AMap, OssClient |
| NewLife.Common | 系统信息 | 机器硬件信息、运行时统计、系统配置 | MachineInfo, Runtime, SysConfig |

---

## 详细模块说明

### 基础扩展（NewLife）

| 类型 | 文档 | 说明 |
|------|------|------|
| Utility | [类型转换](https://newlifex.com/core/utility) | 高效类型转换扩展：ToInt()、ToBoolean()、ToDateTime() 等 |
| StringHelper | [字符串扩展](https://newlifex.com/core/string_helper) | 字符串截取、编码、格式化、空值判断等 |
| DisposeBase | [可销毁基类](https://newlifex.com/core/disposebase) | 标准资源释放模式，自动生命周期追踪 |
| ProcessHelper | [进程扩展](https://newlifex.com/core/process_helper) | 简化进程管理，获取进程信息 |
| Runtime | [运行时信息](https://newlifex.com/core/runtime) | 应用运行时信息获取，便于调试诊断 |
| PinYin | [拼音库](https://newlifex.com/core/pinyin) | 高效汉字转拼音，支持全拼和简拼 |
| WeakAction\<T\> | — | 弱引用委托，防止事件订阅内存泄漏 |
| Setting | — | 全局应用配置 |
| SysConfig | — | 系统配置（应用名、版本等元数据） |

### 缓存系统（NewLife.Caching）

| 类型 | 文档 | 说明 |
|------|------|------|
| ICache | [统一缓存接口](https://newlifex.com/core/icache) | 标准缓存操作接口，Get/Set/Remove/GetAll 等 |
| MemoryCache | [内存缓存](https://newlifex.com/core/memory_cache) | 高性能单机内存缓存，支持过期和容量策略 |
| ICacheProvider | [缓存架构](https://newlifex.com/core/icacheprovider) | 缓存提供者，管理多 ICache 实例 |
| Redis | [Redis客户端](https://newlifex.com/core/redis) | 高性能 Redis 客户端（另有独立包 NewLife.Redis） |
| CacheLock | — | 基于缓存的分布式锁 |
| MemoryQueue\<T\> | — | 内存队列实现 |
| IProducerConsumer\<T\> | — | 生产者-消费者队列接口 |

### 集合与池化（NewLife.Collections）

| 类型 | 文档 | 说明 |
|------|------|------|
| Pool\<T\> | [对象池](https://newlifex.com/core/pool) | 基于 CAS 的高性能通用对象池 |
| ObjectPool\<T\> | [对象池](https://newlifex.com/core/object_pool) | 带生命周期管理的对象池 |
| DictionaryCache\<K,V\> | — | LRU 字典缓存，支持过期和上限 |
| ConcurrentHashSet\<T\> | — | 线程安全的 HashSet |
| BloomFilter | — | 布隆过滤器，概率性集合成员测试 |
| StringBuilderPool | — | StringBuilder 池化（`Pool.StringBuilder` 获取） |

### 配置系统（NewLife.Configuration）

| 类型 | 文档 | 说明 |
|------|------|------|
| IConfigProvider | [配置提供者](https://newlifex.com/core/config_provider) | 统一配置接口，支持回调和层级节点 |
| Config\<T\> | [配置系统](https://newlifex.com/core/config) | 泛型配置基类，单例模式 |
| JsonConfigProvider | — | JSON 格式配置（默认） |
| XmlConfigProvider | — | XML 格式配置 |
| IniConfigProvider | — | INI 格式配置 |
| HttpConfigProvider | — | HTTP 远程配置（适配 Stardust 配置中心） |
| ApolloConfigProvider | — | Apollo 配置中心适配器 |
| CommandParser | — | 命令行参数解析 |

### 数据结构（NewLife.Data）

| 类型 | 文档 | 说明 |
|------|------|------|
| IPacket | [数据包接口](https://newlifex.com/core/packet) | 零拷贝切片、链式包的核心数据包接口 |
| PageParameter | — | 分页参数封装，支持 Web 和 API 场景 |
| DbTable | [数据集](https://newlifex.com/core/dbtable) | 内存数据表，数据处理和分析 |
| Snowflake | [雪花算法](https://newlifex.com/core/snow_flake) | 分布式唯一 ID 生成器 |
| GeoHash | [经纬度哈希](https://newlifex.com/core/geo_hash) | 经纬度编码，用于附近位置搜索 |
| RingBuffer | — | 环形缓冲区 |
| BinaryTree | — | 二叉树实现 |

### HTTP 与 WebSocket（NewLife.Http）

| 类型 | 文档 | 说明 |
|------|------|------|
| HttpServer | [HTTP服务端](https://newlifex.com/core/httpserver) | 轻量级 HTTP 服务器，适合嵌入式/IoT 设备 |
| WebSocketClient | [WebSocket](https://newlifex.com/core/websocket) | WebSocket 客户端 |
| WebSocketSession | [WebSocket](https://newlifex.com/core/websocket) | WebSocket 会话，物联网下行通知 |
| TinyHttpClient | — | 极简 HTTP 客户端 |
| StaticFilesHandler | — | 静态文件服务 |
| ControllerHandler | — | MVC 控制器处理器 |
| TokenHttpFilter | — | Token 认证过滤器 |

### 文件与存储（NewLife.IO）

| 类型 | 文档 | 说明 |
|------|------|------|
| CsvFile | [CSV解析](https://newlifex.com/core/csv_file) | CSV 文件快速读写 |
| CsvDb\<T\> | [CSV数据库](https://newlifex.com/core/csv_db) | 以 CSV 文件为数据库的增删改查 |
| ExcelReader | [Excel读取](https://newlifex.com/core/excel_reader) | 无需 Office，轻量级 Excel 读取 |
| ExcelWriter | — | Excel 文件写入 |
| OssClient | [OSS存储](https://newlifex.com/core/oss) | 阿里云 OSS 对象存储客户端 |
| PathHelper | [路径扩展](https://newlifex.com/core/path_helper) | 跨平台路径处理工具 |
| IOHelper | [数据扩展](https://newlifex.com/core/io_helper) | 高效 IO 操作，流与字节数组转换 |

### 日志与追踪（NewLife.Log）

| 类型 | 文档 | 说明 |
|------|------|------|
| ILog | [日志接口](https://newlifex.com/core/log) | 核心日志接口，支持多种输出方式 |
| XTrace | [日志](https://newlifex.com/core/log) | 高级日志门面，全局日志入口 |
| ITracer | [链路追踪](https://newlifex.com/core/tracer) | 分布式追踪接口，APM 性能追踪 |
| DefaultTracer | [链路追踪](https://newlifex.com/core/tracer) | 默认追踪器实现，集成星尘平台 |
| CodeTimer | — | 代码执行计时器 |
| TextFileLog | — | 文本文件日志 |
| ConsoleLog | — | 控制台日志 |
| CompositeLog | — | 组合日志，同时输出到多个目标 |
| NetworkLog | — | 网络日志 |

### 应用框架（NewLife.Model）

| 类型 | 文档 | 说明 |
|------|------|------|
| ObjectContainer | [对象容器](https://newlifex.com/core/object_container) | 轻量级 IoC/DI 容器 |
| Host | [应用主机](https://newlifex.com/core/host) | 轻量级后台服务托管 |
| IPlugin / PluginManager | [插件框架](https://newlifex.com/core/plugin) | 标准化插件管理，模块化开发 |
| Actor | [并行模型](https://newlifex.com/core/actor) | Actor 并发模型，消息驱动 |
| IPipeline / Pipeline | [管道模型](https://newlifex.com/core/pipeline) | 双向处理链，Read/Write/Open/Close |
| DeferredQueue | [延迟队列](https://newlifex.com/core/deferred_queue) | 聚合高频变更并批处理落地 |
| IHostedService | — | 托管服务接口（兼容 .NET Generic Host） |

### 网络库（NewLife.Net）

| 类型 | 文档 | 说明 |
|------|------|------|
| NetServer | [网络服务端](https://newlifex.com/core/netserver) | 高性能网络服务器，封装会话管理（实测 2266万tps） |
| TcpServer / UdpServer | [网络服务端](https://newlifex.com/core/netserver) | 底层 TCP/UDP 服务器 |
| NetClient | [网络客户端](https://newlifex.com/core/netclient) | 统一 TCP/UDP 客户端 |
| NetSession | — | 高级网络会话 |
| StandardCodec | [编解码器](https://newlifex.com/core/net_handlers) | 请求-响应消息匹配编解码 |
| LengthFieldCodec | [编解码器](https://newlifex.com/core/net_handlers) | 长度字段粘包拆包 |
| WebSocketCodec | — | WebSocket 协议编解码 |
| JsonCodec | — | JSON 消息编解码 |

### RPC 框架（NewLife.Remoting）

| 类型 | 文档 | 说明 |
|------|------|------|
| ApiClient | [RPC客户端](https://newlifex.com/core/api_client) | 快速创建 RPC 客户端 |
| ApiServer | [RPC服务端](https://newlifex.com/core/api_server) | 快速构建 RPC 服务端 |
| ApiHttpClient | [HTTP客户端](https://newlifex.com/core/api_http) | 基于 HttpClient 封装的 API 客户端 |
| ILoadBalancer | [负载均衡](https://newlifex.com/core/load_balancer) | 负载均衡接口 |
| FailoverLoadBalancer | [负载均衡](https://newlifex.com/core/load_balancer) | 故障转移策略 |
| WeightedRoundRobinLoadBalancer | [负载均衡](https://newlifex.com/core/load_balancer) | 加权轮询策略 |
| RaceLoadBalancer | [负载均衡](https://newlifex.com/core/load_balancer) | 竞速策略 |

### 安全加密（NewLife.Security）

| 类型 | 文档 | 说明 |
|------|------|------|
| SecurityHelper | [安全扩展](https://newlifex.com/core/security_helper) | 统一的加解密扩展方法集 |
| RSAHelper | — | RSA 非对称加密 |
| ECDsaHelper | — | ECDSA 数字签名 |
| SM4 | — | 国密 SM4 对称加密 |
| RC4 | — | RC4 流加密 |
| Crc16 / Crc32 | — | CRC 校验 |
| Murmur128 | — | MurmurHash3 哈希 |
| Certificate | — | X.509 证书操作 |
| IPasswordProvider | — | 密码策略接口（MD5/盐值等） |

### 序列化（NewLife.Serialization）

| 类型 | 文档 | 说明 |
|------|------|------|
| Json | [JSON序列化](https://newlifex.com/core/json) | 高性能 JSON 序列化/反序列化 |
| Binary | [二进制序列化](https://newlifex.com/core/binary) | 二进制序列化，支持复杂对象图 |
| Xml | [XML序列化](https://newlifex.com/core/xml) | XML 序列化/反序列化 |
| SpanSerializer | — | 基于 Span 的高性能零分配序列化 |
| IFormatterX | — | 序列化器基础接口 |

### 定时调度（NewLife.Threading）

| 类型 | 文档 | 说明 |
|------|------|------|
| TimerX | [高级定时器](https://newlifex.com/core/timerx) | 高精度定时器，支持 Cron 和异步 |
| Cron | [Cron表达式](https://newlifex.com/core/cron) | 类 Linux Cron 定时任务，支持秒级 |
| ThreadPoolX | — | 线程池包装器 |

### 数据算法（NewLife.Algorithms）

| 类型 | 文档 | 说明 |
|------|------|------|
| LTTBSampling | — | LTTB 降采样算法（保留视觉特征） |
| LinearInterpolation | — | 线性插值 |
| LagrangeInterpolation | — | 拉格朗日多项式插值 |
| BilinearInterpolation | — | 双线性插值 |

### 高性能缓冲区（NewLife.Buffers）

| 类型 | 文档 | 说明 |
|------|------|------|
| PooledByteBufferWriter | [池化写入器](https://newlifex.com/core/pooled_buffer_writer) | 基于 ArrayPool 的池化 IBufferWriter |
| SpanWriter | [Span写入器](https://newlifex.com/core/span_writer) | 零分配二进制数据写入 |
| SpanReader | [Span读取器](https://newlifex.com/core/span_reader) | 零分配二进制数据读取 |
| SpanHelper | [Span辅助](https://newlifex.com/core/span_helper) | Span/ReadOnlySpan 扩展方法集 |

### 压缩归档（NewLife.Compression）

| 类型 | 文档 | 说明 |
|------|------|------|
| TarFile | [压缩归档](https://newlifex.com/core/tar_7zip) | Tar/Tar.Gz 归档操作 |
| SevenZip | [压缩归档](https://newlifex.com/core/tar_7zip) | 7z 压缩解压 |

### 消息与事件（NewLife.Messaging）

| 类型 | 文档 | 说明 |
|------|------|------|
| IMessage | [消息接口](https://newlifex.com/core/message) | 请求-响应消息接口 |
| IEventBus | [事件总线](https://newlifex.com/core/eventbus) | 进程内事件分发 |
| EventHub\<T\> | [事件总线](https://newlifex.com/core/eventbus) | 支持主题路由和队列消息的事件中心 |
| PacketCodec | [数据包编码](https://newlifex.com/core/packet_codec) | 粘包拆包编解码器 |

### Web 工具（NewLife.Web）

| 类型 | 文档 | 说明 |
|------|------|------|
| JwtBuilder | [JWT令牌](https://newlifex.com/core/jwt) | JWT 令牌生成、解码、验证 |
| TokenProvider | [分布式令牌](https://newlifex.com/core/token_provider) | 分布式数字签名令牌 |
| OAuthClient | — | OAuth 2.0 客户端基类 |
| OAuthServer | — | OAuth 2.0 服务端 |
| WebClientX | [网络下载](https://newlifex.com/core/webclient) | 增强型 HTTP 下载，支持多线程、断点续传 |

### 云服务（NewLife.Yun）

| 类型 | 文档 | 说明 |
|------|------|------|
| BaiduMap | — | 百度地图 API 封装 |
| AMap | — | 高德地图 API 封装 |
| WeMap | — | 腾讯地图 API 封装 |

---

## NewLife 生态项目速查

除核心库外，NewLife 还有一系列配套组件和产品平台，均可通过 NuGet 引用：

| NuGet 包 | 仓库 | 说明 |
|----------|------|------|
| NewLife.Core | [NewLifeX/X](https://github.com/NewLifeX/X) | 核心库（本文档） |
| NewLife.XCode | [NewLifeX/NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 大数据中间件，单表百亿级 |
| NewLife.Net | [NewLifeX/NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 网络库，单机 2266万tps |
| NewLife.Remoting | [NewLifeX/NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 统一 RPC 远程通信框架 |
| NewLife.Cube | [NewLifeX/NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 魔方 Web 快速开发平台 |
| NewLife.Agent | [NewLifeX/NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 服务管理（Windows 服务/Systemd） |
| NewLife.Redis | [NewLifeX/NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 客户端，百亿级验证 |
| NewLife.RocketMQ | [NewLifeX/NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | RocketMQ 纯托管客户端 |
| NewLife.MQTT | [NewLifeX/NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 客户端+服务端 |
| NewLife.IoT | [NewLifeX/NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT 标准库 |
| NewLife.Modbus | [NewLifeX/NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus 协议实现 |
| Stardust | [NewLifeX/Stardust](https://github.com/NewLifeX/Stardust) | 星尘分布式服务平台 |
| AntJob | [NewLifeX/AntJob](https://github.com/NewLifeX/AntJob) | 蚂蚁调度，分布式大数据计算 |
| NewLife.Office | [NewLifeX/NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | 办公自动化（Excel/Word/PDF） |
| NewLife.AI | [NewLifeX/NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 多模型 AI 网关与 Agent 平台 |

---

## 常用内置工具

NewLife.Core 提供了一些优先于标准库使用的内置工具：

| 工具 | 用法 | 替代 |
|------|------|------|
| Pool.StringBuilder | `var sb = Pool.StringBuilder.Get(); ... sb.Put();` | `new StringBuilder()` |
| Runtime.TickCount64 | `var t = Runtime.TickCount64;` | `Environment.TickCount64`（仅 .NET 5+） |
| 类型转换扩展 | `"123".ToInt()`, `"true".ToBoolean()` | `Int32.Parse()`, `Boolean.Parse()` |
| 追踪埋点 | `using var span = Tracer?.NewSpan("操作名");` | — |

---

## 兼容性

NewLife.Core 支持 **.NET 4.5** 至 **.NET 10.0**（LangVersion=latest）。

条件编译符号：`NETFRAMEWORK`、`NETSTANDARD2_0`、`NETCOREAPP`、`NET5_0_OR_GREATER`、`NET6_0_OR_GREATER`、`NET8_0_OR_GREATER`
