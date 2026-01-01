using System.Collections.Concurrent;
using System.Diagnostics;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;

namespace NewLife.Model;

/// <summary>延迟队列。缓冲合并对象，批量处理</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/deferred_queue
/// 
/// 借助实体字典，缓冲实体对象，定期给字典换新，实现批量处理。
/// 
/// 有可能外部拿到对象后，正在修改，内部恰巧执行批量处理，导致外部的部分修改未能得到处理。
/// 解决办法是增加一个提交机制，外部用完后提交修改，内部需要处理时，等待一个时间。
/// </remarks>
public class DeferredQueue : DisposeBase
{
    #region 属性
    /// <summary>名称。用于日志和调试</summary>
    public String Name { get; set; }

    private volatile ConcurrentDictionary<String, Object> _Entities = new();
    /// <summary>实体字典。存储待处理的对象</summary>
    public ConcurrentDictionary<String, Object> Entities => _Entities;

    /// <summary>跟踪数。达到该值时输出跟踪日志，默认1000</summary>
    public Int32 TraceCount { get; set; } = 1000;

    /// <summary>周期。定时处理间隔，默认10_000毫秒</summary>
    public Int32 Period { get; set; } = 10_000;

    /// <summary>最大个数。超过该个数时，进入队列将产生堵塞。默认10_000_000</summary>
    public Int32 MaxEntity { get; set; } = 10_000_000;

    /// <summary>批大小。每批处理的最大对象数，默认5_000</summary>
    public Int32 BatchSize { get; set; } = 5_000;

    /// <summary>等待借出对象确认修改的时间。默认3000ms</summary>
    public Int32 WaitForBusy { get; set; } = 3_000;

    /// <summary>保存速度。每秒保存多少个实体</summary>
    public Int32 Speed { get; private set; }

    /// <summary>是否异步处理。默认true表示异步处理，共用DQ定时调度；false表示同步处理，独立线程</summary>
    public Boolean Async { get; set; } = true;

    private volatile Int32 _Times;
    /// <summary>合并保存的总次数</summary>
    public Int32 Times => _Times;

    /// <summary>批次处理成功时的回调</summary>
    public Action<IList<Object>>? Finish;

    /// <summary>批次处理失败时的回调</summary>
    public Action<IList<Object>, Exception>? Error;

    /// <summary>队列溢出通知。参数为当前缓存个数</summary>
    public Action<Int32>? Overflow;

    private volatile Int32 _count;
    /// <summary>当前缓存个数</summary>
    public Int32 Count => _count;

    /// <summary>等待确认修改的借出对象数</summary>
    private volatile Int32 _busy;

    private TimerX? _Timer;
    #endregion

    #region 构造
    /// <summary>实例化延迟队列</summary>
    public DeferredQueue() => Name = GetType().Name.TrimEnd("Queue", "Actor", "Cache");

    /// <summary>销毁资源。统计队列销毁时保存数据</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        try
        {
            // 停止调度器，尽量同步清空缓存，避免销毁时丢数据
            _Timer?.Dispose();
            Flush();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        _Entities?.Clear();
    }

    /// <summary>初始化定时器</summary>
    public void Init()
    {
        // 首次使用时初始化定时器
        if (_Timer == null)
        {
            lock (this)
            {
                _Timer ??= OnInit();
            }
        }
    }

    /// <summary>初始化定时器</summary>
    /// <returns>定时器实例</returns>
    protected virtual TimerX OnInit()
    {
        // 为了避免多队列并发，首次执行时间随机错开
        var p = Period;
        if (p > 1000) p = Rand.Next(1000, p);

        var name = Async ? "DQ" : Name;

        var timer = new TimerX(Work, null, p, Period, name) { Async = Async };

        // 独立调度时加大最大耗时告警
        if (!Async) timer.Scheduler.MaxCost = 30_000;

        return timer;
    }
    #endregion

    #region 方法
    /// <summary>尝试添加对象到队列</summary>
    /// <param name="key">对象键</param>
    /// <param name="value">对象值</param>
    /// <returns>是否添加成功</returns>
    public virtual Boolean TryAdd(String key, Object value)
    {
        Interlocked.Increment(ref _Times);

        Init();

        if (!_Entities.TryAdd(key, value)) return false;

        Interlocked.Increment(ref _count);

        // 超过最大值时，堵塞一段时间，等待消费完成
        CheckMax();

        return true;
    }

    /// <summary>获取或添加实体对象，在外部修改对象值</summary>
    /// <remarks>
    /// 外部正在修改对象时，内部不允许执行批量处理。
    /// 使用完毕后需调用 <see cref="Commit"/> 方法提交修改。
    /// </remarks>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="key">对象键</param>
    /// <param name="valueFactory">对象工厂，用于创建新对象</param>
    /// <returns>获取或创建的对象实例</returns>
    public virtual T? GetOrAdd<T>(String key, Func<String, T>? valueFactory = null) where T : class, new()
    {
        Interlocked.Increment(ref _Times);

        Init();

        Object? entity;
        while (!_Entities.TryGetValue(key, out entity))
        {
            if (entity == null)
            {
                if (valueFactory != null)
                    entity = valueFactory(key);
                else
                    entity = new T();
            }
            if (_Entities.TryAdd(key, entity))
            {
                Interlocked.Increment(ref _count);
                break;
            }
        }

        // 超过最大值时，堵塞一段时间，等待消费完成
        CheckMax();

        // 增加繁忙数
        Interlocked.Increment(ref _busy);

        return entity as T;
    }

