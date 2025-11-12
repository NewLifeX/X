using NewLife.Data;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Net.Handlers;

/// <summary>消息匹配队列接口。用于把响应数据包配对到请求包</summary>
public interface IMatchQueue
{
    /// <summary>加入请求队列</summary>
    /// <param name="owner">拥有者</param>
    /// <param name="request">请求消息</param>
    /// <param name="msTimeout">超时取消时间</param>
    /// <param name="source">任务源</param>
    Task<Object> Add(Object? owner, Object request, Int32 msTimeout, TaskCompletionSource<Object> source);

    /// <summary>检查请求队列是否有匹配该响应的请求</summary>
    /// <param name="owner">拥有者</param>
    /// <param name="response">响应消息</param>
    /// <param name="result">任务结果</param>
    /// <param name="callback">用于检查匹配的回调</param>
    /// <returns></returns>
    Boolean Match(Object? owner, Object response, Object result, Func<Object?, Object?, Boolean> callback);

    /// <summary>清空队列</summary>
    void Clear();
}

/// <summary>消息匹配队列。子类可重载以自定义请求响应匹配逻辑</summary>
public class DefaultMatchQueue : IMatchQueue
{
    private struct ItemWrap
    {
        public Item? Value;
    }

    class Item
    {
        public Object? Owner { get; set; }
        public Object? Request { get; set; }
        public Int64 EndTime { get; set; }
        public TaskCompletionSource<Object>? Source { get; set; }
        public ISpan? Span { get; set; }
    }

    // 固定槽位数组 + 计数。使用CAS清理，避免多线程重复清理造成 _Count 不一致
    private readonly ItemWrap[] Items;
    private Int32 _Count;
    private TimerX? _Timer;

    // 追加一个游标，减少每次从0开始扫描导致的热点
    private Int32 _cursor;

    /// <summary>按指定大小来初始化队列</summary>
    /// <param name="size"></param>
    public DefaultMatchQueue(Int32 size = 256) => Items = new ItemWrap[size];

    /// <summary>加入请求队列</summary>
    /// <param name="owner">拥有者</param>
    /// <param name="request">请求的数据</param>
    /// <param name="msTimeout">超时取消时间</param>
    /// <param name="source">任务源</param>
    public virtual Task<Object> Add(Object? owner, Object request, Int32 msTimeout, TaskCompletionSource<Object> source)
    {
        var now = Runtime.TickCount64;

        // 控制超时时间，默认15秒
        if (msTimeout <= 10) msTimeout = 15_000;

        var ext = owner as IExtend;
        var qi = new Item
        {
            Owner = owner,
            Request = request,
            EndTime = now + msTimeout,
            Source = source,
            Span = ext?["Span"] as ISpan,
        };

        // 若计数已接近容量，先做一次快速清理以回收过期项，避免“看似满”的误判
        var items = Items;
        if (Volatile.Read(ref _Count) >= items.Length)
        {
            Check(null);
        }

        // 加入队列（从游标位置开始扫描，避免总是从0导致争用）
        var len = items.Length;
        var start = _cursor;
        for (var offset = 0; offset < len; ++offset)
        {
            var i = start + offset;
            if (i >= len) i -= len;

            if (Interlocked.CompareExchange(ref items[i].Value, qi, null) == null)
            {
                Interlocked.Increment(ref _Count);

                // 推进游标
                if (++i >= len) i = 0;
                _cursor = i;

                if (_Timer == null)
                {
                    lock (this)
                    {
                        _Timer ??= new TimerX(Check, null, 1000, 1000, "Match") { Async = true };
                    }
                }

                return source.Task;
            }
        }

        // 第一次扫描失败后，再进行一次同步清理并重试，最后才认为真的满
        Check(null);

        // 重试一次
        items = Items; // 允许未来可能的扩容，这里重新读取引用
        len = items.Length;
        start = _cursor;
        for (var offset = 0; offset < len; ++offset)
        {
            var i = start + offset;
            if (i >= len) i -= len;

            if (Interlocked.CompareExchange(ref items[i].Value, qi, null) == null)
            {
                Interlocked.Increment(ref _Count);

                if (++i >= len) i = 0;
                _cursor = i;

                if (_Timer == null)
                {
                    lock (this)
                    {
                        _Timer ??= new TimerX(Check, null, 1000, 1000, "Match") { Async = true };
                    }
                }

                return source.Task;
            }
        }

        DefaultTracer.Instance?.NewError("net:MatchQueue:IsFull", new { items.Length });
        throw new XException("The matching queue is full [{0}]", items.Length);
    }

