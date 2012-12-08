using System;

namespace NewLife.Net.TDP
{
    /// <summary>TDP协议包</summary>
    /// <remarks>
    /// TDP，在UDP上实现TCP。
    /// </remarks>
    class TDPPacket
    {
        #region 属性
        /// <summary>包括4位首部长度，保留6位，还有6位标志位</summary>
        private Int32 _Data;

        /// <summary>头部长度</summary>
        public Byte Length { get { return (Byte)(_Data >> 12); } set { _Data &= (value & 0x0F) << 12; } }

        /// <summary>标记</summary>
        public TDPFlags Flag { get { return (TDPFlags)(_Data & 0xFF); } set { _Data &= (Byte)value; } }

        private Int16 _WindowSize;
        /// <summary>16位窗口大小</summary>
        public Int16 WindowSize { get { return _WindowSize; } set { _WindowSize = value; } }

        private Int32 _SequenceNumber;
        /// <summary>序列号</summary>
        public Int32 SequenceNumber { get { return _SequenceNumber; } set { _SequenceNumber = value; } }

        private Int32 _AckNumber;
        /// <summary>确认序列号</summary>
        public Int32 AckNumber { get { return _AckNumber; } set { _AckNumber = value; } }

        // 选项：只有一个选项字段，为最长报文大小，即MSS。TDP 选项格式与TCP 选项格式一致，kind=0 时表示选项结束，kind=1 时表示无操作，kind=2 时表示最大报文段长度。
        #endregion
    }
}