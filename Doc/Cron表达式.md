# Cron表达式

## 概述

`NewLife.Threading.Cron` 是一个轻量级的 Cron 表达式解析和匹配器，用于判断某个时间点是否满足规则，并能计算下一次/上一次的执行时间。

与标准 Cron 相比，NewLife.Cron 更加轻量简洁，适合在定时任务、调度系统中使用。

**命名空间**: `NewLife.Threading`  
**源码**: [NewLife.Core/Threading/Cron.cs](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Threading/Cron.cs)

---

## 快速入门

### 创建和解析

```csharp
using NewLife.Threading;

// 方式1：构造时传入表达式
var cron = new Cron("0 0 2 * * 1-5");

// 方式2：先创建后解析
var cron2 = new Cron();
cron2.Parse("*/5 * * * * *");
```

### 判断时间是否匹配

```csharp
var cron = new Cron("0 0 2 * * 1-5");  // 每个工作日凌晨2点

// 判断当前时间是否符合表达式
if (cron.IsTime(DateTime.Now))
{
    Console.WriteLine("现在是执行时间");
}

// 判断特定时间
var time = new DateTime(2025, 1, 6, 2, 0, 0);  // 2025年1月6日(周一) 2点
if (cron.IsTime(time))
{
    Console.WriteLine("该时间点符合规则");
}
```

### 计算下一次执行时间

```csharp
var cron = new Cron("0 0 2 * * *");  // 每天凌晨2点
var next = cron.GetNext(DateTime.Now);
Console.WriteLine($"下一次执行时间：{next}");

// 计算上一次执行时间
var prev = cron.GetPrevious(DateTime.Now);
Console.WriteLine($"上一次执行时间：{prev}");
```

---

## 表达式语法

### 表达式结构

Cron 表达式由空格分隔的 **6 个字段**组成（第7个字段"年"暂不支持）：

```
秒 分 时 日 月 星期
```

| 字段 | 范围 | 说明 |
|-----|------|------|
| 秒 | 0-59 | 秒数 |
| 分 | 0-59 | 分钟数 |
| 时 | 0-23 | 小时数（24小时制） |
| 日 | 1-31 | 每月的第几天 |
| 月 | 1-12 | 月份 |
| 星期 | 0-6 | 星期几（0=周日，1=周一，...，6=周六） |

**示例**：
```
0 30 8 * * 1-5     // 每个工作日 8:30
0 0 */2 * * *      // 每2小时整点
0 0 0 1 * *        // 每月1号凌晨
*/10 * * * * *     // 每10秒
```

### 支持的语法

#### 1. 通配符 `*`

表示所有可能的值。

```
* * * * * *        // 每秒
0 * * * * *        // 每分钟的0秒
0 0 * * * *        // 每小时的0分0秒
```

#### 2. 占位符 `?`

不指定值，实际上等价于 `*`，主要用于兼容 Quartz 等其他 Cron 实现。

```
0 0 0 ? * 1        // 每周一凌晨（日期不指定）
```

#### 3. 枚举 `a,b,c`

列出多个指定值。

```
0 0 0 1,15 * *     // 每月1号和15号凌晨
0 0 8,12,18 * * *  // 每天8点、12点、18点
```

#### 4. 范围 `a-b`

表示一个连续范围（闭区间）。

```
0 0 2 * * 1-5      // 周一到周五凌晨2点
0 0 9-17 * * *     // 每天9点到17点（每小时）
```

#### 5. 步进 `*/n`、`a/n`、`a-b/n`

表示按照一定的增量选择值。

- `*/n`：从0开始，每隔n选一个
- `a/n`：从a开始，每隔n选一个
- `a-b/n`：在a到b范围内，每隔n选一个

```
*/2 * * * * *      // 每2秒（0,2,4,6...秒）
5/20 * * * * *     // 每分钟的5秒、25秒、45秒
0 */30 * * * *     // 每30分钟
0 0 0 */5 * *      // 每5天
```

#### 6. 第几个星期几 `d#k` 和 `d#Lk`

仅星期字段支持，用于表达"每月第几个星期几"。

- `d#k`：每月第k个星期d
- `d#Lk`：每月倒数第k个星期d

```
0 0 0 ? ? 1#1      // 每月第1个周一凌晨
0 0 0 ? ? 5#2      // 每月第2个周五凌晨
0 0 0 ? ? 1#L1     // 每月最后1个周一凌晨
0 0 0 ? ? 3-5#L2   // 每月倒数第2个周三到周五凌晨
```

**注意**：`#` 语法会将星期数模 7，因此可以用 7 表示周日。

---

## 星期偏移

### 默认行为

Cron 默认采用 Linux/.NET 风格：
- `0` 表示周日 (Sunday)
- `1` 表示周一 (Monday)
- `2` 表示周二 (Tuesday)
- ...
- `6` 表示周六 (Saturday)

### Sunday 属性

`Sunday` 属性用于调整"周日"对应的数字偏移：

```csharp
var cron = new Cron();
cron.Sunday = 0;  // 默认值，0表示周日
// 0=周日，1=周一，2=周二...

cron.Sunday = 1;  // 修改为1表示周日
// 1=周日，2=周一，3=周二...
```

