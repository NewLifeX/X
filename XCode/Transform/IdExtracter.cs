using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Transform
{
    /// <summary>自增数据抽取器</summary>
    public class IdExtracter
    {
        #region 属性
        /// <summary>批大小。默认5000</summary>
        public Int32 BatchSize { get; set; } = 5000;
        #endregion

        #region 抽取数据
        /// <summary>迭代抽取数据</summary>
        /// <param name="dal"></param>
        /// <param name="tableName"></param>
        /// <param name="idColumn"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public virtual IEnumerable<DbTable> Fetch(DAL dal, String tableName, String idColumn, Int64 start = 0)
        {
            while (true)
            {
                var sb = new SelectBuilder { Table = tableName };

                // 分割数据页，自增或分页
                if (idColumn != null)
                {
                    sb.Where = $"{idColumn}>={start}";
                    sb = dal.Query(sb, 0, BatchSize);
                }
                else
                    sb = dal.Query(sb, start, BatchSize);

                // 查询数据
                var dt = dal.Session.Query(sb, null);
                if (dt == null) break;

                var count = dt.Rows.Count;
                if (count == 0) break;

                // 返回数据
                yield return dt;

                // 下一页
                if (count < BatchSize) break;

                // 自增分割时，取最后一行
                if (idColumn != null)
                    start = dt.Get<Int64>(count - 1, idColumn) + 1;
                else
                    start += BatchSize;
            }
        }

        public virtual IEnumerable<IList<IEntity>> Fetch(IEntityFactory factory, FieldItem id, Int64 row)
        {
            while (true)
            {
                // 分割数据页，自增
                var exp = id >= row;

                // 查询数据
                var list = factory.FindAll(exp, id.Asc(), null, 0, BatchSize);
                if (list.Count == 0) break;

                // 返回数据
                yield return list;

                // 下一页
                if (list.Count < BatchSize) break;

                // 自增分割时，取最后一行
                row = (Int64)list.Last()[id.Name];
            }
        }
        #endregion
    }
}