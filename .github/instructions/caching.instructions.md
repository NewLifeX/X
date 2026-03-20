---
applyTo: "**/Caching/**"
---

# 缓存模块开发指令

适用于 `NewLife.Caching` 命名空间下的缓存系统开发。

---

## 1. 架构分层

| 层级 | 接口/类 | 职责 |
|------|---------|------|
| 统一接口 | `ICache` | 全部缓存操作的标准契约 |
| 内存实现 | `MemoryCache` | 单机内存缓存，LRU 淘汰 |
| 分布式实现 | `Redis`（独立包 NewLife.Redis） | 跨进程/跨机器缓存 |
| 提供者 | `ICacheProvider` / `CacheProvider` | 管理全局缓存 + 本地缓存 + 队列 + 分布式锁 |

**重要原则**：所有缓存操作面向 `ICache` 接口编程，禁止直接依赖具体实现类。

---

## 2. ICache 接口规范

### 2.1 方法签名约定

- `expire` 参数统一使用 `Int32` 秒数，`-1` 表示使用缓存默认过期时间
- `Remove` 支持 `*` 通配符模糊匹配
- `Add` 已存在时不更新（返回 `false`），`Set` 总是覆盖
- `Replace` 原子替换并返回旧值
- `GetOrAdd` 缓存未命中时执行回调

### 2.2 高级操作

- `Increment`/`Decrement` 原子递增递减，返回新值
- `IncrementWithTtl`/`DecrementWithTtl` 返回 `(Value, Ttl)` 元组
- `AcquireLock` 返回 `IDisposable?`，配合 `using` 使用，获取失败返回 `null`
- 集合操作 `GetList`/`GetDictionary`/`GetQueue`/`GetStack`/`GetSet` 返回接口包装

---

## 3. MemoryCache 开发规范

- 构造无参，通过属性配置：`Capacity`（默认 10 万）、`Period`（清理周期秒数）
- 使用 `MemoryCache.Instance` 获取全局单例，勿手动 `new` 全局使用
- `KeyExpired` 事件可用于过期回调，勿在回调中执行重操作
- MemoryCache 的过期由后台定时器驱动，非精确过期

---

## 4. ICacheProvider 规范

- `Cache` 属性为跨进程缓存（默认 MemoryCache，生产环境替换为 Redis）
- `InnerCache` 属性为进程内本地缓存（始终是 MemoryCache）
- `GetQueue` 的 `group` 参数：`null` 返回简单队列，非空返回完整消费组队列
- `AcquireLock` 为分布式锁，超时时间 `msTimeout` 单位毫秒

---

## 5. 扩展实现规范

新增 ICache 实现时：

- **必须继承** `Cache` 基类（提供默认的批量操作实现）
- 批量操作（`GetAll`/`SetAll`）应重写为原生批量调用，而非循环单条
- `Init` 方法用于从连接字符串初始化，格式自定义但需文档说明
- `Dispose` 必须释放底层连接资源

---

## 6. 常见错误

- ❌ 缓存 key 使用用户输入未做校验（注入风险）
- ❌ `AcquireLock` 返回 `null` 时未处理（锁获取失败应有降级逻辑）
- ❌ 在循环内逐条 `Get`/`Set`（应使用 `GetAll`/`SetAll` 批量操作）
- ❌ 过期时间设为 0（0 表示立即过期，应使用 `-1` 表示默认）
