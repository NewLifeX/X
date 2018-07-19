using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NewLife.Data;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>在数据行和实体类之间映射数据的接口</summary>
    public interface IDataRowEntityAccessor
    {
        /// <summary>加载数据表。无数据时返回空集合而不是null。</summary>
        /// <param name="dt">数据表</param>
        /// <returns>实体数组</returns>
        IList<T> LoadData<T>(DataTable dt) where T : Entity<T>, new();

        /// <summary>加载数据表。无数据时返回空集合而不是null。</summary>
        /// <param name="ds">数据表</param>
        /// <returns>实体数组</returns>
        IList<T> LoadData<T>(DbTable ds) where T : Entity<T>, new();

        /// <summary>加载数据表。无数据时返回空集合而不是null。</summary>
        /// <param name="dr">数据读取器</param>
        /// <returns>实体数组</returns>
        IList<T> LoadData<T>(IDataReader dr) where T : Entity<T>, new();
    }

    /// <summary>在数据行和实体类之间映射数据接口的提供者</summary>
    public interface IDataRowEntityAccessorProvider
    {
        /// <summary>创建实体类的数据行访问器</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        IDataRowEntityAccessor CreateDataRowEntityAccessor(Type entityType);
    }

    class DataRowEntityAccessorProvider : IDataRowEntityAccessorProvider
    {
        /// <summary>创建实体类的数据行访问器</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public IDataRowEntityAccessor CreateDataRowEntityAccessor(Type entityType) => new DataRowEntityAccessor();
    }

    class DataRowEntityAccessor : IDataRowEntityAccessor
    {
        #region 存取
        /// <summary>加载数据表。无数据时返回空集合而不是null。</summary>
        /// <param name="dt">数据表</param>
        /// <returns>实体数组</returns>
        public IList<T> LoadData<T>(DataTable dt) where T : Entity<T>, new()
        {
            // 准备好实体列表
            var list = new List<T>();
            if (dt == null || dt.Rows.Count < 1) return list;

            // 对应数据表中字段的实体字段
            var ps = new Dictionary<DataColumn, FieldItem>();
            // 数据表中找不到对应的实体字段的数据字段
            var exts = new Dictionary<DataColumn, String>();
            var ti = Entity<T>.Meta.Table;
            foreach (DataColumn item in dt.Columns)
            {
                //var fi = Entity<T>.Meta.Fields.FirstOrDefault(e => e.ColumnName.EqualIgnoreCase(item.ColumnName));
                if (ti.FindByName(item.ColumnName) is FieldItem fi)
                    ps.Add(item, fi);
                else if (!item.ColumnName.EqualIgnoreCase(IgnoreFields))
                    exts.Add(item, item.ColumnName);
            }

            // 遍历每一行数据，填充成为实体
            foreach (DataRow dr in dt.Rows)
            {
                // 由实体操作者创建实体对象，因为实体操作者可能更换
                var entity = Entity<T>.Meta.Factory.Create() as T;
                foreach (var item in ps)
                    SetValue(entity, item.Value.Name, item.Value.Type, dr[item.Key]);

                foreach (var item in exts)
                    SetValue(entity, item.Value, null, dr[item.Key]);

                list.Add(entity);
            }
            return list;
        }

        /// <summary>加载数据表。无数据时返回空集合而不是null。</summary>
        /// <param name="ds">数据表</param>
        /// <returns>实体数组</returns>
        public IList<T> LoadData<T>(DbTable ds) where T : Entity<T>, new()
        {
            // 准备好实体列表
            var list = new List<T>();
            if (ds == null || ds.Rows.Count < 1) return list;

            // 对应数据表中字段的实体字段
            var ps = new Dictionary<Int32, FieldItem>();
            // 数据表中找不到对应的实体字段的数据字段
            var exts = new Dictionary<Int32, String>();
            var ti = Entity<T>.Meta.Table;
            for (var i = 0; i < ds.Columns.Length; i++)
            {
                var item = ds.Columns[i];
                if (ti.FindByName(item) is FieldItem fi)
                    ps.Add(i, fi);
                else if (!item.EqualIgnoreCase(IgnoreFields))
                    exts.Add(i, item);
            }

            // 遍历每一行数据，填充成为实体
            foreach (var dr in ds.Rows)
            {
                // 由实体操作者创建实体对象，因为实体操作者可能更换
                var entity = Entity<T>.Meta.Factory.Create() as T;
                foreach (var item in ps)
                    SetValue(entity, item.Value.Name, item.Value.Type, dr[item.Key]);

                foreach (var item in exts)
                    SetValue(entity, item.Value, ds.Types[item.Key], dr[item.Key]);

                list.Add(entity);
            }
            return list;
        }

        /// <summary>加载数据表。无数据时返回空集合而不是null。</summary>
        /// <param name="dr">数据读取器</param>
        /// <returns>实体数组</returns>
        public IList<T> LoadData<T>(IDataReader dr) where T : Entity<T>, new()
        {
            // 准备好实体列表
            var list = new List<T>();
            if (dr == null) return list;

            var dr2 = dr as DbDataReader;

            // 对应数据表中字段的实体字段
            var ps = new Dictionary<Int32, FieldItem>();
            // 数据表中找不到对应的实体字段的数据字段
            var exts = new Dictionary<Int32, String>();
            var ti = Entity<T>.Meta.Table;
            for (var i = 0; i < dr2.FieldCount; i++)
            {
                var name = dr2.GetName(i);
                if (ti.FindByName(name) is FieldItem fi)
                    ps.Add(i, fi);
                else if (!name.EqualIgnoreCase(IgnoreFields))
                    exts.Add(i, name);
            }

            // 遍历每一行数据，填充成为实体
            while (dr.Read())
            {
                // 由实体操作者创建实体对象，因为实体操作者可能更换
                var entity = Entity<T>.Meta.Factory.Create() as T;
                foreach (var item in ps)
                    SetValue(entity, item.Value.Name, item.Value.Type, dr[item.Key]);

                foreach (var item in exts)
                    SetValue(entity, item.Value, null, dr[item.Key]);

                list.Add(entity);
            }
            return list;
        }
        #endregion

        #region 方法
        static readonly String[] TrueString = new String[] { "true", "y", "yes", "1" };
        static readonly String[] FalseString = new String[] { "false", "n", "no", "0" };
        static readonly String[] IgnoreFields = new[] { "rowNumber" };

        private void SetValue(IEntity entity, String name, Type type, Object value)
        {
            // 注意：name并不一定是实体类的成员，随便读取原数据可能会造成不必要的麻烦
            Object oldValue = null;
            if (type != null)
                // 仅对精确匹配的字段进行读取旧值
                oldValue = entity[name];
            else
            {
                type = value?.GetType();
                // 如果扩展数据里面有该字段也读取旧值
                if (entity.Extends.ContainsKey(name)) oldValue = entity.Extends[name];
            }

            // 不处理相同数据的赋值
            if (Equals(value, oldValue)) return;

            if (type == typeof(String))
            {
                // 不处理空字符串对空字符串的赋值
                if (value != null && String.IsNullOrEmpty(value.ToString()))
                {
                    if (oldValue == null || String.IsNullOrEmpty(oldValue.ToString())) return;
                }
            }
            else if (type == typeof(Boolean))
            {
                // 处理字符串转为布尔型
                if (value != null && value.GetType() == typeof(String))
                {
                    var vs = value.ToString();
                    if (String.IsNullOrEmpty(vs))
                        value = false;
                    else
                    {
                        if (Array.IndexOf(TrueString, vs.ToLower()) >= 0)
                            value = true;
                        else if (Array.IndexOf(FalseString, vs.ToLower()) >= 0)
                            value = false;
                        else if (DAL.Debug)
                            DAL.WriteLog("无法把字符串{0}转为布尔型！", vs);
                    }
                }
            }
            else if (type == typeof(Guid))
            {
                if (!(value is Guid))
                {
                    if (value is Byte[])
                        value = new Guid((Byte[])value);
                    else if (value is String)
                        value = new Guid((String)value);
                }
            }

            //// 不影响脏数据的状态
            //var ds = entity.Dirtys;
            //Boolean? b = null;
            //if (ds.ContainsKey(name)) b = ds[name];

            entity[name] = value == DBNull.Value ? null : value;

            //if (b != null)
            //    ds[name] = b.Value;
            //else
            //    ds.Remove(name);
        }
        #endregion
    }
}