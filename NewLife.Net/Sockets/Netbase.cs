using System;
using NewLife.Net;

namespace NewLife.Net.Sockets
{
    /// <summary>网络基类，提供资源释放和日志输出的统一处理</summary>
    public abstract class Netbase : DisposeBase
    {
        #region 日志
        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="msg"></param>
        public void WriteLog(String msg)
        {
            if (NetHelper.Debug) NetHelper.WriteLog(msg);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (NetHelper.Debug) NetHelper.WriteLog(format, args);
        }
        #endregion
    }
}