using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using NewLife;
using NewLife.Collections;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体持久化接口。可通过实现该接口来自定义实体类持久化行为。</summary>
    public interface IEntityPersistence
    {
        #region 属性
        /// <summary>实体工厂</summary>
        IEntityFactory Factory { get; }
        #endregion

        #region 添删改方法
        /// <summary>插入</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        Int32 Insert(IEntitySession session, IEntity entity);

        /// <summary>更新</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        Int32 Update(IEntitySession session, IEntity entity);

        /// <summary>删除</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        Int32 Delete(IEntitySession session, IEntity entity);

        /// <summary>把一个实体对象持久化到数据库</summary>
        /// <param name="session">实体会话</param>
        /// <param name="names">更新属性列表</param>
        /// <param name="values">更新值列表</param>
        /// <returns>返回受影响的行数</returns>
        Int32 Insert(IEntitySession session, String[] names, Object[] values);

        /// <summary>更新一批实体数据</summary>
        /// <param name="session">实体会话</param>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        Int32 Update(IEntitySession session, String setClause, String whereClause);

        /// <summary>更新一批实体数据</summary>
        /// <param name="session">实体会话</param>
        /// <param name="setNames">更新属性列表</param>
        /// <param name="setValues">更新值列表</param>
        /// <param name="whereNames">条件属性列表</param>
        /// <param name="whereValues">条件值列表</param>
        /// <returns>返回受影响的行数</returns>
        Int32 Update(IEntitySession session, String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues);

        /// <summary>从数据库中删除指定条件的实体对象。</summary>
        /// <param name="session">实体会话</param>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        Int32 Delete(IEntitySession session, String whereClause);

        /// <summary>从数据库中删除指定属性列表和值列表所限定的实体对象。</summary>
        /// <param name="session">实体会话</param>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns></returns>
        Int32 Delete(IEntitySession session, String[] names, Object[] values);
        #endregion

        #region 获取语句
        /// <summary>获取主键条件</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        WhereExpression GetPrimaryCondition(IEntity entity);

        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <returns>SQL字符串</returns>
        String GetSql(IEntitySession session, IEntity entity, DataObjectMethodType methodType);
        #endregion

        #region 参数化
        /// <summary>插入语句</summary>
        /// <param name="session">实体会话</param>
        /// <returns></returns>
        String InsertSQL(IEntitySession session);
        #endregion
    }

    /// <summary>默认实体持久化</summary>
    public class EntityPersistence : IEntityPersistence
    {
        #region 属性
        /// <summary>实体工厂</summary>
        public IEntityFactory Factory { get; set; }
        #endregion

        #region 添删改方法
        /// <summary>插入</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        public virtual Int32 Insert(IEntitySession session, IEntity entity)
        {
            var factory = Factory;

            // 添加数据前，处理Guid
            SetGuidField(factory.AutoSetGuidField, entity);

            IDataParameter[] dps = null;
            var sql = SQL(session, entity, DataObjectMethodType.Insert, ref dps);
            if (String.IsNullOrEmpty(sql)) return 0;

            var rs = 0;

            //检查是否有标识列，标识列需要特殊处理
            var field = factory.Table.Identity;
            var bAllow = factory.AllowInsertIdentity;
            if (field != null && field.IsIdentity && !bAllow && factory.AutoIdentity)
            {
                var id = session.InsertAndGetIdentity(sql, CommandType.Text, dps);
                if (id > 0) entity[field.Name] = id;
                rs = id > 0 ? 1 : 0;
            }
            else
            {
                if (bAllow)
                {
                    var dal = DAL.Create(factory.ConnName);
                    if (dal.DbType == DatabaseType.SqlServer)
                    {
                        // 如果所有字段都不是自增，则取消对自增的处理
                        if (factory.Fields.All(f => !f.IsIdentity)) bAllow = false;
                        if (bAllow) sql = $"SET IDENTITY_INSERT {session.FormatedTableName} ON;{sql};SET IDENTITY_INSERT {session.FormatedTableName} OFF";
                    }
                }
                rs = session.Execute(sql, CommandType.Text, dps);
            }

            // 清除脏数据，避免连续两次调用Save造成重复提交
            entity.Dirtys.Clear();

            return rs;
        }

        void SetGuidField(FieldItem fi, IEntity entity)
        {
            if (fi != null)
            {
                // 判断是否设置了数据
                if (!entity.IsDirty(fi.Name))
                {
                    // 如果没有设置，这里给它设置
                    if (fi.Type == typeof(Guid))
                        entity.SetItem(fi.Name, Guid.NewGuid());
                    else
                        entity.SetItem(fi.Name, Guid.NewGuid().ToString());
                }
            }
        }

        /// <summary>更新</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        public virtual Int32 Update(IEntitySession session, IEntity entity)
        {
            // 没有脏数据，不需要更新
            if (!entity.HasDirty) return 0;

            IDataParameter[] dps = null;
            var sql = "";

            // 双锁判断脏数据
            lock (entity)
            {
                if (!entity.HasDirty) return 0;

                sql = SQL(session, entity, DataObjectMethodType.Update, ref dps);
                if (sql.IsNullOrEmpty()) return 0;

                //清除脏数据，避免重复提交
                entity.Dirtys.Clear();
            }

            var rs = session.Execute(sql, CommandType.Text, dps);

            //EntityAddition.ClearValues(entity as EntityBase);

            return rs;
        }

        /// <summary>删除</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntitySession session, IEntity entity)
        {
            IDataParameter[] dps = null;
            var sql = SQL(session, entity, DataObjectMethodType.Delete, ref dps);
            if (String.IsNullOrEmpty(sql)) return 0;

            var rs = session.Execute(sql, CommandType.Text, dps);

            // 清除脏数据，避免重复提交保存
            entity.Dirtys.Clear();

            return rs;
        }

        /// <summary>把一个实体对象持久化到数据库</summary>
        /// <param name="session">实体会话</param>
        /// <param name="names">更新属性列表</param>
        /// <param name="values">更新值列表</param>
        /// <returns>返回受影响的行数</returns>
        public virtual Int32 Insert(IEntitySession session, String[] names, Object[] values)
        {
            if (names == null) throw new ArgumentNullException(nameof(names), "属性列表和值列表不能为空");
            if (values == null) throw new ArgumentNullException(nameof(values), "属性列表和值列表不能为空");
            if (names.Length != values.Length) throw new ArgumentException("属性列表必须和值列表一一对应");

            var factory = Factory;
            var db = session.Dal.Db;
            var fs = new Dictionary<String, FieldItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var fi in factory.Fields)
                fs.Add(fi.Name, fi);
            var sbn = Pool.StringBuilder.Get();
            var sbv = Pool.StringBuilder.Get();
            for (var i = 0; i < names.Length; i++)
            {
                if (!fs.ContainsKey(names[i])) throw new ArgumentException("类[" + factory.EntityType.FullName + "]中不存在[" + names[i] + "]属性");
                // 同时构造SQL语句。names是属性列表，必须转换成对应的字段列表
                if (i > 0)
                {
                    sbn.Append(", ");
                    sbv.Append(", ");
                }

                var column = fs[names[i]].Field;
                sbn.Append(db.FormatName(column));
                //sbv.Append(SqlDataFormat(values[i], fs[names[i]]));
                sbv.Append(db.FormatValue(column, values[i]));
            }
            var sn = sbn.Put(true);
            var sv = sbv.Put(true);
            return session.Execute($"Insert Into {session.FormatedTableName}({sn}) values({sv})");
        }

        /// <summary>更新一批实体数据</summary>
        /// <param name="session">实体会话</param>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        public virtual Int32 Update(IEntitySession session, String setClause, String whereClause)
        {
            if (setClause.IsNullOrEmpty() || !setClause.Contains("=") || setClause.ToLower().Contains(" or ")) throw new ArgumentException("非法参数");

            var sql = $"Update {session.FormatedTableName} Set {setClause.Replace("And", ",")}";
            if (!String.IsNullOrEmpty(whereClause)) sql += " Where " + whereClause;
            return session.Execute(sql);
        }

        /// <summary>更新一批实体数据</summary>
        /// <param name="session">实体会话</param>
        /// <param name="setNames">更新属性列表</param>
        /// <param name="setValues">更新值列表</param>
        /// <param name="whereNames">条件属性列表</param>
        /// <param name="whereValues">条件值列表</param>
        /// <returns>返回受影响的行数</returns>
        public virtual Int32 Update(IEntitySession session, String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues)
        {
            var sc = Join(session, setNames, setValues, ", ");
            var wc = Join(session, whereNames, whereValues, " And ");
            return Update(session, sc, wc);
        }

        /// <summary>从数据库中删除指定条件的实体对象。</summary>
        /// <param name="session">实体会话</param>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntitySession session, String whereClause)
        {
            var sql = $"Delete From {session.FormatedTableName}";
            if (!whereClause.IsNullOrEmpty()) sql += " Where " + whereClause;
            return session.Execute(sql);
        }

        /// <summary>从数据库中删除指定属性列表和值列表所限定的实体对象。</summary>
        /// <param name="session">实体会话</param>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntitySession session, String[] names, Object[] values) => Delete(session, Join(session, names, values, "And"));

        private String Join(IEntitySession session, String[] names, Object[] values, String split)
        {
            var factory = Factory;
            var db = session.Dal.Db;
            var fs = new Dictionary<String, FieldItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var fi in factory.Fields)
                fs.Add(fi.Name, fi);

            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < names.Length; i++)
            {
                if (!fs.ContainsKey(names[i])) throw new ArgumentException("类[" + factory.EntityType.FullName + "]中不存在[" + names[i] + "]属性");

                if (i > 0) sb.AppendFormat(" {0} ", split);

                var column = fs[names[i]].Field;
                sb.Append(db.FormatName(column));
                sb.Append('=');
                sb.Append(db.FormatValue(column, values[i]));
            }

            return sb.Put(true);
        }
        #endregion

        #region 获取语句
        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <returns>SQL字符串</returns>
        public virtual String GetSql(IEntitySession session, IEntity entity, DataObjectMethodType methodType)
        {
            IDataParameter[] dps = null;
            return SQL(session, entity, methodType, ref dps);
        }

        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="session">实体会话</param>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <param name="parameters">参数数组</param>
        /// <returns>SQL字符串</returns>
        String SQL(IEntitySession session, IEntity entity, DataObjectMethodType methodType, ref IDataParameter[] parameters)
        {
            return methodType switch
            {
                DataObjectMethodType.Insert => InsertSQL(session, entity, ref parameters),
                DataObjectMethodType.Update => UpdateSQL(session, entity, ref parameters),
                DataObjectMethodType.Delete => DeleteSQL(session, entity, ref parameters),
                _ => null,
            };
        }

        String InsertSQL(IEntitySession session, IEntity entity, ref IDataParameter[] parameters)
        {
            var factory = Factory;
            var db = session.Dal.Db;

            /*
            * 插入数据原则：
            * 1，有脏数据的字段一定要参与
            * 2，没有脏数据，允许空的字段不参与
            * 3，没有脏数据，不允许空，有默认值的不参与
            * 4，没有脏数据，不允许空，没有默认值的参与，需要智能识别并添加相应字段的默认数据
            */

            var sbNames = Pool.StringBuilder.Get();
            var sbValues = Pool.StringBuilder.Get();

            var dps = new List<IDataParameter>();
            // 只读列没有插入操作
            foreach (var fi in factory.Fields)
            {
                var value = entity[fi.Name];
                // 标识列不需要插入，别的类型都需要
                if (CheckIdentity(db, fi, value, sbNames, sbValues)) continue;

                // 1，有脏数据的字段一定要参与
                if (!entity.IsDirty(fi.Name))
                {
                    if (!factory.FullInsert) continue;

                    //// 不允许空时，插入空值没有意义
                    //if (!fi.IsNullable) continue;
                }

                sbNames.Separate(",").Append(db.FormatName(fi.Field));
                sbValues.Separate(",");

                if (db.UseParameter || UseParam(fi, value))
                {
                    var dp = CreateParameter(db, fi, value);
                    dps.Add(dp);

                    sbValues.Append(dp.ParameterName);
                }
                else
                    sbValues.Append(db.FormatValue(fi.Field, value));
            }

            var ns = sbNames.Put(true);
            var vs = sbValues.Put(true);
            if (ns.IsNullOrEmpty()) return null;

            if (dps.Count > 0) parameters = dps.ToArray();

            return $"Insert Into {session.FormatedTableName}({ns}) Values({vs})";
        }

        Boolean CheckIdentity(IDatabase db, FieldItem fi, Object value, StringBuilder sbNames, StringBuilder sbValues)
        {
            if (!fi.IsIdentity) return false;

            // 有些时候需要向自增字段插入数据，这里特殊处理
            String idv = null;
            var factory = Factory;
            if (factory.AllowInsertIdentity)
                idv = "" + value;
            //else
            //    idv = DAL.Create(op.ConnName).Db.FormatIdentity(fi.Field, value);
            //if (String.IsNullOrEmpty(idv)) continue;
            // 允许返回String.Empty作为插入空
            if (idv == null) return true;

            sbNames.Separate(", ").Append(db.FormatName(fi.Field));
            sbValues.Separate(", ");

            sbValues.Append(idv);

            return true;
        }

        String UpdateSQL(IEntitySession session, IEntity entity, ref IDataParameter[] parameters)
        {
            /*
             * 实体更新原则：
             * 1，自增不参与
             * 2，没有脏数据不参与
             * 3，大字段参数化特殊处理
             * 4，累加字段特殊处理
             */

            var factory = Factory;
            var db = session.Dal.Db;

            var exp = GetPrimaryCondition(entity);
            var ps = !db.UseParameter ? null : new Dictionary<String, Object>();
            var def = exp?.GetString(db, ps);
            if (def.IsNullOrEmpty()) return null;

            // 处理累加字段
            var dfs = entity.Addition?.Get();

            var sb = Pool.StringBuilder.Get();
            var dps = new List<IDataParameter>();
            // 只读列没有更新操作
            foreach (var fi in factory.Fields)
            {
                if (fi.IsIdentity) continue;

                //脏数据判断
                if (!entity.IsDirty(fi.Name)) continue;

                // 检查累加，如果累加且累加值为0，则跳过更新
                var flag = TryCheckAdditionalValue(dfs, fi.Name, out var val, out var sign);
                if (flag && Convert.ToDecimal(val) == 0) continue;

                var value = entity[fi.Name];

                sb.Separate(","); // 加逗号

                var name = db.FormatName(fi.Field);
                sb.Append(name);
                sb.Append('=');

                if (db.UseParameter || UseParam(fi, value))
                {
                    var dp = CreateParameter(db, fi, flag ? val : value);
                    dps.Add(dp);

                    // 检查累加
                    if (flag)
                    {
                        if (sign)
                            sb.AppendFormat("{0}+{1}", name, dp.ParameterName);
                        else
                            sb.AppendFormat("{0}-{1}", name, dp.ParameterName);
                    }
                    else
                    {
                        sb.Append(dp.ParameterName);
                    }
                }
                else
                {
                    // 检查累加
                    if (flag)
                    {
                        if (sign)
                            sb.AppendFormat("{0}+{1}", name, val);
                        else
                            sb.AppendFormat("{0}-{1}", name, val);
                    }
                    else
                        sb.Append(db.FormatValue(fi.Field, value));
                }
            }

            // 重置累加数据
            if (dfs != null && dfs.Count > 0) entity.Addition.Reset(dfs);

            var str = sb.Put(true);
            if (str.IsNullOrEmpty()) return null;

            // Where条件里面的参数化
            if (ps != null)
            {
                foreach (var item in ps)
                {
                    var dp = db.CreateParameter(item.Key, item.Value, factory.Table.FindByName(item.Key)?.Field);

                    dps.Add(dp);
                }
            }

            if (dps.Count > 0) parameters = dps.ToArray();
            return $"Update {session.FormatedTableName} Set {str} Where {def}";
        }

        String DeleteSQL(IEntitySession session, IEntity entity, ref IDataParameter[] parameters)
        {
            var factory = Factory;
            var db = session.Dal.Db;

            // 标识列作为删除关键字
            var exp = GetPrimaryCondition(entity);
            var ps = !db.UseParameter ? null : new Dictionary<String, Object>();
            var def = exp?.GetString(db, ps);
            if (def.IsNullOrEmpty()) return null;

            if (ps != null && ps.Count > 0)
            {
                var dps = new List<IDataParameter>();
                foreach (var item in ps)
                {
                    var dp = db.CreateParameter(item.Key, item.Value, factory.Table.FindByName(item.Key)?.Field);

                    dps.Add(dp);
                }
                parameters = dps.ToArray();
            }

            return $"Delete From {session.FormatedTableName} Where {def}";
        }

        static Boolean UseParam(FieldItem fi, Object value)
        {
            //// 是否使用参数化
            //if (Setting.Current.UserParameter) return true;

            if (fi.Length > 0 && fi.Length < 4000) return false;

            // 虽然是大字段，但数据量不大时不用参数
            if (fi.Type == typeof(String))
                return value is String str && str.Length > 4000;
            else if (fi.Type == typeof(Byte[]))
                return value is Byte[] str && str.Length > 4000;

            return false;
        }

        static IDataParameter CreateParameter(IDatabase db, FieldItem fi, Object value)
        {
            var dp = db.CreateParameter(fi.ColumnName ?? fi.Name, value, fi.Field);

            if (dp is DbParameter dbp) dbp.IsNullable = fi.IsNullable;

            return dp;
        }

        static Boolean TryCheckAdditionalValue(IDictionary<String, Object[]> dfs, String name, out Object value, out Boolean sign)
        {
            value = null;
            sign = false;
            if (dfs == null || !dfs.TryGetValue(name, out var vs)) return false;

            var cur = vs[0];
            var old = vs[1];

            //// 如果原始值是0，不使用累加，因为可能原始数据字段是NULL，导致累加失败
            //if (Convert.ToInt64(old) == 0) return false;

            sign = true;
            value = old;

            // 计算累加数据
            switch (cur.GetType().GetTypeCode())
            {
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        var v = Convert.ToInt64(cur) - Convert.ToInt64(old);
                        if (v < 0)
                        {
                            v = -v;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                case TypeCode.Single:
                    {
                        var v = (Single)cur - (Single)old;
                        if (v < 0)
                        {
                            v = -v;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                case TypeCode.Double:
                    {
                        var v = (Double)cur - (Double)old;
                        if (v < 0)
                        {
                            v = -v;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                case TypeCode.Decimal:
                    {
                        var v = (Decimal)cur - (Decimal)old;
                        if (v < 0)
                        {
                            v = -v;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                default:
                    break;
            }

            return true;
        }

        /// <summary>获取主键条件</summary>
        /// <remarks>
        /// 若有标识列，则使用一个标识列作为条件；
        /// 如有主键，则使用全部主键作为条件。
        /// </remarks>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual WhereExpression GetPrimaryCondition(IEntity entity)
        {
            var factory = Factory;
            var exp = new WhereExpression();

            // 标识列作为查询关键字
            var fi = factory.Table.Identity;
            if (fi != null)
            {
                exp &= (fi as Field) == entity[fi.Name];
                return exp;
            }

            // 主键作为查询关键字
            var ps = factory.Table.PrimaryKeys;
            // 没有标识列和主键，返回取所有数据的语句
            if (ps == null || ps.Length < 1) ps = factory.Table.Fields;

            foreach (var item in ps)
            {
                exp &= (item as Field) == entity[item.Name];
            }

            return exp;
        }
        #endregion

        #region 参数化
        /// <summary>插入语句</summary>
        /// <returns></returns>
        public virtual String InsertSQL(IEntitySession session)
        {
            var factory = Factory;
            var db = session.Dal.Db;

            var sbNames = Pool.StringBuilder.Get();
            var sbValues = Pool.StringBuilder.Get();

            foreach (var fi in factory.Fields)
            {
                // 标识列不需要插入，别的类型都需要
                if (fi.IsIdentity && !factory.AllowInsertIdentity) continue;

                sbNames.Separate(", ").Append(db.FormatName(fi.Field));
                sbValues.Separate(", ").Append(db.FormatParameterName(fi.Name));
            }

            var ns = sbNames.Put(true);
            var vs = sbValues.Put(true);

            return $"Insert Into {session.FormatedTableName}({ns}) Values({vs})";
        }
        #endregion
    }
}