    /// <summary>检查请求队列是否有匹配该响应的请求</summary>
    /// <param name="owner">拥有者</param>
    /// <param name="response">响应消息</param>
    /// <param name="result">任务结果</param>
    /// <param name="callback">用于检查匹配的回调</param>
    /// <returns></returns>
    public virtual Boolean Match(Object? owner, Object response, Object result, Func<Object?, Object?, Boolean> callback)
    {
        if (Volatile.Read(ref _Count) <= 0) return false;

        // 直接遍历，队列不会很长
        var qs = Items;
        for (var i = 0; i < qs.Length; i++)
        {
            var qi = Volatile.Read(ref qs[i].Value);
            if (qi == null) continue;

            if (qi.Owner == owner && callback(qi.Request, response))
            {
                // CAS 置空，确保仅一次成功清理，避免并发重复清理造成 _Count 错乱
                if (Interlocked.CompareExchange(ref qs[i].Value, null, qi) != qi) continue;

                Interlocked.Decrement(ref _Count);

                // 异步设置完成结果，否则可能会在当前线程恢复上层await，导致堵塞当前任务
                var src = qi.Source;
                if (src != null && !src.Task.IsCompleted)
                {
                    qi.Span?.AppendTag($"{Runtime.TickCount64} MatchQueue.SetResult(Matched)");
#if NET45
                    Task.Factory.StartNew(() => src.TrySetResult(result));
#else
                    src.TrySetResult(result);
#endif
                }

                return true;
            }
        }

        if (SocketSetting.Current.Debug)
            XTrace.WriteLine("MatchQueue.Check 失败 [{0}] result={1} Items={2}", response, result, _Count);

        return false;
    }

    /// <summary>定时检查发送队列，超时未收到响应则重发</summary>
    /// <param name="state"></param>
    void Check(Object? state)
    {
        if (Volatile.Read(ref _Count) <= 0) return;

        // 直接遍历，队列不会很长
        var now = Runtime.TickCount64;
        var qs = Items;
        for (var i = 0; i < qs.Length; i++)
        {
            var qi = Volatile.Read(ref qs[i].Value);
            if (qi == null) continue;

            // 过期取消
            if (qi.EndTime <= now)
            {
                if (Interlocked.CompareExchange(ref qs[i].Value, null, qi) != qi) continue;

                Interlocked.Decrement(ref _Count);

                // 异步取消任务，避免在当前线程执行上层await的延续任务
                var src = qi.Source;
                if (src != null && !src.Task.IsCompleted)
                {
                    qi.Span?.AppendTag($"{Runtime.TickCount64} MatchQueue.Expired({qi.EndTime}<={now})");

#if NET45
                    Task.Factory.StartNew(() => src.TrySetCanceled());
#else
                    src.TrySetCanceled();
#endif
                }
            }
        }
    }

    /// <summary>清空队列</summary>
    public virtual void Clear()
    {
        var qs = Items;
        for (var i = 0; i < qs.Length; ++i)
        {
            var qi = Interlocked.Exchange(ref qs[i].Value, null);
            if (qi == null) continue;

            Interlocked.Decrement(ref _Count);

            // 异步取消任务，避免在当前线程执行上层await的延续任务
            var src = qi.Source;
            if (src != null && !src.Task.IsCompleted)
            {
                qi.Span?.AppendTag("MatchQueue.Clear()");

#if NET45
                Task.Factory.StartNew(() => src.TrySetCanceled());
#else
                src.TrySetCanceled();
#endif
            }
        }
        _Count = 0;
    }
}