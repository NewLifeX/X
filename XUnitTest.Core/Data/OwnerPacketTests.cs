using NewLife.Data;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Data;

/// <summary>OwnerPacket 专门单元测试</summary>
public class OwnerPacketTests
{
    #region 构造函数测试

    [Fact(DisplayName = "构造函数：创建指定长度的内存包")]
    public void Constructor_WithLength_ShouldCreatePacketCorrectly()
    {
        using var packet = new OwnerPacket(100);

        Assert.NotNull(packet.Buffer);
        Assert.True(packet.Buffer.Length >= 100);
        Assert.Equal(0, packet.Offset);
        Assert.Equal(100, packet.Length);
        Assert.Equal(100, packet.Total);
        Assert.Null(packet.Next);
        Assert.True((Boolean)packet.GetValue("_hasOwner"));
    }

    [Fact(DisplayName = "构造函数：零长度包")]
    public void Constructor_WithZeroLength_ShouldCreateEmptyPacket()
    {
        using var packet = new OwnerPacket(0);

        Assert.NotNull(packet.Buffer);
        Assert.Equal(0, packet.Length);
        Assert.Equal(0, packet.Total);
    }

    [Fact(DisplayName = "构造函数：使用现有缓冲区")]
    public void Constructor_WithExistingBuffer_ShouldCreatePacketCorrectly()
    {
        var buffer = new Byte[200];
        // 注意：使用 hasOwner = false 避免 ArrayPool 返回错误
        var packet = new OwnerPacket(buffer, 10, 50, false);
        try
        {
            Assert.Same(buffer, packet.Buffer);
            Assert.Equal(10, packet.Offset);
            Assert.Equal(50, packet.Length);
            Assert.Equal(50, packet.Total);
            Assert.False((Boolean)packet.GetValue("_hasOwner"));
        }
        finally
        {
            packet.Free(); // 不会尝试返回到 ArrayPool
        }
    }

    [Fact(DisplayName = "构造函数：头部扩展构造")]
    public void Constructor_WithHeaderExpansion_ShouldTransferOwnership()
    {
        // 创建一个从偏移位置开始的包，这样就有前置空间可以扩展
        var buffer = new Byte[200];
        var originalPacket = new OwnerPacket(buffer, 30, 50, false); // offset=30，这样前面有30字节可以扩展
        originalPacket.GetSpan().Fill(0x42);
        var expandSize = 20;

        try
        {
            var expandedPacket = new OwnerPacket(originalPacket, expandSize);
            
            try
            {
                Assert.Same(originalPacket.Buffer, expandedPacket.Buffer);
                Assert.Equal(originalPacket.Offset - expandSize, expandedPacket.Offset);
                Assert.Equal(originalPacket.Length + expandSize, expandedPacket.Length);

                // 验证所有权转移
                Assert.False((Boolean)originalPacket.GetValue("_hasOwner"));
                Assert.False((Boolean)expandedPacket.GetValue("_hasOwner"));
            }
            finally
            {
                expandedPacket.Free();
            }
        }
        finally
        {
            originalPacket.Free();
        }
    }

    #endregion

    #region 索引器测试

    [Fact(DisplayName = "索引器：正常读写操作")]
    public void Indexer_NormalAccess_ShouldWorkCorrectly()
    {
        using var packet = new OwnerPacket(100);
        
        packet[50] = (Byte)'X';
        Assert.Equal((Byte)'X', packet[50]);
    }

    [Fact(DisplayName = "索引器：链式包跨段访问")]
    public void Indexer_ChainedPackets_ShouldAccessAcrossSegments()
    {
        using var packet1 = new OwnerPacket(50);
        using var packet2 = new OwnerPacket(50);
        packet1.Next = packet2;

        packet1[25] = 0x11;
        packet1[75] = 0x22;

        Assert.Equal(0x11, packet1[25]);
        Assert.Equal(0x22, packet1[75]);
        Assert.Equal(0x22, packet2[25]);
    }

    #endregion

    #region 内存访问测试

    [Fact(DisplayName = "GetSpan：应返回正确的内存片段")]
    public void GetSpan_ShouldReturnCorrectSpan()
    {
        using var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);

        var span = packet.GetSpan();

