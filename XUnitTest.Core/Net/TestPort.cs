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

    /// <summary>在用例专属候选段内探测端口并启动服务器，端口被抢占时顺延下一个候选端口重试</summary>
    /// <remarks>
    /// 候选段 = basePort + 宿主序号×1000，宽 50；各用例基址取 500 的倍数，任意“用例 × 宿主”候选段两两不相交。
    /// 本机 TCP 动态端口范围覆盖测试端口带（1024 起），本进程与其他进程的出站连接都可能随机占用候选端口，
    /// 探测与服务器实际绑定之间存在竞争窗口，被抢占时服务器绑定抛 WSAEADDRINUSE。
    /// 此处按候选段顺延重试，且每次都用新实例，避免复用半启动状态的服务器。
    /// </remarks>
    /// <typeparam name="T">服务器类型</typeparam>
    /// <param name="basePort">用例专属基址</param>
    /// <param name="factory">按候选端口创建尚未启动的服务器实例</param>
    /// <param name="start">启动服务器的动作</param>
    /// <returns>已启动的服务器实例</returns>
    public static T StartAny<T>(Int32 basePort, Func<Int32, T> factory, Action<T> start) where T : IDisposable
    {
        var begin = basePort + _host * 1000;
        for (var port = begin; port < begin + 50; port++)
        {
            if (!CanBind(port, SocketType.Stream) || !CanBind(port, SocketType.Dgram)) continue;

            var server = factory(port);
            try
            {
                start(server);
                return server;
            }
            catch (SocketException)
            {
                server.Dispose();
            }
        }

        throw new InvalidOperationException($"从 {begin} 起的 50 个候选端口均无法同时用于 TCP/UDP");
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
