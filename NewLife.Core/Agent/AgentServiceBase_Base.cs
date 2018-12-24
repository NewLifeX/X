using System;
using System.ServiceProcess;
using NewLife.Log;

namespace NewLife.Agent
{
    /// <summary>服务程序基类</summary>
    public abstract class AgentServiceBase : ServiceBase
    {
        #region 属性
        /// <summary>显示名</summary>
        public virtual String DisplayName { get; set; }

        /// <summary>描述</summary>
        public virtual String Description { get; set; }

        /// <summary>线程数</summary>
        public virtual Int32 ThreadCount { get; set; } = 1;
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public AgentServiceBase()
        {
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = true;
            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            AutoLog = true;
        }
        #endregion

        #region 静态属性
        /// <summary>服务实例。每个应用程序域只有一个服务实例</summary>
        public static AgentServiceBase Instance { get; set; }
        #endregion

        private static Int32[] _Intervals;
        /// <summary>间隔数组。默认60秒</summary>
        public static Int32[] Intervals
        {
            get
            {
                if (_Intervals != null) return _Intervals;

                _Intervals = Setting.Current.Intervals.SplitAsInt();
                return _Intervals;
            }
            set { _Intervals = value; }
        }

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null && Log.Enable) Log.Info(format, args);
        }
        #endregion
    }
}