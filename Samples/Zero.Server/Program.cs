using NewLife.Caching;
using NewLife.Caching.Services;
using NewLife.Log;
using NewLife.Model;
using Stardust;
using Zero.Server;
using Zero.TcpServer;
using Zero.TcpServer.Handlers;

// 启用控制台日志，拦截所有异常
XTrace.UseConsole();

//var services = new ServiceCollection();
var services = ObjectContainer.Current;

// 配置星尘。自动读取配置文件 config/star.config 中的服务器地址、应用标识、密钥
var star = services.AddStardust();

// 默认内存缓存，如有配置RedisCache可使用Redis缓存
services.AddSingleton<ICacheProvider, RedisCacheProvider>();

// 引入Redis，用于消息队列和缓存，单例，带性能跟踪。一般使用上面的ICacheProvider替代
//services.AddRedis("127.0.0.1:6379", "123456", 3, 5000);

// 注入消息处理器，可注入多个
services.AddTransient<IMsgHandler, MyHandler>();

var provider = services.BuildServiceProvider();

// 实例化网络服务端，指定端口，同时在Tcp/Udp/IPv4/IPv6上监听
var server = new MyNetServer
{
    Port = 12345,
    ServiceProvider = provider,
    Name = "大网服务端",

    Log = XTrace.Log,
    SessionLog = XTrace.Log,
    Tracer = star?.Tracer,

#if DEBUG
    SocketLog = XTrace.Log,
    LogSend = true,
    LogReceive = true,
#endif
};

// 启动网络服务，监听端口，所有逻辑将在 MyNetSession 中处理
server.Start();
XTrace.WriteLine("服务端启动完成！");

// 注册到星尘，非必须
star?.Service?.Register("MyNetServer", () => $"tcp://*:{server.Port},udp://*:{server.Port}");

// 客户端测试，非服务端代码，正式使用时请注释掉
_ = Task.Run(ClientTest.TcpClientTest);
_ = Task.Run(ClientTest.UdpClientTest);
_ = Task.Run(ClientTest.TcpSessionTest);
_ = Task.Run(ClientTest.UdpSessionTest);

// 阻塞，等待友好退出
var host = services.BuildHost();
await host.RunAsync();
server.Stop("stop");