        Assert.Equal(100, span.Length);
        Assert.True(span.ToArray().All(b => b == 0x42));
    }

    [Fact(DisplayName = "GetMemory：应返回正确的内存块")]
    public void GetMemory_ShouldReturnCorrectMemory()
    {
        using var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x55);

        var memory = packet.GetMemory();

        Assert.Equal(100, memory.Length);
        Assert.True(memory.Span.ToArray().All(b => b == 0x55));
    }

    [Fact(DisplayName = "TryGetArray：应成功获取数组段")]
    public void TryGetArray_ShouldReturnArraySegment()
    {
        using var packet = new OwnerPacket(100);

        var success = ((IPacket)packet).TryGetArray(out var segment);

        Assert.True(success);
        Assert.Same(packet.Buffer, segment.Array);
        Assert.Equal(packet.Offset, segment.Offset);
        Assert.Equal(packet.Length, segment.Count);
    }

    #endregion

    #region 大小调整测试

    [Fact(DisplayName = "Resize：调整到更小大小")]
    public void Resize_ToSmallerSize_ShouldAdjustLength()
    {
        using var packet = new OwnerPacket(100);

        var result = packet.Resize(50);

        Assert.Same(packet, result);
        Assert.Equal(50, packet.Length);
    }

    [Fact(DisplayName = "Resize：保持相同大小")]
    public void Resize_ToSameSize_ShouldKeepLength()
    {
        using var packet = new OwnerPacket(100);

        var result = packet.Resize(100);

        Assert.Same(packet, result);
        Assert.Equal(100, packet.Length);
    }

    #endregion

    #region 切片操作测试

    [Fact(DisplayName = "Slice：基本切片操作")]
    public void Slice_BasicOperation_ShouldCreateNewPacket()
    {
        var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);

        using var sliced = packet.Slice(20, 30) as OwnerPacket;

        Assert.NotNull(sliced);
        Assert.Same(packet.Buffer, sliced.Buffer);
        Assert.Equal(20, sliced.Offset);
        Assert.Equal(30, sliced.Length);

        // 验证所有权转移
        Assert.False((Boolean)packet.GetValue("_hasOwner"));
        Assert.True((Boolean)sliced!.GetValue("_hasOwner"));
    }

    [Fact(DisplayName = "Slice：不转移所有权")]
    public void Slice_WithoutOwnershipTransfer_ShouldRetainOwnership()
    {
        using var packet = new OwnerPacket(100);

        var sliced = packet.Slice(20, 30, transferOwner: false) as OwnerPacket;

        Assert.NotNull(sliced);
        Assert.True((Boolean)packet.GetValue("_hasOwner"));
        Assert.False((Boolean)sliced!.GetValue("_hasOwner"));
    }

    [Fact(DisplayName = "Slice：切片到末尾")]
    public void Slice_ToEnd_ShouldSliceToEndOfPacket()
    {
        var packet = new OwnerPacket(100);

        using var sliced = packet.Slice(30, -1) as OwnerPacket;

        Assert.NotNull(sliced);
        Assert.Equal(30, sliced.Offset);
        Assert.Equal(70, sliced.Length);
    }

    #endregion

    #region 属性测试

    [Fact(DisplayName = "Total：单个包应返回Length")]
    public void Total_SinglePacket_ShouldReturnLength()
    {
        using var packet = new OwnerPacket(100);

        Assert.Equal(100, packet.Total);
        Assert.Equal(packet.Length, packet.Total);
    }

    [Fact(DisplayName = "Total：链式包应返回所有段的总长度")]
    public void Total_ChainedPackets_ShouldReturnTotalLength()
    {
        using var packet1 = new OwnerPacket(50);
        using var packet2 = new OwnerPacket(30);
        using var packet3 = new OwnerPacket(20);
        packet1.Next = packet2;
        packet2.Next = packet3;

        Assert.Equal(100, packet1.Total);
    }

    #endregion

    #region 字符串表示测试

    [Fact(DisplayName = "ToString：应返回格式化的字符串表示")]
    public void ToString_ShouldReturnFormattedString()
    {
        using var packet = new OwnerPacket(100);

        var result = packet.ToString();

        Assert.Matches(@"\[\d+\]\(0, 100\)<100>", result);
    }

    #endregion

    #region 性能测试

    [Fact(DisplayName = "性能：切片操作应该零拷贝")]
    public void Performance_Slice_ShouldBeZeroCopy()
    {
        using var packet = new OwnerPacket(1000);
        packet.GetSpan().Fill(0x42);

        var slice1 = packet.Slice(100, 200, transferOwner: false);
        var slice2 = slice1.Slice(50, 100, transferOwner: false);

        // 验证数据一致性（间接验证零拷贝）
        Assert.Equal(0x42, slice2[0]);
        Assert.Equal(0x42, slice2[99]);
    }

    #endregion

    #region 边界条件测试

    [Fact(DisplayName = "边界条件：空包处理")]
    public void EdgeCase_EmptyPacket_ShouldHandleCorrectly()
    {
        using var packet = new OwnerPacket(0);

        Assert.Equal(0, packet.Length);
        Assert.Equal(0, packet.Total);
        Assert.Empty(packet.GetSpan().ToArray());
        Assert.Empty(packet.GetMemory().ToArray());
    }

    [Fact(DisplayName = "边界条件：大内存包处理")]
    public void EdgeCase_LargePacket_ShouldHandleCorrectly()
    {
        using var packet = new OwnerPacket(1024 * 1024); // 1MB

        Assert.Equal(1024 * 1024, packet.Length);
        Assert.True(packet.Buffer.Length >= 1024 * 1024);

        // 测试边界访问
        packet[0] = 0x11;
        packet[packet.Length - 1] = 0x22;
        Assert.Equal(0x11, packet[0]);
        Assert.Equal(0x22, packet[packet.Length - 1]);
    }

    #endregion

    #region 内存管理测试

    [Fact(DisplayName = "Free：应放弃所有权")]
    public void Free_ShouldAbandonOwnership()
    {
        var packet = new OwnerPacket(100);

        packet.Free();

        Assert.False((Boolean)packet.GetValue("_hasOwner"));
        Assert.Null(packet.Next);
    }

    [Fact(DisplayName = "内存管理：ArrayPool复用验证")]
    public void MemoryManagement_ArrayPoolReuse_ShouldReuseBuffers()
    {
        var packets = new List<OwnerPacket>();

        // 创建多个相同大小的包
        for (var i = 0; i < 10; i++)
        {
            var packet = new OwnerPacket(1024);
            packets.Add(packet);
        }

        // 释放前几个（使用 using 语句会自动释放）
        foreach (var p in packets.Take(5))
        {
            p.Free(); // 使用 Free 而不是 Dispose
        }

        // 创建新的包，应该能复用缓冲区
        using var newPacket = new OwnerPacket(1024);

        Assert.NotNull(newPacket.Buffer);
        Assert.True(newPacket.Buffer.Length >= 1024);

        // 清理剩余的包（使用 using 语句会自动释放）
        foreach (var p in packets.Skip(5))
        {
            p.Free(); // 使用 Free 而不是 Dispose
        }
    }

    #endregion
}