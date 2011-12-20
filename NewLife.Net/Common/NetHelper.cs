using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using NewLife.Configuration;
using NewLife.Log;

namespace NewLife.Net.Common
{
    /// <summary>
    /// 网络工具类
    /// </summary>
    public static class NetHelper
    {
        #region 日志输出
        private static Boolean? _Debug;
        /// <summary>
        /// 是否调试
        /// </summary>
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

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg)
        {
            XTrace.WriteLine(msg);
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            XTrace.WriteLine(format, args);
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 分析地址
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IPAddress ParseAddress(String hostname)
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses(hostname);
            int index = 0;
            while ((index < hostAddresses.Length) && (hostAddresses[index].AddressFamily != AddressFamily.InterNetwork))
            {
                index++;
            }
            if (hostAddresses.Length > 0 && index < hostAddresses.Length) return hostAddresses[index];

            return null;
        }

        /// <summary>
        /// 获取本地IPV4列表
        /// </summary>
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
        #endregion

        #region 远程开机
        /// <summary>唤醒指定MAC地址的计算机</summary>
        /// <param name="mac"></param>
        public static void Wake(String mac)
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
