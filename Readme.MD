# NewLife.Core - 核心库

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/x?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/x?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)

​	NewLife框架是一个全面的 .NET 组件生态系统，它为构建可扩展的应用程序提供了高性能的基础设施。它提供日志、网络、序列化、缓存和多线程等基本功能，作为从 Web 服务到 IoT 设备的一系列应用程序的基础。  

核心库教程：[https://newlifex.com/core](https://newlifex.com/core)  



## 框架概述

​	NewLife 由多个组件库组成，NewLife.Core 提供了基础。该框架支持从 .NET 2.0 到 .NET 10.0 的多个 .NET 版本，从而在不同环境中实现广泛的兼容性。



## 快速拥有

​	使用NewLife组件的最简便方式是从Nuget引用，例如在项目Nuget管理中搜索`NewLife.Core` 并引入。

​	NewLife组件由社区共创20多年，使用MIT开源协议，**任何人可任意修改并再次发行**（无需声明来源）！许多企业基于此构建内部开发框架时，甚至可通过批量替换源码中所有`NewLife`字符串为贵公司名实现私有化定制。

​	团队始终秉承开放态度，不仅支持VisualStudio（最新正式版）打开解决方案编译，也兼容`dotnet build`命令行编译，项目文件摒弃复杂功能以追求简单易用，真正做到开箱即用。

​	我们公开强命名证书`newlife.snk`以支持独自编译替换程序集。



​	命令行中运行以下命令快速体验NewLife组件：

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```



## 主要功能


| 模块分类         | 功能点                                                                       | 主要特点                                         |
| ---------------- | ---------------------------------------------------------------------------- | ------------------------------------------------ |
| **基础扩展**     | [类型转换Utility](https://newlifex.com/core/utility)                         | 提供高效的类型转换，支持各种格式和异常处理。     |
|                  | [字符串扩展StringHelper](https://newlifex.com/core/string_helper)            | 包含多种字符串操作，如截取、编码和格式化等。     |
|                  | [进程扩展ProcessHelper](https://newlifex.com/core/process_helper)            | 简化进程操作，提供获取和管理进程信息的便捷方法。 |
|                  | [路径扩展PathHelper](https://newlifex.com/core/path_helper)                  | 智能处理多种路径格式，实现代码跨平台兼容性。     |
|                  | [数据扩展IOHelper](https://newlifex.com/core/io_helper)                      | 优化IO操作，提供高效的数据流与字节数组转换。     |
|                  | [安全扩展SecurityHelper](https://newlifex.com/core/security_helper)          | 集成多种加解密算法，简化数据安全和校验操作。     |
|                  | [可销毁DisposeBase](https://newlifex.com/core/disposebase)                   | 提供标准的资源释放模式，有效防止内存和资源泄漏。 |
|                  | [反射扩展Reflect](https://newlifex.com/core/reflect)                         | 高性能反射工具，快速进行对象成员的读写操作。     |
|                  | [运行时信息Runtime](https://newlifex.com/core/runtime)                       | 提供简单方法获取应用运行时信息，便于调试诊断。   |
| **实用组件**     | [日志ILog](https://newlifex.com/core/log)                                    | 支持多种日志输出方式，提供强大的异步写入功能。   |
|                  | [链路追踪ITracer](https://newlifex.com/core/tracer)                          | 强大的APM性能追踪，支持与星尘平台集成。          |
|                  | [高级定时器TimerX](https://newlifex.com/core/timerx)                         | 高精度定时器，支持Cron调度和异步周期执行。       |
|                  | [Cron表达式](https://newlifex.com/core/cron)                                 | 提供类似Linux Cron的定时任务，支持秒级匹配。     |
|                  | [机器信息MachineInfo](https://newlifex.com/core/machine_info)                | 全面获取硬件信息，支持Windows和Linux系统。       |
|                  | [拼音库PinYin](https://newlifex.com/core/pinyin)                             | 高效汉字转拼音库，支持全拼和简拼，性能优越。     |
|                  | [对象容器ObjectContainer](https://newlifex.com/core/object_container)        | 轻量级依赖注入，支持多种生命周期和注入模式。     |
|                  | [轻量级应用主机Host](https://newlifex.com/core/host)                         | 简化应用托管，轻松构建长时间运行的后台服务。     |
|                  | [插件框架IPlugin](https://newlifex.com/core/plugin)                          | 提供标准化的插件管理机制，实现应用模块化开发。   |
|                  | [并行模型Actor](https://newlifex.com/core/actor)                             | 简化的并发编程模型，通过消息队列处理提升吞吐。   |
|                  | [脚本引擎ScriptEngine](https://newlifex.com/core/script_engine)              | 支持C#动态编译运行，提供灵活的脚本扩展能力。     |
|                  | [压缩解压缩](https://newlifex.com/core/compression)                          | 集成多种压缩算法，提供统一接口进行数据压缩。     |
|                  | [弱引用委托WeakAction](https://newlifex.com/core/weak_action)                | 解决事件订阅内存泄漏问题，实现安全的事件处理。   |
|                  | [分布式数字签名令牌TokenProvider](https://newlifex.com/core/token_provider)  | 用于生成和验证分布式令牌，保障多节点间安全。     |
|                  | [Web通用令牌JwtBuilder](https://newlifex.com/core/jwt)                       | 提供JWT令牌的生成、解码和验证，简化Web验证。     |
|                  | [百度地图接口BaiduMap](https://newlifex.com/core/baidu_map)                  | 封装百度地图API，提供IP定位和地址解析等功能。    |
|                  | [高德地图接口AMap](https://newlifex.com/core/amap)                           | 封装高德地图API，提供地址解析等服务。            |
|                  | [阿里云对象存储OssClient](https://newlifex.com/core/oss)                     | 简化阿里云OSS操作，提供文件上传下载管理功能。    |
| **序列化与配置** | [Json序列化](https://newlifex.com/core/json)                                 | 高性能Json工具，支持多种复杂场景下的序列化。     |
|                  | [Xml序列化](https://newlifex.com/core/xml)                                   | 提供XML序列化和反序列化，支持对象与XML互转。     |
|                  | [二进制序列化](https://newlifex.com/core/binary)                             | 高性能二进制序列化，支持复杂对象图和版本控制。   |
|                  | [Csv解析CsvFile](https://newlifex.com/core/csv_file)                         | 快速读写CSV文件，支持自定义分隔符和数据绑定。    |
|                  | [Csv数据库CsvDb](https://newlifex.com/core/csv_db)                           | 将CSV文件作为数据库使用，支持基本的增删改查。    |
|                  | [轻量级Excel读取器ExcelReader](https://newlifex.com/core/excel_reader)       | 无需Office依赖，快速读取Excel，支持大数据量。    |
|                  | [配置系统IConfigProvider](https://newlifex.com/core/config)                  | 提供统一配置框架，支持多种来源和热加载机制。     |
|                  | [核心配置Setting](https://newlifex.com/core/setting)                         | 管理组件核心配置，控制如日志、网络等基础行为。   |
|                  | [系统配置SysConfig](https://newlifex.com/core/sysconfig)                     | 管理应用系统配置，支持本地、分布式及加密配置。   |
| **数据缓存**     | [统一缓存接口ICache](https://newlifex.com/core/icache)                       | 定义标准缓存操作，提供内存、Redis等多种实现。    |
|                  | [缓存架构ICacheProvider](https://newlifex.com/core/icacheprovider)           | 用于管理和提供ICache实例，支持多缓存并存。       |
|                  | [内存缓存MemoryCache](https://newlifex.com/core/memory_cache)                | 高性能的单机内存缓存，支持过期和容量策略。       |
|                  | [分布式缓存NewLife.Redis](https://newlifex.com/core/redis)                   | 基于Redis的分布式缓存实现，支持多种数据结构。    |
|                  | [字典缓存DictionaryCache](https://newlifex.com/core/dictionary_cache)        | 增强型字典，增加缓存有效期和上限等实用功能。     |
|                  | [对象池ObjectPool](https://newlifex.com/core/object_pool)                    | 用于复用昂贵对象，提高性能并减少GC压力。         |
|                  | [数据包IPacket](https://newlifex.com/core/packet)                            | 高性能数据包封装，优化大数据读写和网络传输。     |
|                  | [分页参数PageParameter](https://newlifex.com/core/page_parameter)            | 封装分页逻辑，支持Web和API等多种场景分页。       |
|                  | [数据集DbTable](https://newlifex.com/core/dbtable)                           | 内存中的数据表，方便进行数据处理和分析。         |
|                  | [雪花算法Snowflake](https://newlifex.com/core/snow_flake)                    | 分布式唯一ID生成算法，保证高并发下ID唯一性。     |
|                  | [经纬度哈希GeoHash](https://newlifex.com/core/geo_hash)                      | 将经纬度编码为字符串，用于附近位置搜索。         |
| **网络库**       | [网络服务器NetServer](https://newlifex.com/core/netserver)                   | 高性能TCP/UDP服务端，封装了会话和数据处理。      |
|                  | [网络客户端ISocketClient](https://newlifex.com/core/socket_client)           | 统一的TCP/UDP客户端接口，简化网络连接操作。      |
|                  | [ApiHttpClient客户端](https://newlifex.com/core/api_http)                    | 基于HttpClient封装，简化Web API的调用和处理。    |
|                  | [简易远程消息交换协议SRMP](https://newlifex.com/core/srmp)                   | 轻量级RPC协议，支持二进制序列化和UDP传输。       |
|                  | [数据包编码器PacketCodec](https://newlifex.com/core/packet_codec)            | 用于网络粘包处理，支持多种消息分包解决方案。     |
|                  | [RPC通信ApiServer](https://newlifex.com/core/api_server)                     | 快速构建RPC服务端，自动注册服务并提供接口。      |
|                  | [RPC通信ApiClient](https://newlifex.com/core/api_client)                     | 快速创建RPC客户端，支持服务发现和故障转移。      |
|                  | [网络下载WebClientX](https://newlifex.com/core/webclient)                    | 增强版WebClient，支持多线程、断点续传下载。      |
|                  | [Web服务器HttpServer（嵌入式设备）](https://newlifex.com/core/httpserver)    | 轻量级Http服务器，适合嵌入式和物联网设备使用。   |
|                  | [WebSocket服务器（物联网下行通知神器）](https://newlifex.com/core/websocket) | 高性能WebSocket服务端，适用于物联网消息推送。    |



## 新生命项目矩阵
各项目默认支持net10.0/netstandard2.1/netstandard2.0/net4.62/net4.5，旧版（2024.0801）支持net4.0/net2.0  

|                               项目                               | 年份  | 说明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基础组件                             |       | 支撑其它中间件以及产品项目                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心库，日志、配置、缓存、网络、序列化、APM性能追踪                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大数据中间件，单表百亿级，MySql/SQLite/SqlServer/Oracle/PostgreSql/达梦，自动分表，读写分离 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 网络库，单机千万级吞吐率（2266万tps），单机百万级连接（400万Tcp长连接）                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 协议通信库，提供CS应用通信框架，支持Http/RPC通信框架，高吞吐，物联网设备低开销易接入        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速开发平台，集成了用户权限、SSO登录、OAuth服务端等，单表100亿级项目验证               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服务管理组件，把应用安装成为操作系统守护进程，Windows服务、Linux的Systemd                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero零代脚手架，基于NewLife组件生态的项目模板NewLife.Templates，Web、WebApi、Service        |
|                              中间件                              |       | 对接知名中间件平台                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis客户端，微秒级延迟，百万级吞吐，丰富的消息队列，百亿级数据量项目验证                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ纯托管客户端，支持Apache RocketMQ和阿里云消息队列，十亿级项目验                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物联网消息协议，MqttClient/MqttServer，客户端支持阿里云物联网                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT标准库，定义物联网领域的各种通信协议标准规范                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基于IoT标准库实现，支持ZeroIoT平台和IoTEdge网关            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西门子PLC协议，基于IoT标准库实现，支持IoT平台和IoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地图组件库，封装百度地图、高德地图、腾讯地图、天地图                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音频编解码库，PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             产品平台                             |       | 产品平台级，编译部署即用，个性化自定义                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星尘，分布式服务平台，节点管理、APM监控中心、配置中心、注册中心、发布中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 蚂蚁调度，分布式大数据计算平台（实时/离线），蚂蚁搬家分片思想，万亿级数据量项目验证         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企业ERP，产品管理、客户管理、销售管理、供应商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 码神工具，众多开发者工具，网络、串口、加解密、正则表达式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 简易文件存储，支持分布式系统中文件集中存储。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 产品级反向代理，NAT代理、Http代理                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http压力测试工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git源代码管理系统                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式操作系统，完全独立自主，支持ARM Cortex-M芯片架构                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工业计算机，物联网边缘网关，高性能.NET8主机，应用于工业、农业、交通、医疗             |
|                          FIoT物联网平台                          | 2020  | 物联网整体解决方案，建筑、环保、农业，软硬件及大数据分析一体化，单机十万级点位项目验证      |
|                        UWB高精度室内定位                         | 2020  | 厘米级（10~20cm）高精度室内定位，软硬件一体化，与其它系统联动，大型展厅项目验证             |



---
# 新生命开发团队
![XCode](https://newlifex.com/logo.png)

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。NuGet 累计下载 507万+。团队核心组件（XCode / Redis / Stardust / AntJob / IoT 等）已在电力、工业、交通、环保、医疗、文博等行业大规模应用。

我们持续改进，力求成为值得长期信赖的 .NET / IoT / 分布式技术伙伴。

网站：https://newlifex.com   开源组织：https://github.com/NewLifeX  
微信公众号：  
![智能大石头](https://newlifex.com/stone.jpg)

---
> 如果本项目对你有帮助，欢迎 Star、分享给团队，或通过 Issue/PR 反馈改进建议。
