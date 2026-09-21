using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>并发连接规模测试：多客户端并行建立连接并回环收发</summary>
[Collection("Net")]
public class NetConcurrencyTests
{
    /// <summary>100 个客户端并行连接并 echo，全部成功且服务端保持可用</summary>
    [Fact(DisplayName = "并发连接_100客户端并行echo_全部成功")]
    public async Task ConcurrentConnections_100_AllEcho()
    {
        const Int32 count = 100;

        using var server = new NetServer { Port = 0, ProtocolType = NetType.Tcp, Log = XTrace.Log };
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null && e.Packet.Length > 0) session.Send(e.Packet);
        };
        server.Start();

        var results = await Task.WhenAll(Enumerable.Range(0, count).Select(async i =>
        {
            using var client = new TcpClient { ReceiveTimeout = 10_000 };
            await client.ConnectAsync(IPAddress.Loopback, server.Port).WaitAsync(TimeSpan.FromSeconds(10));

            using var ns = client.GetStream();
            var data = $"ping-{i:D3}".GetBytes();
            await ns.WriteAsync(data);

            var buf = new Byte[64];
            var total = 0;
            while (total < data.Length)
            {
                var n = await ns.ReadAsync(buf.AsMemory(total, buf.Length - total));
                if (n <= 0) break;
                total += n;
            }

            return total == data.Length && buf.AsSpan(0, total).SequenceEqual(data);
        }));

        Assert.All(results, ok => Assert.True(ok, "存在未正确回显的客户端"));
        Assert.True(server.Active, "并发连接测试后服务端应保持可用");
    }
}
