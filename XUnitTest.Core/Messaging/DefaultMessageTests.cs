using System.Buffers;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

public class DefaultMessageTests
{
    [Fact]
    public void BinaryEncode()
    {
        var codec = new SrmpCodec();

        var msg = new DefaultMessage { Sequence = 1 };
        msg.SetBody("Open".GetBytes().AsPacket());
        var pk = codec.Build(msg)!;
        Assert.Equal(1, pk[0]);
        Assert.Equal(1, pk[1]);
        Assert.Equal(4, pk[2]);
        Assert.Equal(0, pk[3]);
        var tail1 = pk.Slice(4, -1);
        Assert.Equal("Open", tail1.ToStr());                    // 只读查看：共享切片需自行释放
        tail1.TryDispose();

        // 解析回读（帧层绑体：内存模式，体为共享切片）
        var rs = codec.TryParse(pk.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msgd = Assert.IsType<DefaultMessage>(rs.Value.Message);
        msgd.SetBody(pk.Slice(rs.Value.HeaderSize, (Int32)rs.Value.BodyLength));
        Assert.Equal(msg.Flag, msgd.Flag);
        Assert.Equal(msg.Sequence, msgd.Sequence);
        Assert.Equal("Open", msgd.Payload!.ToStr());

        msgd.Dispose();
        pk.TryDispose();

        var msg2 = new DefaultMessage { Kind = MessageKinds.Response, Sequence = 1 };
        msg2.SetBody("执行成功".GetBytes().AsPacket());
        var pk2 = codec.Build(msg2)!;
        Assert.Equal(0x81, pk2[0]);
        Assert.Equal(1, pk2[1]);
        Assert.Equal(12, pk2[2]);
        Assert.Equal(0, pk2[3]);
        var tail2 = pk2.Slice(4, -1);
        Assert.Equal("执行成功", tail2.ToStr());                 // 只读查看：共享切片需自行释放
        tail2.TryDispose();

        var rs2 = codec.TryParse(pk2.AsReadOnlySequence());
        Assert.NotNull(rs2);
        var msgd2 = Assert.IsType<DefaultMessage>(rs2.Value.Message);
        msgd2.SetBody(pk2.Slice(rs2.Value.HeaderSize, (Int32)rs2.Value.BodyLength));
        Assert.Equal(msg2.Flag, msgd2.Flag);
        Assert.Equal(msg2.Sequence, msgd2.Sequence);
        Assert.Equal("执行成功", msgd2.Payload!.ToStr());

        msgd2.Dispose();
        pk2.TryDispose();
    }

    //[Fact]
    //public void StringEncode()
    //{
    //    var msg = new DefaultMessage
    //    {
    //        Sequence = 1,
    //        Payload = "Open".GetBytes(),
    //    };
    //    var str = msg.Encode();
    //    Assert.Equal("4,1,1:Open", str);

    //    var msg2 = new DefaultMessage
    //    {
    //        Reply = true,
    //        Sequence = 1,
    //        Payload = "执行成功".GetBytes(),
    //    };
    //    var str2 = msg2.Encode();
    //    Assert.Equal("12,1,129:执行成功", str2);
    //}

    //[Fact]
    //public void StringEncodeNoFlag()
    //{
    //    var msg = new DefaultMessage
    //    {
    //        Sequence = 1,
    //        Payload = "Open".GetBytes(),
    //    };
    //    var str = msg.Encode(null, false);
    //    Assert.Equal("4,1:Open", str);

    //    var msg2 = new DefaultMessage
    //    {
    //        Reply = true,
    //        Sequence = 1,
    //        Payload = "执行成功".GetBytes(),
    //    };
    //    var str2 = msg2.Encode(null, false);
    //    Assert.Equal("12,1:执行成功", str2);
    //}

    //[Fact]
    //public void StringDecode()
    //{
    //    {
    //        var msg = new DefaultMessage();
    //        var rs = msg.Decode("4,1,1:Open");
    //        Assert.True(rs);
    //        Assert.Equal(1, msg.Sequence);
    //        Assert.Equal(1, msg.Flag);
    //        Assert.Equal("Open", msg.Payload.ToStr());
    //    }

    //    {
    //        var msg = new DefaultMessage();
    //        var rs = msg.Decode("12,1,129:执行成功");
    //        Assert.True(rs);
    //        Assert.Equal(1, msg.Sequence);
    //        Assert.Equal(0x81, msg.Flag);
    //        Assert.Equal("执行成功", msg.Payload.ToStr());
    //    }

    //    {
    //        var msg = new DefaultMessage();
    //        var rs = msg.Decode("12,1,129:执行成功".GetBytes());
    //        Assert.True(rs);
    //        Assert.Equal(1, msg.Sequence);
    //        Assert.Equal(0x81, msg.Flag);
    //        Assert.Equal("执行成功", msg.Payload.ToStr());
    //    }
    //}
}