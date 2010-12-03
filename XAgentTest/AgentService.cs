using XAgent;
using System.Diagnostics;
using System;

namespace XAgentTest
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

        public override string Description
        {
            get
            {
                return "测试服务";
            }
        }
        #endregion

        #region 构造函数
        public AgentService()
        {
            ServiceName = "XAgentTest";
        }
        #endregion

        #region 核心
        public override bool Work(int index)
        {
            WriteLine("任务{0}，当前时间：{1}", index, DateTime.Now);

            return false;
        }
        #endregion
    }
}