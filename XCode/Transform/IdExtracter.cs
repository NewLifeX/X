using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Data;
using XCode.DataAccessLayer;

namespace XCode.Transform
{
    /// <summary>自增数据抽取器</summary>
    /// <remarks>
    /// 适用于带有自增字段或雪花Id字段的数据抽取器，速度飞快。
    /// </remarks>
    public class IdExtracter : IExtracter<DbTable>
    {
        #region 属性
        /// <summary>数据层</summary>
        public DAL Dal { get; set; }

        /// <summary>查询表达式</summary>
        public SelectBuilder Builder { get; set; }

        /// <summary>Id字段</summary>
        public String IdField { get; set; }

        /// <summary>开始行。默认0</summary>
        public Int64 Row { get; set; }

        /// <summary>批大小。默认5000</summary>
        public Int32 BatchSize { get; set; } = 5000;

        /// <summary>总行数</summary>
        public Int32 TotalCount { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化数据抽取器</summary>
        public IdExtracter() { }

        /// <summary>实例化数据抽取器</summary>
        /// <param name="dal"></param>
        /// <param name="tableName"></param>
        /// <param name="idField"></param>
        public IdExtracter(DAL dal, String tableName, String idField)
        {
            Dal = dal;
            Builder = new SelectBuilder { Table = tableName, OrderBy = idField + " asc" };
            IdField = idField;
            BatchSize = (dal.Db as DbBase).BatchSize;
        }
        #endregion

        #region 抽取数据
        /// <summary>迭代抽取数据</summary>
        /// <returns></returns>
        public virtual IEnumerable<DbTable> Fetch()
        {
            while (true)
            {
                // 分割数据页，自增
                var sb = Builder.Clone();
                if (!sb.Where.IsNullOrEmpty()) sb.Where += " And ";
                sb.Where += $"{IdField}>={Row}";

                // 查询数据
                var dt = Dal.Query(sb, 0, BatchSize);
                if (dt == null) break;

                var count = dt.Rows.Count;
                if (count == 0) break;

                // 返回数据
                yield return dt;

                // 自增分割时，取最后一行
                Row = dt.Get<Int64>(count - 1, IdField) + 1;

                // 下一页
                TotalCount += count;
                if (count < BatchSize) break;
            }
        }
        #endregion
    }
}