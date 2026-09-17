# 序列读取器SequenceReader

本文档介绍 `SequenceReader<T>` 在 NewLife.Core 中的可用性、实现组成与用法。它来自 BCL（`System.Buffers`），但**在 net45 等旧框架原本缺失**——本库通过垫片补齐，使全部 18 个目标框架获得统一的跨段顺序读取能力。

## 概述

`SequenceReader<T>` 是基于 `ReadOnlySequence<T>` 的顺序读取器（`ref struct`），面向"多段缓冲上顺序解析协议"的场景：读取头部字段、跳过前缀、查找分隔符、跨段拷贝——全程不要求数据连续，也无需先把多段拼成一段。

典型价值：

- **跨段直读**：`TryRead`/`TryReadBigEndian`/`TryAdvanceTo` 等直接在段链上工作，段边界透明；
- **零分配**：读取头部不产生堆分配（对比"栈缓冲 + 前缀拼读"的中间拷贝）；
- **可回退**：`Rewind`/`Consumed`/`Position` 支持"先看后决"的解析形态。

## 全 TFM 支持矩阵（编译探针实测）

| 目标框架 | `SequenceReader<T>` 来源 | 端序扩展 |
|----------|--------------------------|----------|
| net45 / net461 / net462 / netstandard2.0 | `Stub/SequenceReader.cs` 垫片（3.1 面，MIT 来源） | 有符号：垫片；无符号：`SequenceReaderHelper` |
| netstandard2.1 / netcoreapp3.1 | BCL 内置 | 有符号；无符号：`SequenceReaderHelper` |
| net5.0 ~ net10.0（含 windows 变体） | BCL 内置 | 有符号；无符号：`SequenceReaderHelper` |

> 统一面不含 `TryPeek(offset)` 与 `UnreadSequence`（ns2.1/3.1 与垫片没有，net5+ 才有）；无符号端序重载 BCL 从未提供，由本库补充。**跨 TFM 共享的库代码请使用下表交集面。**

## 实现组成

| 文件 | 作用 |
|------|------|
| `NewLife.Core/Stub/SequenceReader.cs` | 单文件移植（dotnet/corefx release/3.1 的 `SequenceReader.cs` + `SequenceReader.Search.cs` + `SequenceReaderExtensions.Binary.cs` 三文件合并，文件级命名空间），由 `NETCOREAPP3_0_OR_GREATER` / `NETSTANDARD2_1_OR_GREATER` 守卫、仅四个旧 TFM 生效；内部依赖（`ReadOnlySequence.GetFirstSpan`、`ThrowHelper`）已改写为等价公共 API。含 `SequenceReader<T>`（partial 两部分）与有符号端序扩展 `SequenceReaderExtensions`（`Int16`/`Int32`/`Int64` × 大小端） |
| `NewLife.Core/Stub/SequenceReaderForwards.cs` | `TypeForwardedTo`：高版本资产把类型查找转发到 BCL，保证"netstandard2.0 类库 + 现代运行时"混合场景的类型标识统一 |
| `NewLife.Core/Buffers/SequenceReaderHelper.cs` | 无符号端序读取/窥视扩展（`UInt16`/`UInt32`/`UInt64` × 大小端），全 TFM 可用 |

## 用法

```csharp
// 多段窗口（例如数据管道读取窗口、IPacket 链的序列桥接）
ReadOnlySequence<Byte> window = pk.AsReadOnlySequence();

var reader = new SequenceReader<Byte>(window);

// 读取大端 Int16 与无符号大端 UInt16（跨段透明）
if (reader.TryReadBigEndian(out Int16 magic) &&
    reader.TryReadBigEndian(out UInt16 length))
{
    // 跳过分隔符并取到分隔符之前的内容
    if (reader.TryAdvanceTo((Byte)'#'))
    {
        // 需要连续内存时再拷贝
        Span<Byte> head = stackalloc Byte[8];
        if (reader.TryCopyTo(head)) { /* ... */ }
    }
}
```

## 与 PacketHelper "前缀拼读"的关系

- **序列解析（推荐）**：面向 `ReadOnlySequence<Byte>` 的协议帧首解析已迁移到 `SequenceReader<T>`——`DefaultMessage`、`WebSocketMessage`、`MessageCodec`、`EventHub`、`LengthFieldCodec`；
- **链头部直读（保留）**：`IPacket` 链的单段头部场景仍可用 `PacketHelper.GetPrefix`（首段直读零拷贝，跨段时拼入调用方缓冲）；
- `PacketHelper.CopyPrefix` 保留为内部工具，当前协议解析不再依赖它。

## 兼容性说明

- 垫片面与 BCL 3.1 严格一致，避免"同代码在高版本可用、旧框架不可用"的割裂；
- 混合资产场景：netstandard2.0 类库引用本库垫片类型后，在 net10 应用运行时由 `TypeForwardedTo` 重定向到 BCL（见 `XUnitTest.Compat/CompatLib` 实测）；
- 若生态项目自带 `System.Buffers.SequenceReader` 垫片，与本库垫片在同一编译单元中会类型冲突（CS0433），请移除其一。

## 测试

`XUnitTest.Compat`（net462 / net8.0 双目标）以同一套 37 项用例分别验证垫片与 BCL 的行为一致性，覆盖：空序列/含空段多段链、跨段读写/回退/拷贝、端序扩展（含无符号）、`TryAdvanceTo`/`TryReadTo`/`IsNext`/`AdvancePast`、异常边界与转发链。

## 相关文档

- [缓冲区Buffers](./缓冲区Buffers.md)
- [Span读取器SpanReader](./Span读取器SpanReader.md)
- [数据包IPacket](./数据包IPacket.md)
- [数据管道Pipe](./数据管道Pipe.md)
