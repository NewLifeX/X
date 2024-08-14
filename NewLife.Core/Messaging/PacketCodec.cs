using System.Diagnostics.CodeAnalysis;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Messaging;

/// <summary>数据包编码器</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/packet_codec
/// 编码器的设计目标是作为网络粘包处理的基础实现。
/// </remarks>
public class PacketCodec
{
    #region 属性
    /// <summary>缓存流</summary>
    public MemoryStream? Stream { get; set; }

    /// <summary>获取长度的委托。本包所应该拥有的总长度，满足该长度后解除一个封包</summary>
    public Func<Packet, Int32>? GetLength { get; set; }

    /// <summary>长度的偏移量，截取数据包时加上，否则将会漏掉长度之间的数据包，如MQTT</summary>
    public Int32 Offset { get; set; }

    /// <summary>最后一次解包成功，而不是最后一次接收</summary>
    public DateTime Last { get; set; } = DateTime.Now;

    /// <summary>缓存有效期。超过该时间后仍未匹配数据包的缓存数据将被抛弃，默认5000ms</summary>
    public Int32 Expire { get; set; } = 5_000;

    /// <summary>最大缓存待处理数据。默认1M</summary>
    public Int32 MaxCache { get; set; } = 1024 * 1024;

    /// <summary>APM性能追踪器</summary>
    public ITracer? Tracer { get; set; }
    #endregion

    /// <summary>数据包加入缓存数据末尾，分析数据流，得到一帧或多帧数据</summary>
    /// <param name="pk">待分析数据包</param>
    /// <returns></returns>
    public virtual IList<Packet> Parse(Packet pk)
    {
        var ms = Stream;
        var nodata = ms == null || ms.Position < 0 || ms.Position >= ms.Length;

        var func = GetLength ?? throw new ArgumentNullException(nameof(GetLength));

        var list = new List<Packet>();
        // 内部缓存没有数据，直接判断输入数据流是否刚好一帧数据，快速处理，绝大多数是这种场景
        if (nodata)
        {
            if (pk == null || pk.Total == 0) return list.ToArray();

            //using var span = Tracer?.NewSpan("net:PacketCodec:NoCache", pk.Total + "");

            var idx = 0;
            while (idx < pk.Total)
            {
                // 切出来一片，计算长度
                var pk2 = pk.Slice(idx);
                var len = func(pk2);
                if (len <= 0 || len > pk2.Total) break;

                // 根据计算得到的长度，重新设置数据片正确长度
                pk2.Set(pk2.Data, pk2.Offset, Offset + len);
                list.Add(pk2);
                idx += Offset + len;
            }
            // 如果没有剩余，可以返回
            if (idx == pk.Total) return list.ToArray();

            // 剩下的
            pk = pk.Slice(idx);
        }

        // 加锁，避免多线程冲突
        lock (this)
        {
            // 检查缓存，内部可能创建或清空
            CheckCache();
            ms = Stream;

            using var span = Tracer?.NewSpan("net:PacketCodec:MergeCache", $"Position={ms.Position} Length={ms.Length} NewData=[{pk.Total}]{pk.ToHex(500)}", pk.Total);

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
                //var pk2 = new Packet(ms);
                // 这里可以肯定能够窃取内部缓冲区
                var pk2 = new Packet(ms.GetBuffer(), (Int32)ms.Position, (Int32)(ms.Length - ms.Position));
                var len = func(pk2);
                if (len <= 0 || len > pk2.Total) break;

                // 根据计算得到的长度，重新设置数据片正确长度
                pk2.Set(pk2.Data, pk2.Offset, Offset + len);
                list.Add(pk2);

                ms.Seek(Offset + len, SeekOrigin.Current);
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

    /// <summary>检查缓存</summary>
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
}