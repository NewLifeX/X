using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Collections;

namespace NewLife.Data;

/// <summary>数据包。表示数据区Data的指定范围（Offset, Count）。</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/packet
/// 设计于.NET2.0时代，功能上类似于NETCore的Span/Memory。
/// Packet的设计目标就是网络库零拷贝，所以Slice切片是其最重要功能。
/// </remarks>
public class Packet : IPacket
{
    #region 属性
    /// <summary>数据</summary>
    public Byte[] Data { get; private set; }

    /// <summary>偏移</summary>
    public Int32 Offset { get; private set; }

    /// <summary>长度</summary>
    public Int32 Count { get; private set; }

    Int32 IPacket.Length => Count;

    /// <summary>下一个链式包</summary>
    public Packet? Next { get; set; }

    /// <summary>总长度</summary>
    public Int32 Total => Count + (Next != null ? Next.Total : 0);

    IPacket? IPacket.Next { get => Next; set => Next = (value as Packet) ?? throw new InvalidDataException(); }
    #endregion

    #region 构造
    /// <summary>根据数据区实例化</summary>
    /// <param name="data"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public Packet(Byte[] data, Int32 offset = 0, Int32 count = -1) => Set(data, offset, count);

    /// <summary>根据数组段实例化</summary>
    /// <param name="seg"></param>
    public Packet(ArraySegment<Byte> seg)
    {
        if (seg.Array == null) throw new ArgumentNullException(nameof(seg));

        Set(seg.Array, seg.Offset, seg.Count);
    }

    /// <summary>从可扩展内存流实例化，尝试窃取内存流内部的字节数组，失败后拷贝</summary>
    /// <remarks>因数据包内数组窃取自内存流，需要特别小心，避免多线程共用。常用于内存流转数据包，而内存流不再使用</remarks>
    /// <param name="stream"></param>
    public Packet(Stream stream)
    {
        if (stream is MemoryStream ms)
        {
#if !NET45
            // 尝试抠了内部存储区，下面代码需要.Net 4.6支持
            if (ms.TryGetBuffer(out var seg))
            {
                if (seg.Array == null) throw new ArgumentNullException(nameof(seg));

                Set(seg.Array, seg.Offset + (Int32)ms.Position, seg.Count - (Int32)ms.Position);
                return;
            }
            // GetBuffer窃取内部缓冲区后，无法得知真正的起始位置index，可能导致错误取数
            // public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)

            //try
            //{
            //    Set(ms.GetBuffer(), (Int32)ms.Position, (Int32)(ms.Length - ms.Position));
            //}
            //catch (UnauthorizedAccessException) { }
#endif
        }

        //Set(stream.ToArray());

        var buf = new Byte[stream.Length - stream.Position];
        var count = stream.Read(buf, 0, buf.Length);
        Set(buf, 0, count);

        // 必须确保数据流位置不变
        if (count > 0) stream.Seek(-count, SeekOrigin.Current);
    }

    /// <summary>从Span实例化</summary>
    /// <param name="span"></param>
    public Packet(Span<Byte> span) => Set(span.ToArray());

    /// <summary>从Memory实例化</summary>
    /// <param name="memory"></param>
    public Packet(Memory<Byte> memory)
    {
        if (MemoryMarshal.TryGetArray<Byte>(memory, out var segment))
        {
            Set(segment.Array!, segment.Offset, segment.Count);
        }
        else
        {
            Set(memory.ToArray());
        }
    }
    #endregion

