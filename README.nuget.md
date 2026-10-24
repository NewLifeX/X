<!-- generated from README.md by sync-readme-package.ps1; do not edit -->

# NewLife.Core - Core Library

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)
[![test](https://github.com/NewLifeX/X/actions/workflows/test.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/test.yml)
<!-- metrics:start -->
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-31%2F31-brightgreen?logo=dotnet)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-11%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)
<!-- metrics:end -->
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [简体中文](https://github.com/NewLifeX/X/blob/master/README.zh-CN.md) | [繁體中文](https://github.com/NewLifeX/X/blob/master/README.zh-TW.md) | [Español](https://github.com/NewLifeX/X/blob/master/README.es.md) | [Deutsch](https://github.com/NewLifeX/X/blob/master/README.de.md) | [日本語](https://github.com/NewLifeX/X/blob/master/README.ja.md) | [Français](https://github.com/NewLifeX/X/blob/master/README.fr.md) | [Português](https://github.com/NewLifeX/X/blob/master/README.pt.md) | [Русский](https://github.com/NewLifeX/X/blob/master/README.ru.md) | [한국어](https://github.com/NewLifeX/X/blob/master/README.ko.md) | [العربية](https://github.com/NewLifeX/X/blob/master/README.ar.md)
<!-- lang:end -->

**NewLife.Core** is the foundation of the NewLife ecosystem: one package for logging, configuration, caching, networking, serialization, scheduling, object container and APM tracing. It is NativeAOT-ready, ships runtime messages in <!--locale-count:start-->31<!--locale-count:end--> languages, targets 11 frameworks from net45 to net10.0, and powers XCode, Cube, Redis, Stardust and 30+ sibling projects.

Core library tutorial: [https://newlifex.com/core](https://newlifex.com/core)  

**Contents**: [Quick Start](#quick-start) · [Why NewLife.Core](#why-newlifecore) · [Main Features](#main-features) · [Ecosystem Map](#ecosystem-map) · [Documentation](#documentation) · [Contributing](#contributing) · [Team](#team) · [License](#license)

## Quick Start

### Install

```bash
dotnet add package NewLife.Core
```

### Minimal example

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### Template scaffolding

Run the following commands in a command line to quickly experience NewLife components:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### Platform support

| Item | Coverage |
| --- | --- |
| Target frameworks | 11 in one package: net45, net461, net462, netstandard2.0, netstandard2.1, net6.0, net6.0-windows, net8.0, net8.0-windows, net10.0, net10.0-windows |
| OS and architecture | Windows, Linux, macOS, Android, iOS; x64, x86, arm64, arm, loongarch64, riscv64, s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> core scenarios measured; multi-RID CI smoke matrix (win-x64, linux-x64, linux-arm64 and linux-loongarch64 are blocking gates). Plugins and scripting are not AOT-compatible by design — see [AOT Compatibility Analysis](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |
| Runtime localization | <!--locale-count:start-->31<!--locale-count:end--> built-in locales; log and exception messages are localized at runtime without changing call sites (see [Localization](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)) |
| Legacy frameworks | net40: `dotnet add package NewLife.Core --version 10.10.2024.801-net40` (net40/net45/net461); net20: `--version 10.10.2024.701-net20` (net20 only). Frozen 2024-08 builds that do not track mainline updates. |

## Why NewLife.Core

- **One package for common infrastructure**: logging, configuration, caching, networking, serialization, scheduling, object container and DI — a single reference is enough.
- **Broad coverage, modern runtime**: 11 target frameworks in one package (net45 → net10.0, including Windows variants), NativeAOT-ready on the core path, and runtime messages localized into <!--locale-count:start-->31<!--locale-count:end--> languages — the same API for legacy systems and new projects.
- **Proven at ecosystem scale**: the same foundation runs XCode (single tables of 10 billion rows), Net (1.77M pps / 14.5 Gbps, TCP 1KB), Redis and Stardust (tens of billions of records).
- **Restrained dependencies, permissive license**: BCL and Microsoft compatibility packages only; MIT, so you can modify and rebrand (see [License](#license)).

## Main Features

<details>
<summary><b>Basic Extensions</b> (10)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Type Conversion Utility](https://newlifex.com/core/utility) | Provides efficient type conversion with support for various formats and exception handling. |
| [String Extensions StringHelper](https://newlifex.com/core/string_helper) | Includes various string operations such as truncation, encoding, and formatting. |
| [Path Extensions PathHelper](https://newlifex.com/core/path_helper) | Intelligently handles multiple path formats to achieve cross-platform compatibility of code. |
| [Data Extensions IOHelper](https://newlifex.com/core/io_helper) | Optimizes IO operations, providing efficient conversion between data streams and byte arrays. |
| [Process Extensions ProcessHelper](https://newlifex.com/core/process_helper) | Simplifies process operations with convenient methods for obtaining and managing process information. |
| [Runtime Information Runtime](https://newlifex.com/core/runtime) | Provides simple methods to obtain application runtime information, convenient for debugging and diagnostics. |
| [Disposable DisposeBase](https://newlifex.com/core/disposebase) | Provides a standard resource release pattern that effectively prevents memory and resource leaks. |
| [Reflection Extensions Reflect](https://newlifex.com/core/reflect) | High-performance reflection tooling for fast reading and writing of object members. |
| [Security Extensions SecurityHelper](https://newlifex.com/core/security_helper) | Integrates RSA/AES/DES/MD5 and other encryption and decryption algorithms, simplifying security operations. |
| [Latitude/Longitude Hash GeoHash](https://newlifex.com/core/geo_hash) | Encodes latitude and longitude into strings for nearby location search. |

</details>

<details>
<summary><b>Logging and Tracing</b> (2)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Logging ILog](https://newlifex.com/core/log) | Supports multiple log output methods and provides powerful asynchronous writing. |
| [Tracing ITracer](https://newlifex.com/core/tracer) | Powerful APM performance tracing with support for integration with the Stardust platform. |

</details>

<details>
<summary><b>Scheduling</b> (3)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Advanced Timer TimerX](https://newlifex.com/core/timerx) | High-precision timer supporting Cron scheduling and asynchronous periodic execution. |
| [Cron Expressions](https://newlifex.com/core/cron) | Provides Linux Cron-like scheduled tasks with second-level matching. |
| [Time Provider TimeProvider](https://newlifex.com/core/timerx_timeprovider) | Unified time abstraction and high-precision elapsed time calculation, convenient for cross-version compatibility. |

</details>

<details>
<summary><b>Configuration System</b> (4)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Configuration System Config](https://newlifex.com/core/config) | Unified configuration framework supporting multiple formats, multiple sources, and hot reload. |
| [Configuration Provider IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | Unified configuration interface supporting callbacks and a configuration node model. |
| [Core Configuration Setting](https://newlifex.com/core/setting) | Manages core component configuration, controlling basic behaviors such as logging and networking. |
| [System Configuration SysConfig](https://newlifex.com/core/sysconfig) | Manages application system configuration with support for local, distributed, and encrypted configuration. |

</details>

<details>
<summary><b>Serialization</b> (7)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Json Serialization](https://newlifex.com/core/json) | High-performance Json tool supporting serialization in various complex scenarios. |
| [Xml Serialization](https://newlifex.com/core/xml) | Provides XML serialization and deserialization, supporting conversion between objects and XML. |
| [Binary Serialization](https://newlifex.com/core/binary) | High-performance binary serialization supporting complex object graphs and version control. |
| [Advanced Binary Serialization](https://newlifex.com/core/advanced_binary) | An advanced guide to zero-allocation encoding and decoding throughout protocol messages. |
| [Csv Parser CsvFile](https://newlifex.com/core/csv_file) | Fast reading and writing of CSV files with support for custom delimiters and data binding. |
| [Csv Database CsvDb](https://newlifex.com/core/csv_db) | Uses CSV files as a database, supporting basic create, read, update, and delete operations. |
| [Lightweight Excel Reader ExcelReader](https://newlifex.com/core/excel_reader) | Reads Excel quickly without Office dependencies, supporting large data volumes. |

</details>

<details>
<summary><b>Caching</b> (7)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Unified Cache Interface ICache](https://newlifex.com/core/icache) | Defines standard cache operations, providing multiple implementations such as memory and Redis. |
| [Cache Architecture ICacheProvider](https://newlifex.com/core/icacheprovider) | Manages and provides ICache instances, supporting multiple coexisting caches. |
| [Memory Cache MemoryCache](https://newlifex.com/core/memory_cache) | High-performance standalone memory cache supporting expiration and capacity policies. |
| [Object Pool ObjectPool](https://newlifex.com/core/object_pool) | Reuses expensive objects to improve performance and reduce GC pressure. |
| [Object Pool Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | CAS-based high-performance general-purpose object pool that reduces GC pressure. |
| [Distributed Cache NewLife.Redis](https://newlifex.com/core/redis) | Redis-based distributed cache implementation supporting multiple data structures. |
| [Vector Store IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | Vector collection management interface supporting Top-K similarity search, providing vector retrieval infrastructure for AI applications. |

</details>

<details>
<summary><b>High-Performance Buffers</b> (7)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Data Packet IPacket](https://newlifex.com/core/packet) | Core data packet interface for zero-copy slicing and chained packets. |
| [Buffers](https://newlifex.com/core/buffers) | High-performance buffer operation library aggregating components such as Span and pooled writers. |
| [Pooled Writer PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | ArrayPool-based pooled buffer writer supporting dynamic expansion. |
| [Span Reader SpanReader](https://newlifex.com/core/span_reader) | Zero-allocation high-performance binary stream parser supporting little/big endian. |
| [Span Writer SpanWriter](https://newlifex.com/core/span_writer) | Zero-allocation high-performance binary data writer supporting little/big endian. |
| [Span Helper SpanHelper](https://newlifex.com/core/span_helper) | A collection of Span/ReadOnlySpan extension methods that simplifies memory operations. |
| [Sequence Reader SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | Cross-segment sequential reader (patched with shims for older frameworks, available on all 11 TFMs). |

</details>

<details>
<summary><b>Application Framework</b> (15)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Object Container ObjectContainer](https://newlifex.com/core/object_container) | Lightweight dependency injection supporting multiple lifetimes and injection patterns. |
| [Lightweight Application Host Host](https://newlifex.com/core/host) | Simplifies application hosting to easily build long-running background services. |
| [Plugin Framework IPlugin](https://newlifex.com/core/plugin) | Provides a standardized plugin management mechanism for modular application development. |
| [Parallel Model Actor](https://newlifex.com/core/actor) | Simplified concurrent programming model that improves throughput through message queue processing. |
| [Deferred Queue DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Aggregates high-frequency changes and persists them in batches, reducing write amplification. |
| [Event Bus EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | In-process event dispatch supporting topic routing and queue-style messages. |
| [Weak Reference Delegate WeakAction](https://newlifex.com/core/weak_action) | Solves the memory leak problem of event subscriptions and enables safe event handling. |
| [Script Engine ScriptEngine](https://newlifex.com/core/script_engine) | Supports dynamic C# compilation and execution, providing flexible scripting extensibility. |
| [Compression and Decompression](https://newlifex.com/core/compression) | Integrates multiple compression algorithms and provides a unified interface for data compression. |
| [Archive Compression Tar and 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Covers advanced archive compression usage for Tar/Tar.Gz and 7Zip. |
| [Machine Information MachineInfo](https://newlifex.com/core/machine_info) | Comprehensively obtains hardware information with support for Windows and Linux systems. |
| [Pinyin Library PinYin](https://newlifex.com/core/pinyin) | Efficient Chinese character to Pinyin library supporting full and abbreviated spelling with superior performance. |
| [Data Table DbTable](https://newlifex.com/core/dbtable) | An in-memory data table for convenient data processing and analysis. |
| [Paging Parameter PageParameter](https://newlifex.com/core/page_parameter) | Encapsulates paging logic, supporting pagination in various scenarios such as Web and API. |
| [Snowflake Algorithm Snowflake](https://newlifex.com/core/snow_flake) | Distributed unique ID generation algorithm guaranteeing ID uniqueness under high concurrency. |

</details>

<details>
<summary><b>Security and Tokens</b> (3)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Distributed Digital Signature Token TokenProvider](https://newlifex.com/core/token_provider) | Generates and validates distributed tokens, ensuring security between multiple nodes. |
| [Web General Token JwtBuilder](https://newlifex.com/core/jwt) | Provides generation, decoding, and validation of JWT tokens, simplifying Web authentication. |
| [Token HTTP Filter TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Automatically injects tokens into HTTP requests, intercepting 401/403 for automatic renewal. |

</details>

<details>
<summary><b>Networking</b> (13)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Network Server NetServer](https://newlifex.com/core/netserver) | High-performance TCP/UDP/Unix domain socket server encapsulating sessions and data handling. |
| [Network Client NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Unified TCP/UDP/Unix network client that simplifies connection and send/receive. |
| [Message IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Request-response message contract carrying direction semantics and frame read/write. |
| [Message Protocol Stack MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Frame pump + stateless codec single stack supporting large streaming frame bodies and backpressure. |
| [Data Pipeline Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Zero-copy data pipeline aligned with the System.IO.Pipelines shape. |
| [Simple Remote Message Exchange Protocol SRMP](https://newlifex.com/core/srmp) | Lightweight RPC protocol supporting binary serialization and UDP transport. |
| [Load Balancing and Failover LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Provides three multi-node scheduling strategies: Failover, polling, and racing. |
| [HTTP Client ApiHttpClient](https://newlifex.com/core/api_http) | Based on an HttpClient wrapper, simplifying the calling and handling of Web APIs. |
| [HTTP Server HttpServer (Embedded Devices)](https://newlifex.com/core/httpserver) | Lightweight Http server supporting middleware pipelines, parameterized routing, and static file serving. |
| [Network Download WebClientX](https://newlifex.com/core/webclient) | Enhanced WebClient supporting multi-threaded and resumable downloads. |
| [WebSocket Server (a powerful tool for IoT downlink notifications)](https://newlifex.com/core/websocket) | High-performance WebSocket server suitable for IoT message push. |
| [WebSocket Bidirectional Communication](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Introduces practices for handshake, frame handling, heartbeat, and connection close. |
| [File Storage Client EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP file storage client for EasyIO or OSS. |

</details>

> RPC (`ApiServer`/`ApiClient`) has moved to [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting).

## Ecosystem Map

NewLife is a layered ecosystem of independently versioned open-source projects. The map below is shared verbatim by every NewLife repository; each project links to its repository and its primary NuGet package.

| Project | What it does | Downloads |
| --- | --- | --- |
| **Foundation** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Logging, configuration, caching, networking, serialization, scheduling and APM tracing. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Data middleware: single tables at the tens-of-billions scale, automatic sharding, read-write splitting, 10+ databases. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | High-performance network library: 1.77M pps / 14.5 Gbps on a single machine (TCP 1KB, 2026-10), 4M long-lived TCP connections. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Protocol communication and RPC framework for client/server applications and IoT devices. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installs .NET applications as OS daemons: Windows services and Linux systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` project templates for console, web, API, service, data and IoT projects. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Data & Messaging** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis client with microsecond latency, millions of ops/s and built-in message queues. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Pure managed RocketMQ client for Apache RocketMQ and Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT client and server, including Alibaba Cloud IoT connectivity. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Hybrid database in C#: embedded or networked, relational, time-series, message queue and KV in one. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql driver and helper layer built on the NewLife data stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI & Distributed** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Rapid admin-platform framework: permissions, SSO, OAuth server and CRUD scaffolding. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Distributed service platform: node management, APM, configuration center, registry and release center. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Distributed task and real-time data scheduling platform with sharding, retries and backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Unified client for 46 LLM providers plus ChatAI, a ready-to-run web chat application. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrial & IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT standard library defining protocol contracts and device abstractions. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII implementation on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Siemens PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Omron PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Mitsubishi Melsec PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC client for industrial data access. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet building-automation protocol stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Lightweight open-source IoT platform with gateway and device management. | — |
| **Vehicle Telematics** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 vehicle positioning protocol: 70+ message bodies, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 platform-to-platform protocol for transport authorities. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 in-vehicle video: RTP parsing, FLV/fMP4 remuxing and live streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Domain Components** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | One package for Excel, Word, PPT, PDF and 15+ document formats. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Cross-platform audio: codecs, device I/O, DSP, containers and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Video codecs, container formats, frame processing and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Unified interface over Baidu, AMap, Tencent and Tianditu with geocoding and coordinate conversion. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 geolocation with an offline memory-mapped database and an online API fallback. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Tools & Products** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Developer toolbox: network, serial port, crypto, regex, Modbus and MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Self-hosted Git source code management system. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installs the matching .NET runtime automatically when launched from an older host. | — |

> Badges show the primary package of each project; theme, extension and satellite packages are listed on their NuGet pages. The 30+ packages not listed here (FTP, LoRa, Office extensions, protocol drivers and more) are available under the [NewLifeX organization](https://github.com/NewLifeX) and on [newlifex.com](https://newlifex.com).

## Documentation

### Guides

- [Core Library Contents](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): index of core component articles, grouped by module
- [AOT Compatibility Analysis](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): NativeAOT scope, measured conclusions (smoke <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), RID matrix and known boundaries
- [Localization](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md): supported languages and basic usage
- [Mobile architecture](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md): .NET mobile stack, UI framework selection, APK packaging and AOT notes
- [Samples](https://github.com/NewLifeX/X/tree/master/Samples): console, service, Http, IoT and mobile samples

### Release notes

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md): version history

## Contributing

### Build and test

The solution opens in Visual Studio (latest release) and also builds with the .NET CLI; project files stay deliberately simple so that a fresh clone builds out of the box.

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Full parallel run: `Tools/Scripts/run-tests.ps1` (multi-host parallel, timeout guard and trx summary)
- NativeAOT smoke: `AotSmoke/`, CI in `.github/workflows/aot.yml`
- Benchmarks: `Benchmark/` (BenchmarkDotNet)

### Repository layout

| Directory | Description |
| --- | --- |
| `NewLife.Core/` | Core library sources (logging, networking, caching, serialization, …) |
| `NewLife.Core.SourceGenerator/` | Source generator shipped as `analyzers/` inside the package |
| `Doc/` | Capability docs and article index |
| `Samples/` | Console / service / Http / IoT / mobile samples |
| `Benchmark/` | BenchmarkDotNet performance benchmarks |
| `XUnitTest.Core/` | Main unit tests (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | Cross-version compatibility tests |
| `AotSmoke/` | NativeAOT smoke project |

### Release

Push a `v`-prefixed tag (for example `v12.0.2026.1024`) to trigger `.github/workflows/publish.yml`. The workflow packs the package, publishes it to NuGet.org and GitHub Packages, and creates a GitHub Release whose notes are taken from the matching `ChangeLog.md` section (assets include nupkg and snupkg). A manual run only publishes packages and does not create a Release.

### Development docs

The `Doc/` directory keeps a capability documentation set that mirrors the code: requirements → feature tracking → architecture. These documents are written in Chinese.

- [Requirements](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): vision, core goals and functional requirements
- [Feature List](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): implementation, test and comment status of every feature
- [Architecture Design](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): layering, components and key design decisions

### Feedback

- Before submitting, make sure `dotnet build` and the relevant unit tests pass; follow the repository conventions for code style and commit messages.
- Report bugs and suggestions via [Issue](https://github.com/NewLifeX/X/issues); Stars and PRs are welcome.

## Team

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

Founded in 2002, the NewLife team has spent more than two decades on the .NET and IoT ecosystem, maintains nearly 100 open-source projects (34 of them core to this map), and delivers integrated software and hardware solutions for power, logistics, healthcare, industrial control and internet industries. We live by the motto "learning has no seniority — mastery earns respect".

Open-source components have been downloaded <!--nuget-total:start-->6.48M<!--nuget-total:end--> times from NuGet (ecosystem-wide, as of 2026-10).

Website: https://newlifex.com · Open source organization: https://github.com/NewLifeX

<details>
<summary>WeChat Official Account: 智能大石头 (scan to follow)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## License

This project is licensed under [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE); the strong-name certificate `newlife.snk` is published so you can build and replace assemblies yourself, and the sources may be modified and redistributed without attribution.

> If this project is helpful to you, welcome to Star it, share it with your team, or provide improvement suggestions via Issue/PR.

