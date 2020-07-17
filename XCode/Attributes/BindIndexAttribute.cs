using System;
using System.Collections.Generic;
using XCode.DataAccessLayer;
using NewLife;

namespace XCode
{
    /// <summary>用于指定数据类所绑定到的索引</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BindIndexAttribute : Attribute
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>是否唯一</summary>
        public Boolean Unique { get; set; }

        /// <summary>数据列集合</summary>
        public String Columns { get; set; }
        #endregion

        #region 构造
        /// <summary>指定一个索引</summary>
        /// <param name="name">名称</param>
        /// <param name="unique"></param>
        /// <param name="columns"></param>
        public BindIndexAttribute(String name, Boolean unique, String columns)
        {
            Name = name;
            Unique = unique;
            Columns = columns;
        }
        #endregion

        #region 方法
        /// <summary>填充索引</summary>
        /// <param name="index"></param>
        internal void Fill(IDataIndex index)
        {
            if (!String.IsNullOrEmpty(Name)) index.Name = Name;
            index.Unique = Unique;
            if (!String.IsNullOrEmpty(Columns))
            {
                var ss = Columns.Split(",", ";");
                var list = new List<String>();
                foreach (var item in ss)
                {
                    var column = item.Trim();
                    if (!String.IsNullOrEmpty(column)) list.Add(column);
                }
                index.Columns = list.ToArray();
            }
        }
        #endregion
    }
}