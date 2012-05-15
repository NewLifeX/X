using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.CSharp;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
using XCode.DataAccessLayer.Model;
#endif

namespace XCode.DataAccessLayer
{
    /// <summary>数据模型扩展</summary>
    public static class ModelHelper
    {
        #region 模型扩展方法
        /// <summary>根据字段名获取字段</summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IDataColumn GetColumn(this IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return table.Columns.FirstOrDefault(c => c.Is(name));
        }

        /// <summary>根据字段名数组获取字段数组</summary>
        /// <param name="table"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public static IDataColumn[] GetColumns(this IDataTable table, String[] names)
        {
            if (names == null || names.Length < 1) return null;

            return table.Columns.Where(c => names.Any(n => c.Is(n))).ToArray();
        }

        /// <summary>判断表是否等于指定名字</summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean Is(this IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            return table.Name.EqualIgnoreCase(name) || table.Alias.EqualIgnoreCase(name);
        }

        /// <summary>判断字段是否等于指定名字</summary>
        /// <param name="column"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean Is(this IDataColumn column, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            return column.Name.EqualIgnoreCase(name) || column.Alias.EqualIgnoreCase(name);
        }

        /// <summary>根据字段名找索引</summary>
        /// <param name="table"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public static IDataIndex GetIndex(this IDataTable table, params String[] columnNames)
        {
            if (table == null || table.Indexes == null || table.Indexes.Count < 1) return null;

            var di = table.Indexes.FirstOrDefault(
                e => e.Columns != null &&
                    e.Columns.Length == columnNames.Length &&
                    !e.Columns.Except(columnNames, StringComparer.OrdinalIgnoreCase).Any());
            if (di != null) return di;

            // 用别名再试一次
            var columns = table.GetColumns(columnNames);
            if (columns == null || columns.Length < 1) return null;
            columnNames = columns.Select(e => e.Alias).ToArray();
            di = table.Indexes.FirstOrDefault(
                e => e.Columns != null &&
                    e.Columns.Length == columnNames.Length &&
                    !e.Columns.Except(columnNames, StringComparer.OrdinalIgnoreCase).Any());
            if (di != null) return di;

            return null;
        }

        /// <summary>根据字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, String columnName)
        {
            return table.Relations.FirstOrDefault(e => e.Column.EqualIgnoreCase(columnName));
            //foreach (var item in table.Relations)
            //{
            //    if (String.Equals(item.Column, columnName, StringComparison.OrdinalIgnoreCase)) return item;
            //}

            //return null;
        }

        /// <summary>根据字段、关联表、关联字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, IDataRelation dr)
        {
            return table.GetRelation(dr.Column, dr.RelationTable, dr.RelationColumn);
        }

        /// <summary>根据字段、关联表、关联字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="rtableName"></param>
        /// <param name="rcolumnName"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, String columnName, String rtableName, String rcolumnName)
        {
            foreach (var item in table.Relations)
            {
                if (item.Column == columnName && item.RelationTable == rtableName && item.RelationColumn == rcolumnName) return item;
            }

            return null;
        }
        #endregion

        #region 复制扩展方法
        /// <summary>复制数据表到另一个数据表，不复制数据列、索引和关系</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataTable CopyFrom(this IDataTable src, IDataTable des)
        {
            src.ID = des.ID;
            src.Name = des.Name;
            src.Alias = des.Alias;
            src.Owner = des.Owner;
            src.DbType = des.DbType;
            src.IsView = des.IsView;
            src.Description = des.Description;

            return src;
        }

        /// <summary>复制数据表到另一个数据表，复制所有数据列、索引和关系</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <param name="resetColumnID">是否重置列ID</param>
        /// <returns></returns>
        public static IDataTable CopyAllFrom(this IDataTable src, IDataTable des, Boolean resetColumnID = false)
        {
            src.CopyFrom(des);
            src.Columns.AddRange(des.Columns.Select(i => src.CreateColumn().CopyFrom(i)));
            src.Indexes.AddRange(des.Indexes.Select(i => src.CreateIndex().CopyFrom(i)));
            src.Relations.AddRange(des.Relations.Select(i => src.CreateRelation().CopyFrom(i)));
            // 重载ID
            if (resetColumnID) src.Columns.ForEach((it, i) => it.ID = i + 1);

            return src;
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataColumn CopyFrom(this IDataColumn src, IDataColumn des)
        {
            src.ID = des.ID;
            src.Name = des.Name;
            src.Alias = des.Alias;
            src.DataType = des.DataType;
            src.RawType = des.RawType;
            src.Identity = des.Identity;
            src.PrimaryKey = des.PrimaryKey;
            src.Length = des.Length;
            src.NumOfByte = des.NumOfByte;
            src.Precision = des.Precision;
            src.Scale = des.Scale;
            src.Nullable = des.Nullable;
            src.IsUnicode = des.IsUnicode;
            src.Default = des.Default;
            src.Description = des.Description;

            return src.Fix();
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataIndex CopyFrom(this IDataIndex src, IDataIndex des)
        {
            src.Name = des.Name;
            src.Columns = des.Columns;
            src.Unique = des.Unique;
            src.PrimaryKey = des.PrimaryKey;
            src.Computed = des.Computed;

            return src;
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataRelation CopyFrom(this IDataRelation src, IDataRelation des)
        {
            src.Column = des.Column;
            src.RelationTable = des.RelationTable;
            src.RelationColumn = des.RelationColumn;
            src.Unique = des.Unique;
            src.Computed = des.Computed;

            return src;
        }
        #endregion

        #region 辅助
        #endregion
    }
}