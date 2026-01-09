# CsvDb 使用手册

本文档基于源码 `NewLife.Core/IO/CsvDb.cs` 与其依赖 `NewLife.Core/IO/CsvFile.cs`，用于说明 `CsvDb<T>`（CSV 文件轻量级数据库）的设计目标、数据格式、事务模型与 CRUD 用法。

> 关键词：追加写、高性能顺序查询、跳过损坏行、表头映射、事务缓存、反射缓存、序列化属性名。

---

## 1. 概述

`CsvDb<T>` 是一个以 CSV 文件作为持久化存储的“轻量级数据库”，适合：

- 大量数据需要 **快速追加（Append）**；
- 需要 **顺序扫描式快速查询**（`Query`）；
- 很少修改/删除（修改/删除本质是“全量重写”）；
- 桌面端场景，SQLite 等关系库可能因非法关机导致损坏；`CsvDb<T>` 读取时可 **跳过损坏行**，提高可恢复性。

重要约束：

- **不支持线程安全**：类注释明确要求“务必确保单线程操作”。源码中部分方法使用 `lock (this)` 防并发，但并非完整并发设计。

---

## 2. 数据文件格式

### 2.1 文件头（Header）

首行是列名，来源于实体 `T` 的公共实例属性：

- 通过反射缓存 `_properties = typeof(T).GetProperties(...)` 获取属性；
- 列名使用 `SerialHelper.GetName(PropertyInfo)`（而不是属性名），以保持与序列化名/特性一致。

写文件时：

- 当文件为空（`FileStream.Position == 0`）时写入表头。

读取文件时：

- 首行作为 CSV 列名；
- 建立“文件列 -> 属性索引”的映射数组 `columnToProperty`，避免每行都查字典。

### 2.2 数据行（Data Rows）

每行对应一个 `T` 实例。

写入时：

- 若 `T` 实现 `IModel`：按属性名 `src[e.Name]` 读取值；
- 否则通过反射 `item.GetValue(e)` 读取属性值。

读取时：

- 创建 `new T()`；
- 对每列尝试按目标属性类型做基础校验（整数/浮点/日期等），校验失败则跳过该字段；
- 将字符串 `raw` 转换为目标类型：`raw.ChangeType(pi.PropertyType)`；
- 再通过 `IModel` 或 `model.SetValue(pi, value)` 赋值。

---

## 3. 核心属性

### 3.1 `FileName`

- 类型：`String?`
- 语义：CSV 数据文件路径

使用要求：

- 必须设置；未设置调用会抛出 `ArgumentNullException`（见 `GetFile()`）。

### 3.2 `Encoding`

- 类型：`Encoding`
- 默认：`Encoding.UTF8`

影响：

- 读取与写入 CSV 时传递给 `CsvFile.Encoding`。

### 3.3 `Comparer`

- 类型：`IEqualityComparer<T>`
- 默认：`EqualityComparer<T>.Default`

用途：

- `Remove(T)` / `Remove(IEnumerable<T>)` / `Find(T)` / `Set(T, ...)` 用它来判断“实体是否相同”。

可通过构造函数 `CsvDb(Func<T?, T?, Boolean> comparer)` 传入自定义比较逻辑。

---

## 4. 事务模型（缓存写）

### 4.1 `BeginTransaction()`

- 行为：把当前文件全部数据读入内存（`_cache = FindAll().ToList()`）。
- 之后的 `Add/Remove/Set/Clear/Find/Query` 将基于缓存操作（避免频繁 I/O）。

### 4.2 `Commit()`

- 行为：把 `_cache` 覆盖写回文件（`Write(_cache, false)`），然后清空缓存。

### 4.3 `Rollback()`

- 行为：仅清空缓存，不写回磁盘。

### 4.4 Dispose 自动提交

`CsvDb<T>` 继承 `DisposeBase`，其 `Dispose(Boolean)` 覆盖中会调用 `Commit()`：

- 若开启过事务且仍有缓存，释放对象时会自动提交（保持历史兼容行为）。

建议：

- 对“批处理”为主的场景，建议显示调用 `Commit()`，避免异常时误提交。

---

## 5. 写入/追加

### 5.1 `Write(IEnumerable<T> models, Boolean append)`

语义：批量写入。

关键点：

- 打开文件方式：`FileMode.OpenOrCreate` + `FileAccess.ReadWrite` + `FileShare.ReadWrite`；
- `append=true` 时移动到文件尾：`fs.Position = fs.Length`；
- 文件为空时写入表头；
- 写完后执行 `fs.SetLength(fs.Position)`：
  - 覆盖写（`append=false`）场景：截断原文件多余部分；
  - 追加写时也会把长度设置为当前位置（通常等价）。

