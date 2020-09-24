using System;
using System.Collections.Generic;
using NewLife.Data;
using XCode.DataAccessLayer;

namespace XCode.Transform
{
    /// <summary>分页数据抽取器</summary>
    /// <remarks>
    /// 通用抽取器，既没有Id序列也没有时间戳时使用。
    /// 采用分页技术抽取，通用性很强，但是随着页数增加，速度也会下降。
    /// </remarks>
    public class PagingExtracter
    {
        #region 属性
        /// <summary>数据层</summary>
        public DAL Dal { get; set; }

        /// <summary>查询表达式</summary>
        public SelectBuilder Builder { get; set; }

        /// <summary>开始行</summary>
        public Int64 StartRow { get; set; }

        /// <summary>批大小。默认5000</summary>
        public Int32 BatchSize { get; set; } = 5000;
        #endregion

        #region 构造
        #endregion

        #region 抽取数据
        /// <summary>迭代抽取数据</summary>
        /// <returns></returns>
        public virtual IEnumerable<DbTable> Fetch()
        {
            while (true)
            {
                // 查询数据
                var dt = Dal.Query(Builder, StartRow, BatchSize);
                if (dt == null) break;

                var count = dt.Rows.Count;
                if (count == 0) break;

                // 返回数据
                yield return dt;

                // 下一页
                if (count < BatchSize) break;

                StartRow += BatchSize;
            }
        }
        #endregion
    }
}