using NewLife;
using NewLife.Caching;
using NewLife.Caching.Services;
using NewLife.Http;
using NewLife.Log;
using NewLife.Model;
using NewLife.Remoting;
using NewLife.Threading;
using Stardust;
using Zero.HttpServer;

// 启用控制台日志，拦截所有异常
XTrace.UseConsole();
#if DEBUG
TimerScheduler.Default.Log = XTrace.Log;
#endif

var services = ObjectContainer.Current;

// 配置星尘。自动读取配置文件 config/star.config 中的服务器地址
var star = services.AddStardust();

// 默认内存缓存，如有配置RedisCache可使用Redis缓存
services.AddSingleton<ICacheProvider, RedisCacheProvider>();

// 引入Redis，用于消息队列和缓存，单例，带性能跟踪。一般使用上面的ICacheProvider替代
//services.AddRedis("127.0.0.1:6379", "123456", 3, 5000);

// 创建Http服务器
var server = new HttpServer
{
    Name = "新生命Http服务器",
    Port = 8080,

    Log = XTrace.Log,
#if DEBUG
    SessionLog = XTrace.Log,
#endif
    Tracer = star.Tracer,
};

// 简单路径，返回字符串
server.Map("/", () => "<h1>Hello NewLife!</h1></br> " + DateTime.Now.ToFullString() + "</br><img src=\"logos/leaf.png\" />");
server.Map("/user", (String act, Int32 uid) => new { code = 0, data = $"User.{act}({uid}) success!" });

// 静态文件，支持目录映射
server.MapStaticFiles("/logos", "images/");
server.MapStaticFiles("/files", "./");

// 自定义控制器，映射其中方法，高仿ASP.NET
server.MapController<ApiController>("/api");

// 自定义处理器，操作Http上下文，实现文件上传等复杂逻辑
server.Map("/my", new MyHttpHandler());

// WebSocket处理器
server.Map("/ws", new WebSocketHandler());

server.Start();
XTrace.WriteLine("服务端启动完成！");

// 注册到星尘，非必须
star.Service?.RegisterAsync("Zero.HttpServer", $"http://*:{server.Port}");

// 客户端测试，非服务端代码，正式使用时请注释掉
_ = Task.Run(ClientTest.HttpClientTest);
_ = Task.Run(ClientTest.WebSocketTest);
_ = Task.Run(ClientTest.WebSocketClientTest);

// 异步阻塞，友好退出
var host = services.BuildHost();
(host as Host).MaxTime = 10_000;
await host.RunAsync();
server.Stop("stop");
