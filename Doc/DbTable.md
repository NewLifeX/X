# DbTable 使用手册

`NewLife.Data.DbTable` 是一个轻量级的内存数据表，用于承载“列（字段）+ 行（记录）”结构的数据。

适用场景：

- DAL 查询后把结果集缓存到内存，支持多次遍历、筛选、转换
- 在不依赖 `DataTable` 的情况下进行跨平台数据交换
- 将数据读写为二进制（高效/可压缩）、Json、Xml、Csv
- 在表数据与模型列表之间做映射

- 命名空间：`NewLife.Data`
- 相关类型：`DbTable`、`DbRow`

文档（站点）：https://newlifex.com/core/dbtable

---

## 1. 数据结构

`DbTable` 的核心由四部分组成：

- `Columns`：列名数组
- `Types`：列类型数组（与 `Columns` 一一对应）
- `Rows`：行集合（每行是 `Object?[]`，与 `Columns/Types` 对齐）
- `Total`：总行数（读取/写入二进制时会使用）

示例：

```csharp
var dt = new DbTable
{
    Columns = ["Id", "Name", "Enable"],
    Types = [typeof(Int32), typeof(String), typeof(Boolean)],
    Rows =
    [
        new Object?[] { 1, "Stone", true },
        new Object?[] { 2, "NewLife", false },
    ],
    Total = 2
};
```

注意：

- 通常 `Rows[i].Length == Columns.Length`
- 二进制读写依赖 `Types`，建议始终与 `Columns` 同步设置

---

## 2. 从数据库读取（`IDataReader` / `DbDataReader`）

### 2.1 同步读取

```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = "select Id, Name from User";

using var dr = cmd.ExecuteReader();

var table = new DbTable();
table.Read(dr);

Console.WriteLine(table); // DbTable[列数][行数]
```

`Read(dr)` 会自动调用：

- `ReadHeader(dr)`：读取列名与字段类型
- `ReadData(dr)`：逐行读取并填充 `Rows/Total`

### 2.2 异步读取

```csharp
await using var cmd = connection.CreateCommand();
cmd.CommandText = "select Id, Name from User";

await using var dr = await cmd.ExecuteReaderAsync();

var table = new DbTable();
await table.ReadAsync(dr);
```

库代码内部使用 `ConfigureAwait(false)`。

### 2.3 指定字段映射（`fields`）

`ReadData`/`ReadDataAsync` 支持传入 `fields`，用于将“目标列 i”映射到读取器的“源列 fields[i]”。

```csharp
// 目标列：Id, Name
table.Columns = ["Id", "Name"];
table.Types = [typeof(Int32), typeof(String)];

// 从读取器的第 2 列和第 0 列取值
table.ReadData(dr, fields: [2, 0]);
```

#### `DBNull` 默认值策略

读取到 `DBNull.Value` 时，`DbTable` 会按该列类型写入默认值（例如数值 `0`、`false`、`DateTime.MinValue`），而不是保留 `null`。

---

## 3. 与 `DataTable` 互转

### 3.1 从 `DataTable` 读取

```csharp
var table = new DbTable();
var count = table.Read(dataTable);
```

- 会从 `dataTable.Columns` 获取列名与类型
- 会把每行的 `ItemArray` 作为 `Object?[]` 加入 `Rows`

### 3.2 写入到 `DataTable`

```csharp
DataTable dataTable = table.ToDataTable();

// 或复用已有对象
var dt2 = table.Write(existing);
```

---

## 4. 二进制序列化（推荐）

`DbTable` 内置二进制读写，适合大数据量传输/落盘：

- 头部：幻数 + 版本 + 标记 + 列定义 + 行数
- 数据体：按列类型顺序逐值写入
- `*.gz` 文件可自动压缩/解压

### 4.1 写入/读取 `Stream`

```csharp
using var ms = new MemoryStream();
table.Write(ms);

ms.Position = 0;
var table2 = new DbTable();
table2.Read(ms);
```

### 4.2 转为数据包 `IPacket`

适合网络传输（头部预留 8 字节，方便上层协议追加包头）：

```csharp
IPacket pk = table.ToPacket();
```

### 4.3 保存/加载文件

```csharp
table.SaveFile("data.db");
table.SaveFile("data.db.gz", compressed: true);

var t2 = new DbTable();
t2.LoadFile("data.db");
```

### 4.4 迭代器：边读边处理（避免一次性加载全部 `Rows`）

```csharp
var table = new DbTable();
foreach (var row in table.LoadRows("data.db.gz"))
{
    // row 是 Object?[]
}
```

