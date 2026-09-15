using NewLife;
using NewLife.Buffers;
using NewLife.Data;
using NewLife.Http;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Http;

public class WebSocketMessageTests
{
    [Fact]
    public void Text()
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Text,
            Payload = (ArrayPacket)$"Hello NewLife",
        };

        var pk = msg.ToPacket();
        Assert.Equal("810D48656C6C6F204E65774C696665", pk.ToHex());

        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(msg.Payload.ToHex(), msg2.Payload.ToHex());

        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void Ping()
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Ping,
            Payload = (ArrayPacket)$"Ping {DateTime.UtcNow.ToFullString()}",
        };

        var pk = msg.ToPacket();
        Assert.StartsWith("891850696E67", pk.ToHex());

        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(msg.Payload.ToHex(), msg2.Payload.ToHex());

        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void Close()
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Close,
            CloseStatus = 1000,
            StatusDescription = "Finish",
        };

        var pk = msg.ToPacket();
        Assert.Equal("880803E846696E697368", pk.ToHex());

        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(msg.CloseStatus, msg2.CloseStatus);
        Assert.Equal(msg.StatusDescription, msg2.StatusDescription);

        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void DefaultMessageOverWebsocket()
    {
        var dm = new DefaultMessage
        {
            Flag = 0x01,
            Sequence = 0xAB,
            Payload = (ArrayPacket)"Hello NewLife"
        };

        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = dm.ToPacket(),
        };

        var pk = msg.ToPacket();
        Assert.Equal("821101AB0D0048656C6C6F204E65774C696665", pk.ToHex());

        // 发送帧与消息负载共享底层节点：ToPacket 的 ExpandHeader 就地接管会作废源链，先快照发送负载
        var sent = msg.Payload.ToHex();

        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(sent, msg2.Payload.ToHex());

        var dm2 = new DefaultMessage();
        rs = dm2.Read(msg2.Payload);
        Assert.True(rs);

        Assert.Equal(dm.Flag, dm2.Flag);
        Assert.Equal(dm.Sequence, dm2.Sequence);
        Assert.Equal(dm.Payload.ToHex(), dm2.Payload.ToHex());

        dm2.Dispose();
        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void DefaultMessageOverWebsocket2()
    {
        var str = "Hello NewLife";
        var buf = new Byte[8 + str.Length];
        var src = new ArrayPacket(buf, 8, buf.Length - 8);
        var writer = new SpanWriter(src.GetSpan());
        writer.Write(str, -1);

        var dm = new DefaultMessage
        {
            Flag = 0x01,
            Sequence = 0xAB,
            Payload = src
        };
        var pk = dm.ToPacket();
        Assert.Null(pk.Next);

        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = pk,
        };

        pk = msg.ToPacket();
        Assert.Null(pk.Next);
        Assert.Equal("821101AB0D0048656C6C6F204E65774C696665", pk.ToHex());

        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(msg.Payload.ToHex(), msg2.Payload.ToHex());

        var dm2 = new DefaultMessage();
        rs = dm2.Read(msg2.Payload);
        Assert.True(rs);

        Assert.Equal(dm.Flag, dm2.Flag);
        Assert.Equal(dm.Sequence, dm2.Sequence);
        Assert.Equal(dm.Payload.ToHex(), dm2.Payload.ToHex());
        Assert.Equal(str, dm2.Payload.ToStr());

        dm2.Dispose();
        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void TextWithMask()
    {
        var masks = new Byte[] { 0x12, 0x34, 0x56, 0x78 };
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Text,
            Payload = (ArrayPacket)$"Hello NewLife",
            MaskKey = masks,
        };

        // ToPacket 会原地 XOR 修改 Payload，先保存原始数据用于后续断言
        var originalHex = msg.Payload.ToHex();

        var pk = msg.ToPacket();
        var hex = pk.ToHex();

        // 验证帧头：FIN(0x80) | Text(0x01) = 0x81 + Mask(0x80) | len(13) = 0x8D
        Assert.StartsWith("818D", hex);

        // 验证 4 字节掩码在头部
        Assert.Contains("12345678", hex);

        // 解码验证
        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(originalHex, msg2.Payload.ToHex());
        Assert.NotNull(msg2.MaskKey);
        Assert.Equal(masks, msg2.MaskKey);

        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void BinaryWithMask()
    {
        var masks = new Byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var payload = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = (ArrayPacket)payload,
            MaskKey = masks,
        };

        // ToPacket 会原地 XOR 修改 Payload，先保存原始数据
        var originalPayload = payload.ToArray();

        var pk = msg.ToPacket();
        var hex = pk.ToHex();

        // 验证帧头：FIN(0x80) | Binary(0x02) = 0x82 + Mask(0x80) | len(5) = 0x85
        Assert.StartsWith("8285", hex);

        // 验证 4 字节掩码
        Assert.Contains("DEADBEEF", hex);

        // 解码验证
        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(originalPayload, msg2.Payload?.ToArray());
        Assert.Equal(masks, msg2.MaskKey);

        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void PingWithMask()
    {
        var masks = new Byte[] { 0x11, 0x22, 0x33, 0x44 };
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Ping,
            Payload = (ArrayPacket)$"PingTest",
            MaskKey = masks,
        };

        // ToPacket 会原地 XOR 修改 Payload
        var originalHex = msg.Payload.ToHex();

        var pk = msg.ToPacket();
        var hex = pk.ToHex();

        // 验证帧头：FIN(0x80) | Ping(0x09) = 0x89 + Mask(0x80) | len(8) = 0x88
        Assert.StartsWith("8988", hex);
        Assert.Contains("11223344", hex);

        // 解码验证
        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(originalHex, msg2.Payload.ToHex());

        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void CloseWithMask()
    {
        var masks = new Byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Close,
            CloseStatus = 1000,
            StatusDescription = "Done",
            MaskKey = masks,
        };

        // ToPacket 会原地 XOR 修改 Payload
        var originalStatus = msg.CloseStatus;
        var originalDesc = msg.StatusDescription;

        var pk = msg.ToPacket();
        var hex = pk.ToHex();

        // 验证帧头：FIN(0x80) | Close(0x08) = 0x88 + Mask(0x80) | len(6) = 0x86
        Assert.StartsWith("8886", hex);
        Assert.Contains("AABBCCDD", hex);

        // 解码验证
        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(originalStatus, msg2.CloseStatus);
        Assert.Equal(originalDesc, msg2.StatusDescription);

        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void LargePayloadWithMask()
    {
        var masks = new Byte[] { 0x01, 0x02, 0x03, 0x04 };
        var payload = new Byte[200];
        Random.Shared.NextBytes(payload);

        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = (ArrayPacket)payload,
            MaskKey = masks,
        };

        // ToPacket 会原地 XOR 修改 Payload
        var originalPayload = payload.ToArray();

        var pk = msg.ToPacket();
        var hex = pk.ToHex();

        // >125 字节应使用扩展长度 2 字节 (126)
        Assert.StartsWith("82FE00C8", hex);

        // 解码验证
        var msg2 = new WebSocketMessage();
        var rs = msg2.Read(pk);
        Assert.True(rs);

        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(originalPayload, msg2.Payload?.ToArray());

        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    public void ReadMaskedFrameFromStream()
    {
        // 构造一个由标准客户端产生的掩码帧：文本 "Hello"，掩码 0x37,0xFA,0x21,0x3D
        // 计算掩码后数据：
        // 'H'(0x48)^0x37=0x7F, 'e'(0x65)^0xFA=0x9F, 'l'(0x6C)^0x21=0x4D, 'l'(0x6C)^0x3D=0x51
        // 'o'(0x6F)^0x37=0x58
        var masks = new Byte[] { 0x37, 0xFA, 0x21, 0x3D };
        var maskedPayload = new Byte[] { 0x7F, 0x9F, 0x4D, 0x51, 0x58 };

        // 手动构建帧：FIN+Text(0x81) | MASK+len5(0x85) | MaskKey(4 bytes) | masked payload
        var frame = new Byte[2 + 4 + 5];
        frame[0] = 0x81;
        frame[1] = 0x85; // mask + length 5
        Array.Copy(masks, 0, frame, 2, 4);
        Array.Copy(maskedPayload, 0, frame, 6, 5);

        var pk = new ArrayPacket(frame);
        var msg = new WebSocketMessage();
        var rs = msg.Read(pk);
        Assert.True(rs);

        Assert.Equal(WebSocketMessageType.Text, msg.Type);
        Assert.True(msg.Fin);
        Assert.Equal("Hello", msg.Payload?.ToStr());
        Assert.Equal(masks, msg.MaskKey);
    }

    [Theory(DisplayName = "Read：链式大帧（>8KB 跨段）掩码/非掩码均可解码")]
    [InlineData(true)]
    [InlineData(false)]
    public void Read_ChainedLargeFrame(Boolean masked)
    {
        var payload = new Byte[20_000];
        Random.Shared.NextBytes(payload);

        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = new ArrayPacket(payload.ToArray()),
            MaskKey = masked ? new Byte[] { 0x11, 0x22, 0x33, 0x44 } : null,
        };
        var frame = msg.ToPacket().ToArray();

        // 按 8KB 分段构造链式帧（模拟大帧跨接收轮零拷贝组链）
        IPacket? head = null;
        OwnerPacket? tail = null;
        var pos = 0;
        while (pos < frame.Length)
        {
            var count = Math.Min(8192, frame.Length - pos);
            var seg = new OwnerPacket(count);
            frame.AsSpan(pos, count).CopyTo(seg.GetSpan());
            if (head == null) head = seg; else tail!.Next = seg;
            tail = seg;
            pos += count;
        }

        try
        {
            var msg2 = new WebSocketMessage();
            Assert.True(msg2.Read(head!));
            Assert.Equal(msg.Type, msg2.Type);
            Assert.Equal(payload, msg2.Payload!.ToArray());
        }
        finally
        {
            head!.TryDispose();
        }
    }

    [Theory(DisplayName = "ToPacket：链式负载（>8KB 跨段）掩码/非掩码发送后原样可读")]
    [InlineData(true)]
    [InlineData(false)]
    public void ToPacket_ChainedPayload(Boolean masked)
    {
        var payload = new Byte[20_000];
        Random.Shared.NextBytes(payload);

        // 按 8KB 分段构造链式负载（模拟跨段数据直接发送；发送端 XOR 会原地修改各段缓冲）
        IPacket? head = null;
        OwnerPacket? tail = null;
        var pos = 0;
        while (pos < payload.Length)
        {
            var count = Math.Min(8192, payload.Length - pos);
            var seg = new OwnerPacket(count);
            payload.AsSpan(pos, count).CopyTo(seg.GetSpan());
            if (head == null) head = seg; else tail!.Next = seg;
            tail = seg;
            pos += count;
        }

        try
        {
            var msg = new WebSocketMessage
            {
                Type = WebSocketMessageType.Binary,
                Payload = head,
                MaskKey = masked ? new Byte[] { 0x11, 0x22, 0x33, 0x44 } : null,
            };
            var frame = msg.ToPacket();

            var msg2 = new WebSocketMessage();
            Assert.True(msg2.Read(frame));
            Assert.Equal(msg.Type, msg2.Type);
            Assert.Equal(payload, msg2.Payload!.ToArray());

            msg2.Dispose();
            frame.TryDispose();
        }
        finally
        {
            head!.TryDispose();
        }
    }

    [Theory(DisplayName = "ToPacket：65535 字节负载（16位扩展长度上限）帧长自洽且往返一致")]
    [InlineData(true)]
    [InlineData(false)]
    public void ToPacket_Payload65535(Boolean masked)
    {
        var payload = new Byte[65535];
        Random.Shared.NextBytes(payload);

        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = new ArrayPacket(payload.ToArray()),
            MaskKey = masked ? new Byte[] { 0x11, 0x22, 0x33, 0x44 } : null,
        };
        var frame = msg.ToPacket();

        // 头部 4 字节（FIN+opcode、126、2 字节长度）+ 负载 65535 字节；掩码再加 4 字节
        Assert.Equal(masked ? 65543 : 65539, frame.Total);

        var msg2 = new WebSocketMessage();
        Assert.True(msg2.Read(frame));
        Assert.Equal(msg.Type, msg2.Type);
        Assert.Equal(payload, msg2.Payload!.ToArray());
        Assert.Equal(frame.Total, msg2.GetFrameSize());

        msg2.Dispose();
        frame.TryDispose();
    }

    [Theory(DisplayName = "ToPacket：Close 帧忽略 Payload，正文只由状态码和描述构造")]
    [InlineData(true)]
    [InlineData(false)]
    public void ToPacket_CloseIgnoresPayload(Boolean masked)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Close,
            CloseStatus = 1000,
            StatusDescription = "Finish",
            Payload = (ArrayPacket)"Should Be Ignored",
            MaskKey = masked ? new Byte[] { 0xAA, 0xBB, 0xCC, 0xDD } : null,
        };

        var frame = msg.ToPacket();

        // 帧长 = 2 字节基础头 +（掩码 4）+ 2 字节状态码 + 6 字节描述；Payload 被忽略
        Assert.Equal(masked ? 14 : 10, frame.Total);
        if (!masked) Assert.Equal("880803E846696E697368", frame.ToHex());

        var msg2 = new WebSocketMessage();
        Assert.True(msg2.Read(frame));
        Assert.Equal(WebSocketMessageType.Close, msg2.Type);
        Assert.Equal(1000, msg2.CloseStatus);
        Assert.Equal("Finish", msg2.StatusDescription);

        msg2.Dispose();
        frame.TryDispose();
    }

    [Theory(DisplayName = "ToPacket：空首段链式负载（Length=0，Total>0）不丢数据")]
    [InlineData(true)]
    [InlineData(false)]
    public void ToPacket_EmptyHeadChain(Boolean masked)
    {
        var data = "Hello NewLife".GetBytes();
        var original = data.ToArray();
        IPacket body = new ArrayPacket([]) { Next = new ArrayPacket(data) };
        Assert.Equal(0, body.Length);
        Assert.Equal(data.Length, body.Total);

        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = body,
            MaskKey = masked ? new Byte[] { 0x01, 0x02, 0x03, 0x04 } : null,
        };
        var frame = msg.ToPacket();

        var msg2 = new WebSocketMessage();
        Assert.True(msg2.Read(frame));
        Assert.Equal(original, msg2.Payload!.ToArray());

        msg2.Dispose();
        frame.TryDispose();
    }

    [Theory(DisplayName = "ToPacket：0 长度负载往返")]
    [InlineData(true)]
    [InlineData(false)]
    public void ToPacket_ZeroLengthPayload(Boolean masked)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Text,
            Payload = (ArrayPacket)String.Empty,
            MaskKey = masked ? new Byte[] { 0x01, 0x02, 0x03, 0x04 } : null,
        };

        var frame = msg.ToPacket();
        Assert.Equal(masked ? 6 : 2, frame.Total);

        var msg2 = new WebSocketMessage();
        Assert.True(msg2.Read(frame));
        Assert.Equal(WebSocketMessageType.Text, msg2.Type);
        Assert.Equal(0, msg2.Payload!.Total);

        msg2.Dispose();
        frame.TryDispose();
    }
}