    #region 索引
    /// <summary>获取/设置 指定位置的字节</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Byte this[Int32 index]
    {
        get
        {
            // 超过下标直接报错,谁也不想处理了异常的数据也不知道
            if (index < 0) throw new IndexOutOfRangeException($"Index [{index}] is out of bounds");

            var p = Offset + index;
            if (p >= Offset + Count)
            {
                if (Next == null) throw new IndexOutOfRangeException($"Index [{index}] is out of bounds [>{Total - 1}]");

                return Next[index - Count];
            }

            return Data[p];

            // Offset 至 Offset+Count 代表了当前链的可用数据区
            // Count 是当前链的实际可用数据长度,(而用 Data.Length 是不准确的,Data的数据不是全部可用),
            // 所以  这里通过索引取整个链表的索引数据应该用 Count 作运算.              
        }
        set
        {
            if (index < 0) throw new IndexOutOfRangeException($"Index [{index}] is out of bounds");

            // 设置 对应索引 的数据 应该也是针对整个链表的有效数据区
            var p = Offset + index;
            if (index >= Count)
            {
                if (Next == null) throw new IndexOutOfRangeException($"Index [{index}] is out of bounds [>{Total - 1}]");

                Next[p - Data.Length] = value;
            }
            else
            {
                Data[p] = value;
            }

            // 基础类需要严谨给出明确功用，不能模棱两可，因此不能越界
        }
    }
    #endregion

    #region 方法
    /// <summary>设置新的数据区</summary>
    /// <param name="data">数据区</param>
    /// <param name="offset">偏移</param>
    /// <param name="count">字节个数</param>
    [MemberNotNull(nameof(Data))]
    public virtual void Set(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        Data = data;

        if (data == null)
        {
            Offset = 0;
            Count = 0;
        }
        else
        {
            Offset = offset;

            if (count < 0) count = data.Length - offset;
            Count = count;
        }
    }

    /// <summary>截取子数据区</summary>
    /// <param name="offset">相对偏移</param>
    /// <param name="count">字节个数</param>
    /// <returns></returns>
    public Packet Slice(Int32 offset, Int32 count = -1)
    {
        var start = Offset + offset;
        var remain = Count - offset;

        if (Next == null)
        {
            // count 是 offset 之后的个数
            if (count < 0 || count > remain) count = remain;
            if (count < 0) count = 0;

            return new Packet(Data, start, count);
        }
        else
        {
            // 如果当前段用完，则取下一段
            if (remain <= 0) return Next.Slice(offset - Count, count);

            // 当前包用一截，剩下的全部
            if (count < 0) return new Packet(Data, start, remain) { Next = Next };

            // 当前包可以读完
            if (count <= remain) return new Packet(Data, start, count);

            // 当前包用一截，剩下的再截取
            return new Packet(Data, start, remain) { Next = Next.Slice(0, count - remain) };
        }
    }

    IPacket IPacket.Slice(Int32 offset, Int32 count) => Slice(offset, count);

    /// <summary>查找目标数组</summary>
    /// <param name="data">目标数组</param>
    /// <param name="offset">本数组起始偏移</param>
    /// <param name="count">本数组搜索个数</param>
    /// <returns></returns>
    public Int32 IndexOf(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        var start = offset;
        var length = data.Length;

        if (count < 0 || count > Total - offset) count = Total - offset;

        // 快速查找
        if (Next == null)
        {
            if (start >= Count) return -1;

            //#if NETCOREAPP3_1_OR_GREATER
            //                var s1 = new Span<Byte>(Data, Offset + offset, count);
            //                var p = s1.IndexOf(data);
            //                return p >= 0 ? (p + offset) : -1;
            //#endif
            var p = Data.IndexOf(data, Offset + start, count);
            return p >= 0 ? (p - Offset) : -1;
        }

        // 已匹配字节数
        var win = 0;
        // 索引加上data剩余字节数必须小于count，否则就是已匹配
        for (var i = 0; i + length - win <= count; i++)
        {
            if (this[start + i] == data[win])
            {
                win++;

                // 全部匹配，退出
                if (win >= length) return (start + i) - length + 1;
            }
            else
            {
                //win = 0; // 只要有一个不匹配，马上清零
                // 不能直接清零，那样会导致数据丢失，需要逐位探测，窗口一个个字节滑动
                i -= win;
                win = 0;

                // 本段分析未匹配，递归下一段
                if (start + i == Count && Next != null)
                {
                    var p = Next.IndexOf(data, 0, count - i);
                    if (p >= 0) return (start + i) + p;

                    break;
                }
            }
        }

        return -1;
    }

