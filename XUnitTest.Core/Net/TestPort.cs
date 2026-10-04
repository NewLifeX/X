using System.Net;
using System.Net.Sockets;

namespace XUnitTest.Net;

/// <summary>测试用显式端口分配。仅用于“同一端口同时监听 TCP/UDP”的用例</summary>
/// <remarks>
/// 单协议用例保持动态端口（Port=0 由系统分配，天然对该协议可用）。
/// 双协议用例若用 Port=0，先启动的协议选中的端口可能落在另一协议的保留段（WSAEACCES）；
/// 本类在用例专属候选段内顺序探测第一个 TCP 与 UDP 均可绑定的端口，显式交给服务器，
/// 不经过系统分配与跨协议端口继承。宿主序号取自目标框架输出目录（VS 默认并行运行 5 个目标框架），
/// 各宿主候选段互不相交，多宿主并行不互抢。
/// </remarks>
internal static class TestPort
{
    /// <summary>宿主序号：net8.0=0 / net8.0-windows=1 / net9.0=2 / net10.0=3 / net10.0-windows=4</summary>
    private static readonly Int32 _host = GetHostIndex();

    /// <summary>在用例专属候选段内取一个 TCP 与 UDP 均可绑定的显式端口</summary>
    /// <param name="basePort">用例专属基址（取 2xxxx 低段，避开 Windows 高段动态/保留区）</param>
    /// <returns>可同时用于 TCP 与 UDP 监听的端口</returns>
    public static Int32 PickDual(Int32 basePort)
    {
        var start = basePort + _host * 100;
        for (var port = start; port < start + 50; port++)
        {
            if (CanBind(port, SocketType.Stream) && CanBind(port, SocketType.Dgram)) return port;
        }

        throw new InvalidOperationException($"从 {start} 起的 50 个候选端口均无法同时用于 TCP/UDP");
    }

    private static Boolean CanBind(Int32 port, SocketType type)
    {
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
        var dir = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Name;
        return dir switch
        {
            "net8.0" => 0,
            "net8.0-windows" => 1,
            "net9.0" => 2,
            "net10.0" => 3,
            "net10.0-windows" => 4,
            _ => 5,
        };
    }
}
