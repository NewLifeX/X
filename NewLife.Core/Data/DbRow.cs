using System;
using NewLife.Reflection;

namespace NewLife.Data
{
    /// <summary>数据行</summary>
    public struct DbRow : IIndexAccessor
    {
        #region 属性
        /// <summary>数据表</summary>
        public DbTable Table { get; set; }

        /// <summary>行索引</summary>
        public Int32 Index { get; set; }
        #endregion

        #region 构造
        /// <summary>构造数据行</summary>
        /// <param name="table"></param>
        /// <param name="index"></param>
        public DbRow(DbTable table, Int32 index)
        {
            Table = table;
            Index = index;
        }
        #endregion

        #region 索引器
        /// <summary>基于列索引访问</summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public Object this[Int32 column] { get => Table.Rows[Index][column]; set => Table.Rows[Index][column] = value; }

        /// <summary>基于列名访问</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Object this[String name] { get => this[Table.GetColumn(name)]; set => this[Table.GetColumn(name)] = value; }
        #endregion

        #region 高级扩展
        /// <summary>读取指定行的字段值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T Get<T>(String name) => Table.Get<T>(Index, name);
        #endregion
    }
}