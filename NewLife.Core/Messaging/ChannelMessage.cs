using System;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>通道消息。封装带有通道编号的消息</summary>
    public class ChannelMessage : Message, IAccessor
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Channel; } }

        private Byte _Channel;
        /// <summary>消息通道</summary>
        public Byte Channel { get { return _Channel; } set { _Channel = value; } }

        private Int32 _SessionID;
        /// <summary>会话编号</summary>
        public Int32 SessionID { get { return _SessionID; } set { _SessionID = value; } }

        [NonSerialized]
        private Message _Message;
        /// <summary>内部消息对象</summary>
        public Message Message { get { return _Message; } set { _Message = value; } }

        #region IAccessor 成员
        Boolean IAccessor.Read(IReader reader)
        {
            var r = reader as IReader2;
            Channel = r.ReadByte();
            SessionID = r.ReadInt32();
            if (reader.Stream.Position != reader.Stream.Length)
            {
                if (Message.PeekKind(reader.Stream) != 0) Message = Message.Read(reader.Stream, reader.GetKind());
            }

            return true;
        }

        Boolean IAccessor.ReadComplete(IReader reader, Boolean success) { return success; }

        Boolean IAccessor.Write(IWriter writer)
        {
            writer.Depth++;
            writer.WriteMember("Channel", Channel, Channel.GetType(), 0, null);
            writer.WriteMember("SessionID", SessionID, SessionID.GetType(), 1, null);
            if (Message != null)
                Message.Write(writer.Stream, writer.GetKind());
            else
                writer.WriteMember("Null", 0, typeof(Byte), 0, null);
            writer.Depth--;

            return true;
        }

        Boolean IAccessor.WriteComplete(IWriter writer, Boolean success) { return success; }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var msg = Message;
            if (msg != null)
                return String.Format("{0} {1} {2}", base.ToString(), Channel, msg);
            else
                return base.ToString();
        }
        #endregion
    }
}