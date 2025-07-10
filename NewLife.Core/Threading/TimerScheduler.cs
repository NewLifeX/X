using System.Diagnostics;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;

namespace NewLife.Threading;

/// <summary>定时器调度器</summary>
public class TimerScheduler : IDisposable, ILogFeature
{
    #region 静态
    private static readonly Dictionary<String, TimerScheduler> _cache = [];
    static TimerScheduler()
    {
        Host.RegisterExit(ClearAll);
    }

    /// <summary>创建指定名称的调度器</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static TimerScheduler Create(String name)
    {
        if (_cache.TryGetValue(name, out var scheduler)) return scheduler;
        lock (_cache)
        {
            if (_cache.TryGetValue(name, out scheduler)) return scheduler;

            scheduler = new TimerScheduler(name);
            _cache[name] = scheduler;

            // 跟随默认调度器使用日志
            if (_cache.TryGetValue("Default", out var def) && def.Log != null)
                scheduler.Log = def.Log;

            return scheduler;
        }
    }

    private static void ClearAll()
    {
        var schedulers = _cache;
        if (schedulers == null || schedulers.Count == 0) return;

        XTrace.WriteLine("TimerScheduler.ClearAll [{0}]", schedulers.Count);
        foreach (var item in schedulers)
        {
            item.Value.Dispose();
        }

        schedulers.Clear();
    }

    /// <summary>默认调度器</summary>
    public static TimerScheduler Default { get; } = Create("Default");

    [ThreadStatic]
    private static TimerScheduler? _Current;
    /// <summary>当前调度器</summary>
    public static TimerScheduler? Current { get => _Current; private set => _Current = value; }

    /// <summary>全局时间提供者。影响所有调度器</summary>
    public static TimeProvider GlobalTimeProvider { get; set; } = TimeProvider.System;
    #endregion

    #region 构造
    private TimerScheduler(String name) => Name = name;

    /// <summary>销毁</summary>
    public void Dispose()
    {
        var ts = Timers?.ToList();
        if (ts != null && ts.Count > 0)
        {
            XTrace.WriteLine("{0}Timer.ClearAll [{1}]", Name, ts.Count);
            foreach (var item in ts)
            {
                item.Dispose();
            }
        }
    }
    #endregion

    #region 属性
    /// <summary>名称</summary>
    public String Name { get; private set; }

    /// <summary>定时器个数</summary>
    public Int32 Count { get; private set; }

    /// <summary>最大耗时。超过时报警告日志，默认500ms</summary>
    public Int32 MaxCost { get; set; } = 500;

    /// <summary>时间提供者。该调度器下所有绝对定时器，均从此获取当前时间</summary>
    public TimeProvider? TimeProvider { get; set; }

    private Thread? thread;
    private Int32 _tid;

    private TimerX[] Timers = [];
    #endregion

    /// <summary>把定时器加入队列</summary>
    /// <param name="timer"></param>
    public void Add(TimerX timer)
    {
        if (timer == null) throw new ArgumentNullException(nameof(timer));

        using var span = DefaultTracer.Instance?.NewSpan("timer:Add", new { Name, timer = timer.ToString() });

        timer.Id = Interlocked.Increment(ref _tid);
        WriteLog("Timer.Add {0}", timer);

        lock (this)
        {
            var list = new List<TimerX>(Timers);
            if (list.Contains(timer)) return;
            list.Add(timer);

            Timers = list.ToArray();

            Count++;

            if (thread == null)
            {
                thread = new Thread(Process)
                {
                    Name = Name == "Default" ? "T" : Name,
                    IsBackground = true
                };
                thread.Start();

                WriteLog("启动定时调度器：{0}", Name);
            }

            Wake();
        }
    }

    /// <summary>从队列删除定时器</summary>
    /// <param name="timer"></param>
    /// <param name="reason"></param>
    public void Remove(TimerX timer, String reason)
    {
        if (timer == null || timer.Id == 0) return;

        using var span = DefaultTracer.Instance?.NewSpan("timer:Remove", new { Name, timer = timer.ToString(), reason });
        WriteLog("Timer.Remove {0} reason:{1}", timer, reason);

        lock (this)
        {
            timer.Id = 0;

            var list = new List<TimerX>(Timers);
            if (list.Contains(timer))
            {
                list.Remove(timer);
                Timers = list.ToArray();

                Count--;
            }
        }
    }

    private AutoResetEvent? _waitForTimer;
    private Int32 _period = 10;

    /// <summary>唤醒处理</summary>
    public void Wake()
    {
        var e = _waitForTimer;
        if (e != null)
        {
            var swh = e.SafeWaitHandle;
            if (swh != null && !swh.IsClosed) e.Set();
        }
    }

    /// <summary>调度主程序</summary>
    /// <param name="state"></param>
    private void Process(Object? state)
    {
        Current = this;
        while (true)
        {
            // 准备好定时器列表
            var arr = Timers;

            // 如果没有任务，则销毁线程
            if (arr.Length == 0 && _period == 60_000)
            {
                WriteLog("没有可用任务，销毁线程");

                var th = thread;
                thread = null;
                //th?.Abort();

                break;
            }

            try
            {
                var now = Runtime.TickCount64;

                // 设置一个较大的间隔，内部会根据处理情况调整该值为最合理值
                _period = 60_000;
                foreach (var timer in arr)
                {
                    if (!timer.Calling && CheckTime(timer, now))
                    {
                        // 必须在主线程设置状态，否则可能异步线程还没来得及设置开始状态，主线程又开始了新的一轮调度
                        timer.Calling = true;
                        if (timer.IsAsyncTask)
                            Task.Factory.StartNew(ExecuteAsync, timer);
                        else if (!timer.Async)
                            Execute(timer);
                        else
                            //Task.Factory.StartNew(() => ProcessItem(timer));
                            // 不需要上下文流动，捕获所有异常
                            ThreadPool.UnsafeQueueUserWorkItem(s =>
                            {
                                try
                                {
                                    Execute(s);
                                }
                                catch (Exception ex)
                                {
                                    XTrace.WriteException(ex);
                                }
                            }, timer);
                    }
                }
            }
            catch (ThreadAbortException) { break; }
            catch (ThreadInterruptedException) { break; }
            catch { }

            _waitForTimer ??= new AutoResetEvent(false);
            if (_period > 0) _waitForTimer.WaitOne(_period, true);
        }
    }

