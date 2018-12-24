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
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public AgentServiceBase()
        {
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = false;
            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            AutoLog = true;
        }
        #endregion

        #region 静态属性
        /// <summary>服务实例。每个应用程序域只有一个服务实例</summary>
        public static AgentServiceBase Instance { get; set; }
        #endregion

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