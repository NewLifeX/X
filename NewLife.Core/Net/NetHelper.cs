using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Net;

namespace NewLife;

/// <summary>网络工具类</summary>
public static class NetHelper
{
    #region 属性
    // 私有缓存实例：网络变化时只清自己的 DNS/IP 缓存，不用全局 MemoryCache.Instance。
    // 否则插拔网线/VPN 切换会清空整个应用的业务缓存，引发缓存击穿甚至雪崩
    private static readonly MemoryCache _Cache = new();
    #endregion

    #region 构造
    static NetHelper()
    {
        if (!Runtime.Unity)
        {
            // 网络有变化时，清空所有缓存
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }
    }

    private static void NetworkChange_NetworkAvailabilityChanged(Object? sender, NetworkAvailabilityEventArgs e) => _Cache.Clear();

    private static void NetworkChange_NetworkAddressChanged(Object? sender, EventArgs e) => _Cache.Clear();
    #endregion

    #region 辅助函数
    /// <summary>设置超时检测时间和检测间隔</summary>
    /// <remarks>
    /// 一次对server服务大量积压异常TCP ESTABLISHED链接的排查笔记 https://www.jianshu.com/p/a1c3aba4af96
    /// 查看连接创建时间： sudo ls /proc/128260/fd -l|grep socket ，可发现大量连接的创建时间在很久之前。
    /// 查看连接是否有启用keepalive： ss -aoen|grep ESTAB|grep timer ，带有timer的socket表示启用了keepalive。
    /// </remarks>
    /// <param name="socket">要设置的Socket对象</param>
    /// <param name="isKeepAlive">是否启用Keep-Alive</param>
    /// <param name="startTime">多长时间后开始第一次探测（单位：秒）</param>
    /// <param name="interval">探测时间间隔（单位：秒）</param>
    public static void SetTcpKeepAlive(this Socket socket, Boolean isKeepAlive, Int32 startTime, Int32 interval)
    {
        if (socket == null) return;

#if !NETFRAMEWORK
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#else
        if (Runtime.Windows)
#endif
        {
            UInt32 dummy = 0;
            using var inOptionValues = Pool.Rent(Marshal.SizeOf(dummy) * 3);

            // 是否启用Keep-Alive
            BitConverter.GetBytes((UInt32)(isKeepAlive ? 1 : 0)).CopyTo(inOptionValues, 0);
            // 第一次开始发送探测包时间间隔
            BitConverter.GetBytes((UInt32)startTime * 1000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            // 连续发送探测包时间间隔
            BitConverter.GetBytes((UInt32)interval * 1000).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

            return;
        }

        {
            // 开启keepalive
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, isKeepAlive);
#if NETCOREAPP
            // 开始首次keepalive探测前的TCP空闲时间
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, startTime);
            // 两次keepalive探测之间的时间间隔
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, interval);
#endif
        }
    }

    /// <summary>分析地址，根据IP或者域名得到IP地址，缓存60秒，异步更新</summary>
    /// <param name="hostname"></param>
    /// <returns></returns>
    public static IPAddress? ParseAddress(this String hostname)
    {
        if (hostname.IsNullOrEmpty()) return null;

        var key = $"NetHelper:ParseAddress:{hostname}";
        if (_Cache.TryGetValue<IPAddress>(key, out var address)) return address;

        address = NetUri.ParseAddress(hostname)?.FirstOrDefault();

        _Cache.Set(key, address, 60);

        return address;
    }

    /// <summary>分析网络终结点</summary>
    /// <remarks>
    /// 地址可带协议前缀与路径（自动剥离），可不带端口；支持域名、IPv4、IPv6（含方括号形态）。
    /// 格式非法（坏端口、方括号不闭合等）返回 null；域名解析失败沿用 <see cref="NetUri.ParseAddress"/> 的抛异常口径。
    /// </remarks>
    /// <param name="address">地址，可以不带端口</param>
    /// <param name="defaultPort">地址不带端口时指定的默认端口</param>
    /// <returns></returns>
    public static IPEndPoint? ParseEndPoint(String address, Int32 defaultPort = 0)
    {
        if (String.IsNullOrEmpty(address)) return null;

        var p = address.IndexOf("://");
        if (p >= 0) address = address[(p + 3)..];

        // 去掉路径、查询与片段，只保留主机部分
        p = address.IndexOfAny(['/', '?', '#']);
        if (p >= 0) address = address[..p];

        var port = defaultPort;
        var host = address;

        if (address.StartsWith("["))
        {
            // [IPv6] 或 [IPv6]:port
            var end = address.IndexOf(']');
            if (end < 0) return null;

            host = address[1..end];
            var rest = address[(end + 1)..];
            if (rest.StartsWith(":"))
            {
                if (!Int32.TryParse(rest[1..], out port)) return null;
            }
            else if (!rest.IsNullOrEmpty())
            {
                return null;
            }
        }
        else
        {
            // 冒号唯一才是"主机:端口"；多冒号视为不带端口的裸 IPv6
            var idx = address.IndexOf(':');
            if (idx > 0 && idx == address.LastIndexOf(':'))
            {
                host = address[..idx];
                if (!Int32.TryParse(address[(idx + 1)..], out port)) return null;
            }
        }

        var addr = host.ParseAddress();
        if (addr == null) return null;

        // 端口越界（含默认端口）属于格式非法，构造 IPEndPoint 前拦截，避免抛 ArgumentOutOfRangeException
        if (port is < 0 or > 65535) return null;

        return new IPEndPoint(addr, port);
    }

    /// <summary>针对IPv4和IPv6获取合适的Any地址</summary>
    /// <remarks>除了Any地址以为，其它地址不具备等效性</remarks>
    /// <param name="address"></param>
    /// <param name="family"></param>
    /// <returns></returns>
    public static IPAddress GetRightAny(this IPAddress address, AddressFamily family)
    {
        if (address.AddressFamily == family) return address;

        switch (family)
        {
            case AddressFamily.InterNetwork:
                if (address.Equals(IPAddress.IPv6Any)) return IPAddress.Any;
                break;
            case AddressFamily.InterNetworkV6:
                if (address.Equals(IPAddress.Any)) return IPAddress.IPv6Any;
                break;
            default:
                break;
        }
        //return null;

        //throw new InvalidDataException($"Not Found {family}");

        return address;
    }

    /// <summary>是否Any地址，同时处理IPv4和IPv6</summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static Boolean IsAny(this IPAddress address) => IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address);

    /// <summary>是否Any结点</summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public static Boolean IsAny(this EndPoint endpoint) => endpoint is IPEndPoint ep && (ep.Port == 0 || ep.Address.IsAny());

    /// <summary>是否IPv4地址</summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static Boolean IsIPv4(this IPAddress address) => address.AddressFamily == AddressFamily.InterNetwork;

    /// <summary>是否本地地址</summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static Boolean IsLocal(this IPAddress address) => IPAddress.IsLoopback(address) || GetIPsWithCache().Any(ip => ip.Equals(address));

    /// <summary>是否私有网段IPv4地址。覆盖 10/8、172.16/12、192.168/16、127/8、169.254/16，常用于内网来源判定与可信代理自动学习</summary>
    /// <param name="ip">IPv4地址</param>
    /// <returns>是否私有网段</returns>
    public static Boolean IsPrivateIp(this String? ip)
    {
        var n = ParseIpv4(ip);
        if (n < 0) return false;

        return (n & 0xFF000000L) == 0x0A000000L          // 10.0.0.0/8
            || (n & 0xFFF00000L) == 0xAC100000L          // 172.16.0.0/12
            || (n & 0xFFFF0000L) == 0xC0A80000L          // 192.168.0.0/16
            || (n & 0xFF000000L) == 0x7F000000L          // 127.0.0.0/8
            || (n & 0xFFFF0000L) == 0xA9FE0000L;         // 169.254.0.0/16
    }

    /// <summary>IPv4 CIDR网段匹配</summary>
    /// <param name="ip">IPv4地址</param>
    /// <param name="cidr">网段，如 10.0.0.0/8</param>
    /// <returns>是否属于该网段</returns>
    public static Boolean IsMatchCidr(this String? ip, String? cidr)
    {
        if (ip.IsNullOrEmpty() || cidr.IsNullOrEmpty()) return false;

        var ss = cidr.Split('/');
        if (ss.Length != 2) return false;

        var mask = ss[1].ToInt();
        if (mask is < 0 or > 32) return false;

        var ip1 = ParseIpv4(ip);
        var ip2 = ParseIpv4(ss[0]);
        if (ip1 < 0 || ip2 < 0) return false;

        if (mask == 0) return true;

        var bits = (~((1L << (32 - mask)) - 1)) & 0xFFFFFFFFL;
        return (ip1 & bits) == (ip2 & bits);
    }

    /// <summary>是否匹配可信代理列表。列表项支持精确地址、*通配和IPv4 CIDR网段</summary>
    /// <param name="ip">待判断地址</param>
    /// <param name="trusted">可信代理列表</param>
    /// <returns>是否可信</returns>
    public static Boolean IsTrustedProxy(this String? ip, String?[]? trusted)
    {
        if (ip.IsNullOrEmpty() || trusted == null || trusted.Length == 0) return false;

        foreach (var item in trusted)
        {
            var t = item?.Trim();
            if (t.IsNullOrEmpty()) continue;

            if (t == "*") return true;

            if (t.Contains('/'))
            {
                if (ip!.IsMatchCidr(t)) return true;
            }
            else if (t.IsMatch(ip))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>解析转发链中的客户端地址。未配置可信代理时兼容旧行为信任全部转发头并取链首；配置可信代理后，仅当直连来源可信才采信转发链，从链尾向左跳过可信代理取第一个不可信地址</summary>
    /// <param name="remoteIp">直连来源地址。TCP层地址，不可伪造</param>
    /// <param name="forwarded">转发头链值，逗号分隔，如 "1.2.3.4, 10.0.0.5"</param>
    /// <param name="trusted">可信代理列表，支持精确、通配与CIDR；为空时信任全部转发头</param>
    /// <returns>客户端地址</returns>
    public static String? ResolveClientIp(this String? remoteIp, String? forwarded, String?[]? trusted)
    {
        if (forwarded.IsNullOrEmpty()) return remoteIp;

        // 未配置可信代理，兼容旧行为信任全部转发头；多层反代时取链首地址，并折叠为单一IP
        if (trusted == null || trusted.Length == 0)
        {
            var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).FirstOrDefault(e => e.Length > 0);
            return first.IsNullOrEmpty() ? remoteIp : first;
        }

        // 直连来源不可信时，转发头可被伪造，直接使用直连地址
        if (remoteIp.IsNullOrEmpty() || !remoteIp!.IsTrustedProxy(trusted)) return remoteIp;

        // 从链尾向左跳过可信代理，取第一个不可信地址作为客户端地址
        var ips = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
        for (var i = ips.Count - 1; i >= 0; i--)
        {
            if (!ips[i].IsTrustedProxy(trusted)) return ips[i];
        }

        return ips.Count > 0 ? ips[0] : remoteIp;
    }

    /// <summary>解析IPv4地址为整数，非法返回-1</summary>
    /// <param name="ip">IPv4地址</param>
    /// <returns>整型地址</returns>
    private static Int64 ParseIpv4(String? ip)
    {
        if (ip.IsNullOrEmpty()) return -1;

        var ss = ip.Split('.');
        if (ss.Length != 4) return -1;

        var n = 0L;
        foreach (var item in ss)
        {
            var b = item.ToInt();
            if (b is < 0 or > 255) return -1;

            n = (n << 8) | (UInt32)b;
        }

        return n;
    }

    /// <summary>获取相对于指定远程地址的本地地址</summary>
    /// <param name="address"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    public static IPAddress? GetRelativeAddress(this IPAddress address, IPAddress remote)
    {
        // 如果不是任意地址，直接返回
        var addr = address;
        if (addr == null || !addr.IsAny()) return addr;

        // 如果是本地环回地址，返回环回地址
        if (IPAddress.IsLoopback(remote)) return addr.IsIPv4() ? IPAddress.Loopback : IPAddress.IPv6Loopback;

        // 否则返回本地第一个IP地址
        foreach (var item in GetIPsWithCache())
        {
            if (item.AddressFamily == addr.AddressFamily) return item;
        }

        return null;
    }

    /// <summary>获取相对于指定远程地址的本地地址</summary>
    /// <param name="local"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    public static IPEndPoint? GetRelativeEndPoint(this IPEndPoint local, IPAddress remote)
    {
        if (local == null || remote == null) return local;

        var addr = local.Address.GetRelativeAddress(remote);
        return addr == null ? local : new IPEndPoint(addr, local.Port);
    }

    /// <summary>指定地址的指定端口是否已被使用，似乎没办法判断IPv6地址</summary>
    /// <param name="protocol"></param>
    /// <param name="address"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public static Boolean CheckPort(this IPAddress address, NetType protocol, Int32 port)
    {
        //if (NewLife.Runtime.Mono) return false;
        if (!Runtime.Windows) return false;

        try
        {
            // 某些情况下检查端口占用会抛出异常，原因未知
            var gp = IPGlobalProperties.GetIPGlobalProperties();

            IPEndPoint[]? eps = null;
            switch (protocol)
            {
                case NetType.Tcp:
                    eps = gp.GetActiveTcpListeners();
                    break;
                case NetType.Udp:
                    eps = gp.GetActiveUdpListeners();
                    break;
                default:
                    return false;
            }

            foreach (var item in eps)
                // 先比较端口，性能更好
                if (item.Port == port && item.Address.Equals(address)) return true;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        return false;
    }

    /// <summary>检查该协议的地址端口是否已经被使用</summary>
    /// <remarks>Unix域套接字不使用端口，由服务器启动时实测探测</remarks>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static Boolean CheckPort(this NetUri uri) => !uri.IsUnix && uri.Address.CheckPort(uri.Type, uri.Port);

    /// <summary>获取所有Tcp连接（无参重载，仅为兼容旧二进制保留）</summary>
    /// <returns></returns>
    [Obsolete("请改用 GetAllTcpConnections(processId)")]
    public static TcpConnectionInformation2[] GetAllTcpConnections() => GetAllTcpConnections(-1);

    /// <summary>获取所有Tcp连接，带进程Id</summary>
    /// <remarks>Windows 走扩展Tcp表，Linux 解析 /proc/net/tcp；macOS 等其它平台暂无实现，返回空数组</remarks>
    /// <returns>连接数组；不支持的平台返回空数组</returns>
    public static TcpConnectionInformation2[] GetAllTcpConnections(Int32 processId = -1)
    {
        // Linux 之外的类 Unix 平台（macOS 等）没有 /proc，不能走 GetLinuxTcpConnections，否则读文件直接抛异常
        var rs = Runtime.Windows ?
            TcpConnectionInformation2.GetWindowsTcpConnections() :
            Runtime.Linux ? TcpConnectionInformation2.GetLinuxTcpConnections(processId) : [];

        if (processId <= 0) return rs;

        return rs.Where(e => e.ProcessId == processId).ToArray();
    }
    #endregion

    #region 本机信息
    /// <summary>获取活动的接口信息</summary>
    /// <returns></returns>
    public static IEnumerable<IPInterfaceProperties> GetActiveInterfaces()
    {
        foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.OperationalStatus != OperationalStatus.Up) continue;
            if (item.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel or NetworkInterfaceType.Unknown) continue;

            var ip = item.GetIPProperties();
            if (ip != null) yield return ip;
        }
    }

    /// <summary>获取可用的DHCP地址</summary>
    /// <returns></returns>
    public static IEnumerable<IPAddress> GetDhcps()
    {
        var list = new List<IPAddress>();
        foreach (var item in GetActiveInterfaces())
        {
#if NET5_0_OR_GREATER
            if (item != null && !OperatingSystem.IsMacOS() && item.DhcpServerAddresses.Count > 0)
            {
                foreach (var elm in item.DhcpServerAddresses)
                {
                    if (list.Contains(elm)) continue;
                    list.Add(elm);

                    yield return elm;
                }
            }
#else
            if (item != null && item.DhcpServerAddresses.Count > 0)
            {
                foreach (var elm in item.DhcpServerAddresses)
                {
                    if (list.Contains(elm)) continue;
                    list.Add(elm);

                    yield return elm;
                }
            }
#endif
        }
    }

    /// <summary>获取可用的DNS地址</summary>
    /// <returns></returns>
    public static IEnumerable<IPAddress> GetDns()
    {
        var list = new List<IPAddress>();
        foreach (var item in GetActiveInterfaces())
        {
            if (item != null && item.DnsAddresses.Count > 0)
            {
                foreach (var elm in item.DnsAddresses)
                {
                    if (list.Contains(elm)) continue;
                    list.Add(elm);

                    yield return elm;
                }
            }
        }
    }

    /// <summary>获取可用的网关地址</summary>
    /// <returns></returns>
    public static IEnumerable<IPAddress> GetGateways()
    {
        var list = new List<IPAddress>();
        foreach (var item in GetActiveInterfaces())
        {
            if (item != null && item.GatewayAddresses.Count > 0)
            {
                foreach (var elm in item.GatewayAddresses)
                {
                    if (list.Contains(elm.Address)) continue;
                    list.Add(elm.Address);

                    yield return elm.Address;
                }
            }
        }
    }

    /// <summary>获取可用的IP地址</summary>
    /// <returns></returns>
    public static IEnumerable<IPAddress> GetIPs()
    {
        var dic = new Dictionary<UnicastIPAddressInformation, Int32>();
        foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.OperationalStatus != OperationalStatus.Up) continue;
            if (item.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel or NetworkInterfaceType.Unknown) continue;

            var ipp = item.GetIPProperties();
            if (ipp != null && ipp.UnicastAddresses.Count > 0)
            {
                var gw = 0;

#if NET5_0_OR_GREATER
                if (!OperatingSystem.IsAndroid())
                {
                    gw = ipp.GatewayAddresses.Count;
                }
#else
                gw = ipp.GatewayAddresses.Count;
#endif

                // 引入权重因子，优先返回网关所在网卡的地址，优先IPv4，IPv6优先公网单播地址
                foreach (var elm in ipp.UnicastAddresses)
                {
                    var factor = gw * 10 + 5;
                    var addr = elm.Address;
                    if (addr.IsIPv4())
                    {
                        factor++;
                        if (addr.GetAddressBytes()[0] == 169) factor--;
                    }
                    else
                    {

                        if (addr.IsIPv4MappedToIPv6) continue;
                        if (addr.IsIPv6LinkLocal) factor--;
                        if (addr.IsIPv6Multicast) continue;
                        if (addr.IsIPv6SiteLocal) continue;
                        //if (addr.IsIPv6Teredo) continue;
#if NET6_0_OR_GREATER
                        if (addr.IsIPv6UniqueLocal) factor -= 2;
#endif
                    }

#if NET5_0_OR_GREATER
                    try
                    {
                        if (OperatingSystem.IsWindows() &&
                            elm.DuplicateAddressDetectionState != DuplicateAddressDetectionState.Preferred)
                            continue;
                    }
                    catch { }
#endif

                    dic.Add(elm, factor);
                }
            }
        }

        // 带网关的接口地址很重要，优先返回
        // Linux下不支持PrefixOrigin
        //var ips = dic.OrderByDescending(e => e.Value)
        //    //.ThenByDescending(e => e.Key.PrefixOrigin == PrefixOrigin.Dhcp || e.Key.PrefixOrigin == PrefixOrigin.Manual)
        //    .Select(e => e.Key.Address).ToList();

        // 去除重复IP地址
        var ips = new List<IPAddress>();
        var hash = new List<String>();
        foreach (var item in dic.OrderByDescending(e => e.Value))
        {
            var address = item.Key.Address + "";
            if (!hash.Contains(address))
            {
                ips.Add(item.Key.Address);
                hash.Add(address);
            }
        }

        return ips;
    }

    /// <summary>获取本机可用IP地址，缓存60秒，异步更新</summary>
    /// <returns></returns>
    public static IPAddress[] GetIPsWithCache()
    {
        var key = $"NetHelper:GetIPsWithCache";
        if (_Cache.TryGetValue<IPAddress[]>(key, out var addrs) && addrs != null) return addrs;

        addrs = GetIPs().ToArray();

        _Cache.Set(key, addrs, 60);

        return addrs;
    }

    /// <summary>获取可用的多播地址</summary>
    /// <returns></returns>
    public static IEnumerable<IPAddress> GetMulticasts()
    {
        var list = new List<IPAddress>();
        foreach (var item in GetActiveInterfaces())
        {
            if (item != null && item.MulticastAddresses.Count > 0)
            {
                foreach (var elm in item.MulticastAddresses)
                {
                    if (list.Contains(elm.Address)) continue;
                    list.Add(elm.Address);

                    yield return elm.Address;
                }
            }
        }
    }

    private static readonly String[] _Excludes = ["Loopback", "VMware", "VBox", "Virtual", "Teredo", "Tunnel", "VPN", "VNIC", "IEEE", "Filter", "Npcap", "QoS", "Miniport", "Kernel Debug"];
    /// <summary>获取所有物理网卡MAC地址。包括未启用网卡，剔除本地和隧道</summary>
    /// <returns></returns>
    public static IEnumerable<Byte[]> GetMacs()
    {
        foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
        {
            // 只要物理网卡
            if (item.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel or NetworkInterfaceType.Unknown) continue;
            if (_Excludes.Any(e => item.Description.Contains(e))) continue;
            if (Runtime.Windows && item.Speed < 1_000_000) continue;

            // 物理网卡在禁用时没有IP，如果有IP，则不能是环回
            var ips = item.GetIPProperties();
            var addrs = ips.UnicastAddresses
                .Where(e => e.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(e => e.Address)
                .ToArray();
            if (addrs.Length > 0 && addrs.All(e => IPAddress.IsLoopback(e))) continue;

            var mac = item.GetPhysicalAddress()?.GetAddressBytes();
            if (mac != null && mac.Length == 6) yield return mac;
        }
    }

    /// <summary>获取网卡MAC地址（网关所在网卡）</summary>
    /// <returns></returns>
    public static Byte[]? GetMac()
    {
        foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (_Excludes.Any(e => item.Description.Contains(e))) continue;
            if (Runtime.Windows && item.Speed < 1_000_000) continue;

            var ips = item.GetIPProperties();
            var addrs = ips.UnicastAddresses
                .Where(e => e.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(e => e.Address)
                .ToArray();
            if (addrs.All(e => IPAddress.IsLoopback(e))) continue;

            // 网关
            addrs = ips.GatewayAddresses
                .Where(e => e.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(e => e.Address)
                .ToArray();
            if (addrs.Length == 0) continue;

            var mac = item.GetPhysicalAddress()?.GetAddressBytes();
            if (mac != null && mac.Length == 6) return mac;
        }

        return null;
    }

    /// <summary>获取本地第一个IPv4地址。一般是网关所在网卡的IP地址</summary>
    /// <returns></returns>
    public static IPAddress? MyIP() => GetIPsWithCache().FirstOrDefault(ip => ip.IsIPv4() && !IPAddress.IsLoopback(ip) && ip.GetAddressBytes()[0] != 169);

    /// <summary>获取本地第一个IPv6地址</summary>
    /// <returns></returns>
    public static IPAddress? MyIPv6() => GetIPsWithCache().FirstOrDefault(ip => !ip.IsIPv4() && !IPAddress.IsLoopback(ip));
    #endregion

    #region 远程开机
    /// <summary>唤醒指定MAC地址的计算机</summary>
    /// <param name="macs"></param>
    public static void Wake(params String[] macs)
    {
        if (macs == null || macs.Length <= 0) return;

        foreach (var item in macs)
            Wake(item);
    }

    private static void Wake(String mac)
    {
        var bts = BuildWakePacket(mac);

        using var client = new UdpClient
        {
            EnableBroadcast = true
        };
        client.Send(bts, bts.Length, new IPEndPoint(IPAddress.Broadcast, 7));
        //client.SendAsync(bts, bts.Length, new IPEndPoint(IPAddress.Broadcast, 7));
    }

    /// <summary>构造网络唤醒魔包。6 字节 FF + 16 次重复 MAC</summary>
    /// <param name="mac">MAC 地址，支持横线或冒号分隔</param>
    /// <returns>魔包字节</returns>
    internal static Byte[] BuildWakePacket(String mac)
    {
        mac = mac.Replace("-", null).Replace(":", null);
        using var buffer = Pool.Rent(mac.Length / 2);
        var bts = new Byte[6 + 16 * buffer.Length];

        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = Byte.Parse(mac.Substring(i * 2, 2), NumberStyles.HexNumber);

        for (var i = 0; i < 6; i++)
            bts[i] = 0xFF;
        for (Int32 i = 6, k = 0; i < bts.Length; i++, k++)
        {
            if (k >= buffer.Length) k = 0;

            bts[i] = buffer[k];
        }

        return bts;
    }
    #endregion

    #region MAC获取/ARP协议
    [DllImport("Iphlpapi.dll")]
    private static extern Int32 SendARP(UInt32 destip, UInt32 srcip, Byte[] mac, ref Int32 length);

    /// <summary>根据IP地址获取MAC地址</summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public static Byte[]? GetMac(this IPAddress ip)
    {
        // 考虑到IPv6是16字节，不确定SendARP是否支持IPv6
        var len = 16;
        var buf = new Byte[16];

        if (Runtime.Windows)
        {
            var rs = SendARP(ip.GetAddressBytes().ToUInt32(), 0, buf, ref len);
            if (rs != 0 || len <= 0) return null;
            if (len != buf.Length) buf = buf.ReadBytes(0, len);
        }
        else
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var item in networkInterfaces)
            {
                if (_Excludes.Any(e => item.Description.Contains(e))) continue;
                if (Runtime.Windows && item.Speed < 1_000_000) continue;

                var ips = item.GetIPProperties();
                var addrs = ips.UnicastAddresses
                    .Where(e => e.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(e => e.Address)
                    .ToArray();
                if (addrs.All(e => IPAddress.IsLoopback(e))) continue;

                foreach (var ipInfo in ips.UnicastAddresses)
                {
                    if (!ipInfo.Address.Equals(ip)) continue;
                    buf = item.GetPhysicalAddress()?.GetAddressBytes();
                }

                if (buf != null && buf.Length == 6) return buf;
            }
        }

        return buf;
    }
    #endregion

    #region IP地理位置
    /// <summary>IP地址提供者</summary>
    public static IIPResolver? IpResolver { get; set; }

    /// <summary>获取IP地址的物理地址位置</summary>
    /// <param name="addr"></param>
    /// <returns></returns>
    public static String? GetAddress(this IPAddress addr)
    {
        if (addr.IsAny()) return L.T("Any address");
        if (IPAddress.IsLoopback(addr)) return L.T("Loopback address");
        if (addr.IsLocal()) return L.T("Local address");

        //if (IpProvider == null) IpProvider = new MyIpProvider();

        return IpResolver?.GetAddress(addr);
    }

    /// <summary>根据字符串形式IP地址转为物理地址</summary>
    /// <param name="addr"></param>
    /// <returns></returns>
    public static String IPToAddress(this String addr)
    {
        if (addr.IsNullOrEmpty()) return String.Empty;

        // 有可能是NetUri
        var p = addr.IndexOf("://");
        if (p >= 0) addr = addr[(p + 3)..];

        // 有可能是多个IP地址
        p = addr.IndexOf(',');
        if (p >= 0) addr = addr.Split(',').First();

        // 过滤IPv4/IPv6端口
        if (addr.Replace("::", "").Contains(':')) addr = addr[..addr.LastIndexOf(':')];

        return !IPAddress.TryParse(addr, out var ip) ? String.Empty : (ip.GetAddress() ?? String.Empty);
    }
    #endregion

    #region 创建客户端和会话
    /// <summary>根据本地网络标识创建客户端</summary>
    /// <param name="local"></param>
    /// <returns></returns>
    public static ISocketClient CreateClient(this NetUri local)
    {
        return local == null
            ? throw new ArgumentNullException(nameof(local))
            : local.Type switch
            {
                NetType.Tcp => new TcpSession { Local = local },
                NetType.Udp => new UdpServer { Local = local },
                NetType.Unix => new TcpSession { Local = local },
                _ => throw new NotSupportedException(L.T("The {0} protocol is not supported", local.Type)),
            };
    }

    /// <summary>根据远程网络标识创建客户端</summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    /// <remarks>启用 TLS 由地址语义（<see cref="NetUri.IsSecure"/> 或 <see cref="NetType.Https"/>）决定，不按端口号猜测</remarks>
    public static ISocketClient CreateRemote(this NetUri remote)
    {
        return remote == null
            ? throw new ArgumentNullException(nameof(remote))
            : remote.Type switch
            {
                NetType.Tcp => new TcpSession { Remote = remote },
                NetType.Udp => new UdpServer { Remote = remote },
                NetType.Unix => new TcpSession { Remote = remote },
                NetType.Http or NetType.Https => new TcpSession { Remote = remote, SslProtocol = remote.IsSecure || remote.Type == NetType.Https ? DefaultSslProtocols : SslProtocols.None },
                NetType.WebSocket => new WebSocketClient { Remote = remote, SslProtocol = remote.IsSecure ? DefaultSslProtocols : SslProtocols.None },
                _ => throw new NotSupportedException(L.T("The {0} protocol is not supported", remote.Type)),
            };
    }

    /// <summary>根据Uri创建客户端，主要支持Http/WebSocket</summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    /// <remarks>ws 为明文，wss 由 <see cref="WebSocketClient"/> 依据地址方案自动启用 TLS</remarks>
    /// <exception cref="NotSupportedException"></exception>
    public static ISocketClient CreateRemote(this Uri uri)
    {
        return uri.Scheme is "ws" or "wss"
            ? new WebSocketClient(uri)
            : throw new NotSupportedException(L.T("The {0} protocol is not supported", uri.Scheme));
    }

    /// <summary>启用 TLS 时使用的协议集合。固定 Tls12 会丢失 TLS1.3 与后续版本，现代框架交给运行时协商，旧框架仅 Tls12</summary>
    internal static readonly SslProtocols DefaultSslProtocols =
#if NETFRAMEWORK || NETSTANDARD
        SslProtocols.Tls12;
#else
        SslProtocols.Tls12 | SslProtocols.Tls13;
#endif

    internal static Socket CreateTcp(Boolean ipv4 = true) => new(ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

    internal static Socket CreateUdp(Boolean ipv4 = true) => new(ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

    /// <summary>创建Unix域套接字</summary>
    /// <remarks>使用文件系统路径作为地址，仅支持本机进程间通信</remarks>
    /// <returns></returns>
    internal static Socket CreateUnix()
    {
#if NETFRAMEWORK || NETSTANDARD2_0
        throw new PlatformNotSupportedException(L.T("Unix domain sockets require a target framework of .NET Standard 2.1 or later."));
#else
        return new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
#endif
    }

    /// <summary>创建已绑定本地随机端口的 UdpClient，可安全地在并发任务中同时执行 SendAsync 和 ReceiveAsync</summary>
    /// <remarks>
    /// <para>直接 new UdpClient() 不绑定本地端口。首次 SendAsync/WSASendTo 时操作系统在内核层隐式分配临时端口，
    /// 但 .NET Socket 内部的 _isBound 字段不会更新（该字段仅由托管 Socket.Bind() 写入）。</para>
    /// <para>若并发任务中 SendAsync 先于 ReceiveAsync 执行，ReceiveAsync 内部检测到 _isBound=false，
    /// 随即尝试调用 Bind(0.0.0.0:0)，而内核已绑定该套接字，导致 SocketException WSAEINVAL(10022)，
    /// 接收循环异常退出，后续数据无法收取。</para>
    /// <para>本方法在构造后立即调用 Bind(Any:0)，使 _isBound=true，彻底规避上述竞态。</para>
    /// </remarks>
    /// <param name="ipv4">是否使用 IPv4，默认 true</param>
    /// <returns>已绑定本地随机端口的 UdpClient</returns>
    public static UdpClient CreateUdpClient(Boolean ipv4 = true)
    {
        var udp = new UdpClient(ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6);
        udp.Client.Bind(new IPEndPoint(ipv4 ? IPAddress.Any : IPAddress.IPv6Any, 0));
        return udp;
    }
    #endregion
}