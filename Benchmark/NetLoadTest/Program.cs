using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using NewLife;
using NewLife.Net;

namespace NetLoadTest;

/// <summary>裸 Socket 层压测程序：echo 服务端（可独立进程）+ N 客户端，测量吞吐、往返延迟与内存分配</summary>
/// <remarks>
/// <para>用法：dotnet run --project Benchmark/NetLoadTest -c Release -- [--mode pipeline|roundtrip] [--clients 4] [--size 1024] [--seconds 10] [--warmup 2] [--recvmode sync|asyncpull|event] [--udp] [--frame 24]</para>
/// <para>pipeline：客户端持续发送不等待回显，测吞吐上限（MB/s、msg/s）；roundtrip：逐包往返，测 P50/P95/P99 延迟；--oneway：只发不读，配合 --server 分离进程测服务端纯接收吞吐。</para>
/// <para>--recvmode：客户端接收模式。sync=同步拉取 Receive（阻塞等待）；asyncpull=异步拉取 ReceiveAsync；event=事件接收（AutoReceive=true + Received 回调，与服务器接收模型同构，零阻塞线程）。</para>
/// <para>--frame：应用层帧大小，发送缓冲对齐到帧整倍数模拟粘包，吞吐折算为逻辑帧口径（对标历史 1.4 亿 pkt/s）。</para>
/// <para>--server：独立服务端进程，收到数据后静默 3 秒输出 STEADY 稳态中位数并退出（含服务端分配 B/msg）；--remote 客户端连接独立服务端。</para>
/// <para>--udpconnect：UDP 客户端将 Socket Connect 到目标端（实验），发送走已连接路径（零分配）。</para>
/// <para>分配数据来自进程级 GC.GetTotalAllocatedBytes（分离进程时仅本进程开销）。末尾输出 SUMMARY:{json} 机器可读汇总，供跑批脚本解析。</para>
/// </remarks>
static class Program
{
    private static Int64 _serverBytes;
    private static Int64 _sentBytes;

