﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NewLife.Collections;

namespace NewLife.Data
{
    /// <summary>数据包</summary>
    public class Packet
    {
        #region 属性
        /// <summary>数据</summary>
        public Byte[] Data { get; private set; }

        /// <summary>偏移</summary>
        public Int32 Offset { get; private set; }

        /// <summary>长度</summary>
        public Int32 Count { get; private set; }

        /// <summary>下一个链式包</summary>
        public Packet Next { get; set; }

        /// <summary>总长度</summary>
        public Int32 Total => Count + (Next != null ? Next.Total : 0);
        #endregion

        #region 构造
        /// <summary>根据数据区实例化</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public Packet(Byte[] data, Int32 offset = 0, Int32 count = -1) => Set(data, offset, count);

        /// <summary>根据数组段实例化</summary>
        /// <param name="seg"></param>
        public Packet(ArraySegment<Byte> seg) => Set(seg.Array, seg.Offset, seg.Count);

        /// <summary>从可扩展内存流实例化，尝试窃取内存流内部的字节数组，失败后拷贝</summary>
        /// <remarks>因数据包内数组窃取自内存流，需要特别小心，避免多线程共用</remarks>
        /// <param name="stream"></param>
        public Packet(Stream stream)
        {
            if (stream is MemoryStream ms)
            {
                try
                {
#if NET46
                    // 尝试抠了内部存储区，下面代码需要.Net 4.6支持
                    if (stream.TryGetBuffer(out var seg))
                        Set(seg.Array, seg.Offset, seg.Count);
                    else
#endif
                    Set(ms.GetBuffer(), (Int32)ms.Position, (Int32)(ms.Length - ms.Position));

                    return;
                }
                catch (UnauthorizedAccessException) { }
            }

            Set(stream.ToArray());
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
                if (index >= Total || index < 0)
                {  //超过下标直接报错,谁也不想处理了异常的数据也不知道
                    throw new IndexOutOfRangeException($"超出Packet 索引范围 获取的索引{index} Packet最大索引{Total - 1}");
                }
                var p = Offset + index;//读取位置
                var can = Offset + Count;//实际可用位置
                if (p >= can && Next != null) return Next[index - Count];
                return Data[p];

                // Offset 至 Offset+Count 代表了当前链的可用数据区
                // Count 是当前链的实际可用数据长度,(而用 Data.Length 是不准确的,Data的数据不是全部可用),
                // 所以  这里通过索引取整个链表的索引数据应该用 Count 作运算.              
            }
            set
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException($"{index} 索引参数不在Packet索引给定的范围内");
                }
                //设置 对应索引 的数据 应该也是针对整个链表的有效数据区
                var p = Offset + index;
                var can = Offset + Count;//实际可用位置
                if (p < can)
                {
                    Data[p] = value;
                }
                else if (Next != null)
                {
                    Next[index - Count] = value;
                }
                else
                {
                    //throw new IndexOutOfRangeException();//超出索引下标报错
                    Byte[] b;// 或新建一个Pakcet 继续延申数据链,我觉得选择延时比较好,有时候写代码可以偷懒
                    b = new Byte[index - Count + 1];
                    var pk = new Packet(b);
                    Next = pk;
                    Next[index - Count] = value;

                }

            }
            set
            {
                var p = Offset + index;
                if (p >= Data.Length && Next != null)
                    Next[p - Data.Length] = value;
                else
                    Data[p] = value;
            }
        }
        #endregion

        #region 方法
        /// <summary>设置新的数据区</summary>
        /// <param name="data">数据区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">字节个数</param>
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
            // 已匹配字节数
            var win = 0;
            // 索引加上data剩余字节数必须小于count
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
                }
            }

            return -1;
        }

        /// <summary>附加一个包到当前包链的末尾</summary>
        /// <param name="pk"></param>
        public void Append(Packet pk)
        {
            if (pk == null) return;

            var p = this;
            while (p.Next != null) p = p.Next;
            p.Next = pk;
        }

        /// <summary>返回字节数组。如果是完整数组直接返回，否则截取</summary>
        /// <remarks>不一定是全新数据，如果需要全新数据请克隆</remarks>
        /// <returns></returns>
        public virtual Byte[] ToArray()
        {
            if (Offset == 0 && (Count < 0 || Offset + Count == Data.Length) && Next == null) return Data;

            if (Next == null) Data.ReadBytes(Offset, Count);

            // 链式包输出
            var ms = Pool.MemoryStream.Get();
            CopyTo(ms);

            return ms.Put(true);
        }

        /// <summary>从封包中读取指定数据</summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Byte[] ReadBytes(Int32 offset = 0, Int32 count = -1)
        {
            if (offset == 0 && count < 0) return ToArray();

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
            return ms.Put(true);

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

        /// <summary>获取封包的数据流形式</summary>
        /// <returns></returns>
        public virtual MemoryStream GetStream()
        {
            //if (Next == null) return new MemoryStream(Data, Offset, Count, false);
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
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
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

        /// <summary>深度克隆一份数据包，拷贝数据区</summary>
        /// <returns></returns>
        public Packet Clone()
        {
            if (Next == null) return new Packet(Data.ReadBytes(Offset, Count));

            return new Packet(ToArray());
        }

        /// <summary>以字符串表示</summary>
        /// <param name="encoding">字符串编码，默认URF-8</param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public String ToStr(Encoding encoding = null, Int32 offset = 0, Int32 count = -1)
        {
            if (Data == null) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            if (count < 0) count = Total - offset;

            if (Next == null) return Data.ToStr(encoding, Offset + offset, count);

            return ReadBytes(offset, count).ToStr(encoding);
        }

        /// <summary>以十六进制编码表示</summary>
        /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
        /// <param name="separate">分隔符</param>
        /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
        /// <returns></returns>
        public String ToHex(Int32 maxLength = 32, String separate = null, Int32 groupSize = 0)
        {
            if (Data == null) return null;

            return ReadBytes(0, maxLength).ToHex(separate, groupSize);
        }
        #endregion

        #region 重载运算符
        /// <summary>重载类型转换，字节数组直接转为Packet对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator Packet(Byte[] value) => value == null ? null : new Packet(value);

        /// <summary>重载类型转换，一维数组直接转为Packet对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator Packet(ArraySegment<Byte> value) => new Packet(value);

        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString() => $"[{Data.Length}]({Offset}, {Count})" + (Next == null ? "" : $"<{Total}>");
        #endregion
    }
}