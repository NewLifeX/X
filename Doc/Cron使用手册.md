# Cron 使用手册

`NewLife.Threading.Cron` 是一个轻量级 Cron 表达式解析与匹配器，用于在“秒级”判断某个时间是否命中规则，并计算下一次/上一次命中时间。

- 源码：`NewLife.Core\Threading\Cron.cs`
- 命名空间：`NewLife.Threading`

---

## 1. 适用场景

- 需要用字符串描述“固定时间点/周期性时间点”触发规则
- 需要根据当前时间快速判断是否命中
- 需要计算下一次触发点用于调度（典型搭配 `TimerX`）

> 设计取舍：实现简单、无外部依赖；`GetNext/GetPrevious` 使用逐秒遍历（最多 1 年），适合调度层使用，不建议在热点路径高频调用。

---

## 2. 表达式格式

### 2.1 字段

表达式由空格分隔，最多支持 6 段（不足的段默认 `*`）：

```
秒 分 时 日 月 周
```

字段范围（由 `Parse` 内部参数决定）：

| 字段 | 范围 | 说明 |
|---|---:|---|
| 秒 | 0-59 | `TryParse(..., 0, 60)` |
| 分 | 0-59 | `TryParse(..., 0, 60)` |
| 时 | 0-23 | `TryParse(..., 0, 24)` |
| 日(每月) | 1-31 | `TryParse(..., 1, 32)` |
| 月 | 1-12 | `TryParse(..., 1, 13)` |
| 周 | 0-6（并允许 7 的写法） | `TryParseWeek(..., 0, 7)` |

> 注：类注释提到“年”字段，但当前实现未解析第 7 段。

### 2.2 支持的语法

每段字段支持：

- `*`：任意值
- `?`：不指定值（实现上等价于 `*` 处理，并不会像 Quartz 那样“忽略该字段”）
- `a,b,c`：枚举
- `a-b`：范围（包含端点）
- `*/n`、`a/n`、`a-b/n`：步进

周字段额外支持：

- `d#k`：每月第 `k` 个星期 `d`
- `d#Lk`：每月倒数第 `k` 个星期 `d`

> `#` 语法在实现中会将步进设为 `7`，用于匹配“同一周几”的序号计算。

---

## 3. 星期（周）规则与 `Sunday` 偏移

### 3.1 默认规则

`Cron` 默认采用 Linux/.NET 常见规则：

- `0` 表示周日
- `1` 表示周一
- ...
- `6` 表示周六

在 `IsTime(DateTime time)` 中：

```csharp
var w = (Int32)time.DayOfWeek + Sunday;
```

因此，实际匹配键会受 `Sunday` 影响。

### 3.2 `Sunday` 偏移

`Sunday` 用于“周日对应的数字”偏移：

- `Sunday = 0`（默认）：`DayOfWeek.Sunday(0)` → 0
- `Sunday = 1`：`DayOfWeek.Sunday(0)` → 1，`Monday(1)` → 2 ...

使用建议：

- 如果你希望表达式里用 `1` 表示周日，则设置 `cron.Sunday = 1`
- `Parse` 不会自动推断 `Sunday` 风格，需要由调用方统一约定

---

## 4. 核心 API

### 4.1 构造与解析

```csharp
var cron = new Cron();
var ok = cron.Parse("0 */5 * * * *");

var cron2 = new Cron("0 */5 * * * *");
```

- `Parse` 返回 `false` 表示表达式非法
- 构造函数 `Cron(String expression)` 内部直接调用 `Parse(expression)`

### 4.2 判断时间是否命中：`IsTime`

`IsTime` 会同时校验：秒、分、时、日、月、周。

```csharp
var cron = new Cron("0 0 2 * * 1-5");
var hit = cron.IsTime(DateTime.Parse("2026-01-09 02:00:00"));
```

周字段包含 `#` 时，还会进一步判断“第几个/倒数第几个周几”。

### 4.3 下一次命中时间：`GetNext`

- 从 `time` 的下一秒开始查找（不含 `time`）
- 若 `time` 带毫秒，会先 `Trim()` 对齐到秒，再做偏移（避免“刚好对齐”造成重复命中）
- 最多查找 1 年，失败返回 `DateTime.MinValue`

```csharp
var cron = new Cron("5/20 * * * * *");
var next = cron.GetNext(DateTime.Today);
```

### 4.4 上一次命中时间：`GetPrevious`

- 从 `time` 前一秒开始反向查找（不含 `time`）
- 最多回溯 1 年，失败返回 `DateTime.MinValue`

```csharp
var cron = new Cron("0 */10 * * * *");
var prev = cron.GetPrevious(DateTime.Now);
```

### 4.5 多表达式：取最近/最近一次

```csharp
var arr = new[] { "0 */2 * * * *", "30 */5 * * * *" };

var next = Cron.GetNext(arr, DateTime.Now);
var prev = Cron.GetPrevious(arr, DateTime.Now);
```

---

## 5. 示例

### 5.1 秒级步进

- `*/2`：每 2 秒一次
- `5/20`：每分钟的 5/25/45 秒

### 5.2 工作日凌晨 2 点

```
0 0 2 * * 1-5
```

### 5.3 每月第 2 个星期三

```
0 0 0 ? ? 3#2
```

> 可参考测试：`XUnitTest.Core\Threading\CronTests.cs` 中的 `dayweek_test`。

---

## 6. 常见问题

### 6.1 `?` 的语义

本实现中 `?` 的处理更接近 `*`，并不会像 Quartz Cron 那样“忽略该字段”。

因此，如果你从 Quartz 表达式迁移，请优先使用 `*`、范围、步进、枚举、`#` 等明确实现的语法。

### 6.2 `GetNext/GetPrevious` 性能

`GetNext/GetPrevious` 通过逐秒遍历查找命中点，表达式越稀疏、距离越远，遍历越久。

建议：

- 不要在高频业务逻辑中反复调用
- 将计算放到调度层（例如定时器初始化、任务执行完后计算下一次）

---

## 7. 相关链接

- 文档：<https://newlifex.com/core/cron>
