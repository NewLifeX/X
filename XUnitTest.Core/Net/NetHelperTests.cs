using NewLife;
using NewLife.Caching;
using NewLife.Net;
using System;
using System.Net;
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

    #region WOL 魔包
    [Theory(DisplayName = "Wake 魔包字节：FF×6 + MAC×16，兼容三种 MAC 写法")]
    [InlineData("00-11-22-33-44-55")]
    [InlineData("00:11:22:33:44:55")]
    [InlineData("001122334455")]
    public void WakePacket_Bytes(String mac)
    {
        var packet = NetHelper.BuildWakePacket(mac);

        var macBytes = new Byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };
        Assert.Equal(6 + 16 * macBytes.Length, packet.Length);
        for (var i = 0; i < 6; i++)
            Assert.Equal(0xFF, packet[i]);

        for (var r = 0; r < 16; r++)
        {
            for (var i = 0; i < macBytes.Length; i++)
                Assert.Equal(macBytes[i], packet[6 + r * macBytes.Length + i]);
        }
    }

    [Fact(DisplayName = "Wake 空参数直接返回，不产生网络发送")]
    public void Wake_Empty_NoOp()
    {
        NetHelper.Wake();
        NetHelper.Wake([]);
    }
    #endregion

    #region 地址转换边界
    [Theory(DisplayName = "ParseEndPoint 端口/路径/查询/片段/IPv6 边界")]
    [InlineData("127.0.0.1", 0, "127.0.0.1:0")]
    [InlineData("127.0.0.1", 8080, "127.0.0.1:8080")]
    [InlineData("http://127.0.0.1/path?q=1#f", 0, "127.0.0.1:0")]
    [InlineData("127.0.0.1:8080/api?q=1", 0, "127.0.0.1:8080")]
    [InlineData("::1", 80, "[::1]:80")]
    [InlineData("[::1]", 80, "[::1]:80")]
    [InlineData("[::1]:0", 80, "[::1]:0")]
    [InlineData("2001:db8::1", 0, "[2001:db8::1]:0")]
    [InlineData("[::1]:8080/api", 0, "[::1]:8080")]
    public void ParseEndPoint_More(String address, Int32 defaultPort, String expected)
        => Assert.Equal(expected, NetHelper.ParseEndPoint(address, defaultPort) + "");

    [Theory(DisplayName = "ParseEndPoint 畸形与缺省地址返回 null")]
    [InlineData("")]
    [InlineData("[::1]x:80")]
    [InlineData("[::1]:")]
    [InlineData("127.0.0.1:")]
    [InlineData("127.0.0.1:abc")]
    public void ParseEndPoint_Malformed(String address) => Assert.Null(NetHelper.ParseEndPoint(address));

    [Fact(DisplayName = "ParseEndPoint 端口越界当前抛异常（现状记录，非契约）")]
    public void ParseEndPoint_PortOutOfRange()
    {
        // 现状记录：端口仅做 Int32.TryParse，未校验 0~65535，构造 IPEndPoint 时抛 ArgumentOutOfRangeException。
        // 与 XML 注释“格式非法（坏端口…）返回 null”不一致，属已知缺陷，本任务只记录不修复。
        Assert.Throws<ArgumentOutOfRangeException>(() => NetHelper.ParseEndPoint("127.0.0.1:99999"));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetHelper.ParseEndPoint("127.0.0.1:-1"));
    }

    [Fact(DisplayName = "GetRelativeEndPoint 任意地址按远端解析为对应族环回")]
    public void GetRelativeEndPoint_Any()
    {
        var local4 = new IPEndPoint(IPAddress.Any, 1234);
        var rel4 = local4.GetRelativeEndPoint(IPAddress.Loopback);
        Assert.NotNull(rel4);
        Assert.Equal(IPAddress.Loopback, rel4!.Address);
        Assert.Equal(1234, rel4.Port);
        Assert.NotSame(local4, rel4);

        var local6 = new IPEndPoint(IPAddress.IPv6Any, 5678);
        var rel6 = local6.GetRelativeEndPoint(IPAddress.IPv6Loopback);
        Assert.NotNull(rel6);
        Assert.Equal(IPAddress.IPv6Loopback, rel6!.Address);
        Assert.Equal(5678, rel6.Port);

        // 非 Any 地址值不变（实现会新建 IPEndPoint，值相等但非同一实例）
        var concrete = new IPEndPoint(IPAddress.Loopback, 80);
        var same = concrete.GetRelativeEndPoint(IPAddress.Any);
        Assert.Equal(concrete, same);
        Assert.NotSame(concrete, same);

        // 空参数：local 为空返回 null；remote 为空原样返回 local
        IPEndPoint? none = null;
        Assert.Null(none.GetRelativeEndPoint(IPAddress.Loopback));
        Assert.Same(concrete, concrete.GetRelativeEndPoint(null!));
    }

    [Fact(DisplayName = "GetRelativeAddress 任意地址按地址族返回环回或本机地址")]
    public void GetRelativeAddress_Any()
    {
        Assert.Equal(IPAddress.Loopback, IPAddress.Any.GetRelativeAddress(IPAddress.Loopback));
        Assert.Equal(IPAddress.IPv6Loopback, IPAddress.IPv6Any.GetRelativeAddress(IPAddress.IPv6Loopback));

        // IPv6Any + IPv4 远端：按本地地址族返回 IPv6 环回
        Assert.Equal(IPAddress.IPv6Loopback, IPAddress.IPv6Any.GetRelativeAddress(IPAddress.Loopback));

        // 非 Any 原样返回；空地址返回 null
        Assert.Equal(IPAddress.Loopback, IPAddress.Loopback.GetRelativeAddress(IPAddress.Any));
        Assert.Null(((IPAddress)null!).GetRelativeAddress(IPAddress.Any));
    }
    #endregion

    #region 本机 MAC
    [Fact(DisplayName = "GetMacs 返回的物理地址均为 6 字节且格式合法")]
    public void GetMacs_Format()
    {
        var macs = NetHelper.GetMacs().ToArray();
        Assert.All(macs, mac => Assert.Equal(6, mac.Length));

        // 有网卡时格式化为 XX-XX-XX-XX-XX-XX
        foreach (var mac in macs)
            Assert.Matches("^[0-9A-F]{2}(-[0-9A-F]{2}){5}$", mac.ToHex("-"));
    }

    [Fact(DisplayName = "GetMac 单网卡：返回 null 或 6 字节，不抛异常")]
    public void GetMac_Single()
    {
        // 无网卡的 CI 容器允许返回 null
        var mac = NetHelper.GetMac();
        if (mac != null) Assert.Equal(6, mac.Length);
    }

    [Fact(DisplayName = "GetMac(IP) 按 IP 查询：不抛异常")]
    public void GetMac_ByIP()
    {
        // 环回地址没有物理网卡映射：Windows 走 SendARP，Unix 走网卡扫描（现状可能返回 16 字节缓冲）。
        // 结果语义跨平台不一致，这里只断言不抛异常且非空结果不含空数组。
        var mac = IPAddress.Loopback.GetMac();
        if (mac != null) Assert.NotEmpty(mac);
    }
    #endregion
}
