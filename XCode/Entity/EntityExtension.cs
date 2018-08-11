using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NewLife;
using NewLife.Collections;
using NewLife.Model;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体扩展方法</summary>
    public static class EntityExtension
    {
        #region 泛型实例列表扩展
        /// <summary>根据指定项查找</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.FirstOrDefault()")]
        public static T Find<T>(this IList<T> list, String name, Object value) where T : IEntity
        {
            return list.FirstOrDefault(e => e[name] == value);
        }

        /// <summary>根据指定项查找</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.FirstOrDefault()")]
        public static T FindIgnoreCase<T>(this IList<T> list, String name, String value) where T : IEntity
        {
            return list.FirstOrDefault(e => (e[name] + "").EqualIgnoreCase(value));
        }

        ///// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        ///// <param name="list">实体列表</param>
        ///// <param name="match">条件</param>
        ///// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用list.FirstOrDefault()")]
        //public static T Find<T>(this IList<T> list, Predicate<T> match) where T : IEntity
        //{
        //    return list.FirstOrDefault(e => match(e));
        //}

        /// <summary>根据指定项查找</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.Where()")]
        public static IList<T> FindAll<T>(this IList<T> list, String name, Object value) where T : IEntity
        {
            return list.Where(e => e[name] == value).ToList();
        }

        /// <summary>根据指定项查找</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.Where()")]
        public static IList<T> FindAllIgnoreCase<T>(this IList<T> list, String name, String value) where T : IEntity
        {
            return list.Where(e => (e[name] + "").EqualIgnoreCase(value)).ToList();
        }

        ///// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        ///// <param name="list">实体列表</param>
        ///// <param name="match">条件</param>
        ///// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用list.Where()")]
        //public static IList<T> FindAll<T>(this IList<T> list, Predicate<T> match) where T : IEntity
        //{
        //    return list.Where(e => match(e)).ToList();
        //}

        /// <summary>集合是否包含指定项</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.Any()")]
        public static Boolean Exists<T>(this IList<T> list, String name, Object value) where T : IEntity
        {
            return list.Any(e => e[name] == value);

        }

        /// <summary>实体列表转为字典。主键为Key</summary>
        /// <param name="list">实体列表</param>
        /// <param name="valueField">作为Value部分的字段，默认为空表示整个实体对象为值</param>
        /// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用Linq")]
        public static IDictionary ToDictionary<T>(this IEnumerable<T> list, String valueField = null) where T : IEntity
        {
            if (list == null || !list.Any()) return new Dictionary<String, String>();

            var type = list.First().GetType();
            var fact = EntityFactory.CreateOperate(type);

            // 构造主键类型和值类型
            var key = fact.Unique;
            var ktype = key.Type;

            if (!valueField.IsNullOrEmpty())
            {
                var fi = fact.Table.FindByName(valueField) as FieldItem;
                if (fi == null) throw new XException("无法找到名为{0}的字段", valueField);

                type = fi.Type;
            }

            // 创建字典
            var dic = typeof(Dictionary<,>).MakeGenericType(ktype, type).CreateInstance() as IDictionary;
            foreach (var item in list)
            {
                var k = item[key.Name];
                if (!dic.Contains(k))
                {
                    if (!valueField.IsNullOrEmpty())
                        dic.Add(k, item[valueField]);
                    else
                        dic.Add(k, item);
                }
            }

            return dic;
        }

        /// <summary>从实体对象创建参数</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public static IDataParameter[] CreateParameter<T>(this T entity) where T : IEntity
        {
            var dps = new List<IDataParameter>();
            if (entity == null) return dps.ToArray();

            var type = entity.GetType();
            var fact = EntityFactory.CreateOperate(type);
            var db = fact.Session.Dal.Db;

            foreach (var item in fact.Fields)
            {
                dps.Add(db.CreateParameter(item.ColumnName ?? item.Name, entity[item.Name], item.Field));
            }

            return dps.ToArray();
        }

        /// <summary>从实体列表创建参数</summary>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public static IDataParameter[] CreateParameters<T>(this IEnumerable<T> list) where T : IEntity
        {
            var dps = new List<IDataParameter>();
            if (list == null || !list.Any()) return dps.ToArray();

            var type = list.First().GetType();
            var fact = EntityFactory.CreateOperate(type);
            var db = fact.Session.Dal.Db;

            foreach (var item in fact.Fields)
            {
                var vs = list.Select(e => e[item.Name]).ToArray();
                dps.Add(db.CreateParameter(item.ColumnName ?? item.Name, vs, item.Field));
            }

            return dps.ToArray();
        }
        #endregion

        #region 对象操作
        /// <summary>把整个集合插入到数据库</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Insert<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            // 避免列表内实体对象为空
            var entity = list.FirstOrDefault(e => e != null);
            if (entity == null) return 0;

            var fact = entity.GetType().AsFactory();
            var db = fact.Session.Dal.Db;

            // Oracle参数化批量插入
            if (db.UseParameter && db.Type == DatabaseType.Oracle) return OracleBatchInsert(list, fact);

            // MySql批量插入
            if (db.Type == DatabaseType.MySql) return MySqlBatchInsert(list, fact);

            return DoAction(list, useTransition, e => e.Insert());
        }

        private static Int32 OracleBatchInsert<T>(IEnumerable<T> list, IEntityOperate fact) where T : IEntity
        {
            var db = fact.Session.Dal.Db;
            var ps = ObjectContainer.Current.ResolveInstance<IEntityPersistence>();

            var sql = ps.InsertSQL(fact);
            var dps = new List<IDataParameter>();
            foreach (var fi in fact.Fields)
            {
                // 标识列不需要插入，别的类型都需要
                if (fi.IsIdentity && !fact.AllowInsertIdentity) continue;

                var vs = new List<Object>();
                foreach (var entity in list)
                {
                    (entity as EntityBase).Valid(true);

                    vs.Add(entity[fi.Name]);
                }
                var dp = db.CreateParameter(fi.Name, vs.ToArray(), fi.Field);

                dps.Add(dp);
            }

            return fact.Session.Execute(sql, CommandType.Text, dps.ToArray());
        }

        private static Int32 MySqlBatchInsert<T>(IEnumerable<T> list, IEntityOperate fact) where T : IEntity
        {
            var db = fact.Session.Dal.Db;

            var sbNames = Pool.StringBuilder.Get();
            foreach (var fi in fact.Fields)
            {
                // 标识列不需要插入，别的类型都需要
                if (fi.IsIdentity && !fact.AllowInsertIdentity) continue;

                sbNames.Separate(", ").Append(fact.FormatName(fi.ColumnName));
            }

            var sbValues = Pool.StringBuilder.Get();
            foreach (var entity in list)
            {
                (entity as EntityBase).Valid(true);

                sbValues.Append("(");
                var first = true;
                foreach (var fi in fact.Fields)
                {
                    // 标识列不需要插入，别的类型都需要
                    if (fi.IsIdentity && !fact.AllowInsertIdentity) continue;

                    if (!first) sbValues.Append(",");
                    first = false;

                    var value = entity[fi.Name];
                    //// 1，有脏数据的字段一定要参与同时对于实体有值的也应该参与（针对通过置空主键的方式另存）
                    //if (value == null && !entity.Dirtys[fi.Name])
                    //{
                    //    // 2，没有脏数据，允许空的字段不参与
                    //    // 4，没有脏数据，不允许空，没有默认值的参与，需要智能识别并添加相应字段的默认数据
                    //    if (!fi.IsNullable) value = FormatParamValue(fi, null, op);
                    //}
                    sbValues.Append(fact.FormatValue(fi, value));
                }
                sbValues.Append("),");
            }

            var ns = sbNames.Put(true);
            var vs = sbValues.Put(true).TrimEnd(",");

            var sql = $"Insert Into {fact.FormatedTableName}({ns}) Values{vs}";

            return fact.Session.Execute(sql);
        }

        /// <summary>把整个集合更新到数据库</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Update<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.Update());
        }

        /// <summary>把整个保存更新到数据库</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Save<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.Save());
        }

        /// <summary>把整个保存更新到数据库，保存时不需要验证</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 SaveWithoutValid<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.SaveWithoutValid());
        }

        /// <summary>把整个集合从数据库中删除</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Delete<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.Delete());
        }

        private static Int32 DoAction<T>(this IEnumerable<T> list, Boolean? useTransition, Func<T, Int32> func) where T : IEntity
        {
            if (!list.Any()) return 0;

            // 避免列表内实体对象为空
            var entity = list.First(e => e != null);
            if (entity == null) return 0;

            var fact = EntityFactory.CreateOperate(entity.GetType());

            // SQLite 批操作默认使用事务，其它数据库默认不使用事务
            if (useTransition == null) useTransition = fact.Session.Dal.DbType == DatabaseType.SQLite;

            // 禁用自动关闭连接，提升批操作性能
            //var ss = fact.Session.Dal.Session;
            //ss.SetAutoClose(false);

            var count = 0;
            //try
            //{
            if (useTransition != null && useTransition.Value)
            {
                using (var trans = fact.CreateTrans())
                {
                    count = DoAction(list, func, count);

                    trans.Commit();
                }
            }
            else
            {
                count = DoAction(list, func, count);
            }
            //}
            //finally
            //{
            //    ss.SetAutoClose(null);
            //}

            return count;
        }

        private static Int32 DoAction<T>(this IEnumerable<T> list, Func<T, Int32> func, Int32 count) where T : IEntity
        {
            // 加锁拷贝，避免遍历时出现多线程冲突
            var arr = list is ICollection<T> cs ? cs.ToArray() : list.ToArray();
            foreach (var item in arr)
            {
                if (item != null) count += func(item);
            }
            return count;
        }
        #endregion
    }
}