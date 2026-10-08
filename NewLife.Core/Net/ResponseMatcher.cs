using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>响应匹配器。请求-响应配对的通用支撑：等待登记、匹配交付、关闭清理，与传输形态无关</summary>
/// <remarks>
/// <para>匹配不以消息泵为前提，而是交付阶段的能力：流式会话（TCP）由消息泵在帧定界交付时调用 <see cref="TryMatch"/>；
/// 数据报会话（UDP，每包即完整帧、无粘包处理）在报文交付时调用，二者共用本组件与同一匹配队列，等待与超时语义一致。</para>
/// <para>队列首次等待时自动创建，可注入共享或自定义实现（<see cref="Queue"/>）。</para>
/// </remarks>
sealed class ResponseMatcher
{
    #region 属性
    /// <summary>匹配队列。首次等待时自动创建，可注入共享或自定义实现</summary>
    public IMatchQueue? Queue
    {
        get => _queue;
        set => _queue = value;
    }

    private IMatchQueue? _queue;

    /// <summary>请求-响应匹配等待超时（毫秒）。默认30_000</summary>
    public Int32 Timeout { get; set; } = 30_000;
    #endregion

    #region 等待
    /// <summary>登记请求等待。调用方随后送出请求，响应到达时经 <see cref="TryMatch"/> 交付</summary>
    /// <param name="owner">拥有者（会话实例）。匹配按拥有者过滤，收到的消息只交付同拥有者的等待</param>
    /// <param name="request">请求消息</param>
    /// <param name="span">关联的性能追踪 Span，随等待方签收而释放</param>
    /// <returns>等待源。调用方负责把 <see cref="PooledValueTaskSource{T}.ValueTask"/> 交给等待方，送出失败时 <c>TrySetException</c></returns>
    public PooledValueTaskSource<IMessage> Register(Object owner, IMessage request, ISpan? span)
    {
        // 并发首用时只能有一个队列胜出：败者丢弃自建实例，否则请求会入队到无人匹配的队列，只能等超时
        var queue = _queue;
        if (queue == null)
        {
            var created = new DefaultMatchQueue();
            queue = Interlocked.CompareExchange(ref _queue, created, null) ?? created;
        }

        var source = PooledValueTaskSource<IMessage>.Rent();
        source.AttachSpan(span);

        queue.Add(owner, request, Timeout, source);

        return source;
    }
    #endregion

    #region 匹配
    /// <summary>尝试把收到的消息匹配给等待中的请求。命中则交付等待方并返回 true</summary>
    /// <param name="owner">拥有者（与登记时一致）</param>
    /// <param name="codec">协议编解码器，配对能力在内层时逐层解包</param>
    /// <param name="message">收到的消息</param>
    /// <returns>是否已匹配交付。未命中、协议无配对能力或等待方已失效（取消、等待源已复用）时返回 false，调用方据此按普通消息收尾</returns>
    public Boolean TryMatch(Object owner, IMessageCodec? codec, IMessage message)
    {
        var queue = _queue;
        if (queue == null) return false;

        var matcher = Resolve(codec);
        if (matcher == null) return false;

        return queue.Match(owner, message, message, (req, resp) =>
            req is IMessage rq && resp is IMessage rs && matcher.Match(rq, rs));
    }

    /// <summary>取出协议的请求-响应配对能力。装饰协议（压缩/加密等）把配对能力留给内层，需逐层解包</summary>
    /// <param name="codec">协议</param>
    /// <returns>配对器；协议不支持配对时返回 null</returns>
    public static IMessageMatcher? Resolve(IMessageCodec? codec) => codec switch
    {
        IMessageMatcher matcher => matcher,
        IMessageCodecDecorator decorator => Resolve(decorator.Inner),
        _ => null,
    };
    #endregion

    #region 清理
    /// <summary>清空队列，唤醒全部等待方（会话关闭时调用，避免调用方悬挂）</summary>
    public void Clear() => _queue?.Clear();
    #endregion
}
