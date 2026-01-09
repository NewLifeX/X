# Snowflake 雪花算法使用手册

本文档基于源码 `NewLife.Core/Data/Snowflake.cs` 与测试 `XUnitTest.Core/Data/SnowflakeTests.cs`，用于说明 `Snowflake`（雪花算法分布式 Id 生成器）的设计、用法与注意事项。

> 关键词：单例、WorkerId、时间戳、序列号、时间回拨、集群分配（Redis）。

---

## 1. 概述

`Snowflake` 用一个 64 bit 的 `Int64` 作为全局唯一 Id。

位分配：

- 1 bit：保留（符号位）
- 41 bit：时间戳（毫秒）
- 10 bit：工作节点（`WorkerId`，0~1023）
- 12 bit：序列号（0~4095）

生成的 Id 具备以下特点：

- 大体趋势自增（按时间推进）
- 同一毫秒内可生成最多 4096 个 Id
- **在同一个 `Snowflake` 实例内**可保证不重复

重要约束：

- **业务内必须确保单例**。若并发场景存在多个 `Snowflake` 实例，并且 `WorkerId` 相同，则可能产生重复 Id。

---

## 2. 核心概念

### 2.1 `StartTimestamp`（起始时间戳）

- 属性：`public DateTime StartTimestamp { get; set; }`
- 默认值：`UTC 1970-01-01` 转为本地时间（`DateTimeKind.Local`）

语义要点：

- `Snowflake` 会把参与计算的时间转换到 `StartTimestamp` 所属时区，然后做差得到毫秒数。
- 默认使用本地时间，是为了方便解析雪花 Id 时直接得到本地时间，并最大兼容已有业务（尤其是按本地日期分表/分区的场景）。

使用建议：

- `StartTimestamp` **必须在首次调用 `NewId()` 前设置**，首次生成后再修改不会影响已初始化实例（因为初始化过程只做一次）。

### 2.2 `WorkerId`（工作节点 Id）

- 属性：`public Int32 WorkerId { get; set; }`
- 范围：0~1023（10 位）

说明：

- 在分布式系统内，`WorkerId` 的全局唯一性决定了跨节点是否会产生重复 Id。
- 仅依靠默认算法（IP/进程/线程）**无法绝对保证唯一**，高要求场景建议外部显式分配。

### 2.3 `Sequence`（序列号）

- 属性：`public Int32 Sequence => _sequence;`
- 范围：0~4095（12 位）

说明：

- 同一毫秒内通过递增序列号保证唯一。
- 序列溢出（超过 4095）时，算法会逻辑上推进到下一毫秒继续生成。

---

## 3. WorkerId 初始化优先级

`Snowflake` 在首次生成 Id 时会自动执行一次初始化（`Initialize()`），按优先级决定 `WorkerId`：

1. 如果实例 `WorkerId > 0`：使用实例值
2. 否则如果 `Snowflake.GlobalWorkerId > 0`：使用 `GlobalWorkerId & 1023`
3. 否则如果 `Snowflake.Cluster != null`：调用 `JoinCluster(Cluster)` 从集群分配
4. 否则：使用默认算法生成（基于 IP 派生的实例号 + 进程/线程）

注意：

- 代码中使用的是判断 `WorkerId <= 0`，因此 **`WorkerId=0` 会被视为“未设置”并继续走后续策略**。若你希望固定为 0，需要自行确保不要触发初始化覆盖（一般不建议）。

---

## 4. API 速查

### 4.1 `Int64 NewId()`

基于当前时间生成下一个 Id。

行为要点：

- 使用 `DateTime.Now`（本地时间），并通过 `ConvertKind` 转换到 `StartTimestamp` 的时区。
- 处理时间回拨：
  - 若检测到时间回拨且回拨幅度大于 `MaxClockBack`（约 1 小时 + 10 秒），抛出 `InvalidOperationException`
  - 否则使用上一次时间戳继续生成（保持单实例唯一性）

适用场景：

- 常规业务主键生成（最常用）。

### 4.2 `Int64 NewId(DateTime time)`

基于指定时间生成 Id（携带当前实例的 `WorkerId` 与序列号）。

注意：

- 若你为同一个“指定毫秒时间”生成超过 4096 个 Id，则可能重复（因为序列号只有 12 位，会取模）。

适用场景：

- 需要用业务时间构造插入 Id（例如按采集时间落库）。

### 4.3 `Int64 NewId(DateTime time, Int32 uid)`

基于指定时间生成 Id，使用 `uid` 的低 10 位作为 `WorkerId`（1024 分组），仍保留 12 位序列号。

适用场景：

- 物联网数据采集：每 1024 个传感器为一组，每组每毫秒可生成最多 4096 个 Id。

注意：

- 若同一分组同一毫秒生成超过 4096 个 Id，则可能重复。

### 4.4 `Int64 NewId22(DateTime time, Int32 uid)`

