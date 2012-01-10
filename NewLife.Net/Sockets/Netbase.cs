using System;
using NewLife.Net;

namespace NewLife.Net.Sockets
{
    /// <summary>网络基类，提供资源释放和日志输出的统一处理</summary>
    public abstract class Netbase : DisposeBase
    {
        #region 日志
        private Boolean? _EnableLog;
        /// <summary>是否显示日志。默认是NetHelper.Debug</summary>
        public Boolean EnableLog { get { return _EnableLog ?? NetHelper.Debug; } set { _EnableLog = value; } }

        /// <summary>写日志</summary>
        /// <param name="msg"></param>
        public void WriteLog(String msg)
        {
            if (EnableLog) NetHelper.WriteLog(msg);
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (EnableLog) NetHelper.WriteLog(format, args);
        }
        #endregion
    }
}