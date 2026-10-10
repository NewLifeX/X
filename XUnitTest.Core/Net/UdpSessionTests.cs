using System.Buffers;
using System.ComponentModel;
using System.Net;
using System.Runtime.ExceptionServices;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UdpSession 空数据报约定的判定时机测试</summary>
/// <remarks>
/// 约定：收到空数据报时结束会话。判定必须以事件链之前的原始数据报为准——
/// 事件内业务按契约消费/释放本轮句柄后 <c>Packet.Length</c> 归零，
/// 事件之后再判会把正常数据报误判为空包而销毁会话。
/// </remarks>
public class UdpSessionTests
{
    #region 工具
    private static readonly IPEndPoint _remote = new(IPAddress.Loopback, 12345);

    private static UdpSession NewSession(UdpServer server) => new(server, null, _remote);

    private static ReceivedEventArgs NewArgs(IPacket? pk) => new()
    {
        Local = IPAddress.Loopback,
        Remote = _remote,
        Packet = pk,
    };

    /// <summary>构造带拥有权的数据报句柄：缓冲取自池，保证释放后原样归还</summary>
    private static OwnerPacket NewPacket(Int32 length) => new(ArrayPool<Byte>.Shared.Rent(16), 0, length, true);

    /// <summary>测试用协议：整个数据报即一条消息（无头部字节）</summary>
    private sealed class DatagramCodec : IMessageCodec
    {
        public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
        {
            if (buffer.Length == 0) return null;

            return new ParseResult { Message = new Message(), HeaderSize = 0, BodyLength = buffer.Length };
        }

        public IOwnerPacket? Build(IMessage message) => throw new NotSupportedException("测试协议仅验证接收路径");

        public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength) => throw new NotSupportedException("测试协议仅验证接收路径");
    }
    #endregion

    [Theory(DisplayName = "UDP事件内消费或置空句柄_不得误判为空数据报停会话")]
    [InlineData("dispose")]
    [InlineData("null")]
    public void ConsumedInHandler_NotTreatedAsEmpty(String mode)
    {
        using var server = new UdpServer();
        using var session = NewSession(server);

        var handled = 0;
        session.Received += (s, e) =>
        {
            handled++;

            // 契约允许的两类改写：消费本轮句柄（交发送链路或显式释放）、置空做标记
            if (mode == "dispose")
                e.Packet?.TryDispose();
            else
                e.Packet = null;
        };

        var pk = NewPacket(3);
        try
        {
            session.OnReceive(NewArgs(pk));
        }
        finally
        {
            pk.TryDispose();
        }

        Assert.Equal(1, handled);

        // 非空数据报不得触发 Stop+Dispose：会话存活、服务器引用保留
        Assert.False(session.Disposed);
        Assert.Same(server, session.Server);
    }

    [Fact(DisplayName = "UDP空数据报_结束会话")]
    public void EmptyDatagram_StopsSession()
    {
        using var server = new UdpServer();
        using var session = NewSession(server);

        var handled = 0;
        session.Received += (s, e) => handled++;

        var pk = NewPacket(0);
        try
        {
            session.OnReceive(NewArgs(pk));
        }
        finally
        {
            pk.TryDispose();
        }

        // 事件照旧抛出（业务可感知），但会话按约定结束
        Assert.Equal(1, handled);
        Assert.True(session.Disposed);
        Assert.Null(session.Server);
    }

    /// <summary>并行派发下的并发收包：消息不得滞留、并发槽位不得溢出</summary>
    /// <remarks>
    /// 首访竞态窗口极窄（读字段到赋值只有几十纳秒），无法确定复现——实测旧实现跑本用例同样通过。
    /// 本用例守护的是并行派发契约：并发注入的数据报必须全部处理完，且不得出现槽位溢出
    /// （溢出会从 fire-and-forget 任务逃逸，无人观察、无日志，只能靠首轮异常通知检出）。
    /// </remarks>
    [Fact(DisplayName = "UDP并行派发_并发首访与并发收包_消息不丢且无槽位溢出")]
    public void ParallelDispatch_ConcurrentFirstAccess_NoSlotOverflow()
    {
        using var server = new UdpServer();
        using var session = NewSession(server);
        session.Protocol = new DatagramCodec();
        session.MaxConcurrency = 4;

        const Int32 workers = 8;
        const Int32 rounds = 8;
        const Int32 count = workers * rounds;

        using var done = new CountdownEvent(count);
        session.Received += (s, e) => done.Signal();

        // 并发信号量的首访竞态会让“等待的实例”与“归还的实例”不同，槽位账目失衡。
        // 溢出异常从 fire-and-forget 任务逃逸（无人观察、无日志），只能靠首轮异常通知捕获
        var overflow = 0;
        EventHandler<FirstChanceExceptionEventArgs> handler = (s, e) =>
        {
            if (e.Exception is SemaphoreFullException) Interlocked.Increment(ref overflow);
        };
        AppDomain.CurrentDomain.FirstChanceException += handler;

        try
        {
            // Barrier 让所有线程同时首访并发信号量，再并发注入数据报
            using var barrier = new Barrier(workers);
            var threads = new Thread[workers];
            for (var i = 0; i < workers; i++)
            {
                threads[i] = new Thread(() =>
                {
                    barrier.SignalAndWait();
                    for (var j = 0; j < rounds; j++)
                    {
                        var pk = NewPacket(3);
                        try
                        {
                            session.OnReceive(NewArgs(pk));
                        }
                        finally
                        {
                            pk.TryDispose();
                        }
                    }
                })
                { IsBackground = true };
            }

            foreach (var th in threads) th.Start();
            foreach (var th in threads) Assert.True(th.Join(10_000), "并发收包未在超时内完成");

            Assert.True(done.Wait(TimeSpan.FromSeconds(10)), $"并行处理未在超时内完成，已处理 {count - done.CurrentCount}/{count}");
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        Assert.Equal(0, overflow);
    }
}
