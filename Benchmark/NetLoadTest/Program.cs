using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Net;

namespace NetLoadTest;

/// <summary>裸 Socket 层回环压测程序：同进程 echo 服务端 + N 客户端，测量吞吐、往返延迟与内存分配</summary>
/// <remarks>
/// <para>用法：dotnet run --project Benchmark/NetLoadTest -c Release -- [--mode pipeline|roundtrip] [--clients 4] [--size 1024] [--seconds 10] [--warmup 2] [--udp] [--frame 24]</para>
/// <para>pipeline：客户端持续发送不等待回显，测吞吐上限（MB/s、msg/s）；roundtrip：逐包往返，测 P50/P95/P99 延迟。</para>
/// <para>--frame：应用层帧大小，发送缓冲对齐到帧整倍数模拟粘包，吞吐折算为逻辑帧口径（对标历史 1.4 亿 pkt/s）。</para>
/// <para>分配数据来自进程级 GC.GetTotalAllocatedBytes，含服务端与客户端双向开销（同进程回环）。</para>
/// </remarks>
static class Program
{
    private static Int64 _serverBytes;
    private static Int64 _sentBytes;

    public static void Main(String[] args)
    {
        // 高并发下大量阻塞式收发任务会触线程池注入限速（~1线程/秒），预热最小线程避免秒级毛刺干扰测量
        ThreadPool.SetMinThreads(512, 512);

        var mode = GetArg(args, "--mode") ?? "pipeline";
        var clients = GetInt(args, "--clients", 4);
        var size = GetInt(args, "--size", 1024);
        var seconds = GetInt(args, "--seconds", 10);
        var warmup = GetInt(args, "--warmup", 2);
        var udp = args.Contains("--udp");
        var serverOnly = args.Contains("--server");
        var remote = GetArg(args, "--remote");
        var port = GetInt(args, "--port", 7800);
        var oneway = args.Contains("--oneway");
        var frame = GetInt(args, "--frame", 0);

        // --frame：应用层帧大小。发送缓冲对齐到帧整倍数，模拟“大量小帧粘成大包”的协议场景，
        // 统计口径折算为逻辑帧吞吐（对标历史 23.4Gbps ÷ 24B = 1.4 亿 pkt/s 记录）
        if (frame > 0) size = Math.Max(frame, size / frame * frame);

        var roundtrip = mode.Equals("roundtrip", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine("=== 裸Socket回环压测（同进程 echo）===");
        Console.WriteLine($"模式    : {(roundtrip ? "逐包往返（延迟）" : "流水线（吞吐）")}");
        Console.WriteLine($"协议    : {(udp ? "UDP" : "TCP")}");
        Console.WriteLine($"包大小  : {size:N0} B{(frame > 0 ? $"（含 {size / frame:N0} 个 {frame} B 逻辑帧，粘包口径）" : "")}");
        Console.WriteLine($"客户端  : {clients}");
        Console.WriteLine($"预热/窗口: {warmup} s / {seconds} s");
        Console.WriteLine();

        // ===== 服务端（echo + 计数） =====
        // 增大会话接收缓冲，降低大包分段的系统调用开销
        SocketSetting.Current.BufferSize = Math.Max(64 * 1024, size);

        NetServer? server = null;
        if (remote == null || serverOnly)
        {
            server = new NetServer
            {
                Port = serverOnly ? port : 0,
                ProtocolType = udp ? NetType.Udp : NetType.Tcp,
                AddressFamily = AddressFamily.InterNetwork,
            };
            server.Received += (s, e) =>
            {
                var pk = e.Packet;
                if (pk == null || pk.Length <= 0) return;

                Interlocked.Add(ref _serverBytes, pk.Length);

                // 单向上行模式只计数不回发，用于测量服务端纯接收吞吐（对齐历史口径）
                if (oneway) return;

                // TCP 服务端 sender 为 NetSession（INetSession）；UDP 服务端 sender 为 UdpSession（ISocketSession），两者接口不同
                if (s is INetSession ns) ns.Send(pk);
                else if (s is UdpSession us) us.Send(pk);
            };
            server.Start();
        }

        if (serverOnly)
        {
            // 独立服务端进程：打印就绪与每秒接收速率，持续运行（Ctrl+C 退出）
            Console.WriteLine($"READY {(udp ? "udp" : "tcp")}://0.0.0.0:{server!.Port}");
            var last = 0L;
            while (true)
            {
                Thread.Sleep(1000);
                var now = Interlocked.Read(ref _serverBytes);
                var rate = (now - last) / 1024.0 / 1024.0;
                if (frame > 0)
                {
                    // 粘包口径：吞吐按逻辑帧折算（接收字节 ÷ 帧大小）
                    var frames = (now - last) / (Double)frame;
                    Console.WriteLine($"[server] {rate:N1} MB/s  累计 {now / 1024.0 / 1024.0:N1} MB  帧率 {frames / 1_000_000:N2} M帧/s");
                }
                else
                {
                    Console.WriteLine($"[server] {rate:N1} MB/s  累计 {now / 1024.0 / 1024.0:N1} MB");
                }
                last = now;
            }
        }

        // ===== 客户端（拉取模式） =====
        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);

        var conns = new List<ISocketClient>();
        for (var i = 0; i < clients; i++)
        {
            var hostPort = remote ?? $"127.0.0.1:{server!.Port}";
            ISocketClient conn;
            if (udp)
            {
                conn = new UdpServer
                {
                    Remote = new NetUri($"udp://{hostPort}"),
                    AutoReceive = false,
                    // UDP 无流控可能丢包：短接收超时便于排水阶段识别“不再有数据”
                    Timeout = 300,
                };
            }
            else
            {
                conn = new TcpSession
                {
                    Remote = new NetUri($"tcp://{hostPort}"),
                    AutoReceive = false,
                    BufferSize = Math.Max(64 * 1024, size),
                    Timeout = 30_000,
                };
            }
            conn.Open();
            conns.Add(conn);
        }

        // 预热：让连接与接收环进入稳定状态
        Thread.Sleep(warmup * 1000);

        var alloc0 = GC.GetTotalAllocatedBytes(false);
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        var bytes0 = Interlocked.Read(ref _serverBytes);
        var sent0 = Interlocked.Read(ref _sentBytes);
        var sw = Stopwatch.StartNew();

        if (roundtrip)
            RunRoundTrip(conns, payload, size, seconds, udp);
        else if (oneway)
            RunOneWay(conns, payload, size, seconds);
        else
            RunPipeline(conns, payload, size, seconds, udp);

        sw.Stop();
        var bytes1 = Interlocked.Read(ref _serverBytes);
        var sent1 = Interlocked.Read(ref _sentBytes);
        var alloc1 = GC.GetTotalAllocatedBytes(false);

        // ===== 统计 =====
        // 本地模式以服务端回显字节为准；远程模式（--remote）以客户端发送字节为准（TCP 回环等值）
        // 单向上行按固定发送窗计时（排水阶段不计入窗口）
        var elapsed = oneway ? seconds : sw.Elapsed.TotalSeconds;
        var totalBytes = remote != null ? sent1 - sent0 : bytes1 - bytes0;
        var totalMsgs = totalBytes / size;
        var mbps = totalBytes / elapsed / (1024.0 * 1024.0);
        var allocPerMsg = totalMsgs > 0 ? (alloc1 - alloc0) / (Double)totalMsgs : 0;

        Console.WriteLine();
        Console.WriteLine("------- 结果 -------");
        Console.WriteLine($"吞吐      : {totalMsgs / elapsed:N0} msg/s | {mbps:N1} MB/s");
        if (frame > 0)
        {
            // 粘包口径：按逻辑帧折算吞吐，对标历史“1.4 亿 pkt/s”（23.4Gbps ÷ 24B）
            var totalFrames = totalBytes / frame;
            Console.WriteLine($"帧吞吐    : {totalFrames / elapsed:N0} frame/s（帧大小 {frame} B，每大包 {size / frame:N0} 帧）");
        }
        Console.WriteLine($"服务端回显: {totalMsgs:N0} 包 / {totalBytes:N0} B（窗口 {elapsed:F2} s）");
        Console.WriteLine($"分配      : {allocPerMsg:N1} B/msg | 窗口总分配 {(alloc1 - alloc0) / (1024.0 * 1024.0):N1} MB");
        Console.WriteLine($"GC        : Gen0 +{GC.CollectionCount(0) - gen0} Gen1 +{GC.CollectionCount(1) - gen1} Gen2 +{GC.CollectionCount(2) - gen2}");

        foreach (var conn in conns) conn.Dispose();
        server?.Dispose();
    }

