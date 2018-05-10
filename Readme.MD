新生命团队基础框架X组件，包括`网络、数据库、安全、多线程、反射、序列化、模版引擎、服务代理、远程过程调用`等模块，包括`Mvc后台魔方、码神工具、消息队列`等子系统，支持`Mono/Android/iOS/NetStandard`。

#### 新生命开发团队  
网站：http://www.NewLifeX.com  
博客：https://nnhy.cnblogs.com  
QQ群：1600800  

## 项目源码位置
`注意：X组件具有15年漫长历史，源码库保留有2010年以来所有修改记录，并一直保持更新，请确保获取得到最新版本源代码`  
国内 http://git.NewLifeX.com/NewLife/X  
国外 https://github.com/NewLifeX/X  

## 组件各组成部分
### 核心库 NewLife.Core
核心组件，支撑其它所有组件。  
主要功能包括：  
+ **[网络库]** 支持企业应用服务十万级并发 Tcp/Udp/Http/WebSocket/IPv6
+ **[安全]** AES/DES/RC4/RSA/DSA/CRC
+ **[多线程]** 定时调度TimerX
+ **[反射]** 快速反射、脚本引擎ScriptEngine、动态对象
+ **[序列化]** Binary/Json/Xml
+ **[服务代理]** Windows服务、守护、监控
+ **[远程过程调用RPC]** Tcp/Udp/Http/Json、身份验证、数据加密

[网络库]:Net/Readme.md "支持企业应用服务十万级并发"
[安全]:Security/Readme.md
[多线程]:Threading/Readme.md
[反射]:Reflection/Readme.md
[序列化]:Serialization/Readme.md
[服务代理]:Agent/Readme.md
[远程过程调用RPC]:Remoting/Readme.md

### Android库 NewLife.Android
Android版核心库，共用代码，供Xamarin Android开发使用

### iOS库 NewLife.iOS
iOS版核心库，共用代码，供Xamarin iOS开发使用

### [数据中间件 NewLife.XCode]
主要特点：  
1，设计极致的缓存，超高性能  
2，反向工程，根据实体类主动建立数据库表结构并初始化数据，支持8种数据库  
3，无限分表分库，支持任意数据库，无需修改业务代码  

### 魔方Mvc管理后台 NewLife.Cube
酷比魔方，Bootstrap风格的Mvc权限管理后台，集成管理员、角色、菜单、权限、日志等基本功能模块。

### 网络协议库 NewLife.Net
支持DNS/DHCP等几十种常见以太网协议的解析

### 码神工具 XCoder
由一系列开发者工具组成：  
1，数据模型工具  
2，网络调试工具  
3，串口调试工具  
4，正则表达式工具  
5，图标处理工具  
6，文件信息处理工具  

### 模版引擎 XTemplate
使用C#编写模版，T4风格，主要用于生成实体代码和页面  

### 消息队列 NewLife.MQ
发布与订阅  