**使用建议**：
- 一般情况下保持默认 `Sunday = 0` 即可
- 如果需要兼容其他系统（如某些数据库），可调整为 `Sunday = 1`
- `Parse` 方法不会自动推断 `Sunday`，需要手动设置

---

## 核心 API

### IsTime - 判断时间是否匹配

```csharp
/// <summary>指定时间是否位于表达式之内</summary>
/// <param name="time">要判断的时间</param>
/// <returns>是否匹配</returns>
public Boolean IsTime(DateTime time)
```

**示例**：
```csharp
var cron = new Cron("0 0 2 * * 1-5");
var time = new DateTime(2025, 1, 6, 2, 0, 0);
if (cron.IsTime(time))
{
    Console.WriteLine("匹配");
}
```

**注意事项**：
- 判断时会考虑秒、分、时、日、月、星期所有维度
- 星期字段支持"第几个星期几"的复杂判断
- 时间会按照 `Sunday` 属性进行星期计算

### GetNext - 获取下一次执行时间

```csharp
/// <summary>获得指定时间之后的下一次执行时间，不含指定时间</summary>
/// <param name="time">从该时间秒的下一秒算起的下一个执行时间</param>
/// <returns>下一次执行时间（秒级），如果没有匹配则返回最小时间</returns>
public DateTime GetNext(DateTime time)
```

**示例**：
```csharp
var cron = new Cron("0 0 2 * * *");  // 每天凌晨2点
var now = DateTime.Now;
var next = cron.GetNext(now);
Console.WriteLine($"下一次执行：{next:yyyy-MM-dd HH:mm:ss}");
```

**注意事项**：
- 如果传入时间带有毫秒（如 09:14:23.456），会向前对齐到下一秒（09:14:24）后再计算
- 返回的时间不包含传入的时间本身
- 如果1年内找不到匹配时间，返回 `DateTime.MinValue`
- **性能警告**：该方法通过逐秒遍历查找，最多遍历1年，不适合频繁调用

### GetPrevious - 获取上一次执行时间

```csharp
/// <summary>获得与指定时间符合表达式的最近过去时间（秒级）</summary>
/// <param name="time">基准时间</param>
/// <returns>上一次执行时间，如果没有匹配则返回最小时间</returns>
public DateTime GetPrevious(DateTime time)
```

**示例**：
```csharp
var cron = new Cron("0 0 2 * * *");
var prev = cron.GetPrevious(DateTime.Now);
Console.WriteLine($"上一次执行：{prev:yyyy-MM-dd HH:mm:ss}");
```

### 批量 Cron 计算

```csharp
/// <summary>对一批Cron表达式，获取下一次执行时间</summary>
public static DateTime GetNext(String[] crons, DateTime time)

/// <summary>对一批Cron表达式，获取前一次执行时间</summary>
public static DateTime GetPrevious(String[] crons, DateTime time)
```

**示例**：
```csharp
var crons = new[] { "0 0 2 * * *", "0 0 14 * * *" };  // 每天2点和14点
var next = Cron.GetNext(crons, DateTime.Now);
Console.WriteLine($"下一次执行：{next}");
```

---

## 配合 TimerX 使用

Cron 最常见的使用场景是配合 `TimerX` 实现定时任务：

```csharp
using NewLife.Threading;

// 创建 Cron 定时器：每个工作日早上8点执行
var timer = new TimerX(state =>
{
    Console.WriteLine($"执行任务：{DateTime.Now}");
}, null, "0 0 8 * * 1-5");

// 支持多个 Cron 表达式，分号分隔
var timer2 = new TimerX(state =>
{
    Console.WriteLine("执行任务");
}, null, "0 0 2 * * 1-5;0 0 3 * * 6");  // 工作日2点，周六3点

// 使用完毕记得释放
timer.Dispose();
timer2.Dispose();
```

详见：[TimerX 使用手册](timerx-高级定时器TimerX.md)

---

## 常用表达式示例

### 每秒/每分/每时

```
* * * * * *        // 每秒
*/2 * * * * *      // 每2秒
*/5 * * * * *      // 每5秒
0 * * * * *        // 每分钟
0 */5 * * * *      // 每5分钟
0 */15 * * * *     // 每15分钟
0 */30 * * * *     // 每30分钟
0 0 * * * *        // 每小时
0 0 */2 * * *      // 每2小时
```

### 每天固定时间

```
0 0 0 * * *        // 每天凌晨0点
0 0 1 * * *        // 每天凌晨1点
0 30 8 * * *       // 每天8点30分
0 0 12 * * *       // 每天中午12点
0 0 23 * * *       // 每天晚上23点
0 0 0,12 * * *     // 每天0点和12点
0 0 8,12,18 * * *  // 每天8点、12点、18点
```

### 工作日/周末

```
0 0 9 * * 1-5      // 每个工作日早上9点
0 0 2 * * 1-5      // 每个工作日凌晨2点
0 0 10 * * 6,0     // 每个周末（周六、周日）10点
0 0 0 * * 1        // 每周一凌晨
0 0 0 * * 5        // 每周五凌晨
```

