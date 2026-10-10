using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

public class DnsResolverTests
{
    [Fact]
    [DisplayName("DnsResolver_缓存未过期_直接返回缓存")]
    public void FreshCache_ReturnsDirectly()
    {
        var resolver = new DnsResolver { Expire = TimeSpan.FromMinutes(5) };
        resolver.Set("localhost", [IPAddress.Parse("1.2.3.4")], 300);

        var addrs = resolver.Resolve("localhost");

        Assert.NotNull(addrs);
        Assert.Equal("1.2.3.4", addrs[0].ToString());
    }

    [Fact]
    [DisplayName("DnsResolver_陈旧数据超阈值_同步刷新返回最新地址")]
    public void StaleCache_SyncRefresh()
    {
        // 模拟长时间无人访问后仍指向过期地址的缓存：一小时前更新
        var resolver = new DnsResolver { Expire = TimeSpan.FromMilliseconds(1), StaleTime = TimeSpan.FromMinutes(10) };
        resolver.Set("localhost", [IPAddress.Parse("1.2.3.4")], -3600);

        var addrs = resolver.Resolve("localhost");

        Assert.NotNull(addrs);
        // 超过陈旧阈值时同步刷新，返回本机真实解析结果，而不是缓存中的假地址
        Assert.DoesNotContain(IPAddress.Parse("1.2.3.4"), addrs);
        Assert.Contains(addrs, IPAddress.IsLoopback);
    }

    [Fact]
    [DisplayName("DnsResolver_轻度超时_异步刷新不阻塞本次请求")]
    public void SlightlyExpired_AsyncRefresh()
    {
        // 仅超过 Expire 而未达 StaleTime 的缓存，由后台异步刷新，本次请求立即返回旧值
        var resolver = new DnsResolver { Expire = TimeSpan.FromMilliseconds(1), StaleTime = TimeSpan.FromMinutes(10) };

        // 使用必然解析失败的主机，保证后台刷新不会替换缓存，结果可确定断言
        var host = new String('a', 300) + ".invalid";
        resolver.Set(host, [IPAddress.Parse("1.2.3.4")], -10);

        var addrs = resolver.Resolve(host);

        // 后台异步刷新不影响本次返回，仍为旧地址
        Assert.NotNull(addrs);
        Assert.Equal("1.2.3.4", addrs[0].ToString());
        // 异步路径不设置退避时间，被设置即说明误入同步分支
        Assert.Equal(DateTime.MinValue, GetNextSyncTime(resolver, host));
    }

    [Fact]
    [DisplayName("DnsResolver_同步刷新失败_进入退避不再同步阻塞")]
    public void SyncRefreshFails_BacksOff()
    {
        var resolver = new DnsResolver { Expire = TimeSpan.FromSeconds(30), StaleTime = TimeSpan.FromMilliseconds(1) };

        // 使用必然解析失败的超长主机名，模拟DNS异常
        var host = new String('a', 300) + ".invalid";
        resolver.Set(host, [IPAddress.Parse("1.2.3.4")], -3600);

        // 第一次：陈旧数据触发同步刷新，解析失败后保留旧值
        var addrs = resolver.Resolve(host);
        Assert.NotNull(addrs);
        Assert.Equal("1.2.3.4", addrs[0].ToString());

        // 同步刷新失败后进入退避，避免DNS异常期间每个请求都被同步阻塞
        var nextSync = GetNextSyncTime(resolver, host);
        Assert.True(nextSync > DateTime.Now, "同步刷新失败后应设置退避时间");
    }

    [Fact]
    [DisplayName("DnsResolver_退避期内_不再同步刷新")]
    public void Backoff_NoSyncRefresh()
    {
        var resolver = new DnsResolver { Expire = TimeSpan.FromSeconds(30), StaleTime = TimeSpan.FromMilliseconds(1) };
        var host = new String('a', 300) + ".invalid";
        resolver.Set(host, [IPAddress.Parse("1.2.3.4")], -3600);

        // 第一次：同步刷新失败，进入退避
        var addrs = resolver.Resolve(host);
        Assert.NotNull(addrs);
        Assert.Equal("1.2.3.4", addrs[0].ToString());
        var nextSync1 = GetNextSyncTime(resolver, host);

        // 退避期内再次解析：改走异步刷新，不再同步阻塞，退避时间也不应被延长
        Thread.Sleep(50);
        addrs = resolver.Resolve(host);
        Assert.NotNull(addrs);
        Assert.Equal("1.2.3.4", addrs[0].ToString());
        Assert.Equal(nextSync1, GetNextSyncTime(resolver, host));
    }

    [Theory]
    [DisplayName("DnsResolver_空域名_返回null")]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyHost_ReturnsNull(String host)
    {
        var resolver = new DnsResolver();

        Assert.Null(resolver.Resolve(host));
    }

    [Fact]
    [DisplayName("DnsResolver_首次解析_同步等待并返回本机地址")]
    public void FirstResolve_Sync()
    {
        var resolver = new DnsResolver();

        var addrs = resolver.Resolve("localhost");

        Assert.NotNull(addrs);
        Assert.Contains(addrs, IPAddress.IsLoopback);
    }

    /// <summary>读取缓存项的下次同步刷新时间。退避行为无法从公开返回值观察，只能反射验证</summary>
    /// <param name="resolver">解析器</param>
    /// <param name="host">域名</param>
    /// <returns></returns>
    private static DateTime GetNextSyncTime(DnsResolver resolver, String host)
    {
        var cacheField = typeof(DnsResolver).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
        var cache = (IDictionary)cacheField!.GetValue(resolver)!;
        var item = cache[host]!;
        return (DateTime)item.GetType().GetProperty("NextSyncTime")!.GetValue(item)!;
    }
}
