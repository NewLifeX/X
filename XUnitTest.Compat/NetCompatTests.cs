using System.ComponentModel;
using System.Diagnostics;
using NewLife;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Compat;

/// <summary>网络会话兼容性测试：net462 走 NETFRAMEWORK 的 Begin/EndReceive 分支、net8.0 走 Socket.ReceiveAsync 分支，同一套用例校验显式接收行为一致</summary>
public class NetCompatTests
{
    /// <summary>显式接收（AutoReceive=false 关闭接收环）在回环回显下完整往返，覆盖两个目标框架各自的接收分支</summary>
    [Fact]
    [DisplayName("显式接收_回环回显_数据完整往返")]
    public async Task ReceiveAsync_LoopbackEcho()
    {
        using var server = new TcpServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            if (e.Session is TcpSession session)
            {
                // 收到什么回什么，形成回显
                session.Received += (ss, ee) =>
                {
                    var buf = ee.GetBytes();
                    if (buf != null) session.Send(buf);
                };
            }
        };
        server.Start();

        // AutoReceive=false 拉取模式，显式调用 ReceiveAsync（用法见 WebSocketClient.ReceiveMessageAsync 注释）
        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            AutoReceive = false,
            Timeout = 5000,
        };
        client.Open();

        var payloads = new List<Byte[]> { "Hello NewLife"u8.ToArray(), BuildPayload(2048) };
        foreach (var sendData in payloads)
        {
            Assert.Equal(sendData.Length, client.Send(sendData));

            // 显式传入令牌以选择 Task 版重载（启动后台接收环的 Boolean 版已改名 StartReceive）
            var receiveTask = client.ReceiveAsync(CancellationToken.None);
            var finished = await Task.WhenAny(receiveTask, Task.Delay(5000));
            Assert.True(finished == receiveTask, "等待回显数据超时");

            using var pk = await receiveTask;
            Assert.NotNull(pk);
            Assert.Equal(sendData, pk!.ToArray());
        }
    }

    /// <summary>发送泵在无异步发送重载的框架上走同步兜底发送（net462 即此分支），数据必须只发一次且管道排空</summary>
    /// <remarks>
    /// 泵的兜底入口不得复查发送管道：泵此时已接管出口，一旦复查就把数据回投进泵自己正在消费的管道，
    /// 表现为数据永远发不出去（每次发送都被改写入队）或重复发送。
    /// </remarks>
    [Fact]
    [DisplayName("发送泵_兜底发送路径_数据只发一次且管道排空")]
    public async Task SendPipe_PumpFallback_SendsOnce()
    {
        var payload = BuildPayload(64 * 1024);

        using var server = new TcpServer { Port = 0 };
        var received = new MemoryStream();
        var total = 0;
        var sessionReady = new ManualResetEventSlim();
        var completed = new ManualResetEventSlim();

        server.NewSession += (s, e) =>
        {
            e.Session.Received += (ss, ee) =>
            {
                var buf = ee.GetBytes();
                if (buf == null || buf.Length == 0) return;

                lock (received)
                {
                    received.Write(buf, 0, buf.Length);
                    total += buf.Length;
                }

                if (total >= payload.Length) completed.Set();
            };
            sessionReady.Set();
        };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            Timeout = 5000,
        };
        client.Open();
        Assert.True(sessionReady.Wait(3000), "等待服务端会话超时");

        // 创建发送管道：此后发送统一由发送泵送出（net462 上泵走同步兜底发送）
        var pipe = client.SendPipe;
        Assert.Equal(payload.Length, client.Send(payload));

        Assert.True(completed.Wait(5000), "未收到完整数据：泵的兜底发送可能把数据回投进了自己的管道");

        // 泵必须已把数据从管道消费掉；回投会让积压不减反增
        var sw = Stopwatch.StartNew();
        while (pipe.UnconsumedLength > 0 && sw.ElapsedMilliseconds < 5000) await Task.Delay(10);
        Assert.Equal(0, pipe.UnconsumedLength);

        // 静置后确认没有重复发送
        await Task.Delay(200);
        lock (received)
        {
            Assert.Equal(payload.Length, received.Length);
            Assert.Equal(payload, received.ToArray());
        }
    }

    private static Byte[] BuildPayload(Int32 count)
    {
        var data = new Byte[count];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (Byte)i;
        }

        return data;
    }
}
