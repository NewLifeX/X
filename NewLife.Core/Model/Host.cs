using System.Runtime.InteropServices;
using NewLife.Log;

namespace NewLife.Model;

/// <summary>轻量级主机服务</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/host
/// </remarks>
public interface IHostedService
{
    /// <summary>开始服务</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>停止服务</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StopAsync(CancellationToken cancellationToken);
}

/// <summary>轻量级应用主机</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/host
/// </remarks>
public interface IHost
{
    /// <summary>添加服务</summary>
    /// <param name="service"></param>
    void Add(IHostedService service);

    /// <summary>添加服务</summary>
    /// <typeparam name="TService"></typeparam>
    void Add<TService>() where TService : IHostedService;

    /// <summary>同步运行，大循环阻塞</summary>
    void Run();

    /// <summary>异步允许，大循环阻塞</summary>
    /// <returns></returns>
    Task RunAsync();
}

/// <summary>轻量级应用主机</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/host
/// </remarks>
public class Host : IHost
{
    #region 属性
    /// <summary>服务提供者</summary>
    public IServiceProvider ServiceProvider { get; set; }

    private readonly IList<Type> _serviceTypes = new List<Type>();
    /// <summary>服务集合</summary>
    public IList<IHostedService> Services { get; } = new List<IHostedService>();
    #endregion

    #region 构造
    static Host()
    {
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        Console.CancelKeyPress += OnExit;
#if NETCOREAPP
        System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += ctx => OnExit(ctx, null);
#endif
#if NET6_0_OR_GREATER
        PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => OnExit(ctx, null));
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => OnExit(ctx, null));
#endif
    }

    /// <summary>通过制定服务提供者来实例化一个应用主机</summary>
    /// <param name="serviceProvider"></param>
    public Host(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;
    #endregion

    #region 服务集合
    /// <summary>添加服务</summary>
    /// <typeparam name="TService"></typeparam>
    public void Add<TService>() where TService : IHostedService
    {
        var type = typeof(TService);
        _serviceTypes.Add(type);

        var ioc = (ServiceProvider as ServiceProvider)?.Container ?? ObjectContainer.Current;
        ioc.TryAddTransient(type, type);
    }

    /// <summary>添加服务</summary>
    /// <param name="service"></param>
    public void Add(IHostedService service) => Services.Add(service);
    #endregion

    #region 开始停止
    /// <summary>开始</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var item in _serviceTypes)
        {
            if (ServiceProvider.GetService(item) is IHostedService service) Services.Add(service);
        }
        foreach (var item in Services)
        {
            await item.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>停止</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var item in Services)
        {
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
    private TaskCompletionSource<Object> _life;
    /// <summary>同步运行，大循环阻塞</summary>
    public void Run() => RunAsync().GetAwaiter().GetResult();

    /// <summary>异步允许，大循环阻塞</summary>
    /// <returns></returns>
    public async Task RunAsync()
    {
        XTrace.WriteLine("Starting......");

        using var source = new CancellationTokenSource();

        _life = new TaskCompletionSource<Object>();

        RegisterExit((s, e) => _life.TrySetResult(null));

        await StartAsync(source.Token);
        XTrace.WriteLine("Application started. Press Ctrl+C to shut down.");

        await _life.Task;

        XTrace.WriteLine("Application is shutting down...");

        await StopAsync(source.Token);

        XTrace.WriteLine("Stopped!");
    }
    #endregion

    #region 退出事件
    private static List<EventHandler> _events = new();
    private static List<Action> _events2 = new();
    private static Int32 _exited;
    /// <summary>注册应用退出事件</summary>
    /// <remarks>在不同场景可能被多次执行，调用方需要做判断</remarks>
    /// <param name="onExit">回调函数</param>
    public static void RegisterExit(EventHandler onExit) => _events.Add(onExit);

    /// <summary>注册应用退出事件。仅执行一次</summary>
    /// <param name="onExit">回调函数</param>
    public static void RegisterExit(Action onExit) => _events2.Add(onExit);

    private static void OnExit(Object sender, EventArgs e)
    {
        foreach (var item in _events)
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

        foreach (var item in _events2)
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

/// <summary>后台任务</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/host
/// </remarks>
public abstract class BackgroundService : IHostedService, IDisposable
{
    private Task _executingTask;

    private CancellationTokenSource _stoppingCts;

    /// <summary>执行</summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    /// <summary>开始</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_stoppingCts.Token);
#if NET45
        return _executingTask.IsCompleted ? _executingTask : Task.FromResult(0);
#else
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
#endif
    }

    /// <summary>停止</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask != null)
        {
            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }

    /// <summary>销毁</summary>
    public virtual void Dispose() => _stoppingCts?.Cancel();
}