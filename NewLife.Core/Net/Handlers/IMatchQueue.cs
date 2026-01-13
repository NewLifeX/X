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
    /// <param name="size">队列大小</param>
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

        var items = Items;
        var len = items.Length;

        // 若计数已接近容量，先做一次快速清理以回收过期项，避免"看似满"的误判
        if (Volatile.Read(ref _Count) >= len) Check(null);

        // 加入队列（从游标位置开始扫描，避免总是从0导致争用）
        var start = Volatile.Read(ref _cursor);
        for (var offset = 0; offset < len; ++offset)
        {
            var i = (start + offset) % len;
            if (Interlocked.CompareExchange(ref items[i].Value, qi, null) == null)
            {
                Interlocked.Increment(ref _Count);

                // 推进游标到下一个位置
                Volatile.Write(ref _cursor, (i + 1) % len);

                StartTimer();
                return source.Task;
            }
        }

        // 第一次扫描失败后，再进行一次同步清理并重试
        Check(null);

        // 重试一次
        start = Volatile.Read(ref _cursor);
        for (var offset = 0; offset < len; ++offset)
        {
            var i = (start + offset) % len;
            if (Interlocked.CompareExchange(ref items[i].Value, qi, null) == null)
            {
                Interlocked.Increment(ref _Count);
                Volatile.Write(ref _cursor, (i + 1) % len);

                StartTimer();
                return source.Task;
            }
        }

        DefaultTracer.Instance?.NewError("net:MatchQueue:IsFull", new { items.Length });
        throw new XException("The matching queue is full [{0}]", items.Length);
    }

    private void StartTimer()
    {
        if (_Timer != null) return;
        lock (this)
        {
            _Timer ??= new TimerX(Check, null, 1000, 1000, "Match") { Async = true };
        }
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

        // 从游标位置向前搜索，响应通常与最近的请求匹配
        var items = Items;
        var len = items.Length;
        var start = Volatile.Read(ref _cursor);

        // 先从游标往前搜索（最近添加的请求更可能匹配）
        for (var offset = 1; offset <= len; ++offset)
        {
            var i = (start - offset + len) % len;
            var qi = Volatile.Read(ref items[i].Value);
            if (qi == null) continue;

            if (qi.Owner == owner && callback(qi.Request, response))
            {
                // CAS 置空，确保仅一次成功清理
                if (Interlocked.CompareExchange(ref items[i].Value, null, qi) != qi) continue;

                Interlocked.Decrement(ref _Count);

                // 设置完成结果，TaskCreationOptions.RunContinuationsAsynchronously确保不会阻塞当前线程
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
            XTrace.WriteLine("MatchQueue.Match 失败 [{0}] result={1} Items={2}", response, result, _Count);

        return false;
    }

    /// <summary>定时检查发送队列，超时未收到响应则取消</summary>
    /// <param name="state">状态参数</param>
    void Check(Object? state)
    {
        if (Volatile.Read(ref _Count) <= 0) return;

        var now = Runtime.TickCount64;
        var items = Items;

        // 遍历清理过期项
        for (var i = 0; i < items.Length; i++)
        {
            var qi = Volatile.Read(ref items[i].Value);
            if (qi == null) continue;

            // 过期取消
            if (qi.EndTime <= now)
            {
                if (Interlocked.CompareExchange(ref items[i].Value, null, qi) != qi) continue;

                Interlocked.Decrement(ref _Count);

                // 异步取消任务
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
        var items = Items;
        for (var i = 0; i < items.Length; ++i)
        {
            var qi = Interlocked.Exchange(ref items[i].Value, null);
            if (qi == null) continue;

            Interlocked.Decrement(ref _Count);

            // 异步取消任务
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