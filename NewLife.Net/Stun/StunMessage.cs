using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Serialization;
using System.Net;

namespace NewLife.Net.Stun
{
    /// <summary>Stun消息</summary>
    /// <remarks>未测试，可能没有考虑字节序</remarks>
    public class StunMessage : IAccessor
    {
        #region 属性
        /* RFC 5389 6.             
                All STUN messages MUST start with a 20-byte header followed by zero
                or more Attributes.  The STUN header contains a STUN message type,
                magic cookie, transaction ID, and message length.

                 0                   1                   2                   3
                 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                 |0 0|     STUN Message Type     |         Message Length        |
                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                 |                         Magic Cookie                          |
                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                 |                                                               |
                 |                     Transaction ID (96 bits)                  |
                 |                                                               |
                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
              
               The message length is the count, in bytes, of the size of the
               message, not including the 20 byte header.
            */

        private StunMessageType _Type;
        /// <summary>消息类型</summary>
        public StunMessageType Type { get { return _Type; } set { _Type = value; } }

        private UInt16 _Length;
        /// <summary>消息长度</summary>
        public UInt16 Length { get { return _Length; } set { _Length = value; } }

        private Int32 _MagicCookie;
        /// <summary>幻数。0x2112A442</summary>
        public Int32 MagicCookie { get { return _MagicCookie; } set { _MagicCookie = value; } }

        [FieldSize(12)]
        private Byte[] _TransactionID;
        /// <summary>会话编号</summary>
        public Byte[] TransactionID { get { return _TransactionID; } set { _TransactionID = value; } }

        [FieldSize("_Length", -20)]
        private Byte[] _Data;
        /// <summary>数据</summary>
        private Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        #region 扩展属性
        [NonSerialized]
        private Encoding _Encoding = Encoding.Default;
        /// <summary>编码</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }

        [NonSerialized]
        private IPEndPoint _MappedAddress;
        /// <summary>映射地址</summary>
        public IPEndPoint MappedAddress { get { return _MappedAddress; } set { _MappedAddress = value; } }

        [NonSerialized]
        private IPEndPoint _ResponseAddress;
        /// <summary>响应地址</summary>
        public IPEndPoint ResponseAddress { get { return _ResponseAddress; } set { _ResponseAddress = value; } }

        [NonSerialized]
        private ChangeRequest _Change;
        /// <summary>请求改变</summary>
        public ChangeRequest Change { get { return _Change; } set { _Change = value; } }

        [NonSerialized]
        private IPEndPoint _SourceAddress;
        /// <summary>源地址</summary>
        public IPEndPoint SourceAddress { get { return _SourceAddress; } set { _SourceAddress = value; } }

        [NonSerialized]
        private IPEndPoint _ChangedAddress;
        /// <summary>改变后的地址</summary>
        public IPEndPoint ChangedAddress { get { return _ChangedAddress; } set { _ChangedAddress = value; } }

        [NonSerialized]
        private String _UserName;
        /// <summary>用户名</summary>
        public String UserName { get { return _UserName; } set { _UserName = value; } }

        [NonSerialized]
        private String _Password;
        /// <summary>密码</summary>
        public String Password { get { return _Password; } set { _Password = value; } }

        [NonSerialized]
        private Error _Err;
        /// <summary>错误</summary>
        public Error Err { get { return _Err; } set { _Err = value; } }

        [NonSerialized]
        private IPEndPoint _ReflectedFrom;
        /// <summary>服务端从客户端拿到的地址</summary>
        public IPEndPoint ReflectedFrom { get { return _ReflectedFrom; } set { _ReflectedFrom = value; } }

