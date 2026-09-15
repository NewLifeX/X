using System.Runtime.CompilerServices;
using NewLife;
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
        Assert.NotNull(packet.GetValue("_owner"));
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
            Assert.Null(packet.GetValue("_owner"));
        }
        finally
        {
            packet.Dispose(); // 借用视图 Dispose 无操作，不会尝试返回到 ArrayPool
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
                Assert.Same(buffer, expandedPacket.Buffer);
                Assert.Equal(30 - expandSize, expandedPacket.Offset);
                Assert.Equal(50 + expandSize, expandedPacket.Length);

                // 验证所有权移动：源实例整体作废
                Assert.Null(originalPacket.GetValue("_owner"));
                Assert.Null(originalPacket.GetValue("_buffer"));
                Assert.Null(expandedPacket.GetValue("_owner"));
            }
            finally
            {
                expandedPacket.Dispose();
            }
        }
        finally
        {
            originalPacket.Dispose();
        }
    }

    [Fact(DisplayName = "构造函数：Stream窃取MemoryStream内部缓冲区")]
    public void Constructor_WithExposableMemoryStream_ShouldStealBuffer()
    {
        var originalData = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var ms = new MemoryStream();
        ms.Write(originalData, 0, originalData.Length);
        ms.Position = 0;

        Assert.True(ms.TryGetBuffer(out var seg));

        var packet = new OwnerPacket(ms);

        Assert.Same(seg.Array, packet.Buffer);
        Assert.Equal(seg.Offset, packet.Offset);
        Assert.Equal(seg.Count, packet.Length);

        var span = packet.GetSpan();
        for (var i = 0; i < originalData.Length; i++)
        {
            Assert.Equal(originalData[i], span[i]);
        }

        Assert.Equal(0, ms.Position);

        packet.Dispose();
    }

    [Fact(DisplayName = "构造函数：Stream窃取时Position参与偏移计算")]
    public void Constructor_WithExposableMemoryStreamAndPosition_ShouldUsePosition()
    {
        var ms = new MemoryStream();
        for (var i = 0; i < 10; i++) ms.WriteByte((Byte)(0xA0 + i));
        ms.Position = 3;

        Assert.True(ms.TryGetBuffer(out var seg));

        var packet = new OwnerPacket(ms);

        Assert.Same(seg.Array, packet.Buffer);
        Assert.Equal(seg.Offset + 3, packet.Offset);
        Assert.Equal(7, packet.Length);

        var span = packet.GetSpan();
        for (var i = 0; i < 7; i++)
        {
            Assert.Equal((Byte)(0xA3 + i), span[i]);
        }

        Assert.Equal(3, ms.Position);

        packet.Dispose();
    }

    [Fact(DisplayName = "构造函数：Stream在不可窃取时走池拷贝")]
    public void Constructor_WithNonExposableMemoryStream_ShouldCopyFromPool()
    {
        var originalData = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var ms = new MemoryStream(originalData);
        ms.Position = 0;

        Assert.False(ms.TryGetBuffer(out _));

        var packet = new OwnerPacket(ms);

        Assert.NotSame(originalData, packet.Buffer);
        Assert.Equal(5, packet.Length);

        var span = packet.GetSpan();
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(originalData[i], span[i]);
        }

        Assert.Equal(0, ms.Position);

        packet.Dispose();
    }

    [Fact(DisplayName = "构造函数：Stream大数据池拷贝完整性")]
    public void Constructor_WithLargeNonExposableMemoryStream_ShouldCopyIntegrity()
    {
        var largeData = new Byte[2000];
        for (var i = 0; i < largeData.Length; i++) largeData[i] = (Byte)(i % 256);

        var ms = new MemoryStream(largeData);

        var packet = new OwnerPacket(ms);
        Assert.Equal(2000, packet.Length);
        Assert.NotSame(largeData, packet.Buffer);

        var span = packet.GetSpan();
        for (var i = 0; i < 2000; i++)
        {
            Assert.Equal(largeData[i], span[i]);
        }

        Assert.Equal(0, ms.Position);
        packet.Dispose();
    }

    [Fact(DisplayName = "构造函数：从空流创建")]
    public void Constructor_WithEmptyStream_ShouldCreateEmptyPacket()
    {
        var ms1 = new MemoryStream(Array.Empty<Byte>());
        var packet1 = new OwnerPacket(ms1);
        Assert.Equal(0, packet1.Length);
        packet1.Dispose();

        var ms2 = new MemoryStream();
        Assert.True(ms2.TryGetBuffer(out _));
        var packet2 = new OwnerPacket(ms2);
        Assert.Equal(0, packet2.Length);
        packet2.Dispose();
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

        var success = packet.TryGetArray(out var segment);

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
        using var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);
        var buffer = packet.Buffer;

        using var sliced = packet.Slice(20, 30);

        Assert.NotNull(sliced);
        Assert.Same(buffer, sliced.Buffer);
        Assert.Equal(20, sliced.Offset);
        Assert.Equal(30, sliced.Length);

        // 共享语义：源实例保持可用，双方共享同一引用计数对象
        Assert.NotNull(packet.GetValue("_owner"));
        Assert.NotNull(packet.GetValue("_buffer"));
        Assert.Same(packet.GetValue("_owner"), sliced!.GetValue("_owner"));
        Assert.Equal(2, packet.RefCount);
        Assert.Equal(0x42, sliced[0]);
    }

    [Fact(DisplayName = "RefCount：追踪共享句柄数量")]
    public void RefCount_ShouldTrackSharedHandles()
    {
        var packet = new OwnerPacket(100);
        Assert.Equal(1, packet.RefCount);

        var first = packet.Slice(0, 50);
        var second = packet.Slice(50, 50);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(3, packet.RefCount);
        Assert.Equal(3, first!.RefCount);

        first.Dispose();
        Assert.Equal(2, packet.RefCount);

        second!.Dispose();
        Assert.Equal(1, packet.RefCount);

        packet.Dispose();
        Assert.Equal(0, packet.RefCount);
    }

    [Fact(DisplayName = "Slice：切片到末尾")]
    public void Slice_ToEnd_ShouldSliceToEndOfPacket()
    {
        using var packet = new OwnerPacket(100);

        using var sliced = packet.Slice(30, -1);

        Assert.NotNull(sliced);
        Assert.Equal(30, sliced.Offset);
        Assert.Equal(70, sliced.Length);
        Assert.Equal(2, packet.RefCount);
    }

    [Fact(DisplayName = "Slice：IOwnerPacket 接口返回拥有句柄，可 using 释放")]
    public void Slice_IOwnerPacketInterface_ShouldReturnOwnedHandle()
    {
        var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);

        IOwnerPacket owner = packet;
        using (var view = owner.Slice(20, 30))
        {
            Assert.IsType<OwnerPacket>(view);
            Assert.Equal(30, view.Length);
            Assert.Equal(0x42, view.GetSpan()[0]);
            Assert.Equal(0x42, view[19]);

            // 视图与源各自持有引用，双方同时可用
            Assert.Equal(2, packet.RefCount);
        }

        // 视图释放后源仍可使用
        Assert.Equal(1, packet.RefCount);
        Assert.Equal(0x42, packet[0]);

        packet.Dispose();
    }

    [Fact(DisplayName = "Slice：经 IPacket 接口访问保持原有契约")]
    public void Slice_IPacketInterface_ShouldKeepContract()
    {
        var packet = new OwnerPacket(100);

        // 接口访问仍返回 IPacket，切片结果实际是拥有句柄
        IPacket view = ((IPacket)packet).Slice(10, 20);

        Assert.IsAssignableFrom<IOwnerPacket>(view);
        Assert.Equal(20, view.Length);
        Assert.Equal(2, packet.RefCount);

        view.TryDispose();
        Assert.Equal(1, packet.RefCount);

        packet.Dispose();
    }

    /// <summary>链式共享切片只递增窗口覆盖段的引用计数，窗口外的段保持不变</summary>
    /// <remarks>
    /// 场景：first(2字节) + second(8字节) 组成链，从偏移 4 开始切片——起点落在 second 段内。
    /// 期望：second 引用计数 +1（共享），first 保持不变；各句柄各自释放，最后一个归零归还池。
    /// </remarks>
    [Fact(DisplayName = "Slice：链式共享切片只递增窗口覆盖段的引用计数")]
    public void Slice_ChainShare_ShouldOnlyAddRefCoveredSegments()
    {
        var first = new OwnerPacket(2);
        var second = new OwnerPacket(8);
        first.Next = second;

        var firstOwner = first.GetValue("_owner");
        var secondOwner = second.GetValue("_owner");

        var slice = first.Slice(4, -1);

        Assert.NotNull(slice);
        Assert.Equal(6, slice!.Total);

        // 窗口外的 first：引用计数不变；second 在窗口内：引用计数 +1
        Assert.Equal(1, (Int32)firstOwner!.GetValue("_refCount")!);
        Assert.Equal(2, (Int32)secondOwner!.GetValue("_refCount")!);
        Assert.Same(secondOwner, slice.GetValue("_owner"));

        // 切片独立释放：second 回到 1
        slice.Dispose();
        Assert.Equal(1, (Int32)secondOwner.GetValue("_refCount")!);

        // 源链释放：链式递归归零
        first.Dispose();
        Assert.Equal(0, (Int32)firstOwner.GetValue("_refCount")!);
        Assert.Equal(0, (Int32)secondOwner.GetValue("_refCount")!);
    }

    /// <summary>链式共享切片：窗口未覆盖的尾部段引用计数不变，由源链继续持有</summary>
    /// <remarks>窗口只覆盖 first 段时，second 整段在窗口之外，不参与引用计数变化。</remarks>
    [Fact(DisplayName = "Slice：链式共享切片不触碰窗口外的尾部段")]
    public void Slice_ChainShare_ShouldNotTouchTailSegment()
    {
        var first = new OwnerPacket(2);
        var second = new OwnerPacket(8);
        first.Next = second;

        var firstOwner = first.GetValue("_owner");
        var secondOwner = second.GetValue("_owner");

        var slice = first.Slice(0, 2);

        Assert.NotNull(slice);
        Assert.Equal(2, slice!.Total);
        Assert.Equal(2, (Int32)firstOwner!.GetValue("_refCount")!);
        Assert.Equal(1, (Int32)secondOwner!.GetValue("_refCount")!);

        slice.Dispose();
        first.Dispose();
        Assert.Equal(0, (Int32)firstOwner.GetValue("_refCount")!);
        Assert.Equal(0, (Int32)secondOwner.GetValue("_refCount")!);
    }

    /// <summary>共享切片不改变源实例：长度、链、数据访问全部保持可用</summary>
    [Fact(DisplayName = "Slice：共享切片不改变源实例")]
    public void Slice_ShouldNotVoidSource()
    {
        var single = new OwnerPacket(100);
        var slice = single.Slice(10, 20);

        Assert.NotNull(slice);
        Assert.Equal(100, single.Length);
        Assert.Equal(100, single.Total);
        Assert.NotNull(single.GetValue("_buffer"));
        Assert.NotNull(single.GetValue("_owner"));
        Assert.Equal(20, slice!.Length);

        slice.Dispose();
        single.Dispose();
    }

    /// <summary>共享句柄与原实例同时持有引用，各自释放，最后一个释放才归还</summary>
    [Fact(DisplayName = "Slice：共享句柄双方独立使用与释放")]
    public void Slice_TwoHandles_ShouldBeIndependent()
    {
        var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);

        var shared = packet.Slice(0);
        var owner = packet.GetValue("_owner");

        // 双方引用计数为 2，均可读取
        Assert.Equal(2, (Int32)owner!.GetValue("_refCount")!);
        Assert.Equal(0x42, shared[10]);
        Assert.Equal(0x42, packet[10]);

        // 先释放共享句柄：源仍可使用
        shared.TryDispose();
        Assert.Equal(1, (Int32)owner.GetValue("_refCount")!);
        Assert.Equal(0x42, packet[10]);

        // 再释放源：归零归还
        packet.Dispose();
        Assert.Equal(0, (Int32)owner.GetValue("_refCount")!);
    }

    /// <summary>共享窗口可跨链段，按段递增引用计数</summary>
    [Fact(DisplayName = "Slice：窗口共享跨链段递增引用计数")]
    public void Slice_Window_ShouldCoverChainedNodes()
    {
        var first = new OwnerPacket(4);
        var second = new OwnerPacket(8);
        first.Next = second;
        first.GetSpan().Fill(0x11);
        second.GetSpan().Fill(0x22);

        var shared = first.Slice(2, 8);

        Assert.Equal(8, shared.Total);
        Assert.Equal(0x11, shared[0]);
        Assert.Equal(0x22, shared[2]);
        Assert.Equal(0x22, shared[7]);

        var firstOwner = first.GetValue("_owner");
        var secondOwner = second.GetValue("_owner");
        Assert.Equal(2, (Int32)firstOwner!.GetValue("_refCount")!);
        Assert.Equal(2, (Int32)secondOwner!.GetValue("_refCount")!);

        // 共享句柄独立释放：源链引用保持不变
        shared.TryDispose();
        Assert.Equal(1, (Int32)firstOwner.GetValue("_refCount")!);
        Assert.Equal(1, (Int32)secondOwner.GetValue("_refCount")!);

        // 源链释放：链式递归归零
        first.Dispose();
        Assert.Equal(0, (Int32)firstOwner.GetValue("_refCount")!);
        Assert.Equal(0, (Int32)secondOwner.GetValue("_refCount")!);
    }

    /// <summary>原地前移窗口：不分配、不调整引用计数</summary>
    [Fact(DisplayName = "Skip：原地前移窗口")]
    public void Skip_ShouldAdvanceWindowInPlace()
    {
        var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);
        var offset = packet.Offset;

        var rs = packet.Skip(10);

        Assert.Same(packet, rs);
        Assert.Equal(offset + 10, packet.Offset);
        Assert.Equal(90, packet.Length);
        Assert.Equal(90, packet.Total);
        Assert.Equal(0x42, packet[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => packet.Skip(91));
    }

    /// <summary>兼容三参切片：直接转发到两参重载，true 与 false 行为一致（引用计数共享）</summary>
    [Fact(DisplayName = "Slice：三参兼容转发到两参")]
    public void SliceCompat_TransferOwner_ShouldForwardToShare()
    {
#pragma warning disable CS0618 // 三参重载为兼容旧版二进制保留，此处验证其转发行为
        var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);

        // 捕获引用计数对象用于观测
        var owner = packet.GetValue("_owner");

        // 经接口调用三参重载（旧版编译的库走的就是接口调用），行为与两参一致
        var shared = ((IPacket)packet).Slice(8, 16, true);

        Assert.Equal(16, shared.Length);
        Assert.Equal(0x42, shared[0]);
        Assert.Equal(0x42, shared[15]);
        Assert.Equal(0x42, packet[10]);

        // 共享模型：双方各自持有引用
        Assert.Equal(2, (Int32)owner!.GetValue("_refCount")!);

        // 双方独立释放，最后一个释放时归零归还
        shared.TryDispose();
        Assert.Equal(1, (Int32)owner.GetValue("_refCount")!);
        Assert.Equal(0x42, packet[10]);
        packet.TryDispose();
        Assert.Equal(0, (Int32)owner.GetValue("_refCount")!);
