using System.Diagnostics.CodeAnalysis;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Messaging;

/// <summary>获取长度的委托</summary>
/// <param name="span">数据片段</param>
/// <returns>完整消息长度，返回0或负数表示数据不足</returns>
public delegate Int32 GetLengthDelegate(ReadOnlySpan<Byte> span);

/// <summary>数据包编码器。用于网络粘包处理</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/packet_codec
/// 
/// <para><b>设计目标</b>：作为网络粘包处理的基础实现，支持：</para>
/// <list type="bullet">
/// <item><description>快速路径：无缓存时直接解析完整帧，零拷贝</description></item>
/// <item><description>慢速路径：有缓存时合并数据后解析，自动管理缓存生命周期</description></item>
/// <item><description>超大包：支持超过64k的数据包</description></item>
/// </list>
/// 
/// <para><b>使用方式</b>：</para>
/// <code>
/// var codec = new PacketCodec { GetLength2 = DefaultMessage.GetLength };
/// foreach (var pk in codec.Parse(receivedData))
/// {
///     // 处理完整的数据包
/// }
/// </code>
/// 
/// <para><b>线程安全</b>：单个实例不是线程安全的，每个连接应使用独立的编码器实例。</para>
/// </remarks>
public class PacketCodec : IDisposable
{
    #region 属性
    /// <summary>缓存流。用于存储不完整的数据包</summary>
    public MemoryStream? Stream { get; set; }

    /// <summary>获取长度的委托。本包所应该拥有的总长度，满足该长度后解除一个封包</summary>
    [Obsolete("请使用 GetLength2，性能更优")]
    public Func<IPacket, Int32>? GetLength { get; set; }

    /// <summary>获取长度的委托。本包所应该拥有的总长度，满足该长度后解除一个封包</summary>
    public GetLengthDelegate? GetLength2 { get; set; }

    /// <summary>最后一次解包成功时间，而不是最后一次接收时间</summary>
    public DateTime Last { get; set; } = DateTime.Now;

    /// <summary>缓存有效期。超过该时间后仍未匹配数据包的缓存数据将被抛弃，默认5000ms</summary>
    public Int32 Expire { get; set; } = 5_000;

    /// <summary>最大缓存待处理数据。默认1M</summary>
    public Int32 MaxCache { get; set; } = 1024 * 1024;

