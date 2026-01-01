using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>消息工厂接口</summary>
/// <remarks>
/// 消息工厂负责创建和回收消息实例，支持：
/// <list type="bullet">
/// <item><description>对象池化：减少GC压力，提升高频消息场景性能</description></item>
/// <item><description>类型隔离：不同消息类型使用独立的工厂实例</description></item>
/// <item><description>生命周期管理：统一管理消息的创建、使用和回收</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TMessage">消息类型</typeparam>
public interface IMessageFactory<TMessage> where TMessage : IMessage
{
    /// <summary>创建或获取消息实例</summary>
    /// <returns>消息实例</returns>
    TMessage Create();

    /// <summary>从数据包解析消息</summary>
    /// <param name="pk">数据包</param>
    /// <returns>解析后的消息，解析失败返回null</returns>
    TMessage? Parse(IPacket pk);

    /// <summary>回收消息实例到对象池</summary>
    /// <param name="message">待回收的消息</param>
    void Return(TMessage message);
}

/// <summary>默认消息工厂。支持对象池化</summary>
/// <typeparam name="TMessage">消息类型，必须有无参构造函数</typeparam>
public class DefaultMessageFactory<TMessage> : IMessageFactory<TMessage>
    where TMessage : Message, new()
{
    #region 属性
    /// <summary>对象池</summary>
    private readonly Pool<TMessage> _pool = new();

    /// <summary>是否启用池化。默认true</summary>
    public Boolean EnablePooling { get; set; } = true;

    /// <summary>对象池最大容量。默认256</summary>
    public Int32 MaxPoolSize
    {
        get => _pool.Max;
        set => _pool.Max = value;
    }
    #endregion

    #region 方法
    /// <summary>创建或获取消息实例</summary>
    /// <returns>消息实例</returns>
    public virtual TMessage Create()
    {
        if (!EnablePooling) return new TMessage();

        return _pool.Get();
    }

    /// <summary>从数据包解析消息</summary>
    /// <param name="pk">数据包</param>
    /// <returns>解析后的消息，解析失败返回null</returns>
    public virtual TMessage? Parse(IPacket pk)
    {
        if (pk == null || pk.Total == 0) return default;

        var msg = Create();
        try
        {
            if (msg.Read(pk)) return msg;

            // 解析失败，回收消息
            Return(msg);
            return default;
        }
        catch
        {
            Return(msg);
            throw;
        }
    }

    /// <summary>回收消息实例到对象池</summary>
    /// <param name="message">待回收的消息</param>
    public virtual void Return(TMessage message)
    {
        if (message == null || !EnablePooling) return;

        // 重置消息状态
        message.Reset();

        _pool.Return(message);
    }
    #endregion
}

/// <summary>DefaultMessage 专用工厂</summary>
public class DefaultMessageFactory : DefaultMessageFactory<DefaultMessage>
{
    /// <summary>默认实例</summary>
    public static DefaultMessageFactory Instance { get; } = new();

    /// <summary>从数据包解析消息（静态便捷方法）</summary>
    /// <param name="pk">数据包</param>
    /// <returns>解析后的消息，解析失败返回null</returns>
    public static DefaultMessage? ParseMessage(IPacket pk) => Instance.Parse(pk);
}
