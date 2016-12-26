using System;
using System.IO;

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

        #region 方法
        /// <summary>设置新的数据区</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public virtual void Set(Byte[] data, Int32 offset = 0, Int32 count = -1)
        {
            Data = data;
            Offset = offset;

            if (count < 0 && data != null) count = data.Length - offset;
            Count = count;
        }

        /// <summary>返回字节数组。如果是完整数组直接返回，否则截取</summary>
        /// <returns></returns>
        public virtual Byte[] Get()
        {
            if (Offset == 0 && (Count < 0 || Offset + Count == Data.Length)) return Data;

            return Data.ReadBytes(Offset, Count);
        }

        /// <summary>获取封包的数据流形式</summary>
        /// <returns></returns>
        public virtual Stream GetStream() { return new MemoryStream(Data, Offset, Count, false); }
        #endregion
    }
}