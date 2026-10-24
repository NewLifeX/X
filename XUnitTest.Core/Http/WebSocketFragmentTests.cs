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
        Assert.True(f.TooBig);
    }

    [Fact]
    [DisplayName("分片重组_首片超限_丢弃并置TooBig")]
    public void Fragment_BeginTooBig()
    {
        // 首片同样受上限约束：置标记让调用方能按 RFC 6455 §7.4.1 以 1009 失败连接，而非静默吞掉
        var f = new WebSocketFragment { MaxFragments = 8 };

        f.Begin(WebSocketMessageType.Binary, new ArrayPacket(new Byte[9]));

        Assert.True(f.TooBig);
        Assert.False(f.Active);
    }

    [Fact]
    [DisplayName("分片重组_新序列开始_复位TooBig")]
    public void Fragment_NewSequence_ResetsTooBig()
    {
        // 标记随新序列复位，否则上一段的超限结论会误伤其后每一条消息
        var f = new WebSocketFragment { MaxFragments = 8 };
        f.Begin(WebSocketMessageType.Binary, new ArrayPacket(new Byte[4]));
        Assert.Null(f.Append(false, new ArrayPacket(new Byte[6])));
        Assert.True(f.TooBig);

        f.Begin(WebSocketMessageType.Text, new ArrayPacket(new Byte[1]));

        Assert.False(f.TooBig);
        Assert.True(f.Active);
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

    [Fact]
    [DisplayName("分片重组_首片超限_不开始累积")]
    public void OversizedFirstFragment_NotStarted()
    {
        var f = new WebSocketFragment { MaxFragments = 8 };

        // 首片自身就超限：不得进入累积状态（否则超大 FIN=0 首片会绕过上限、白分配一大块内存）
        f.Begin(WebSocketMessageType.Binary, new ArrayPacket(new Byte[9]));

        Assert.False(f.Active);

        // 无首片时，后续续片也拼不出消息
        Assert.Null(f.Append(true, new ArrayPacket(new Byte[2])));
    }

    [Fact]
    [DisplayName("分片重组_首片恰好达上限_正常累积")]
    public void FirstFragmentAtLimit_Accepted()
    {
        var f = new WebSocketFragment { MaxFragments = 8 };

        // 恰好等于上限仍应接受（超过上限才丢弃）
        f.Begin(WebSocketMessageType.Binary, new ArrayPacket(new Byte[8]));

        Assert.True(f.Active);
    }
}
