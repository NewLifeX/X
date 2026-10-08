// NewLife.Core NativeAOT compatibility smoke test.
// Runs a fixed set of scenarios against the core library and reports PASS/FAIL per scenario.
// Same binary is run under JIT (dotnet run) and NativeAOT (publish -p:PublishAot=true) to compare.
//
// 说明：AOT 能力验收冒烟件，由 .github/workflows/test.yml 的 aot job 自动执行（非 0 退出码即失败）。
// 背景与实测结论见 Doc/AOT兼容性分析.md。
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using NewLife;
using NewLife.Buffers;
using NewLife.Configuration;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Web;

try { Console.OutputEncoding = Encoding.UTF8; } catch { }

Console.WriteLine("=== NewLife.Core AOT smoke ===");
Console.WriteLine($"framework={RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"IsDynamicCodeSupported={RuntimeFeature.IsDynamicCodeSupported} IsDynamicCodeCompiled={RuntimeFeature.IsDynamicCodeCompiled}");

var results = new List<(String Name, Boolean Ok, String Error)>();
void Run(String name, Action action)
{
    try
    {
        action();
        results.Add((name, true, ""));
        Console.WriteLine($"[PASS] {name}");
    }
    catch (Exception ex)
    {
        results.Add((name, false, $"{ex.GetType().Name}: {ex.Message}"));
        Console.WriteLine($"[FAIL] {name} -> {ex.GetType().Name}: {ex.Message}");
    }
}

var temp = Path.Combine(Path.GetTempPath(), "aot-smoke-" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(temp);

var logdir = Path.Combine(temp, "logs");
Directory.CreateDirectory(logdir);
XTrace.Log = new TextFileLog { LogPath = logdir };
Console.WriteLine($"[diag] logdir={logdir}");

Run("基础类型转换 Utility", () =>
{
    if ("123".ToInt() != 123) throw new Exception("ToInt");
    if ("3.14".ToDouble() != 3.14) throw new Exception("ToDouble");
    if ("true".ToBoolean() != true) throw new Exception("ToBoolean");
    if ("2026-10-08".ToDateTime().Year != 2026) throw new Exception("ToDateTime");
});

Run("反射 Type.GetTypeEx", () =>
{
    var t = "NewLife.Log.XTrace".GetTypeEx();
    if (t == null) throw new Exception("Type.GetTypeEx 返回 null（类型可能被裁剪）");
});

Run("日志 XTrace 控制台", () =>
{
    var saved = XTrace.Log;
    try
    {
        XTrace.UseConsole();
        XTrace.WriteLine("aot-smoke-console");
    }
    finally
    {
        XTrace.Log = saved;
    }
});

Run("日志 TextFileLog 文件", () =>
{
    var flog = new TextFileLog { LogPath = temp };
    XTrace.Log = flog;
    XTrace.WriteLine("aot-smoke-file");
    for (var i = 0; i < 30 && !Directory.EnumerateFiles(temp).Any(); i++) Thread.Sleep(100);
    if (!Directory.EnumerateFiles(temp).Any()) throw new Exception("3 秒内未生成日志文件");
});

Run("ReceivedEventArgs.Rent 直接调用", () =>
{
    var e = ReceivedEventArgs.Rent();
    if (e == null) throw new Exception("Rent 返回 null");
    ReceivedEventArgs.Return(e);
});

Run("配置 JsonConfigProvider 往返", () =>
{
    var file = Path.Combine(temp, "cfg.json");
    var p = new JsonConfigProvider { FileName = file };
    if (!p.Save(new SmokeConfig { Name = "n1", Level = 5 })) throw new Exception("Save 返回 false");
    var back = p.Load<SmokeConfig>();
    if (back == null) throw new Exception("Load 返回 null");
    if (back.Name != "n1" || back.Level != 5) throw new Exception($"往返不一致 {back.Name}/{back.Level}");
});

Run("JSON JsonHelper 往返", () =>
{
    var m = new SmokeConfig { Name = "aot", Level = 3 };
    var json = JsonHelper.ToJson(m);
    var back = json.ToJsonEntity<SmokeConfig>();
    if (back?.Name != "aot" || back.Level != 3) throw new Exception("往返不一致");
});

Run("SpanSerializer 往返", () =>
{
    var m = new SmokeConfig { Name = "span", Level = 7 };
    var buf = new Byte[1024];
    var writer = new SpanWriter(buf);
    SpanSerializer.WriteObject(ref writer, m, typeof(SmokeConfig));
    var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
    var back = (SmokeConfig)SpanSerializer.ReadObject(ref reader, typeof(SmokeConfig));
    if (back.Name != "span" || back.Level != 7) throw new Exception("往返不一致");
});

Run("Binary 写入", () =>
{
    var bn = new Binary { IsLittleEndian = true };
    bn.Write(42);
    bn.Write("hello");
    var data = bn.GetPacket().ToArray();
    if (data.Length == 0) throw new Exception("写入为空");
});

Run("NetServer/NetClient TCP+SrmpCodec 回环", () =>
{
    using var server = new NetServer { Port = 0, ProtocolType = NetType.Tcp, Protocol = new SrmpCodec() };
    server.Log = XTrace.Log;
    server.SessionLog = XTrace.Log;
    server.Start();
    Console.WriteLine($"[diag] net server active={server.Active} port={server.Port}");

    var serverGot = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
    server.Received += (s, e) =>
    {
        if (e.Message is not DefaultMessage msg) return;
        var all = msg.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
        var body = all.AsReadOnlySequence().ToArray();
        all.TryDispose();
        serverGot.TrySetResult(body);

        var reply = (DefaultMessage)msg.CreateReply();
        reply.SetBody(new ArrayPacket("pong"u8.ToArray()));
        ((INetSession)s!).SendMessage(reply);
    };

    using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { Protocol = new SrmpCodec() };
    client.Log = XTrace.Log;
    var clientGot = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
    client.Received += (s, e) =>
    {
        if (e.Message is not DefaultMessage m || m.Kind < MessageKinds.Response) return;
        var all = m.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
        var body = all.AsReadOnlySequence().ToArray();
        all.TryDispose();
        clientGot.TrySetResult(body);
    };

    if (!client.Open()) throw new Exception("客户端打开失败");
    Console.WriteLine($"[diag] net client active={client.Active} sessions={server.SessionCount}");
    var req = new DefaultMessage { Sequence = 0x35 };
    req.SetBody(new ArrayPacket("ping"u8.ToArray()));
    client.SendMessage(req);

    if (!serverGot.Task.Wait(5000)) throw new Exception($"服务端未收到请求（超时）sessions={server.SessionCount}");
    if (!clientGot.Task.Wait(5000)) throw new Exception("客户端未收到应答（超时）");
    if (Encoding.UTF8.GetString(clientGot.Task.Result) != "pong") throw new Exception("应答负载不一致");
});

Run("HttpServer+ApiHttpClient GET", () =>
{
    using var server = new HttpServer { Port = 0 };
    server.Log = XTrace.Log;
    server.SessionLog = XTrace.Log;
    server.Map("/txt", () => "pong-aot");
    server.Map("/obj", () => new SmokePing { Msg = "pong-aot" });
    server.Start();
    Console.WriteLine($"[diag] http server active={server.Active} port={server.Port}");

    using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    var t = hc.GetStringAsync($"http://127.0.0.1:{server.Port}/txt").GetAwaiter().GetResult();
    if (t != "pong-aot") throw new Exception("文本响应不一致: " + t);

    var http = new NewLife.Remoting.ApiHttpClient($"http://127.0.0.1:{server.Port}");
    var r = http.GetAsync<SmokePing>("/obj").GetAwaiter().GetResult();
    if (r?.Msg != "pong-aot") throw new Exception($"ApiHttpClient 对象反序列化不一致: {r?.Msg ?? "null"}");
});

Run("WebSocket 文本回显", () =>
{
    using var server = new HttpServer { Port = 0 };
    server.Log = XTrace.Log;
    server.SessionLog = XTrace.Log;
    server.Map("/ws", new SmokeWsHandler());
    server.Start();

    var ws = new WebSocketClient($"ws://127.0.0.1:{server.Port}/ws") { Log = XTrace.Log };
    if (!ws.OpenAsync().GetAwaiter().GetResult()) throw new Exception("WebSocket 建连失败");
    ws.SendTextAsync("hi-aot").GetAwaiter().GetResult();
    var msg = ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    var text = msg?.Payload?.ToStr() ?? "";
    if (!text.Contains("echo:hi-aot")) throw new Exception("回显不一致: " + text);
    ws.CloseAsync(1000, "done").GetAwaiter().GetResult();
});

Run("ObjectContainer 服务解析", () =>
{
    var ioc = ObjectContainer.Current;
    ioc.AddTransient<ISmokeSvc, SmokeSvc>();
    var svc = ioc.GetService<ISmokeSvc>();
    if (svc == null || svc.GetType() != typeof(SmokeSvc)) throw new Exception("解析失败");
});

Run("动态能力守卫 PluginHelper", () =>
{
    // JIT 下：未提供 dll 时查不到类型返回 null；NativeAOT 下应抛明确的 PlatformNotSupportedException（而非晦涩崩溃）
    if (RuntimeFeature.IsDynamicCodeSupported)
    {
        var t = PluginHelper.LoadPlugin("NewLife.XXX.NotExist", null, null, null);
        if (t != null) throw new Exception("意外解析到类型: " + t.FullName);
    }
    else
    {
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
});

Run("源生成器服务工厂", () =>
{
    // 编译期由源生成器扫描容器注册调用点，为唯一公开无参构造的实现类型注册静态工厂
    if (ServiceFactoryTable.TryGet(typeof(SmokeSvc)) == null) throw new Exception("未生成 SmokeSvc 工厂");

    var ioc = ObjectContainer.Current;
    var svc = ioc.GetService<ISmokeSvc>();
    if (svc == null || svc.GetType() != typeof(SmokeSvc)) throw new Exception("解析失败");
});

Run("源生成器 Json 成员访问", () =>
{
    // 编译期由源生成器为 [JsonSerializable] 标记类型注册成员访问委托
    if (MemberAccessorTable.TryGetGetter(typeof(SmokeJsonModel), "Name") == null) throw new Exception("未生成 Json 成员访问器");

    var m = new SmokeJsonModel { Name = "sg", Count = 5, Note = "n" };
    var json = JsonHelper.ToJson(m);
    var back = json.ToJsonEntity<SmokeJsonModel>();
    if (back?.Name != "sg" || back.Count != 5 || back.Note != "n") throw new Exception("往返不一致: " + json);
});

Run("源生成器配置绑定", () =>
{
    // 编译期由源生成器扫描 Config<T> 继承链，为配置模型注册成员访问委托
    if (MemberAccessorTable.TryGetSetter(typeof(SmokeSetting), "Port") == null) throw new Exception("未生成配置成员访问器");

    var provider = new SmokeConfigProvider();
    provider["Host"] = "1.2.3.4";
    provider["Port"] = "8080";

    // 标准入口 Load<T> 的泛型参数带 DynamicallyAccessedMembers 注解，AOT 下按需保留模型公开属性元数据
    var model = provider.Load<SmokeSetting>() ?? throw new Exception("Load<SmokeSetting> 返回 null");
    if (model.Host != "1.2.3.4" || model.Port != 8080) throw new Exception($"Load 绑定不一致: {model.Host}:{model.Port}");

    // 类型元数据一经标准入口保留，直接调用 MapTo 扩展同样可用
    var model2 = new SmokeSetting();
    provider.Root.MapTo(model2, provider);
    if (model2.Host != "1.2.3.4" || model2.Port != 8080) throw new Exception($"MapTo 绑定不一致: {model2.Host}:{model2.Port}");
});

Console.WriteLine("=== summary ===");
var failed = 0;
foreach (var r in results)
{
    if (!r.Ok) failed++;
    Console.WriteLine($"{(r.Ok ? "PASS" : "FAIL")}\t{r.Name}{(r.Ok ? "" : "\t" + r.Error)}");
}
Console.WriteLine($"total={results.Count} pass={results.Count - failed} fail={failed}");
Environment.ExitCode = failed;

Thread.Sleep(1500);

public class SmokeConfig
{
    public String Name { get; set; } = "";
    public Int32 Level { get; set; }
}

public class SmokePing
{
    public String Msg { get; set; } = "";
}

public interface ISmokeSvc
{
    String Ping();
}

public class SmokeSvc : ISmokeSvc
{
    public String Ping() => "pong";
}

public class SmokeWsHandler : WebSocketHandler
{
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

[JsonSerializable(typeof(SmokeJsonModel))]
public partial class SmokeJsonContext : JsonSerializerContext { }

public class SmokeJsonModel
{
    public String Name { get; set; } = "";
    public Int32 Count { get; set; }
    public String? Note { get; set; }
}

public class SmokeSetting : Config<SmokeSetting>
{
    public String Host { get; set; } = "";
    public Int32 Port { get; set; }
}

public class SmokeConfigProvider : ConfigProvider
{
}
