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
    IPAddress[]? Resolve(String host);
}

/// <summary>DNS解析器，带有缓存，解析失败时使用旧数据</summary>
public class DnsResolver : IDnsResolver
{
    /// <summary>静态实例</summary>
    public static DnsResolver Instance { get; set; } = new();

    /// <summary>缓存超时时间</summary>
    public TimeSpan Expire { set; get; } = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<String, DnsItem> _cache = new();
    private readonly ConcurrentDictionary<String, Byte> _refreshing = new(); // 刷新去重

    /// <summary>解析域名</summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public IPAddress[]? Resolve(String host)
    {
        if (host.IsNullOrEmpty()) return null;

        if (_cache.TryGetValue(host, out var item))
        {
            // 超时数据，异步更新，不影响当前请求
            if (item.UpdateTime.Add(Expire) <= DateTime.Now)
            {
                if (_refreshing.TryAdd(host, 0)) _ = ResolveCoreAsync(host, item, false);
            }
        }
        else
        {
            // 首次解析同步等待（保持现有同步接口语义）
            item = ResolveCoreAsync(host, null, true).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        return item?.Addresses;
    }

    /// <summary>异步解析/刷新核心</summary>
    /// <param name="host"></param>
    /// <param name="item"></param>
    /// <param name="throwError"></param>
    /// <returns></returns>
    async Task<DnsItem?> ResolveCoreAsync(String host, DnsItem? item, Boolean throwError)
    {
        using var span = DefaultTracer.Instance?.NewSpan($"dns:{host}");
        try
        {
            // 执行DNS解析
#if NET6_0_OR_GREATER
            using var source = new CancellationTokenSource(5000);
            var addrs = await Dns.GetHostAddressesAsync(host, source.Token).ConfigureAwait(false);
#else
            var task = Dns.GetHostAddressesAsync(host);
            if (!task.Wait(5000)) throw new TaskCanceledException();
            var addrs = task.ConfigureAwait(false).GetAwaiter().GetResult();
#endif
            span?.AppendTag($"addrs={addrs.Join(",")}");
            if (addrs != null && addrs.Length > 0)
            {
                span?.Value = addrs.Length;

                // 更新缓存数据
                if (item == null)
                {
                    _cache[host] = item = new DnsItem
                    {
                        Host = host,
                        Addresses = addrs,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    };
                }
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
            if (item != null) return item; // 保留旧值

            span?.SetError(ex, null);

            if (throwError) throw;
        }
        finally
        {
            // 结束刷新标记
            _refreshing.TryRemove(host, out _);
        }

        return item;
    }

    /// <summary>设置缓存</summary>
    /// <remarks>一般用于单元测试，或者局部篡改DNS解析。受Expire影响，过期后仍然刷新</remarks>
    /// <param name="host">域名</param>
    /// <param name="addrs">IP地址集合</param>
    /// <param name="expire">过期时间。默认0秒，使用Exire</param>
    public void Set(String host, IPAddress[] addrs, Int32 expire = 0)
    {
        var item = new DnsItem
        {
            Host = host,
            Addresses = addrs,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now.AddSeconds(expire)
        };
        _cache[host] = item;
    }

    class DnsItem
    {
        public String Host { get; set; } = null!;

        public IPAddress[]? Addresses { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }
}