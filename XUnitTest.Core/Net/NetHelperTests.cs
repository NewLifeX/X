using System.Net.NetworkInformation;
using System.Reflection;
using NewLife;
using NewLife.Caching;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>NetHelper 缓存隔离测试</summary>
[Collection("Net.A")]
public class NetHelperTests
{
    [Fact(DisplayName = "网络变化只清自己的缓存_不动全局缓存")]
    public void NetworkChange_ShouldNotClearGlobalCache()
    {
        // 全局缓存放入哨兵键：NetHelper 旧实现直接用 MemoryCache.Instance 并在网络变化时 Clear，
        // 插拔网线/VPN 切换会把整个应用的业务缓存一并清掉（缓存击穿甚至雪崩）
        var global = MemoryCache.Instance;
        global.Set("NetHelperTests:sentinel", "keep", 60);

        try
        {
            // 直接触发 NetHelper 的网络变化回调（私有静态）
            var mi = typeof(NetHelper).GetMethod("NetworkChange_NetworkAddressChanged", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mi);
            mi!.Invoke(null, [null, EventArgs.Empty]);

            // 全局缓存的哨兵键必须仍在
            Assert.True(global.TryGetValue<String>("NetHelperTests:sentinel", out var v));
            Assert.Equal("keep", v);
        }
        finally
        {
            global.Remove("NetHelperTests:sentinel");
        }
    }

    [Fact(DisplayName = "分析终结点：畸形与非典型地址返回 null 或正常值，不抛异常")]
    public void ParseEndPoint_EdgeCases()
    {
        // 常规形态
        Assert.Equal("127.0.0.1:8080", NetHelper.ParseEndPoint("127.0.0.1:8080") + "");
        Assert.Equal("127.0.0.1:80", NetHelper.ParseEndPoint("127.0.0.1", 80) + "");

        // 带协议前缀与路径：端口后不要带路径
        Assert.Equal("127.0.0.1:8080", NetHelper.ParseEndPoint("http://127.0.0.1:8080/api") + "");

        // IPv6 各种形态
        Assert.Equal("[::1]:80", NetHelper.ParseEndPoint("::1", 80) + "");
        Assert.Equal("[::1]:80", NetHelper.ParseEndPoint("[::1]", 80) + "");
        Assert.Equal("[::1]:8080", NetHelper.ParseEndPoint("[::1]:8080") + "");

        // 畸形：不抛异常，返回 null
        Assert.Null(NetHelper.ParseEndPoint("127.0.0.1:abc"));
        Assert.Null(NetHelper.ParseEndPoint("[::1"));
        Assert.Null(NetHelper.ParseEndPoint("[::1]x:80"));
    }
}
