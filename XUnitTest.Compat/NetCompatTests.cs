using System.ComponentModel;
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

            // 显式传入令牌以选择 Task 版重载（无参 ReceiveAsync() 是启动后台接收环的 Boolean 版）
            var receiveTask = client.ReceiveAsync(CancellationToken.None);
            var finished = await Task.WhenAny(receiveTask, Task.Delay(5000));
            Assert.True(finished == receiveTask, "等待回显数据超时");

            using var pk = await receiveTask;
            Assert.NotNull(pk);
            Assert.Equal(sendData, pk!.ToArray());
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
