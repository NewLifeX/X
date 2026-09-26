using System.Buffers;
using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>消息编解码器（帧协议）。定义一种帧格式的字节流与消息互转：定界+构造、整帧构建、头部构建</summary>
/// <remarks>
/// <para><b>角色</b>：协议层唯一契约。实现类描述一种帧格式（如 <see cref="SrmpCodec"/> 的标准消息），无状态、可跨连接共享。</para>
/// <list type="bullet">
/// <item><description><b>定界+构造</b>：<see cref="TryParse"/> 从帧首窗口解析头部并构造消息；头部不足返回 null，不消费、不产生对象。</description></item>
/// <item><description><b>整帧构建</b>：<see cref="Build"/> 构建头 + 内存体的完整帧（发送路径）。</description></item>
/// <item><description><b>头部构建</b>：<see cref="BuildHeader"/> 仅构建头部并声明体长，配合流式体发送（先发头、再流式送体）。</description></item>
/// </list>
/// <para><b>体绑定</b>：TryParse 成功后由帧层（<see cref="MessagePump"/>）绑定消息体——帧已完整为内存视图（零拷贝），帧未完整为流式读取器（数据随管道到达）。</para>
/// <para><b>尺寸不变量</b>：<see cref="ParseResult.HeaderSize"/> + <see cref="ParseResult.BodyLength"/> 就是帧层本次需要消费的字节数。
/// 帧尾分隔符不计入字段的协议（如 <see cref="SplitDataCodec"/>）把帧尾留给下一帧作为“前导分隔符”跳过，
/// 因此消费方（<see cref="MessagePump"/>、<c>UdpSession</c>、装饰协议）可以统一按“头长 + 体长”推进窗口。</para>
/// <para><b>能力矩阵（并非每个成员每个协议都支持）</b>：定界是协议的本职（均实现）；整帧构建要求内存体；
/// 头部构建仅长度可预声明的协议支持（<see cref="SplitDataCodec"/> 与 <see cref="CompressedCodec"/> 会抛 <see cref="NotSupportedException"/>）；
/// 请求-响应配对是可选能力，由 <see cref="IMessageMatcher"/> 单独表达。装饰器（<see cref="IMessageCodecDecorator"/>）把这些能力留给内层，
/// 消费方需逐层解包后再查询（参见 <c>SessionBase</c> 的配对器解包）。不可组合的协议对：分隔符协议不能作为压缩的内层——压缩字节可能含分隔符，会被提前切开。</para>
/// </remarks>
/// <example>
/// <code>
/// var pump = new MessagePump(new SrmpCodec());
/// if (pump.TryRead(pipe.Reader, out var msg))
/// {
///     // msg 头部字段就位、体已绑定；读取负载后释放
///     var body = await msg.Body.ReadAllAsync();
///     msg.TryDispose();
/// }
/// </code>
/// </example>
public interface IMessageCodec
{
    /// <summary>定界并构造消息。从帧首窗口解析头部，构造头部字段就位的消息实例</summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <returns>解析结果；null 表示头部数据不足（不消费、不产生对象）。结果的消息为 null 表示无消息帧：帧已被消费但无内容交付（空行/心跳/注释等），帧层跳过 HeaderSize 字节后继续解析下一帧</returns>
    /// <remarks>实现须保证“成功即有进展”：产出消息时消费 HeaderSize；无消息帧时跳过 HeaderSize（&gt;0）。既不产出消息又不消费字节会被帧层拒绝，避免死循环。</remarks>
    ParseResult? TryParse(ReadOnlySequence<Byte> buffer);

    /// <summary>整帧构建（头 + 内存体）。构建不消费消息负载，消息数据保持原样</summary>
    /// <param name="message">消息（体须为内存模式）</param>
    /// <returns>整帧拥有句柄，调用方负责 Dispose；无内容可发时为 null</returns>
    /// <remarks>已预留的负载零拷贝借位共享（帧头落在预留区），其余以新头节点挂接负载链；两种策略都不改动 <see cref="IMessage.Payload"/>。</remarks>
    /// <exception cref="InvalidOperationException">消息体为流式绑定，无法整帧构建（请改用 <see cref="BuildHeader"/> + 流式发送）</exception>
    IOwnerPacket? Build(IMessage message);

    /// <summary>仅构建头部数据包，声明消息体长度（头 + 流式体发送）。构建不改动消息数据</summary>
    /// <param name="message">消息（提供头部字段）</param>
    /// <param name="bodyLength">消息体字节数</param>
    /// <returns>头部拥有句柄，调用方负责 Dispose</returns>
    IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength);
}

/// <summary>协议装饰器。装饰内层协议（压缩/加密等变换层），自身不确定的能力（如请求-响应配对）由内层决定</summary>
/// <remarks>
/// <para>同一个 <see cref="IMessageCodec"/> 可以任意嵌套组合，但只能由最内层承担帧定界与配对：
/// 消费方需逐层解包到实际协议再查询能力（如 <see cref="IMessageMatcher"/>），否则会误判“不支持”。</para>
/// </remarks>
public interface IMessageCodecDecorator
{
    /// <summary>内层协议</summary>
    IMessageCodec Inner { get; }
}

/// <summary>帧解析结果。<see cref="IMessageCodec.TryParse"/> 的产出：消息实例与帧尺寸</summary>
/// <remarks>
/// <para>消息为 null 表示无消息帧：帧已被消费但无内容交付（空行/心跳/注释等），帧层跳过 <see cref="HeaderSize"/> 字节后继续解析下一帧。</para>
/// <para>TryParse 返回 null（整个结果为空）表示头部数据不足：不消费、不产生对象，等待更多数据。</para>
/// </remarks>
public readonly struct ParseResult
{
    /// <summary>解析出的消息。成功且产出消息时有效；null 表示无消息帧（跳过 HeaderSize 字节）</summary>
    public IMessage? Message { get; init; }

    /// <summary>头部字节数。无消息帧时为要跳过的字节数（须大于 0）</summary>
    public Int32 HeaderSize { get; init; }

    /// <summary>消息体字节数。无消息帧时无意义</summary>
    public Int64 BodyLength { get; init; }

    /// <summary>是否为损坏帧。头部已完整但内容非法（如长度字段为负、超出协议上限）</summary>
    /// <remarks>
    /// <para>与“<see cref="IMessageCodec.TryParse"/> 返回 null（头部不足）”必须区分：帧层对不足是等待更多数据，
    /// 对损坏应立即报错——流式传输关闭连接、数据报丢弃该包；一律当作不足会让连接僵死到残余上限才断开。</para>
    /// </remarks>
    public Boolean Invalid { get; init; }
}
