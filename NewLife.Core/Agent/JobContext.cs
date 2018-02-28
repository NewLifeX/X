using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Collections;

namespace NewLife.Agent
{
    /// <summary>任务上下文</summary>
    public class JobContext
    {

        #region 索引器
        private IDictionary<String, Object> _Items = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>用户数据</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Object this[String item] { get => _Items[item]; set => _Items[item] = value; }
        #endregion
    }
}