using System.Buffers;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Messaging;

/// <summary>获取数据帧长度的委托（兼容旧版 span 版本）</summary>
/// <param name="span">数据片段（帧首到可用数据末尾的连续内存）</param>
/// <returns>完整帧长度（包含头部长度位）；返回0或负数表示数据不足无法定界</returns>
public delegate Int32 GetLengthDelegate(ReadOnlySpan<Byte> span);

/// <summary>数据包编码器。用于网络粘包处理</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/packet_codec
/// 
/// <para><b>模型</b>：单一缓存链 + 共享切片切帧。缓存是一条由跨轮残段组成的单链；每次 Parse 先把本轮数据追加进缓存，再从链头连续切出完整帧。</para>
/// <para><b>所有权规则</b>：</para>
/// <list type="bullet">
/// <item><description>入参句柄归调用方，Parse 不释放；需要跨轮保留的部分以共享切片（拥有句柄输入）或视图（视图输入）追加进缓存，缓存持有自己的引用</description></item>
/// <item><description>返回帧为共享切片（或链）；拥有句柄输入时可跨轮持有（用后 Dispose），视图输入时仅本轮同步有效</description></item>
/// <item><description>帧恰好覆盖整条缓存时直接转移缓存节点句柄（零分配）；跨节点帧自动组链，各段独立引用</description></item>
/// <item><description>借阅视图的残片不能跨轮保留，Parse 结束前转为自有拷贝；Clear/Dispose 释放整条缓存链</description></item>
/// </list>
/// 
/// <para><b>使用方式</b>：</para>
/// <code>
/// var codec = new PacketCodec { GetLength = DefaultMessage.GetLength };
/// foreach (var frame in codec.Parse(receivedPacket))
/// {
///     // 帧为共享切片/链时可自由持有，用后 Dispose；视图输入时仅本轮同步有效
/// }
/// </code>
/// 
/// <para><b>线程安全</b>：单个实例不是线程安全的，每个连接应使用独立的编码器实例。</para>
/// </remarks>
public class PacketCodec : IDisposable
{
    #region 属性
    /// <summary>获取帧长度的委托。入参为缓存链头（帧首），实现方需链感知</summary>
    /// <remarks>
    /// 入参是缓存链（帧首节点及其后续节点），首段不足时可跨节点读取所需字节（如通过 <see cref="PacketHelper.ReadBytes(IPacket, Span{Byte})"/> 拼入栈缓冲）；
    /// 注意 pk.Total 是缓存总量而非帧长。返回完整帧长度（可能大于现有数据）；返回0或负数表示无法定界，等下一轮。
    /// </remarks>
    public Func<IPacket, Int32>? GetLength { get; set; }

    /// <summary>最后一次解包时间。每次加入数据时刷新，用于缓存过期判定</summary>
    public DateTime Last { get; set; } = DateTime.Now;

    /// <summary>缓存有效期。超过该时间后仍未匹配数据包的缓存数据将被抛弃，默认5000ms</summary>
    public Int32 Expire { get; set; } = 5_000;

    /// <summary>最大缓存待处理数据。默认1M</summary>
    public Int32 MaxCache { get; set; } = 1024 * 1024;

    /// <summary>APM性能追踪器</summary>
    public ITracer? Tracer { get; set; }

    /// <summary>获取帧长度的委托（兼容旧版，span 版本）</summary>
    /// <remarks>
    /// <para>为兼容基于 span 委托编译的旧版库（历史版本的 MQTT/RocketMQ/JT1078 等）而保留。</para>
    /// <para>单段缓存时直接传入片段；缓存为链式时拼接可用数据后传入，保持旧版“连续片段”语义；新代码请使用 <see cref="GetLength"/>。</para>
    /// </remarks>
    [Obsolete("请使用 GetLength，支持链式缓存；本属性仅兼容旧版二进制。")]
    public GetLengthDelegate? GetLength2 { get; set; }
    #endregion

    #region 待处理缓存
    /// <summary>缓存链头。跨轮保留的残段，每个节点持有自己的引用（拥有切片或借阅视图）</summary>
    private IPacket? _head;
    #endregion