    /// <summary>单向上行：仅发送不回读，测服务端纯接收吞吐（配合 --server 分离进程消除 CPU 共享）</summary>
    private static void RunOneWay(List<ISocketClient> conns, Byte[] payload, Int32 size, Int32 seconds)
    {
        var sent = new Int64[conns.Count];
        var tasks = new List<Task>();
        for (var i = 0; i < conns.Count; i++)
        {
            var idx = i;
            var conn = conns[idx];
            tasks.Add(Task.Run(() =>
            {
                var n = 0L;
                var batch = 0L;
                // 各客户端独立时间窗，到点即停（阻塞中的 Send 返回后立即退出）
                var deadline = Stopwatch.GetTimestamp() + (Int64)(seconds * Stopwatch.Frequency);
                while (Stopwatch.GetTimestamp() < deadline)
                {
                    conn.Send(payload);
                    n++;
                    batch += payload.Length;
                    if ((n & 0xFF) == 0)
                    {
                        Interlocked.Add(ref _sentBytes, batch);
                        batch = 0;
                    }
                }
                Interlocked.Add(ref _sentBytes, batch);
                sent[idx] = n;
            }));
        }

        // 发送窗结束后仍有少量在途：TCP 流控下缓冲满时会稍晚返回，宽限等待
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(seconds + 60));
        var total = sent.Sum(n => n);
        Console.WriteLine($"完整发送    : {total:N0} 包 / {total * (Int64)size:N0} B（无回读，接收真值以服务端计数为准）");
    }

    /// <summary>流水线模式：持续发送 + 并发读取回显，发送停止后排水读满</summary>
    private static void RunPipeline(List<ISocketClient> conns, Byte[] payload, Int32 size, Int32 seconds, Boolean tolerateLoss)
    {
        var runners = conns.Select(c => new PipelineClient(c, size, tolerateLoss)).ToArray();
        var tasks = new List<Task>();
        foreach (var r in runners)
        {
            tasks.Add(r.StartSend(payload));
            tasks.Add(r.StartReceive());
        }

        Thread.Sleep(seconds * 1000);

        foreach (var r in runners) r.Stop();

        // 排水：等待接收任务读满发送总量（限时，防对端丢包导致死等）
        var drained = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(60));
        if (!drained)
        {
            foreach (var c in conns) c.Close("drain-timeout");
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
            Console.WriteLine("警告：排水超时，部分回显未读满（可能存在丢包）");
        }

        var sent = runners.Sum(r => r.SentPackets);
        var received = runners.Sum(r => r.ReceivedBytes);
        Console.WriteLine($"完整性    : 发送 {sent:N0} 包 / 接收 {received / size:N0} 包（差 {sent - received / size:N0}）");
    }

    /// <summary>往返模式：逐包发送并读满回显，采样单次往返耗时</summary>
    private static void RunRoundTrip(List<ISocketClient> conns, Byte[] payload, Int32 size, Int32 seconds, Boolean tolerateLoss)
    {
        var runners = conns.Select(c => new RoundTripClient(c, size, tolerateLoss)).ToArray();
        var tasks = runners.Select(r => r.Start(payload)).ToArray();

        Thread.Sleep(seconds * 1000);

        foreach (var r in runners) r.Stop();

        var done = Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
        if (!done)
        {
            foreach (var c in conns) c.Close("sleep-timeout");
            Task.WaitAll(tasks, TimeSpan.FromSeconds(10));
            Console.WriteLine("警告：部分往返任务未及时结束");
        }

        // 合并样本并计算分位数
        var samples = new List<Double>();
        foreach (var r in runners) samples.AddRange(r.Samples);
        if (samples.Count == 0)
        {
            Console.WriteLine("警告：未采集到往返样本");
            return;
        }
        samples.Sort();

        var avg = samples.Average();
        Console.WriteLine($"往返样本  : {samples.Count:N0} 次（{String.Join(" + ", runners.Select(r => r.Samples.Count))}）");
        Console.WriteLine($"延迟(往返): P50={Percentile(samples, 0.50):F1}µs  P95={Percentile(samples, 0.95):F1}µs  P99={Percentile(samples, 0.99):F1}µs");
        Console.WriteLine($"           平均={avg:F1}µs  最小={samples[0]:F1}µs  最大={samples[^1]:F1}µs");
    }

    /// <summary>取分位数（升序样本）</summary>
    private static Double Percentile(List<Double> sorted, Double p)
    {
        var idx = (Int32)Math.Min(sorted.Count - 1, Math.Max(0, Math.Round((sorted.Count - 1) * p)));

        return sorted[idx];
    }

    private static String? GetArg(String[] args, String name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }

        return null;
    }

    private static Int32 GetInt(String[] args, String name, Int32 defaultValue)
    {
        var text = GetArg(args, name);

        return text != null && Int32.TryParse(text, out var value) ? value : defaultValue;
    }

    /// <summary>流水线客户端：一个发送任务 + 一个接收任务，接收任务读满发送总量（UDP 容忍丢包）</summary>
    private sealed class PipelineClient
    {
        private readonly ISocketClient _conn;
        private readonly Int32 _size;
        private readonly Boolean _tolerateLoss;
        private volatile Boolean _stopped;

        public Int64 SentPackets;
        public Int64 ReceivedBytes;

        public PipelineClient(ISocketClient conn, Int32 size, Boolean tolerateLoss)
        {
            _conn = conn;
            _size = size;
            _tolerateLoss = tolerateLoss;
        }

        public Task StartSend(Byte[] payload)
            => Task.Run(() =>
            {
                var n = 0L;
                var batch = 0L;
                try
                {
                    while (!_stopped)
                    {
                        _conn.Send(payload);
                        n++;
                        batch += payload.Length;
                        // 每 256 包汇入一次全局发送计数，兼顾实时窗口统计与低原子开销
                        if ((n & 0xFF) == 0)
                        {
                            Interlocked.Add(ref _sentBytes, batch);
                            batch = 0;
                        }
                    }
                }
                finally
                {
                    Interlocked.Add(ref _sentBytes, batch);
                    SentPackets = n;
                }
            });

        public Task StartReceive()
            => Task.Run(() =>
            {
                var bytes = 0L;
                // 发送未结束前持续读取；发送结束后读满发送总量（排水）
                while (SentPackets == 0 || bytes < SentPackets * _size)
                {
                    try
                    {
                        using var pk = _conn.Receive();
                        if (pk == null || pk.Length <= 0) break;

                        bytes += pk.Length;
                    }
                    catch (SocketException ex) when (_tolerateLoss && ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        // UDP 无流控可能丢包：发送已停止且接收超时，视为排水结束
                        if (SentPackets > 0) break;
                    }
                }
                ReceivedBytes = bytes;
            });

        public void Stop() => _stopped = true;
    }

    /// <summary>往返客户端：串行逐包往返，采样单次耗时（微秒）；UDP 丢包时跳过该次继续</summary>
    private sealed class RoundTripClient
    {
        private readonly ISocketClient _conn;
        private readonly Int32 _size;
        private readonly Boolean _tolerateLoss;
        private volatile Boolean _stopped;

        public List<Double> Samples { get; } = [];

        public RoundTripClient(ISocketClient conn, Int32 size, Boolean tolerateLoss)
        {
            _conn = conn;
            _size = size;
            _tolerateLoss = tolerateLoss;
        }

        public Task Start(Byte[] payload)
            => Task.Run(() =>
            {
                while (!_stopped)
                {
                    try
                    {
                        var t0 = Stopwatch.GetTimestamp();
                        _conn.Send(payload);

                        var need = _size;
                        while (need > 0)
                        {
                            using var pk = _conn.Receive();
                            if (pk == null || pk.Length <= 0) throw new TimeoutException("回显中断");

                            need -= pk.Length;
                        }

                        var us = (Stopwatch.GetTimestamp() - t0) * 1_000_000.0 / Stopwatch.Frequency;
                        Samples.Add(us);
                    }
                    catch (SocketException ex) when (_tolerateLoss && ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        // UDP 丢包：跳过该次往返，继续下一轮（回显错位由协议本质决定，样本仍近似往返时延）
                    }
                }
            });

        public void Stop() => _stopped = true;
    }
}
