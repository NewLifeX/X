using System.ComponentModel;
using NewLife.Data;
using NewLife.Http;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Http;

/// <summary>WebSocket 分片重组器测试</summary>
[DisplayName("WebSocket分片重组")]
public class WebSocketFragmentTests
{
    [Fact]
    [DisplayName("分片重组_两片合并_完整还原")]
    public void TwoFragments_Merge()
    {
        var f = new WebSocketFragment();
        f.Begin(WebSocketMessageType.Text, new ArrayPacket("hel"u8.ToArray()));

        var msg = f.Append(true, new ArrayPacket("lo"u8.ToArray()));

        Assert.NotNull(msg);
        Assert.True(msg!.Fin);
        Assert.Equal(WebSocketMessageType.Text, msg.Type);
        Assert.Equal("hello", msg.Payload!.ToStr());
        Assert.False(f.Active);
    }

    [Fact]
    [DisplayName("分片重组_多片追加_末片合并")]
    public void ManyFragments_Merge()
    {
        var f = new WebSocketFragment();
        f.Begin(WebSocketMessageType.Binary, new ArrayPacket(new Byte[] { 1, 2 }));

        Assert.Null(f.Append(false, new ArrayPacket(new Byte[] { 3 })));
        Assert.Null(f.Append(false, new ArrayPacket(new Byte[] { 4 })));
        var msg = f.Append(true, new ArrayPacket(new Byte[] { 5 }));

        Assert.NotNull(msg);
        Assert.Equal(new Byte[] { 1, 2, 3, 4, 5 }, msg!.Payload!.ToArray());
    }

    [Fact]
    [DisplayName("分片重组_孤立续片_忽略返回空")]
    public void OrphanContinuation_Ignored()
    {
        var f = new WebSocketFragment();

        Assert.Null(f.Append(true, new ArrayPacket(new Byte[] { 1 })));
        Assert.False(f.Active);
    }

    [Fact]
    [DisplayName("分片重组_累计超限_丢弃整段")]
    public void OverLimit_Discard()
    {
        var f = new WebSocketFragment { MaxFragments = 8 };
        f.Begin(WebSocketMessageType.Binary, new ArrayPacket(new Byte[4]));

        Assert.Null(f.Append(false, new ArrayPacket(new Byte[6])));
        Assert.False(f.Active);
    }

    [Fact]
    [DisplayName("分片重组_新首片_丢弃未完成旧序列")]
    public void NewBegin_DiscardsOld()
    {
        var f = new WebSocketFragment();
        f.Begin(WebSocketMessageType.Text, new ArrayPacket("aa"u8.ToArray()));
        f.Begin(WebSocketMessageType.Binary, new ArrayPacket(new Byte[] { 9 }));

        var msg = f.Append(true, new ArrayPacket(new Byte[] { 8 }));

        Assert.NotNull(msg);
        Assert.Equal(WebSocketMessageType.Binary, msg!.Type);
        Assert.Equal(new Byte[] { 9, 8 }, msg.Payload!.ToArray());
    }
}
