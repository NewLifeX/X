using System.Security.Cryptography;
using System.Text;
using NewLife.Data;

namespace NewLife.Net;

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
        var pk = e.Packet;
        if (!_HandeShake && pk.ReadBytes(0, _prefix.Length).StartsWith(_prefix))
        {
            HandeShake(pk.ToStr());

            _HandeShake = true;

            return;
        }

        pk = ProcessReceive(pk);
        e.Packet = pk;

        if (pk != null) base.OnReceive(e);
    }

    /// <summary>发送数据前需要处理</summary>
    /// <param name="pk">数据包</param>
    /// <returns></returns>
    public override INetSession Send(Packet pk)
    {
        if (_HandeShake) pk = ProcessSend(pk);

        return base.Send(pk);
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

    private Packet ProcessReceive(Packet pk)
    {
        if (pk.Count < 2) return null;

        var ms = pk.GetStream();

        // 仅处理一个包
        var fin = (ms.ReadByte() & 0x80) == 0x80;
        if (!fin) return null;

        var len = ms.ReadByte();

        var mask = (len & 0x80) == 0x80;

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

        // 如果mask，剩下的就是数据，避免拷贝，提升性能
        if (!mask) return new Packet(pk.Data, pk.Offset + (Int32)ms.Position, len);

        var masks = new Byte[4];
        if (mask) masks = ms.ReadBytes(4);

        // 读取数据
        var data = ms.ReadBytes(len);

        if (mask)
        {
            for (var i = 0; i < len; i++)
            {
                data[i] = (Byte)(data[i] ^ masks[i % 4]);
            }
        }

        return data;
    }

    private Packet ProcessSend(Packet pk)
    {
        var size = pk.Count;

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

        pk.WriteTo(ms);

        return new Packet(ms.ToArray());
    }
}