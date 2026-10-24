using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UDP 服务端关闭窗口与接收环的竞态测试</summary>
/// <remarks>
/// 关闭收尾（<c>UdpServer.OnCloseAsync</c>）先清 <c>Client</c>，<c>Active</c> 稍后才由基类置假。
/// 窗口内接收回调在环尾重投时，<c>OnReceiveAsync</c> 不得用 false 表示“无法发起接收”——
/// false 的语义是“本次同步完成、事件参数已持有新状态”，会让基类把上一次完成的陈旧状态反复重新投递。
/// 本用例以闸门把关闭窗口拉长到可控，稳定暴露该路径。
/// </remarks>
[Collection("Net.C")]
public class UdpServerCloseRaceTests
{
    private static readonly Byte[] WarmPayload = "warm"u8.ToArray();
    private static readonly Byte[] RacePayload = "race"u8.ToArray();

    private static async Task WaitUntilAsync(Func<Boolean> condition, Int32 timeoutMs = 5_000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }

    /// <summary>探针：在关闭窗口（Client 已空、Active 仍真）内拉闸，把窗口拉长到可控</summary>
    private sealed class GatedCloseUdpServer : UdpServer
    {
        public readonly ManualResetEventSlim Entered = new(false);
        public readonly ManualResetEventSlim Gate = new(false);

        private Socket? _saved;

        protected override Task<Boolean> OnCloseAsync(String reason, CancellationToken cancellationToken)
            => Task.Run<Boolean>(() =>
            {
                // 复刻真实实现的顺序：先清 Client 进入窗口，再挂起等待（真实实现此处为通知与清理）
                _saved = Client;
                Client = null;
                Entered.Set();
                Gate.Wait(10_000);

                _saved?.Shutdown();

                return true;
            });
    }

    [Fact(DisplayName = "UDP关闭竞态_关闭窗口内到达的数据报_不重复派发")]
    public async Task CloseWindow_ArrivalDuringClose_NoDuplicateDelivery()
    {
        using var server = new GatedCloseUdpServer { Port = 0, MaxAsync = 1 };

        var warm = 0;
        var raced = 0;
        server.Received += (s, e) =>
        {
            var buf = e.GetBytes();
            if (buf.SequenceEqual(WarmPayload)) Interlocked.Increment(ref warm);
            if (buf.SequenceEqual(RacePayload)) Interlocked.Increment(ref raced);
        };
        server.Open();

        using var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var ep = new IPEndPoint(IPAddress.Loopback, server.Port);

        // 先完成一轮正常收发，确保接收环已进入“等待下一报文”的挂起态
        client.SendTo(WarmPayload, ep);
        await WaitUntilAsync(() => Volatile.Read(ref warm) >= 1);

        // 关闭线程进入窗口（Client 已空、Active 仍真）并保持挂起
        var closeTask = Task.Run(() => server.Close("race"));
        Assert.True(server.Entered.Wait(5_000), "关闭未进入窗口");

        // 窗口内到达的数据报：正常交付一次后，接收环重投不得把陈旧状态再投一遍
        client.SendTo(RacePayload, ep);
        await WaitUntilAsync(() => Volatile.Read(ref raced) >= 1);

        // 留出时间给（旧实现的）重投循环累积重复派发，再放行关闭
        await Task.Delay(200);
        server.Gate.Set();

        Assert.True(await closeTask.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(1, Volatile.Read(ref raced));
        Assert.False(server.Active);
    }
}
