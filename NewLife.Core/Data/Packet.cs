using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Data
{
    /// <summary>封包</summary>
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
        public Int32 Total { get { return Count + (Next != null ? Next.Total : 0); } }
        #endregion

        #region 构造
        /// <summary>根据数据区实例化</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public Packet(Byte[] data, Int32 offset = 0, Int32 count = -1) { Set(data, offset, count); }
        #endregion

        #region 索引
        /// <summary>获取/设置 指定位置的字节</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Byte this[Int32 index]
        {
            get { return Data[Offset + index]; }
            set { Data[Offset + index] = value; }
        }
        #endregion

        #region 方法
        /// <summary>设置新的数据区</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public virtual void Set(Byte[] data, Int32 offset = 0, Int32 count = -1)
        {
            Data = data;

            if (data == null)
            {
                Offset = 0;
                count = 0;
            }
            else
            {
                Offset = offset;

                if (count < 0) count = data.Length - offset;
                Count = count;
            }
        }

        /// <summary>截取</summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Packet Sub(Int32 offset, Int32 count = -1)
        {
            offset += Offset;

            if (count < 0 || offset + count > Count) count = Count - offset;

            if (count < 0) count = 0;

            return new Packet(Data, offset, count);
        }

        /// <summary>返回字节数组。如果是完整数组直接返回，否则截取</summary>
        /// <remarks>不一定是全新数据，如果需要全新数据请克隆</remarks>
        /// <returns></returns>
        public virtual Byte[] ToArray()
        {
            if (Offset == 0 && (Count < 0 || Offset + Count == Data.Length) && Next == null) return Data;

            if (Next == null) Data.ReadBytes(Offset, Count);

            // 链式包输出
            var ms = new MemoryStream();
            WriteTo(ms);

            return ms.ToArray();
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
            var ms = new MemoryStream();

            // 遍历
            var cur = this;
            while (cur != null && count > 0)
            {
                var len = cur.Count;
                // 当前包不够用
                if (len < offset)
                    offset -= len;
                else
                {
                    len -= offset;
                    if (len > count) len = count;
                    ms.Write(cur.Data, cur.Offset + offset, len);

                    offset = 0;
                    count -= len;
                }

                cur = cur.Next;
            }
            return ms.ToArray();

            //// 以上算法太复杂，直接来
            //return ToArray().ReadBytes(offset, count);
        }

        /// <summary>获取封包的数据流形式</summary>
        /// <returns></returns>
        public virtual Stream GetStream()
        {
            if (Next == null) return new MemoryStream(Data, Offset, Count, false);

            var ms = new MemoryStream();
            WriteTo(ms);
            ms.Position = 0;

            return ms;
        }

        /// <summary>把封包写入到数据流</summary>
        /// <param name="stream"></param>
        public void WriteTo(Stream stream)
        {
            stream.Write(Data, Offset, Count);
            Next?.WriteTo(stream);
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
        /// <returns></returns>
        public String ToStr(Encoding encoding = null)
        {
            if (Data == null) return null;
            //if (Count == 0) return String.Empty;

            if (Next == null) return Data.ToStr(encoding ?? Encoding.UTF8, Offset, Count);

            return ToArray().ToStr(encoding ?? Encoding.UTF8);
        }

        /// <summary>以十六进制编码表示</summary>
        /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
        /// <param name="separate">分隔符</param>
        /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
        /// <returns></returns>
        public String ToHex(Int32 maxLength = 32, String separate = null, Int32 groupSize = 0)
        {
            if (Data == null) return null;
            //if (Count == 0) return String.Empty;

            //var len = Math.Min(Count, maxLength);
            //var buf = Data;
            //if (Offset > 0) buf = Data.ReadBytes(Offset, len);
            //return buf.ToHex(separate, groupSize, len);

            return ReadBytes(0, maxLength).ToHex(separate, groupSize);
        }
        #endregion

        #region 重载运算符
        /// <summary>重载类型转换，字节数组直接转为Packet对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator Packet(Byte[] value)
        {
            return new Packet(value);
        }
        #endregion
    }
}