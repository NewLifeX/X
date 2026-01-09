# ExcelReader 使用手册

本文档基于源码 `NewLife.Core/IO/ExcelReader.cs`，用于说明 `ExcelReader`（轻量级 Excel xlsx 读取器）的定位、支持范围、数据读取方式、类型转换规则与使用注意事项。

> 关键词：xlsx、ZipArchive、sharedStrings、styles、sheetData、列索引 AA/AB、缺失列补齐、数值格式。

---

## 1. 概述

`ExcelReader` 是一个仅用于“导入数据”的轻量级 xlsx 读取器。

- 仅支持 `xlsx`（OpenXML），本质是 zip 压缩包；
- 不依赖第三方 Office/Interop 组件；
- 当前实现只做最小化解析：
  - 共享字符串（`xl/sharedStrings.xml`）
  - 样式（`xl/styles.xml`，数字格式）
  - 工作表数据（`xl/worksheets/sheet*.xml` 中的 `sheetData`）

适用场景：

- 服务器/桌面端批量导入 Excel；
- 只需要把工作表按行读取成对象数组；
- 不关注公式计算、合并单元格、图表、批注等复杂特性。

---

## 2. 构造与资源管理

### 2.1 `ExcelReader(String fileName)`

- 以共享方式打开文件：`FileShare.ReadWrite`，避免文件被其它进程占用时报错；
- 用 `ZipArchive` 读取 zip 内容；
- 构造函数会立即调用 `Parse()` 解析必要的索引。

### 2.2 `ExcelReader(Stream stream, Encoding encoding)`

- 传入 xlsx 数据流（调用方负责流生命周期，需保持可读）；
- `encoding` 用于 zip 条目名称/注释等编码（一般为 UTF-8）。

### 2.3 Dispose

`ExcelReader` 继承 `DisposeBase`：

- `Dispose(Boolean)` 会清理 `_entries` 并释放 `_zip`；
- 这会同时释放其底层 `FileStream`（若由构造函数创建）。

建议：

- 始终使用 `using var reader = new ExcelReader(...)`。

---

## 3. 基本属性

### 3.1 `FileName`

- 类型：`String?`
- 从文件构造函数直接赋值；
- 从流构造函数中，当 `stream is FileStream` 时取 `fs.Name`。

### 3.2 `Sheets`

- 类型：`ICollection<String>?`
- 语义：可用工作表名称集合（键来自 `_entries.Keys`）。

说明：

- `Parse()` 会把工作表名称映射到对应 `ZipArchiveEntry`。

---

## 4. 读取数据

### 4.1 `IEnumerable<Object?[]> ReadRows(String? sheet = null)`

按行返回数据（第一行通常是表头）：

- `sheet=null` 时默认取 `Sheets.FirstOrDefault()`；
- 找不到工作表会抛 `ArgumentOutOfRangeException`；
- 读取流程：
  1. 打开目标 sheet 条目流；
  2. `XDocument.Load` 读取 XML；
  3. 在根节点下找 `sheetData`；
  4. 遍历每个 `<row>`，对下面 `<c>` 单元格进行解析。

返回值：

- 每一行是一个 `Object?[]`；
- 值可能被转换为：`DateTime` / `TimeSpan` / `Int32` / `Int64` / `Decimal` / `Double` / `Boolean` / `String`；
- 无值或缺失列以 `null` 表示。

### 4.2 关键行为：列索引与缺失列补齐

Excel 单元格引用如 `A1`、`AB23`；实现会：

- 解析列字母为 0 基索引（`A=0`，`B=1`，`AA=26`）；
- 若本行出现跳列（例如只有 A、C），会自动把 B 补为 `null`；
- 会记录首行列数 `headerColumnCount`，后续行若尾部列缺失也会补齐到与首行一致。

这使得：

- 读取结果更接近“二维表格”的直观结构；
- 便于直接按列索引访问。

---

## 5. 单元格类型解析与转换规则

### 5.1 共享字符串（`t="s"`）

当单元格属性 `t="s"`：

- `<v>` 存储的是共享字符串索引；
- 会到 `_sharedStrings[sharedIndex]` 取真实文本。

