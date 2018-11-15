using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using NewLife.Collections;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体持久化接口。可通过实现该接口来自定义实体类持久化行为。</summary>
    public interface IEntityPersistence
    {
        #region 添删改方法
        /// <summary>插入</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Int32 Insert(IEntity entity);

        /// <summary>更新</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Int32 Update(IEntity entity);

        /// <summary>删除</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Int32 Delete(IEntity entity);

        /// <summary>把一个实体对象持久化到数据库</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="names">更新属性列表</param>
        /// <param name="values">更新值列表</param>
        /// <returns>返回受影响的行数</returns>
        Int32 Insert(IEntityOperate factory, String[] names, Object[] values);

        /// <summary>更新一批实体数据</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        Int32 Update(IEntityOperate factory, String setClause, String whereClause);

        /// <summary>更新一批实体数据</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="setNames">更新属性列表</param>
        /// <param name="setValues">更新值列表</param>
        /// <param name="whereNames">条件属性列表</param>
        /// <param name="whereValues">条件值列表</param>
        /// <returns>返回受影响的行数</returns>
        Int32 Update(IEntityOperate factory, String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues);

        /// <summary>从数据库中删除指定条件的实体对象。</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        Int32 Delete(IEntityOperate factory, String whereClause);

        /// <summary>从数据库中删除指定属性列表和值列表所限定的实体对象。</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns></returns>
        Int32 Delete(IEntityOperate factory, String[] names, Object[] values);
        #endregion

        #region 获取语句
        /// <summary>获取主键条件</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        WhereExpression GetPrimaryCondition(IEntity entity);

        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <returns>SQL字符串</returns>
        String GetSql(IEntity entity, DataObjectMethodType methodType);
        #endregion

        #region 参数化
        /// <summary>插入语句</summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        String InsertSQL(IEntityOperate factory);

        ///// <summary>插入参数</summary>
        ///// <param name="entity"></param>
        //IList<IDataParameter> InsertParameters(IEntity entity);
        #endregion
    }

    /// <summary>默认实体持久化</summary>
    public class EntityPersistence : IEntityPersistence
    {
        #region 添删改方法
        /// <summary>插入</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 Insert(IEntity entity)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());
            var session = op.Session;

            // 添加数据前，处理Guid
            SetGuidField(op, entity);

            IDataParameter[] dps = null;
            var sql = SQL(entity, DataObjectMethodType.Insert, ref dps);
            if (String.IsNullOrEmpty(sql)) return 0;

            var rs = 0;

            //检查是否有标识列，标识列需要特殊处理
            var field = op.Table.Identity;
            var bAllow = op.AllowInsertIdentity;
            if (field != null && field.IsIdentity && !bAllow && op.AutoIdentity)
            {
                var id = session.InsertAndGetIdentity(sql, CommandType.Text, dps);
                if (id > 0) entity[field.Name] = id;
                rs = id > 0 ? 1 : 0;
            }
            else
            {
                if (bAllow)
                {
                    var dal = DAL.Create(op.ConnName);
                    if (dal.DbType == DatabaseType.SqlServer)
                    {
                        // 如果所有字段都不是自增，则取消对自增的处理
                        if (op.Fields.All(f => !f.IsIdentity)) bAllow = false;
                        if (bAllow) sql = String.Format("SET IDENTITY_INSERT {1} ON;{0};SET IDENTITY_INSERT {1} OFF", sql, op.FormatedTableName);
                    }
                }
                rs = session.Execute(sql, CommandType.Text, dps);
            }

            // 清除脏数据，避免连续两次调用Save造成重复提交
            entity.Dirtys.Clear();

            return rs;
        }

        static void SetGuidField(IEntityOperate op, IEntity entity)
        {
            var fi = op.AutoSetGuidField;
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
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 Update(IEntity entity)
        {
            // 没有脏数据，不需要更新
            if (!entity.HasDirty) return 0;

            IDataParameter[] dps = null;
            var sql = "";

            // 双锁判断脏数据
            lock (entity)
            {
                if (!entity.HasDirty) return 0;

                sql = SQL(entity, DataObjectMethodType.Update, ref dps);
                if (sql.IsNullOrEmpty()) return 0;

                //清除脏数据，避免重复提交
                entity.Dirtys.Clear();
            }

            var op = EntityFactory.CreateOperate(entity.GetType());
            var session = op.Session;
            var rs = session.Execute(sql, CommandType.Text, dps);

            //EntityAddition.ClearValues(entity as EntityBase);

            return rs;
        }

        /// <summary>删除</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntity entity)
        {
            IDataParameter[] dps = null;
            var sql = SQL(entity, DataObjectMethodType.Delete, ref dps);
            if (String.IsNullOrEmpty(sql)) return 0;

            var op = EntityFactory.CreateOperate(entity.GetType());
            var session = op.Session;
            var rs = session.Execute(sql, CommandType.Text, dps);

            // 清除脏数据，避免重复提交保存
            entity.Dirtys.Clear();

            return rs;
        }

        /// <summary>把一个实体对象持久化到数据库</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="names">更新属性列表</param>
        /// <param name="values">更新值列表</param>
        /// <returns>返回受影响的行数</returns>
        public virtual Int32 Insert(IEntityOperate factory, String[] names, Object[] values)
        {
            if (names == null) throw new ArgumentNullException(nameof(names), "属性列表和值列表不能为空");
            if (values == null) throw new ArgumentNullException(nameof(values), "属性列表和值列表不能为空");
            if (names.Length != values.Length) throw new ArgumentException("属性列表必须和值列表一一对应");

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
                sbn.Append(factory.FormatName(fs[names[i]].Name));
                //sbv.Append(SqlDataFormat(values[i], fs[names[i]]));
                sbv.Append(factory.FormatValue(names[i], values[i]));
            }
            var sn = sbn.Put(true);
            var sv = sbv.Put(true);
            return factory.Session.Execute(String.Format("Insert Into {2}({0}) values({1})", sn, sv, factory.FormatedTableName));
        }

        /// <summary>更新一批实体数据</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        public virtual Int32 Update(IEntityOperate factory, String setClause, String whereClause)
        {
            if (setClause.IsNullOrEmpty() || !setClause.Contains("=")||setClause.Contains("Or")) throw new ArgumentException("非法参数");

            var sql = String.Format("Update {0} Set {1}", factory.FormatedTableName, setClause.Replace("And",","));
            if (!String.IsNullOrEmpty(whereClause)) sql += " Where " + whereClause;
            return factory.Session.Execute(sql);
        }

        /// <summary>更新一批实体数据</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="setNames">更新属性列表</param>
        /// <param name="setValues">更新值列表</param>
        /// <param name="whereNames">条件属性列表</param>
        /// <param name="whereValues">条件值列表</param>
        /// <returns>返回受影响的行数</returns>
        public virtual Int32 Update(IEntityOperate factory, String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues)
        {
            var sc = Join(factory, setNames, setValues, ", ");
            var wc = Join(factory, whereNames, whereValues, " And ");
            return Update(factory, sc, wc);
        }

        /// <summary>从数据库中删除指定条件的实体对象。</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntityOperate factory, String whereClause)
        {
            var sql = String.Format("Delete From {0}", factory.FormatedTableName);
            if (!whereClause.IsNullOrEmpty()) sql += " Where " + whereClause;
            return factory.Session.Execute(sql);
        }

        /// <summary>从数据库中删除指定属性列表和值列表所限定的实体对象。</summary>
        /// <param name="factory">实体工厂</param>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntityOperate factory, String[] names, Object[] values) => Delete(factory, Join(factory, names, values, "And"));

        private static String Join(IEntityOperate factory, String[] names, Object[] values, String split)
        {
            var fs = new Dictionary<String, FieldItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var fi in factory.Fields)
                fs.Add(fi.Name, fi);

            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < names.Length; i++)
            {
                if (!fs.ContainsKey(names[i])) throw new ArgumentException("类[" + factory.EntityType.FullName + "]中不存在[" + names[i] + "]属性");

                if (i > 0) sb.AppendFormat(" {0} ", split);
                sb.Append(factory.FormatName(fs[names[i]].Name));
                sb.Append("=");
                sb.Append(factory.FormatValue(names[i], values[i]));
            }

            return sb.Put(true);
        }
        #endregion

        #region 获取语句
        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <returns>SQL字符串</returns>
        public virtual String GetSql(IEntity entity, DataObjectMethodType methodType)
        {
            IDataParameter[] dps = null;
            return SQL(entity, methodType, ref dps);
        }

        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <param name="parameters">参数数组</param>
        /// <returns>SQL字符串</returns>
        String SQL(IEntity entity, DataObjectMethodType methodType, ref IDataParameter[] parameters)
        {
            //var op = EntityFactory.CreateOperate(entity.GetType());
            //var formatedTalbeName = op.FormatedTableName;

            //String sql;

            switch (methodType)
            {
                //case DataObjectMethodType.Fill:
                //    return String.Format("Select * From {0}", formatedTalbeName);
                //case DataObjectMethodType.Select:
                //    sql = DefaultCondition(entity);
                //    // 没有标识列和主键，返回取所有数据的语句
                //    if (String.IsNullOrEmpty(sql)) throw new XCodeException("实体类缺少主键！");
                //    return String.Format("Select * From {0} Where {1}", formatedTalbeName, sql);
                case DataObjectMethodType.Insert:
                    return InsertSQL(entity, ref parameters);
                case DataObjectMethodType.Update:
                    return UpdateSQL(entity, ref parameters);
                case DataObjectMethodType.Delete:
                    return DeleteSQL(entity, ref parameters);
            }
            return null;
        }

        static String InsertSQL(IEntity entity, ref IDataParameter[] parameters)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());
            var up = op.Session.Dal.Db.UseParameter;

            /*
            * 插入数据原则：
            * 1，有脏数据的字段一定要参与
            * 2，没有脏数据，允许空的字段不参与
            * 3，没有脏数据，不允许空，有默认值的不参与
            * 4，没有脏数据，不允许空，没有默认值的参与，需要智能识别并添加相应字段的默认数据
            */

            // 缓存参数化时的SQL语句
            var key = "{0}_Insert".F(entity.GetType().FullName);
            var sql = "";

            StringBuilder sbNames = null;
            StringBuilder sbValues = null;
            if (!up || !op.Session.Items.TryGetValue(key, out var oql))
            {
                sbNames = Pool.StringBuilder.Get();
                sbValues = Pool.StringBuilder.Get();
            }
            else
                sql = oql + "";

            var dps = new List<IDataParameter>();
            // 只读列没有插入操作
            foreach (var fi in op.Fields)
            {
                var value = entity[fi.Name];
                // 标识列不需要插入，别的类型都需要
                if (sbNames != null && CheckIdentity(fi, value, op, sbNames, sbValues)) continue;

                // 1，有脏数据的字段一定要参与同时对于实体有值的也应该参与（针对通过置空主键的方式另存）
                if (!up && value == null && !entity.IsDirty(fi.Name))
                {
                    // 2，没有脏数据，允许空的字段不参与
                    if (fi.IsNullable) continue;
                    //// 3，没有脏数据，不允许空，有默认值的不参与
                    //if (fi.DefaultValue != null) continue;

                    // 4，没有脏数据，不允许空，没有默认值的参与，需要智能识别并添加相应字段的默认数据
                    value = FormatParamValue(fi, null, op);
                }

                if (sbNames != null) sbNames.Separate(", ").Append(op.FormatName(fi.ColumnName));
                if (sbValues != null) sbValues.Separate(", ");

                if (up || UseParam(fi, value))
                    dps.Add(CreateParameter(sbValues, op, fi, value));
                else
                    sbValues.Append(op.FormatValue(fi, value));
            }

            var ns = sbNames.Put(true);
            var vs = sbValues.Put(true);
            if (ns.IsNullOrEmpty() && sql.IsNullOrEmpty()) return null;

            if (dps.Count > 0) parameters = dps.ToArray();

            if (!ns.IsNullOrEmpty())
            {
                sql = String.Format("Insert Into {0}({1}) Values({2})", op.FormatedTableName, ns, vs);
                // 缓存参数化时的SQL语句
                if (up) op.Session.Items[key] = sql;
            }

            return sql;
        }

        static Boolean CheckIdentity(FieldItem fi, Object value, IEntityOperate op, StringBuilder sbNames, StringBuilder sbValues)
        {
            if (!fi.IsIdentity) return false;

            // 有些时候需要向自增字段插入数据，这里特殊处理
            String idv = null;
            if (op.AllowInsertIdentity)
                idv = "" + value;
            //else
            //    idv = DAL.Create(op.ConnName).Db.FormatIdentity(fi.Field, value);
            //if (String.IsNullOrEmpty(idv)) continue;
            // 允许返回String.Empty作为插入空
            if (idv == null) return true;

            sbNames.Separate(", ").Append(op.FormatName(fi.ColumnName));
            sbValues.Separate(", ");

            sbValues.Append(idv);

            return true;
        }

        static String UpdateSQL(IEntity entity, ref IDataParameter[] parameters)
        {
            /*
             * 实体更新原则：
             * 1，自增不参与
             * 2，没有脏数据不参与
             * 3，大字段参数化特殊处理
             * 4，累加字段特殊处理
             */

            var def = DefaultCondition(entity);
            //if (String.IsNullOrEmpty(def)) return null;
            if (def.Empty) return null;

            // 处理累加字段
            var dfs = (entity as EntityBase).GetAddition();

            var op = EntityFactory.CreateOperate(entity.GetType());
            var up = op.Session.Dal.Db.UseParameter;

            var sb = Pool.StringBuilder.Get();
            var dps = new List<IDataParameter>();
            // 只读列没有更新操作
            foreach (var fi in op.Fields)
            {
                if (fi.IsIdentity) continue;

                //脏数据判断
                if (!entity.IsDirty(fi.Name)) continue;

                var value = entity[fi.Name];

                sb.Separate(", "); // 加逗号

                var name = op.FormatName(fi.ColumnName);
                sb.Append(name);
                sb.Append("=");

                if (up || UseParam(fi, value))
                    dps.Add(CreateParameter(sb, op, fi, value));
                else
                {
                    // 检查累加
                    if (!CheckAdditionalValue(sb, dfs, fi.Name, name))
                        sb.Append(op.FormatValue(fi, value)); // 数据
                }
            }

            // 重置累加数据
            if (dfs != null && dfs.Count > 0) (entity as EntityBase).Addition.Reset(dfs);

            var str = sb.Put(true);
            if (str.IsNullOrEmpty()) return null;

            if (dps.Count > 0) parameters = dps.ToArray();
            return String.Format("Update {0} Set {1} Where {2}", op.FormatedTableName, str, def);
        }

        static String DeleteSQL(IEntity entity, ref IDataParameter[] parameters)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());
            var up = op.Session.Dal.Db.UseParameter;

            // 标识列作为删除关键字
            var exp = DefaultCondition(entity);
            var ps = !up ? null : new Dictionary<String, Object>();
            var sql = exp?.GetString(ps);
            if (sql.IsNullOrEmpty()) return null;

            if (ps != null && ps.Count > 0)
            {
                var db = op.Session.Dal.Db;
                var dps = new List<IDataParameter>();
                foreach (var item in ps)
                {
                    var dp = db.CreateParameter(item.Key, item.Value, op.Table.FindByName(item.Key)?.Field);

                    dps.Add(dp);
                }
                parameters = dps.ToArray();
            }

            var formatedTalbeName = op.FormatedTableName;
            return String.Format("Delete From {0} Where {1}", formatedTalbeName, sql);
        }

        static Boolean UseParam(FieldItem fi, Object value)
        {
            //// 是否使用参数化
            //if (Setting.Current.UserParameter) return true;

            if (fi.Length > 0 && fi.Length < 4000) return false;

            // 虽然是大字段，但数据量不大时不用参数
            if (fi.Type == typeof(String))
            {
                return value is String str && str.Length > 4000;
            }
            else if (fi.Type == typeof(Byte[]))
            {
                return value is Byte[] str && str.Length > 4000;
            }

            return false;
        }

        static Object FormatParamValue(FieldItem fi, Object value, IEntityOperate eop)
        {
            if (value != null) return value;

            if (fi.IsNullable) return DBNull.Value;

            switch (Type.GetTypeCode(fi.Type))
            {
                case TypeCode.Boolean:
                    return false;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    return DBNull.Value;
                case TypeCode.DateTime:
                    return DateTime.MinValue;
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return 0;
                case TypeCode.String:
                    return String.Empty;
                default:
                    break;
            }

            return DBNull.Value;
        }

        static IDataParameter CreateParameter(StringBuilder sb, IEntityOperate op, FieldItem fi, Object value)
        {
            var dp = op.Session.Dal.Db.CreateParameter(fi.ColumnName ?? fi.Name, value, fi.Field);

            if (sb != null) sb.Append(dp.ParameterName);

            if (dp is DbParameter dbp) dbp.IsNullable = fi.IsNullable;

            return dp;
        }

        static Boolean CheckAdditionalValue(StringBuilder sb, IDictionary<String, Object[]> dfs, String name, String cname)
        {
            if (dfs == null || !dfs.TryGetValue(name, out var vs)) return false;

            var cur = vs[0];
            var old = vs[1];

            //// 如果原始值是0，不使用累加，因为可能原始数据字段是NULL，导致累加失败
            //if (Convert.ToInt64(old) == 0) return false;

            var sign = true;
            var value = old;

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
            if (sign)
                sb.AppendFormat("{0}+{1}", cname, value);
            else
                sb.AppendFormat("{0}-{1}", cname, value);

            return true;
        }

        /// <summary>获取主键条件</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual WhereExpression GetPrimaryCondition(IEntity entity) => DefaultCondition(entity);

        /// <summary>
        /// 默认条件。
        /// 若有标识列，则使用一个标识列作为条件；
        /// 如有主键，则使用全部主键作为条件。
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>条件</returns>
        static WhereExpression DefaultCondition(IEntity entity)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());
            var exp = new WhereExpression();

            // 标识列作为查询关键字
            var fi = op.Table.Identity;
            if (fi != null)
            {
                exp &= (fi as Field) == entity[fi.Name];
                return exp;
            }

            // 主键作为查询关键字
            var ps = op.Table.PrimaryKeys;
            // 没有标识列和主键，返回取所有数据的语句
            if (ps == null || ps.Length < 1)
            {
                //if (DAL.Debug) throw new XCodeException("因为没有主键，无法给实体类构造默认条件！");
                //return null;
                ps = op.Table.Fields;
            }

            //var sb = Pool.StringBuilder.Get();
            foreach (var item in ps)
            {
                //if (sb.Length > 0) sb.Append(" And ");
                //sb.Append(op.FormatName(item.ColumnName));
                //sb.Append("=");
                //sb.Append(op.FormatValue(item, entity[item.Name]));

                exp &= (item as Field) == entity[item.Name];
            }
            //return sb.ToString();
            return exp;
        }
        #endregion

        #region 参数化
        /// <summary>插入语句</summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        public virtual String InsertSQL(IEntityOperate factory)
        {
            var op = factory;
            var db = op.Session.Dal.Db;

            var sbNames = Pool.StringBuilder.Get();
            var sbValues = Pool.StringBuilder.Get();

            foreach (var fi in op.Fields)
            {
                // 标识列不需要插入，别的类型都需要
                if (fi.IsIdentity && !op.AllowInsertIdentity) continue;

                sbNames.Separate(", ").Append(op.FormatName(fi.ColumnName));
                sbValues.Separate(", ").Append(db.FormatParameterName(fi.Name));
            }

            var ns = sbNames.Put(true);
            var vs = sbValues.Put(true);

            return $"Insert Into {op.FormatedTableName}({ns}) Values({vs})";
        }

        ///// <summary>插入参数</summary>
        ///// <param name="entity"></param>
        //public virtual IList<IDataParameter> InsertParameters(IEntity entity)
        //{
        //    var op = entity.GetType().AsFactory();
        //    var db = op.Session.Dal.Db;

        //    var dps = new List<IDataParameter>();
        //    foreach (var fi in op.Fields)
        //    {
        //        // 标识列不需要插入，别的类型都需要
        //        if (fi.IsIdentity && !op.AllowInsertIdentity) continue;

        //        var dp = db.CreateParameter(fi.Name, entity[fi.Name], fi.Field);
        //        dps.Add(dp);
        //    }

        //    return dps;
        //}
        #endregion
    }
}