### 每月固定日期

```
0 0 0 1 * *        // 每月1号凌晨
0 0 0 15 * *       // 每月15号凌晨
0 0 0 1,15 * *     // 每月1号和15号凌晨
0 0 0 L * *        // 每月最后一天凌晨（需配合特殊处理）
```

### 复杂场景

```
0 0 2 * * 1-5      // 每个工作日凌晨2点
5/20 * * * * *     // 每分钟的5秒、25秒、45秒
0 0 0 ? ? 1#1      // 每月第1个周一凌晨
0 0 0 ? ? 5#L1     // 每月最后1个周五凌晨
0 0 0 1-7 * 1      // 每月第一个周一凌晨（另一种写法）
```

---

## 注意事项与限制

### 性能考量

`GetNext` 和 `GetPrevious` 方法通过**逐秒遍历**实现，性能特点：
- **适合场景**：低频率调用，如定时任务初始化时计算下一次执行时间
- **不适合场景**：高频调用、实时路径、热点代码
- **性能上限**：最多遍历1年（约3千万次循环）

**建议**：
- 在 `TimerX` 初始化时计算一次下一次执行时间即可
- 避免在循环中频繁调用
- 如需高性能场景，考虑自行实现算法

### 不支持年份字段

当前实现仅支持6个字段（秒、分、时、日、月、星期），**不支持第7个字段"年"**。

### 字段缺省规则

- 如果表达式少于6个字段，缺省字段默认为 `*`
- 例如：`0 0 2 * *` 会被解析为 `0 0 2 * * *`

### 星期字段特殊性

- 星期计算受 `Sunday` 属性影响
- `#` 语法会将星期数模7，因此 7 也可以表示周日
- 星期范围是 0-6（或根据 `Sunday` 调整为 1-7）

---

## 源码解析

### 解析流程

```csharp
public Boolean Parse(String expression)
{
    var ss = expression.Split([' '], StringSplitOptions.RemoveEmptyEntries);
    
    // 解析秒
    if (!TryParse(ss[0], 0, 60, out var vs)) return false;
    Seconds = vs;
    
    // 解析分
    if (!TryParse(ss.Length > 1 ? ss[1] : "*", 0, 60, out vs)) return false;
    Minutes = vs;
    
    // ... 依次解析时、日、月
    
    // 解析星期（特殊处理）
    var weeks = new Dictionary<Int32, Int32>();
    if (!TryParseWeek(ss.Length > 5 ? ss[5] : "*", 0, 7, weeks)) return false;
    DaysOfWeek = weeks;
    
    return true;
}
```

### 匹配判断

```csharp
public Boolean IsTime(DateTime time)
{
    // 基础时间判断
    if (!Seconds.Contains(time.Second) ||
        !Minutes.Contains(time.Minute) ||
        !Hours.Contains(time.Hour) ||
        !DaysOfMonth.Contains(time.Day) ||
        !Months.Contains(time.Month))
        return false;
    
    // 星期判断（考虑 Sunday 偏移）
    var w = (Int32)time.DayOfWeek + Sunday;
    if (!DaysOfWeek.TryGetValue(w, out var index)) return false;
    
    // 第几个星期几判断（index > 0 表示正数第几个，< 0 表示倒数）
    // ... 复杂逻辑
    
    return true;
}
```

---

## 常见问题

### 1. 表达式解析失败？

检查语法是否正确：
- 字段数量是否为6个（或更少，缺省默认`*`）
- 范围值是否在有效范围内
- 步进值语法是否正确

```csharp
var cron = new Cron();
if (!cron.Parse("0 0 2 * * 1-5"))
{
    Console.WriteLine("解析失败");
}
```

### 2. GetNext 返回 MinValue？

说明1年内找不到匹配的时间，可能原因：
- 表达式本身矛盾（如 `0 0 0 31 2 *` 2月没有31号）
- 日期和星期冲突

### 3. 星期计算不对？

检查 `Sunday` 属性设置：
```csharp
var cron = new Cron("0 0 0 * * 1");
cron.Sunday = 0;  // 确认偏移量
```

### 4. 毫秒影响计算？

`GetNext` 会自动处理毫秒，向上对齐到下一秒：
```csharp
var time = new DateTime(2025, 1, 1, 9, 14, 23, 456);  // 带毫秒
var next = cron.GetNext(time);  // 从 09:14:24 开始计算
```

---

## 参考资料

- **TimerX 文档**: [timerx-高级定时器TimerX.md](timerx-高级定时器TimerX.md)
- **阿里云Cron参考**: https://help.aliyun.com/document_detail/64769.html
- **在线文档**: https://newlifex.com/core/cron
- **源码**: https://github.com/NewLifeX/X/blob/master/NewLife.Core/Threading/Cron.cs

---

## 更新日志

- **2025-01**: 完善文档，补充详细示例和源码解析
- **2024**: 支持 .NET 9.0
- **2023**: 优化解析性能
- **2022**: 增加批量Cron计算方法
- **2020**: 初始版本，支持基本Cron语法
