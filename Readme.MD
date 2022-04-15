# NewLife.Core - 核心库

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/x?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/x?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)

核心基础组件，日志（文件/网络）、配置（XML/Json/Http）、缓存（内存/Redis）、网络（Tcp/Udp/Http）、RPC框架、序列化（Binary/XML/Json）、APM性能追踪。  

核心库教程：[https://newlifex.com/core](https://newlifex.com/core)  

## 新生命项目矩阵
各项目默认支持net6.0/netstandard2.1，旧版（2021.1225）支持net5.0/netstandard2.0/net4.5/net4.0/net2.0  

|                               项目                               | 年份  |  状态  |  .NET6  | 说明                                                                                 |
| :--------------------------------------------------------------: | :---: | :----: | :-----: | ------------------------------------------------------------------------------------ |
|                             基础组件                             |       |        |         | 支撑其它中间件以及产品项目                                                           |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 维护中 |    √    | 日志、配置、缓存、网络、RPC、序列化、APM性能追踪                                     |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 维护中 |    √    | 大数据中间件，MySQL/SQLite/SqlServer/Oracle/TDengine/达梦，自动建表分表              |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 维护中 |    √    | 网络库，单机千万级吞吐率（2266万tps），单机百万级连接（400万Tcp）                    |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 维护中 |    √    | 魔方快速开发平台，集成了用户权限、SSO登录、OAuth服务端等，单表100亿级项目验证        |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 维护中 |    √    | 服务管理框架，把应用安装成为操作系统守护进程，Windows服务、Linux的Systemd            |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | 维护中 |    √    | Zero零代脚手架，各种类型拷贝即用的项目模板，Web应用、WebApi、网络服务、消息服务      |
|                              中间件                              |       |        |         | 对接知名中间件平台                                                                 |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | 维护中 |    √    | Redis客户端，微秒级延迟，百万级吞吐，丰富的消息队列，百亿级数据量项目验证            |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | 维护中 |    √    | 支持Apache RocketMQ和阿里云消息队列，十亿级项目验证                                  |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 维护中 |    √    | 物联网消息协议，客户端支持阿里云物联网                                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | 维护中 |    √    | IoT标准库，定义物联网领域的各种通信协议标准规范                                               |
|     [NewLife.LoRa](https://github.com/NewLifeX/NewLife.LoRa)     | 2016  | 维护中 |    √    | 超低功耗的物联网远程通信协议LoRaWAN                                                  |
|                             产品平台                             |       |        |         | 产品平台级，编译部署即用，个性化自定义                                               |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 维护中 |    √    | 蚂蚁调度，分布式大数据计算平台（实时/离线），蚂蚁搬家分片思想，万亿级数据量项目验证  |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 维护中 |    √    | 星尘，分布式服务平台，节点管理、APM监控中心、配置中心、注册中心、发布中心、消息中心  |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 维护中 |    √    | 码神工具，众多开发者工具，网络、串口、加解密、正则表达式、Modbus                     |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 维护中 |    √    | 产品级反向代理，NAT代理、Http代理                                                    |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 维护中 |  C++11  | 嵌入式操作系统，完全独立自主，ARM Cortex-M芯片架构                                   |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | 维护中 |    ×    | Git源代码管理系统                                                                    |
|                           NewLife.A2                           | 2019  |  商用  |    √    | 嵌入式工业计算机，物联网边缘网关，高性能.NET主机，应用于工业、农业、交通、医疗       |
|                          NewLife.IoT                           | 2020  |  商用  |    √    | 物联网整体解决方案，建筑业、环保、农业，软硬件及大数据分析一体化，十万级点位项目验证 |
|                          NewLife.UWB                          | 2020  |  商用  |    √    | 厘米级高精度室内定位，软硬件一体化，与其它系统联动，大型展厅项目验证                 |

## 核心库 NewLife.Core
核心组件，支撑其它所有组件。
主要功能包括：
+ **[日志]** 统一ILog接口，内置控制台、文本文件、WinForm控件和网络日志等实现
+ **[网络]** 单点最高400万长连接
+ **[RPC]** 单点最高处理能力2266万tps
+ **[缓存]** 统一ICache接口，内置MemoryCache、Redis实现
+ **[安全]** AES/DES/RC4/RSA/DSA/CRC
+ **[多线程]** 定时调度TimerX
+ **[APM]** 性能追踪，分布式应用必备
+ **[序列化]** Binary/Json/Xml

[日志]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Log
[网络]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Net
[RPC]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Remoting
[缓存]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Caching
[安全]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Security
[多线程]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Threading
[APM]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Log
[序列化]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Serialization

## 新生命开发团队
![XCode](https://newlifex.com/logo.png)  

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的开源NewLife系列组件已被广泛应用于各行业，Nuget累计下载量高达60余万次。  
团队开发的大数据核心组件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台NewLife.IoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。  

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IT服务供应商。  

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公众号：  
![智能大石头](https://newlifex.com/stone.jpg)  
