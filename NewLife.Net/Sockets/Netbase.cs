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
            //var asmx = AssemblyX.Create(Assembly.GetExecutingAssembly());
            //XTrace.WriteLine("{0,-16} v{1} Build {2:yyyy-MM-dd HH:mm:ss}", asmx.Name, asmx.FileVersion, asmx.Compile);
            Assembly.GetExecutingAssembly().WriteVersion();
        }
        #endregion

        #region 日志
        private ILog _Log = NetHelper.Debug ? XTrace.Log : Logger.Null;
        /// <summary>日志提供者</summary>
        public ILog Log { get { return _Log; } set { _Log = value ?? Logger.Null; } }

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