using System;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>消息头</summary>
    /// <remarks>
    /// 消息头结构：
    /// 1字节标识
    /// 1字节通道
    /// 4字节会话
    /// </remarks>
    public class MessageHeader
    {
        #region 属性
        private Flags _Flag;
        /// <summary>标识位</summary>
        public Flags Flag { get { return _Flag; } set { _Flag = value; } }

        /// <summary>是否采用消息头</summary>
        public Boolean UseHeader { get { return HasFlag(Flags.Header); } set { SetFlag(Flags.Header, value); } }

        private Byte _Channel;
        /// <summary>消息通道</summary>
        public Byte Channel { get { return _Channel; } set { _Channel = value; SetFlag(Flags.Channel, value != 0); } }

        private Int32 _SessionID;
        /// <summary>会话编号</summary>
        public Int32 SessionID { get { return _SessionID; } set { _SessionID = value; SetFlag(Flags.SessionID, value != 0); } }

        private Int32 _Length;
        /// <summary>消息长度，消息头以外的长度，也不包括Kind。建议在较长消息中指定消息体长度，便于接收方处理粘包的问题。</summary>
        public Int32 Length { get { return _Length; } set { _Length = value; SetFlag(Flags.Length, value != 0); } }
        #endregion

        #region 读写
        /// <summary>把消息头写入到流中</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            if (!UseHeader) return;

            var writer = new BinaryWriterX(stream);
            writer.Settings.EncodeInt = true;
            writer.Write((Byte)Flag);
            if (HasFlag(Flags.Channel)) writer.Write(Channel);
            if (HasFlag(Flags.SessionID)) writer.Write(SessionID);
            if (HasFlag(Flags.Length)) writer.Write(Length);
        }

        /// <summary>把消息头转为字节数组</summary>
        /// <returns></returns>
        public Byte[] ToArray()
        {
            if (!UseHeader) return new Byte[0];

            var ms = new MemoryStream();
            Write(ms);
            return ms.ToArray();
        }

        /// <summary>从数据流中读取消息头</summary>
        /// <param name="stream"></param>
        public void Read(Stream stream)
        {
            var reader = new BinaryReaderX(stream);
            reader.Settings.EncodeInt = true;
            Flag = (Flags)reader.ReadByte();
            if (HasFlag(Flags.Channel)) Channel = reader.ReadByte();
            if (HasFlag(Flags.SessionID)) SessionID = reader.ReadInt32();
            if (HasFlag(Flags.Length)) Length = reader.ReadInt32();
        }
        #endregion

        #region 方法
        ///// <summary>计算指定类型消息头所需要的长度</summary>
        ///// <param name="flag"></param>
        ///// <returns></returns>
        //public static Int32 GetSize(Flags flag)
        //{
        //    // 第一个字节Flag
        //    var n = 1;
        //    if (HasFlag(Flags.Channel)) n++;
        //    if (HasFlag(Flags.SessionID)) n += 4;
        //}

        /// <summary>克隆</summary>
        /// <returns></returns>
        public MessageHeader Clone()
        {
            return this.MemberwiseClone() as MessageHeader;
        }
        #endregion

        #region 标识
        /// <summary>读取标识位</summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Boolean HasFlag(Flags flag) { return Flag.Has(flag); }

        /// <summary>设置标识位</summary>
        /// <param name="flag"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public MessageHeader SetFlag(Flags flag, Boolean value)
        {
            if (flag != Flags.Header)
            {
                // 设置任意标识，会启用头。取消所有标识，会禁用头
                if (value)
                {
                    Flag = Flag.Set(Flags.Header, true);
                }
                else
                {
                    if ((Byte)flag == 0) Flag = Flag.Set(Flags.Header, false);
                }
            }

            Flag = Flag.Set(flag, value);
            return this;
        }

        /// <summary>是否有效消息头</summary>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static Boolean IsValid(Byte bit) { return ((Flags)bit).Has(Flags.Header); }
        #endregion

        #region 枚举
        /// <summary>用于指定消息头采用那些字段的标识</summary>
        [Flags]
        public enum Flags : byte
        {
            /// <summary>是否使用消息头</summary>
            Header = 0x80,

            /// <summary>是否使用通道</summary>
            Channel = 2,

            /// <summary>是否使用会话</summary>
            SessionID = 4,

            /// <summary>是否使用长度</summary>
            Length = 8
        }
        #endregion
    }
}