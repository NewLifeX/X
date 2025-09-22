# DbTable 使用说明

`NewLife.Data.DbTable` 是一个轻量级的表格数据容器，支持从数据库读取、二进制/Xml/Csv 序列化、与 `DataTable` 互转，以及模型对象与行之间的映射。

- 命名空间：`NewLife.Data`
- 主要类型：`DbTable`、`DbRow`
- 典型场景：
  - 从 `IDataReader`/`DbDataReader` 读取查询结果
  - 与 `DataTable` 互转，提高与 ADO.NET 的互操作
  - 将表数据序列化为二进制、XML、CSV 或数据包
  - 将模型列表写入为表，或将表读取为模型列表

## 核心成员

- 列定义
  - `string[] Columns` 列名集合
  - `Type[] Types` 列类型集合
- 数据
  - `IList<object?[]> Rows` 行集合
  - `int Total` 总行数
- 读取
  - `Read(IDataReader dr)` / `ReadAsync(DbDataReader dr)`
  - `ReadData(IDataReader dr, int[]? fields = null)`：仅读取指定列，`fields[i]` 表示源数据列索引映射到目标列 `i`
  - `Read(DataTable table)` 从 `DataTable` 读取
  - 二进制读取：`Read(Stream)`、`ReadHeader(Binary)`、`ReadData(Binary, rows)`、`ReadRows(Binary, rows)`
  - 文件/数据包：`LoadFile(path)`、`LoadRows(path)`、`Read(IPacket)`、`Read(byte[])`
- 写入
  - `Write(Stream)`/`WriteHeader(Binary)`/`WriteData(Binary)`/`WriteData(Binary, int[] fields)`
  - `WriteRows(Binary, IEnumerable<object?[]>, int[]? fields = null)`/`WriteRow(...)`
  - `SaveFile(path)`/`SaveRows(path, rows, fields)`
  - `ToPacket()` 转数据包
- 转换
  - `ToDataTable()`/`Write(DataTable)`
  - `ToJson(...)`/`WriteXml(Stream)`/`GetXml()`/`SaveCsv(path)`/`LoadCsv(path)`
- 模型映射
  - `WriteModels<T>(IEnumerable<T>)` 将模型写入为表（仅基础类型属性）
  - `Cast<T>(IEnumerable<T>)` 将模型按列顺序转为行
  - `ReadModels<T>()`/`ReadModels(Type)` 将表转为模型列表
- 访问
  - `Get<T>(rowIndex, name)`/`TryGet<T>(rowIndex, name, out value)`
  - `GetColumn(name)` 根据列名找索引
  - 枚举：`foreach (var row in table)`，每个 `row` 是 `DbRow`

## 快速上手

### 1) 从数据库读取

```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = "select Id, Name, CreateTime from User";
using var reader = cmd.ExecuteReader();

var table = new DbTable();
table.Read(reader);

Console.WriteLine(table.Total);       // 行数
Console.WriteLine(table.Columns[0]);  // 列名
```

仅选择部分列：

```csharp
// fields: 将目标列 i 映射到源 reader 的列索引 fields[i]
// 例如只读第 0 和第 2 列
int[] fields = [0, 2];
var table = new DbTable();
table.ReadHeader(reader);      // 先读取列定义
table.ReadData(reader, fields);
```

### 2) 与 DataTable 互转

```csharp
var dataTable = table.ToDataTable();
var table2 = new DbTable();
table2.Read(dataTable);
```

### 3) 序列化

- 二进制：

```csharp
using var fs = File.Create("users.db");
table.SaveFile("users.db"); // 或 table.Write(fs)

var t2 = new DbTable();
t2.LoadFile("users.db");
```

- XML/Csv：

```csharp
var xml = table.GetXml();
table.SaveCsv("users.csv");
```

- 数据包：

```csharp
var pk = table.ToPacket();
```

### 4) 模型映射

```csharp
public sealed class User
{
    public Int32 Id { get; set; }
    public String Name { get; set; } = "";
    public DateTime CreateTime { get; set; }
}

// 模型 -> 表
var users = new List<User> { new() { Id = 1, Name = "Stone", CreateTime = DateTime.UtcNow } };
var table = new DbTable();
table.WriteModels(users);

// 表 -> 模型
var list = table.ReadModels<User>().ToList();
```

## 进阶说明

- `fields` 映射规则
  - 读取：`ReadData(dr, fields)` 将目标列 `i` 映射到源列索引 `fields[i]`，空值会按源列类型填充默认值（数值 0、`false`、`DateTime.MinValue` 等）。
  - 写入：`WriteData(bn, fields)`/`WriteRow(bn, row, fields)` 将目标列 `i` 写入源行的 `row[fields[i]]`；若 `fields[i] == -1` 则按目标列类型写入空值。
- 如果以迭代器方式消费：`ReadRows(bn, -1)` 可持续读取至流末尾。
- `DbRow` 提供快捷访问：`row.Get<T>("Name")`。

## 兼容与注意

- 二进制格式头含幻数与版本，当前版本 `3`，向前兼容旧版本时间写入格式。
- MySQL 特殊日期（如 `0000-00-00`）可能在读取时异常，内部已用 `try/catch` 忽略并填充默认值。
- 高性能路径避免大量 LINQ/反射；类型、列名等已缓存于 `DbTable` 的 `Columns`/`Types`。

## 变更摘要（本次重构）

- 修复 `ReadData/ReadDataAsync` 在 `fields` 映射场景下的类型索引错位问题。
- 修复 `WriteData(Binary, int[])` 与 `WriteRow(Binary, object?[], int[]?)` 在 `idx < 0` 时越界访问 `ts[idx]` 的问题，改为按目标列类型写入空值。
- `Read(DataTable)` 设置 `Total` 与其他读取方式保持一致。
- `GetXml()` 改为同步等待 `WriteXml` 完成，避免 `Wait(15000)` 可能导致内容不完整。
- 改进枚举器实现，支持 `Reset()` 后重新枚举。

## 参考

- 文档：https://newlifex.com/core/dbtable
- 命名空间：`NewLife.Data`
