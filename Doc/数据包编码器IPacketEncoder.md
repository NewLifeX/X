# 数据包编码器IPacketEncoder

## 概述

`IPacketEncoder` 是 NewLife.Core 中对象与数据包（`IPacket`）之间双向转换的编码器接口。与 `PacketCodec`（网络粘包处理）不同，`IPacketEncoder` 关注的是"对象 ↔ 字节包"的序列化转换，支持多种类型的自动编码策略选择。

**命名空间**：`NewLife.Data`  
**文档地址**：https://newlifex.com/core/packet_encoder

## 核心接口

```csharp
public interface IPacketEncoder
{
    /// <summary>将对象编码为数据包</summary>
    IPacket? Encode(Object? value);

    /// <summary>将数据包解码为指定类型的对象</summary>
    Object? Decode(IPacket data, Type type);
}

public static class PacketEncoderExtensions
{
    /// <summary>泛型解码扩展</summary>
    public static T? Decode<T>(this IPacketEncoder encoder, IPacket data);
}
```

## 默认实现 DefaultPacketEncoder

`DefaultPacketEncoder` 根据对象类型自动选择编码策略：

| 类型 | 编码策略 |
|------|----------|
| 基础类型（Int32、Double 等） | 直接转换为字符串再转 UTF-8 字节数组 |
| `String` | 直接转 UTF-8 字节数组 |
| `DateTime` | 格式化为 `yyyy-MM-dd HH:mm:ss.fff` |
| `IPacket` | 直接返回或适配 |
| `Byte[]` | 包装为 `ArrayPacket` |
| `IAccessor`（访问器） | 调用 `ToPacket()` 方法 |
| 复杂类型 | 使用 JSON 序列化 |

## 快速开始

```csharp
using NewLife.Data;

var encoder = new DefaultPacketEncoder();

// 编码
var data = encoder.Encode("Hello, World!");
var numData = encoder.Encode(12345);

// 解码
var str = encoder.Decode<String>(data);
var num = encoder.Decode<Int32>(numData);
```

## 与 PacketCodec 的区别

| 特性 | `IPacketEncoder` | `PacketCodec` |
|------|------------------|---------------|
| 职责 | 对象 ↔ 数据包转换 | TCP 粘包拆包 |
| 输入 | 任意对象 | 网络字节流 |
| 输出 | 完整数据包 | 完整数据包集合 |
| 状态 | 无状态 | 有状态（缓存） |
| 命名空间 | `NewLife.Data` | `NewLife.Messaging` |