#pragma warning restore CS0618
    }

    /// <summary>兼容三参切片：转发到两参（共享），调用方遗漏释放也不悬空</summary>
    [Fact(DisplayName = "Slice：三参兼容借用按共享处理")]
    public void SliceCompat_Share_ShouldKeepBothHandles()
    {
#pragma warning disable CS0618 // 三参重载为兼容旧版二进制保留
        var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);

        // 旧版“借用视图”用法：调用方（如 HttpMessage）不会释放返回的切片
        var header = ((IPacket)packet).Slice(0, 10, false);
        var payload = ((IPacket)packet).Slice(10, -1, false);

        var owner = packet.GetValue("_owner");
        Assert.Equal(3, (Int32)owner!.GetValue("_refCount")!);
        Assert.Equal(0x42, header[0]);
        Assert.Equal(0x42, payload[0]);

        // 即使调用方遗漏释放，原句柄释放后数据仍由切片引用撐住
        packet.TryDispose();
        Assert.Equal(2, (Int32)owner.GetValue("_refCount")!);
        Assert.Equal(0x42, payload[5]);

        header.TryDispose();
        payload.TryDispose();
        Assert.Equal(0, (Int32)owner.GetValue("_refCount")!);
#pragma warning restore CS0618
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

        var slice1 = packet.Slice(100, 200);
        var slice2 = slice1.Slice(50, 100);

        // 验证数据一致性（间接验证零拷贝）
        Assert.Equal(0x42, slice2[0]);
        Assert.Equal(0x42, slice2[99]);

        slice2.TryDispose();
        slice1.TryDispose();
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

    [Fact(DisplayName = "Dispose：应归还缓冲区")]
    public void Dispose_ShouldReturnBufferToPool()
    {
        var packet = new OwnerPacket(100);
        Assert.NotNull(packet.GetValue("_owner"));

        packet.Dispose();

        Assert.Null(packet.GetValue("_owner"));
        Assert.Null(packet.GetValue("_buffer"));
        Assert.Null(packet.Next);
    }

    [Fact(DisplayName = "Dispose：多次调用应幂等")]
    public void Dispose_MultipleCalls_ShouldBeIdempotent()
    {
        var packet = new OwnerPacket(100);

        // 多次 Dispose 不应抛异常
        packet.Dispose();
        packet.Dispose();
        packet.Dispose();

        Assert.Null(packet.GetValue("_owner"));
    }

    [Fact(DisplayName = "Dispose：无所有权时不归还缓冲区")]
    public void Dispose_WithoutOwnership_ShouldNotReturnBuffer()
    {
        var buffer = new Byte[200];
        var packet = new OwnerPacket(buffer, 10, 50, false);

        // 无所有权的 Dispose 不应报错
        packet.Dispose();

        Assert.Null(packet.GetValue("_owner"));
    }

    [Fact(DisplayName = "Dispose：应释放链式后续节点")]
    public void Dispose_ShouldDisposeChainedNextPackets()
    {
        var packet1 = new OwnerPacket(50);
        var packet2 = new OwnerPacket(50);
        packet1.Next = packet2;

        packet1.Dispose();

        Assert.Null(packet1.Next);
        Assert.Null(packet2.GetValue("_owner"));
    }

    [Fact(DisplayName = "Dispose：借用视图头节点释放链上拥有尾链")]
    public void Dispose_BorrowHeadReleasesOwnedTail()
    {
        var buffer = new Byte[] { 1, 2, 3, 4 };
        var owned = new OwnerPacket(8);
        var head = new OwnerPacket(buffer, 0, buffer.Length, false) { Next = owned };

        // 从借用头（不持引用）切出共享切片：头节点为借用、尾节点递增引用计数
        var slice = head.Slice(0, -1);
        Assert.Equal(2, owned.RefCount);

        // 借用视图头 Dispose 也必须释放链上拥有节点，否则引用计数永不归零
        slice.TryDispose();
        Assert.Equal(1, owned.RefCount);

        owned.Dispose();
        Assert.Equal(0, owned.RefCount);
        head.TryDispose();
    }

    [Fact(DisplayName = "释放后访问：GetSpan 应抛异常")]
    public void AfterDispose_GetSpan_ShouldThrow()
    {
        var packet = new OwnerPacket(100);
        packet.Dispose();

        Assert.Throws<ObjectDisposedException>(() => packet.GetSpan());
    }

    [Fact(DisplayName = "释放后访问：GetMemory 应抛异常")]
    public void AfterDispose_GetMemory_ShouldThrow()
    {
        var packet = new OwnerPacket(100);
        packet.Dispose();

        Assert.Throws<ObjectDisposedException>(() => packet.GetMemory());
    }

    [Fact(DisplayName = "释放后访问：索引器应抛异常")]
    public void AfterDispose_Indexer_ShouldThrow()
    {
        var packet = new OwnerPacket(100);
        packet.Dispose();

        Assert.Throws<ObjectDisposedException>(() => packet[0]);
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

        // 释放前几个，归还内存池
        foreach (var p in packets.Take(5))
        {
            p.Dispose();
        }

        // 创建新的包，应该能复用缓冲区
        using var newPacket = new OwnerPacket(1024);

        Assert.NotNull(newPacket.Buffer);
        Assert.True(newPacket.Buffer.Length >= 1024);

        // 清理剩余的包
        foreach (var p in packets.Skip(5))
        {
            p.Dispose();
        }
    }

