using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Model
{
    /// <summary>数据接收事件参数</summary>
    public class DataEventArgs : EventArgs
    {
        private Byte[] _Buffer;
        /// <summary>缓冲区</summary>
        public Byte[] Buffer { get { return _Buffer; } set { _Buffer = value; } }

        private Int32 _Offset;
        /// <summary>偏移</summary>
        public Int32 Offset { get { return _Offset; } set { _Offset = value; } }

        private Int32 _Count;
        /// <summary>字节数</summary>
        public Int32 Count { get { return _Count; } set { _Count = value; } }

        /// <summary>实例化</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public DataEventArgs(Byte[] buffer, Int32 offset, Int32 count)
        {
            Buffer = buffer;
            Offset = offset;
            Count = count;
        }
    }
}