using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using NewLife;
using NewLife.Buffers;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Compression;
using NewLife.Configuration;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Web;

namespace Mobile;

/// <summary>移动端冒烟用例。全部用例只依赖本机回环，不访问外部网络。</summary>
public static class SmokeCases
{
    /// <summary>枚举全部用例。</summary>
    /// <returns>名称与执行委托。</returns>
    public static IEnumerable<(String Name, Action Action)> All()
    {
        yield return ("Runtime 平台信息", RuntimeInfo);
        yield return ("Utility/StringHelper", UtilityAndString);
        yield return ("MemoryCache 存取", MemoryCacheRoundTrip);
        yield return ("TimerX 定时回调", TimerXCallback);
        yield return ("JsonHelper 往返", JsonRoundTrip);
        yield return ("JsonConfigProvider 往返", ConfigRoundTrip);
        yield return ("TextFileLog 文件日志", FileLogRoundTrip);
        yield return ("PathHelper/Setting 可写路径", PathAndSetting);
        yield return ("MachineInfo 平台信息", MachineInfoRead);
        yield return ("Security 哈希与 AES", SecurityRoundTrip);
        yield return ("SpanSerializer 往返", SpanSerializerRoundTrip);
        yield return ("Binary 写入", BinaryWrite);
        yield return ("TarFile 归档往返", TarRoundTrip);
        yield return ("System.Net.Sockets TCP 回环", RawTcpLoopback);
        yield return ("NetServer/NetClient TCP 回环", TcpLoopback);
        yield return ("HttpServer/ApiHttpClient 回环", HttpLoopback);
        yield return ("WebSocket 回环", WebSocketLoopback);
        yield return ("ObjectContainer 服务解析", ObjectContainerResolve);
        yield return ("PluginHelper 动态能力守卫", PluginGuard);
        yield return ("Type.GetTypeEx 反射", TypeReflection);
        yield return ("ProcessHelper 进程信息", ProcessInfo);
    }

    private static void RuntimeInfo()
    {
        if (String.IsNullOrEmpty(RuntimeInformation.OSDescription)) throw new Exception("OSDescription 为空");
        if (String.IsNullOrEmpty(RuntimeInformation.FrameworkDescription)) throw new Exception("FrameworkDescription 为空");
        if (Runtime.TickCount64 <= 0) throw new Exception("TickCount64 非正");

        SmokeEnvironment.WriteDiagnostic($"[diag] os={RuntimeInformation.OSDescription}");
        SmokeEnvironment.WriteDiagnostic($"[diag] framework={RuntimeInformation.FrameworkDescription}");
        SmokeEnvironment.WriteDiagnostic($"[diag] dynamicCode={RuntimeFeature.IsDynamicCodeSupported}");
        SmokeEnvironment.WriteDiagnostic($"[diag] android={OperatingSystem.IsAndroid()} ios={OperatingSystem.IsIOS()} windows={OperatingSystem.IsWindows()} linux={OperatingSystem.IsLinux()}");
    }

    private static void UtilityAndString()
    {
        if ("123".ToInt() != 123) throw new Exception("ToInt 不一致");
        if (Math.Abs("3.14".ToDouble() - 3.14) > 0.0001) throw new Exception("ToDouble 不一致");
        if (!"true".ToBoolean()) throw new Exception("ToBoolean 不一致");
        if ("2026-10-08".ToDateTime().Year != 2026) throw new Exception("ToDateTime 不一致");

        var sb = Pool.StringBuilder.Get();
        try
        {
            sb.Append("mobile").Append('-').Append("smoke");
            if (sb.ToString() != "mobile-smoke") throw new Exception("Pool.StringBuilder 不一致");
        }
        finally
        {
            Pool.StringBuilder.Return(sb);
        }
    }

