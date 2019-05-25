using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Data;
using NewLife.IO;
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
        public static Int32 Delete<T>(this IEnumerable<T> list, Boolean? useTransition = null) where T : IEntity
        {
            // 避免列表内实体对象为空
            var entity = list.FirstOrDefault(e => e != null);
            if (entity == null) return 0;

            // 单一主键，采用批量操作
            var fact = entity.GetType().AsFactory();
            var pks = fact.Table.PrimaryKeys;
            if (pks != null && pks.Length == 1)
            {
                var pk = pks[0];
                var count = 0;
                var rs = 0;
                var ks = new List<Object>();
                var sql = $"Delete From {fact.FormatedTableName} Where ";
                foreach (var item in list)
                {
                    ks.Add(item[pk.Name]);
                    count++;

                    // 分批执行
                    if (count >= 1000)
                    {
                        rs += fact.Session.Execute(sql + pk.In(ks));

                        ks.Clear();
                        count = 0;
                    }
                }
                if (count > 0)
                {
                    rs += fact.Session.Execute(sql + pk.In(ks));
                }

                return rs;
            }

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
        /// <returns>
        /// Oracle：当批量插入操作中有一条记录无法正常写入，则本次写入的所有数据都不会被写入（可以理解为自带事物）
        /// MySQL：当批量插入操作中有一条记录无法正常写入，则本次写入的所有数据都不会被写入（可以理解为自带事物）
        /// </returns>
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

                // 每个列要么有脏数据，要么允许空。不允许空又没有脏数据的字段插入没有意义
                //var dirtys = GetDirtyColumns(fact, list.Cast<IEntity>());
                //if (fact.FullInsert)
                //    columns = columns.Where(e => e.Nullable || dirtys.Contains(e.Name)).ToArray();
                //else
                //    columns = columns.Where(e => dirtys.Contains(e.Name)).ToArray();
                if (!fact.FullInsert)
                {
                    var dirtys = GetDirtyColumns(fact, list.Cast<IEntity>());
                    columns = columns.Where(e => dirtys.Contains(e.Name)).ToArray();
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
                var dirtys = GetDirtyColumns(fact, list.Cast<IEntity>());
                // 创建时间等字段不参与Update
                dirtys = dirtys.Where(e => !e.StartsWithIgnoreCase("Create")).ToArray();

                if (dirtys.Length > 0) updateColumns = dirtys;
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
        /// <returns>
        /// MySQL返回值：返回值相当于流程执行次数，及时insert失败也会累计一次执行（所以不建议通过该返回值确定操作记录数）
        /// do insert success = 1次; 
        /// do update success =2次(insert 1次+update 1次)，
        /// 简单来说：对于一行记录，如果Insert 成功则返回1，如果需要执行的是update 则返回2
        /// Oracle返回值：无论是插入还是更新返回的都始终为-1
        /// </returns>
        public static Int32 Upsert<T>(this IEnumerable<T> list, IDataColumn[] columns = null, ICollection<String> updateColumns = null, ICollection<String> addColumns = null) where T : IEntity
        {
            if (list == null || !list.Any()) return 0;

            var entity = list.First();
            var fact = entity.GetType().AsFactory();

            // SqlServer的批量Upsert需要主键参与，哪怕是自增，构建update的where时用到主键
            if (columns == null)
            {
                var dbt = fact.Session.Dal.DbType;
                if (dbt == DatabaseType.SqlServer || dbt == DatabaseType.Oracle)
                    columns = fact.Fields.Select(e => e.Field).Where(e => !e.Identity || e.PrimaryKey).ToArray();
                else if (dbt == DatabaseType.MySql)
                    columns = fact.Fields.Select(e => e.Field).ToArray(); //只有标识键的情况下会导致重复执行insert方法 目前只测试了Mysql库
                else
                    columns = fact.Fields.Select(e => e.Field).Where(e => !e.Identity).ToArray();

                // 每个列要么有脏数据，要么允许空。不允许空又没有脏数据的字段插入没有意义
                //var dirtys = GetDirtyColumns(fact, list.Cast<IEntity>());
                //if (fact.FullInsert)
                //    columns = columns.Where(e => e.Nullable || dirtys.Contains(e.Name)).ToArray();
                //else
                //    columns = columns.Where(e => dirtys.Contains(e.Name)).ToArray();
                if (!fact.FullInsert)
                {
                    var dirtys = GetDirtyColumns(fact, list.Cast<IEntity>());
                    columns = columns.Where(e => e.PrimaryKey || dirtys.Contains(e.Name)).ToArray();
                }
            }
            //if (updateColumns == null) updateColumns = entity.Dirtys.Keys;
            if (updateColumns == null)
            {
                // 所有实体对象的脏字段作为更新字段
                var dirtys = GetDirtyColumns(fact, list.Cast<IEntity>());
                // 创建时间等字段不参与Update
                dirtys = dirtys.Where(e => !e.StartsWithIgnoreCase("Create")).ToArray();

                if (dirtys.Length > 0) updateColumns = dirtys;
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
        /// <param name="entity">实体对象</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="updateColumns">主键已存在时，要更新的字段</param>
        /// <param name="addColumns">主键已存在时，要累加更新的字段</param>
        /// <returns>
        /// MySQL返回值：返回值相当于流程执行次数，及时insert失败也会累计一次执行（所以不建议通过该返回值确定操作记录数）
        /// do insert success = 1次; 
        /// do update success =2次(insert 1次+update 1次)，
        /// 简单来说：如果Insert 成功则返回1，如果需要执行的是update 则返回2，
        /// </returns>
        public static Int32 Upsert(this IEntity entity, IDataColumn[] columns = null, ICollection<String> updateColumns = null, ICollection<String> addColumns = null)
        {
            var fact = entity.GetType().AsFactory();
            if (columns == null)
            {
                columns = fact.Fields.Select(e => e.Field).Where(e => !e.Identity).ToArray();

                // 每个列要么有脏数据，要么允许空。不允许空又没有脏数据的字段插入没有意义
                //var dirtys = GetDirtyColumns(fact, new[] { entity });
                //if (fact.FullInsert)
                //    columns = columns.Where(e => e.Nullable || dirtys.Contains(e.Name)).ToArray();
                //else
                //    columns = columns.Where(e => dirtys.Contains(e.Name)).ToArray();
                if (!fact.FullInsert)
                {
                    var dirtys = GetDirtyColumns(fact, new[] { entity });
                    columns = columns.Where(e => e.PrimaryKey || dirtys.Contains(e.Name)).ToArray();
                }
            }
            if (updateColumns == null) updateColumns = entity.Dirtys.Where(e => !e.StartsWithIgnoreCase("Create")).Distinct().ToArray();
            if (addColumns == null) addColumns = fact.AdditionalFields;

            var session = fact.Session;
            session.InitData();
            session.Dal.CheckDatabase();

            return fact.Session.Dal.Session.Upsert(session.TableName, columns, updateColumns, addColumns, new[] { entity as IIndexAccessor });
        }

        /// <summary>获取脏数据列</summary>
        /// <param name="fact"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        private static String[] GetDirtyColumns(IEntityOperate fact, IEnumerable<IEntity> list)
        {
            //var fact = list.FirstOrDefault().GetType().AsFactory();

            // 获取所有带有脏数据的字段
            var ns = new List<String>();
            foreach (var entity in list)
            {
                foreach (var fi in fact.Fields)
                {
                    if (!ns.Contains(fi.Name) && entity.Dirtys[fi.Name])
                    {
                        ns.Add(fi.Name);
                    }
                }
            }

            return ns.ToArray();
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

        /// <summary>写入文件，二进制格式</summary>
        /// <param name="list">实体列表</param>
        /// <param name="file">文件</param>
        /// <returns></returns>
        public static Int64 SaveFile<T>(this IEnumerable<T> list, String file) where T : IEntity
        {
            if (list == null) return 0;

            // 确保创建目录
            file.EnsureDirectory(true);

            using (var fs = new FileStream(file.GetFullPath(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                foreach (var item in list)
                {
                    (item as IAccessor).Write(fs, null);
                }

                fs.SetLength(fs.Position);
                return fs.Position;
            }
        }

        /// <summary>写入数据流，Csv格式</summary>
        /// <param name="list">实体列表</param>
        /// <param name="stream">数据量</param>
        /// <param name="displayName">是否使用中文显示名，否则使用英文属性名</param>
        /// <returns></returns>
        public static Int64 SaveCsv<T>(this IEnumerable<T> list, Stream stream, Boolean displayName = false) where T : IEntity
        {
            if (list == null) return 0;

            var p = stream.Position;
            var fact = typeof(T).AsFactory();
            using (var csv = new CsvFile(stream, true))
            {
                var fs = fact.Fields;
                if (displayName)
                    csv.WriteLine(fs.Select(e => e.DisplayName));
                else
                    csv.WriteLine(fs.Select(e => e.Name));
                foreach (var entity in list)
                {
                    csv.WriteLine(fs.Select(e => entity[e.Name]));
                }
            }

            return stream.Position - p;
        }

        /// <summary>写入文件，Csv格式</summary>
        /// <param name="list">实体列表</param>
        /// <param name="file">文件</param>
        /// <param name="displayName">是否使用中文显示名，否则使用英文属性名</param>
        /// <returns></returns>
        public static Int64 SaveCsv<T>(this IEnumerable<T> list, String file, Boolean displayName = false) where T : IEntity
        {
            if (list == null) return 0;

            // 确保创建目录
            file.EnsureDirectory(true);

            using (var fs = new FileStream(file.GetFullPath(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                SaveCsv(list, fs, displayName);

                fs.SetLength(fs.Position);
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

        /// <summary>从文件读取列表，二进制格式</summary>
        /// <param name="list">实体列表</param>
        /// <param name="file">文件</param>
        /// <returns>实体列表</returns>
        public static IList<T> LoadFile<T>(this IList<T> list, String file) where T : IEntity
        {
            if (file.IsNullOrEmpty()) return list;
            file = file.GetFullPath();
            if (!File.Exists(file)) return list;

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

        /// <summary>从数据流读取列表，Csv格式</summary>
        /// <param name="list">实体列表</param>
        /// <param name="stream">数据流</param>
        /// <returns>实体列表</returns>
        public static IList<T> LoadCsv<T>(this IList<T> list, Stream stream) where T : IEntity
        {
            var fact = typeof(T).AsFactory();
            using (var csv = new CsvFile(stream, true))
            {
                // 匹配字段
                var names = csv.ReadLine();
                var fields = new FieldItem[names.Length];
                for (var i = 0; i < names.Length; i++)
                {
                    fields[i] = fact.Fields.FirstOrDefault(e => names[i].EqualIgnoreCase(e.Name, e.DisplayName, e.ColumnName));
                }

                // 读取数据
                while (true)
                {
                    var line = csv.ReadLine();
                    if (line == null || line.Length == 0) break;

                    var entity = (T)fact.Create();
                    for (var i = 0; i < fields.Length; i++)
                    {
                        var fi = fields[i];
                        if (fi != null && !line[i].IsNullOrEmpty()) entity[fi.Name] = line[i].ChangeType(fi.Type);
                    }

                    list.Add(entity);
                }
            }

            return list;
        }

        /// <summary>从文件读取列表，Csv格式</summary>
        /// <param name="list">实体列表</param>
        /// <param name="file">文件</param>
        /// <returns>实体列表</returns>
        public static IList<T> LoadCsv<T>(this IList<T> list, String file) where T : IEntity
        {
            if (file.IsNullOrEmpty()) return list;
            file = file.GetFullPath();
            if (!File.Exists(file)) return list;

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return LoadCsv(list, fs);
            }
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