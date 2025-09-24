using System.Text;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

public class PacketHelperTests
{
    #region ToStr 单包
    [Fact]
    public void ToStr_SinglePacket_Default()
    {
        var str = "StoneNewLife";
        IPacket pk = new ArrayPacket(str.GetBytes());

        var rs = pk.ToStr();
        Assert.Equal(str, rs);
    }

    [Fact]
    public void ToStr_SinglePacket_OffsetCount()
    {
        var str = "StoneNewLife"; // 12
        IPacket pk = new ArrayPacket(str.GetBytes());

        Assert.Equal("oneNew", pk.ToStr(null, 2, 6));
        Assert.Equal("", pk.ToStr(null, 100, 5));
        Assert.Equal("", pk.ToStr(null, 2, 0));
        // count 超出可用，自动截断
        Assert.Equal(str[2..], pk.ToStr(null, 2, 999));
        // count -1 直到末尾
        Assert.Equal(str[2..], pk.ToStr(null, 2, -1));
    }

    [Fact]
    public void ToStr_SinglePacket_Encoding()
    {
        var str = "新生命Team";
        var buf = Encoding.Unicode.GetBytes(str);
        IPacket pk = new ArrayPacket(buf);

        var rs = pk.ToStr(Encoding.Unicode);
        Assert.Equal(str, rs);
    }

    [Fact]
    public void ToStr_SinglePacket_Empty()
    {
        IPacket pk = new ArrayPacket(Array.Empty<byte>());
        Assert.Equal(string.Empty, pk.ToStr());
    }
    #endregion

    #region ToStr 多链
    [Fact]
    public void ToStr_MultiChain_Basic()
    {
        IPacket pk = new ArrayPacket("Stone".GetBytes())
            .Append("New".GetBytes())
            .Append("Life".GetBytes());

        Assert.Equal("StoneNewLife", pk.ToStr());
    }

    [Fact]
    public void ToStr_MultiChain_OffsetCrossBoundary()
    {
        IPacket pk = new ArrayPacket("Stone".GetBytes())
            .Append("NewLife".GetBytes()); // 5 + 7

        Assert.Equal("neNew", pk.ToStr(null, 3, 5)); // 从第一段后部跨越到第二段
        Assert.Equal("NewL", pk.ToStr(null, 5, 4));  // 精确落在第二段开头
        Assert.Equal("NewLife", pk.ToStr(null, 5, -1));
    }

    [Fact]
    public void ToStr_MultiChain_CountStopsEarly()
    {
        IPacket pk = new ArrayPacket("Stone".GetBytes())
            .Append("NewLife".GetBytes());

        // 指定长度比剩余少，提前结束
        Assert.Equal("oneNe", pk.ToStr(null, 2, 5));
    }

    [Fact]
    public void ToStr_NullPacketExtensionCall()
    {
        IPacket? pk = null;
        // 扩展方法允许空实例调用，方法内部返回 null
        var rs = PacketHelper.ToStr(pk!, null, 0, -1);
        Assert.Null(rs);
    }
    #endregion

    #region Append
    [Fact]
    public void Append_PacketAndBytes()
    {
        IPacket pk = new ArrayPacket("Stone".GetBytes());
        pk = pk.Append("NewLife".GetBytes());
        pk = pk.Append(new ArrayPacket("Framework".GetBytes()));

        Assert.Equal("StoneNewLifeFramework", pk.ToStr());
        Assert.NotNull(pk.Next);
        Assert.Equal(5 + 7 + 9, pk.Total);
    }
    #endregion

    #region ToHex 单包
    [Fact]
    public void ToHex_SinglePacket_DefaultLimit()
    {
        // 40 字节 > 默认 32
        var data = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();
        IPacket pk = new ArrayPacket(data);

        var hex = pk.ToHex(); // 默认 maxLength=32
        Assert.Equal(32 * 2, hex.Length); // 无分隔符
        var full = data.ToHex();
        Assert.Equal(full.Substring(0, 64), hex);
    }

    [Fact]
    public void ToHex_SinglePacket_MaxLengthAll()
    {
        var data = Enumerable.Range(0, 20).Select(i => (byte)(i + 1)).ToArray();
        IPacket pk = new ArrayPacket(data);

        var hex = pk.ToHex(-1); // 全部
        Assert.Equal(data.ToHex(), hex);
    }

    [Fact]
    public void ToHex_SinglePacket_SeparatorEveryByte()
    {
        var data = new byte[] { 0x01, 0xAB, 0x10, 0xFF };
        IPacket pk = new ArrayPacket(data);

        var hex = pk.ToHex( -1, "-", 0); // groupSize=0 => 每字节
        var expected = data.ToHex("-", 0, -1); // 使用 Byte[] 扩展生成基准
        Assert.Equal(expected, hex);
    }

    [Fact]
    public void ToHex_SinglePacket_GroupSize()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
        IPacket pk = new ArrayPacket(data);

