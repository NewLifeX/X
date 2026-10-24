using System.Net;
using System.Net.Sockets;

namespace XUnitTest;

/// <summary>本机连接黑洞：监听但从不 Accept，accept 队列打满后新连接停在半开状态</summary>
/// <remarks>
/// <para>用于验证"建连阶段必须受超时预算约束"的用例：没有预算约束时，建连要等操作系统默认 TCP 超时
/// （Windows 约 21 秒），用例无从收敛。黑洞完全在本机构造，不依赖任何外部网络。</para>
/// <para>构造自带校验：只有确实观察到探测连接在窗口内不完成时才视为可用；本机 backlog 语义不支持
/// 时返回 null，用例跳过（属本机环境差异，与外部依赖无关）。</para>
/// </remarks>
internal sealed class TestBlackhole : IDisposable
{
    private readonly Socket _listener;
    private readonly List<TcpClient> _prefill = [];

    private TestBlackhole(Socket listener, Int32 port)
    {
        _listener = listener;
        Port = port;
    }

    /// <summary>黑洞端口。连接该端口会停在半开状态，既不通也不立刻失败</summary>
    public Int32 Port { get; }

    /// <summary>尝试构造黑洞。无法构造（本机 backlog 语义不支持）时返回 null</summary>
    /// <returns>黑洞实例；不可用时返回 null</returns>
    public static TestBlackhole? TryCreate()
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
        }
        catch
        {
            listener.Dispose();

            return null;
        }

        var blackhole = new TestBlackhole(listener, ((IPEndPoint)listener.LocalEndPoint!).Port);
        try
        {
            // 逐批连接压满 accept 队列（不等待完成，握手完成的连接驻留队列），再用探测连接确认"半开"成立。
            // 调用方给出的超时预算必须明显小于探测窗口，确保探测通过后建连仍处在半开窗口内
            for (var round = 0; round < 8; round++)
            {
                for (var i = 0; i < 32; i++) blackhole.Connect();

                Thread.Sleep(150);

                var probe = new TcpClient();
                blackhole._prefill.Add(probe);
                var task = Observe(probe.ConnectAsync(IPAddress.Loopback, blackhole.Port));

                var pending = false;
                try { pending = !task.Wait(1000); }
                catch { }

                if (pending) return blackhole;
            }
        }
        catch { }

        blackhole.Dispose();

        return null;
    }

    private void Connect()
    {
        var client = new TcpClient();
        _prefill.Add(client);
        Observe(client.ConnectAsync(IPAddress.Loopback, Port));
    }

    /// <summary>观察连接任务异常，避免未观察异常在用例结束后抛到全局</summary>
    private static Task Observe(Task task)
    {
        _ = task.ContinueWith(static t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

        return task;
    }

    /// <summary>释放全部预占连接与监听器</summary>
    public void Dispose()
    {
        foreach (var item in _prefill)
        {
            try { item.Dispose(); } catch { }
        }
        _prefill.Clear();

        try { _listener.Dispose(); } catch { }
    }
}
