using System;
using System.ComponentModel;
using CompatLib;
using Xunit;

namespace XUnitTest.Compat;

/// <summary>垫片类型跨资产转发验证：CompatLib 以 netstandard2.0 编译（引用 NewLife.Core 垫片），
/// net8.0 腿运行时通过 TypeForwardedTo 解析到 BCL，net462 腿直接使用垫片</summary>
public class ForwardingTests
{
    [Fact]
    [DisplayName("ShimProbe_跨资产引用垫片类型_两种资产下运行结果一致")]
    public void ShimProbe_Runs()
    {
        var first = new Byte[] { 0x12 };
        var second = new Byte[] { 0x34, 0x78, 0x56, 0x34, 0x12 };

        var expected = ((Int64)0x1234 << 32) ^ 0x12345678;
        Assert.Equal(expected, ShimProbe.ReadHeader(first, second));
    }

    [Fact]
    [DisplayName("ShimProbe_无符号端序扩展_返回正确大端值")]
    public void Helper_Runs()
    {
        Assert.Equal((UInt16)0xABCD, ShimProbe.ReadUInt16(new Byte[] { 0xAB, 0xCD }));
    }
}