    /// <summary>检查定时器是否到期</summary>
    /// <param name="timer"></param>
    /// <param name="now"></param>
    /// <returns></returns>
    private Boolean CheckTime(TimerX timer, Int64 now)
    {
        // 删除过期的，为了避免占用过多CPU资源，TimerX禁止小于10ms的任务调度
        var p = timer.Period;
        if (p is < 10 and > 0)
        {
            // 周期0表示只执行一次
            if (p is < 10 and > 0) XTrace.WriteLine("为了避免占用过多CPU资源，TimerX禁止小于{1}ms<10ms的任务调度，关闭任务{0}", timer, p);
            timer.Dispose();
            return false;
        }

        var ts = timer.NextTick - now;
        if (ts > 0)
        {
            // 缩小间隔，便于快速调用
            if (ts < _period) _period = (Int32)ts;

            return false;
        }

        return true;
    }

    /// <summary>处理每一个定时器</summary>
    /// <param name="state"></param>
    private void Execute(Object? state)
    {
        if (state is not TimerX timer) return;

        TimerX.Current = timer;

        // 控制日志显示
        WriteLogEventArgs.CurrentThreadName = Name == "Default" ? "T" : Name;

        timer.hasSetNext = false;

        DefaultSpan.Current = null;
        using var span = timer.Tracer?.NewSpan(timer.TracerName ?? $"timer:Execute", timer.Timers + "");
        var sw = Stopwatch.StartNew();
        try
        {
            // 弱引用判断
            var target = timer.Target.Target;
            if (target == null && !timer.Method.IsStatic)
            {
                Remove(timer, "委托已不存在（GC回收委托所在对象）");
                timer.Dispose();
                return;
            }

            var func = timer.Method.As<TimerCallback>(target);
            func!(timer.State);
        }
        catch (ThreadAbortException) { throw; }
        catch (ThreadInterruptedException) { throw; }
        // 如果用户代码没有拦截错误，则这里拦截，避免出错了都不知道怎么回事
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteException(ex);
        }
        finally
        {
            sw.Stop();

            OnExecuted(timer, (Int32)sw.ElapsedMilliseconds);
        }
    }

    /// <summary>处理每一个定时器</summary>
    /// <param name="state"></param>
    private async void ExecuteAsync(Object? state)
    {
        if (state is not TimerX timer) return;

        TimerX.Current = timer;

        // 控制日志显示
        WriteLogEventArgs.CurrentThreadName = Name == "Default" ? "T" : Name;

        timer.hasSetNext = false;

        DefaultSpan.Current = null;
        using var span = timer.Tracer?.NewSpan(timer.TracerName ?? $"timer:ExecuteAsync", timer.Timers + "");
        var sw = Stopwatch.StartNew();
        try
        {
            // 弱引用判断
            var target = timer.Target.Target;
            if (target == null && !timer.Method.IsStatic)
            {
                Remove(timer, "委托已不存在（GC回收委托所在对象）");
                timer.Dispose();
                return;
            }

            var func = timer.Method.As<Func<Object?, Task>>(target);
            await func!(timer.State).ConfigureAwait(false);
        }
        catch (ThreadAbortException) { throw; }
        catch (ThreadInterruptedException) { throw; }
        // 如果用户代码没有拦截错误，则这里拦截，避免出错了都不知道怎么回事
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteException(ex);
        }
        finally
        {
            sw.Stop();

            OnExecuted(timer, (Int32)sw.ElapsedMilliseconds);
        }
    }

    private void OnExecuted(TimerX timer, Int32 ms)
    {
        timer.Cost = timer.Cost == 0 ? ms : (timer.Cost + ms) / 2;

        if (ms > MaxCost && !timer.Async && !timer.IsAsyncTask) XTrace.WriteLine("任务 {0} 耗时过长 {1:n0}ms，建议使用异步任务Async=true", timer, ms);

        timer.Timers++;
        OnFinish(timer);

        timer.Calling = false;

        TimerX.Current = null;

        // 控制日志显示
        WriteLogEventArgs.CurrentThreadName = null;

        // 调度线程可能在等待，需要唤醒
        Wake();
    }

    private void OnFinish(TimerX timer)
    {
        // 如果内部设置了下一次时间，则不再递加周期
        var p = timer.SetAndGetNextTime();

        // 清理一次性定时器
        if (p <= 0)
        {
            Remove(timer, "Period<=0");
            timer.Dispose();
        }
        else if (p < _period)
            _period = p;
    }

    /// <summary>获取当前时间。该调度器下所有绝对定时器，均从此获取当前时间</summary>
    /// <returns></returns>
    public DateTime GetNow() => (TimeProvider ?? GlobalTimeProvider).GetUtcNow().LocalDateTime;

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => Name;

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    private void WriteLog(String format, params Object?[] args) => Log?.Info(Name + format, args);
    #endregion
}
