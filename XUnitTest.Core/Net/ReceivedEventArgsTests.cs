using System.Buffers;
using System.ComponentModel;
using System.Net;
using NewLife;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>ReceivedEventArgs 基础契约测试</summary>
public class ReceivedEventArgsTests
{
    [Fact]
    [DisplayName("池化复用：Return 后字段全部清空、Rent 返回干净实例")]
    public void Return_ShouldResetFields()
    {
        var e = ReceivedEventArgs.Rent();
        e.Packet = new ArrayPacket(new Byte[] { 1, 2, 3 });
        e.Message = "msg";
        e.UserState = 123;
        e.Local = IPAddress.Loopback;
        e.Remote = new IPEndPoint(IPAddress.Loopback, 1234);

        ReceivedEventArgs.Return(e);

        // 池为全静态共享，并行测试类可能取走刚归还的实例；此处断言稳定契约：取出的实例字段已全部清空
        var e2 = ReceivedEventArgs.Rent();
        Assert.Null(e2.Packet);
        Assert.Null(e2.Message);
        Assert.Null(e2.UserState);
        Assert.Null(e2.Local);
        Assert.Null(e2.Remote);

        ReceivedEventArgs.Return(e2);
    }

    [Fact]
    [DisplayName("GetBytes：拷贝当前数据")]
    public void GetBytes_ShouldCopy()
    {
        var e = new ReceivedEventArgs { Packet = new ArrayPacket(new Byte[] { 1, 2, 3 }) };

        Assert.Equal(new Byte[] { 1, 2, 3 }, e.GetBytes());
    }

    [Fact]
    [DisplayName("事件内 Slice 带出：轮末释放后仍可读（跨轮共享切片）")]
    public void Packet_Slice_ShouldOutliveRound()
    {
        // 模拟接收层每轮：把整块缓冲包装为拥有句柄直接交下游（直连）
        var round = new OwnerPacket(new Byte[] { 1, 2, 3, 4 }, 0, 4, true);
        var e = new ReceivedEventArgs { Packet = round };

        // 事件内切片带出本轮（从轮句柄切出共享切片，计数加一）
        var owned = (OwnerPacket)e.Packet!.Slice(2, 2);

        // 轮末接收层裁决：存在共享切片（计数大于 1）→ 释放本句柄引用并解绑换新；旧缓冲由最后持有者归还
        Assert.Equal(2, round.RefCount);
        round.TryDispose();

        Assert.Equal(new Byte[] { 3, 4 }, owned.ToArray());
        owned.TryDispose();
    }

    [Fact]
    [DisplayName("轮末裁决：无人带出（计数为 1）→ 句柄回挂接收槽，下轮 Resize 重设窗口复用")]
    public void RoundHandle_Unowned_ReuseByResize()
    {
        // 模拟接收层每轮：整块池化缓冲包装为拥有句柄直接交下游；下游同步只读、不持有
        var buffer = ArrayPool<Byte>.Shared.Rent(16);
        var round = new OwnerPacket(buffer, 0, 4, true);

        // 无共享切片 → 计数为 1，轮末把句柄回挂接收槽（不脱手、不归还），缓冲留在会话继续接收
        Assert.Equal(1, round.RefCount);

        // 下一轮：同一句柄把窗口重设到本段数据（缓冲原地不动，零 Rent/Return、零分配）
        buffer[0] = 9;
        buffer[1] = 8;
        round.Resize(2);

        Assert.Same(buffer, round.Buffer);
        Assert.Equal(2, round.Length);
        Assert.Equal(new Byte[] { 9, 8 }, round.ToArray());

        round.Dispose();
    }

    [Fact]
    [DisplayName("轮末裁决：下游消费轮句柄归零（非 1 即换新，禁止复用已归还缓冲）")]
    public void RoundHandle_ConsumedByDownstream_SwapsBuffer()
    {
        // 模拟接收层每轮：整块包装为拥有句柄直接交下游；下游（应答/发送链路）把它消费释放
        var round = new OwnerPacket(new Byte[] { 1, 2, 3, 4 }, 0, 4, true);
        round.TryDispose();

        // 缓冲已随句柄归还池、计数归零：轮末必须按“非 1 即换新”处理（解绑换新），
        // 不能脱手复用——否则同块缓冲被池借给他人后产生脏读
        Assert.Equal(0, round.RefCount);
        Assert.Throws<ObjectDisposedException>(() => round.Buffer);
    }
}
