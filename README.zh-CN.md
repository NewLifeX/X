# NewLife.Core - 核心库

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)
[![test](https://github.com/NewLifeX/X/actions/workflows/test.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/test.yml)
<!-- metrics:start -->
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-31%2F31-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-11%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
<!-- metrics:end -->
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [English](README.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core** 是 NewLife 生态的基础库：日志、配置、缓存、网络、序列化、定时调度、对象容器与 APM 追踪，一个包即可开工。原生 AOT 就绪，内置 <!--locale-count:start-->31<!--locale-count:end--> 种语言运行时消息，单包覆盖 net45 → net10.0 共 11 个目标框架，支撑 XCode、Cube、Redis、Stardust 等 30 多个兄弟项目。

核心库教程：[https://newlifex.com/core](https://newlifex.com/core)  

> ℹ️ 本中文版与 [English README](README.md) 同批维护；若两者内容不一致，以英文版为对外口径。

**目录**：[快速开始](#快速开始) · [为什么选择](#为什么选择-newlifecore) · [主要功能](#主要功能) · [生态版图](#生态版图) · [文档与示例](#文档与示例) · [参与贡献](#参与贡献) · [新生命开发团队](#新生命开发团队) · [许可证](#许可证)

## 快速开始

### 安装

```bash
dotnet add package NewLife.Core
```

### 最小示例

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // 控制台日志（同时写文件）
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // 内存缓存
cache.Set("key", "value", 60);          // 60 秒过期
var value = cache.Get<String>("key");   // 读取
```

### 模板脚手架

命令行中运行以下命令快速体验 NewLife 组件：

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### 平台支持

| 项目 | 覆盖范围 |
| --- | --- |
| 目标框架 | 单包 11 个：net45、net461、net462、netstandard2.0、netstandard2.1、net6.0、net6.0-windows、net8.0、net8.0-windows、net10.0、net10.0-windows |
| 系统与架构 | Windows、Linux、macOS、Android、iOS；x64、x86、arm64、arm、loongarch64、riscv64、s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> 个核心场景实测通过，多 RID CI 冒烟矩阵把关（win-x64、linux-x64、linux-arm64、linux-loongarch64 为阻断门禁）。插件、脚本等动态能力按设计不支持，边界见 [AOT兼容性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |
| 运行时本地化 | 内置 <!--locale-count:start-->31<!--locale-count:end--> 种语言；日志与异常消息在运行时本地化，无需改动调用点（见 [多语言本地化](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)） |
| 旧版框架 | net40：`dotnet add package NewLife.Core --version 10.10.2024.801-net40`（含 net40/net45/net461）；net20：`--version 10.10.2024.701-net20`（仅 net20）。两者为 2024-08 冻结版，不随主线更新。 |

## 为什么选择 NewLife.Core

- **一套组件覆盖常见基础设施**：日志、配置、缓存、网络、序列化、定时调度、对象容器与依赖注入，引用一个包即可开工。
- **覆盖广、运行时现代**：单包 11 个目标框架（net45 → net10.0，含 Windows 变体），原生 AOT 就绪，运行时消息支持 <!--locale-count:start-->31<!--locale-count:end--> 种语言，老系统与新项目共用同一套 API。
- **经生态规模验证**：同一套底座支撑 XCode（单表百亿级）、Net（TCP 1KB 单机 177 万 pps、14.5 Gbps）、Redis 与 Stardust（百亿级数据量）等项目。
- **依赖克制、协议宽松**：仅使用 BCL 与微软官方兼容包；MIT 协议，允许改源码、改名私有化（见[许可证](#许可证)）。

## 主要功能

<details>
<summary><b>基础扩展</b>（10 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [类型转换Utility](https://newlifex.com/core/utility) | 提供高效的类型转换，支持各种格式和异常处理。 |
| [字符串扩展StringHelper](https://newlifex.com/core/string_helper) | 包含多种字符串操作，如截取、编码和格式化等。 |
| [路径扩展PathHelper](https://newlifex.com/core/path_helper) | 智能处理多种路径格式，实现代码跨平台兼容性。 |
| [数据扩展IOHelper](https://newlifex.com/core/io_helper) | 优化IO操作，提供高效的数据流与字节数组转换。 |
| [进程扩展ProcessHelper](https://newlifex.com/core/process_helper) | 简化进程操作，提供获取和管理进程信息的便捷方法。 |
| [运行时信息Runtime](https://newlifex.com/core/runtime) | 提供简单方法获取应用运行时信息，便于调试诊断。 |
| [可销毁DisposeBase](https://newlifex.com/core/disposebase) | 提供标准的资源释放模式，有效防止内存和资源泄漏。 |
| [反射扩展Reflect](https://newlifex.com/core/reflect) | 高性能反射工具，快速进行对象成员的读写操作。 |
| [经纬度哈希GeoHash](https://newlifex.com/core/geo_hash) | 将经纬度编码为字符串，用于附近位置搜索。 |
| [安全扩展SecurityHelper](https://newlifex.com/core/security_helper) | 集成RSA/AES/DES/MD5等多种加解密，简化安全操作。 |

</details>

<details>
<summary><b>日志与追踪</b>（2 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [日志ILog](https://newlifex.com/core/log) | 支持多种日志输出方式，提供强大的异步写入功能。 |
| [链路追踪ITracer](https://newlifex.com/core/tracer) | 强大的APM性能追踪，支持与星尘平台集成，并与 OpenTelemetry 双向互通。 |

</details>

<details>
<summary><b>定时调度</b>（3 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [高级定时器TimerX](https://newlifex.com/core/timerx) | 高精度定时器，支持Cron调度和异步周期执行。 |
| [Cron表达式](https://newlifex.com/core/cron) | 提供类似Linux Cron的定时任务，支持秒级匹配。 |
| [时间提供者TimeProvider](https://newlifex.com/core/timerx_timeprovider) | 统一时间抽象与高精度耗时计算，便于跨版本兼容。 |

</details>

<details>
<summary><b>配置系统</b>（4 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [配置系统Config](https://newlifex.com/core/config) | 统一配置框架，支持多格式、多来源和热加载机制。 |
| [配置提供者IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | 统一配置接口，支持回调和配置节点模型。 |
| [核心配置Setting](https://newlifex.com/core/setting) | 管理组件核心配置，控制如日志、网络等基础行为。 |
| [系统配置SysConfig](https://newlifex.com/core/sysconfig) | 管理应用系统配置，支持本地、分布式及加密配置。 |

</details>

<details>
<summary><b>序列化</b>（7 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [Json序列化](https://newlifex.com/core/json) | 高性能Json工具，支持多种复杂场景下的序列化。 |
| [Xml序列化](https://newlifex.com/core/xml) | 提供XML序列化和反序列化，支持对象与XML互转。 |
| [二进制序列化](https://newlifex.com/core/binary) | 高性能二进制序列化，支持复杂对象图和版本控制。 |
| [高级二进制序列化](https://newlifex.com/core/advanced_binary) | 协议消息全程零分配编解码的进阶指南。 |
| [Csv解析CsvFile](https://newlifex.com/core/csv_file) | 快速读写CSV文件，支持自定义分隔符和数据绑定。 |
| [Csv数据库CsvDb](https://newlifex.com/core/csv_db) | 将CSV文件作为数据库使用，支持基本的增删改查。 |
| [轻量级Excel读取器ExcelReader](https://newlifex.com/core/excel_reader) | 无需Office依赖，快速读取Excel，支持大数据量。 |

</details>

<details>
<summary><b>缓存</b>（7 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [统一缓存接口ICache](https://newlifex.com/core/icache) | 定义标准缓存操作，提供内存、Redis等多种实现。 |
| [缓存架构ICacheProvider](https://newlifex.com/core/icacheprovider) | 用于管理和提供ICache实例，支持多缓存并存。 |
| [内存缓存MemoryCache](https://newlifex.com/core/memory_cache) | 高性能的单机内存缓存，支持过期和容量策略。 |
| [对象池ObjectPool](https://newlifex.com/core/object_pool) | 用于复用昂贵对象，提高性能并减少GC压力。 |
| [对象池Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | 基于CAS的高性能通用对象池，降低GC压力。 |
| [分布式缓存NewLife.Redis](https://newlifex.com/core/redis) | 基于Redis的分布式缓存实现，支持多种数据结构。 |
| [向量存储IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | 向量集合管理接口，支持 Top-K 相似度检索，为 AI 应用提供向量检索基础设施。 |

</details>

<details>
<summary><b>高性能缓冲区</b>（7 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [数据包IPacket](https://newlifex.com/core/packet) | 零拷贝切片、链式包的核心数据包接口。 |
| [缓冲区Buffers](https://newlifex.com/core/buffers) | 高性能缓冲区操作库，汇总Span和池化写入等组件。 |
| [池化写入器PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | 基于ArrayPool的池化缓冲写入器，支持动态扩容。 |
| [Span读取器SpanReader](https://newlifex.com/core/span_reader) | 零分配高性能二进制流解析器，支持大小端。 |
| [Span写入器SpanWriter](https://newlifex.com/core/span_writer) | 零分配高性能二进制数据写入器，支持大小端。 |
| [Span辅助SpanHelper](https://newlifex.com/core/span_helper) | Span/ReadOnlySpan扩展方法集，简化内存操作。 |
| [序列读取器SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | 跨段顺序读取器（旧框架经垫片补齐，全11 TFM可用）。 |

</details>

<details>
<summary><b>应用框架</b>（15 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [对象容器ObjectContainer](https://newlifex.com/core/object_container) | 轻量级依赖注入，支持多种生命周期和注入模式。 |
| [轻量级应用主机Host](https://newlifex.com/core/host) | 简化应用托管，轻松构建长时间运行的后台服务。 |
| [插件框架IPlugin](https://newlifex.com/core/plugin) | 提供标准化的插件管理机制，实现应用模块化开发。 |
| [并行模型Actor](https://newlifex.com/core/actor) | 简化的并发编程模型，通过消息队列处理提升吞吐。 |
| [延迟队列DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | 聚合高频变更并批处理落地，降低写入放大。 |
| [事件总线EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | 进程内事件分发，支持主题路由和队列式消息。 |
| [弱引用委托WeakAction](https://newlifex.com/core/weak_action) | 解决事件订阅内存泄漏问题，实现安全的事件处理。 |
| [脚本引擎ScriptEngine](https://newlifex.com/core/script_engine) | 支持C#动态编译运行，提供灵活的脚本扩展能力。 |
| [压缩解压缩](https://newlifex.com/core/compression) | 集成多种压缩算法，提供统一接口进行数据压缩。 |
| [压缩归档Tar与7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | 覆盖Tar/Tar.Gz与7Zip进阶归档压缩用法。 |
| [机器信息MachineInfo](https://newlifex.com/core/machine_info) | 全面获取硬件信息，支持Windows和Linux系统。 |
| [拼音库PinYin](https://newlifex.com/core/pinyin) | 高效汉字转拼音库，支持全拼和简拼，性能优越。 |
| [数据集DbTable](https://newlifex.com/core/dbtable) | 内存中的数据表，方便进行数据处理和分析。 |
| [分页参数PageParameter](https://newlifex.com/core/page_parameter) | 封装分页逻辑，支持Web和API等多种场景分页。 |
| [雪花算法Snowflake](https://newlifex.com/core/snow_flake) | 分布式唯一ID生成算法，保证高并发下ID唯一性。 |

</details>

<details>
<summary><b>安全与令牌</b>（3 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [分布式数字签名令牌TokenProvider](https://newlifex.com/core/token_provider) | 用于生成和验证分布式令牌，保障多节点间安全。 |
| [Web通用令牌JwtBuilder](https://newlifex.com/core/jwt) | 提供JWT令牌的生成、解码和验证，简化Web验证。 |
| [令牌HTTP过滤器TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | HTTP请求自动注入令牌，拦截401/403自动续期。 |

</details>

<details>
<summary><b>网络库</b>（13 项）</summary>

| 功能点 | 主要特点 |
| --- | --- |
| [网络服务端NetServer](https://newlifex.com/core/netserver) | 高性能TCP/UDP/Unix域套接字服务端，封装了会话和数据处理。 |
| [网络客户端NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | 统一的TCP/UDP/Unix网络客户端，简化连接与收发。 |
| [消息IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | 请求-响应消息契约，承载方向语义与帧读写。 |
| [消息协议栈MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | 帧泵 + 无状态编解码器单栈，支持大帧流式体与背压。 |
| [数据管道Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | 零拷贝数据管道，对齐 System.IO.Pipelines 形态。 |
| [简易远程消息交换协议SRMP](https://newlifex.com/core/srmp) | 轻量级RPC协议，支持二进制序列化和UDP传输。 |
| [负载均衡与故障转移LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | 提供Failover/轮询/竞速三种多节点调度策略。 |
| [HTTP客户端ApiHttpClient](https://newlifex.com/core/api_http) | 基于HttpClient封装，简化Web API的调用和处理。 |
| [HTTP服务端HttpServer（嵌入式设备）](https://newlifex.com/core/httpserver) | 轻量级Http服务器，支持中间件管道、参数化路由与静态文件服务。 |
| [网络下载WebClientX](https://newlifex.com/core/webclient) | 增强版WebClient，支持多线程、断点续传下载。 |
| [WebSocket服务器（物联网下行通知神器）](https://newlifex.com/core/websocket) | 高性能WebSocket服务端，适用于物联网消息推送。 |
| [WebSocket双向通信](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | 介绍握手、帧处理、心跳与连接关闭实践。 |
| [文件存储客户端EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP文件存储客户端，对接EasyIO或OSS。 |

</details>

> RPC 通信（ApiServer/ApiClient）已移至 [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting)。

## 生态版图

NewLife 生态按依赖分层，各项目独立版本、独立发版。下面的版图在所有 NewLife 仓库中逐字一致；每个项目链接到代码仓库与主 NuGet 包。

| 项目 | 一句话定位 | 下载量 |
| --- | --- | --- |
| **基础底座** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 日志、配置、缓存、网络、序列化、定时调度与 APM 追踪。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 数据中间件：单表百亿级、自动分表、读写分离，支持 10 多种数据库。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高性能网络库：单机 177 万 pps、14.5 Gbps（TCP 1KB，2026-10 实测），400 万 Tcp 长连接。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 协议通信与 RPC 框架，面向 CS 应用与物联网设备。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 把 .NET 应用安装为操作系统守护进程：Windows 服务与 Linux systemd。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` 项目模板：控制台、Web、WebApi、服务、数据与物联网。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **数据存储与消息** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 客户端：微秒级延迟、百万级吞吐，内置消息队列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 纯托管 RocketMQ 客户端，支持 Apache RocketMQ 与阿里云消息队列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 客户端与服务端，客户端支持阿里云物联网。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 混合数据库：嵌入/网络双模，关系型、时序、消息队列与 KV 于一体。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql 驱动与数据访问辅助层，基于 NewLife 数据栈实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web、AI 与分布式** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 魔方快速开发平台：用户权限、SSO、OAuth 服务端与 CRUD 脚手架。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 星尘分布式服务平台：节点管理、APM、配置中心、注册中心与发布中心。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 蚂蚁调度：分布式任务与实时数据平台，支持分片、重试与回溯补算。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 统一接入 46 家大模型服务商，附带开箱即用的 ChatAI Web 对话应用。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **工业与物联网** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT 标准库，定义物联网通信协议契约与设备抽象。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 西门子 PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | 欧姆龙 PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | 三菱 Melsec PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC 客户端，用于工业数据采集。 | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 楼宇自控协议栈。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 开源轻量级 IoT 平台，含网关与设备管理。 | — |
| **车联网** | | |
| NewLife.JT808 | JT/T 808 车辆定位协议：70+ 消息体，覆盖 2011/2013/2019 三个版本。 | ![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads) |
| NewLife.JT809 | JT/T 809 部标平台互联协议，面向运输管理部门。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| NewLife.JT1078 | JT/T 1078 车载视频：RTP 解析、FLV/fMP4 转封装与直播推送。 | ![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads) |
| **领域组件** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | 一个包覆盖 Excel、Word、PPT、PDF 等 15+ 文档格式。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 跨平台音频：编解码、设备 IO、DSP、容器与流式处理。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 视频编解码、封装格式、帧处理与流媒体。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度/高德/腾讯/天地图统一接口，支持地理编码与坐标转换。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 归属地：离线内存映射库 + 在线 API 双源。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **工具与产品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 码神工具：网络、串口、加解密、正则、Modbus 与 MQTT。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 自托管 Git 源代码管理系统。 | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 从低版本宿主启动时，自动安装匹配的 .NET 运行时。 | — |

> 徽章取各项目主包；主题包、扩展包与卫星包见对应 NuGet 页面。JT808/JT1078 正在发布准备中，包上线后徽章自动显示下载量。未在表中列出的 30 多个包（FTP、LoRa、Office 扩展、各类协议驱动等）见 [NewLifeX 组织](https://github.com/NewLifeX) 与 [newlifex.com](https://newlifex.com)。

## 文档与示例

### 使用文档

- [核心库目录](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md)：核心组件文章索引（按模块分类）
- [AOT兼容性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)：NativeAOT 支持范围、实测结论（冒烟 <!--aot-smoke:start-->31/31<!--aot-smoke:end-->）、多 RID 覆盖矩阵与已知边界
- [多语言本地化](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)：异常与日志的多语言模型、语言表扩充、下游覆盖与运行时新增语言
- [下游多语言接入清单](https://github.com/NewLifeX/X/blob/master/Doc/下游多语言接入清单.md)：下游组件接入步骤、语言口径、CI 门禁与徽章模板
- [移动端技术架构](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md)：.NET 移动技术栈分层、UI 框架选型、APK 打包与 AOT 说明
- [Samples](https://github.com/NewLifeX/X/tree/master/Samples)：控制台、服务、Http、物联网与移动端示例

### 版本记录

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md)：版本变更记录

## 参与贡献

### 构建与测试

解决方案支持 Visual Studio（最新正式版）打开编译，也兼容 `dotnet build` 命令行；项目文件保持简单，克隆后即可开箱构建。

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- 全量并行测试：`run-tests.ps1`（多宿主并行 + 超时兜底 + trx 汇总）
- NativeAOT 冒烟：`AotSmoke/`，CI 见 `.github/workflows/aot.yml`
- 性能基准：`Benchmark/`（BenchmarkDotNet）

### 仓库结构

| 目录 | 说明 |
| --- | --- |
| `NewLife.Core/` | 核心库源码（日志、网络、缓存、序列化等） |
| `NewLife.Core.SourceGenerator/` | 源生成器，随主包分发到 `analyzers/` |
| `Doc/` | 能力文档与文章索引 |
| `Samples/` | 控制台 / 服务 / Http / 物联网 / 移动端示例 |
| `Benchmark/` | BenchmarkDotNet 性能基准 |
| `XUnitTest.Core/` | 主单元测试工程（net10.0） |
| `XUnitTest.Compat/`、`XUnitTest.Net6/` | 跨版本兼容性测试 |
| `AotSmoke/` | NativeAOT 冒烟工程 |

### 发版

推送 `v` 前缀 tag（如 `v12.0.2026.0927`）触发 `.github/workflows/publish.yml`。工作流完成打包后同时发布到 NuGet.org 与 GitHub Packages，并创建同名 GitHub Release（说明取自 `ChangeLog.md` 对应段落，资产含 nupkg/snupkg）；手工触发工作流只发包，不创建 Release。

### 研发过程文档

`Doc/` 目录同时维护与代码同步的能力文档体系：需求定义 → 功能追踪 → 架构设计。这些文档以中文撰写。

- [需求文档](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md)：愿景、核心目标与功能需求
- [功能清单](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md)：全部功能点的实现/测试/注释状态追踪
- [架构设计](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md)：分层、模块组件与关键设计决策

### 反馈

- 提交前请确保 `dotnet build` 与相关单元测试通过；代码风格与提交信息见仓库约定。
- 缺陷与建议请提 [Issue](https://github.com/NewLifeX/X/issues)，欢迎 Star 与 PR。

## 新生命开发团队

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

新生命团队成立于 2002 年，二十余年专注 .NET 与物联网技术生态，主导开源项目近百个（生态版图收录 34 个主干项目），为电力、物流、医疗、工控、互联网等行业提供软硬件一体化解决方案；以“学无先后达者为师”为信条。

开源组件在 NuGet 累计下载 <!--nuget-total:start-->6.48M<!--nuget-total:end--> 次（全生态口径，截至 2026-10）。

网站：https://newlifex.com · 开源组织：https://github.com/NewLifeX

<details>
<summary>微信公众号：智能大石头（扫码关注）</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## 许可证

本项目采用 [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE) 协议；公开强命名证书 `newlife.snk`，支持自行编译并替换程序集，也可修改源码后再次发行而无需声明来源。

> 如果本项目对你有帮助，欢迎 Star、分享给团队，或通过 Issue/PR 反馈改进建议。
