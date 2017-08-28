using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode
{
    /// <summary>实体分表分库</summary>
    public static class EntitySplit
    {
        #region 分表分库
        /// <summary>在分库上执行操作，自动还原</summary>
        /// <param name="factory"></param>
        /// <param name="connName"></param>
        /// <param name="tableName"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T Split<T>(this IEntityOperate factory, String connName, String tableName, Func<T> func)
        {
            using (var split = new SplitPackge(factory, connName, tableName))
            {
                return func();
            }
        }

        class SplitPackge : IDisposable
        {
            /// <summary>实体工厂</summary>
            public IEntityOperate Factory { get; set; }

            /// <summary>连接名</summary>
            public String ConnName { get; set; }

            /// <summary>表名</summary>
            public String TableName { get; set; }

            public SplitPackge(IEntityOperate factory, String connName, String tableName)
            {
                Factory = factory;

                var fact = Factory;
                ConnName = fact.ConnName;
                TableName = fact.TableName;

                fact.ConnName = connName;
                fact.TableName = tableName;
            }

            public void Dispose()
            {
                var fact = Factory;
                fact.ConnName = ConnName;
                fact.TableName = TableName;
            }
        }
        #endregion
    }
}