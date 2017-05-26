using System;
using System.Diagnostics;
using System.Reflection;
using NewLife.Log;

namespace NewLife.Net.Sockets
{
    /// <summary>网络基类，提供资源释放和日志输出的统一处理</summary>
    public abstract class Netbase : DisposeBase
    {
        #region 构造销毁
        static Netbase()
        {
            // 输出网络库版本
            Assembly.GetExecutingAssembly().WriteVersion();
        }

        /// <summary>实例化</summary>
        public Netbase()
        {
            if (Setting.Current.Debug) Log = XTrace.Log;
        }
        #endregion

        #region 日志
        /// <summary>日志提供者</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteLog(String format, params Object[] args)
        {
            Log.Info(format, args);
        }

        /// <summary>写调试日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public virtual void WriteDebugLog(String format, params Object[] args)
        {
            Log.Debug(format, args);
        }
        #endregion
    }
}