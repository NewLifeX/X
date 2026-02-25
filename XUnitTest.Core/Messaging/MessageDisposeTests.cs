using NewLife.Data;
using NewLife.Messaging;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>IMessage 释放机制单元测试</summary>
/// <remarks>
/// 验证 IMessage.Dispose → Payload.TryDispose → IOwnerPacket.Dispose 的完整释放链路。
/// 该设计使得 RPC 场景中，上层只需 Dispose(IMessage) 即可自动归还底层 OwnerPacket 的池化内存。
/// </remarks>
public class MessageDisposeTests
{
    [Fact(DisplayName = "Dispose释放OwnerPacket：Message.Dispose应归还Payload的池化内存")]
    public void Dispose_WithOwnerPacketPayload_ShouldDisposePayload()
    {
        // Arrange - 创建带 OwnerPacket 负载的消息
        var payload = new OwnerPacket(128);
        payload.GetSpan().Fill(0xAB);
        var msg = new DefaultMessage
        {
            Sequence = 1,
            Payload = payload,
        };

        // 验证初始状态
        Assert.NotNull(msg.Payload);
        Assert.True((Boolean)payload.GetValue("_hasOwner"));

        // Act - 释放消息
        msg.Dispose();

        // Assert - Payload 应被置空，OwnerPacket 应已释放（失去所有权）
        Assert.Null(msg.Payload);
        Assert.False((Boolean)payload.GetValue("_hasOwner"));
    }

    [Fact(DisplayName = "Dispose释放链式OwnerPacket：链式Payload应递归释放")]
    public void Dispose_WithChainedOwnerPacketPayload_ShouldDisposeEntireChain()
    {
        // Arrange - 创建链式 OwnerPacket 作为消息负载
        var part1 = new OwnerPacket(64);
        var part2 = new OwnerPacket(32);
        part1.Next = part2;
        var msg = new Message { Payload = part1 };

        // 验证初始所有权
        Assert.True((Boolean)part1.GetValue("_hasOwner"));
        Assert.True((Boolean)part2.GetValue("_hasOwner"));

        // Act
        msg.Dispose();

        // Assert - 链式节点应全部释放
        Assert.Null(msg.Payload);
        Assert.False((Boolean)part1.GetValue("_hasOwner"));
        Assert.False((Boolean)part2.GetValue("_hasOwner"));
    }

    [Fact(DisplayName = "Dispose非IDisposable的Payload：ArrayPacket不受影响")]
    public void Dispose_WithArrayPacketPayload_ShouldNotThrow()
    {
        // Arrange - ArrayPacket 不实现 IDisposable
        var msg = new Message
        {
            Payload = new ArrayPacket(new Byte[] { 1, 2, 3 }),
        };

        // Act & Assert - 不应抛出异常
        msg.Dispose();
        Assert.Null(msg.Payload);
    }

    [Fact(DisplayName = "Dispose后Payload为null：防止二次访问已释放资源")]
    public void Dispose_ShouldSetPayloadToNull()
    {
        var payload = new OwnerPacket(64);
        var msg = new DefaultMessage { Payload = payload };

        msg.Dispose();

        Assert.Null(msg.Payload);
    }

    [Fact(DisplayName = "Dispose幂等性：多次Dispose不应抛出异常")]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        var payload = new OwnerPacket(64);
        var msg = new DefaultMessage { Payload = payload };

        msg.Dispose();
        msg.Dispose(); // 第二次不应抛出

        Assert.Null(msg.Payload);
    }

    [Fact(DisplayName = "RPC场景：Read解析后Dispose应释放切片所有权")]
    public void Dispose_AfterRead_ShouldDisposeSlicedPayload()
    {
        // Arrange - 模拟 RPC 接收场景：
        // 底层收到原始数据 → OwnerPacket → DefaultMessage.Read 解析 → Payload 为切片
        var raw = new OwnerPacket(128);
        var span = raw.GetSpan();
        // 构造一个有效的 DefaultMessage 二进制包：Flag=1, Seq=5, Len=10
        span[0] = 0x01; // Flag=1, 请求
        span[1] = 0x05; // Sequence=5
        span[2] = 0x0A; // Length=10 (低字节)
        span[3] = 0x00; // Length=0 (高字节)
        for (var i = 4; i < 14; i++) span[i] = (Byte)(i - 4); // 填充 10 字节负载

        var msg = new DefaultMessage();
        var ok = msg.Read(raw);
        Assert.True(ok);
        Assert.Equal(5, msg.Sequence);
        Assert.NotNull(msg.Payload);
        Assert.Equal(10, msg.Payload!.Total);

        // Act - 上层使用完毕后 Dispose 消息
        msg.Dispose();

        // Assert - Payload 已被清理
        Assert.Null(msg.Payload);
    }

    [Fact(DisplayName = "using模式：使用using自动释放消息及其Payload")]
    public void Using_ShouldAutoDisposePayloadOnScopeExit()
    {
        var payload = new OwnerPacket(64);
        payload.GetSpan().Fill(0xCD);

        using (var msg = new DefaultMessage { Payload = payload })
        {
            Assert.Equal(64, msg.Payload!.Total);
            Assert.True((Boolean)payload.GetValue("_hasOwner"));
        }
        // using 块退出后，消息和 Payload 都应被释放

        Assert.False((Boolean)payload.GetValue("_hasOwner"));
    }

    [Fact(DisplayName = "CreateReply不影响原始Payload：响应消息独立管理")]
    public void CreateReply_ShouldNotAffectOriginalPayload()
    {
        // Arrange
        using var payload = new OwnerPacket(64);
        var request = new DefaultMessage
        {
            Sequence = 1,
            Payload = payload,
        };

        // Act - 创建响应消息
        var reply = request.CreateReply();

        // Assert - 响应消息 Payload 为空，原请求 Payload 不受影响
        Assert.Null(reply.Payload);
        Assert.NotNull(request.Payload);
        Assert.Same(payload, request.Payload);

        reply.Dispose();
    }

    [Fact(DisplayName = "OwnerPacket作为Payload的完整生命周期")]
    public void FullLifecycle_OwnerPacketAsPayload()
    {
        // 1. 底层申请内存
        var buffer = new OwnerPacket(256);
        Assert.True((Boolean)buffer.GetValue("_hasOwner"));

        // 2. 填充协议头 + 负载
        var span = buffer.GetSpan();
        span[0] = 0x01;
        span[1] = 0x03;
        span[2] = 0x04;
        span[3] = 0x00;
        span[4] = 0x41;
        span[5] = 0x42;
        span[6] = 0x43;
        span[7] = 0x44;

        // 3. 解析成消息（Read 内部 Slice 转移所有权）
        var msg = new DefaultMessage();
        msg.Read(buffer);

        Assert.NotNull(msg.Payload);
        Assert.Equal(4, msg.Payload!.Total);

        // 4. 上层使用完毕后 Dispose 消息
        msg.Dispose();

        // 5. 验证资源已释放
        Assert.Null(msg.Payload);
    }
}