    /// <summary>尝试移除一个键</summary>
    /// <param name="key">对象键</param>
    /// <returns>是否移除成功</returns>
    public virtual Boolean TryRemove(String key)
    {
        if (_Entities.TryRemove(key, out _))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }
        return false;
    }

    /// <summary>立即触发一次处理</summary>
    public void Trigger()
    {
        Init();
        _Timer?.SetNext(0);
    }

    /// <summary>同步清空并处理当前缓存</summary>
    public void Flush()
    {
        // 由于 Work 会交换 _Entities，因此循环直到为空
        while (!_Entities.IsEmpty)
        {
            Work(null);
        }
    }

    private void CheckMax()
    {
        if (_count < MaxEntity) return;

        using var span = Tracer?.NewError("MaxQueueOverflow", $"延迟队列[{Name}]超过上限{MaxEntity:n0}");

        // 通知外部发生溢出
        try { Overflow?.Invoke(_count); } catch { /* 忽略业务侧异常 */ }

        // 超过最大值时，堵塞一段时间，等待消费完成
        var t = WaitForBusy * 5;
        while (t > 0)
        {
            if (_count < MaxEntity) return;

            Thread.Sleep(100);
            t -= 100;
        }

        throw new InvalidOperationException($"The existing data amount [{_count:n0}] exceeds the maximum data amount [{MaxEntity:n0}]");
    }

    /// <summary>提交对象的修改，外部不再使用该对象</summary>
    /// <param name="key">对象键</param>
    public virtual void Commit(String key)
    {
        // 减少繁忙数
        if (_busy > 0) Interlocked.Decrement(ref _busy);
    }

    private void Work(Object? state)
    {
        var es = _Entities;
        if (es.IsEmpty) return;

        _Entities = new ConcurrentDictionary<String, Object>();
        var times = _Times;

        Interlocked.Add(ref _count, -es.Count);
        Interlocked.Add(ref _Times, -times);

        // 检查繁忙数，等待外部未完成的修改
        var t = WaitForBusy;
        while (_busy > 0 && t > 0)
        {
            Thread.Sleep(100);
            t -= 100;
        }
        //_busy = 0;

        // 先取出来
        var list = es.Values.ToList();

        if (list.Count == 0) return;

        using var span = Tracer?.NewSpan($"mq:{Name}:Process", null, list.Count);
        var sw = Stopwatch.StartNew();
        var total = ProcessAll(list);
        sw.Stop();

        var ms = sw.Elapsed.TotalMilliseconds;
        Speed = ms == 0 ? 0 : (Int32)(list.Count * 1000 / ms);
        if (list.Count >= TraceCount)
        {
            var sp = ms == 0 ? 0 : (Int32)(times * 1000 / ms);
            WriteLog($"保存 {list.Count:n0}\t耗时 {ms:n0}ms\t速度 {Speed:n0}tps\t次数 {times:n0}\t速度 {sp:n0}tps\t成功 {total:n0}");
        }
    }

    /// <summary>定时处理全部数据</summary>
    /// <param name="list">待处理对象集合</param>
    /// <returns>成功处理的对象数</returns>
    protected virtual Int32 ProcessAll(ICollection<Object> list)
    {
        var total = 0;

        // 使用 List.GetRange 降低分配
        var data = list as List<Object> ?? list.ToList();
        for (var i = 0; i < data.Count;)
        {
            var count = Math.Min(BatchSize, data.Count - i);
            var batch = data.GetRange(i, count);

            try
            {
                total += Process(batch);

                Finish?.Invoke(batch);
            }
            catch (Exception ex)
            {
                OnError(batch, ex);
            }

            i += count;
        }

        return total;
    }

    /// <summary>处理一批对象</summary>
    /// <param name="list">待处理对象列表</param>
    /// <returns>成功处理的对象数</returns>
    public virtual Int32 Process(IList<Object> list) => 0;

    /// <summary>发生错误时的处理</summary>
    /// <param name="list">处理失败的对象列表</param>
    /// <param name="ex">异常信息</param>
    protected virtual void OnError(IList<Object> list, Exception ex)
    {
        if (Error != null)
            Error(list, ex);
        else
            XTrace.WriteException(ex);
    }
    #endregion

    #region 辅助
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>链路追踪</summary>
    public ITracer? Tracer { get; set; }

    /// <summary>写日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    protected void WriteLog(String format, params Object?[] args) => Log?.Info($"延迟队列[{Name}]\t{format}", args);
    #endregion
}