using NewLife.Buffers;

namespace NewLife.Serialization;

/// <summary>高性能Span序列化接口</summary>
/// <remarks>
/// 实现此接口的类型可通过 <see cref="SpanSerializer"/> 进行零反射、极低分配的二进制序列化。
/// 适用于RPC通信快速序列化和高频文件读写场景。
/// <para>
/// 实现者只需关注自身成员的读写，null标记由 <see cref="SpanSerializer"/> 统一处理。
/// </para>
/// </remarks>
public interface ISpanSerializable
{
    /// <summary>将对象成员序列化写入SpanWriter</summary>
    /// <param name="writer">Span写入器</param>
    void Write(ref SpanWriter writer);

    /// <summary>从SpanReader反序列化读取对象成员</summary>
    /// <param name="reader">Span读取器</param>
    void Read(ref SpanReader reader);
}