    private static void MemoryCacheRoundTrip()
    {
        var cache = MemoryCache.Instance;
        var key = "mobile-smoke-" + Guid.NewGuid().ToString("N");
        try
        {
            if (!cache.Set(key, "value-1", 60)) throw new Exception("MemoryCache.Set 返回 false");
            if (cache.Get<String>(key) != "value-1") throw new Exception("MemoryCache.Get 不一致");
        }
        finally
        {
            cache.Remove(key);
        }
    }

    private static void TimerXCallback()
    {
        using var signal = new ManualResetEventSlim(false);
        using var timer = new TimerX(_ =>
        {
            signal.Set();
            return Task.CompletedTask;
        }, null, 10, 0);

        if (!signal.Wait(5000)) throw new TimeoutException("TimerX 未在 5 秒内回调");
    }

    private static void JsonRoundTrip()
    {
        var model = new SmokeConfig { Name = "mobile", Level = 3 };
        var json = JsonHelper.ToJson(model);
        var back = json.ToJsonEntity<SmokeConfig>();
        if (back == null || back.Name != model.Name || back.Level != model.Level) throw new Exception($"JsonHelper 往返不一致: {json}");
    }

    private static void ConfigRoundTrip()
    {
        var dir = SmokeEnvironment.NewTempDirectory("cfg");
        var file = Path.Combine(dir, "cfg.json");
        var provider = new JsonConfigProvider { FileName = file };
        if (!provider.Save(new SmokeConfig { Name = "cfg", Level = 5 })) throw new Exception("JsonConfigProvider.Save 返回 false");

        var back = provider.Load<SmokeConfig>();
        if (back == null || back.Name != "cfg" || back.Level != 5) throw new Exception("JsonConfigProvider.Load 不一致");
    }

    private static void FileLogRoundTrip()
    {
        var dir = SmokeEnvironment.NewTempDirectory("log");
        var log = new TextFileLog { LogPath = dir };
        var saved = XTrace.Log;
        try
        {
            XTrace.Log = log;
            XTrace.WriteLine("mobile-smoke-log");
        }
        finally
        {
            XTrace.Log = saved;
            log.Dispose();
        }

        for (var i = 0; i < 30 && !HasFiles(dir); i++) Thread.Sleep(100);
        if (!HasFiles(dir)) throw new Exception("3 秒内未生成日志文件");
    }

    private static void PathAndSetting()
    {
        if (String.IsNullOrEmpty(PathHelper.BasePath)) throw new Exception("PathHelper.BasePath 为空");

        var file = Path.Combine(SmokeEnvironment.WritableRoot, "path-" + Guid.NewGuid().ToString("N")[..8] + ".txt");
        File.WriteAllText(file, "hello-mobile");
        if (File.ReadAllText(file) != "hello-mobile") throw new Exception("可写路径文件往返失败");

        SmokeEnvironment.WriteDiagnostic($"[diag] BasePath={PathHelper.BasePath}");
        SmokeEnvironment.WriteDiagnostic($"[diag] Setting.LogPath={Setting.Current.LogPath}");
        SmokeEnvironment.WriteDiagnostic($"[diag] WritableRoot={SmokeEnvironment.WritableRoot}");
    }

    private static void MachineInfoRead()
    {
        var mi = new MachineInfo();
        mi.Init();

        if (String.IsNullOrEmpty(mi.OSName)) throw new Exception("MachineInfo.OSName 为空");
        if (String.IsNullOrEmpty(mi.OSVersion)) throw new Exception("MachineInfo.OSVersion 为空");
        if (OperatingSystem.IsIOS() && mi.OSName != "iOS") throw new Exception($"MachineInfo.OSName 异常：{mi.OSName}");
        if (OperatingSystem.IsIOS() && mi.OSVersion.StartsWithIgnoreCase("Unix")) throw new Exception($"MachineInfo.OSVersion 未取到系统产品版本：{mi.OSVersion}");
        if (mi.Memory == 0)
        {
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()) throw new Exception("MachineInfo.Memory 未采集");
            SmokeEnvironment.WriteDiagnostic("[diag] MachineInfo.Memory 未采集（当前平台未提供）");
        }

