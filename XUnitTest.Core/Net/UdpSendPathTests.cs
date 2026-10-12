using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UDP 发送路径契约：已连接判定与发送目标校验</summary>
/// <remarks>
/// 已连接的 Socket 走 Send 会忽略传入目标（一律发往连接时的端点），因此发送前必须回答两个问题：
/// ① 到底连没连——Linux+Mono 的 Connected 恒为 true，只能按远端端点判定；
/// ② 目标还是不是连接时的那个——不一致时改走 SendTo，由内核以 EISCONN/WSAEISCONN 明确报错，而不是静默错投。
/// </remarks>
[Collection("Net.D")]
public class UdpSendPathTests
{
    #region 已连接判定
    /// <summary>按远端端点判定已连接：通配地址或端口为0视为未连接</summary>
    [Theory(DisplayName = "UDP已连接判定_按远端端点_通配或零端口视为未连接")]
    [InlineData(null, false)]
    [InlineData("0.0.0.0|0", false)]
    [InlineData("0.0.0.0|1234", false)]
    [InlineData("127.0.0.1|0", false)]
    [InlineData("127.0.0.1|1234", true)]
    [InlineData("::|0", false)]
    [InlineData("::|1234", false)]
    [InlineData("::1|0", false)]
    [InlineData("::1|1234", true)]
    public void IsConnectedRemote_ByEndPoint(String? endPoint, Boolean expected)
    {
        IPEndPoint? ep = null;
        if (endPoint != null)
        {
            var ss = endPoint.Split('|');
            ep = new IPEndPoint(IPAddress.Parse(ss[0]), Int32.Parse(ss[1]));
        }

        Assert.Equal(expected, UdpServer.IsConnectedRemote(ep));
    }
    #endregion

    #region 未连接发送
    /// <summary>未连接的 Socket 走 SendTo：字节数组入口（ArraySegment/ReadOnlySpan 重载）必须照样送达</summary>
    [Fact(DisplayName = "UDP发送_未连接Socket_字节数组入口走SendTo并送达")]
    public void UnconnectedSocket_ByteArrayEntry_ReachesTarget()
    {
        using var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        receiver.ReceiveTimeout = 5_000;
        var ep = (IPEndPoint)receiver.LocalEndPoint!;

        // 打开时未设远端 → 不连接；打开后再设远端，发送必须走 SendTo
        using var udp = new UdpServer { Port = 0 };
        udp.Open();
        udp.Remote = new NetUri($"udp://127.0.0.1:{ep.Port}");

        var payload = "byte-array-entry"u8.ToArray();
        Assert.Equal(payload.Length, udp.Send(payload));

        var buf = new Byte[256];
        var n = receiver.Receive(buf, SocketFlags.None);
        Assert.Equal(payload, buf[..n]);
    }
    #endregion

    #region 已连接发送
    /// <summary>纯客户端实例打开后改远端：不得静默发往连接时的旧目标</summary>
    [Fact(DisplayName = "UDP客户端_打开后改Remote_不静默发往旧目标")]
    public void ConnectedClient_RemoteChanged_NotSilentlySentToOldTarget()
    {
        // 旧目标 A
        using var sockA = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sockA.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        sockA.ReceiveTimeout = 5_000;
        var epA = (IPEndPoint)sockA.LocalEndPoint!;

        // 新目标 B
        using var sockB = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sockB.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var epB = (IPEndPoint)sockB.LocalEndPoint!;

        // 纯客户端模式（未指定本地端口 + 打开前设远端）→ 打开时连接 A，发送走已连接的 Send
        using var udp = new UdpServer { Remote = new NetUri($"udp://127.0.0.1:{epA.Port}") };
        udp.Open();

        var payloadA = "to-A"u8.ToArray();
        Assert.Equal(payloadA.Length, udp.Send(payloadA));

        var buf = new Byte[256];
        Assert.Equal(payloadA, buf[..sockA.Receive(buf, SocketFlags.None)]);

        // 改目标后发送：目标与连接端点不一致，必须退回 SendTo 由内核报错（返回 -1），
        // 而不是沿用已连接的 Send 把数据静默发给 A
        udp.Remote = new NetUri($"udp://127.0.0.1:{epB.Port}");
        var payloadB = "to-B"u8.ToArray();
        udp.Send(payloadB);

        // A 不应再收到任何数据（旧实现会把 to-B 发给 A）
        EndPoint from = new IPEndPoint(IPAddress.Any, 0);
        var ex = Assert.Throws<SocketException>(() => sockA.ReceiveFrom(buf, ref from));
        Assert.Equal(SocketError.TimedOut, ex.SocketErrorCode);
    }
    #endregion
}
