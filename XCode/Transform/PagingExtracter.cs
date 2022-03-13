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
    public class PagingExtracter : IExtracter<DbTable>
    {
        #region 属性
        /// <summary>数据层</summary>
        public DAL Dal { get; set; }

        /// <summary>查询表达式</summary>
        public SelectBuilder Builder { get; set; }

        /// <summary>开始行。默认0</summary>
        public Int64 Row { get; set; }

        /// <summary>批大小。默认5000</summary>
        public Int32 BatchSize { get; set; } = 5000;
        #endregion

        #region 构造
        /// <summary>实例化分页抽取器</summary>
        public PagingExtracter() { }

        /// <summary>实例化分页抽取器</summary>
        /// <param name="dal"></param>
        /// <param name="tableName"></param>
        public PagingExtracter(DAL dal, String tableName)
        {
            Dal = dal;
            Builder = new SelectBuilder { Table = tableName };
            BatchSize = dal.Db.BatchSize;
        }

        /// <summary>实例化分页抽取器</summary>
        /// <param name="dal"></param>
        /// <param name="tableName"></param>
        /// <param name="orderBy"></param>
        public PagingExtracter(DAL dal, String tableName, String orderBy)
        {
            Dal = dal;
            Builder = new SelectBuilder { Table = tableName, OrderBy = orderBy };
            BatchSize = dal.Db.BatchSize;
        }
        #endregion

        #region 抽取数据
        /// <summary>迭代抽取数据</summary>
        /// <returns></returns>
        public virtual IEnumerable<DbTable> Fetch()
        {
            while (true)
            {
                // 查询数据
                var dt = Dal.Query(Builder, Row, BatchSize);
                if (dt == null) break;

                var count = dt.Rows.Count;
                if (count == 0) break;

                // 返回数据
                yield return dt;

                Row += BatchSize;

                // 下一页
                if (count < BatchSize) break;
            }
        }
        #endregion
    }
}