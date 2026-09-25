﻿using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Messaging;

/// <summary>消息种类。与协议状态位一一对应：00 请求 / 01 单向 / 10 响应 / 11 响应+错误</summary>
/// <remarks>
/// 值即协议位（头部高 2 位 mode），请求-响应语义的完整分类。
/// 仅带状态位协议的消息使用方向字段；无状态位协议（长度字段/分隔符等）不使用，保持默认的请求即可。
/// </remarks>
public enum MessageKinds
{
    /// <summary>请求</summary>
    Request = 0,

    /// <summary>单向请求。不需要等待响应</summary>
    OneWay = 1,

    /// <summary>响应</summary>
    Response = 2,

    /// <summary>响应+错误</summary>
    Error = 3,
}

/// <summary>消息契约。纯数据与负载读写；帧格式的解析与构建由 <see cref="IMessageCodec"/> 承担</summary>
/// <remarks>
/// <para>消息接口定义消息的最小契约：负载读写（<see cref="Payload"/>/<see cref="Body"/>）与方向语义（<see cref="Kind"/>/<see cref="Reply"/>）。</para>
/// <para>消息为纯数据载体，不感知帧字节：定界/解析/构建由协议对象（<see cref="IMessageCodec"/>）统一处理。</para>
/// <para>协议字段的读写属于消息类：带帧字段的消息子类公开 TryParse/WriteHeader 方法，由 codec 委托调用（参考 <see cref="DefaultMessage"/> 与 <see cref="WsMessage"/>）。</para>
/// <para>实现类须自行声明线程安全性（本接口不承诺线程安全；<see cref="Message"/> 及其子类为非线程安全）。</para>
/// </remarks>
public interface IMessage : IDisposable
{
    /// <summary>负载数据包。消息体的句柄视图：整帧/发送模式为底层数据包（零拷贝，所有权随消息）；流式模式为 null</summary>
    /// <remarks>
    /// 负载为数据包（借阅视图或拥有帧/链）。拥有帧的所有权随消息持有：消息 <see cref="IDisposable.Dispose"/>
    /// 时唯一归还（<see cref="IPacket"/> 实现 Dispose 归还池化缓冲）；
    /// 借阅视图（ArrayPacket 等）无所有权，仅在本轮同步链路内有效。写路径统一走 <see cref="SetBody"/>（null 表示所有权转移、不归还）。
    /// 流式消息体经 <see cref="Body"/> 读取，本属性为 null。
    /// </remarks>
    IPacket? Payload { get; }

    /// <summary>消息体。整帧模式为内存视图，头先行模式为限长流式读取器；未设置时为 null</summary>
    LimitedReader? Body { get; }

    /// <summary>消息种类。请求/单向/响应/响应+错误，与协议状态位一一对应</summary>
    /// <remarks>无状态位协议不使用本字段，保持默认的请求即可</remarks>
    MessageKinds Kind { get; set; }

    /// <summary>是否为应答消息（响应或响应+错误）。配对交付与流式物化的判断入口</summary>
    Boolean Reply { get; }

    /// <summary>根据请求创建配对的响应消息（继承序列号等配对键）</summary>
    /// <returns>响应消息实例；当前消息是应答（<see cref="Reply"/>）或协议不支持回复时返回 null</returns>
    IMessage? CreateReply();

    /// <summary>设置消息体（整帧/发送路径）。拥有句柄所有权随消息，Dispose 时归还；传入 null 表示所有权转移（调用方接管），不归还</summary>
    /// <param name="packet">消息体数据包</param>
    void SetBody(IPacket? packet);

    /// <summary>绑定流式消息体（帧层专用）。头部先行解析后由帧泵调用</summary>
    /// <param name="body">限长读取器；null 表示清除流式体绑定（不归还其资源，由帧层管理）</param>
    void BindBody(LimitedReader body);
}

