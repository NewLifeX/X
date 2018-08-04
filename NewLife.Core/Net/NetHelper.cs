using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
#if !__MOBILE__ && !__CORE__
using Microsoft.Win32;
using System.Management;
using System.Security.AccessControl;
#endif

namespace System
{
    /// <summary>网络工具类</summary>
    public static class NetHelper
    {
        #region 辅助函数
        /// <summary>设置超时检测时间和检测间隔</summary>
        /// <param name="socket">要设置的Socket对象</param>
        /// <param name="iskeepalive">是否启用Keep-Alive</param>
        /// <param name="starttime">多长时间后开始第一次探测（单位：毫秒）</param>
        /// <param name="interval">探测时间间隔（单位：毫秒）</param>
        public static void SetTcpKeepAlive(this Socket socket, Boolean iskeepalive, Int32 starttime = 10000, Int32 interval = 10000)
        {
            if (socket == null || !socket.Connected) return;
            UInt32 dummy = 0;
            var inOptionValues = new Byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((UInt32)(iskeepalive ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((UInt32)starttime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((UInt32)interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        private static DictionaryCache<String, IPAddress> _dnsCache = new DictionaryCache<String, IPAddress>(NetUri.ParseAddress, StringComparer.OrdinalIgnoreCase) { Expire = 60 };
        /// <summary>分析地址，根据IP或者域名得到IP地址，缓存60秒，异步更新</summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IPAddress ParseAddress(this String hostname)
        {
            if (hostname.IsNullOrEmpty()) return null;

            //return _dnsCache.GetItem(hostname, NetUri.ParseAddress);
            return _dnsCache[hostname];
        }

        /// <summary>分析网络终结点</summary>
        /// <param name="address">地址，可以不带端口</param>
        /// <param name="defaultPort">地址不带端口时指定的默认端口</param>
        /// <returns></returns>
        public static IPEndPoint ParseEndPoint(String address, Int32 defaultPort = 0)
        {
            if (String.IsNullOrEmpty(address)) return null;

            var p = address.IndexOf("://");
            if (p >= 0) address = address.Substring(p + 3);

            p = address.IndexOf(":");
            if (p > 0)
                return new IPEndPoint(ParseAddress(address.Substring(0, p)), Int32.Parse(address.Substring(p + 1)));
            else
                return new IPEndPoint(ParseAddress(address), defaultPort);
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
                    if (address == IPAddress.IPv6Any) return IPAddress.Any;
                    break;
                case AddressFamily.InterNetworkV6:
                    if (address == IPAddress.Any) return IPAddress.IPv6Any;
                    break;
                default:
                    break;
            }
            return null;
        }

        /// <summary>是否Any地址，同时处理IPv4和IPv6</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsAny(this IPAddress address) => IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address);

        /// <summary>是否Any结点</summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static Boolean IsAny(this EndPoint endpoint) => (endpoint as IPEndPoint).Address.IsAny() || (endpoint as IPEndPoint).Port == 0;

        /// <summary>是否IPv4地址</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsIPv4(this IPAddress address) => address.AddressFamily == AddressFamily.InterNetwork;

        /// <summary>是否本地地址</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsLocal(this IPAddress address) => IPAddress.IsLoopback(address) || GetIPsWithCache().Any(ip => ip.Equals(address));

        /// <summary>获取相对于指定远程地址的本地地址</summary>
        /// <param name="address"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public static IPAddress GetRelativeAddress(this IPAddress address, IPAddress remote)
        {
            // 如果不是任意地址，直接返回
            var addr = address;
            if (addr == null || !addr.IsAny()) return addr;

            // 如果是本地环回地址，返回环回地址
            if (IPAddress.IsLoopback(remote)) return addr.IsIPv4() ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            // 否则返回本地第一个IP地址
            foreach (var item in NetHelper.GetIPsWithCache())
            {
                if (item.AddressFamily == addr.AddressFamily) return item;
            }
            return null;
        }

        /// <summary>获取相对于指定远程地址的本地地址</summary>
        /// <param name="local"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public static IPEndPoint GetRelativeEndPoint(this IPEndPoint local, IPAddress remote)
        {
            if (local == null || remote == null) return local;

            var addr = GetRelativeAddress(local.Address, remote);
            return addr == null ? local : new IPEndPoint(addr, local.Port);
        }

        /// <summary>指定地址的指定端口是否已被使用，似乎没办法判断IPv6地址</summary>
        /// <param name="protocol"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Boolean CheckPort(this IPAddress address, NetType protocol, Int32 port)
        {
            if (NewLife.Runtime.Mono) return false;

            try
            {
                // 某些情况下检查端口占用会抛出异常，原因未知
                var gp = IPGlobalProperties.GetIPGlobalProperties();

                IPEndPoint[] eps = null;
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
                {
                    // 先比较端口，性能更好
                    if (item.Port == port && item.Address.Equals(address)) return true;
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            return false;
        }

        /// <summary>检查该协议的地址端口是否已经呗使用</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Boolean CheckPort(this NetUri uri) => CheckPort(uri.Address, uri.Type, uri.Port);
        #endregion

        #region 本机信息
        /// <summary>获取活动的接口信息</summary>
        /// <returns></returns>
        public static IEnumerable<IPInterfaceProperties> GetActiveInterfaces()
        {
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.OperationalStatus != OperationalStatus.Up) continue;

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
                if (item != null && item.DhcpServerAddresses.Count > 0)
                {
                    foreach (var elm in item.DhcpServerAddresses)
                    {
                        if (list.Contains(elm)) continue;
                        list.Add(elm);

                        yield return elm;
                    }
                }
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
#if __ANDROID__
            return Dns.GetHostAddresses(Dns.GetHostName());
#endif
#if __IOS__
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                {
                    if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        yield return addrInfo.Address;
                    }
                }
            }
#endif
#if !__MOBILE__

            var dic = new Dictionary<UnicastIPAddressInformation, Int32>();
            foreach (var item in GetActiveInterfaces())
            {
                if (item != null && item.UnicastAddresses.Count > 0)
                {
                    var gw = item.GatewayAddresses.Count;
                    foreach (var elm in item.UnicastAddresses)
                    {
                        try
                        {
                            if (elm.DuplicateAddressDetectionState != DuplicateAddressDetectionState.Preferred) continue;
                        }
                        catch { }

                        dic.Add(elm, gw);
                    }
                }
            }

            // 带网关的接口地址很重要，优先返回
            // Linux下不支持PrefixOrigin
            var ips = dic.OrderByDescending(e => e.Value)
                //.ThenByDescending(e => e.Key.PrefixOrigin == PrefixOrigin.Dhcp || e.Key.PrefixOrigin == PrefixOrigin.Manual)
                .Select(e => e.Key.Address).ToList();

            return ips;
#endif
        }

        private static DictionaryCache<Int32, IPAddress[]> _ips = new DictionaryCache<Int32, IPAddress[]> { Expire = 60/*, Asynchronous = true*/ };
        /// <summary>获取本机可用IP地址，缓存60秒，异步更新</summary>
        /// <returns></returns>
        public static IPAddress[] GetIPsWithCache()
        {
            //return _ips.GetItem(1, k => GetIPs().ToArray());
            if (_ips.FindMethod == null) _ips.FindMethod = k => GetIPs().ToArray();
            return _ips[1];
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

        /// <summary>获取以太网MAC地址</summary>
        /// <returns></returns>
        public static IEnumerable<Byte[]> GetMacs()
        {
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue;

                var mac = item.GetPhysicalAddress();
                if (mac != null) yield return mac.GetAddressBytes();
            }
        }

        /// <summary>获取本地第一个IPv4地址</summary>
        /// <returns></returns>
        public static IPAddress MyIP() => GetIPsWithCache().FirstOrDefault(ip => ip.IsIPv4() && !IPAddress.IsLoopback(ip) && ip.GetAddressBytes()[0] != 169);

        /// <summary>获取本地第一个IPv6地址</summary>
        /// <returns></returns>
        public static IPAddress MyIPv6() => GetIPsWithCache().FirstOrDefault(ip => !ip.IsIPv4() && !IPAddress.IsLoopback(ip));
        #endregion

        #region 设置适配器信息
#if !__MOBILE__ && !__CORE__
        static private ManagementObjectCollection GetInstances()
        {
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            return mc.GetInstances();
        }

        /// <summary>设置IP，默认掩码255.255.255.0</summary>
        /// <param name="ip"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static Boolean SetIP(String ip, String mask = "255.255.255.0")
        {
            var moc = GetInstances();
            if (moc == null) return false;

            foreach (ManagementObject mo in moc)
            {
                if (!(Boolean)mo["IPEnabled"]) continue;

                // 设置IP和掩码
                var inPar = mo.GetMethodParameters("EnableStatic");
                inPar["IPAddress"] = new String[] { ip };
                inPar["SubnetMask"] = new String[] { mask };
                var outPar = mo.InvokeMethod("EnableStatic", inPar, null);
            }

            var ips = GetIPs().ToList();
            var pp = ips.Find(e => e.ToString() == ip);
            return pp != null;
        }

        /// <summary>设置默认网关</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        static public Boolean SetGateway(String address)
        {
            var moc = GetInstances();
            if (moc == null) return false;

            foreach (ManagementObject mo in moc)
            {
                if (!(Boolean)mo["IPEnabled"]) continue;

                // 设置网关 
                var inPar = mo.GetMethodParameters("SetGateways");
                inPar["DefaultIPGateway"] = new String[] { address };
                var outPar = mo.InvokeMethod("SetGateways", inPar, null);
            }

            var ips = GetGateways().ToList();
            var pp = ips.Find(e => e.ToString() == address);
            return pp != null;
        }

        /// <summary>启动DHCP</summary>
        static public void StartDHCP()
        {
            var moc = GetInstances();
            if (moc == null) return;

            foreach (ManagementObject mo in moc)
            {
                if (!(Boolean)mo["IPEnabled"]) continue;

                mo.InvokeMethod("SetDNSServerSearchOrder", null);
                // 开启DHCP
                mo.InvokeMethod("EnableDHCP", null);
            }
        }
#endif
        #endregion

        #region 远程开机
        /// <summary>唤醒指定MAC地址的计算机</summary>
        /// <param name="macs"></param>
        public static void Wake(params String[] macs)
        {
            if (macs == null || macs.Length < 1) return;

            foreach (var item in macs)
            {
                Wake(item);
            }
        }

        static void Wake(String mac)
        {
            mac = mac.Replace("-", null).Replace(":", null);
            var buffer = new Byte[mac.Length / 2];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Byte.Parse(mac.Substring(i * 2, 2), NumberStyles.HexNumber);
            }

            var bts = new Byte[6 + 16 * buffer.Length];
            for (var i = 0; i < 6; i++)
            {
                bts[i] = 0xFF;
            }
            for (Int32 i = 6, k = 0; i < bts.Length; i++, k++)
            {
                if (k >= buffer.Length) k = 0;

                bts[i] = buffer[k];
            }

            var client = new UdpClient
            {
                EnableBroadcast = true
            };
            client.Send(bts, bts.Length, new IPEndPoint(IPAddress.Broadcast, 7));
            client.Close();
            //client.SendAsync(bts, bts.Length, new IPEndPoint(IPAddress.Broadcast, 7));
        }
        #endregion

        #region MAC获取/ARP协议
        [DllImport("Iphlpapi.dll")]
        private static extern Int32 SendARP(UInt32 destip, UInt32 srcip, Byte[] mac, ref Int32 length);

        /// <summary>根据IP地址获取MAC地址</summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static Byte[] GetMac(this IPAddress ip)
        {
            // 考虑到IPv6是16字节，不确定SendARP是否支持IPv6
            var len = 16;
            var buf = new Byte[16];
            var rs = SendARP(ip.GetAddressBytes().ToUInt32(), 0, buf, ref len);
            if (rs != 0 || len <= 0) return null;

            if (len != buf.Length) buf = buf.ReadBytes(0, len);
            return buf;
        }
        #endregion

        #region IP地理位置
        static IPProvider _IpProvider;
        /// <summary>获取IP地址的物理地址位置</summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static String GetAddress(this IPAddress addr)
        {
            if (addr.IsAny())
                return "任意地址";
            else if (IPAddress.IsLoopback(addr))
                return "本地环回地址";
            else if (addr.IsLocal())
                return "本机地址";

            if (_IpProvider == null) _IpProvider = ObjectContainer.Current.AutoRegister<IPProvider, IpProviderDefault>().Resolve<IPProvider>();

            return _IpProvider.GetAddress(addr);
        }

        /// <summary>根据字符串形式IP地址转为物理地址</summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static String IPToAddress(this String addr)
        {
            if (addr.IsNullOrEmpty()) return null;

            // 有可能是NetUri
            var p = addr.IndexOf("://");
            if (p >= 0) addr = addr.Substring(p + 3);

            // 有可能是多个IP地址
            p = addr.IndexOf(",");
            if (p >= 0) addr = addr.Split(",").FirstOrDefault();

            // 过滤IPv4/IPv6端口
            if (addr.Replace("::", "").Contains(":")) addr = addr.Substring(0, addr.LastIndexOf(":"));

            if (!IPAddress.TryParse(addr, out var ip)) return null;

            return ip.GetAddress();
        }

        /// <summary>IP地址提供者接口</summary>
        public interface IPProvider
        {
            /// <summary>获取IP地址的物理地址位置</summary>
            /// <param name="addr"></param>
            /// <returns></returns>
            String GetAddress(IPAddress addr);
        }

        class IpProviderDefault : IPProvider
        {
            public String GetAddress(IPAddress addr)
            {
                // 判断局域网地址
                var ip = addr.ToString();
                var myip = MyIP().ToString();
                if (ip.CutEnd(".") == myip.CutEnd(".")) return "本地局域网";

                var f = addr.GetAddressBytes()[0];
                if ((f & 0x7F) == 0) return "A类地址";
                if ((f & 0xC0) == 0x80) return "B类地址";
                if ((f & 0xE0) == 0xC0) return "C类地址";
                if ((f & 0xF0) == 0xE0) return "D类地址";
                if ((f & 0xF8) == 0xF0) return "E类地址";

                return "";
            }
        }
        #endregion

        #region Tcp参数
#if !__MOBILE__ && !__CORE__
        /// <summary>设置最大Tcp连接数</summary>
        public static void SetTcpMax()
        {
            TcpNumConnections = 0x00FFFFFE;
            MaxUserPort = 65534;
            MaxFreeTcbs = 16000;
            MaxHashTableSize = 65536;
            TcpTimedWaitDelay = 30;
            KeepAliveTime = 30 * 60 * 1000;
            EnableConnectionRateLimiting = 0;
        }

        /// <summary>显示Tcp参数</summary>
        public static void ShowTcpParameters()
        {
            XTrace.WriteLine("{0,-17}: {1,10:n0} Max : {2,10:n0}", "TcpNumConnections", TcpNumConnections, 0x00FFFFFE);
            XTrace.WriteLine("{0,-17}: {1,10:n0} Max : {2,10:n0}", "MaxUserPort", MaxUserPort, 65534);
            XTrace.WriteLine("{0,-17}: {1,10:n0} Max : {2,10:n0}", "MaxFreeTcbs", MaxFreeTcbs, 16000);
            XTrace.WriteLine("{0,-17}: {1,10:n0} Max : {2,10:n0}", "MaxHashTableSize", MaxHashTableSize, 65536);
            XTrace.WriteLine("{0,-17}: {1,10:n0} Best: {2,10:n0}", "TcpTimedWaitDelay", TcpTimedWaitDelay, 30);
            XTrace.WriteLine("{0,-17}: {1,10:n0} Best: {2,10:n0}", "KeepAliveTime", KeepAliveTime, 30 * 60 * 1000);
            XTrace.WriteLine("{0,-17}: {1,10:n0} Best: {2,10:n0}", "EnableConnectionRateLimiting", EnableConnectionRateLimiting, 0);
        }

        /// <summary>最大TCP连接数。默认16M</summary>
        public static Int32 TcpNumConnections { get { return GetReg("TcpNumConnections", 0x00FFFFFE); } set { SetReg("TcpNumConnections", value); } }

        /// <summary>最大用户端口数。默认5000</summary>
        /// <remarks>
        /// TCP客户端和服务器连接时，客户端必须分配一个动态端口，默认情况下这个动态端口的分配范围为 1024-5000 ，也就是说默认情况下，客户端最多可以同时发起3977 个Socket 连接
        /// </remarks>
        public static Int32 MaxUserPort { get { return GetReg("MaxUserPort", 5000); } set { SetReg("MaxUserPort", value); } }

        /// <summary>最大TCB 数量。默认2000</summary>
        /// <remarks>
        /// 系统为每个TCP 连接分配一个TCP 控制块(TCP control block or TCB)，这个控制块用于缓存TCP连接的一些参数，每个TCB需要分配 0.5 KB的pagepool 和 0.5KB 的Non-pagepool，也就说，每个TCP连接会占用 1KB 的系统内存
        /// </remarks>
        public static Int32 MaxFreeTcbs { get { return GetReg("MaxFreeTcbs", 2000); } set { SetReg("MaxFreeTcbs", value); } }

        /// <summary>最大TCP连接数。默认16M</summary>
        /// <remarks>
        /// 这个值指明分配 pagepool 内存的数量，也就是说，如果MaxFreeTcbs = 1000 , 则 pagepool 的内存数量为 500KB
        /// 那么 MaxHashTableSize 应大于 500 才行。这个数量越大，则Hash table 的冗余度就越高，每次分配和查找 TCP  连接用时就越少。这个值必须是2的幂，且最大为65536.
        /// </remarks>
        public static Int32 MaxHashTableSize { get { return GetReg("MaxHashTableSize", 512); } set { SetReg("MaxHashTableSize", value); } }

        /// <summary>系统释放已关闭的TCP连接并复用其资源之前，必须等待的时间。默认240</summary>
        /// <remarks>
        /// 这段时间间隔就是TIME_WAIT状态（2MSL，数据包最长生命周期的两倍状态）。
        /// 如果系统显示大量连接处于TIME_WAIT状态，则会导致并发量与吞吐量的严重下降，通过减小该项的值，系统可以更快地释放已关闭的连接，
        /// 从而为新连接提供更多的资源，特别是对于高并发短连接的Server具有积极的意义。
        /// </remarks>
        public static Int32 TcpTimedWaitDelay { get { return GetReg("TcpTimedWaitDelay", 240); } set { SetReg("TcpTimedWaitDelay", value); } }

        /// <summary>控制系统尝试验证空闲连接是否仍然完好的频率。默认2小时</summary>
        /// <remarks>
        /// 如果该连接在一段时间内没有活动，那么系统会发送保持连接的信号，如果网络正常并且接收方是活动的，它就会响应。如果需要对丢失接收方的情况敏感，也就是说需要更快地发现是否丢失了接收方，请考虑减小该值。而如果长期不活动的空闲连接的出现次数较多，但丢失接收方的情况出现较少，那么可能需要增大该值以减少开销。
        /// </remarks>
        public static Int32 KeepAliveTime { get { return GetReg("KeepAliveTime", 2 * 60 * 60 * 1000); } set { SetReg("KeepAliveTime", value); } }

        /// <summary>半开连接数限制。默认0</summary>
        public static Int32 EnableConnectionRateLimiting { get { return GetReg("EnableConnectionRateLimiting", 0); } set { SetReg("EnableConnectionRateLimiting", value); } }

        private static Int32 GetReg(String key, Int32 defvalue = 0)
        {
            using (var rkey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Services\Tcpip\Parameters"))
            {
                //var sub = rkey.OpenSubKey(key);
                //if (sub == null) return defvalue;

                return rkey.GetValue(key).ToInt(defvalue);
            }
        }

        private static void SetReg(String key, Int32 value)
        {
            using (var rkey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Services\Tcpip\Parameters", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl))
            {
                //var sub = rkey.CreateSubKey(key);
                rkey.SetValue(key, value);
            }
        }
#endif
        #endregion

        #region 创建客户端和会话
        /// <summary>根据本地网络标识创建客户端</summary>
        /// <param name="local"></param>
        /// <returns></returns>
        public static ISocketClient CreateClient(this NetUri local)
        {
            if (local == null) throw new ArgumentNullException(nameof(local));

            switch (local.Type)
            {
                case NetType.Tcp:
                    return new TcpSession { Local = local };
                case NetType.Udp:
                    return new UdpServer { Local = local };
                //case NetType.Http:
                //    return new HttpClient { Local = local };
                default:
                    throw new NotSupportedException("不支持{0}协议".F(local.Type));
            }
        }

        /// <summary>根据远程网络标识创建客户端</summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        public static ISocketClient CreateRemote(this NetUri remote)
        {
            if (remote == null) throw new ArgumentNullException(nameof(remote));

            switch (remote.Type)
            {
                case NetType.Tcp:
                    return new TcpSession { Remote = remote };
                case NetType.Udp:
                    return new UdpServer { Remote = remote };
                //case NetType.Http:
                //    var http = new HttpClient { Remote = remote };
                //    //http.IsSSL = remote.Protocol.EqualIgnoreCase("https");
                //    return http;
                //case NetType.WebSocket:
                //    var ws = new HttpClient { Remote = remote };
                //    //ws.IsSSL = remote.Protocol.EqualIgnoreCase("https");
                //    ws.IsWebSocket = true;
                //    return ws;
                default:
                    throw new NotSupportedException("不支持{0}协议".F(remote.Type));
            }
        }

        ///// <summary>根据远程网络标识创建客户端</summary>
        ///// <param name="uri"></param>
        ///// <returns></returns>
        //public static ISocketClient CreateRemote(this Uri uri)
        //{
        //    var http = new HttpClient();
        //    http.Request.Url = uri ?? throw new ArgumentNullException(nameof(uri));
        //    http.Remote = new NetUri(uri + "");

        //    switch (uri.Scheme.ToLower())
        //    {
        //        case "https":
        //            http.IsSSL = true;
        //            break;
        //        case "wss":
        //            http.IsWebSocket = true;
        //            http.IsSSL = true;
        //            break;
        //        case "ws":
        //            http.IsWebSocket = true;
        //            break;
        //        default:
        //            break;
        //    }

        //    return http;
        //}

        internal static Socket CreateTcp(Boolean ipv4 = true) => new Socket(ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

        internal static Socket CreateUdp(Boolean ipv4 = true) => new Socket(ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
        #endregion
    }
}