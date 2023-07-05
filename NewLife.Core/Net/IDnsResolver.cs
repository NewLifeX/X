using System.Collections.Concurrent;
using System.Net;
using NewLife.Log;

namespace NewLife.Net;

/// <summary>DNS解析器</summary>
public interface IDnsResolver
{
    /// <summary>解析域名</summary>
    /// <param name="host"></param>
    /// <returns></returns>
    IPAddress[] Resolve(String host);
}

/// <summary>DNS解析器，带有缓存，解析失败时使用旧数据</summary>
public class DnsResolver : IDnsResolver
{
    /// <summary>静态实例</summary>
    public static DnsResolver Instance { get; set; } = new();

    /// <summary>缓存超时时间</summary>
    public TimeSpan Expire { set; get; } = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<String, DnsItem> _cache = new();

    /// <summary>解析域名</summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public IPAddress[] Resolve(String host)
    {
        if (_cache.TryGetValue(host, out var item))
        {
            // 超时数据，异步更新，不影响当前请求
            if (item.UpdateTime.Add(Expire) <= DateTime.Now)
                _ = Task.Run(() => ResolveCore(host, item, false));
        }
        else
            item = ResolveCore(host, item, true);

        return item.Addresses;
    }

    DnsItem ResolveCore(String host, DnsItem item, Boolean throwError)
    {
        using var span = DefaultTracer.Instance?.NewSpan($"dns:{host}");
        try
        {
            // 执行DNS解析
#if NET6_0_OR_GREATER
            using var source = new CancellationTokenSource(5000);
            var task = Dns.GetHostAddressesAsync(host, source.Token);
            var addrs = task.ConfigureAwait(false).GetAwaiter().GetResult();
#else
            var task = Dns.GetHostAddressesAsync(host);
            if (!task.Wait(5000)) throw new TaskCanceledException();
            var addrs = task.Result;
#endif
            span?.AppendTag($"addrs={addrs.Join(",")}");
            if (addrs != null && addrs.Length > 0)
            {
                // 更新缓存数据
                if (item == null)
                    _cache[host] = item = new DnsItem
                    {
                        Host = host,
                        Addresses = addrs,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    };
                else
                {
                    item.Addresses = addrs;
                    item.UpdateTime = DateTime.Now;

                    span?.AppendTag($"CreateTime={item.CreateTime.ToFullString()}");
                }
            }
        }
        catch (Exception ex)
        {
            if (item != null) return item;

            span?.SetError(ex, null);

            if (throwError) throw;
        }

        return item;
    }

    class DnsItem
    {
        public String Host { get; set; }

        public IPAddress[] Addresses { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }
}