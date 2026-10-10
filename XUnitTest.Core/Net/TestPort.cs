using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Net;

namespace XUnitTest.Net;

/// <summary>测试用显式端口分配。仅用于“同一端口同时监听 TCP/UDP”的用例</summary>
/// <remarks>
/// 单协议用例保持动态端口（Port=0 由系统分配，天然对该协议可用）。
/// 双协议用例若用 Port=0，先启动的协议选中的端口可能落在另一协议的保留段（WSAEACCES）；
/// 本类在用例专属候选段内顺序探测第一个 TCP 与 UDP 均可绑定的端口，显式交给服务器，
/// 不经过系统分配与跨协议端口继承。宿主序号取自输出目录的“工程目录 + 目标框架”，
/// 四个测试工程共 10 个宿主（VS 并行多目标框架与本地并行跑测脚本均适用），各宿主候选段互不相交，多宿主并行不互抢。
/// </remarks>
internal static class TestPort
{
    /// <summary>宿主序号（工程目录 + TFM）：UnitTest/net8.0=0 … UnitTestSG/net10.0=9，未登记目录兜底 10</summary>
    private static readonly Int32 _host = GetHostIndex();

    /// <summary>在用例专属候选段内取一个 TCP 与 UDP 均可绑定的显式端口</summary>
    /// <remarks>
    /// 候选段 = basePort + 宿主序号×1000，宽 50。各用例基址取 500 的倍数（26xxx 低段，
    /// 避开 Syncthing 22000 等常见服务与 Windows 高段动态/保留区）；宿主步长 1000、段宽 50，
    /// 任意“用例 × 宿主”候选段两两不相交（起点均为 500 的倍数，两两相隔至少 500）。
    /// </remarks>
    /// <param name="basePort">用例专属基址</param>
    /// <returns>可同时用于 TCP 与 UDP 监听的端口</returns>
    public static Int32 PickDual(Int32 basePort)
    {
        var start = basePort + _host * 1000;
        for (var port = start; port < start + 50; port++)
        {
            if (CanBind(port, SocketType.Stream) && CanBind(port, SocketType.Dgram)) return port;
        }

        throw new InvalidOperationException($"从 {start} 起的 50 个候选端口均无法同时用于 TCP/UDP");
    }

    private static Boolean CanBind(Int32 port, SocketType type)
    {
        var netType = type == SocketType.Stream ? NetType.Tcp : NetType.Udp;

        // 与 NetServer.CreateServer 相同口径：已有监听者的端口一律跳过。
        // 对端可能设置了 SO_REUSEADDR（如 Syncthing 等 Go 程序），此时裸 bind 探测会误判“可绑”，
        // 而 NetServer 会按 CheckPort 拒绝该地址，导致“Failed to listen to all ports”
        if (IPAddress.Any.CheckPort(netType, port)) return false;

        var protocol = type == SocketType.Stream ? System.Net.Sockets.ProtocolType.Tcp : System.Net.Sockets.ProtocolType.Udp;
        using var socket = new Socket(AddressFamily.InterNetwork, type, protocol);
        try
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static Int32 GetHostIndex()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var tfm = dir.Name;
        var project = dir.Parent?.Name;
        return (project, tfm) switch
        {
            ("UnitTest", "net8.0") => 0,
            ("UnitTest", "net8.0-windows") => 1,
            ("UnitTest", "net9.0") => 2,
            ("UnitTest", "net10.0") => 3,
            ("UnitTest", "net10.0-windows") => 4,
            ("UnitTest6", "net6.0") => 5,
            ("UnitTest6", "net6.0-windows") => 6,
            ("UnitTestCompat", "net462") => 7,
            ("UnitTestCompat", "net8.0") => 8,
            ("UnitTestSG", "net10.0") => 9,
            _ => 10,
        };
    }
}
