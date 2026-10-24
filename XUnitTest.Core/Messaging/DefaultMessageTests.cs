using System.Buffers;
using System.ComponentModel;
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

    [Fact]
    [DisplayName("消息头部_非法负载长度_抛参数越界")]
    public void WriteHeader_InvalidBodyLength()
    {
        var msg = new DefaultMessage { Sequence = 1 };
        var header = new Byte[8];

        // 非法长度：旧实现写入 FF FF（被解析侧当成 8 字节扩展头）或静默截断长度字段
        Assert.Throws<ArgumentOutOfRangeException>(() => msg.WriteHeader(header, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => msg.WriteHeader(header, Int32.MaxValue + 1L));

        // 边界值可正常写入：0 用 4 字节头
        Assert.Equal(4, msg.WriteHeader(header, 0));

        // 32 位上限用 8 字节扩展头，回读长度不被截断
        var codec = new SrmpCodec();
        var pk = codec.BuildHeader(msg, Int32.MaxValue);
        Assert.Equal(8, pk.Total);
        var rs = codec.TryParse(pk.AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(Int32.MaxValue, rs.Value.BodyLength);
        pk.TryDispose();
    }

    [Fact]
    [DisplayName("标准编解码_头部未到齐_返回空且不产生对象")]
    public void TryParse_IncompleteHeader_NoAllocation()
    {
        var codec = new SrmpCodec();

        // 4 字节定长头只到 3 字节
        var partial = new ReadOnlySequence<Byte>(new Byte[] { 0x01, 0x01, 0x04 });
        Assert.Null(codec.TryParse(partial));

        var memory = GC.GetAllocatedBytesForCurrentThread();
        var rs = codec.TryParse(partial);
        var used = GC.GetAllocatedBytesForCurrentThread() - memory;

        Assert.Null(rs);
        Assert.True(used == 0, $"头部未到齐时不应产生对象，本次分配 {used} 字节");

        // 8 字节扩展头：0xFFFF 标记已到，但 4 字节正式长度未到齐
        var ext = new ReadOnlySequence<Byte>(new Byte[] { 0x01, 0x01, 0xFF, 0xFF, 0x00, 0x00 });
        Assert.Null(codec.TryParse(ext));

        var memory2 = GC.GetAllocatedBytesForCurrentThread();
        var rs2 = codec.TryParse(ext);
        var used2 = GC.GetAllocatedBytesForCurrentThread() - memory2;

        Assert.Null(rs2);
        Assert.True(used2 == 0, $"扩展头未到齐时不应产生对象，本次分配 {used2} 字节");
    }

    [Fact]
    [DisplayName("标准消息_实例解析_字段就位且失败路径无副作用")]
    public void TryParse_Instance_FillsFields()
    {
        // 4 字节头：Flag=3 Sequence=0x07 len=4
        var frame = new ReadOnlySequence<Byte>(new Byte[] { 0x03, 0x07, 0x04, 0x00, 0x41, 0x42, 0x43, 0x44 });
        var msg = new DefaultMessage();
        Assert.True(msg.TryParse(frame, out var bodyLength, out var headerSize, out var invalid));
        Assert.False(invalid);
        Assert.Equal(4, headerSize);
        Assert.Equal(4L, bodyLength);
        Assert.Equal(0x03, msg.Flag);
        Assert.Equal(0x07, msg.Sequence);
        Assert.Equal(MessageKinds.Request, msg.Kind);

        // 数据不足：不写入实例
        var keep = new DefaultMessage { Flag = 0x22, Sequence = 0x55, Kind = MessageKinds.Response };
        Assert.False(keep.TryParse(new ReadOnlySequence<Byte>(new Byte[] { 0x01, 0x02 }), out _, out _, out var invalid1));
        Assert.False(invalid1);
        Assert.Equal(0x22, keep.Flag);
        Assert.Equal(0x55, keep.Sequence);
        Assert.Equal(MessageKinds.Response, keep.Kind);

        // 扩展长度非法（负数）：标记损坏，同样不写入实例
        var bad = new ReadOnlySequence<Byte>(new Byte[] { 0x81, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
        Assert.False(keep.TryParse(bad, out _, out _, out var invalid2));
        Assert.True(invalid2);
        Assert.Equal(0x55, keep.Sequence);
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