using System;
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
    }
}