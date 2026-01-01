using System.Runtime.InteropServices;
using NewLife.Log;
#if !NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Model;

/// <summary>主机服务接口</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/host
/// 
/// 实现该接口以创建可由主机管理的后台服务。
/// 主机在启动时调用 <see cref="StartAsync"/>，在停止时调用 <see cref="StopAsync"/>。
/// </remarks>
public interface IHostedService
{
    /// <summary>开始服务</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>停止服务</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task StopAsync(CancellationToken cancellationToken);
}

/// <summary>主机服务扩展方法</summary>
public static class HostedServiceExtensions
{
    /// <summary>注册主机服务，在主机启动和停止时执行</summary>
    /// <typeparam name="THostedService">主机服务类型</typeparam>
    /// <param name="services">对象容器</param>
    /// <returns>对象容器</returns>
    public static IObjectContainer AddHostedService<THostedService>(this IObjectContainer services) where THostedService : class, IHostedService
    {
        services.AddSingleton<IHostedService, THostedService>();

        return services;
    }

    /// <summary>注册主机服务，在主机启动和停止时执行</summary>
    /// <typeparam name="THostedService">主机服务类型</typeparam>
    /// <param name="services">对象容器</param>
    /// <param name="implementationFactory">服务实例工厂</param>
    /// <returns>对象容器</returns>
    public static IObjectContainer AddHostedService<THostedService>(this IObjectContainer services, Func<IServiceProvider, THostedService> implementationFactory) where THostedService : class, IHostedService
    {
        services.AddSingleton<IHostedService>(implementationFactory);

        return services;
    }
}

/// <summary>轻量级应用主机接口</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/host
/// 
/// 提供应用程序生命周期管理，包括启动、停止和运行主循环。
/// 销毁主机时，会触发所有服务的停止事件。
/// </remarks>
public interface IHost
{
    /// <summary>服务提供者</summary>
    IServiceProvider Services { get; }

    /// <summary>添加服务实例</summary>
    /// <param name="service">服务实例</param>
    void Add(IHostedService service);

    /// <summary>添加服务类型</summary>
    /// <typeparam name="TService">服务类型</typeparam>
    void Add<TService>() where TService : class, IHostedService;

    /// <summary>异步启动所有服务</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>异步停止所有服务</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>同步运行，大循环阻塞</summary>
    void Run();

    /// <summary>异步运行，大循环阻塞</summary>
    /// <returns>异步任务</returns>
    Task RunAsync();

    /// <summary>关闭主机</summary>
    /// <param name="reason">关闭原因</param>
    void Close(String? reason);
}

/// <summary>轻量级应用主机</summary>
/// <remarks>
/// 通过指定服务提供者来实例化一个应用主机。
/// 
/// 文档 https://newlifex.com/core/host
/// 
/// 销毁主机时，会触发所有服务的停止事件。
/// </remarks>
/// <param name="serviceProvider">服务提供者</param>
public class Host(IServiceProvider serviceProvider) : DisposeBase, IHost
{
    #region 属性
    /// <summary>服务提供者</summary>
    public IServiceProvider Services { get; } = serviceProvider;

    /// <summary>服务集合。已注册的主机服务列表</summary>
    public IList<IHostedService> HostedServices { get; } = [];

    /// <summary>最大执行时间。单位毫秒，默认-1表示永久阻塞，等待外部ControlC/SIGINT信号</summary>
    public Int32 MaxTime { get; set; } = -1;

    private Int32 _closed;
    #endregion

