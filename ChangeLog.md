# NewLife.Core 版本更新记录

## v11.12.2026.0301 (2026-03-01)

### 序列化增强
- **SpanSerializer**：新增高性能 Span 二进制序列化器，支持零分配读写，性能全面优于传统方式
- **SpanReader/SpanWriter**：新增对 `byte[]` 的直接构造支持
- **字符串读取优化**：优化字符串读取时的内存分配策略，减少堆分配

### IPacket 重构与性能优化
- **OwnerPacket**：重构为 `sealed` 类，优化内存管理与生命周期
- **切片性能**：优化 IPacket 切片逻辑，减少装箱，完善测试
- **PacketHelper.AsPacket**：新增快捷转换方法，简化 Packet 操作
- **释放机制**：完善 IPacket 与 IMessage 的释放文档与测试

### 网络层性能优化
- **SendMessageAsync**：重构为非异步路径，降低 Task 分配开销
- **PooledValueTaskSource**：增强池化 ValueTask 源，提升并发吞吐
- **编解码器池化**：对编解码器与事件参数进行池化，Echo 场景性能大幅提升
- **NetServer 实测**：压测跑出 23.4 Gbps 带宽、1.4 亿 pkt/s 吞吐

### 配置系统热加载重构
- **事件驱动**：FileConfigProvider 从低效轮询改为 FileSystemWatcher 事件驱动热更新
- **双重保险**：Watcher 可用时定时器周期延长为 60 秒作为兜底，保持 Period 属性向后兼容

### IO 工具增强
- **IOHelper**：新增异步精确读取（`ReadExactlyAsync`）与最少读取（`ReadAtLeastAsync`）方法及测试
- **MaxSafeArraySize**：新增安全最大数组尺寸常量

### 其他优化
- **DeferredQueue**：支持运行时动态修改处理周期
- **MemoryCache**：`Remove` 单键路径实现零分配优化
- **文件操作**：增强异常处理与哈希校验链路追踪

### 性能基准测试
- 新增 Benchmark 基准测试项目（net10.0），覆盖 IPacket、MemoryCache、TCP Echo、NetServer 等核心场景
- 发布 IPacket、NetServer、MemoryCache 等性能测试报告

### 文档与 Copilot 指令
- 新增 NewLife.Net 开发规范文档与 Copilot 协作指令
- 新增 AI 开发流程指令及自治批处理规范
- 优化 Copilot 指令文档结构与基准测试规范

---

## v11.11.2026.0201 (2026-02-01)

### 核心功能

#### ApiHttpClient 增强
- **多服务竞速与负载均衡**：支持多地址竞速下载，自动选择最快节点
- **哈希校验**：支持文件下载时的哈希校验功能
- **负载均衡器重构**：新增 `RaceLoadBalancer`、`PeerEndpointSelector` 等负载均衡组件
- **节点管理**：自动屏蔽失败节点，支持可配置的启动延迟步长
- **性能追踪**：为竞速调度和下载过程增加 Tracer 埋点

#### 依赖注入（DI）增强
- **ObjectContainer 优化**：增强文档与核心功能，支持延迟 DI 集成
- **Token 模型扩展**：引入 `IToken` 接口，提升令牌模型的扩展性
- **TokenModel 扩展**：新增 Scope 属性支持，改为 partial 类提高扩展容错

### 网络层优化
- **NetClient 重构**：完善文档与注释，补充单元测试
- **会话/服务器增强**：增强 Net 会话与服务器注释及单元测试
- **消息处理统一**：统一 `IMessage` 处理逻辑，简化消息提取代码，支持原始内容追踪
- **UdpServer 修复**：修正监听 0 端口后没有自动回填到 NetServer 的问题
- **并发性能优化**：优化消息队列并发性能，完善编码器注释

### 工具类新增与增强
- **UriInfo 增强**：新增 `ToUri` 方法，增强对 IPv6 及多格式 URL 的解析支持
- **Span 扩展**：新增 `Span<byte> Trim` 扩展方法及单元测试
- **配置支持**：`Config<T>` 支持泛型字典（修复 [#172](https://github.com/NewLifeX/X/issues/172)）
- **序列化优化**：如果属性定义了 `DataObjectFieldAttribute`，让它们排在前面

### 文档与协作
- **使用手册**：为主要功能模块编写使用手册（ApiHttpClient、网络库等）
- **Copilot 指令优化**：
  - 新增 Markdown 文档规范
  - 完善主动优化原则及细化规范
  - 新增防御性注释与日志规范
  - 新增集合表达式与 Null 条件运算符指引
  - 优化 XCode/Cube 指令读取与 XML 检测策略
  - 自动分发 Copilot 指令文件及 MSBuild 支持

### 测试与质量
- **单元测试大幅增强**：为网络库核心功能、UriInfo、PeerEndpointSelector、ApiHttpClient 等新增大量单元测试
- **测试健壮性提升**：优化测试用例健壮性与时区兼容性，提升 CI 兼容性

### 其他优化
- **日志优化**：简化线程池和长任务在日志里的名字，让应用启动时的日志更整齐
- **AddServer 改进**：添加服务地址时返回服务节点

---

## 历史版本

### v11.10.2026.0101 (2026-01-01)
- 重构 IEventBus，支持 EventHub
- ApiHttpClient 支持文件下载

---

**说明**：
- 正式版发布周期：每月月初
- 测试版发布周期：提交代码到 GitHub 时自动发布
- 版本号格式：正式版 `{主版本}.{子版本}.{年}.{月日}`，测试版 `{主版本}.{子版本}.{年}.{月日}-beta{时分}`