    /// <summary>APM性能追踪器</summary>
    public ITracer? Tracer { get; set; }
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
            Stream?.Dispose();
            Stream = null;
        }
    }
    #endregion

    #region 方法
    /// <summary>数据包加入缓存数据末尾，分析数据流，得到一帧或多帧数据</summary>
    /// <param name="pk">待分析数据包</param>
    /// <returns>解析出的完整数据包列表</returns>
    public virtual IList<IPacket> Parse(IPacket pk)
    {
        var ms = Stream;
        var nodata = ms == null || ms.Position < 0 || ms.Position >= ms.Length;

#pragma warning disable CS0618 // 类型或成员已过时
        var func = GetLength;
#pragma warning restore CS0618 // 类型或成员已过时
        var func2 = GetLength2;
        if (func == null && func2 == null) throw new ArgumentNullException(nameof(GetLength2));

        var list = new List<IPacket>();
        // 内部缓存没有数据，直接判断输入数据流是否刚好一帧数据，快速处理，绝大多数是这种场景
        if (nodata)
        {
            if (pk == null || pk.Total == 0) return list.ToArray();

            //using var span = Tracer?.NewSpan("net:PacketCodec:NoCache", pk.Total + "");

            var idx = 0;
            while (idx < pk.Total)
            {
                // 基于Span的性能更优，但是它不支持链式包。网络接收中几乎不存在链式包
                var len = 0;
                if (func2 != null && pk.Next == null)
                {
                    // 切出来一片，计算长度
                    var span = pk.GetSpan().Slice(idx);
                    len = func2(span);
                    if (len <= 0 || len > span.Length) break;
                }
                else
                {
                    // 切出来一片，计算长度
                    var pk2 = pk.Slice(idx, -1, false);
                    len = func!(pk2);
                    if (len <= 0 || len > pk2.Total) break;
                }

                // 根据计算得到的长度，重新设置数据片正确长度
                list.Add(pk.Slice(idx, len, false));
                idx += len;
            }
            // 如果没有剩余，可以返回
            if (idx == pk.Total) return list.ToArray();

            // 剩下的
            pk = pk.Slice(idx, -1, false);
        }

        // 加锁，避免多线程冲突
        lock (this)
        {
            // 检查缓存，内部可能创建或清空
            CheckCache();
            ms = Stream;

            using var span = Tracer?.NewSpan("net:PacketCodec:MergeCache", $"Position={ms.Position} Length={ms.Length} NewData=[{pk.Length}]{pk.ToHex(500)}", pk.Length);

            // 合并数据到最后面
            if (pk != null && pk.Total > 0)
            {
                var p = ms.Position;
                ms.Position = ms.Length;
                pk.CopyTo(ms);
                ms.Position = p;
            }

            // 尝试解包
            while (ms.Position < ms.Length)
            {
                // 该方案在NET40/NET45上会导致拷贝大量数据，而读取包头长度没必要拷贝那么多数据，不划算
                var pk2 = new ArrayPacket(ms);
                // 这里可以肯定能够窃取内部缓冲区
                //var pk2 = new ArrayPacket(ms.GetBuffer(), (Int32)ms.Position, (Int32)(ms.Length - ms.Position));
                var len = func2 != null ? func2(pk2.GetSpan()) : func!(pk2);
                if (len <= 0 || len > pk2.Total) break;

                // 根据计算得到的长度，重新设置数据片正确长度
                //pk2.Set(pk2.Data, pk2.Offset, Offset + len);
                //pk2 = new ArrayPacket(pk2.Buffer, pk2.Offset, pk2.Offset + len);
                list.Add(pk2.Slice(0, len));

                ms.Seek(len, SeekOrigin.Current);
            }

            // 如果读完了数据，需要重置缓冲区
            if (ms.Position >= ms.Length)
            {
                ms.SetLength(0);
                ms.Position = 0;
            }

            //// 记录最后一次解包成功时间，以此作为过期依据，避免收到错误分片后，持续的新片而不能过期
            //if (list.Count > 0) Last = TimerX.Now;

            return list;
        }
    }

    /// <summary>检查缓存，超时或超大时清空</summary>
    [MemberNotNull(nameof(Stream))]
    protected virtual void CheckCache()
    {
        var ms = Stream ??= new MemoryStream();

        // 超过过期时间或超过最大缓存容量后废弃缓存数据
        var now = DateTime.Now;
        var retain = ms.Length - ms.Position;
        if (retain > 0 && (Last.AddMilliseconds(Expire) < now || MaxCache > 0 && MaxCache <= retain))
        {
            //var buf = ms.ReadBytes(retain > 64 ? 64 : retain);
            var buf = Pool.Shared.Rent((Int32)(retain > 64 ? 64 : retain));
            var count = ms.Read(buf, 0, buf.Length);
            ms.Seek(-count, SeekOrigin.Current);
            var hex = buf.ToHex(0, count);
            Pool.Shared.Return(buf);

            using var span = Tracer?.NewSpan("net:PacketCodec:DropCache", $"[{retain}]{hex}", retain);
            span?.SetError(new Exception($"数据包编码器放弃数据 retain={retain} MaxCache={MaxCache}"), null);

            if (XTrace.Debug) XTrace.Log.Debug("数据包编码器放弃数据 {0:n0}，Last={1}，MaxCache={2:n0}", retain, Last, MaxCache);

            // 如果较大则重新分配内存，避免内存碎片
            if (ms.Capacity > 1024)
                ms = Stream = new MemoryStream();
            else
            {
                ms.SetLength(0);
                ms.Position = 0;
            }
        }
        Last = now;
    }

    /// <summary>清空缓存</summary>
    public virtual void Clear()
    {
        var ms = Stream;
        if (ms != null)
        {
            ms.SetLength(0);
            ms.Position = 0;
        }
    }
    #endregion
}