    /// <summary>附加一个包到当前包链的末尾</summary>
    /// <param name="pk"></param>
    public Packet Append(Packet pk)
    {
        if (pk == null) return this;

        var p = this;
        while (p.Next != null) p = p.Next;
        p.Next = pk;

        return this;
    }

    /// <summary>返回字节数组。无差别复制，一定返回新数组</summary>
    /// <returns></returns>
    public virtual Byte[] ToArray()
    {
        //if (Offset == 0 && (Count < 0 || Offset + Count == Data.Length) && Next == null) return Data;

        if (Next == null) return Data.ReadBytes(Offset, Count);

        // 链式包输出
        var ms = Pool.MemoryStream.Get();
        CopyTo(ms);

        return ms.Return(true);
    }

    /// <summary>从封包中读取指定数据区，读取全部时直接返回缓冲区，以提升性能</summary>
    /// <param name="offset">相对于数据包的起始位置，实际上是数组的Offset+offset</param>
    /// <param name="count">字节个数</param>
    /// <returns></returns>
    public Byte[] ReadBytes(Int32 offset = 0, Int32 count = -1)
    {
        // 读取全部
        if (offset == 0 && count < 0)
        {
            if (Offset == 0 && (Count < 0 || Offset + Count == Data.Length) && Next == null) return Data;

            return ToArray();
        }

        if (Next == null) return Data.ReadBytes(Offset + offset, count < 0 || count > Count ? Count : count);

        // 当前包足够长
        if (count >= 0 && offset + count <= Count) return Data.ReadBytes(Offset + offset, count);

        // 链式包输出
        if (count < 0) count = Total - offset;
        var ms = Pool.MemoryStream.Get();

        // 遍历
        var cur = this;
        while (cur != null && count > 0)
        {
            var len = cur.Count;
            // 当前包不够用
            if (len < offset)
                offset -= len;
            else if (cur.Data != null)
            {
                len -= offset;
                if (len > count) len = count;
                ms.Write(cur.Data, cur.Offset + offset, len);

                offset = 0;
                count -= len;
            }

            cur = cur.Next;
        }
        return ms.Return(true);

        //// 以上算法太复杂，直接来
        //return ToArray().ReadBytes(offset, count);
    }

    /// <summary>返回数据段</summary>
    /// <returns></returns>
    public ArraySegment<Byte> ToSegment()
    {
        if (Next == null) return new ArraySegment<Byte>(Data, Offset, Count);

        return new ArraySegment<Byte>(ToArray());
    }

    /// <summary>返回数据段集合</summary>
    /// <returns></returns>
    public IList<ArraySegment<Byte>> ToSegments()
    {
        // 初始4元素，优化扩容
        var list = new List<ArraySegment<Byte>>(4);

        for (var pk = this; pk != null; pk = pk.Next)
        {
            list.Add(new ArraySegment<Byte>(pk.Data, pk.Offset, pk.Count));
        }

        return list;
    }

    /// <summary>转为Span</summary>
    /// <returns></returns>
    public Span<Byte> AsSpan()
    {
        if (Next == null) return new Span<Byte>(Data, Offset, Count);

        return new Span<Byte>(ToArray());
    }

    /// <summary>转为Memory</summary>
    /// <returns></returns>
    public Memory<Byte> AsMemory()
    {
        if (Next == null) return new Memory<Byte>(Data, Offset, Count);

        return new Memory<Byte>(ToArray());
    }

    Span<Byte> IPacket.GetSpan() => AsSpan();
    Memory<Byte> IPacket.GetMemory() => AsMemory();

    /// <summary>获取封包的数据流形式</summary>
    /// <returns></returns>
    public virtual MemoryStream GetStream()
    {
        if (Next == null) return new MemoryStream(Data, Offset, Count, false, true);

        var ms = new MemoryStream();
        CopyTo(ms);
        ms.Position = 0;

        return ms;
    }