        var hex = pk.ToHex(-1, "-", 4); // groupSize=4 插入一次
        // 手工构造：前4字节无分隔开头，之后加一次分隔再跟随后2字节
        var expected = string.Join(null, new[]
        {
            data[0..4].ToHex(),
            "-",
            data[4..6].ToHex()
        });
        Assert.Equal(expected, hex);
    }

    [Fact]
    public void ToHex_SinglePacket_MaxLengthZero()
    {
        var data = new byte[] { 1, 2, 3 };
        IPacket pk = new ArrayPacket(data);
        Assert.Equal(string.Empty, pk.ToHex(0));
    }

    [Fact]
    public void ToHex_SinglePacket_Empty()
    {
        IPacket pk = new ArrayPacket(Array.Empty<byte>());
        Assert.Equal(string.Empty, pk.ToHex());
    }
    #endregion

    #region ToHex 多链
    [Fact]
    public void ToHex_MultiChain_CrossSegmentsMaxLength()
    {
        var seg1 = Enumerable.Range(0, 10).Select(i => (byte)i).ToArray();
        var seg2 = Enumerable.Range(10, 10).Select(i => (byte)i).ToArray();
        IPacket pk = new ArrayPacket(seg1).Append(seg2);

        var hex = pk.ToHex(15); // 只取前15字节
        var expected = seg1.Concat(seg2).Take(15).ToArray().ToHex();
        Assert.Equal(expected, hex);
    }

    [Fact]
    public void ToHex_MultiChain_SeparatorAndGroup()
    {
        var seg1 = new byte[] { 0x01, 0x02, 0x03 };
        var seg2 = new byte[] { 0x04, 0x05, 0x06, 0x07 };
        IPacket pk = new ArrayPacket(seg1).Append(seg2);

        // groupSize=2 ，应在第2、4、6...字节后边界前插入分隔（实现里在满足 i%groupSize==0 的位置插入到下一个字节前）
        var hex = pk.ToHex(-1, ":", 2);

        // 组合所有字节后使用 SpanHelper 的逻辑分段构造期望值（简单方式：拼整体后用相同 API 生成）
        var all = seg1.Concat(seg2).ToArray();
        var expected = all.AsSpan().ToHex(":", 2, -1);
        Assert.Equal(expected, hex);
    }
    #endregion

    #region 新增边界 & 防护测试
    [Fact]
    public void ToHex_FirstSegmentEmpty_UsesFollowingSegments()
    {
        IPacket pk = new ArrayPacket(Array.Empty<byte>()); // 首段空
        pk = pk.Append("ABC".GetBytes());
        var hex = pk.ToHex(-1);
        Assert.Equal("414243", hex); // ABC
    }

    [Fact]
    public void ToHex_FirstSegmentEmpty_WithMaxLength()
    {
        IPacket pk = new ArrayPacket(Array.Empty<byte>())
            .Append(new byte[] { 0x01, 0x02, 0x03 });
        var hex = pk.ToHex(2); // 仅前2字节
        Assert.Equal("0102", hex);
    }

    [Fact]
    public void ToHex_NegativeGroupSize_TreatedAsEveryByte()
    {
        var data = new byte[] { 0xAA, 0xBB };
        IPacket pk = new ArrayPacket(Array.Empty<byte>()).Append(data);
        var hex = pk.ToHex(-1, "-", -5); // groupSize<0 等价每字节
        Assert.Equal("AA-BB", hex);
    }

    [Fact]
    public void ToStr_NegativeOffset_TreatedAsZero()
    {
        IPacket pk = new ArrayPacket("Hello".GetBytes());
        var rs = pk.ToStr(null, -10, 2);
        Assert.Equal("He", rs);
    }

    [Fact]
    public void ToStr_MultiChain_OffsetBeyondTotal_ReturnsEmpty()
    {
        IPacket pk = new ArrayPacket("Hello".GetBytes()).Append("World".GetBytes()); // 10
        var rs = pk.ToStr(null, 50, 5);
        Assert.Equal(string.Empty, rs);
    }

    [Fact]
    public void ToStr_CountZero_ReturnsEmpty()
    {
        IPacket pk = new ArrayPacket("Data".GetBytes());
        var rs = pk.ToStr(null, 1, 0);
        Assert.Equal(string.Empty, rs);
    }

    [Fact]
    public void Append_SelfAppend_NoLoop()
    {
        IPacket pk = new ArrayPacket("Loop".GetBytes());
        pk = pk.Append(pk); // 自连接，预期被忽略
        Assert.Null(pk.Next);
    }

    [Fact]
    public void Append_RingBrokenSafely()
    {
        // 使用引用类型 OwnerPacket 构造环
        IPacket pk = new OwnerPacket(3).Resize(3); // 长度 3
        var second = new OwnerPacket(2).Resize(2);
        pk.Append(second);
        // 人为制造环：second.Next 指回首节点
        second.Next = pk;

        // 追加第三段，预期算法检测环并在环处终止，然后挂接新段
        var third = new OwnerPacket(1).Resize(1);
        pk.Append(third);

        // 遍历不应死循环，限制步数
        var steps = 0;
        for (var p = pk; p != null && steps < 10; p = p.Next) steps++;
        Assert.True(steps <= 5, "链表遍历步骤异常，可能存在环");
    }
    #endregion
}
