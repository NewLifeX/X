using System;
using System.IO;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Messaging
{
    /// <summary>标准消息</summary>
    /// <remarks>
    /// 标准网络封包协议：1 Flag + 1 Sequence + 2 Length + N Payload
    /// 1个字节标识位，标识请求、响应、错误、加密、压缩等；
    /// 1个字节序列号，用于请求响应包配对；
    /// 2个字节数据长度N，小端，指示后续负载数据长度（不包含头部4个字节），解决粘包问题；
    /// N个字节负载数据，数据内容完全由业务决定，最大长度65535=64k。
    /// 如：
    /// Open => OK
    /// 01-01-04-00-"Open" => 81-01-02-00-"OK"
    /// 
    /// 针对纯字符串场景，采用8字符HEX编码头部。
    /// 首字符0表示请求，8表示响应
    /// 如：
    /// Open => OK
    /// 01010400 Open => 81010200 OK
    /// </remarks>
    public class DefaultMessage : Message
    {
        #region 属性
        /// <summary>标记位</summary>
        public Byte Flag { get; set; } = 1;

        ///// <summary>是否有错</summary>
        //public Boolean Error { get; set; }

        /// <summary>序列号，匹配请求和响应</summary>
        public Byte Sequence { get; set; }

        /// <summary>对方使用纯字符串，不具备二进制编码能力</summary>
        private Boolean _IsChar;
        #endregion

        #region 方法
        /// <summary>根据请求创建配对的响应消息</summary>
        /// <returns></returns>
        public override IMessage CreateReply()
        {
            if (Reply) throw new Exception("不能根据响应消息创建响应消息");

            var type = GetType();
            var msg = type == typeof(DefaultMessage) ? new DefaultMessage() : type.CreateInstance() as DefaultMessage;
            msg.Flag = Flag;
            msg.Reply = true;
            msg.Sequence = Sequence;

            return msg;
        }

        /// <summary>从数据包中读取消息</summary>
        /// <param name="pk"></param>
        /// <returns>是否成功</returns>
        public override Boolean Read(Packet pk)
        {
            if (pk.Count < 4) throw new ArgumentOutOfRangeException(nameof(pk), "数据包头部长度不足4字节");

            var size = 4;
            var buf = pk.Data.ReadBytes(pk.Offset, size);

            // 检查纯字符串以字符0或8开头，所以二进制消息不许用0x30和0x38开头
            if (buf[0] == '0' || buf[0] == '8')
            {
                _IsChar = true;
                size = 8;
                if (pk.Count < size) throw new ArgumentOutOfRangeException(nameof(pk), "数据包头部长度不足{0}字节".F(size));

                buf = pk.Data.ReadBytes(pk.Offset, size).ToStr().ToHex();
            }

            Flag = buf[0];
            if ((Flag & 0x80) == 0x80) Reply = true;
            //if ((Flag & 0x40) == 0x40) Error = true;

            Sequence = buf[1];

            var len = (buf[3] << 8) | buf[2];
            if (size + len > pk.Count) throw new ArgumentOutOfRangeException(nameof(pk), "数据包长度{0}不足{1}字节".F(pk.Count, size + len));

            Payload = new Packet(pk.Data, pk.Offset + size, len);

            return true;
        }

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream"></param>
        public override void Write(Stream stream)
        {
            var ms = stream;
            if (_IsChar) ms = new MemoryStream();

            // 标记位
            var b = Flag;
            if (Reply) b |= 0x80;
            //if (Error) b |= 0x40;
            ms.WriteByte(b);

            // 序列号
            ms.WriteByte(Sequence);

            // 2字节长度，小端字节序
            var len = 0;
            if (Payload != null) len = Payload.Count;
            ms.WriteByte((Byte)(len & 0xFF));
            ms.WriteByte((Byte)(len >> 8));

            if (_IsChar) stream.Write(ms.ToArray());

            Payload?.WriteTo(stream);
        }
        #endregion
    }
}