    #region 构造
    static Host()
    {
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        Console.CancelKeyPress += OnExit;
#if NETCOREAPP
        System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += ctx => OnExit(ctx, EventArgs.Empty);
#endif
#if NET6_0_OR_GREATER
        PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => OnExit(ctx.Signal + "", EventArgs.Empty));
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, ctx => OnExit(ctx.Signal + "", EventArgs.Empty));
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => OnExit(ctx.Signal + "", EventArgs.Empty));
#endif
    }

    /// <summary>销毁资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        Close(disposing ? "Dispose" : "GC");
    }
    #endregion

    #region 服务集合
    /// <summary>添加服务类型</summary>
    /// <typeparam name="TService">服务类型</typeparam>
    public void Add<TService>() where TService : class, IHostedService
    {
        // 把服务类型注册到容器中，以便后续获取
        var ioc = (Services as ServiceProvider)?.Container ?? ObjectContainer.Current;
        ioc.AddHostedService<TService>();
    }

    /// <summary>添加服务实例</summary>
    /// <param name="service">服务实例</param>
    public void Add(IHostedService service) => HostedServices.Add(service);
    #endregion

    #region 开始停止
    /// <summary>异步启动所有服务</summary>
    /// <remarks>
    /// 按注册顺序依次启动服务，任意服务启动失败则回滚已启动的服务。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 从容器中获取所有服务。此时服务是倒序，需要反转
        var svcs = new List<IHostedService>();
        foreach (var item in Services.GetServices<IHostedService>())
        {
            svcs.Add(item);
        }
        svcs.Reverse();
        foreach (var item in svcs)
        {
            HostedServices.Add(item);
        }

        // 开始所有服务，任意服务出错都导致启动失败。增加回滚，按已启动服务反向停止
        var started = new List<IHostedService>();
        var errors = new List<Exception>();
        foreach (var item in HostedServices)
        {
            try
            {
                await item.StartAsync(cancellationToken).ConfigureAwait(false);
                started.Add(item);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                errors.Add(ex);
                break; // 停止继续启动，进入回滚
            }
        }

        if (errors.Count > 0)
        {
            // 回滚：反向停止已成功启动的服务
            for (var i = started.Count - 1; i >= 0; i--)
            {
                var svc = started[i];
                try
                {
                    await svc.StopAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    XTrace.WriteException(ex2);
                    errors.Add(ex2);
                }
            }

            throw new AggregateException("启动主机服务失败", errors);
        }
    }

    /// <summary>异步停止所有服务</summary>
    /// <remarks>
    /// 按注册的反向顺序停止服务，保证依赖后启动的先行释放。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // 反向顺序停止，保证依赖后启动的先行释放
        for (var i = HostedServices.Count - 1; i >= 0; i--)
        {
            var item = HostedServices[i];
            try
            {
                await item.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
    }
    #endregion

    #region 运行大循环
    private TaskCompletionSource<Object>? _life;
    private TaskCompletionSource<Object>? _life2;

    /// <summary>同步运行，大循环阻塞</summary>
    public void Run() => RunAsync().GetAwaiter().GetResult();

    /// <summary>异步运行，大循环阻塞</summary>
    /// <returns>异步任务</returns>
    public async Task RunAsync()
    {
        XTrace.WriteLine("Starting......");

        using var source = new CancellationTokenSource();

#if NET45
        _life = new TaskCompletionSource<Object>();
        _life2 = new TaskCompletionSource<Object>();
#else
        _life = new TaskCompletionSource<Object>(TaskCreationOptions.RunContinuationsAsynchronously);
        _life2 = new TaskCompletionSource<Object>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

        RegisterExit((s, e) => Close(s as String ?? s?.GetType().Name ?? (e as ConsoleCancelEventArgs)?.SpecialKey.ToString()));

        await StartAsync(source.Token).ConfigureAwait(false);
        XTrace.WriteLine("Application started. Press Ctrl+C to shut down.");

        // 等待生命周期结束：非阻塞等待方式
        if (MaxTime >= 0)
        {
            await Task.WhenAny(_life.Task, Task.Delay(MaxTime)).ConfigureAwait(false);
        }
        else
        {
            await _life.Task.ConfigureAwait(false);
        }

        XTrace.WriteLine("Application is shutting down...");

        await StopAsync(source.Token).ConfigureAwait(false);

        XTrace.WriteLine("Stopped!");

        // 通知外部，主循环已完成
        _life2.TrySetResult(0);
    }

    /// <summary>关闭主机</summary>
    /// <param name="reason">关闭原因</param>
    public void Close(String? reason)
    {
        // 防止重复关闭
        if (Interlocked.CompareExchange(ref _closed, 1, 0) != 0) return;

        XTrace.WriteLine("Application closed. {0}", reason);

        // 通知主循环，可以进入Stop流程
        _life?.TrySetResult(0);

        // 需要阻塞，等待StopAsync执行完成。调用者可能是外部SIGINT信号，需要阻塞它，给Stop留出执行时间
        _life2?.Task.Wait(15_000);

        // 再阻塞一会，让host.RunAsync后面的清理代码有机会执行
        if (reason == "SIGINT") Thread.Sleep(500);
    }
    #endregion

    #region 退出事件
    private static readonly Object _eventLock = new();
    private static readonly List<EventHandler> _events = [];
    private static readonly List<Action> _events2 = [];
    private static Int32 _exited;

    /// <summary>注册应用退出事件</summary>
    /// <remarks>在不同场景可能被多次执行，调用方需要做判断</remarks>
    /// <param name="onExit">回调函数</param>
    public static void RegisterExit(EventHandler onExit)
    {
        lock (_eventLock)
        {
            _events.Add(onExit);
        }
    }

    /// <summary>注册应用退出事件。仅执行一次</summary>
    /// <param name="onExit">回调函数</param>
    public static void RegisterExit(Action onExit)
    {
        lock (_eventLock)
        {
            _events2.Add(onExit);
        }
    }

    private static void OnExit(Object? sender, EventArgs e)
    {
        // 复制一份，避免遍历时被修改
        EventHandler[] handlers;
        lock (_eventLock)
        {
            handlers = _events.ToArray();
        }

        foreach (var item in handlers)
        {
            try
            {
                item(sender, e);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        // 只执行一次
        if (Interlocked.Increment(ref _exited) > 1) return;

        Action[] handlers2;
        lock (_eventLock)
        {
            handlers2 = _events2.ToArray();
        }

        foreach (var item in handlers2)
        {
            try
            {
                item();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
    }
    #endregion
}

/// <summary>后台服务基类</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/host
/// 
/// 提供后台长时间运行任务的基础实现。
/// 派生类只需重写 <see cref="ExecuteAsync"/> 方法即可。
/// </remarks>
public abstract class BackgroundService : IHostedService, IDisposable
{
    #region 属性
    private Task? _executingTask;
    private CancellationTokenSource? _stoppingCts;
    private Int32 _stopped;
    #endregion

    #region 方法
    /// <summary>执行后台任务</summary>
    /// <remarks>
    /// 该方法在服务启动时被调用，应包含长时间运行的逻辑。
    /// 当 <paramref name="stoppingToken"/> 被取消时，应优雅地结束任务。
    /// </remarks>
    /// <param name="stoppingToken">停止令牌</param>
    /// <returns>异步任务</returns>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    /// <summary>启动服务</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_stoppingCts.Token);
        return _executingTask.IsCompleted ? _executingTask : TaskEx.CompletedTask;
    }

    /// <summary>停止服务</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        // 防止重复停止
        if (Interlocked.CompareExchange(ref _stopped, 1, 0) != 0) return;

        if (_executingTask == null || _executingTask.IsCompleted) return;

        try
        {
            // 通知后台任务停止
            _stoppingCts?.Cancel();
        }
        finally
        {
            // 等待后台任务完成，但不超过取消令牌的超时时间
            // 使用 TaskCompletionSource 替代 Task.Delay(-1) 以避免取消时抛出异常
#if NET45
            var tcs = new TaskCompletionSource<Boolean>();
#else
            var tcs = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                await Task.WhenAny(_executingTask, tcs.Task).ConfigureAwait(false);
            }
        }
    }

    /// <summary>销毁资源</summary>
    public virtual void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
    }
    #endregion
}