基于指定时间生成 Id，使用 22 位业务 Id（`uid & ((1<<22)-1)`），不再保留序列号。

适用场景：

- 物联网数据采集：每 4,194,304 个传感器一组，每组每毫秒最多 1 个 Id。
- 常用于配合 upsert：同一毫秒同一传感器写多行数据时只保留一行。

注意：

- 同一业务 id 在同一毫秒内生成多个 Id 会重复（因为没有序列号）。

### 4.5 `Int64 GetId(DateTime time)`

把时间转换为“仅包含时间部分”的 Id（不带 `WorkerId` 与序列号）。

适用场景：

- 构造时间片段查询：将时间区间转换为雪花 Id 区间进行范围查询。

### 4.6 `Boolean TryParse(Int64 id, out DateTime time, out Int32 workerId, out Int32 sequence)`

解析雪花 Id，得到时间、`WorkerId`、序列号。

说明：

- 解析得到的 `time` 是 `StartTimestamp` 所属时区的时间。

### 4.7 `DateTime ConvertKind(DateTime time)`

把输入时间转换为与 `StartTimestamp` 相同的时区，便于相减。

规则：

- `time.Kind == DateTimeKind.Unspecified`：直接返回（不做转换）
- `StartTimestamp.Kind == Utc`：返回 `time.ToUniversalTime()`
- `StartTimestamp.Kind == Local`：返回 `time.ToLocalTime()`

---

## 5. 集群模式：确保 WorkerId 绝对唯一

### 5.1 `static ICache? Cluster`

`Snowflake.Cluster` 用于配置一个缓存实例作为 WorkerId 分配器，建议使用 Redis。

- 当 `Cluster != null` 且实例 `WorkerId` 未显式设置时，初始化阶段会调用 `JoinCluster(Cluster)`。

### 5.2 `void JoinCluster(ICache cache, String key = "SnowflakeWorkerId")`

通过自增键从集群获取 WorkerId：

- `workerId = (Int32)cache.Increment(key, 1)`
- `WorkerId = workerId & 1023`

使用建议：

- 在多进程/多节点场景，无脑优先采用集群分配 WorkerId。
- 需要区分环境（dev/test/prod）时，建议为不同环境使用不同的 `key`，避免 WorkerId 分配交叉。

---

## 6. 正确使用姿势

### 6.1 在应用内保持单例

关键点：

- 每个应用/服务内，尽量只创建一个全局 `Snowflake` 实例。
- 若使用 ORM/中间件（例如 XCode），需确保同一张表（或同一业务域）不要各自 new 一个 `Snowflake`。

示例：

```csharp
using NewLife.Data;

public static class IdGenerator
{
    public static readonly Snowflake Instance = new()
    {
        // 如有需要，在首次调用 NewId 前设置
        // StartTimestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local),
        // WorkerId = 1,
    };
}

var id = IdGenerator.Instance.NewId();
```

### 6.2 显式指定 WorkerId（推荐）

```csharp
var snow = new Snowflake { WorkerId = 1 };
var id = snow.NewId();
```

### 6.3 使用集群分配 WorkerId（推荐，分布式场景）

```csharp
using NewLife.Caching;
using NewLife.Data;

Snowflake.Cluster = /* RedisCache 实例 */;

var snow = new Snowflake();
var id = snow.NewId();
```

---

## 7. 常见问题与坑

### 7.1 为什么强调“单例”？

`Snowflake` 只保证“本实例”生成的 Id 唯一。

- 只要出现多个实例，并且 `WorkerId` 一样，并发下就可能产生重复。

### 7.2 默认 WorkerId 是否可靠？

默认策略使用 IP 派生实例 Id + 进程/线程信息，目的是降低同机冲突概率，但无法在所有环境下绝对唯一。

- 容器环境、NAT、同一局域网 IP 变化、进程重启等都可能引入冲突。
- 高要求场景强烈建议：显式 WorkerId 或使用 Redis 自增分配。

### 7.3 时间回拨会发生什么？

- 小范围回拨：会沿用上次时间戳继续生成，保持单实例不重复。
- 回拨过大：抛出异常拒绝生成（防止大概率碰撞）。

### 7.4 使用 `NewId(DateTime time)` 是否一定唯一？

不一定。

- 在同一毫秒内，序列号只有 12 位（4096），超出会取模导致重复风险。
- 这类 API 更适合“按时间落库/分区”的业务需求，而不是高并发写入同一个业务时间点。

---

## 8. 兼容性说明

- `Snowflake` 属于基础库，面向 `net45` ~ `net10` 多目标框架。
- 算法核心基于 `DateTime`、`Interlocked`、`Volatile`、`ICache` 等通用 API。

---

## 9. 相关链接

- 在线文档：`https://newlifex.com/core/snow_flake`
- 源码：`NewLife.Core/Data/Snowflake.cs`
- 单元测试：`XUnitTest.Core/Data/SnowflakeTests.cs`
