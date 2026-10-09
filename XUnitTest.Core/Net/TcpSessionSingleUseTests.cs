using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>TcpSession 会话对象一次性契约：关闭后不可重新打开，重连请新建对象</summary>
/// <remarks>
/// <para>会话对象一旦活动过（打开成功或服务端接受连接），关闭后即终结：再次调用 <see cref="SessionBase.Open()"/> /
/// <see cref="SessionBase.OpenAsync(CancellationToken)"/> 抛 <see cref="InvalidOperationException"/>，
/// 避免上一轮连接被中止的异步残响落到新一轮连接的接收环、管道与队列上。</para>
/// <para>打开失败不标记活动，允许原对象重试；重连场景应新建会话对象（NetClient 等宿主已自动如此）。</para>
/// </remarks>
[Collection("Net.D")]
public class TcpSessionSingleUseTests
{
    #region 工具
    private static async Task WaitUntilAsync(Func<Boolean> condition, Int32 timeoutMs = 5_000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }

    /// <summary>从裸套接字读满指定字节数</summary>
    private static Int32 ReceiveAll(Socket sock, Byte[] buffer)
    {
        var n = 0;
        while (n < buffer.Length)
        {
            var c = sock.Receive(buffer, n, buffer.Length - n, SocketFlags.None);
            if (c <= 0) break;

            n += c;
        }

        return n;
    }
    #endregion

    [Fact]
    [DisplayName("会话一次性_关闭复位入站管道_且明确拒绝重开")]
    public async Task Closed_ResetsPipes_AndRejectsReopen()
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var session = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        Socket? peer = null;
        try
        {
            Assert.True(session.Open());
            peer = await listener.AcceptWithinAsync();
            peer.ReceiveTimeout = 10_000;

            var pipe = session.Pipe;
            Assert.Same(pipe, session.GetPipe());

            Assert.True(session.Close("test"));

            // 关闭即释放：会话上不得再留已完成管道
            Assert.True(pipe.IsCompleted);
            Assert.Null(session.GetPipe());
            Assert.False(session.Active);

            // 会话对象一次性：同步与异步入口都必须明确拒绝重新打开
            Assert.Throws<InvalidOperationException>(() => session.Open());
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.OpenAsync());
        }
        finally
        {
            peer?.Dispose();
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("会话一次性_关闭后新建对象继续_收发正常")]
    public async Task AfterClose_NewSessionContinues()
    {
        // 裸监听套接字：接受两次连接，验证“重连=新建对象”路线可用
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(2);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        Socket? peer1 = null;
        Socket? peer2 = null;
        try
        {
            // 第一轮：建立连接并完成一次收发，随后关闭
            {
                using var session1 = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };

                Assert.True(session1.Open());
                peer1 = await listener.AcceptWithinAsync();
                peer1.ReceiveTimeout = 10_000;

                var pipe1 = session1.Pipe;

                Assert.Equal(3, session1.Send(new ArrayPacket(new Byte[] { 1, 2, 3 })));
                var buf = new Byte[3];
                Assert.Equal(3, ReceiveAll(peer1, buf));
                Assert.Equal(new Byte[] { 1, 2, 3 }, buf);

                peer1.Send(new Byte[] { 4, 5, 6 });
                await WaitUntilAsync(() => pipe1.UnconsumedLength >= 3);

                Assert.True(session1.Close("first"));
                Assert.Throws<InvalidOperationException>(() => session1.Open());
            }

            // 第二轮：新建会话接续，发送与接收均恢复
            using var session2 = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
            Assert.True(session2.Open());
            peer2 = await listener.AcceptWithinAsync();
            peer2.ReceiveTimeout = 10_000;

            var pipe2 = session2.Pipe;

            Assert.Equal(3, session2.Send(new ArrayPacket(new Byte[] { 7, 8, 9 })));
            var buf2 = new Byte[3];
            Assert.Equal(3, ReceiveAll(peer2, buf2));
            Assert.Equal(new Byte[] { 7, 8, 9 }, buf2);

            peer2.Send(new Byte[] { 10, 11, 12 });
            await WaitUntilAsync(() => pipe2.UnconsumedLength >= 3);
        }
        finally
        {
            peer1?.Dispose();
            peer2?.Dispose();
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("会话一次性_打开失败不算活动_允许重试")]
    public async Task OpenFailure_StillRetryable()
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var session = new TcpSession();
        Socket? peer = null;
        try
        {
            // 未设置远端地址：打开返回失败，不标记活动
            Assert.False(await session.OpenAsync());
            Assert.False(session.Active);

            // 补上远端地址后重试：原对象未被一次性守卫拒绝
            session.Remote = new NetUri($"tcp://127.0.0.1:{port}");
            Assert.True(await session.OpenAsync());
            peer = await listener.AcceptWithinAsync();

            Assert.True(session.Close("test"));

            // 活动过再关闭：二次打开明确拒绝
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.OpenAsync());
        }
        finally
        {
            peer?.Dispose();
            listener.Dispose();
        }
    }

    [Fact]
    [DisplayName("会话关闭_取消令牌在入口拦截_不拆流式收尾且会话仍可用")]
    public async Task CloseAsync_PreCancelled_KeepsStreamsIntact()
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var session = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        Socket? peer = null;
        try
        {
            Assert.True(session.Open());
            peer = await listener.AcceptWithinAsync();

            // 建立入站管道（等价于用过 Pipe 的会话）
            var pipe = session.Pipe;

            // 已取消的令牌在入口被拦截：未进入关闭流程
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.False(await session.CloseAsync("cancelled", cts.Token));

            // 会话必须保持原状：仍活动、管道是原实例且未完成。
            // 旧实现无条件执行流式收尾（管道被释放并置空），会话外显“还开着”却再也收不到数据
            Assert.True(session.Active);
            Assert.Same(pipe, session.GetPipe());
            Assert.False(pipe.IsCompleted);

            // 仍能收：对端数据照常进入管道
            peer.Send(new Byte[] { 5, 6, 7 });
            await WaitUntilAsync(() => pipe.UnconsumedLength >= 3);

            // 正常关闭仍然可用
            Assert.True(session.Close("real"));
            Assert.False(session.Active);
        }
        finally
        {
            peer?.Dispose();
        }
    }
}