        [NonSerialized]
        private String _ServerName;
        /// <summary>属性说明</summary>
        public String ServerName { get { return _ServerName; } set { _ServerName = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个Stun消息</summary>
        public StunMessage()
        {
            TransactionID = new Byte[12];
            var rnd = new Random((Int32)DateTime.Now.Ticks);
            rnd.NextBytes(TransactionID);
        }
        #endregion

        #region 读写
        /// <summary>从流中读取消息</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static StunMessage Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            var reader = new BinaryReaderX();
            reader.Stream = stream;
            reader.Settings.EncodeInt = false;
            return reader.ReadObject<StunMessage>();
        }

        void ParseAttribute(BinaryReader reader)
        {
            /* RFC 3489 11.2.
                Each attribute is TLV encoded, with a 16 bit type, 16 bit length, and variable value:

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |         Type                  |            Length             |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |                             Value                             ....
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                            
            */

            // Type
            AttributeType type = (AttributeType)reader.ReadUInt16();

            // Length
            int length = reader.ReadInt16();
            //Byte[] data = reader.ReadBytes(length);

            if (type == AttributeType.MappedAddress)
                MappedAddress = ParseEndPoint(reader);
            else if (type == AttributeType.ResponseAddress)
                ResponseAddress = ParseEndPoint(reader);
            else if (type == AttributeType.ChangeRequest)
            {
                /*
                    The CHANGE-REQUEST attribute is used by the client to request that
                    the server use a different address and/or port when sending the
                    response.  The attribute is 32 bits long, although only two bits (A
                    and B) are used:

                     0                   1                   2                   3
                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                    The meaning of the flags is:

                    A: This is the "change IP" flag.  If true, it requests the server
                       to send the Binding Response with a different IP address than the
                       one the Binding Request was received on.

                    B: This is the "change port" flag.  If true, it requests the
                       server to send the Binding Response with a different port than the
                       one the Binding Request was received on.
                */

                Change = new ChangeRequest(reader.ReadInt32());
            }
            else if (type == AttributeType.SourceAddress)
                SourceAddress = ParseEndPoint(reader);
            else if (type == AttributeType.ChangedAddress)
                ChangedAddress = ParseEndPoint(reader);
            else if (type == AttributeType.Username)
                UserName = Encoding.GetString(reader.ReadBytes(length));
            else if (type == AttributeType.Password)
                Password = Encoding.GetString(reader.ReadBytes(length));
            else if (type == AttributeType.MessageIntegrity)
                reader.ReadBytes(length);
            else if (type == AttributeType.ErrorCode)
            {
                /* 3489 11.2.9.
                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |                   0                     |Class|     Number    |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |      Reason Phrase (variable)                                ..
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                */

                reader.ReadBytes(2);
                if (Err == null) Err = new Error();
                Err.Code = (reader.ReadByte() & 0x7) * 100 + (reader.ReadByte() & 0xFF);
                Err.Reason = Encoding.GetString(reader.ReadBytes(length - 4));
            }
            else if (type == AttributeType.UnknownAttribute)
                reader.ReadBytes(length);
            else if (type == AttributeType.ReflectedFrom)
                ReflectedFrom = ParseEndPoint(reader);
            // XorMappedAddress
            // XorOnly
            // ServerName
            else if (type == AttributeType.ServerName)
                ServerName = Encoding.GetString(reader.ReadBytes(length));
            else
                reader.ReadBytes(length);
        }

        /// <summary>把消息写入流中</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            // 因为写入前会预先处理Data，所以可以知道最终长度，不需要事后处理
            var writer = new BinaryWriterX();
            writer.Stream = stream;
            writer.Settings.EncodeInt = false;
            writer.WriteObject(this);
        }

        void ProcessAttribute(BinaryWriter writer)
        {
            /* RFC 3489 11.2.
                            After the header are 0 or more attributes.  Each attribute is TLV
                            encoded, with a 16 bit type, 16 bit length, and variable value:

                            0                   1                   2                   3
                            0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                           |         Type                  |            Length             |
                           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                           |                             Value                             ....
                           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                        */

            if (MappedAddress != null)
                StoreEndPoint(AttributeType.MappedAddress, MappedAddress, writer);
            else if (ResponseAddress != null)
                StoreEndPoint(AttributeType.ResponseAddress, ResponseAddress, writer);
            else if (Change != null)
            {
                /*
                    The CHANGE-REQUEST attribute is used by the client to request that
                    the server use a different address and/or port when sending the
                    response.  The attribute is 32 bits long, although only two bits (A
                    and B) are used:

                     0                   1                   2                   3
                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                    The meaning of the flags is:

                    A: This is the "change IP" flag.  If true, it requests the server
                       to send the Binding Response with a different IP address than the
                       one the Binding Request was received on.

                    B: This is the "change port" flag.  If true, it requests the
                       server to send the Binding Response with a different port than the
                       one the Binding Request was received on.
                */

                // Attribute header
                writer.Write((Int16)AttributeType.ChangeRequest);
                writer.Write((Int16)4);
                writer.Write(Change.ToInt32());
            }
            else if (SourceAddress != null)
                StoreEndPoint(AttributeType.SourceAddress, SourceAddress, writer);
            else if (ChangedAddress != null)
                StoreEndPoint(AttributeType.ChangedAddress, ChangedAddress, writer);
            else if (UserName != null)
            {
                byte[] userBytes = Encoding.GetBytes(UserName);
                writer.Write((Int16)AttributeType.Username);
                writer.Write((Int16)userBytes.Length);
                writer.Write(userBytes);
            }
            else if (Password != null)
            {
                byte[] userBytes = Encoding.ASCII.GetBytes(Password);
                writer.Write((Int16)AttributeType.Password);
                writer.Write((Int16)userBytes.Length);
                writer.Write(userBytes);
            }
            else if (Err != null)
            {
                /* 3489 11.2.9.
                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |                   0                     |Class|     Number    |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |      Reason Phrase (variable)                                ..
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                */

                byte[] reasonBytes = Encoding.GetBytes(Err.Reason);
                writer.Write((Int16)AttributeType.ErrorCode);
                writer.Write((Int16)(4 + reasonBytes.Length));
                writer.Write((Int16)0);
                writer.Write((byte)Math.Floor((double)(Err.Code / 100)));
                writer.Write((byte)(Err.Code & 0xFF));
            }
            else if (ReflectedFrom != null)
                StoreEndPoint(AttributeType.ReflectedFrom, ReflectedFrom, writer);
        }
        #endregion

