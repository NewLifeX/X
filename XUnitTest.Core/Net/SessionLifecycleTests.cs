using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>会话生命周期（SessionBase.Open/Close 打开关闭与重入）测试</summary>
public class SessionLifecycleTests
{
    #region 工具
    private static async Task WaitUntilAsync(Func<Boolean> condition, Int32 timeoutMs = 10_000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }

    /// <summary>生命周期测试假体：不连套接字，打开/关闭可挂起以观察并发语义</summary>
    private sealed class FakeSession : SessionBase
    {
        public FakeSession() => AutoReceive = false;

        /// <summary>打开闸门。非空时挂起打开，直到释放</summary>
        public ManualResetEventSlim? OpenGate { get; set; }

        /// <summary>置位后打开返回失败，由测试自行清除</summary>
        public Boolean FailOpen { get; set; }

        /// <summary>置位后在关闭核心内模拟 TcpSession 约定：提前置 Active 为假并重入关闭</summary>
        public Boolean NestedClose { get; set; }

        public Int32 OpenCount;
        public Int32 CloseCount;

        protected override async Task<Boolean> OnOpenAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref OpenCount);

            var gate = OpenGate;
            if (gate != null) await Task.Run(() => gate.Wait(3_000, cancellationToken)).ConfigureAwait(false);

            return !FailOpen;
        }

        protected override async Task<Boolean> OnCloseAsync(String reason, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CloseCount);

            // 模拟 TcpSession：关闭时拆除连接并提前置假，此后的重入关闭（如 Dispose 触发）应快速返回不自等待
            if (NestedClose)
            {
                Active = false;
                Close("Nested");
            }

            return true;
        }

        protected override Int32 OnSend(IPacket data) => data.Total;

        protected override Int32 OnSend(ArraySegment<Byte> data) => data.Count;

        protected override Int32 OnSend(ReadOnlySpan<Byte> data) => data.Length;

        protected internal override ISocketSession? OnPreReceive(IPacket pk, IPAddress local, IPEndPoint remote) => null;

        protected override Boolean OnReceive(ReceivedEventArgs e) => false;

        internal override Boolean OnReceiveAsync(SocketAsyncEventArgs se) => false;
    }
    #endregion

    #region 基本
    [Fact]
    [DisplayName("生命周期_打开失败_不缓存可重试")]
    public async Task FailedOpen_NotCached()
    {
        using var session = new FakeSession { FailOpen = true };

        Assert.False(await session.OpenAsync());
        Assert.Equal(1, session.OpenCount);
        Assert.False(session.Active);

        session.FailOpen = false;

        Assert.True(await session.OpenAsync());
        Assert.Equal(2, session.OpenCount);
        Assert.True(session.Active);
    }
    #endregion

    #region 桥接与空闲
    [Fact]
    [DisplayName("生命周期_同步桥接_打开与关闭")]
    public void SyncOpenClose_Bridge()
    {
        using var session = new FakeSession();

        Assert.True(session.Open());
        Assert.True(session.Active);
        Assert.Equal(1, session.OpenCount);

        Assert.True(session.Close("bridge"));
        Assert.False(session.Active);
        Assert.Equal(1, session.CloseCount);
    }

    [Fact]
    [DisplayName("生命周期_空闲关闭_直接成功")]
    public void IdleClose_ReturnsTrue()
    {
        using var session = new FakeSession();

        Assert.True(session.Close("idle"));
        Assert.False(session.Active);
        Assert.Equal(0, session.CloseCount);
    }
    #endregion

    #region 销毁与重入
    [Fact]
    [DisplayName("生命周期_打开期间销毁_不复活")]
    public async Task DisposeDuringOpen_NotRevive()
    {
        var session = new FakeSession();
        var gate = new ManualResetEventSlim();
        session.OpenGate = gate;

        var openTask = session.OpenAsync();
        await WaitUntilAsync(() => Volatile.Read(ref session.OpenCount) == 1);

        // 销毁在途：打开未完成时无连接可关，关闭直接成功；打开随后完成时发现实例已销毁，不标记活动
        session.Dispose();

        gate.Set();

        Assert.False(await openTask);
        Assert.True(session.Disposed);
        Assert.False(session.Active);
    }

    [Fact]
    [DisplayName("生命周期_关闭内重入关闭_快速返回不自等待")]
    public async Task NestedCloseDuringClose_NoSelfWait()
    {
        using var session = new FakeSession { NestedClose = true };

        Assert.True(await session.OpenAsync());

        var sw = Stopwatch.StartNew();
        Assert.True(await session.CloseAsync("outer"));
        sw.Stop();

        Assert.Equal(1, session.CloseCount);
        Assert.False(session.Active);

        // 重入关闭遇 Active 已置假，走“未活动直接成功”快路径，必须快速返回
        Assert.True(sw.ElapsedMilliseconds < 3_000, $"关闭耗时 {sw.ElapsedMilliseconds}ms，疑似自等待");
    }

    [Fact]
    [DisplayName("生命周期_顺序开关多次_可重开")]
    public async Task SequentialOpenClose_Repeated()
    {
        using var session = new FakeSession();

        for (var i = 1; i <= 3; i++)
        {
            Assert.True(await session.OpenAsync());
            Assert.True(session.Active);

            Assert.True(await session.CloseAsync("loop"));
            Assert.False(session.Active);
        }

        Assert.Equal(3, session.OpenCount);
        Assert.Equal(3, session.CloseCount);

        // 关闭后仍可重新打开
        Assert.True(await session.OpenAsync());
        Assert.Equal(4, session.OpenCount);
    }
    #endregion
}