    #region 构造
    /// <summary>释放资源</summary>
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
            // 归还缓存链持有的全部引用
            Release();
        }
    }
    #endregion

    #region 方法
    /// <summary>加入数据，分析数据流，得到一帧或多帧完整数据</summary>
    /// <remarks>
    /// <para>本轮数据整体追加进缓存后连续切帧。拥有句柄输入（接收链路）时，返回帧均为独立拥有切片/链，
    /// 可安全跨 await 逃逸，调用方负责 Dispose；借阅视图输入（<see cref="ArrayPacket"/> 等）时，
    /// 返回帧为视图，仅在本轮同步链路内有效。</para>
    /// </remarks>
    /// <param name="pk">本轮接收数据。接收链路为接收层每轮包装的拥有句柄；链式多段自动合并入缓存</param>
    /// <returns>完整帧列表（拥有句柄/链或借阅视图）</returns>
    public IList<IPacket> Parse(IPacket pk)
    {
        var list = new List<IPacket>();
        if (pk == null || pk.Total == 0) return list;

        var getLength = GetLength;
#pragma warning disable CS0618 // 兼容旧版二进制
        var getLength2 = GetLength2;
#pragma warning restore CS0618 // 兼容旧版二进制
        if (getLength == null && getLength2 == null) throw new ArgumentNullException(nameof(GetLength));

        lock (this)
        {
            CheckCache();

            // 本轮数据追加到缓存链尾（单段共享切片零拷贝；多段链拉直为自有单节点），再从头连续切帧
            var node = pk.Next == null ? pk.Slice(0, pk.Total) : pk.Clone();
            _head = _head == null ? node : _head.Append(node);

            Cut(getLength, getLength2, list);

            // 借阅视图的残片不能跨轮保留，转为自有拷贝（拥有句柄输入的残片本身就是自有的）
            EnsureOwned();
        }

        return list;
    }

    /// <summary>从缓存链头连续切出完整帧</summary>
    /// <param name="getLength">帧长计算委托（链感知）。可为空，为空时使用 <paramref name="getLength2"/></param>
    /// <param name="getLength2">帧长计算委托（span 版，兼容旧版）。可为空</param>
    /// <param name="list">帧列表</param>
    private void Cut(Func<IPacket, Int32>? getLength, GetLengthDelegate? getLength2, IList<IPacket> list)
    {
        while (_head != null)
        {
            // 定界失败（头部不足/分隔符未找到）等下一轮
            var len = HeadLength(getLength, getLength2, _head);
            if (len <= 0) return;

            var total = _head.Total;
            if (len > total) return;   // 帧未齐，等下一轮

            list.Add(CutFrame(len, total));
        }
    }

    /// <summary>计算链头帧长度。单段优先 span 委托（与旧版行为一致）；链式优先链感知委托，仅提供 span 委托时拼接可用数据保证连续</summary>
    /// <param name="getLength">链感知帧长委托</param>
    /// <param name="getLength2">span 版帧长委托</param>
    /// <param name="head">缓存链头</param>
    /// <returns>帧长度；返回0或负数表示数据不足</returns>
    private static Int32 HeadLength(Func<IPacket, Int32>? getLength, GetLengthDelegate? getLength2, IPacket head)
    {
        // 单段：优先 span 直读（零拷贝）
        if (head.Next == null)
            return getLength2 != null ? getLength2(head.GetSpan()) : getLength!(head);

        // 链式：优先链感知委托（零拷贝，可跨节点读帧头）
        if (getLength != null) return getLength(head);

        // 仅 span 委托：拼接可用数据后传入，保持旧版“连续片段”语义（该路径仅出现于跨轮残片，数据量已受限）
        var total = head.Total;
        var buf = ArrayPool<Byte>.Shared.Rent(total);
        try
        {
            var n = head.ReadBytes(buf);
            return getLength2!(buf.AsSpan(0, n));
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(buf);
        }
    }

    /// <summary>切出一帧，缓存窗口前移</summary>
    /// <param name="len">帧长度（不超过缓存总量）</param>
    /// <param name="total">缓存总长度</param>
    /// <returns>帧（共享切片/链）</returns>
    private IPacket CutFrame(Int32 len, Int32 total)
    {
        var head = _head!;

        // 帧覆盖整条缓存：直接转移句柄（零分配），缓存清空
        if (len == total)
        {
            _head = null;
            return head;
        }

        // 帧与剩余窗口各切一份共享切片（零拷贝），原句柄归还
        var frame = head.Slice(0, len);
        _head = head.Slice(len);
        head.TryDispose();

        return frame;
    }

    /// <summary>确保缓存跨轮安全：借阅视图的残片转为自有拷贝</summary>
    /// <remarks>
    /// <para>Slice 不改变所有权分类：拥有句柄切片后仍是自有（引用计数共享），视图切片后仍是借用；只有 Clone 无条件自有。</para>
    /// <para>拥有句柄输入（接收链路）时残片本身即自有，走一次找尾检查后直接返回、无拷贝；视图输入（直接调用）时仅未被切走的残片需要拷贝（完整帧保持零拷贝视图）。</para>
    /// <para>借阅节点只可能是链尾（本轮追加），链上其余节点均为自有；只需处理链尾</para>
    /// </remarks>
    private void EnsureOwned()
    {
        var node = _head;
        if (node == null) return;

        // 找链尾
        IPacket? prev = null;
        while (node.Next != null)
        {
            prev = node;
            node = node.Next;
        }

        if (!IsBorrowed(node)) return;

        // 只拷尾节点，链上其余节点保持不动
        var copy = node.Clone();
        if (prev == null)
            _head = copy;
        else
            prev.Next = copy;
    }

    /// <summary>归还整条缓存链并清空</summary>
    private void Release()
    {
        var node = _head;
        while (node != null)
        {
            var next = node.Next;
            node.Next = null;
            node.TryDispose();
            node = next;
        }

        _head = null;
    }

    /// <summary>判断节点是否借阅视图（不持有缓冲引用）</summary>
    /// <param name="node">缓存节点</param>
    /// <returns>是否借阅视图</returns>
    private static Boolean IsBorrowed(IPacket node) => node is not OwnerPacket op || op.RefCount == 0;

    /// <summary>检查缓存，超时或超大时清空</summary>
    protected virtual void CheckCache()
    {
        // 超过过期时间或超过最大缓存容量后废弃缓存数据
        var now = DateTime.Now;
        var retain = _head?.Total ?? 0;
        if (retain > 0 && (Last.AddMilliseconds(Expire) < now || MaxCache > 0 && MaxCache <= retain))
        {
            var hex = _head!.ToHex(64);

            using var span = Tracer?.NewSpan("net:PacketCodec:DropCache", $"[{retain}]{hex}", retain);
            span?.SetError(new Exception($"数据包编码器放弃数据 retain={retain} MaxCache={MaxCache}"), null);

            if (XTrace.Debug) XTrace.Log.Debug("数据包编码器放弃数据 {0:n0}，Last={1}，MaxCache={2:n0}", retain, Last, MaxCache);

            Release();
        }
        Last = now;
    }

    /// <summary>清空缓存</summary>
    public virtual void Clear() => Release();
    #endregion
}