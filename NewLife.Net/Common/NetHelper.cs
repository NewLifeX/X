using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NewLife.Collections;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;

#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace System
{
    /// <summary>网络工具类</summary>
    public static class NetHelper
    {
        #region 日志输出
        private static Boolean? _Debug;
        /// <summary>是否调试</summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                _Debug = Config.GetConfig<Boolean>("NewLife.Net.Debug", false);

                return _Debug.Value;
            }
            set { _Debug = value; }
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            XTrace.WriteLine(format, args);
        }
        #endregion

        #region 辅助函数
        /// <summary>设置超时检测时间和检测间隔</summary>
        /// <param name="socket">要设置的Socket对象</param>
        /// <param name="iskeepalive">是否启用Keep-Alive</param>
        /// <param name="starttime">多长时间后开始第一次探测（单位：毫秒）</param>
        /// <param name="interval">探测时间间隔（单位：毫秒）</param>
        public static void SetTcpKeepAlive(this Socket socket, Boolean iskeepalive, Int32 starttime = 10000, Int32 interval = 10000)
        {
            if (socket == null || !socket.Connected) return;
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(iskeepalive ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)starttime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        private static DictionaryCache<String, IPAddress> _dnsCache = new DictionaryCache<String, IPAddress>(StringComparer.OrdinalIgnoreCase) { Expriod = 600, Asynchronous = true };
        /// <summary>分析地址</summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IPAddress ParseAddress(String hostname)
        {
            if (String.IsNullOrEmpty(hostname)) return null;

            try
            {
                return _dnsCache.GetItem(hostname, key =>
                {
                    IPAddress addr = null;
                    if (IPAddress.TryParse(key, out addr)) return addr;

                    var hostAddresses = Dns.GetHostAddresses(key);
                    if (hostAddresses == null || hostAddresses.Length < 1) return null;

                    return hostAddresses.FirstOrDefault(d => d.AddressFamily == AddressFamily.InterNetwork || d.AddressFamily == AddressFamily.InterNetworkV6);
                });
            }
            catch (SocketException ex)
            {
                throw new NetException("解析主机" + hostname + "的地址失败！" + ex.Message, ex);
            }
        }

        /// <summary>分析网络终结点</summary>
        /// <param name="address">地址，可以不带端口</param>
        /// <param name="defaultPort">地址不带端口时指定的默认端口</param>
        /// <returns></returns>
        public static IPEndPoint ParseEndPoint(String address, Int32 defaultPort = 0)
        {
            if (String.IsNullOrEmpty(address)) return null;

            Int32 p = address.IndexOf(":");
            if (p > 0)
                return new IPEndPoint(ParseAddress(address.Substring(0, p)), Int32.Parse(address.Substring(p + 1)));
            else
                return new IPEndPoint(ParseAddress(address), defaultPort);
        }

        /// <summary>针对IPv4和IPv6获取合适的Any地址</summary>
        /// <param name="address"></param>
        /// <param name="family"></param>
        /// <returns></returns>
        public static IPAddress GetRightAny(this IPAddress address, AddressFamily family)
        {
            switch (family)
            {
                case AddressFamily.InterNetwork:
                    if (address == IPAddress.IPv6Any) return IPAddress.Any;
                    return address;
                case AddressFamily.InterNetworkV6:
                    if (address == IPAddress.Any) return IPAddress.IPv6Any;
                    return address;
                default:
                    return address;
            }
        }

        /// <summary>是否Any地址，同时处理IPv4和IPv6</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsAny(this IPAddress address) { return IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address); }

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
            if (IPAddress.IsLoopback(remote))
                return addr.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            // 否则返回本地第一个IP地址
            foreach (var item in NetHelper.GetIPs())
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
        internal static Boolean IsUsed(ProtocolType protocol, IPAddress address, Int32 port)
        {
            var gp = IPGlobalProperties.GetIPGlobalProperties();

            IPEndPoint[] eps = null;
            if (protocol == ProtocolType.Tcp)
                eps = gp.GetActiveTcpListeners();
            else if (protocol == ProtocolType.Udp)
                eps = gp.GetActiveUdpListeners();
            else
                return false;

            foreach (var item in eps)
            {
                if (item.Address.Equals(address) && item.Port == port) return true;
            }

            return false;
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

        /// <summary>获取可用的IP地址</summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetIPs()
        {
            var list = new List<IPAddress>();
            foreach (var item in GetActiveInterfaces())
            {
                if (item != null && item.UnicastAddresses.Count > 0)
                {
                    foreach (var elm in item.UnicastAddresses)
                    {
                        if (list.Contains(elm.Address)) continue;
                        list.Add(elm.Address);

                        yield return elm.Address;
                    }
                }
            }
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
            mac = mac.Replace("-", null);
            Byte[] buffer = new Byte[mac.Length / 2];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Byte.Parse(mac.Substring(i * 2, 2), NumberStyles.HexNumber);
            }

            Byte[] bts = new Byte[6 + 16 * buffer.Length];
            for (int i = 0; i < 6; i++)
            {
                bts[i] = 0xFF;
            }
            for (int i = 6, k = 0; i < bts.Length; i++, k++)
            {
                if (k >= buffer.Length) k = 0;

                bts[i] = buffer[k];
            }

            UdpClient client = new UdpClient();
            client.EnableBroadcast = true;
            client.Send(bts, bts.Length, new IPEndPoint(IPAddress.Broadcast, 7));
        }
        #endregion

        #region 关闭连接
        /// <summary>关闭连接</summary>
        /// <param name="socket"></param>
        /// <param name="reuseAddress"></param>
        internal static void Close(Socket socket, Boolean reuseAddress = false)
        {
            if (socket == null || mSafeHandle == null) return;

            var hand = mSafeHandle.GetValue(socket) as SafeHandle;
            if (hand == null || hand.IsClosed) return;

            // 先用Shutdown禁用Socket（发送未完成发送的数据），再用Close关闭，这是一种比较优雅的关闭Socket的方法
            if (socket.Connected)
            {
                try
                {
                    socket.Disconnect(reuseAddress);
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException ex2)
                {
                    if (ex2.SocketErrorCode != SocketError.NotConnected) WriteLog(ex2.ToString());
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex3)
                {
                    if (Debug) WriteLog(ex3.ToString());
                }
            }

            socket.Close();
        }

        private static MemberInfoX[] _mSafeHandle;
        /// <summary>SafeHandle字段</summary>
        private static MemberInfoX mSafeHandle
        {
            get
            {
                if (_mSafeHandle != null && _mSafeHandle.Length > 0) return _mSafeHandle[0];

                MemberInfoX pix = FieldInfoX.Create(typeof(Socket), "m_Handle");
                if (pix == null) pix = PropertyInfoX.Create(typeof(Socket), "SafeHandle");
                _mSafeHandle = new MemberInfoX[] { pix };

                return pix;
            }
        }
        #endregion
    }
}