#if DEBUG || OWNERPACKET_FINALIZER
    // 注：本测试依赖 NewLife.Core 以相同条件编译析构兜底（默认 Debug 构建成立）
    [Fact(DisplayName = "析构兜底：漏释放的句柄在 GC 时释放引用")]
    public void Finalizer_ShouldReleaseReference()
    {
        var owner = LeakOne();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // 析构已释放漏释放句柄的引用
        Assert.Equal(0, (Int32)owner.GetValue("_refCount")!);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Object LeakOne()
    {
        var packet = new OwnerPacket(100);
        return packet.GetValue("_owner")!;
    }
#endif

    [Fact(DisplayName = "Detach：脱手后缓冲不归还，句柄作废")]
    public void Detach_ShouldAbandonHandleWithoutReturn()
    {
        var packet = new OwnerPacket(100);
        var owner = packet.GetValue("_owner");

        packet.Detach();

        Assert.Null(packet.GetValue("_owner"));
        Assert.Null(packet.GetValue("_buffer"));
        Assert.Null(packet.Next);
        Assert.Equal(0, packet.Length);
        Assert.Throws<ObjectDisposedException>(() => packet.GetSpan());

        // 引用计数保持 1：缓冲仍处于借出状态，归还责任已转移给调用方
        Assert.Equal(1, (Int32)owner!.GetValue("_refCount")!);
    }

    [Fact(DisplayName = "Detach：GC 兜底不归还借用中的缓冲")]
    public void Detach_ShouldSuppressFinalizer()
    {
        var owner = DetachAndForget();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // 析构被抑制：缓冲仍处于借出状态，未被归还
        Assert.Equal(1, (Int32)owner.GetValue("_refCount")!);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Object DetachAndForget()
    {
        var packet = new OwnerPacket(100);
        var owner = packet.GetValue("_owner")!;
        packet.Detach();
        return owner;
    }

    [Fact(DisplayName = "Detach：存在其它句柄时抛出异常且不改变状态")]
    public void Detach_WithOtherHandles_ShouldThrow()
    {
        var packet = new OwnerPacket(100);
        var slice = packet.Slice(0, 50);

        Assert.Throws<InvalidOperationException>(() => packet.Detach());

        // 句柄保持可用：异常不改变任何状态
        Assert.NotNull(packet.GetValue("_owner"));
        Assert.Equal(2, packet.RefCount);

        slice.TryDispose();
        packet.Dispose();
    }

    [Fact(DisplayName = "Detach：重复调用幂等，借用视图无操作")]
    public void Detach_MultipleCalls_ShouldBeIdempotent()
    {
        var packet = new OwnerPacket(100);
        packet.Detach();
        packet.Detach();

        Assert.Null(packet.GetValue("_owner"));

        // 借用视图不持引用，脱手无操作
        var view = new OwnerPacket(new Byte[10], 0, 10, false);
        view.Detach();
        Assert.NotNull(view.GetValue("_buffer"));
        view.TryDispose();
    }

    /// <summary>兼容 Free：转发到 Detach（放弃引用不归还，实例作废）</summary>
    [Fact(DisplayName = "Free：兼容转发到 Detach")]
    public void FreeCompat_ShouldForwardToDetach()
    {
#pragma warning disable CS0618 // 兼容旧版的 Free
        var packet = new OwnerPacket(100);
        packet.GetSpan().Fill(0x42);
        var owner = packet.GetValue("_owner");

        packet.Free();

        // 脱手：本句柄不再持有引用（引用计数保持 1，缓冲保持已借出状态不归还）
        Assert.Null(packet.GetValue("_owner"));
        Assert.Equal(1, (Int32)owner!.GetValue("_refCount")!);

        // 实例已作废：再次 Dispose 无操作
        packet.TryDispose();
        Assert.Equal(1, (Int32)owner.GetValue("_refCount")!);
#pragma warning restore CS0618 // 兼容旧版的 Free
    }

    #endregion
}