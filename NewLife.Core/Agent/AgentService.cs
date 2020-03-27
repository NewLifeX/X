using System;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Agent
{
    /// <summary>代理服务例子。自定义服务程序可参照该类实现。</summary>
    public class MyXService : ServiceBase
    {
        #region 属性
        #endregion

        #region 构造函数
        /// <summary>实例化一个代理服务</summary>
        public MyXService()
        {
            // 一般在构造函数里面指定服务名
            ServiceName = "XAgent";

            DisplayName = "新生命服务代理";
            Description = "用于承载各种服务的服务代理！";
        }
        #endregion

        #region 核心
        private TimerX _timer;
        private TimerX _timer2;
        /// <summary>开始工作</summary>
        /// <param name="reason"></param>
        protected override void StartWork(String reason)
        {
            WriteLog("业务开始……");

            // 5秒开始，每60秒执行一次
            _timer = new TimerX(DoWork, null, 5_000, 60_000) { Async = true };
            // 每天凌晨执行一次
            _timer2 = new TimerX(DoWork, null, DateTime.Today, 24 * 3600 * 1000) { Async = true };

            base.StartWork(reason);
        }

        private void DoWork(Object state)
        {
            XTrace.WriteLine("定时任务");
        }

        /// <summary>停止服务</summary>
        /// <param name="reason"></param>
        protected override void StopWork(String reason)
        {
            WriteLog("业务结束！");

            _timer.Dispose();
            _timer2.Dispose();

            base.StopWork(reason);
        }
        #endregion
    }
}