    public static void Main(String[] args)
    {
        // 统一 UTF-8 输出，保证重定向日志（跑批脚本采集）中文不乱码
        Console.OutputEncoding = System.Text.Encoding.UTF8;

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
        var udpConnect = args.Contains("--udpconnect");
        var recvMode = (GetArg(args, "--recvmode") ?? "sync").ToLowerInvariant();
        if (recvMode is not ("sync" or "asyncpull" or "event"))
        {
            Console.WriteLine($"未知接收模式 --recvmode {recvMode}，可选 sync|asyncpull|event");
            return;
        }

        // UDP 单包上限：IP 首部 20 + UDP 首部 8 后最大负载 65507，超出时内核直接拒绝（Send 返回 -1）
        if (udp && size > 65507) size = 65507;

        // --frame：应用层帧大小。发送缓冲对齐到帧整倍数，模拟“大量小帧粘成大包”的协议场景，
        // 统计口径折算为逻辑帧吞吐（对标历史 23.4Gbps ÷ 24B = 1.4 亿 pkt/s 记录）
        if (frame > 0) size = Math.Max(frame, size / frame * frame);

        var roundtrip = mode.Equals("roundtrip", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine("=== 裸Socket压测（echo 服务端）===");
        Console.WriteLine($"模式    : {(roundtrip ? "逐包往返（延迟）" : oneway ? "单向上行（吞吐）" : "流水线（吞吐）")}");
        Console.WriteLine($"协议    : {(udp ? "UDP" : "TCP")}");
        Console.WriteLine($"包大小  : {size:N0} B{(frame > 0 ? $"（含 {size / frame:N0} 个 {frame} B 逻辑帧，粘包口径）" : "")}");
        Console.WriteLine($"客户端  : {clients}    接收模式: {recvMode}{(udpConnect ? "    UDP-Connect实验" : "")}");
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
            // 独立服务端进程：打印就绪与每秒接收速率；收到数据后连续静默 3 秒视为客户端结束，输出稳态中位数并退出
            Console.WriteLine($"READY {(udp ? "udp" : "tcp")}://0.0.0.0:{server!.Port}");
            var last = 0L;
            var rates = new List<Double>();
            var idle = 0;
            // 服务端分配统计：接收路径（含回显发送）的托管分配总量，按接收消息数折算 B/msg
            var srvAlloc0 = GC.GetTotalAllocatedBytes(false);
            var srvGc0 = GC.CollectionCount(0);
            var srvGc1 = GC.CollectionCount(1);
            var srvGc2 = GC.CollectionCount(2);
            while (true)
            {
                Thread.Sleep(1000);
                var now = Interlocked.Read(ref _serverBytes);
                var delta = now - last;
                var rate = delta / 1024.0 / 1024.0;
                if (delta > 0)
                {
                    rates.Add(rate);
                    idle = 0;
                }
                else
                {
                    idle++;
                }
                if (frame > 0)
                {
                    // 粘包口径：吞吐按逻辑帧折算（接收字节 ÷ 帧大小）
                    Console.WriteLine($"[server] {rate:N1} MB/s  累计 {now / 1024.0 / 1024.0:N1} MB  帧率 {delta / (Double)frame / 1_000_000:N2} M帧/s");
                }
                else
                {
                    Console.WriteLine($"[server] {rate:N1} MB/s  累计 {now / 1024.0 / 1024.0:N1} MB");
                }
                last = now;

                if (idle >= 3 && rates.Count > 0)
                {
                    // 丢掉前 2 秒爬升段（连接建立/慢启动），取剩余秒采样中位数作为稳态
                    var window = rates.Count > 6 ? rates.GetRange(2, rates.Count - 2) : rates;
                    var steady = Median(window);
                    var msgs = now / size;
                    var srvAllocPerMsg = msgs > 0 ? (GC.GetTotalAllocatedBytes(false) - srvAlloc0) / (Double)msgs : 0;
                    var line = $"[server] STEADY {steady:N1} MB/s  Gbps={steady * 0.008388608:N2}  pkt={steady * 1048576 / size:N0} pkt/s";
                    if (frame > 0) line += $"  frame={steady * 1048576 / frame:N0} frame/s";
                    line += $"  alloc={srvAllocPerMsg:N2} B/msg  gc={GC.CollectionCount(0) - srvGc0}/{GC.CollectionCount(1) - srvGc1}/{GC.CollectionCount(2) - srvGc2}  秒采样={window.Count}";
                    Console.WriteLine(line);
                    break;
                }

                // 从未收到任何数据（客户端异常或包超限被内核拒绝）：10 秒后退出，避免跑批脚本挂等
                if (idle >= 10 && rates.Count == 0)
                {
                    Console.WriteLine("[server] STEADY 0.0 MB/s  Gbps=0  pkt=0 pkt/s  alloc=0  gc=0/0/0  秒采样=0");
                    break;
                }
            }
            server.Dispose();
            return;
        }

        // ===== 客户端 =====
        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);