    /// <summary>把封包写入到数据流</summary>
    /// <param name="stream"></param>
    public void CopyTo(Stream stream)
    {
        stream.Write(Data, Offset, Count);
        Next?.CopyTo(stream);
    }

    /// <summary>把封包写入到目标数组</summary>
    /// <param name="buffer">目标数组</param>
    /// <param name="offset">目标数组的偏移量</param>
    /// <param name="count">目标数组的字节数</param>
    public void WriteTo(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
    {
        if (count < 0) count = Total;
        var len = count;
        if (len > Count) len = Count;
        Buffer.BlockCopy(Data, Offset, buffer, offset, len);

        offset += len;
        count -= len;
        if (count > 0) Next?.WriteTo(buffer, offset, count);
    }

    /// <summary>异步复制到目标数据流</summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public async Task CopyToAsync(Stream stream)
    {
        await stream.WriteAsync(Data, Offset, Count);
        if (Next != null) await Next.CopyToAsync(stream);
    }

    /// <summary>异步复制到目标数据流</summary>
    /// <param name="stream"></param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public async Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(Data, Offset, Count, cancellationToken);
        if (Next != null) await Next.CopyToAsync(stream, cancellationToken);
    }

    /// <summary>深度克隆一份数据包，拷贝数据区</summary>
    /// <returns></returns>
    public Packet Clone()
    {
        if (Next == null) return new Packet(Data.ReadBytes(Offset, Count));

        // 链式包输出
        var ms = Pool.MemoryStream.Get();
        CopyTo(ms);

        return new Packet(ms.Return(true));
    }

    /// <summary>以字符串表示</summary>
    /// <param name="encoding">字符串编码，默认URF-8</param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public String ToStr(Encoding? encoding = null, Int32 offset = 0, Int32 count = -1)
    {
        if (Data == null) return String.Empty;

        encoding ??= Encoding.UTF8;
        if (count < 0) count = Total - offset;

        if (Next == null) return Data.ToStr(encoding, Offset + offset, count);

        return ReadBytes(offset, count).ToStr(encoding);
    }

    /// <summary>以十六进制编码表示</summary>
    /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
    /// <param name="separate">分隔符</param>
    /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
    /// <returns></returns>
    public String ToHex(Int32 maxLength = 32, String? separate = null, Int32 groupSize = 0)
    {
        if (Data == null) return String.Empty;

        var hex = ReadBytes(0, maxLength).ToHex(separate, groupSize);

        return (maxLength == -1 || Count <= maxLength) ? hex : String.Concat(hex, "...");
    }

    /// <summary>转为Base64编码</summary>
    /// <returns></returns>
    public String ToBase64()
    {
        if (Data == null) return String.Empty;

        if (Next == null) Data.ToBase64(Offset, Count);

        return ToArray().ToBase64();
    }

    /// <summary>读取无符号短整数</summary>
    /// <param name="isLittleEndian"></param>
    /// <returns></returns>
    public UInt16 ReadUInt16(Boolean isLittleEndian = true) => Data.ToUInt16(Offset, isLittleEndian);

    /// <summary>读取无符号整数</summary>
    /// <param name="isLittleEndian"></param>
    /// <returns></returns>
    public UInt32 ReadUInt32(Boolean isLittleEndian = true) => Data.ToUInt32(Offset, isLittleEndian);
    #endregion

    #region 重载运算符
    /// <summary>重载类型转换，字节数组直接转为Packet对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator Packet(Byte[] value) => value == null ? null! : new(value);

    /// <summary>重载类型转换，一维数组直接转为Packet对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator Packet(ArraySegment<Byte> value) => new(value);

    /// <summary>重载类型转换，字符串直接转为Packet对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator Packet(String value) => new(value.GetBytes());

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override String ToString() => $"[{Data.Length}]({Offset}, {Count})" + (Next == null ? "" : $"<{Total}>");
    #endregion
}