using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NewLife.Net
{
    /// <summary>WebSocket会话</summary>
    public class WebSocketSession : NetSession
    {
        private static Byte[] _prefix = "GET /".GetBytes();

        /// <summary>是否已经握手</summary>
        private Boolean _HandeShake;

        /// <summary>收到数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            if (!_HandeShake && e.Data.StartsWith(_prefix))
            {
                HandeShake(e.ToStr());

                _HandeShake = true;

                return;
            }

            e.Data = ProcessReceive(e.Stream);

            if (e.Data != null) base.OnReceive(e);
        }

        /// <summary>发送数据前需要处理</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override INetSession Send(Byte[] buffer, Int32 offset = 0, Int32 size = -1)
        {
            if (_HandeShake)
            {
                buffer = ProcessSend(buffer, offset, size);
                offset = 0;
                size = -1;
            }

            return base.Send(buffer, offset, size);
        }

        private void HandeShake(String data)
        {
            var key = data.Substring("\r\nSec-WebSocket-Key:", "\r\n");
            if (key.IsNullOrEmpty()) return;

            var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
            key = buf.ToBase64();

            var sb = new StringBuilder();
            sb.AppendLine("HTTP/1.1 101 Switching Protocols");
            sb.AppendLine("Upgrade: websocket");
            sb.AppendLine("Connection: Upgrade");
            sb.AppendLine("Sec-WebSocket-Accept: " + key);
            sb.AppendLine();

            Send(sb.ToString());
        }

        private Byte[] ProcessReceive(Stream ms)
        {
            if (ms.Length < 2) return null;

            // 仅处理一个包
            var fin = (ms.ReadByte() & 0x80) == 0x80;
            if (!fin) return null;

            var len = ms.ReadByte();

            bool mask = (len & 0x80) == 0x80;

            /*
             * 数据长度
             * len < 126    单字节表示长度
             * len = 126    后续2字节表示长度
             * len = 127    后续8字节表示长度
             */
            len = len & 0x7F;
            if (len == 126)
                len = ms.ReadBytes(2).ToInt();
            else if (len == 127)
                // 没有人会传输超大数据
                len = (Int32)BitConverter.ToUInt64(ms.ReadBytes(8), 0);

            var masks = new byte[4];
            if (mask) masks = ms.ReadBytes(4);

            // 读取数据
            var data = ms.ReadBytes(len);

            if (mask)
            {
                for (var i = 0; i < len; i++)
                {
                    data[i] = (byte)(data[i] ^ masks[i % 4]);
                }
            }

            return data;
        }

        private byte[] ProcessSend(Byte[] buffer, Int32 offset, Int32 size)
        {
            if (size < 0) size = buffer.Length - offset;

            var ms = new MemoryStream();
            ms.WriteByte(0x81);

            if (size < 126)
                ms.WriteByte((Byte)size);
            else if (size < 0xFFFF)
            {
                ms.WriteByte(126);
                ms.Write(size.GetBytes());
            }
            else
                throw new NotSupportedException();

            ms.Write(buffer, offset, size);

            return ms.ToArray();
        }
    }
}