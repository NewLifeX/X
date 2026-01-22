# CsvFile 使用手册

本文档基于源码 `NewLife.Core/IO/CsvFile.cs`，用于说明 `CsvFile`（CSV 读写器）的设计目标、RFC4180 兼容行为、同步/异步 API，以及大文件场景下的使用建议。

> 关键词：RFC4180、流式解析、引号字段、CRLF、增量读写、Encoding、Separator。

---

## 1. 概述

`CsvFile` 是一个面向“超大 CSV 文件”的轻量工具类，支持：

- **逐行（Record）读取**：按记录解析而不是简单 `ReadLine()+Split`；
- **逐行写入**：按需追加写入，避免一次性构建整个文件；
- **RFC4180 基本规则兼容**：
  - 字段使用 `Separator` 分隔；
  - 字段中含分隔符/换行/双引号时使用双引号包裹；
  - 字段内双引号用 `""` 转义；
  - 允许引号字段内部出现换行（跨行字段）。

适用场景：

- 数据导入导出；
- 需要对超大 CSV 做流式读取、边读边处理；
- 需要正确处理含逗号/换行/引号的字段。

---

## 2. 核心属性

### 2.1 `Encoding`

- 类型：`Encoding`
- 默认：`Encoding.UTF8`

影响：

- 读取时用于构造 `StreamReader`；
- 写入时用于构造 `StreamWriter`。

说明：

- `EnsureReader()` 使用 `new StreamReader(_stream, Encoding)`，默认启用 BOM 检测（`detectEncodingFromByteOrderMarks=true` 为默认行为）。

### 2.2 `Separator`

- 类型：`Char`
- 默认：`,`

说明：

- 读取时用于字段分隔；
- 写入时用于拼接字段，并据此判断是否需要加引号。

---

## 3. 构造与资源管理

### 3.1 构造方式

- `CsvFile(Stream stream)`
- `CsvFile(Stream stream, Boolean leaveOpen)`
- `CsvFile(String file, Boolean write = false)`

`write=false`：以 `FileAccess.Read` 打开（只读）。

`write=true`：以 `FileAccess.ReadWrite` 打开（读写，不自动截断）。适合增量追加或覆盖写（覆盖写时由调用方控制 `Position/SetLength`）。

### 3.2 `leaveOpen`

当使用 `CsvFile(Stream, leaveOpen:true)`：

- `Dispose()` 不会关闭 `_stream`；
- 但仍会 `Flush()`/释放内部 `_reader/_writer`。

### 3.3 Dispose 行为

- `Dispose()` 会先 `_writer?.Flush()`，避免写入器缓冲未落盘；
- 若 `_leaveOpen=false`：会释放 `_reader/_writer` 并关闭流。

在 `NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER` 下还提供 `DisposeAsync()`。

---

## 4. 读取（RFC4180 风格 Record 解析）

### 4.1 `String[]? ReadLine()`

读取一条记录（Record），返回字段数组：

- EOF 返回 `null`；
- 支持引号字段内部包含 `Separator`、`\r\n`、`\n`；
- `""` 解析为 `"`；
- 支持尾部空字段（例如 `a,b,` => 三个字段，最后一个为空字符串）。

### 4.2 `IEnumerable<String[]> ReadAll()`

同步枚举读取全部记录：

- 内部循环调用 `ReadLine()` 直到 EOF。

### 4.3 异步读取

仅在 `NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER` 下可用：

- `ValueTask<String[]?> ReadLineAsync()`
- `IAsyncEnumerable<String[]> ReadAllAsync()`

异步实现采用内部字符缓冲区（`Char[4096]`）解析。

---

## 5. 写入（RFC4180 风格转义）

### 5.1 `void WriteLine(IEnumerable<Object?> line)`

写一行记录（自动追加行尾换行）：

- `DateTime`：使用 `ToFullString("")`；
- `Boolean`：写 `1/0`；
- 其它类型：`item + ""` 转字符串；
- **长整数字符串（长度 > 9 且可解析为 Int64）**：前置 `\t`，用于避免 Excel/WPS 等显示为科学计数法；
- 若字段包含：分隔符/CR/LF/双引号，则整体加双引号，并将内部双引号替换为 `""`。

### 5.2 `void WriteAll(IEnumerable<IEnumerable<Object?>> data)`

逐行写入：

- 内部循环调用 `WriteLine(line)`。

### 5.3 `Task WriteLineAsync(IEnumerable<Object> line)`

异步写一行（仅写入器异步）。

注意：

- 该方法参数为 `IEnumerable<Object>`，不接受 `null` 项（与同步 `Object?` 不同）。

---

## 6. 最小示例

### 6.1 读取 CSV

```csharp
using NewLife.IO;

using var csv = new CsvFile("./data.csv");

while (true)
{
    var row = csv.ReadLine();
    if (row == null) break;

    // row 是字段数组
    // 例如：row[0], row[1] ...
}
```

### 6.2 写入 CSV（覆盖写）

```csharp
using NewLife.IO;

using var csv = new CsvFile("./out.csv", write: true);

// 直接写入
csv.WriteLine("Id", "Name", "Remark");
csv.WriteLine(1, "Stone", "hello,world");
```

### 6.3 追加写（增量）

```csharp
using NewLife.IO;

using var csv = new CsvFile("./out.csv", write: true);

// 构造函数 write:true 以 ReadWrite 打开且不截断
// 若要追加到尾部，可自行定位
// （也可用 FileStream + CsvFile(stream, true) 更灵活）

csv.WriteLine(DateTime.Now, true, "append");
```

---

## 7. 注意事项与常见问题

### 7.1 读取不是按“物理行”而是按“记录”

当字段被双引号包裹且内部含换行时，`ReadLine()` 会跨越多行读到引号闭合为止，这是 CSV 语义正确的行为。

### 7.2 `Separator` 改为制表符（TSV）

```csharp
var csv = new CsvFile(file) { Separator = '\t' };
```

写入时会据此判断是否需要引号。

### 7.3 编码选择

- Excel 在某些环境下对 UTF-8 无 BOM 识别不佳；若需要兼容，可考虑写入前自行输出 BOM 或改用 `Encoding.UTF8` 带 BOM 的版本（由调用方控制流写入）。

---

## 8. 相关链接

- 在线文档：`https://newlifex.com/core/csv_file`
- 源码：`NewLife.Core/IO/CsvFile.cs`
