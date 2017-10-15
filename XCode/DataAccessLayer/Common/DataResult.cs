using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode.DataAccessLayer
{
    /// <summary>数据集</summary>
    public class DataResult
    {
        #region 属性
        /// <summary>所有行，每一行是字段数值数组</summary>
        public IList<Object[]> Rows { get; set; }

        /// <summary>字段名称</summary>
        public String[] Names { get; set; }

        /// <summary>字段类型</summary>
        public Type[] Types { get; set; }
        #endregion

        #region 辅助
        /// <summary>返回行数字段数</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return "Rows={0} Names={1}".F(Rows == null ? 0 : Rows.Count, Names == null ? 0 : Names.Length);
        }
        #endregion
    }
}