        SmokeEnvironment.WriteDiagnostic($"[diag] OSName={mi.OSName}; OSVersion={mi.OSVersion}; Memory={mi.Memory}; Available={mi.AvailableMemory}");
        mi.Refresh();
        if (mi.AvailableMemory > mi.Memory) throw new Exception($"AvailableMemory 异常 {mi.AvailableMemory}/{mi.Memory}");
    }

    private static void SecurityRoundTrip()
    {
        var text = "mobile-smoke";
        var md5 = text.MD5();
        if (md5.Length != 32) throw new Exception("MD5 长度异常");

        var data = Encoding.UTF8.GetBytes(text);
        var sha = data.SHA256();
        if (sha.Length != 32) throw new Exception("SHA256 长度异常");

        var key = Encoding.UTF8.GetBytes("1234567890123456");
        using var aes = Aes.Create();
        var encrypted = aes.Encrypt(data, key);
        var decrypted = aes.Decrypt(encrypted, key);
        if (Encoding.UTF8.GetString(decrypted) != text) throw new Exception("AES 往返不一致");
    }

    private static void SpanSerializerRoundTrip()
    {
        var model = new SmokeConfig { Name = "span", Level = 7 };
        var buffer = new Byte[256];
        var writer = new SpanWriter(buffer);
        SpanSerializer.WriteObject(ref writer, model, typeof(SmokeConfig));

        var reader = new SpanReader(buffer.AsSpan(0, writer.WrittenCount));
        var back = (SmokeConfig)SpanSerializer.ReadObject(ref reader, typeof(SmokeConfig))!;
        if (back.Name != model.Name || back.Level != model.Level) throw new Exception("SpanSerializer 往返不一致");
    }

    private static void BinaryWrite()
    {
        var bn = new Binary { IsLittleEndian = true };
        bn.Write(42);
        bn.Write("hello");
        var data = bn.GetPacket().ToArray();
        if (data.Length == 0) throw new Exception("Binary 写入为空");
    }

    private static void TarRoundTrip()
    {
        var root = SmokeEnvironment.NewTempDirectory("tar");
        var src = Path.Combine(root, "src");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "a.txt"), "hello-tar");

        var archive = Path.Combine(root, "smoke.tar");
        TarFile.CreateFromDirectory(src, archive);

        var dst = Path.Combine(root, "dst");
        Directory.CreateDirectory(dst);
        TarFile.ExtractToDirectory(archive, dst, true);

        var file = Path.Combine(dst, "a.txt");
        if (!File.Exists(file) || File.ReadAllText(file) != "hello-tar") throw new Exception("TarFile 往返不一致");
    }

    private static void RawTcpLoopback()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        SmokeEnvironment.WriteDiagnostic($"[diag] raw tcp listener port={port}");

        var serverGot = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Task.Run(() =>
        {
            try
            {
                using var client = listener.AcceptTcpClient();
                using var stream = client.GetStream();
                var buffer = new Byte[4];
                var count = stream.Read(buffer, 0, buffer.Length);
                serverGot.TrySetResult(buffer[..count]);
                stream.Write("pong"u8.ToArray());
            }
            catch (Exception ex)
            {
                serverGot.TrySetException(ex);
            }
        });

        using var tcp = new TcpClient();
        tcp.Connect(IPAddress.Loopback, port);
        using var ns = tcp.GetStream();
        ns.Write("ping"u8.ToArray());

        if (!serverGot.Task.Wait(5000)) throw new TimeoutException($"raw TCP 服务端未收到请求（port={port}）");
        if (serverGot.Task.IsFaulted) throw new Exception($"raw TCP 服务端异常（port={port}）", serverGot.Task.Exception);

        var reply = new Byte[4];
        ns.ReadExactly(reply);
        if (Encoding.UTF8.GetString(reply) != "pong") throw new Exception($"raw TCP 应答不一致（port={port}）");
    }

    private static void TcpLoopback()
    {
        using var server = new NetServer(IPAddress.Loopback, 0, NetType.Tcp) { Protocol = new SrmpCodec() };
        server.Log = XTrace.Log;
        server.SessionLog = XTrace.Log;
        server.Start();
        var state = $"active={server.Active} port={server.Port} servers={server.Servers.Count} sessions={server.SessionCount} local={server.Server?.Local}";
        SmokeEnvironment.WriteDiagnostic($"[diag] tcp server {state}");

        var serverGot = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage msg) return;

            var all = msg.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var body = all.AsReadOnlySequence().ToArray();
            all.TryDispose();
            serverGot.TrySetResult(body);

            if (msg.CreateReply() is not DefaultMessage reply) return;
            reply.SetBody(new ArrayPacket("pong"u8.ToArray()));
            ((INetSession)s!).SendMessage(reply);
        };

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
        client.Log = XTrace.Log;

        var clientGot = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Received += (s, e) =>
        {
            if (e.Message is not DefaultMessage msg || msg.Kind < MessageKinds.Response) return;

            var all = msg.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            var body = all.AsReadOnlySequence().ToArray();
            all.TryDispose();
            clientGot.TrySetResult(body);
        };

        if (!client.Open()) throw new Exception($"NetClient 打开失败（{state}）");

        var request = new DefaultMessage { Sequence = 0x35 };
        request.SetBody(new ArrayPacket("ping"u8.ToArray()));
        client.SendMessage(request);

        if (!serverGot.Task.Wait(5000)) throw new TimeoutException($"服务端未收到请求（{state}）");
        if (!clientGot.Task.Wait(5000)) throw new TimeoutException($"客户端未收到应答（{state}）");
        if (Encoding.UTF8.GetString(clientGot.Task.Result) != "pong") throw new Exception($"TCP 应答不一致（{state}）");
    }

    private static void HttpLoopback()
    {
        using var server = new HttpServer { Port = 0, AddressFamily = AddressFamily.InterNetwork };
        server.Local.Address = IPAddress.Loopback;
        server.Log = XTrace.Log;
        server.SessionLog = XTrace.Log;
        server.Map("/txt", () => "pong-mobile");
        server.Map("/obj", () => new SmokePing { Msg = "pong-mobile" });
        server.Start();
        var state = $"active={server.Active} port={server.Port} servers={server.Servers.Count}";
        SmokeEnvironment.WriteDiagnostic($"[diag] http server {state}");

        try
        {
            using var hc = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var text = hc.GetStringAsync($"http://127.0.0.1:{server.Port}/txt").GetAwaiter().GetResult();
            if (text != "pong-mobile") throw new Exception("HTTP 文本响应不一致: " + text);

            var api = new ApiHttpClient($"http://127.0.0.1:{server.Port}");
            var obj = api.GetAsync<SmokePing>("/obj").GetAwaiter().GetResult();
            if (obj?.Msg != "pong-mobile") throw new Exception("ApiHttpClient 对象响应不一致: " + (obj?.Msg ?? "null"));
        }
        catch (Exception ex)
        {
            throw new Exception($"HTTP 回环失败（{state}）", ex);
        }
    }

    private static void WebSocketLoopback()
    {
        using var server = new HttpServer { Port = 0, AddressFamily = AddressFamily.InterNetwork };
        server.Local.Address = IPAddress.Loopback;
        server.Log = XTrace.Log;
        server.SessionLog = XTrace.Log;
        server.Map("/ws", new SmokeWsHandler());
        server.Start();
        var state = $"active={server.Active} port={server.Port} servers={server.Servers.Count}";
        SmokeEnvironment.WriteDiagnostic($"[diag] ws server {state}");

        var ws = new WebSocketClient($"ws://127.0.0.1:{server.Port}/ws") { Log = XTrace.Log };
        if (!ws.OpenAsync().GetAwaiter().GetResult()) throw new Exception($"WebSocket 建连失败（{state}）");

        ws.SendTextAsync("hi-mobile").GetAwaiter().GetResult();
        var msg = ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        var text = msg?.Payload?.ToStr() ?? "";
        if (!text.Contains("echo:hi-mobile")) throw new Exception("WebSocket 回显不一致: " + text);

        ws.CloseAsync(1000, "done").GetAwaiter().GetResult();
    }

    private static void ObjectContainerResolve()
    {
        var ioc = ObjectContainer.Current;
        ioc.AddTransient<ISmokeSvc, SmokeSvc>();

        var svc = ioc.GetService<ISmokeSvc>();
        if (svc == null || svc.GetType() != typeof(SmokeSvc)) throw new Exception("ObjectContainer 解析失败");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "冒烟刻意调用带 RequiresUnreferencedCode 的插件守卫 API，验证其降级行为")]
    private static void PluginGuard()
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            var type = PluginHelper.LoadPlugin("NewLife.XXX.NotExist", null, null, null);
            if (type != null) throw new Exception("意外解析到类型: " + type.FullName);
            return;
        }

        try
        {
            PluginHelper.LoadPlugin("NewLife.XXX.NotExist", null, null, null);
            throw new Exception("AOT 下插件动态加载竟然可用");
        }
        catch (PlatformNotSupportedException ex)
        {
            if (!ex.Message.Contains("插件动态加载")) throw new Exception("异常信息不明确: " + ex.Message);
        }
    }

    private static void TypeReflection()
    {
        var type = "NewLife.Log.XTrace".GetTypeEx();
        if (type == null) throw new Exception("Type.GetTypeEx 返回 null（类型可能被裁剪）");
    }

    private static void ProcessInfo()
    {
        try
        {
            var p = Process.GetCurrentProcess();
            if (p.Id <= 0) throw new Exception("进程 Id 异常");

            var name = p.GetProcessName();
            if (String.IsNullOrEmpty(name)) throw new Exception("进程名为空");

            SmokeEnvironment.WriteDiagnostic($"[diag] process={name}; id={p.Id}; ws={p.WorkingSet64 / 1024 / 1024}MB");
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or NotSupportedException or InvalidOperationException)
        {
            throw new SkipException(ex.Message);
        }
    }

    private static Boolean HasFiles(String dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Any();
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>配置/序列化冒烟模型。</summary>
public class SmokeConfig
{
    /// <summary>名称。</summary>
    public String Name { get; set; } = "";

    /// <summary>级别。</summary>
    public Int32 Level { get; set; }
}

/// <summary>HTTP 对象响应模型。</summary>
public class SmokePing
{
    /// <summary>消息。</summary>
    public String Msg { get; set; } = "";
}

/// <summary>对象容器冒烟服务。</summary>
public interface ISmokeSvc
{
    /// <summary>返回固定响应。</summary>
    /// <returns>pong。</returns>
    String Ping();
}

/// <summary>对象容器冒烟服务实现。</summary>
public class SmokeSvc : ISmokeSvc
{
    /// <summary>返回固定响应。</summary>
    /// <returns>pong。</returns>
    public String Ping() => "pong";
}

/// <summary>WebSocket 回显处理器。</summary>
public class SmokeWsHandler : WebSocketHandler
{
    /// <summary>处理消息。</summary>
    /// <param name="socket">WebSocket 会话。</param>
    /// <param name="message">消息。</param>
    public override void ProcessMessage(NewLife.Http.WebSocket socket, WsMessage message)
    {
        if (message.Type == WebSocketMessageType.Text)
        {
            socket.Send($"echo:{message.Payload?.ToStr()}");
            return;
        }

        base.ProcessMessage(socket, message);
    }
}
