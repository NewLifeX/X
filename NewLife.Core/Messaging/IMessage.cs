using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Messaging;

/// <summary>消息命令接口</summary>
/// <remarks>
/// 消息接口定义了请求-响应模式的基本消息结构：
/// <list type="bullet">
/// <item><description><see cref="Reply"/>：标识消息方向，请求或响应</description></item>
/// <item><description><see cref="Error"/>：标识处理状态，成功或失败</description></item>
/// <item><description><see cref="OneWay"/>：标识通信模式，单向或双向</description></item>
/// <item><description><see cref="Payload"/>：消息负载数据</description></item>
/// </list>
/// 实现类应保证线程安全性，特别是在消息池化复用场景。
/// </remarks>
public interface IMessage : IDisposable
{
    /// <summary>是否响应。为true表示这是响应消息，为false表示这是请求消息</summary>
    Boolean Reply { get; set; }

    /// <summary>是否有错。为true表示处理过程中发生错误</summary>
    Boolean Error { get; set; }

    /// <summary>单向请求。为true表示不需要等待响应</summary>
    Boolean OneWay { get; set; }

    /// <summary>负载数据。消息的实际内容</summary>
    IPacket? Payload { get; set; }

    /// <summary>根据请求创建配对的响应消息</summary>
    /// <remarks>
    /// 响应消息会继承请求消息的序列号等关键属性，用于请求-响应匹配。
    /// 仅请求消息可调用此方法，响应消息调用将抛出异常。
    /// </remarks>
    /// <returns>响应消息实例</returns>
    /// <exception cref="InvalidOperationException">当在响应消息上调用时抛出</exception>
    IMessage CreateReply();

    /// <summary>从数据包中读取消息</summary>
    /// <param name="pk">原始数据包</param>
    /// <returns>是否成功解析</returns>
    Boolean Read(IPacket pk);

    /// <summary>把消息转为封包</summary>
    /// <returns>序列化后的数据包</returns>
    IPacket? ToPacket();
}

/// <summary>消息命令基类</summary>
/// <remarks>
/// 提供 <see cref="IMessage"/> 接口的基础实现，支持：
/// <list type="bullet">
/// <item><description>基本的请求/响应/错误/单向标志位</description></item>
/// <item><description>负载数据的透传</description></item>
/// <item><description>资源释放（回收数据包到内存池）</description></item>
/// </list>
/// 子类可重写 <see cref="Read"/> 和 <see cref="ToPacket"/> 实现自定义协议格式。
/// </remarks>
public class Message : IMessage
{
    #region 属性
    /// <summary>是否响应。为true表示这是响应消息，为false表示这是请求消息</summary>
    public Boolean Reply { get; set; }

    /// <summary>是否有错。为true表示处理过程中发生错误</summary>
    public Boolean Error { get; set; }

    /// <summary>单向请求。为true表示不需要等待响应</summary>
    public Boolean OneWay { get; set; }

    /// <summary>负载数据。消息的实际内容</summary>
    public IPacket? Payload { get; set; }
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
            Payload.TryDispose();
            Payload = null;
        }
    }
    #endregion

    #region 方法
    /// <summary>根据请求创建配对的响应消息</summary>
    /// <returns>响应消息实例</returns>
    public virtual IMessage CreateReply()
    {
        if (Reply) throw new InvalidOperationException("Cannot create response message based on response message");

        var msg = CreateInstance();
        msg.Reply = true;

        return msg;
    }

    /// <summary>创建当前类型的新实例</summary>
    /// <remarks>子类可重写以避免反射开销，或实现对象池化复用</remarks>
    /// <returns>新的消息实例</returns>
    protected virtual Message CreateInstance()
    {
        var type = GetType();
        if (type == typeof(Message)) return new Message();

        var msg = type.CreateInstance() as Message;
        if (msg == null) throw new InvalidOperationException($"Cannot create an instance of type [{type.FullName}]");

        return msg;
    }

    /// <summary>从数据包中读取消息</summary>
    /// <param name="pk">原始数据包</param>
    /// <returns>是否成功解析</returns>
    public virtual Boolean Read(IPacket pk)
    {
        Payload = pk;

        return true;
    }

    /// <summary>把消息转为封包</summary>
    /// <returns>序列化后的数据包</returns>
    public virtual IPacket? ToPacket() => Payload;

    /// <summary>重置消息状态，用于对象池复用</summary>
    /// <remarks>子类应重写此方法以重置所有字段到初始状态</remarks>
    public virtual void Reset()
    {
        Reply = false;
        Error = false;
        OneWay = false;
        Payload = null;
    }
    #endregion
}