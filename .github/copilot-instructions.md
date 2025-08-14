# 项目概述

本项目是一个.Net类库形式的MIT开源项目，属于NewLife框架的一员，支持net45/net461/net462/netstandard2.0/netstandard2.1/netcoreapp3.1/net5.0/net6.0/net7.0/net8.0/net9.0等主流.Net版本，同时支持Windows特定版本(net5.0-windows至net9.0-windows)。  

NewLife框架是一个全面的 .NET 组件生态系统，它为构建可扩展的应用程序提供了高性能的基础设施。它提供日志、网络、序列化、缓存和多线程等基本功能，作为从 Web 服务到 IoT 设备的一系列应用程序的基础。

## 项目地址

- 源码地址：https://github.com/NewLifeX/X
- 文档地址：https://newlifex.com/core
- NuGet包：NewLife.Core

## 文件夹结构

- `/NewLife.Core`：包含核心类库源代码，主要的业务逻辑都在这里
- `/NewLife.Security`：包含安全扩展相关的类库源代码，加密解密等功能
- `/Test` 和 `/Test2`：包含可运行的局部模块测试用例代码，用于验证功能
- `/XUnitTest.Core`：包含xUnit单元测试源代码，确保代码质量
- `/Samples`：包含较完整的示例项目代码，包括Zero.HttpServer、Zero.EchoServer、Zero.Server、Zero.Desktop等
- `/Doc`：包含项目文档、图标和签名证书newlife.snk
- `/readme.md`：包含项目总体描述

## 技术栈和框架特性

### 支持的.NET版本
- **.NET Framework**: 4.5, 4.6.1, 4.6.2
- **.NET Standard**: 2.0, 2.1  
- **.NET Core**: 3.1
- **.NET**: 5.0, 6.0, 7.0, 8.0, 9.0
- **Windows特定版本**: net5.0-windows 到 net9.0-windows (支持WinForms)

### 核心功能模块
1. **基础扩展**: 类型转换、字符串扩展、进程扩展、路径扩展、IO扩展、安全扩展等
2. **实用组件**: 日志系统、链路追踪、定时器、机器信息、拼音库、依赖注入容器等  
3. **序列化与配置**: Json/Xml/二进制序列化、CSV处理、Excel读取、配置系统等
4. **数据缓存**: 统一缓存接口、内存缓存、分布式缓存、对象池等
5. **网络库**: TCP/UDP服务器客户端、HTTP客户端、WebSocket、RPC通信等

### 技术要求
- **C#版本**: 最新版(latest)，启用nullable和implicit usings
- **编译**: 支持Visual Studio和dotnet CLI
- **强命名**: 使用newlife.snk证书进行强命名签名
- **文档**: 生成XML文档文件用于IntelliSense

## 编码规范

### 基本规范
- **基础类型**: 使用.Net类型名而不是C#关键字（如String而不是string，Int32而不是int，Boolean而不是bool）
- **语法**: 使用最新版C#语法来简化代码，例如自动属性、模式匹配、表达式主体成员、record类型等
- **命名**: 遵循Pascal命名法用于类型和公共成员，camelCase用于私有字段和参数

### 文档注释要求
- 所有公开的类或成员都需要编写XML文档注释
- summary标签头尾放在同一行，内容简洁明了
- 如果注释内容过长则增加remarks标签来补充详细说明
- 使用param和returns标签描述参数和返回值
- 示例格式：
```csharp
/// <summary>获取或设置配置名称</summary>
/// <remarks>用于标识不同的配置实例，支持多配置并存</remarks>
public String Name { get; set; }

/// <summary>异步保存配置到指定路径</summary>
/// <param name="path">保存路径，为空时使用默认路径</param>
/// <returns>保存是否成功</returns>
public async Task<Boolean> SaveAsync(String? path = null)
```

### 异步编程规范
- 优先使用async/await模式，避免直接使用.Result或.GetAwaiter().GetResult()
- 异步方法名以Async结尾
- 使用ConfigureAwait(false)避免死锁，除非需要回到原始上下文
- 提供同步和异步版本的重载方法时，明确区分使用场景

### 错误处理
- 使用具体的异常类型而不是通用Exception
- 在日志中记录异常信息，使用XTrace.WriteException
- 对于性能敏感的代码，考虑使用try-parse模式而不是异常

### 性能优化原则
- 使用对象池减少GC压力
- 优先使用Span<T>和Memory<T>处理大数据
- 避免不必要的字符串拼接，使用StringBuilder或字符串插值
- 合理使用缓存避免重复计算

## 测试规范

### 单元测试
- 使用xUnit测试框架
- 测试类放在XUnitTest.Core项目中
- 测试方法使用[Fact]或[Theory]特性
- 测试方法名称要清晰表达测试意图
- 使用DisplayName提供中文描述

### 测试组织
- 按功能模块组织测试目录结构
- 每个类对应一个测试类，命名为{ClassName}Tests
- 复杂功能提供多个测试方法覆盖不同场景
- 使用临时文件进行文件操作测试，确保测试清理

## 项目特定注意事项

### 兼容性考虑
- 代码需要同时支持.NET Framework 4.5到.NET 9.0
- 使用条件编译指令处理平台差异(如#if NET5_0_OR_GREATER)
- Windows特定功能使用__WIN__宏控制
- 注意不同目标框架的API差异

### 依赖管理
- 尽量减少外部依赖，保持核心库的轻量级
- 不同目标框架使用不同的包引用(如System.Memory仅在旧框架引用)
- 条件引用Windows Desktop框架用于WinForms支持

### 安全和性能
- 启用AllowUnsafeBlocks用于高性能场景
- 使用强命名程序集确保安全性
- 关注内存使用和GC性能
- 网络操作考虑并发性能

### 日志和诊断
- 使用ILog接口进行日志记录
- 支持APM性能追踪
- 提供详细的错误信息用于问题诊断
- 考虑不同日志级别的性能影响