        #region 分析终结点
        IPEndPoint ParseEndPoint(BinaryReader reader)
        {
            /*
                It consists of an eight bit address family, and a sixteen bit
                port, followed by a fixed length value representing the IP address.

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |x x x x x x x x|    Family     |           Port                |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                             Address                           |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            */

            // Skip family
            reader.ReadByte();
            reader.ReadByte();

            // Port
            int port = reader.ReadInt16();

            // Address
            byte[] ip = reader.ReadBytes(4);

            return new IPEndPoint(new IPAddress(ip), port);
        }

        void StoreEndPoint(AttributeType type, IPEndPoint endPoint, BinaryWriter writer)
        {
            /*
                It consists of an eight bit address family, and a sixteen bit
                port, followed by a fixed length value representing the IP address.

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |x x x x x x x x|    Family     |           Port                |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                             Address                           |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+             
            */

            // 头部，类型和长度（固定8）
            writer.Write((Int16)type);
            writer.Write((Int16)8);

            writer.Write((Byte)1);
            writer.Write(endPoint.Port);
            writer.Write(endPoint.Address.GetAddressBytes());
        }
        #endregion

        #region 内嵌类型
        /// <summary>属性类型</summary>
        private enum AttributeType
        {
            MappedAddress = 0x0001,
            ResponseAddress = 0x0002,
            ChangeRequest = 0x0003,
            SourceAddress = 0x0004,
            ChangedAddress = 0x0005,
            Username = 0x0006,
            Password = 0x0007,
            MessageIntegrity = 0x0008,
            ErrorCode = 0x0009,
            UnknownAttribute = 0x000A,
            ReflectedFrom = 0x000B,
            XorMappedAddress = 0x8020,
            XorOnly = 0x0021,
            ServerName = 0x8022,
        }

        /// <summary>请求改变</summary>
        public class ChangeRequest
        {
            private Boolean _ChangeIP;
            /// <summary>要求服务器必须改变IP</summary>
            public Boolean ChangeIP { get { return _ChangeIP; } set { _ChangeIP = value; } }

            private Boolean _ChangePort;
            /// <summary>要求服务器必须改变端口</summary>
            public Boolean ChangePort { get { return _ChangePort; } set { _ChangePort = value; } }

            internal Int32 ToInt32() { return (ChangeIP ? 4 : 0) + (ChangePort ? 2 : 0); }

            /// <summary>实例化</summary>
            /// <param name="b"></param>
            public ChangeRequest(Int32 b)
            {
                ChangeIP = (b & 4) != 0;
                ChangePort = (b & 2) != 0;
            }

            /// <summary>实例化</summary>
            /// <param name="changeip"></param>
            /// <param name="changeport"></param>
            public ChangeRequest(Boolean changeip, Boolean changeport)
            {
                ChangeIP = changeip;
                ChangePort = changeport;
            }
        }

        /// <summary>错误</summary>
        public class Error
        {
            private Int32 _Code;
            /// <summary>错误代码</summary>
            public Int32 Code { get { return _Code; } set { _Code = value; } }

            private String _Reason;
            /// <summary>错误原因</summary>
            public String Reason { get { return _Reason; } set { _Reason = value; } }
        }
        #endregion

        #region IAccessor 成员
        bool IAccessor.Read(IReader reader) { return false; }

        bool IAccessor.ReadComplete(IReader reader, bool success)
        {
            // 分析属性
            if (Data != null && Data.Length > 0)
            {
                var ms = new MemoryStream(Data);
                var r = new BinaryReader(ms);
                ParseAttribute(r);
            }

            return success;
        }

        bool IAccessor.Write(IWriter writer)
        {
            // 处理属性

            return false;
        }

        bool IAccessor.WriteComplete(IWriter writer, bool success) { return success; }
        #endregion
    }
}