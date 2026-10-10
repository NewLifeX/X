using NewLife;
using NewLife.Caching;
using NewLife.Net;
using System;
using System.Net.NetworkInformation;
using System.Reflection;
using Xunit;

namespace XUnitTest.Net;

/// <summary>网络工具安全扩展测试：私有网段判定、CIDR 匹配、可信代理与转发链解析</summary>
public class NetHelperTests
{
    [Theory(DisplayName = "IsPrivateIp 私有网段判定")]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("169.254.1.1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("172.32.0.1", false)]
    [InlineData("11.0.0.1", false)]
    [InlineData("", false)]
    [InlineData("not-an-ip", false)]
    public void IsPrivateIp_Works(String ip, Boolean expected) => Assert.Equal(expected, ip.IsPrivateIp());

    [Theory(DisplayName = "IsMatchCidr 网段匹配")]
    [InlineData("10.1.2.3", "10.0.0.0/8", true)]
    [InlineData("11.1.2.3", "10.0.0.0/8", false)]
    [InlineData("192.168.1.5", "192.168.1.0/24", true)]
    [InlineData("192.168.2.5", "192.168.1.0/24", false)]
    [InlineData("172.20.1.1", "172.16.0.0/12", true)]
    [InlineData("172.32.1.1", "172.16.0.0/12", false)]
    [InlineData("1.2.3.4", "1.2.3.4/32", true)]
    [InlineData("1.2.3.5", "1.2.3.4/32", false)]
    [InlineData("8.8.8.8", "0.0.0.0/0", true)]
    [InlineData("8.8.8.8", "bad-cidr", false)]
    public void IsMatchCidr_Works(String ip, String cidr, Boolean expected) => Assert.Equal(expected, ip.IsMatchCidr(cidr));

    [Fact(DisplayName = "IsTrustedProxy 精确、通配与CIDR")]
    public void IsTrustedProxy_Works()
        {
        Assert.True("10.0.0.5".IsTrustedProxy(["10.0.0.5"]));
        Assert.True("10.0.0.5".IsTrustedProxy(["10.0.*"]));
        Assert.True("10.1.2.3".IsTrustedProxy(["10.0.0.0/8"]));
        Assert.True("1.2.3.4".IsTrustedProxy(["*"]));
        Assert.False("8.8.8.8".IsTrustedProxy(["10.0.0.0/8", "192.168.*"]));
        Assert.False("8.8.8.8".IsTrustedProxy(null));
        Assert.False("".IsTrustedProxy(["*"]));
    }

    [Fact(DisplayName = "ResolveClientIp 未配置可信代理：兼容旧行为折叠链首")]
    public void ResolveClientIp_NoTrusted()
        {
        Assert.Equal("1.2.3.4", "10.0.0.5".ResolveClientIp("1.2.3.4, 10.0.0.5", null));
        Assert.Equal("10.0.0.5", "10.0.0.5".ResolveClientIp(null, null));
        Assert.Equal("10.0.0.5", "10.0.0.5".ResolveClientIp("", null));
        Assert.Equal("1.2.3.4", "10.0.0.5".ResolveClientIp("1.2.3.4", []));
    }

    [Fact(DisplayName = "ResolveClientIp 配置可信代理：链解析与防伪造")]
    public void ResolveClientIp_Trusted()
    {
        var trusted = new String[] { "10.0.0.0/8" };

        // 直连可信：从右向左跳过可信代理取客户端
        Assert.Equal("1.2.3.4", "10.0.0.1".ResolveClientIp("1.2.3.4, 10.0.0.5", trusted));

        // 直连不可信：忽略转发头防伪造
        Assert.Equal("8.8.8.8", "8.8.8.8".ResolveClientIp("1.2.3.4", trusted));

        // 链全可信：回退链首
        Assert.Equal("1.2.3.4", "10.0.0.1".ResolveClientIp("1.2.3.4, 10.0.0.5", ["10.0.0.0/8", "1.2.3.0/24"]));

        // 无转发头：直连地址
        Assert.Equal("10.0.0.1", "10.0.0.1".ResolveClientIp(null, trusted));
    }
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
