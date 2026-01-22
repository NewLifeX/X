# NewLife.Core 使用文档汇总

## 概述

NewLife.Core 是一个全功能的 .NET 基础类库，提供日志、网络、序列化、缓存、多线程等基础功能。

**仓库**: https://github.com/NewLifeX/X  
**官方文档**: https://newlifex.com/core  
**Nuget包**: [NewLife.Core](https://www.nuget.org/packages/NewLife.Core)

---

## 文档目录

### ?? 实用工具

核心工具模块，应用开发中常用的功能。

| 模块 | 文档 | 说明 |
|------|------|------|
| **链路追踪** | [链路追踪ITracer.md](链路追踪ITracer.md) | 分布式APM，性能监控与调用追踪 |
| **日志系统** | [日志ILog.md](日志ILog.md) | 统一日志接口，支持文件/控制台/网络日志 |
| **高级定时器** | [高级定时器TimerX.md](高级定时器TimerX.md) | 毫秒级定时器，支持Cron表达式 |
| **Cron表达式** | [Cron表达式.md](Cron表达式.md) | 解析器Cron，计算下次执行时间点 |
| **机器信息** | [机器信息MachineInfo.md](机器信息MachineInfo.md) | 获取硬件和系统信息 |
| **运行时信息** | [运行时信息Runtime.md](运行时信息Runtime.md) | 应用运行时信息 |
| **拼音库** | [拼音库PinYin.md](拼音库PinYin.md) | 高效汉字转拼音 |
| **对象容器** | [对象容器ObjectContainer.md](对象容器ObjectContainer.md) | 轻量级依赖注入 |
| **应用主机** | [轻量级应用主机Host.md](轻量级应用主机Host.md) | 轻应用生命周期托管 |
| **插件框架** | [插件框架IPlugin.md](插件框架IPlugin.md) | 通用插件系统 |
| **并行模型Actor** | [并行模型Actor.md](并行模型Actor.md) | 简化并发编程 |
| **JWT令牌** | [Web通用令牌JwtBuilder.md](Web通用令牌JwtBuilder.md) | JWT生成与验证 |
| **事件总线** | [事件总线EventBus.md](事件总线EventBus.md) | 进程内事件订阅发布 |

### ?? 基础扩展

常用的扩展方法和工具类。

| 模块 | 文档 | 说明 |
|------|------|------|
| **类型转换** | [类型转换Utility.md](类型转换Utility.md) | ToInt/ToDateTime等类型转换 |
| **字符串扩展** | [字符串扩展StringHelper.md](字符串扩展StringHelper.md) | 截取、加密、格式化等 |
| **进程扩展** | [进程扩展ProcessHelper.md](进程扩展ProcessHelper.md) | 进程管理与信息获取 |
| **路径扩展** | [路径扩展PathHelper.md](路径扩展PathHelper.md) | 跨平台路径处理 |
| **数据扩展** | [数据扩展IOHelper.md](数据扩展IOHelper.md) | IO读写优化 |
| **安全扩展** | [安全扩展SecurityHelper.md](安全扩展SecurityHelper.md) | 加解密算法封装 |
| **可销毁基类** | [可销毁DisposeBase.md](可销毁DisposeBase.md) | 资源释放模式 |
| **反射扩展** | [反射扩展Reflect.md](反射扩展Reflect.md) | 高性能反射工具 |

### ?? 序列化与配置

数据序列化和配置管理。

| 模块 | 文档 | 说明 |
|------|------|------|
| **JSON序列化** | [JSON序列化.md](JSON序列化.md) | 轻量级JSON处理 |
| **XML序列化** | [XML序列化.md](XML序列化.md) | XML序列化与反序列化 |
| **二进制序列化** | [二进制序列化Binary.md](二进制序列化Binary.md) | 高性能二进制序列化 |
| **CSV文件** | [CSV文件CsvFile.md](CSV文件CsvFile.md) | CSV文件读写 |
| **CSV数据库** | [CSV数据库CsvDb.md](CSV数据库CsvDb.md) | CSV作为数据库使用 |
| **Excel读取器** | [Excel读取器ExcelReader.md](Excel读取器ExcelReader.md) | 轻量级Excel读取 |
| **配置系统** | [配置系统Config.md](配置系统Config.md) | 统一配置框架 |
| **配置提供者** | [配置提供者IConfigProvider.md](配置提供者IConfigProvider.md) | 配置提供者详解 |

### ?? 数据缓存

高性能数据处理工具。

| 模块 | 文档 | 说明 |
|------|------|------|
| **缓存系统** | [缓存系统ICache.md](缓存系统ICache.md) | 统一缓存接口 |
| **对象池** | [对象池Pool.md](对象池Pool.md) | 高性能对象复用 |
| **数据包** | [数据包IPacket.md](数据包IPacket.md) | 零拷贝数据包 |
| **数据集** | [数据集DbTable.md](数据集DbTable.md) | 内存数据表 |
| **雪花算法** | [雪花算法Snowflake.md](雪花算法Snowflake.md) | 分布式唯一ID |

### ?? Span/Buffer 操作

高性能内存操作工具。

| 模块 | 文档 | 说明 |
|------|------|------|
| **Span辅助** | [Span辅助SpanHelper.md](Span辅助SpanHelper.md) | Span扩展方法 |
| **Span读取器** | [Span读取器SpanReader.md](Span读取器SpanReader.md) | 高效二进制读取 |
| **Span写入器** | [Span写入器SpanWriter.md](Span写入器SpanWriter.md) | 高效二进制写入 |
| **缓冲区** | [缓冲区Buffers.md](缓冲区Buffers.md) | 缓冲区管理 |
| **池化写入器** | [池化写入器PooledByteBufferWriter.md](池化写入器PooledByteBufferWriter.md) | 池化字节写入器 |

### ?? 网络通信

网络通信和HTTP客户端。

| 模块 | 文档 | 说明 |
|------|------|------|
| **网络服务端** | [网络服务端NetServer.md](网络服务端NetServer.md) | 高性能TCP/UDP服务端 |
| **网络客户端** | [网络客户端NetClient.md](网络客户端NetClient.md) | 统一TCP/UDP客户端接口 |
| **ApiHttpClient** | [HTTP客户端ApiHttpClient.md](HTTP客户端ApiHttpClient.md) | 面向Web API调用 |
| **HTTP服务端** | [HTTP服务端HttpServer.md](HTTP服务端HttpServer.md) | 轻量级HTTP服务器 |

---

## 快速导航

### 常用功能

- **日志输出**: [日志ILog.md](日志ILog.md)
- **定时任务**: [高级定时器TimerX.md](高级定时器TimerX.md) + [Cron表达式.md](Cron表达式.md)
- **性能监控**: [链路追踪ITracer.md](链路追踪ITracer.md)
- **硬件信息**: [机器信息MachineInfo.md](机器信息MachineInfo.md)
- **依赖注入**: [对象容器ObjectContainer.md](对象容器ObjectContainer.md)
- **服务托管**: [轻量级应用主机Host.md](轻量级应用主机Host.md)
- **数据缓存**: [缓存系统ICache.md](缓存系统ICache.md)
- **配置管理**: [配置系统Config.md](配置系统Config.md)

### 外部资源

- **GitHub**: https://github.com/NewLifeX/X
- **Nuget**: https://www.nuget.org/packages/NewLife.Core
- **官方文档**: https://newlifex.com/core
- **QQ群**: 1600800

---

## 文档规范

所有文档遵循统一格式：

1. **概述** - 功能简要描述
2. **快速开始** - 最小示例代码
3. **核心API** - 接口/类/方法说明
4. **使用场景** - 实际应用示例
5. **最佳实践** - 推荐用法
6. **注意事项** - 潜在问题
7. **常见问题** - FAQ
8. **参考链接** - 相关文档
9. **更新日志** - 版本历史

---

## 贡献指南

欢迎贡献文档！

1. Fork 仓库
2. 编写新文档，遵循命名规范：`{中文标题}.md`
3. 提交 Pull Request

---

## 更新记录

- **2025-01-07**: 新增序列化、配置、缓存、对象池等文档
- **2025-01-06**: 创建文档框架，完成 15 篇核心文档
- 计划编写约 45 篇文档

---

**注意**: 标记为"_待撰写_"的文档正在编写中。