说明：

- `LoadRows` 会先读取头部，再根据 `Total` 决定读取行数
- 若 `Total == 0` 且文件非空，将以 `rows = -1` 一直读到流结束

### 4.5 迭代器：边处理边写入

```csharp
var table = new DbTable
{
    Columns = ["Id", "Name"],
    Types = [typeof(Int32), typeof(String)]
};

IEnumerable<Object?[]> rows = GetRows();
var count = table.SaveRows("data.db.gz", rows);
```

指定列映射顺序：

```csharp
var fields = new[] { 1, 0 }; // 目标列 i 对应源 row 的 fields[i]
var count = table.SaveRows("data.db", rows, fields);
```

- `fields[i] == -1` 表示写入空值（按目标列类型写入）

---

## 5. Json 序列化

`ToJson()` 会先转换为“字典数组”再序列化：

```csharp
String json = table.ToJson(indented: true);
```

字典数组形式：

```csharp
IList<IDictionary<String, Object?>> list = table.ToDictionary();
```

- 字典 key 为列名 `Columns[i]`
- value 为行值 `row[i]`

---

## 6. Xml 序列化

`GetXml()` 会生成 `DbTable` 根节点，内部每行是 `Table` 节点，每个列名作为子节点。

```csharp
String xml = table.GetXml();
```

写入到任意 `Stream`：

```csharp
await table.WriteXml(stream);
```

类型写入策略：

- `Boolean`：写入布尔值
- `DateTime`：写入 `DateTimeOffset`
- `DateTimeOffset`：直接写入
- `IFormattable`：按格式写入字符串
- 其他：`ToString()`

---

## 7. Csv 序列化

```csharp
table.SaveCsv("data.csv");

var t2 = new DbTable();
t2.LoadCsv("data.csv");
```

- `SaveCsv`：先写表头（列名行）再写入所有行
- `LoadCsv`：第一行作为 `Columns`，其余作为数据行

---

## 8. 模型互转

### 8.1 模型列表写入 `DbTable`

```csharp
var list = new[]
{
    new User { Id = 1, Name = "Stone" },
    new User { Id = 2, Name = "NewLife" },
};

var table = new DbTable();
table.WriteModels(list);
```

规则：

- 选择 `T` 的公共属性
- 仅保留“基础类型属性”（`IsBaseType()`）
- 若 `Columns` 为空则自动按属性生成 `Columns/Types`
- 行值通过反射读取；若模型实现 `IModel`，则优先用索引器 `model[name]`

### 8.2 `DbTable` 读取为模型列表

```csharp
IEnumerable<User> users = table.ReadModels<User>();

// 或指定 Type
IEnumerable<Object> objs = table.ReadModels(typeof(User));
```

映射规则：

- 使用 `SerialHelper.GetName(PropertyInfo)` 获取字段名（支持特性别名）
- 列名大小写不敏感匹配属性
- 若目标模型实现 `IModel`，则通过索引器赋值，否则通过反射 `SetValue`

---

## 9. 便捷读取（按行列获取值）

```csharp
var name = table.Get<String>(row: 0, name: "Name");

if (table.TryGet<Int32>(1, "Id", out var id))
{
    // ...
}
```

- `GetColumn(name)` 支持忽略大小写

---

## 10. 枚举与 `DbRow`

`DbTable` 实现 `IEnumerable<DbRow>`，可直接 `foreach`：

```csharp
foreach (var row in table)
{
    // row 是 DbRow
}
```

获取指定行：

```csharp
var row = table.GetRow(0);
```

---

## 11. 克隆

`Clone()` 为浅拷贝：

- `Columns/Types` 拷贝为新数组
- `Rows` 使用 `ToList()` 创建新列表，但行数组引用仍共享

```csharp
var copy = table.Clone();
```

---

## 12. 常见问题

### 12.1 为什么 `DBNull` 会变成默认值？

这是 `DbTable.ReadData(...)` 的既定策略：按列类型填充默认值，便于后续直接做数值/布尔/时间计算。

若业务需要保留 `null` 语义，请在上层自行处理。

### 12.2 二进制格式是否稳定？

二进制格式包含版本号（当前版本为 `3`）。读取时遇到更高版本会抛出 `InvalidDataException`。

### 12.3 大数据量建议怎么用？

- 读取：优先 `LoadRows`/`ReadRows` 迭代消费
- 写入：优先 `SaveRows`/`WriteRows` 迭代写入
- 网络传输：使用 `ToPacket()`
