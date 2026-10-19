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

NewLife framework is a comprehensive .NET component ecosystem that provides high-performance infrastructure for building scalable applications. It offers essential capabilities such as logging, networking, serialization, caching, and multithreading, serving as the foundation for a range of applications from web services to IoT devices.  

Core library tutorial: [https://newlifex.com/core](https://newlifex.com/core)  

**Contents**: [Framework Overview](#framework-overview) · [Why NewLife.Core](#why-newlifecore) · [Quick Start](#quick-start) · [Main Features](#main-features) · [Documentation Index](#documentation-index) · [Repository layout](#repository-layout) · [Build and test](#build-and-test) · [NewLife Project Matrix](#newlife-project-matrix) · [Contributing and feedback](#contributing-and-feedback) · [License](#license)

## Framework Overview

NewLife is composed of multiple component libraries, with NewLife.Core providing the foundation. The framework supports multiple target frameworks from net45 to net10.0 (.NET Framework 4.5 through .NET 10.0), thereby achieving broad compatibility across different environments.

**NativeAOT**: The core path is supported (measured in <!--aot-scenarios:start-->31<!--aot-scenarios:end--> scenarios + CI regression protection); dynamic capabilities such as plugins and scripting are not supported by design, see [AOT Compatibility Analysis (中文)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) for the boundaries.


## Why NewLife.Core

- **One package for common infrastructure**: logging, configuration, caching, networking, serialization, scheduling, object container and DI — a single reference is enough.
- **Broad version coverage**: 11 target frameworks in one package (net45 → net10.0, including Windows variants), the same API for legacy systems and new projects.
- **NativeAOT ready**: <!--aot-scenarios:start-->31<!--aot-scenarios:end--> core scenarios measured and guarded by a multi-RID CI smoke matrix; boundaries in [AOT Compatibility Analysis](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md).
- **Restrained dependencies, permissive license**: BCL and Microsoft compatibility packages only; MIT, so you can modify and rebrand (see [License](#license)).

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

### Build and customize

The team has always maintained an open attitude: it not only supports opening the solution and compiling in VisualStudio (latest release), but is also compatible with `dotnet build` command-line compilation. The project files abandon complex features in pursuit of simplicity and ease of use, truly ready to use out of the box.

We publish the strong-name certificate `newlife.snk` to support compiling and replacing assemblies on your own.

NewLife components have been co-created by the community for more than 20 years and use the MIT open source license. **Anyone can modify and redistribute them at will** (no need to indicate the source)! Many enterprises build internal development frameworks based on this, and can even achieve private customization by batch-replacing all `NewLife` strings in the source code with their company name.

## Documentation Index

### Usage docs

- [Core Library Contents (中文)](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): Index of core component articles
- [AOT Compatibility Analysis (中文)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): NativeAOT support scope, measured conclusions (smoke test <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), usage guidance, and known boundaries

### Development docs

The repository's `Doc/` directory maintains a capability documentation system (requirements definition → feature tracking → architecture design):

- [Requirements (中文)](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): Vision, core goals, and functional requirements
- [Feature List (中文)](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): Implementation/test/comment status tracking for all feature points
- [Architecture Design (中文)](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): Layering, module components, and key design decisions

## Main Features


<details>
<summary><b>Basic Extensions</b> (9)</summary>

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
<summary><b>Application Framework</b> (16)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Object Container ObjectContainer](https://newlifex.com/core/object_container) | Lightweight dependency injection supporting multiple lifetimes and injection patterns. |
| [Lightweight Application Host Host](https://newlifex.com/core/host) | Simplifies application hosting to easily build long-running background services. |
| [Plugin Framework IPlugin](https://newlifex.com/core/plugin) | Provides a standardized plugin management mechanism for modular application development. |
| [Parallel Model Actor](https://newlifex.com/core/actor) | Simplified concurrent programming model that improves throughput through message queue processing. |
| [Pipeline Model Pipeline](https://github.com/NewLifeX/X/blob/master/Doc/管道模型Pipeline.md) ⚠️ Retired, replaced by the protocol pattern (frame pump) | Bidirectional processing chain model unifying the Read/Write/Open/Close flow. |
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
<summary><b>Cloud Services</b> (1)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Latitude/Longitude Hash GeoHash](https://newlifex.com/core/geo_hash) | Encodes latitude and longitude into strings for nearby location search. |

</details>

<details>
<summary><b>Networking</b> (15)</summary>

| Feature | Key Characteristics |
| --- | --- |
| [Network Server NetServer](https://newlifex.com/core/netserver) | High-performance TCP/UDP/Unix domain socket server encapsulating sessions and data handling. |
| [Network Client NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Unified TCP/UDP/Unix network client that simplifies connection and send/receive. |
| [Message IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Request-response message contract carrying direction semantics and frame read/write. |
| [Message Protocol Stack MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Frame pump + stateless codec single stack supporting large streaming frame bodies and backpressure. |
| [Data Pipeline Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Zero-copy data pipeline aligned with the System.IO.Pipelines shape. |
| [Simple Remote Message Exchange Protocol SRMP](https://newlifex.com/core/srmp) | Lightweight RPC protocol supporting binary serialization and UDP transport. |
| [Load Balancing and Failover LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Provides three multi-node scheduling strategies: Failover, polling, and racing. |
| [RPC Communication ApiServer](https://newlifex.com/core/api_server) | ⚠️ Moved to [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [RPC Communication ApiClient](https://newlifex.com/core/api_client) | ⚠️ Moved to [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [HTTP Client ApiHttpClient](https://newlifex.com/core/api_http) | Based on an HttpClient wrapper, simplifying the calling and handling of Web APIs. |
| [HTTP Server HttpServer (Embedded Devices)](https://newlifex.com/core/httpserver) | Lightweight Http server supporting middleware pipelines, parameterized routing, and static file serving. |
| [Network Download WebClientX](https://newlifex.com/core/webclient) | Enhanced WebClient supporting multi-threaded and resumable downloads. |
| [WebSocket Server (a powerful tool for IoT downlink notifications)](https://newlifex.com/core/websocket) | High-performance WebSocket server suitable for IoT message push. |
| [WebSocket Bidirectional Communication](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Introduces practices for handshake, frame handling, heartbeat, and connection close. |
| [File Storage Client EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP file storage client for EasyIO or OSS. |

</details>

> ⚠️ Baidu Maps/AMap have been extracted into separate NuGet packages.


## Repository layout

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

## Build and test

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Full parallel run: `run-tests.ps1` (multi-host parallel + timeout + trx summary)
- NativeAOT smoke: `AotSmoke/`, CI in `.github/workflows/aot.yml`
- Benchmarks: `Benchmark/` (BenchmarkDotNet)

## NewLife Project Matrix

All projects support net10.0/netstandard2.1/netstandard2.0/net462/net4.5 by default; older versions (2024.0801) support net4.0/net2.0  

| Project | Year | Description |
| :---: | :---: | --- |
| Basic Components |  | Supports other middleware and product projects |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | Core library: logging, configuration, caching, networking, serialization, APM performance tracing |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | Big data middleware, single tables at the tens-of-billions scale, MySql/SQLite/SqlServer/Oracle/PostgreSql/DaMeng, automatic table sharding, read-write splitting |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | Networking library, tens of millions of throughput per machine (22.66 million tps), millions of connections per machine (4 million long-lived TCP connections) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011 | Protocol communication library providing a CS application communication framework, supporting Http/RPC communication frameworks, high throughput, low overhead and easy access for IoT devices |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | Cube rapid development platform integrating user permissions, SSO login, OAuth server, etc., validated on a project with 10 billion records in a single table |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 2008 | Service management component that installs applications as operating system daemons: Windows services and Linux systemd |
| [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero) | 2020 | Zero-generation scaffolding, the NewLife.Templates project template based on the NewLife component ecosystem: Web, WebApi, Service |
| Middleware |  | Connects to well-known middleware platforms |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Redis client with microsecond-level latency, millions-level throughput, rich message queues, validated on a project with tens of billions of records |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018 | Pure managed RocketMQ client supporting Apache RocketMQ and Alibaba Cloud Message Queue, validated on billion-level projects |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | 2019 | IoT messaging protocol, MqttClient/MqttServer, the client supports Alibaba Cloud IoT |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | IoT standard library defining various communication protocol standards and specifications in the IoT field |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | ModbusTcp/ModbusRTU/ModbusASCII, implemented based on the IoT standard library, supporting the ZeroIoT platform and IoTEdge gateway |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | Siemens PLC protocol, implemented based on the IoT standard library, supporting the IoT platform and IoTEdge |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 2022 | Map component library wrapping Baidu Maps, AMap, Tencent Maps, and Tianditu |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 2023 | Audio codec library, PCM/ADPCMA/G711A/G722U/WAV/AAC |
| Product Platforms |  | Product platform level, ready to use after compilation and deployment, with personalized customization |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | Stardust, distributed service platform: node management, APM monitoring center, configuration center, registration center, release center |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | AntJob, distributed big data computing platform (real-time/offline), ant-moving sharding concept, validated on a project with trillions of records |
| [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP) | 2021 | Enterprise ERP: product management, customer management, sales management, supplier management |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 2006 | CrazyCoder, numerous developer tools: networking, serial ports, encryption/decryption, regular expressions, Modbus, MQTT |
| [EasyIO](https://github.com/NewLifeX/EasyIO) | 2023 | Simple file storage, supporting centralized file storage in distributed systems. |
| [XProxy](https://github.com/NewLifeX/XProxy) | 2005 | Product-grade reverse proxy, NAT proxy, Http proxy |
| [HttpMeter](https://github.com/NewLifeX/HttpMeter) | 2022 | Http stress testing tool |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 2015 | Git source code management system |
| [SmartOS](https://github.com/NewLifeX/SmartOS) | 2014 | Embedded operating system, completely independent and self-developed, supporting the ARM Cortex-M chip architecture |
| [SmartA2](https://github.com/NewLifeX/SmartA2) | 2019 | Embedded industrial computer, IoT edge gateway, high-performance .NET8 host, applied in industry, agriculture, transportation, and healthcare |
| FIoT IoT Platform | 2020 | Complete IoT solutions for buildings, environmental protection, and agriculture, integrating software, hardware, and big data analysis, validated on a project with 100,000-level points per machine |


---

## NewLife Development Team

![XCode](https://newlifex.com/logo.png)

The NewLife team (NewLife) was founded in 2002 and is an IoT industry solution provider for the new era, dedicated to providing software and hardware application solution consulting, system architecture planning, and development services. 5.23 million+ cumulative NuGet downloads. The team's core components (XCode / Redis / Stardust / AntJob / IoT, etc.) have been applied at scale in industries such as power, industry, transportation, environmental protection, healthcare, and cultural heritage.

We continue to improve, striving to become a long-term trusted .NET / IoT / distributed technology partner.

Website: https://newlifex.com   Open source organization: https://github.com/NewLifeX  
WeChat Official Account:  
![智能大石头](https://newlifex.com/stone.jpg)

## Contributing and feedback

- Before submitting, make sure `dotnet build` and the relevant unit tests pass; see the repository conventions for code style and commit messages.
- Report bugs and suggestions via [Issue](https://github.com/NewLifeX/X/issues); Stars and PRs are welcome.
- See [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md) for release notes.

## License

This project is licensed under [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE); the strong-name certificate `newlife.snk` is published so you can build and replace assemblies yourself.

> If this project is helpful to you, welcome to Star it, share it with your team, or provide improvement suggestions via Issue/PR.
