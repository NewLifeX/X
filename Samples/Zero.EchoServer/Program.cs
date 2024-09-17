using NewLife.Log;
using NewLife.Model;
using Stardust;
using Zero.EchoServer;

// 启用控制台日志，拦截所有异常
XTrace.UseConsole();

var services = ObjectContainer.Current;

// 配置星尘。自动读取配置文件 config/star.config 中的服务器地址、应用标识、密钥
var star = services.AddStardust();

var provider = services.BuildServiceProvider();

// 实例化网络服务端，指定端口，同时在Tcp/Udp/IPv4/IPv6上监听
var server = new EchoNetServer
{
    Port = 7777,
    ServiceProvider = provider,
    Name = "回声服务端",

    Log = XTrace.Log,
    Tracer = star?.Tracer,

#if DEBUG
    SessionLog = XTrace.Log,
#endif
};

// 启动网络服务，监听端口，所有逻辑将在 EchoSession 中处理
server.Start();
XTrace.WriteLine("服务端启动完成！");

// 注册到星尘，非必须
star?.Service?.Register("EchoServer", () => $"tcp://*:{server.Port},udp://*:{server.Port}");

// 阻塞，等待友好退出
var host = services.BuildHost();
await host.RunAsync();