/// <summary>消息基类。负载协作面的基础实现；帧格式由 <see cref="IMessageCodec"/> 承担</summary>
/// <remarks>
/// <para>消息只承载头部字段与负载（<see cref="SetBody"/>/<see cref="BindBody"/> 由帧层在解析后绑定），不感知帧字节。</para>
/// <para>方向字段对无状态位协议无意义（保持默认请求）；<see cref="Reply"/> 与 <see cref="OneWay"/> 为 <see cref="Kind"/> 的便捷视图。</para>
/// </remarks>
public class Message : IMessage
{
    #region 属性
    /// <summary>消息体。整帧模式为内存视图，头先行模式为限长流式读取器；未设置时为 null</summary>
    public LimitedReader? Body { get; private set; }

    /// <summary>负载数据包。消息体的句柄视图：整帧/发送模式为底层数据包（零拷贝，所有权随消息）；流式模式为 null</summary>
    /// <remarks>写路径统一走 <see cref="SetBody"/>；置 null 表示所有权转移、不归还。</remarks>
    public IPacket? Payload { get; private set; }

    /// <summary>消息种类。请求/单向/响应/响应+错误，与协议状态位一一对应</summary>
    public MessageKinds Kind { get; set; }

    /// <summary>是否为应答消息（响应或响应+错误）。配对交付与流式物化的判断入口</summary>
    public Boolean Reply
    {
        get => Kind is MessageKinds.Response or MessageKinds.Error;
        set => Kind = value ? MessageKinds.Response : MessageKinds.Request;
    }

    /// <summary>是否为单向请求（无需应答）</summary>
    public Boolean OneWay
    {
        get => Kind == MessageKinds.OneWay;
        set => Kind = value ? MessageKinds.OneWay : MessageKinds.Request;
    }
    #endregion

    #region 构造
    /// <summary>销毁。回收数据包到内存池</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>释放资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(Boolean disposing)
    {
        if (disposing)
        {
            // 拥有帧时归还池化缓冲；借阅视图（ArrayPacket）Dispose 无操作；流式体的管道端由帧层管理
            Payload.TryDispose();
            Payload = null;
            Body = null;
        }
    }
    #endregion

    #region 方法
    /// <summary>创建当前类型的新实例</summary>
    /// <remarks>子类可重写以避免反射开销，或实现对象池化复用</remarks>
    /// <returns>新的消息实例</returns>
    protected virtual Message CreateInstance()
    {
        var type = GetType();
        if (type == typeof(Message)) return new Message();

        if (type.CreateInstance() is Message msg) return msg;

        throw new InvalidOperationException($"Cannot create an instance of type [{type.FullName}]");
    }

    /// <summary>根据请求创建配对的响应消息。基类不支持回复，返回 null；带配对键的协议子类重写</summary>
    /// <returns>响应消息实例；基类恒返回 null</returns>
    public virtual IMessage? CreateReply() => null;

    /// <summary>设置消息体（整帧/发送路径）</summary>
    /// <param name="packet">消息体数据包；置 null 表示所有权转移（调用方接管），不归还</param>
    public void SetBody(IPacket? packet)
    {
        var old = Payload;

        // 置 null 表示所有权转移：负载已随构建结果或调用方交付，消息不再持有，也不能归还。
        // 构建路径中 ExpandHeader 可能以旧句柄为后继链节点，此时归还会击穿结果包链
        if (packet == null)
        {
            Payload = null;
            Body = null;
            return;
        }

        Payload = packet;
        Body = new LimitedReader(packet, 0, packet.Total);

        // 替换旧体时归还其拥有帧
        if (old != null && !ReferenceEquals(old, packet)) old.TryDispose();
    }

    /// <summary>绑定流式消息体（帧层专用）。头部先行解析后由帧泵调用</summary>
    /// <param name="body">限长读取器</param>
    public void BindBody(LimitedReader body)
    {
        var old = Payload;
        Payload = null;
        Body = body;
        old?.TryDispose();
    }
    #endregion
}
