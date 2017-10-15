using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Agent
{
    /// <summary>任务调度器</summary>
    public class JobSchedule
    {
        #region 属性
        /// <summary>任务集合</summary>
        public IJob[] Jobs { get; private set; } = new IJob[0];

        /// <summary>任务个数</summary>
        public Int32 Count => Jobs.Length;
        #endregion

        #region 方法
        /// <summary>向任务调度器添加任务</summary>
        /// <param name="job"></param>
        public void Add(IJob job)
        {
            lock (Jobs)
            {
                var list = new List<IJob>(Jobs);
                if (!list.Contains(job))
                {
                    list.Add(job);

                    Jobs = list.ToArray();
                }
            }
        }
        #endregion
    }
}