共享字符串来自 `xl/sharedStrings.xml`，该条目可能缺失（允许）。

### 5.2 布尔（`t="b"`）

- `0/1` 或 `true/false`；
- 转为 `Boolean`。

### 5.3 公式结果文本（`t="str"`）

- 不做特殊处理，直接取文本值。

### 5.4 数字/日期/时间：样式驱动转换

当单元格值为字符串且存在样式 `_styles` 时：

- 读取单元格属性 `s`（StyleIndex）；
- 根据 `styles[si]` 的 `NumFmtId/Format` 决定转换策略。

转换逻辑位于 `ChangeType(Object? val, ExcelNumberFormat st)`：

- **日期/时间**：
  - 条件：格式包含 `yy`/`mmm` 或 `NumFmtId` 在 14~17 或为 22；
  - Excel 序列值以 1900-01-01 为基准，历史兼容实现会做 `d-2` 调整；
  - 使用 `AddSeconds(Math.Round((d - 2) * 24 * 3600))`，尽量规避浮点误差。

- **时间间隔**（TimeSpan）：
  - 条件：`NumFmtId` 在 18~21 或 45~47；
  - 转为 `TimeSpan.FromSeconds(Math.Round(d2 * 24 * 3600))`。

- **General / 0**：
  - 条件：`NumFmtId == 0`；
  - 依次尝试 `Int32`、`Int64`、`Decimal(InvariantCulture)`、`Double`。

- **整数格式**：
  - 条件：`NumFmtId` 为 1/3/37/38；
  - 尝试 `Int32/Int64`。

- **小数格式**：
  - 条件：`NumFmtId` 为 2/4/11/39/40；
  - 尝试 `Decimal(InvariantCulture)` 或 `Double`。

- **百分比**：
  - 条件：`NumFmtId` 为 9/10；
  - 尝试 `Double`（注意：得到的是 0.x，如 12% => 0.12）。

- **文本格式**：
  - 条件：`NumFmtId == 49`；
  - 若可解析为数值则再转回字符串（避免导入时进入数值类型）。

---

## 6. 最小示例

### 6.1 读取第一个工作表

```csharp
using NewLife.IO;

using var reader = new ExcelReader("./data.xlsx");

foreach (var row in reader.ReadRows())
{
    // 第一行通常是表头
    // row[i] 可能是 String/Int32/DateTime/Boolean/TimeSpan/null
}
```

### 6.2 指定工作表名称

```csharp
using var reader = new ExcelReader("./data.xlsx");

var sheet = reader.Sheets?.FirstOrDefault();
if (!sheet.IsNullOrEmpty())
{
    foreach (var row in reader.ReadRows(sheet))
    {
    }
}
```

### 6.3 与 `CsvFile` 组合：Excel 转 CSV

```csharp
using NewLife.IO;

using var reader = new ExcelReader("./data.xlsx");
using var csv = new CsvFile("./out.csv", write: true);

foreach (var row in reader.ReadRows())
{
    csv.WriteLine(row);
}
```

---

## 7. 注意事项与常见问题

### 7.1 只读取 `sheetData`

本实现只读取 `sheetData`，不会解析：

- 合并单元格（mergedCells）
- 公式计算（只读结果）
- 图片/图表/批注

若需要扩展，可根据 OpenXML 结构基于 `ZipArchive` 条目继续解析。

### 7.2 内存占用

当前实现对每次 `ReadRows()`：

- 会 `XDocument.Load` 把整个 sheet XML 载入内存。

对超大工作表可能占用较多内存；若要支持更大文件，需要改为 `XmlReader` 流式解析（属于功能扩展，不在本文档范围）。

### 7.3 日期偏移（减 2）

源码中对 Excel 日期序列值使用 `d - 2` 的历史兼容行为，用于匹配现有用户期望。若你对日期精度有严格要求，需要结合具体样例验证。

---

## 8. 相关链接

- 在线文档：`https://newlifex.com/core/excel_reader`
- 源码：`NewLife.Core/IO/ExcelReader.cs`
