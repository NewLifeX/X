using System;

namespace NewLife.Agent
{
#if DEBUG
    /// <summary>代理服务例子。自定义服务程序可参照该类实现。</summary>
    public class AgentService : AgentServiceBase
    {
        #region 属性
        #endregion

        #region 构造函数
        /// <summary>实例化一个代理服务</summary>
        public AgentService()
        {
            // 一般在构造函数里面指定服务名
            ServiceName = "XAgent";

            DisplayName = "新生命服务代理";
            Description = "用于承载各种服务的服务代理！";
        }
        #endregion

        #region 核心
        /// <summary>开始工作</summary>
        /// <param name="reason"></param>
        protected override void StartWork(String reason)
        {
            WriteLog("业务开始……");

            base.StartWork(reason);
        }

        /// <summary>停止服务</summary>
        /// <param name="reason"></param>
        protected override void StopWork(String reason)
        {
            WriteLog("业务结束！");

            base.StopWork(reason);
        }
        #endregion
    }
#endif
}