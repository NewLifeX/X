using System;
using System.Diagnostics;
using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Net.Sockets
{
    /// <summary>网络基类，提供资源释放和日志输出的统一处理</summary>
    public abstract class Netbase : DisposeBase
    {
        #region 属性
        //private DateTime _StartTime = DateTime.Now;
        ///// <summary>开始时间</summary>
        //public DateTime StartTime { get { return _StartTime; } /*set { _StartTime = value; }*/ }
        #endregion

        #region 构造销毁
        static Netbase()
        {
            var asmx = AssemblyX.Create(Assembly.GetExecutingAssembly());
            XTrace.WriteLine("{0} v{1} Build {2:yyyy-MM-dd HH:mm:ss}", asmx.Name, asmx.FileVersion, asmx.Compile);
        }

        //        /// <summary>子类重载实现资源释放逻辑</summary>
        //        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        //        protected override void OnDispose(bool disposing)
        //        {
        //            base.OnDispose(disposing);

        //#if DEBUG
        //            //WriteLog("Dispose {0} {1}", this.GetType().Name, this);
        //#endif
        //        }
        #endregion

        #region 日志
        private Boolean? _EnableLog;
        /// <summary>是否显示日志。默认是NetHelper.Debug</summary>
        public virtual Boolean EnableLog { get { return _EnableLog ?? NetHelper.Debug; } set { _EnableLog = value; } }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (EnableLog) NetHelper.WriteLog(format, args);
        }

        /// <summary>写调试日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public void WriteDebugLog(String format, params Object[] args)
        {
            if (EnableLog) NetHelper.WriteLog(format, args);
        }
        #endregion
    }
}