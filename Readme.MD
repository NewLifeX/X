新生命团队基础框架X组件，包括`日志、数据库、网络、RPC、序列化、缓存、Windows服务、多线程`等模块，支持`.Net Framework/.netstandard/Mono`。

2002~2018，成千上万兄弟们努力的见证！  

国内文档中心：[http://doc.newlifex.com/](http://doc.newlifex.com/)  
国外文档中心：[https://newlifex.github.io/XDoc/](https://newlifex.github.io/XDoc/)  

## 新生命开源项目矩阵

| 项目 | 年份 | 状态 | .NET Core | 说明 |
| :------: | :------: | :------: | :------: | ------ |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | 维护中 | √ | 日志、网络、RPC、序列化、缓存、Windows服务、多线程 |
| [XCode](https://github.com/NewLifeX/X) | 2005 | 维护中 | √ | 数据中间件，MySQL、SQLite、SqlServer、Oracle |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | 维护中 | √ | Web魔方，权限基础框架，集成OAuth |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | 维护中 | √ | 网络库，千万级吞吐率，学习gRPC、Thrift |
| [XAgent](https://github.com/NewLifeX/XAgent) | 2008 | 重构中 | √ | 集群节点分布式计算平台，学习Hadoop |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | 设计中 | √ | 星尘，微服务平台，学习Dubbo |
| [XCoder](https://github.com/NewLifeX/XCoder) | 2006 | 维护中 | × | 码神工具，开发者必备 |
| [XProxy](https://github.com/NewLifeX/XProxy) | 2005 | 维护中 | √ | 产品级反向代理 |
| [XScript](https://github.com/NewLifeX/XScript) | 2010 | 维护中 | × | C#脚本引擎 |
| [XTemplate](https://github.com/NewLifeX/XTemplate) | 2008 | 维护中 | × | 模版引擎，学习TT |
| [NewLife.DNS](https://github.com/NewLifeX/NewLife.DNS) | 2011 | 维护中 | × | DNS代理服务器 |
| [NewLife.CMX](https://github.com/NewLifeX/NewLife.CMX) | 2013 | 维护中 | × | 内容管理系统 |
| [SmartOS](https://github.com/NewLifeX/SmartOS) | 2014 | 保密中 | C++11 | 嵌入式操作系统，面向ARM Cortex-M芯片架构 |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 2015 | 维护中 | × | Git管理系统 |
| [XLink](https://github.com/NewLifeX/XLink) | 2016 | 保密中 | √ | 物联网云平台 |
| [NoDb](https://github.com/NewLifeX/NoDb) | 2017 | 开发中 | √ | NoSQL数据库，百万级读写性能，学习Redis+MongoDB |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | 维护中 | √ | Redis客户端，微秒级延迟，百亿级项目验证 |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | 2018 | 开发中 | √ | MySql驱动 |
| [NewLife.Cache](https://github.com/NewLifeX/NewLife.Cache) | 2018 | 维护中 | √ | 自定义缓存服务器 |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018 | 维护中 | √ | 支持Apache RocketMQ和阿里云消息队列 |
| NewLife.Kafka | 2019 | 设计中 | √ | Kafka组件 |
| ConfigServer | 2019 | 设计中 | √ | 分布式配置中心，学习Apollo |
| [X组件 .NET2.0](https://github.com/NewLifeX/X_NET20) | 2002 | 存档中 | .NET2.0 | 日志、网络、RPC、序列化、缓存、Windows服务、多线程 |
| [X组件 .NET4.0](https://github.com/NewLifeX/X_NET40) | 2002 | 存档中 | .NET4.0 | 日志、网络、RPC、序列化、缓存、Windows服务、多线程 |

## 组件各组成部分
### 核心库 NewLife.Core
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
+ **[服务代理]** Windows服务、守护、监控

[日志]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Log
[网络]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Net
[RPC]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Remoting
[缓存]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Caching
[安全]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Security
[多线程]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Threading
[反射]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Reflection
[序列化]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Serialization
[服务代理]:https://github.com/NewLifeX/X/tree/master/NewLife.Core/Agent

### 数据中间件 [NewLife.XCode]
[NewLife.XCode](https://github.com/NewLifeX/X/tree/master/XCode)主要特点：
1，设计极致的缓存，超高性能
2，反向工程，根据实体类主动建立数据库表结构并初始化数据，支持8种数据库
3，无限分表分库，支持任意数据库，无需修改业务代码


## 未来路线图
1. [x] [魔方netcore版](https://github.com/NewLifeX/NewLife.Cube)。需要尽快推出，其它所有组件都已经支持netcore
2. [x] 产品级反向代理[XProxy](https://github.com/NewLifeX/XProxy)。一个XProxy服务支持多组反向代理设置，支持数据库和远程桌面等，netcore版，Web管理得等netcore魔方。比较简单，可以尽快完成。
3. [ ] 产品级分布式服务代理[XAgent](https://github.com/NewLifeX/XAgent)。XAgent.exe安装到各个Windows/Linux节点并注册成为服务，控制中心推送应用包到个节点，由XAgent启动并负责监管。工程量大，预计年底开工。
4. [ ] ApiServer支持http协议。ApiServer在Linux上大量使用，验证了它的可靠性和性能，如果能够直接支持http协议，即可在一定程度上替代WebApi，避免维护两套系统。
5. [ ] ApiServer支持非标协议。标准协议是4字节头部，为了在物联网领域得到扩大，有必要支持非标协议。工作量不小，需要考虑的东西挺多。
6. [ ] 发布[NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql)驱动。现在官方新版MySql驱动用起来还不错，自己的驱动倒是动力不强了，慢慢推进吧。
7. [ ] 发布NoDb数据库。至今为止最期待的大数据存储方案，各种算法，比较伤脑细胞。
8. [x] 开源[NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)。实现更多的Redis命令，列表、哈希字典等。比较简单，可以尽快搞定
9. [ ] 开源NewLife.Kafka。封装Apache官方驱动，实现生产消费接口IProducerConsumer。比较简单，尽快
10. [x] 升级[NewLife.Cache](https://github.com/NewLifeX/NewLife.Cache)。CacheServer/CacheClient由Json协议改为二进制协议，以获取极致性能。不太难
11. [x] 开源[NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ)。实现发布消息、消费消息、负载均衡，支持Apache RocketMQ和阿里云消息队列

## 新生命团队netcore服务器免费开放计划
为了便于大家学习测试netcore，我们计划提供1~3台公网Linux服务器（CentOS/Ubuntu），1vCPU+1G内存+100Mbps，为期1年，每周重置系统修改一次密码。
对使用者要求如下：
1，不干坏事。由Q群1600800监督管理
2，向源码库贡献源码或star，https://github.com/NewLifeX
3，博客分享学习经验

我们希望同学们不仅可以部署测试netcore应用，还可以在一台真实公网服务器上学习如何安装配置环境，观察应用系统运行指标，如何诊断系统异常等。

开放服务器暂定由Q群1600800监督管理，每周修改一次密码并重置系统，如有用户报告异常，核实后马上重置。
修改后的密码会在Q群1600800和源码库https://github.com/NewLifeX/X/blob/master/Readme.MD公布。

服务器已准备就绪！第二期账号如下：
SSH地址：`centos.newlifex.com`
账号密码：`root / nx@466364748`

感谢笑笑和漫游者近半年来给大家带来的netcore知识，以他们账号作为密码，实至名归！

#### 新生命开发团队
网站：http://www.NewLifeX.com
博客：https://nnhy.cnblogs.com
QQ群：1600800

## 项目源码位置
`注意：X组件具有15年漫长历史，源码库保留有2010年以来所有修改记录，并一直保持更新，请确保获取得到最新版本源代码`
国内 http://git.NewLifeX.com/NewLife/X
国外 https://github.com/NewLifeX/X
