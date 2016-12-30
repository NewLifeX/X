using System;
using System.IO;
using System.Text;

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

        /// <summary>返回字节数组。如果是完整数组直接返回，否则截取</summary>
        /// <remarks>不一定是全新数据，如果需要全新数据请克隆</remarks>
        /// <returns></returns>
        public virtual Byte[] ToArray()
        {
            if (Offset == 0 && (Count < 0 || Offset + Count == Data.Length)) return Data;

            return Data.ReadBytes(Offset, Count);
        }

        /// <summary>获取封包的数据流形式</summary>
        /// <returns></returns>
        public virtual Stream GetStream() { return new MemoryStream(Data, Offset, Count, false); }

        /// <summary>把封包写入到数据流</summary>
        /// <param name="stream"></param>
        public void WriteTo(Stream stream) { stream.Write(Data, Offset, Count); }

        /// <summary>深度克隆一份数据包，拷贝数据区</summary>
        /// <returns></returns>
        public Packet Clone()
        {
            return new Packet(Data.ReadBytes(Offset, Count));
        }

        /// <summary>以字符串表示</summary>
        /// <param name="encoding">字符串编码，默认URF-8</param>
        /// <returns></returns>
        public String ToStr(Encoding encoding = null)
        {
            if (Data == null) return null;
            if (Count == 0) return String.Empty;

            return Data.ToStr(encoding ?? Encoding.UTF8, Offset, Count);
        }

        /// <summary>以十六进制编码表示</summary>
        /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
        /// <param name="separate">分隔符</param>
        /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
        /// <returns></returns>
        public String ToHex(Int32 maxLength = 32, String separate = null, Int32 groupSize = 0)
        {
            if (Data == null) return null;
            if (Count == 0) return String.Empty;

            var buf = Data;
            if (Offset > 0 || Count > maxLength) buf = Data.ReadBytes(Offset, Math.Min(Count, maxLength));
            return buf.ToHex(separate, groupSize, buf.Length);
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