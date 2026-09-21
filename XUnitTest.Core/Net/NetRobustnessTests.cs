using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>裸 Socket 层鲁棒性测试：全双工大流量互发、载荷边界矩阵、反复断开后服务可用性</summary>
[Collection("Net")]
public class NetRobustnessTests
{
    #region 全双工
    /// <summary>两端同时互发 10MB，双向数据逐字节完整</summary>
    [Fact(DisplayName = "全双工_两端同时互发10MB_双向逐字节一致")]
    public async Task FullDuplex_BothDirections_10MB()
    {
        const Int32 total = 10 * 1024 * 1024;
        const Int32 chunk = 64 * 1024;

        // 双向数据副本，供两端各自校验
        var up = new Byte[total];       // 客户端 → 服务端
        var down = new Byte[total];     // 服务端 → 客户端
        Random.Shared.NextBytes(up);
        Random.Shared.NextBytes(down);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
            Log = XTrace.Log,
        };

        var serverDone = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sendTasks = new List<Task>();
        var sessionReady = new ManualResetEventSlim(false);
        server.NewSession += (s, e) =>
        {
            var session = e.Session;

            // 上行数据累计
            var buf = new Byte[total];
            var off = 0;
            session.Session.Received += (s2, e2) =>
            {
                var data = e2.GetBytes();
                lock (buf)
                {
                    var n = Math.Min(data.Length, total - off);
                    System.Buffer.BlockCopy(data, 0, buf, off, n);
                    off += n;
                    if (off >= total) serverDone.TrySetResult(buf);
                }
            };

            // 服务端反向发送 10MB，与上行同时进行
            lock (sendTasks)
            {
                sendTasks.Add(Task.Run(() =>
                {
                    for (var pos = 0; pos < total; pos += chunk)
                    {
                        var len = Math.Min(chunk, total - pos);
                        session.Send(down, pos, len);
                    }
                }));
            }
            sessionReady.Set();
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}")
        {
            AutoReconnect = false,
            Log = XTrace.Log,
        };

        // 下行数据累计
        var clientDone = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cbuf = new Byte[total];
        var coff = 0;
        client.Received += (s2, e2) =>
        {
            var data = e2.GetBytes();
            lock (cbuf)
            {
                var n = Math.Min(data.Length, total - coff);
                System.Buffer.BlockCopy(data, 0, cbuf, coff, n);
                coff += n;
                if (coff >= total) clientDone.TrySetResult(cbuf);
            }
        };
        Assert.True(client.Open());

        Assert.True(sessionReady.Wait(3000), "服务端会话未建立");
        Task[] sends;
        lock (sendTasks) sends = sendTasks.ToArray();

        // 客户端同时上行发送 10MB
        var upTask = Task.Run(() =>
        {
            for (var pos = 0; pos < total; pos += chunk)
            {
                var len = Math.Min(chunk, total - pos);
                client.Send(up, pos, len);
            }
        });

        // 双向各自收满
        var gotDown = await clientDone.Task.WaitAsync(TimeSpan.FromSeconds(60));
        var gotUp = await serverDone.Task.WaitAsync(TimeSpan.FromSeconds(60));
        await Task.WhenAll([upTask, .. sends]).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(up.SequenceEqual(gotUp), "服务端收到的上行数据应逐字节一致");
        Assert.True(down.SequenceEqual(gotDown), "客户端收到的下行数据应逐字节一致");

        client.Close("done");
    }
    #endregion

    #region 载荷边界
    /// <summary>1B 与 64KB 前后边界、256KB 的裸回显逐字节一致</summary>
    [Theory(DisplayName = "载荷边界_回环echo_逐字节一致")]
    [InlineData(1)]
    [InlineData(65_535)]
    [InlineData(65_536)]
    [InlineData(65_537)]
    [InlineData(262_144)]
    public async Task PayloadBoundary_Echo_ByteExact(Int32 size)
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) =>
        {
            var session = e.Session;
            session.Session.Received += (s2, e2) =>
            {
                var pk = e2.Packet;
                if (pk != null && pk.Length > 0) session.Send(pk);
            };
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { AutoReconnect = false, Log = XTrace.Log };

        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);

        var buf = new Byte[size];
        var off = 0;
        var done = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Received += (s2, e2) =>
        {
            var data = e2.GetBytes();
            lock (buf)
            {
                var n = Math.Min(data.Length, size - off);
                System.Buffer.BlockCopy(data, 0, buf, off, n);
                off += n;
                if (off >= size) done.TrySetResult(buf);
            }
        };
        Assert.True(client.Open());
        client.Send(payload);

        var got = await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(payload, got);

        client.Close("done");
    }
    #endregion

    #region 反复断开
    /// <summary>连续 20 轮连接/断开，服务端会话快速失活且服务保持可用</summary>
    [Fact(DisplayName = "反复断开_20轮_会话失活且服务可用")]
    public async Task RepeatedDisconnects_20Rounds_ServiceStaysAvailable()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
            Log = XTrace.Log,
        };
        var sessions = new List<INetSession>();
        var recvCount = 0;
        server.NewSession += (s, e) =>
        {
            var session = e.Session;
            lock (sessions) sessions.Add(session);
            session.Session.Received += (s2, e2) => Interlocked.Increment(ref recvCount);
        };
        server.Start();

        for (var round = 0; round < 20; round++)
        {
            using var client = new TcpClient { NoDelay = true };
            client.Connect(IPAddress.Loopback, server.Port);
            client.GetStream().WriteByte((Byte)round);

            // 等待服务端收到该轮数据（会话已建立并进入接收）
            for (var i = 0; i < 100 && Volatile.Read(ref recvCount) <= round; i++) await Task.Delay(20);
            Assert.True(Volatile.Read(ref recvCount) > round, $"第{round}轮服务端未收到数据");

            INetSession session;
            lock (sessions) session = sessions[^1];

            client.Close();

            // 客户端关闭后，服务端会话应快速失活
            var socket = (SessionBase)session.Session;
            for (var i = 0; i < 100 && socket.Active; i++) await Task.Delay(20);
            Assert.False(socket.Active, $"第{round}轮会话应快速失活");
        }

        // 全部轮次结束后，服务仍能接受新连接并处理数据
        var before = Volatile.Read(ref recvCount);
        using var probe = new TcpClient { NoDelay = true };
        probe.Connect(IPAddress.Loopback, server.Port);
        probe.GetStream().WriteByte(0xFF);
        for (var i = 0; i < 100 && Volatile.Read(ref recvCount) <= before; i++) await Task.Delay(20);
        Assert.True(Volatile.Read(ref recvCount) > before, "20轮断开后服务应仍能处理新连接");
        probe.Close();
    }
    #endregion
}
