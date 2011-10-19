using XAgent;
using System.Diagnostics;
using System;

namespace XAgent
{
    /// <summary>
    /// 代理服务例子。自定义服务程序可参照该类实现。
    /// </summary>
    class AgentService : AgentServiceBase<AgentService>
    {
        #region 属性
        /// <summary>线程数</summary>
        public override int ThreadCount { get { return 2; } }

        /// <summary>显示名</summary>
        public override string DisplayName { get { return "新生命服务代理"; } }

        /// <summary>描述</summary>
        public override string Description { get { return "用于承载各种服务的服务代理！"; } }
        #endregion

        #region 构造函数
        /// <summary>实例化一个代理服务</summary>
        public AgentService()
        {
            // 一般在构造函数里面指定服务名
            ServiceName = "XAgent";
        }
        #endregion

        #region 核心
        /// <summary>
        /// 核心工作方法。调度线程会定期调用该方法
        /// </summary>
        /// <param name="index">线程序号</param>
        /// <returns>是否立即开始下一步工作。某些任务能达到满负荷，线程可以不做等待</returns>
        public override bool Work(int index)
        {
            // XAgent讲开启ThreadCount个线程，0<index<ThreadCount，本函数即为每个任务线程的主函数，间隔Interval循环调用
            //WriteLine("任务{0}，当前时间：{1}", index, DateTime.Now);

            return false;
        }
        #endregion
    }
}