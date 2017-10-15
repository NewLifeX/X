using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Agent
{
    /// <summary>任务接口</summary>
    public interface IJob
    {
        /// <summary>执行一次任务</summary>
        /// <param name="context"></param>
        void Execute(JobContext context);
    }

    /// <summary>工作任务基类</summary>
    public abstract class JobBase : IJob
    {
        /// <param name="context"></param>
        public abstract void Execute(JobContext context);

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}