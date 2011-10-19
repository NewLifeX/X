using XAgent;
using System.Diagnostics;
using System;

namespace XAgent
{
    public class AgentService : AgentServiceBase<AgentService>
    {
        #region 属性
        public override int ThreadCount
        {
            get
            {
                return 2;
            }
        }

        public override string DisplayName
        {
            get
            {
                return "测试服务";
            }
        }

        public override string Description
        {
            get
            {
                return "这是一个用于测试XAgent的服务！";
            }
        }
        #endregion

        #region 构造函数
        public AgentService()
        {
            // 一般在构造函数里面指定服务名
            ServiceName = "XAgentTest";
        }
        #endregion

        #region 核心
        public override bool Work(int index)
        {
            // XAgent讲开启ThreadCount个线程，0<index<ThreadCount，本函数即为每个任务线程的主函数，间隔Interval循环调用
            WriteLine("任务{0}，当前时间：{1}", index, DateTime.Now);

            return false;
        }
        #endregion
    }
}