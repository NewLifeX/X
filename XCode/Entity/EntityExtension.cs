using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;
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

            if (list.Count() > 1)
            {
                var fact = entity.GetType().AsFactory();
                var db = fact.Session.Dal;

                // Oracle/MySql批量插入
                if (db.SupportBatch)
                {
                    if (!(list is IList<T> es)) es = list.ToList();
                    foreach (IEntity item in es.ToArray())
                    {
                        if (item is EntityBase entity2) entity2.Valid(item.IsNullKey);
                        if (!fact.Modules.Valid(item, item.IsNullKey)) es.Remove((T)item);
                    }
                    return BatchInsert(list);
                }
            }

            return DoAction(list, useTransition, e => e.Insert());
        }

        /// <summary>把整个集合更新到数据库</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Update<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            // 避免列表内实体对象为空
            var entity = list.FirstOrDefault(e => e != null);
            if (entity == null) return 0;

            if (list.Count() > 1)
            {
                var fact = entity.GetType().AsFactory();
                var db = fact.Session.Dal;

                // Oracle批量更新
                if (db.DbType == DatabaseType.Oracle) return BatchUpdate(list.Valid());
            }

            return DoAction(list, useTransition, e => e.Update());
        }

        /// <summary>把整个保存更新到数据库</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Save<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            /*
           * Save的几个场景：
           * 1，Find, Update()
           * 2，new, Insert()
           * 3，new, Upsert()
           */

            // 避免列表内实体对象为空
            var entity = list.FirstOrDefault(e => e != null);
            if (entity == null) return 0;

            var rs = 0;
            if (list.Any())
            {
                var fact = entity.GetType().AsFactory();
                var db = fact.Session.Dal;

                // Oracle/MySql批量插入
                if (db.SupportBatch)
                {
                    // 根据是否来自数据库，拆分为两组
                    var ts = Split(list);
                    list = ts.Item1;
                    rs += BatchSave(fact, ts.Item2.Valid());
                }
            }

            return rs + DoAction(list, useTransition, e => e.Save());
        }

        /// <summary>把整个保存更新到数据库，保存时不需要验证</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 SaveWithoutValid<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            // 避免列表内实体对象为空
            var entity = list.FirstOrDefault(e => e != null);
            if (entity == null) return 0;

            var rs = 0;
            if (list.Any())
            {
                var fact = entity.GetType().AsFactory();
                var db = fact.Session.Dal;

                // Oracle/MySql批量插入
                if (db.SupportBatch)
                {
                    // 根据是否来自数据库，拆分为两组
                    var ts = Split(list);
                    list = ts.Item1;
                    rs += BatchSave(fact, ts.Item2);
                }
            }

            return rs + DoAction(list, useTransition, e => e.SaveWithoutValid());
        }

        private static Tuple<IList<T>, IList<T>> Split<T>(IEnumerable<T> list) where T : IEntity
        {
            var updates = new List<T>();
            var others = new List<T>();
            foreach (var item in list)
            {
                if (item.IsFromDatabase)
                    updates.Add(item);
                else
                    others.Add(item);
            }

            return new Tuple<IList<T>, IList<T>>(updates, others);
        }

        private static Int32 BatchSave<T>(IEntityOperate fact, IEnumerable<T> list) where T : IEntity
        {
            // 没有其它唯一索引，且主键为空时，走批量插入
            var rs = 0;
            if (!fact.Table.DataTable.Indexes.Any(di => di.Unique))
            {
                var inserts = new List<T>();
                var updates = new List<T>();
                var upserts = new List<T>();
                foreach (var item in list)
                {
                    // 来自数据库，更新
                    if (item.IsFromDatabase)
                        updates.Add(item);
                    // 空主键，插入
                    else if (item.IsNullKey)
                        inserts.Add(item);
                    // 其它 Upsert
                    else
                        upserts.Add(item);
                }
                list = upserts;

                if (inserts.Count > 0) rs += BatchInsert(inserts);
                if (updates.Count > 0)
                {
                    // 只有Oracle支持批量Update
                    if (fact.Session.Dal.DbType == DatabaseType.Oracle)
                        rs += BatchUpdate(updates);
                    else
                        upserts.AddRange(upserts);
                }
            }

            if (list.Any()) rs += Upsert(list);

            return rs;
        }

        /// <summary>把整个集合从数据库中删除</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Delete<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity => DoAction(list, useTransition, e => e.Delete());

        private static Int32 DoAction<T>(this IEnumerable<T> list, Boolean? useTransition, Func<T, Int32> func) where T : IEntity
        {
            if (!list.Any()) return 0;

            // 避免列表内实体对象为空
            var entity = list.First(e => e != null);
            if (entity == null) return 0;

            var fact = EntityFactory.CreateOperate(entity.GetType());

            // SQLite 批操作默认使用事务，其它数据库默认不使用事务
            if (useTransition == null) useTransition = fact.Session.Dal.DbType == DatabaseType.SQLite;

            var count = 0;
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

        /// <summary>批量验证对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IList<T> Valid<T>(this IEnumerable<T> list) where T : IEntity
        {
            var rs = new List<T>();

            var entity = list.FirstOrDefault(e => e != null);
            if (entity == null) return rs;

            var fact = entity.GetType().AsFactory();
            var modules = fact.Modules;

            // 验证对象
            foreach (IEntity item in list)
            {
                if (item is EntityBase entity2) entity2.Valid(item.IsNullKey);
                if (modules.Valid(item, item.IsNullKey)) rs.Add((T)item);
            }

            return rs;
        }
        #endregion

        #region 批量更新
        /// <summary>批量插入</summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <returns></returns>
        public static Int32 BatchInsert<T>(this IEnumerable<T> list, IDataColumn[] columns = null) where T : IEntity
        {
            if (list == null || !list.Any()) return 0;

            var entity = list.First();
            var fact = entity.GetType().AsFactory();
            if (columns == null)
            {
                columns = fact.Fields.Select(e => e.Field).ToArray();

                // 第一列数据包含非零自增，表示要插入自增值
                var id = columns.FirstOrDefault(e => e.Identity);
                if (id != null)
                {
                    if (entity[id.Name].ToLong() == 0) columns = columns.Where(e => !e.Identity).ToArray();
                }
            }

            var session = fact.Session;
            session.InitData();
            session.Dal.CheckDatabase();

            return session.Dal.Session.Insert(session.TableName, columns, list.Cast<IIndexAccessor>());
        }

        /// <summary>批量更新</summary>
        /// <remarks>
        /// 注意类似：XCode.Exceptions.XSqlException: ORA-00933: SQL 命令未正确结束
        /// [SQL:Update tablen_Name Set FieldName=:FieldName W [:FieldName=System.Int32[]]][DB:AAA/Oracle]
        /// 建议是优先检查表是否存在主键，如果由于没有主键导致，及时通过try...cache 依旧无法正常保存。
        /// </remarks>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="columns">要更新的字段，默认所有字段</param>
        /// <param name="updateColumns">要更新的字段，默认脏数据</param>
        /// <param name="addColumns">要累加更新的字段，默认累加</param>
        /// <returns></returns>
        public static Int32 BatchUpdate<T>(this IEnumerable<T> list, IDataColumn[] columns = null, ICollection<String> updateColumns = null, ICollection<String> addColumns = null) where T : IEntity
        {
            if (list == null || !list.Any()) return 0;

            var entity = list.First();
            var fact = entity.GetType().AsFactory();
            if (columns == null) columns = fact.Fields.Select(e => e.Field).Where(e => !e.Identity).ToArray();
            //if (updateColumns == null) updateColumns = entity.Dirtys.Keys;
            if (updateColumns == null)
            {
                // 所有实体对象的脏字段作为更新字段
                var hs = new HashSet<String>();
                foreach (var item in list)
                {
                    foreach (var elm in item.Dirtys)
                    {
                        // 创建时间等字段不参与Update
                        if (elm.StartsWithIgnoreCase("Create")) continue;

                        if (!hs.Contains(elm)) hs.Add(elm);
                    }
                }
                if (hs.Count > 0) updateColumns = hs;
            }
            if (addColumns == null) addColumns = fact.AdditionalFields;

            if ((updateColumns == null || updateColumns.Count < 1) && (addColumns == null || addColumns.Count < 1)) return 0;

            var session = fact.Session;
            session.InitData();
            session.Dal.CheckDatabase();

            return session.Dal.Session.Update(session.TableName, columns, updateColumns, addColumns, list.Cast<IIndexAccessor>());
        }

        /// <summary>批量插入或更新</summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="updateColumns">要更新的字段，默认脏数据</param>
        /// <param name="addColumns">要累加更新的字段，默认累加</param>
        /// <returns></returns>
        public static Int32 Upsert<T>(this IEnumerable<T> list, IDataColumn[] columns = null, ICollection<String> updateColumns = null, ICollection<String> addColumns = null) where T : IEntity
        {
            if (list == null || !list.Any()) return 0;

            var entity = list.First();
            var fact = entity.GetType().AsFactory();
            if (columns == null) columns = fact.Fields.Select(e => e.Field).Where(e => !e.Identity).ToArray();
            //if (updateColumns == null) updateColumns = entity.Dirtys.Keys;
            if (updateColumns == null)
            {
                // 所有实体对象的脏字段作为更新字段
                var hs = new HashSet<String>();
                foreach (var item in list)
                {
                    foreach (var elm in item.Dirtys)
                    {
                        // 创建时间等字段不参与Update
                        if (elm.StartsWithIgnoreCase("Create")) continue;

                        if (!hs.Contains(elm)) hs.Add(elm);
                    }
                }
                if (hs.Count > 0) updateColumns = hs;
            }
            if (addColumns == null) addColumns = fact.AdditionalFields;
            // 没有任何数据变更则直接返回0
            if ((updateColumns == null || updateColumns.Count <= 0) && (addColumns == null || addColumns.Count <= 0)) return 0;

            var session = fact.Session;
            session.InitData();
            session.Dal.CheckDatabase();

            return session.Dal.Session.Upsert(session.TableName, columns, updateColumns, addColumns, list.Cast<IIndexAccessor>());
        }

        /// <summary>批量插入或更新</summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="updateColumns">要更新的字段，默认脏数据</param>
        /// <param name="addColumns">要累加更新的字段，默认累加</param>
        /// <returns></returns>
        [Obsolete("请改用list.Upsert()")]
        public static Int32 InsertOrUpdate<T>(this IEnumerable<T> list, IDataColumn[] columns = null, ICollection<String> updateColumns = null, ICollection<String> addColumns = null) where T : IEntity
        {
            return Upsert(list, columns, updateColumns, addColumns);
        }

        /// <summary>批量插入或更新</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="updateColumns">主键已存在时，要更新的字段</param>
        /// <param name="addColumns">主键已存在时，要累加更新的字段</param>
        /// <returns></returns>
        public static Int32 Upsert(this IEntity entity, IDataColumn[] columns = null, ICollection<String> updateColumns = null, ICollection<String> addColumns = null)
        {
            var fact = entity.GetType().AsFactory();
            if (columns == null) columns = fact.Fields.Select(e => e.Field).Where(e => !e.Identity).ToArray();
            if (updateColumns == null) updateColumns = entity.Dirtys.Where(e => !e.StartsWithIgnoreCase("Create")).Distinct().ToArray();
            if (addColumns == null) addColumns = fact.AdditionalFields;

            var session = fact.Session;
            session.InitData();
            session.Dal.CheckDatabase();

            return fact.Session.Dal.Session.Upsert(session.TableName, columns, updateColumns, addColumns, new[] { entity as IIndexAccessor });
        }
        #endregion

        #region 读写数据流
        /// <summary>转为DbTable</summary>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public static DbTable ToTable<T>(this IEnumerable<T> list) where T : IEntity
        {
            var entity = list.FirstOrDefault();
            if (entity == null) return null;

            var fact = entity.GetType().AsFactory();
            var fs = fact.Fields;

            var count = fs.Length;
            var dt = new DbTable
            {
                Columns = new String[count],
                Types = new Type[count],
                Rows = new List<Object[]>(),
            };
            for (var i = 0; i < fs.Length; i++)
            {
                var fi = fs[i];
                dt.Columns[i] = fi.Name;
                dt.Types[i] = fi.Type;
            }

            foreach (var item in list)
            {
                var dr = new Object[count];
                for (var i = 0; i < fs.Length; i++)
                {
                    var fi = fs[i];
                    dr[i] = item[fi.Name];
                }
                dt.Rows.Add(dr);
            }

            return dt;
        }

        /// <summary>写入数据流</summary>
        /// <param name="list">实体列表</param>
        /// <param name="stream">数据流</param>
        /// <returns></returns>
        public static Int64 Write<T>(this IEnumerable<T> list, Stream stream) where T : IEntity
        {
            if (list == null) return 0;

            var p = stream.Position;
            foreach (var item in list)
            {
                (item as IAccessor).Write(stream, null);
            }

            return stream.Position - p;
        }

        /// <summary>写入数据流</summary>
        /// <param name="list">实体列表</param>
        /// <param name="file">文件</param>
        /// <returns></returns>
        public static Int64 SaveFile<T>(this IEnumerable<T> list, String file) where T : IEntity
        {
            if (list == null) return 0;

            // 确保创建目录
            file.EnsureDirectory(true);

            using (var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                foreach (var item in list)
                {
                    (item as IAccessor).Write(fs, null);
                }

                return fs.Position;
            }
        }

        /// <summary>从数据流读取列表</summary>
        /// <param name="list">实体列表</param>
        /// <param name="stream">数据流</param>
        /// <returns>实体列表</returns>
        public static IList<T> Read<T>(this IList<T> list, Stream stream) where T : IEntity
        {
            if (stream == null) return list;

            var fact = typeof(T).AsFactory();
            while (stream.Position < stream.Length)
            {
                var entity = (T)fact.Create();
                (entity as IAccessor).Read(stream, null);

                list.Add(entity);
            }

            return list;
        }

        /// <summary>从数据流读取列表</summary>
        /// <param name="list">实体列表</param>
        /// <param name="stream">数据流</param>
        /// <returns>实体列表</returns>
        public static IEnumerable<T> ReadEnumerable<T>(this IList<T> list, Stream stream) where T : IEntity
        {
            if (stream == null) yield break;

            var fact = typeof(T).AsFactory();
            while (stream.Position < stream.Length)
            {
                var entity = (T)fact.Create();
                (entity as IAccessor).Read(stream, null);

                list.Add(entity);

                yield return entity;
            }
        }

        /// <summary>从数据流读取列表</summary>
        /// <param name="list">实体列表</param>
        /// <param name="file">文件</param>
        /// <returns>实体列表</returns>
        public static IList<T> LoadFile<T>(this IList<T> list, String file) where T : IEntity
        {
            if (file.IsNullOrEmpty() || !File.Exists(file)) return list;

            var fact = typeof(T).AsFactory();
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                while (fs.Position < fs.Length)
                {
                    var entity = (T)fact.Create();
                    (entity as IAccessor).Read(fs, null);

                    list.Add(entity);
                }
            }

            return list;
        }
        #endregion

        #region 转 DataTable/DataSet
        /// <summary>转为DataTable</summary>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this IEnumerable<T> list) where T : IEntity
        {
            var entity = list.FirstOrDefault();
            if (entity == null) return null;

            var fact = entity.GetType().AsFactory();

            var dt = new DataTable();
            foreach (var fi in fact.Fields)
            {
                var dc = new DataColumn
                {
                    ColumnName = fi.Name,
                    DataType = fi.Type,
                    Caption = fi.Description,
                    AutoIncrement = fi.IsIdentity
                };

                // 关闭这两项，让DataTable宽松一点
                //dc.Unique = item.PrimaryKey;
                //dc.AllowDBNull = item.IsNullable;

                dt.Columns.Add(dc);
            }

            foreach (var item in list)
            {
                var dr = dt.NewRow();
                foreach (var fi in fact.Fields)
                {
                    dr[fi.Name] = item[fi.Name];
                }
                dt.Rows.Add(dr);
            }

            return dt;
        }

        /// <summary>转为DataSet</summary>
        /// <returns></returns>
        public static DataSet ToDataSet<T>(this IEnumerable<T> list) where T : IEntity
        {
            var ds = new DataSet();
            ds.Tables.Add(ToDataTable(list));
            return ds;
        }
        #endregion
    }
}