        // event 模式走接收环事件推送（AutoReceive=true）；sync/asyncpull 为拉取模式（打开前必须关自动接收）
        var autoReceive = recvMode == "event";
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
                    AutoReceive = autoReceive,
                    // UDP 无流控可能丢包：短接收超时便于排水阶段识别“不再有数据”
                    Timeout = 300,
                };
            }
            else
            {
                conn = new TcpSession
                {
                    Remote = new NetUri($"tcp://{hostPort}"),
                    AutoReceive = autoReceive,
                    BufferSize = Math.Max(64 * 1024, size),
                    Timeout = 30_000,
                };
            }
            conn.Open();

            // UDP Connect 化实验：连接后发送走已连接路径（零分配、省 SendTo 序列化开销）
            if (udp && udpConnect && conn is SessionBase sb && sb.Client is Socket usk)
            {
                var idx = hostPort.LastIndexOf(':');
                try
                {
                    usk.Connect(new IPEndPoint(IPAddress.Parse(hostPort[..idx]), Int32.Parse(hostPort[(idx + 1)..])));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UDP Connect 失败：{ex.Message}");
                }
            }

            conns.Add(conn);
        }

        // 预热：让连接与接收环进入稳定状态
        Thread.Sleep(warmup * 1000);

        // 预热流量：短促收发触发服务端回显/接收链路与客户端接收链路的首次 JIT 与池化初始化。
        // 每个场景都是新起的服务端进程，若不做这轮预热，首个测量样本会把服务端冷启动成本（百毫秒级）计入尾延迟。
        PrimeTrafficAsync(oneway, udp, remote, server, size, recvMode, payload).GetAwaiter().GetResult();

        var alloc0 = GC.GetTotalAllocatedBytes(false);
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        var bytes0 = Interlocked.Read(ref _serverBytes);
        var sent0 = Interlocked.Read(ref _sentBytes);
        var sw = Stopwatch.StartNew();

        List<Double>? samples = null;
        Int64 sent = 0, received = 0;
        if (roundtrip)
            samples = RunRoundTrip(conns, payload, size, seconds, udp, recvMode);
        else if (oneway)
            RunOneWay(conns, payload, size, seconds);
        else
            (sent, received) = RunPipeline(conns, payload, size, seconds, udp, recvMode);

        sw.Stop();
        var sent1 = Interlocked.Read(ref _sentBytes);
        var alloc1 = GC.GetTotalAllocatedBytes(false);

        // ===== 统计 =====
        // 单向上行按固定发送窗计时（排水阶段不计入窗口）；其余按实际墙钟
        var elapsed = oneway ? (Double)seconds : sw.Elapsed.TotalSeconds;
        var sentBytes = sent1 - sent0;
        // pipeline 吞吐取客户端收到的回显字节；oneway 无回读取发送字节
        var totalBytes = oneway ? sentBytes : received;
        var msgCount = roundtrip ? samples?.Count ?? 0 : totalBytes / size;
        var allocPerMsg = msgCount > 0 ? (alloc1 - alloc0) / (Double)msgCount : 0;

        Console.WriteLine();
        Console.WriteLine("------- 结果 -------");
        if (!roundtrip)
        {
            var mbps = totalBytes / elapsed / (1024.0 * 1024.0);
            Console.WriteLine($"吞吐      : {totalBytes / size / elapsed:N0} msg/s | {mbps:N1} MB/s");
            if (frame > 0)
            {
                // 粘包口径：按逻辑帧折算吞吐，对标历史“1.4 亿 pkt/s”（23.4Gbps ÷ 24B）
                var totalFrames = totalBytes / frame;
                Console.WriteLine($"帧吞吐    : {totalFrames / elapsed:N0} frame/s（帧大小 {frame} B，每大包 {size / frame:N0} 帧）");
            }
            Console.WriteLine($"{(oneway ? "发送" : "回显")}      : {totalBytes / size:N0} 包 / {totalBytes:N0} B（窗口 {elapsed:F2} s）");
        }
        Console.WriteLine($"分配      : {allocPerMsg:N1} B/msg | 窗口总分配 {(alloc1 - alloc0) / (1024.0 * 1024.0):N1} MB");
        Console.WriteLine($"GC        : Gen0 +{GC.CollectionCount(0) - gen0} Gen1 +{GC.CollectionCount(1) - gen1} Gen2 +{GC.CollectionCount(2) - gen2}");

        // 机器可读汇总（跑批脚本按 SUMMARY: 前缀解析 JSON）
        var summary = new Dictionary<String, Object?>
        {
            ["mode"] = roundtrip ? "roundtrip" : oneway ? "oneway" : "pipeline",
            ["protocol"] = udp ? "udp" : "tcp",
            ["recvMode"] = recvMode,
            ["clients"] = clients,
            ["size"] = size,
            ["frame"] = frame,
            ["elapsed"] = Math.Round(elapsed, 3),
            ["sentBytes"] = sentBytes,
            ["allocPerMsg"] = Math.Round(allocPerMsg, 2),
            ["gc0"] = GC.CollectionCount(0) - gen0,
            ["gc1"] = GC.CollectionCount(1) - gen1,
            ["gc2"] = GC.CollectionCount(2) - gen2,
        };
        if (roundtrip)
        {
            summary["latencyCount"] = samples?.Count ?? 0;
            if (samples is { Count: > 0 })
            {
                summary["p50"] = Math.Round(Percentile(samples, 0.50), 3);
                summary["p95"] = Math.Round(Percentile(samples, 0.95), 3);
                summary["p99"] = Math.Round(Percentile(samples, 0.99), 3);
                summary["avg"] = Math.Round(samples.Average(), 3);
                summary["max"] = Math.Round(samples[^1], 3);
            }
        }
        else
        {
            summary["recvBytes"] = totalBytes;
            summary["msgsPerSec"] = Math.Round(totalBytes / size / elapsed, 1);
            summary["mbps"] = Math.Round(totalBytes / elapsed / (1024.0 * 1024.0), 2);
            if (frame > 0) summary["framesPerSec"] = Math.Round(totalBytes / frame / elapsed, 1);
            if (!oneway)
            {
                var sentMsgs = sentBytes / size;
                var recvMsgs = totalBytes / size;
                summary["integrityDiff"] = sentMsgs - recvMsgs;
                if (udp && sentMsgs > 0) summary["lossPct"] = Math.Round((sentMsgs - recvMsgs) * 100.0 / sentMsgs, 2);
            }
        }
        Console.WriteLine("SUMMARY:" + JsonSerializer.Serialize(summary));

        foreach (var conn in conns) conn.Dispose();
        server?.Dispose();
    }

    /// <summary>单向上行：仅发送不回读，测服务端纯接收吞吐（配合 --server 分离进程消除 CPU 共享）</summary>
    private static void RunOneWay(List<ISocketClient> conns, Byte[] payload, Int32 size, Int32 seconds)
    {
        var tasks = new List<Task>();
        for (var i = 0; i < conns.Count; i++)
        {
            var conn = conns[i];
            tasks.Add(Task.Run(() =>
            {
                var n = 0L;
                var batch = 0L;
                // 各客户端独立时间窗，到点即停（阻塞中的 Send 返回后立即退出）
                var deadline = Stopwatch.GetTimestamp() + (Int64)(seconds * Stopwatch.Frequency);
                var errors = 0;
                while (Stopwatch.GetTimestamp() < deadline)
                {
                    // 发送失败（如 UDP 包超限返回 -1）不计入发送量，连续失败则提前退出防死转
                    if (conn.Send(payload) <= 0)
                    {
                        if (++errors > 1000) break;
                        continue;
                    }
                    errors = 0;
                    n++;
                    batch += payload.Length;
                    if ((n & 0xFF) == 0)
                    {
                        Interlocked.Add(ref _sentBytes, batch);
                        batch = 0;
                    }
                }
                Interlocked.Add(ref _sentBytes, batch);
            }));
        }

        // 发送窗结束后仍有少量在途：TCP 流控下缓冲满时会稍晚返回，宽限等待。
        // 服务端饱和场景中 Send 可能长期阻塞于零窗口，宽限 10 秒后放弃等待：
        // 发送统计直接读全局累计（_sentBytes），不依赖阻塞任务是否返回
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
        var totalBytes = Interlocked.Read(ref _sentBytes);
        Console.WriteLine($"完整发送    : {totalBytes / size:N0} 包 / {totalBytes:N0} B（无回读，接收真值以服务端计数为准）");
    }

    /// <summary>预热流量：触发服务端收发链路与客户端接收链路的首次 JIT 与池化初始化，避免冷启动成本计入测量</summary>
    private static async Task PrimeTrafficAsync(Boolean oneway, Boolean udp, String? remote, NetServer? server, Int32 size, String recvMode, Byte[] payload)
    {
        var hostPort = remote ?? $"127.0.0.1:{server!.Port}";
        // 总预热流量控制在 4MB 量级：次数够触发 JIT，大包又不至于把内核缓冲堵满
        var count = Math.Clamp(4 * 1024 * 1024 / size, 4, 64);

        // 单向模式服务端不回显：只发不收，触发服务端接收链路 JIT
        if (oneway)
        {
            using var pc = CreateClient(udp, hostPort, false, size);
            pc.Open();
            for (var i = 0; i < count; i++) pc.Send(payload);
            Thread.Sleep(200);
            return;
        }

        // 事件模式：回执走接收环回调，预热客户端事件链路（与测量模式一致）
        if (recvMode == "event")
        {
            var pc = CreateClient(udp, hostPort, true, size);
            try
            {
                var got = 0L;
                using var done = new ManualResetEventSlim();
                pc.Received += (s, e) =>
                {
                    var n = e.Packet?.Length ?? 0;
                    if (n > 0 && Interlocked.Add(ref got, n) >= (Int64)count * size) done.Set();
                };
                pc.Open();
                for (var i = 0; i < count; i++) pc.Send(payload);
                done.Wait(3000);  // UDP 容忍丢包：超时即视为预热足够
            }
            finally
            {
                pc.Dispose();
            }
            return;
        }

        // 拉取模式：串行往返（TCP 异步拉取走 ReceiveAsync，与测量路径一致；UDP 用短超时同步接收）
        using (var pc = CreateClient(udp, hostPort, false, size))
        {
            pc.Open();
            for (var i = 0; i < count; i++)
            {
                pc.Send(payload);
                var need = size;
                while (need > 0)
                {
                    try
                    {
                        using var pk = !udp && recvMode == "asyncpull" ? await pc.ReceiveAsync() : pc.Receive();
                        if (pk == null || pk.Length <= 0) break;
                        need -= pk.Length;
                    }
                    catch (SocketException ex) when (udp && ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        // UDP 丢包：跳过该次
                        break;
                    }
                }
            }
        }
    }

    /// <summary>创建指定接收模式的客户端连接（UDP 客户端用 UdpServer 承载）</summary>
    private static ISocketClient CreateClient(Boolean udp, String hostPort, Boolean autoReceive, Int32 size)
        => udp
        ? new UdpServer
        {
            Remote = new NetUri($"udp://{hostPort}"),
            AutoReceive = autoReceive,
            Timeout = 300,
        }
        : new TcpSession
        {
            Remote = new NetUri($"tcp://{hostPort}"),
            AutoReceive = autoReceive,
            BufferSize = Math.Max(64 * 1024, size),
            Timeout = 30_000,
        };

    /// <summary>流水线模式：持续发送 + 并发接收回显（同步/异步拉取或事件接收），发送停止后排水读满</summary>
    private static (Int64 Sent, Int64 Received) RunPipeline(List<ISocketClient> conns, Byte[] payload, Int32 size, Int32 seconds, Boolean tolerateLoss, String recvMode)
    {
        var runners = conns.Select(c => new PipelineClient(c, size, tolerateLoss, recvMode)).ToArray();
        var sendTasks = new List<Task>();
        var recvTasks = new List<Task>();
        foreach (var r in runners)
        {
            sendTasks.Add(r.StartSend(payload));
            recvTasks.Add(r.StartReceive());
        }

        Thread.Sleep(seconds * 1000);

        foreach (var r in runners) r.Stop();

        // 先等发送任务收尾：SentPackets 在发送任务 finally 才发布，未收尾就排水会以偏小的发送量提前判定“已排干”
        if (!Task.WaitAll(sendTasks.ToArray(), TimeSpan.FromSeconds(30)))
            Console.WriteLine("警告：部分发送任务未及时结束");

        // 排水：轮询等接收读满发送总量（UDP 丢包或长时间无进展则放弃）
        var drained = false;
        var deadline = Stopwatch.GetTimestamp() + (Int64)(60 * Stopwatch.Frequency);
        var lastTotal = -1L;
        var stall = 0;
        if (tolerateLoss)
        {
            // UDP 无流控：灌包必然丢包，排水无意义；短结算后直接统计（发送与接收差即为丢包）
            Thread.Sleep(500);
        }
        else
        {
            while (true)
            {
                var sent = runners.Sum(r => r.SentPackets);
                var recv = runners.Sum(r => r.TotalReceived);
                if (sent > 0 && recv >= sent * size)
                {
                    drained = true;
                    break;
                }
                if (Stopwatch.GetTimestamp() > deadline)
                {
                    Console.WriteLine("警告：排水超时，部分回显未读满（可能存在丢包）");
                    break;
                }
                if (recv == lastTotal)
                {
                    if (++stall >= 50) break;
                }
                else
                {
                    stall = 0;
                    lastTotal = recv;
                }
                Thread.Sleep(100);
            }
        }

        // 未排干时主动关闭，避免接收侧悬挂
        if (!drained)
            foreach (var c in conns) c.Close("drain-timeout");

        // 等接收任务收尾（拉取模式循环退出；事件模式无接收任务）
        if (!Task.WaitAll(recvTasks.ToArray(), TimeSpan.FromSeconds(15)))
            Console.WriteLine("警告：部分接收任务未及时结束");

        var sentBytes = runners.Sum(r => r.SentPackets) * size;
        var recvBytes = runners.Sum(r => r.TotalReceived);
        Console.WriteLine($"完整性    : 发送 {sentBytes / size:N0} 包 / 接收 {recvBytes / size:N0} 包（差 {sentBytes / size - recvBytes / size:N0}）");
        return (sentBytes, recvBytes);
    }

    /// <summary>往返模式：逐包发送并读满回显（同步/异步拉取或事件乒乓），采样单次往返耗时（微秒）</summary>
    private static List<Double>? RunRoundTrip(List<ISocketClient> conns, Byte[] payload, Int32 size, Int32 seconds, Boolean tolerateLoss, String recvMode)
    {
        var samples = new List<Double>();
        if (recvMode == "event")
        {
            // 事件乒乓：发送→Received 回执→再发送（零阻塞线程；与服务器接收模型同构）
            var runners = conns.Select(c => new EventRoundTripClient(c, size, payload, tolerateLoss)).ToArray();
            foreach (var r in runners) r.Start();

            Thread.Sleep(seconds * 1000);

            foreach (var r in runners) r.Stop();
            Thread.Sleep(500);

            foreach (var r in runners)
            {
                samples.AddRange(r.Snapshot());
                if (r.Missed > 0) Console.WriteLine($"警告：{r.Missed} 次回执超时（丢包容忍已重发）");
            }
        }
        else
        {
            var runners = conns.Select(c => new RoundTripClient(c, size, tolerateLoss, recvMode)).ToArray();
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

            foreach (var r in runners) samples.AddRange(r.Samples);
        }

        if (samples.Count == 0)
        {
            Console.WriteLine("警告：未采集到往返样本");
            return null;
        }

        samples.Sort();
        var avg = samples.Average();
        Console.WriteLine($"往返样本  : {samples.Count:N0} 次（窗口 {seconds} s）");
        Console.WriteLine($"延迟(往返): P50={Percentile(samples, 0.50):F1}µs  P95={Percentile(samples, 0.95):F1}µs  P99={Percentile(samples, 0.99):F1}µs");
        Console.WriteLine($"           平均={avg:F1}µs  最小={samples[0]:F1}µs  最大={samples[^1]:F1}µs");
        return samples;
    }

    /// <summary>取分位数（升序样本）</summary>
    private static Double Percentile(List<Double> sorted, Double p)
    {
        var idx = (Int32)Math.Min(sorted.Count - 1, Math.Max(0, Math.Round((sorted.Count - 1) * p)));

        return sorted[idx];
    }

    /// <summary>取中位数</summary>
    private static Double Median(List<Double> values)
    {
        var sorted = new List<Double>(values);
        sorted.Sort();

        return Percentile(sorted, 0.50);
    }

    /// <summary>拉取一次数据长度（同步 Receive 或异步 ReceiveAsync；UDP 带超时容忍丢包）</summary>
    private static async Task<Int32> PullOnce(ISocketClient conn, Boolean useAsync, Boolean tolerateLoss)
    {
        if (useAsync)
        {
            if (tolerateLoss)
            {
                using var cts = new CancellationTokenSource(300);
                using var pk = await conn.ReceiveAsync(cts.Token);
                return pk?.Length ?? 0;
            }
            else
            {
                using var pk = await conn.ReceiveAsync();
                return pk?.Length ?? 0;
            }
        }
        else
        {
            using var pk = conn.Receive();
            return pk?.Length ?? 0;
        }
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

    /// <summary>流水线客户端：一个发送任务 + 接收（同步/异步拉取循环或事件回调），接收读满发送总量（UDP 容忍丢包）</summary>
    private sealed class PipelineClient
    {
        private readonly ISocketClient _conn;
        private readonly Int32 _size;
        private readonly Boolean _tolerateLoss;
        private readonly Boolean _eventMode;
        private readonly Boolean _useAsync;
        private volatile Boolean _stopped;
        private Int64 _eventBytes;

        public Int64 SentPackets;
        public Int64 ReceivedBytes;

        public PipelineClient(ISocketClient conn, Int32 size, Boolean tolerateLoss, String recvMode)
        {
            _conn = conn;
            _size = size;
            _tolerateLoss = tolerateLoss;
            _eventMode = recvMode == "event";
            _useAsync = recvMode == "asyncpull";
            // 事件模式：订阅接收环推送，字节在回调中累计（与服务器同一接收模型）
            if (_eventMode) _conn.Received += OnReceived;
        }

        /// <summary>事件模式接收回调（接收环线程）</summary>
        private void OnReceived(Object? sender, ReceivedEventArgs e)
        {
            var pk = e.Packet;
            if (pk != null && pk.Length > 0) Interlocked.Add(ref _eventBytes, pk.Length);
        }

        public Task StartSend(Byte[] payload)
            => Task.Run(() =>
            {
                var n = 0L;
                var batch = 0L;
                try
                {
                    var errors = 0;
                    while (!_stopped)
                    {
                        // 发送失败（如 UDP 包超限返回 -1）不计入发送量，连续失败则提前退出防死转
                        if (_conn.Send(payload) <= 0)
                        {
                            if (++errors > 1000) break;
                            continue;
                        }
                        errors = 0;
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
        {
            // 事件模式无接收任务：数据由接收环回调推送
            if (_eventMode) return Task.CompletedTask;

            return Task.Run(async () =>
            {
                var bytes = 0L;
                // 发送未结束前持续读取；发送结束后读满发送总量（排水）
                while (SentPackets == 0 || bytes < SentPackets * _size)
                {
                    try
                    {
                        var n = await PullOnce(_conn, _useAsync, _tolerateLoss);
                        if (n <= 0) break;

                        bytes += n;
                    }
                    catch (SocketException ex) when (_tolerateLoss && ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        // UDP 无流控可能丢包：发送已停止且接收超时，视为排水结束
                        if (SentPackets > 0) break;
                    }
                    catch (OperationCanceledException) when (_tolerateLoss)
                    {
                        if (SentPackets > 0) break;
                    }
                }
                ReceivedBytes = bytes;
            });
        }

        /// <summary>当前已接收字节（事件模式读回调计数）</summary>
        public Int64 TotalReceived => _eventMode ? Interlocked.Read(ref _eventBytes) : ReceivedBytes;

        public void Stop() => _stopped = true;
    }

    /// <summary>往返客户端（拉取模式）：串行逐包往返，采样单次耗时（微秒）；UDP 丢包时跳过该次继续</summary>
    private sealed class RoundTripClient
    {
        private readonly ISocketClient _conn;
        private readonly Int32 _size;
        private readonly Boolean _tolerateLoss;
        private readonly Boolean _useAsync;
        private volatile Boolean _stopped;

        public List<Double> Samples { get; } = [];

        public RoundTripClient(ISocketClient conn, Int32 size, Boolean tolerateLoss, String recvMode)
        {
            _conn = conn;
            _size = size;
            _tolerateLoss = tolerateLoss;
            _useAsync = recvMode == "asyncpull";
        }

        public Task Start(Byte[] payload)
            => Task.Run(async () =>
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
                            var n = await PullOnce(_conn, _useAsync, _tolerateLoss);
                            if (n <= 0) throw new TimeoutException("回显中断");

                            need -= n;
                        }

                        var us = (Stopwatch.GetTimestamp() - t0) * 1_000_000.0 / Stopwatch.Frequency;
                        Samples.Add(us);
                    }
                    catch (SocketException ex) when (_tolerateLoss && ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        // UDP 丢包：跳过该次往返，继续下一轮（回显错位由协议本质决定，样本仍近似往返时延）
                    }
                    catch (OperationCanceledException) when (_tolerateLoss)
                    {
                        // 异步拉取超时等价于接收超时
                    }
                }
            });

        public void Stop() => _stopped = true;
    }

    /// <summary>事件模式往返客户端：发送→Received 回执→再发送（乒乓链），零阻塞线程；UDP 用看门狗重发容忍丢包</summary>
    private sealed class EventRoundTripClient
    {
        private readonly ISocketClient _conn;
        private readonly Int32 _size;
        private readonly Byte[] _payload;
        private volatile Boolean _stopped;
        private Int64 _sentAt;
        private Int32 _need;
        private readonly Object _lock = new();

        public List<Double> Samples { get; } = [];
        public Int64 Missed;

        public EventRoundTripClient(ISocketClient conn, Int32 size, Byte[] payload, Boolean tolerateLoss)
        {
            _conn = conn;
            _size = size;
            _payload = payload;
            _conn.Received += OnReceived;

            // UDP 丢包容忍：静默 1 秒视为回执丢失，记录并重发（保持乒乓链推进）
            if (tolerateLoss) _ = Task.Run(Watchdog);
        }

        public void Start() => SendNext();

        private void SendNext()
        {
            _need = _size;
            Volatile.Write(ref _sentAt, Stopwatch.GetTimestamp());
            _conn.Send(_payload);
        }

        /// <summary>回执回调（接收环线程）：累计完整回显后记录样本并立即链入下一次发送</summary>
        private void OnReceived(Object? sender, ReceivedEventArgs e)
        {
            var pk = e.Packet;
            if (pk == null || pk.Length <= 0) return;

            if (_need - pk.Length > 0)
            {
                _need -= pk.Length;
                return;
            }

            var us = (Stopwatch.GetTimestamp() - Volatile.Read(ref _sentAt)) * 1_000_000.0 / Stopwatch.Frequency;
            lock (_lock) Samples.Add(us);
            if (_stopped) return;

            SendNext();
        }

        private void Watchdog()
        {
            while (!_stopped)
            {
                Thread.Sleep(100);
                var sentAt = Volatile.Read(ref _sentAt);
                if (sentAt == 0 || Stopwatch.GetTimestamp() - sentAt < Stopwatch.Frequency) continue;

                Missed++;
                SendNext();
            }
        }

        public void Stop() => _stopped = true;

        public List<Double> Snapshot()
        {
            lock (_lock) return [.. Samples];
        }
    }
}
