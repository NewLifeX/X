using System;
using NewLife.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NewLife.Configuration;
using NewLife.Log;
using System.Runtime.InteropServices;

namespace NewLife.Net
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
        /// <param name="msg"></param>
        public static void WriteLog(String msg)
        {
            XTrace.WriteLine(msg);
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
        public static void SetKeepAlive(this Socket socket, Boolean iskeepalive, Int32 starttime = 10000, Int32 interval = 10000)
        {
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(iskeepalive ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)starttime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        /// <summary>分析地址</summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IPAddress ParseAddress(String hostname)
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses(hostname);
            if (hostAddresses == null || hostAddresses.Length < 1) return null;

            return hostAddresses.FirstOrDefault(d => d.AddressFamily == AddressFamily.InterNetwork || d.AddressFamily == AddressFamily.InterNetworkV6);
        }

        /// <summary>获取本地IPV4列表</summary>
        /// <returns></returns>
        public static List<IPAddress> GetIPV4()
        {
            IPAddress[] IPList = Dns.GetHostAddresses(Dns.GetHostName());
            List<IPAddress> list = new List<IPAddress>();
            foreach (IPAddress item in IPList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork) list.Add(item);
            }

            return list;
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
        public static Boolean IsAny(this IPAddress address) { return address == IPAddress.Any || address == IPAddress.IPv6Any; }

        /// <summary>
        /// 指定地址的指定端口是否已被使用，似乎没办法判断IPv6地址
        /// </summary>
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
    }
}
