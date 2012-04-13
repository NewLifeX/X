using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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
        private Flags _Flag = Flags.Header;
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
        #endregion

        #region 读写
        /// <summary>把消息头写入到流中</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            var writer = new BinaryWriter(stream);
            writer.Write((Byte)Flag);
            if (HasFlag(Flags.Channel)) writer.Write(Channel);
            if (HasFlag(Flags.SessionID)) writer.Write(SessionID);
        }

        /// <summary>把消息头转为字节数组</summary>
        /// <returns></returns>
        public Byte[] ToArray()
        {
            var ms = new MemoryStream();
            Write(ms);
            return ms.ToArray();
        }

        /// <summary>从数据流中读取消息头</summary>
        /// <param name="stream"></param>
        public void Read(Stream stream)
        {
            var reader = new BinaryReader(stream);
            Flag = (Flags)reader.ReadByte();
            if (HasFlag(Flags.Channel)) Channel = reader.ReadByte();
            if (HasFlag(Flags.SessionID)) SessionID = reader.ReadInt32();
        }
        #endregion

        #region 标识
        /// <summary>读取标识位</summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Boolean HasFlag(Flags flag) { return Flag.Has(flag); }

        /// <summary>设置标识位</summary>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public MessageHeader SetFlag(Flags flag, Boolean value)
        {
            if (flag != Flags.Header)
            {
                // 设置任意标识，会启用头。取消所有标识，会禁用头
                if (value)
                {
                    Flag.Set(Flags.Header, true);
                }
                else
                {
                    if ((Byte)flag == 0) Flag.Set(Flags.Header, false);
                }
            }

            Flag.Set(flag, value);
            return this;
        }

        /// <summary>是否有效消息头</summary>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static Boolean IsValid(Byte bit) { return (bit & 0x80) == 0x80; }
        #endregion

        #region 枚举
        /// <summary>用于指定消息头采用那些字段的标识</summary>
        public enum Flags : byte
        {
            /// <summary>是否使用消息头</summary>
            Header = 0x80,

            /// <summary>是否使用通道</summary>
            Channel = 2,

            /// <summary>是否使用会话</summary>
            SessionID = 4,
        }
        #endregion
    }
}