### 5.2 `Add(T model)` / `Add(IEnumerable<T> models)`

- 若已 `BeginTransaction()`：仅追加到 `_cache`；
- 否则：直接 `Write(..., append:true)`，性能最好。

---

## 6. 查询

### 6.1 `IEnumerable<T> Query(Func<T, Boolean>? predicate, Int32 count = -1)`

语义：顺序扫描查询，按需返回。

行为要点：

- 开启事务（`_cache!=null`）时：从缓存枚举，命中则 `yield return`；
- 未开启事务时：
  - 使用 `CsvFile.ReadLine()` 逐记录读取；
  - 首条记录作为表头；
  - 后续每条记录根据映射填充到新对象。

损坏行处理：

- 类型转换过程中任何异常会被捕获并记录（`XTrace.WriteException(ex)`），该行跳过；
- 若一行中没有任何字段成功匹配（`success == 0`），视为损坏行并跳过。

`count`：

- 默认 `-1` 表示不限制；
- 每 `yield` 一次后 `--count`，到 0 结束。

### 6.2 `T? Find(Func<T, Boolean>? predicate)`

- 等价于 `Query(predicate, 1).FirstOrDefault()`。

### 6.3 `IList<T> FindAll()`

- 开启事务时返回缓存副本 `_cache.ToList()`；
- 否则读取全部。

### 6.4 `Int32 FindCount()`

- 非事务场景：使用 `StreamReader.ReadLine()` 逐行计数（跳过头部）。
- 注意：这里按物理行计数，不考虑 CSV 引号字段内换行的情况；在常规表格型 CSV 中通常可接受。

---

## 7. 更新与删除（全量重写，较慢）

### 7.1 `Remove(Func<T, Boolean> predicate)`

- 若开启事务：对 `_cache` 执行 `RemoveAll`；
- 否则：
  - `FindAll()` 读入全部；
  - 过滤掉命中项；
  - 再 `Write(list, false)` 覆盖写回。

### 7.2 `Update(T model)` / `Set(T model)`

- `Update`：只更新，不存在则返回 `false`；
- `Set`：存在则更新，不存在则追加一条。

未开启事务时：

- 读取全部到内存，修改后覆盖写回。

---

## 8. 异步查询（net5+ / netstandard2.1+）

在 `NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER` 下提供：

- `IAsyncEnumerable<T> QueryAsync(Func<T, Boolean>? predicate, Int32 count = -1)`
- `Task<IList<T>> FindAllAsync()`

实现要点：

- 内部使用 `CsvFile.ReadAllAsync()`；
- 头部映射逻辑与同步版一致；
- 发生异常时同样记录并跳过行。

---

## 9. 最小示例

### 9.1 定义实体

```csharp
public class User
{
    public Int32 Id { get; set; }
    public String? Name { get; set; }
    public DateTime CreateTime { get; set; }
}
```

### 9.2 追加写入

```csharp
using NewLife.IO;

var db = new CsvDb<User>
{
    FileName = "./user.csv",
    Encoding = Encoding.UTF8,
};

db.Add(new User { Id = 1, Name = "Stone", CreateTime = DateTime.Now });
db.Add(new User { Id = 2, Name = "NewLife", CreateTime = DateTime.Now });
```

### 9.3 查询

```csharp
foreach (var u in db.Query(e => e.Id > 0))
{
    Console.WriteLine($"{u.Id} {u.Name}");
}
```

### 9.4 批处理事务

```csharp
using var db = new CsvDb<User> { FileName = "./user.csv" };

db.BeginTransaction();

db.Add(new User { Id = 3, Name = "Tx" });
db.Remove(e => e.Id == 1);

db.Commit();
```

---

## 10. 注意事项与最佳实践

1. **高频写入优先用 `Add`（非事务）**：它走追加写路径，避免全量重写。
2. **修改/删除是一种“批处理操作”**：建议 `BeginTransaction()` 后集中处理，再 `Commit()`。
3. **单线程使用**：即使内部有 `lock (this)`，也不建议多线程并发操作同一个实例。
4. **表头列名与属性名**：写入使用 `SerialHelper.GetName`，读取是按列名映射到属性；若你自定义列名（序列化特性），要确保写入/读取一致。

---

## 11. 相关链接

- 在线文档：`https://newlifex.com/core/csv_db`
- 源码：`NewLife.Core/IO/CsvDb.cs`
- 依赖：`NewLife.Core/IO/CsvFile.cs`
