![XCode](https://www.newlifex.com/logo.png)  
新生命基础框架X组件，包括`算法、日志、数据库、网络、RPC、序列化、缓存、多线程`等模块，支持`.NET Framework/.NET Core/Mono/Xamarin`。

2002~2020，成千上万兄弟姐妹们努力的见证！  

核心库教程：[https://www.yuque.com/smartstone/nx](https://www.yuque.com/smartstone/nx)  
XCode教程：[https://www.yuque.com/smartstone/xcode](https://www.yuque.com/smartstone/xcode)  

## 新生命开源项目矩阵
各项目默认支持net5.0/netstandard2.0/net4.5/net4.0/  

|                               项目                               | 年份  |  状态  | .NET Core | 说明                                                       |
| :--------------------------------------------------------------: | :---: | :----: | :-------: | ---------------------------------------------------------- |
|                             基础组件                             |       |        |           | 支撑其它中间件以及产品项目                                 |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 维护中 |     √     | 算法、日志、网络、RPC、序列化、缓存、多线程                |
|              [XCode](https://github.com/NewLifeX/X)              | 2005  | 维护中 |     √     | 数据中间件，MySQL、SQLite、SqlServer、Oracle、Postgresql   |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 维护中 |     √     | 网络库，千万级吞吐率，各种常见网络协议                     |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 维护中 |     √     | Web魔方，企业级快速开发框架，集成OAuth2.0（Client/Server） |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 维护中 |     √     | 服务管理框架，Windows服务、Linux的Systemd                  |
|                              中间件                              |       |        |           | 对接各知名中间件平台                                       |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | 维护中 |     √     | Redis客户端，微秒级延迟，百亿级项目验证                    |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | 维护中 |     √     | 支持Apache RocketMQ和阿里云消息队列，十亿级项目验证        |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 维护中 |     √     | 物联网消息协议，客户端支持阿里云物联网                     |
|     [NewLife.LoRa](https://github.com/NewLifeX/NewLife.LoRa)     | 2016  | 维护中 |     √     | 超低功耗的物联网远程通信协议LoRaWAN                        |
|   [NewLife.Thrift](https://github.com/NewLifeX/NewLife.Thrift)   | 2019  | 维护中 |     √     | Thrift协议实现                                             |
|     [NewLife.Hive](https://github.com/NewLifeX/NewLife.Hive)     | 2019  | 维护中 |     √     | 纯托管读写Hive，Hadoop数据仓库，基于Thrift协议             |
|             [NoDb](https://github.com/NewLifeX/NoDb)             | 2017  | 开发中 |     √     | NoSQL数据库，百万级kv读写性能，持久化                      |
|      [NewLife.Ftp](https://github.com/NewLifeX/NewLife.Ftp)      | 2008  | 维护中 |     √     | Ftp客户端实现                                              |
|                             产品平台                             |       |        |           | 产品平台级，编译部署即用，个性化自定义                     |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 维护中 |     √     | 蚂蚁调度系统，大数据实时计算平台                           |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 维护中 |     √     | 星尘，微服务平台，分布式平台                               |
|            [XLink](https://github.com/NewLifeX/XLink)            | 2016  | 维护中 |     √     | 物联网云平台                                               |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 维护中 |     √     | 产品级反向代理                                             |
|          [XScript](https://github.com/NewLifeX/XScript)          | 2010  | 维护中 |     ×     | C#脚本引擎                                                 |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 维护中 |   C++11   | 嵌入式操作系统，完全独立自主，ARM Cortex-M芯片架构         |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | 维护中 |     ×     | Git管理系统                                                |
|                               其它                               |       |        |           |                                                            |
|           [XCoder](https://github.com/NewLifeX/XCoder)           | 2006  | 维护中 |     √     | 码神工具，开发者必备                                       |
|        [XTemplate](https://github.com/NewLifeX/XTemplate)        | 2008  | 维护中 |     √     | 模版引擎，T4(Text Template)语法                            |
|       [X组件 .NET2.0](https://github.com/NewLifeX/X_NET20)       | 2002  | 存档中 |  .NET2.0  | 日志、网络、RPC、序列化、缓存、Windows服务、多线程         |

## 核心库 NewLife.Core
核心组件，支撑其它所有组件。
主要功能包括：
+ **[日志]** 统一ILog接口，内置控制台、文本文件、WinForm控件和网络日志等实现
+ **[网络]** 单点最高84.5万长连接
+ **[RPC]** 单点最高处理能力2256万tps
+ **[缓存]** 统一ICache接口，内置MemoryCache、Redis、DbCache实现
+ **[安全]** AES/DES/RC4/RSA/DSA/CRC
+ **[多线程]** 定时调度TimerX
+ **[反射]** 快速反射、脚本引擎ScriptEngine
+ **[序列化]** Binary/Json/Xml

[日志]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Log
[网络]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Net
[RPC]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Remoting
[缓存]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Caching
[安全]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Security
[多线程]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Threading
[反射]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Reflection
[序列化]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Serialization

## 数据中间件 [NewLife.XCode]
[NewLife.XCode](https://github.com/NewLifeX/X/tree/master/XCode)主要特点：  
1，设计极致的缓存，超高性能  
2，反向工程，根据实体类主动建立数据库表结构并初始化数据，支持8种数据库  
3，无限分表分库，支持任意数据库，无需修改业务代码  

## 新生命开发团队
`新生命团队始于2002年，部分开源项目具有18年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://www.NewLifeX.com  
开源：https://github.com/NewLifeX  
博客：https://nnhy.cnblogs.com  
QQ群：1600800/1600838  
