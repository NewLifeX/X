using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
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

                String str = ConfigurationManager.AppSettings["NewLife.Net.Debug"];
                if (String.IsNullOrEmpty(str))
                    _Debug = false;
                else if (str == "1" || str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                    _Debug = true;
                else if (str == "0" || str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                    _Debug = false;
                else
                    _Debug = Convert.ToBoolean(str);
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
        #endregion
    }
}
