using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Exceptions;

#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

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
        /// <param name="entityType">实体类</param>
        /// <param name="names">更新属性列表</param>
        /// <param name="values">更新值列表</param>
        /// <returns>返回受影响的行数</returns>
        Int32 Insert(Type entityType, String[] names, Object[] values);

        /// <summary>更新一批实体数据</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        Int32 Update(Type entityType, String setClause, String whereClause);

        /// <summary>更新一批实体数据</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="setNames">更新属性列表</param>
        /// <param name="setValues">更新值列表</param>
        /// <param name="whereNames">条件属性列表</param>
        /// <param name="whereValues">条件值列表</param>
        /// <returns>返回受影响的行数</returns>
        Int32 Update(Type entityType, String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues);

        /// <summary>从数据库中删除指定条件的实体对象。</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        Int32 Delete(Type entityType, String whereClause);

        /// <summary>从数据库中删除指定属性列表和值列表所限定的实体对象。</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns></returns>
        Int32 Delete(Type entityType, String[] names, Object[] values);
        #endregion

        #region 获取语句
        /// <summary>获取主键条件</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        String GetPrimaryCondition(IEntity entity);

        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <returns>SQL字符串</returns>
        String GetSql(IEntity entity, DataObjectMethodType methodType);
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

            // 添加数据前，处理Guid
            SetGuidField(op, entity);

            DbParameter[] dps = null;
            var sql = SQL(entity, DataObjectMethodType.Insert, ref dps);
            if (String.IsNullOrEmpty(sql)) return 0;

            Int32 rs = 0;

            //检查是否有标识列，标识列需要特殊处理
            var field = op.Table.Identity;
            var bAllow = op.AllowInsertIdentity;
            if (field != null && field.IsIdentity && !bAllow)
            {
                Int64 res = dps != null && dps.Length > 0 ? op.InsertAndGetIdentity(sql, CommandType.Text, dps) : op.InsertAndGetIdentity(sql);
                if (res > 0) entity[field.Name] = res;
                rs = res > 0 ? 1 : 0;
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
                rs = dps != null && dps.Length > 0 ? op.Execute(sql, CommandType.Text, dps) : op.Execute(sql);
            }

            //清除脏数据，避免连续两次调用Save造成重复提交
            if (entity.Dirtys != null) entity.Dirtys.Clear();

            return rs;
        }

        static void SetGuidField(IEntityOperate op, IEntity entity)
        {
            var fi = op.AutoSetGuidField;
            if (fi != null)
            {
                // 判断是否设置了数据
                if (!entity.Dirtys[fi.Name])
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
            //没有脏数据，不需要更新
            if (entity.Dirtys == null || entity.Dirtys.Count <= 0) return 0;

            DbParameter[] dps = null;
            var sql = SQL(entity, DataObjectMethodType.Update, ref dps);
            if (String.IsNullOrEmpty(sql)) return 0;

            var op = EntityFactory.CreateOperate(entity.GetType());
            Int32 rs = dps != null && dps.Length > 0 ? op.Execute(sql, CommandType.Text, dps) : op.Execute(sql);

            //清除脏数据，避免重复提交
            if (entity.Dirtys != null) entity.Dirtys.Clear();

            entity.ClearAdditionalValues();

            return rs;
        }

        /// <summary>删除</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntity entity)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());

            String sql = DefaultCondition(entity);
            if (String.IsNullOrEmpty(sql)) return 0;

            return op.Execute(String.Format("Delete From {0} Where {1}", op.FormatedTableName, sql));
        }

        /// <summary>把一个实体对象持久化到数据库</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="names">更新属性列表</param>
        /// <param name="values">更新值列表</param>
        /// <returns>返回受影响的行数</returns>
        public virtual Int32 Insert(Type entityType, String[] names, Object[] values)
        {
            if (names == null) throw new ArgumentNullException("names", "属性列表和值列表不能为空");
            if (values == null) throw new ArgumentNullException("values", "属性列表和值列表不能为空");
            if (names.Length != values.Length) throw new ArgumentException("属性列表必须和值列表一一对应");

            IEntityOperate op = EntityFactory.CreateOperate(entityType);

            Dictionary<String, FieldItem> fs = new Dictionary<String, FieldItem>(StringComparer.OrdinalIgnoreCase);
            foreach (FieldItem fi in op.Fields)
                fs.Add(fi.Name, fi);
            StringBuilder sbn = new StringBuilder();
            StringBuilder sbv = new StringBuilder();
            for (Int32 i = 0; i < names.Length; i++)
            {
                if (!fs.ContainsKey(names[i])) throw new ArgumentException("类[" + entityType.FullName + "]中不存在[" + names[i] + "]属性");
                // 同时构造SQL语句。names是属性列表，必须转换成对应的字段列表
                if (i > 0)
                {
                    sbn.Append(", ");
                    sbv.Append(", ");
                }
                sbn.Append(op.FormatName(fs[names[i]].Name));
                //sbv.Append(SqlDataFormat(values[i], fs[names[i]]));
                sbv.Append(op.FormatValue(names[i], values[i]));
            }
            return op.Execute(String.Format("Insert Into {2}({0}) values({1})", sbn.ToString(), sbv.ToString(), op.FormatedTableName));
        }

        /// <summary>更新一批实体数据</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        public virtual Int32 Update(Type entityType, String setClause, String whereClause)
        {
            if (String.IsNullOrEmpty(setClause) || !setClause.Contains("=")) throw new ArgumentException("非法参数");

            var op = EntityFactory.CreateOperate(entityType);
            var sql = String.Format("Update {0} Set {1}", op.FormatedTableName, setClause);
            if (!String.IsNullOrEmpty(whereClause)) sql += " Where " + whereClause;
            return op.Execute(sql);
        }

        /// <summary>更新一批实体数据</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="setNames">更新属性列表</param>
        /// <param name="setValues">更新值列表</param>
        /// <param name="whereNames">条件属性列表</param>
        /// <param name="whereValues">条件值列表</param>
        /// <returns>返回受影响的行数</returns>
        public virtual Int32 Update(Type entityType, String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues)
        {
            var op = EntityFactory.CreateOperate(entityType);

            var sc = op.MakeCondition(setNames, setValues, ", ");
            var wc = op.MakeCondition(whereNames, whereValues, " And ");
            return Update(entityType, sc, wc);
        }

        /// <summary>从数据库中删除指定条件的实体对象。</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        public virtual Int32 Delete(Type entityType, String whereClause)
        {
            var op = EntityFactory.CreateOperate(entityType);

            var sql = String.Format("Delete From {0}", op.FormatedTableName);
            if (!String.IsNullOrEmpty(whereClause)) sql += " Where " + whereClause;
            return op.Execute(sql);
        }

        /// <summary>从数据库中删除指定属性列表和值列表所限定的实体对象。</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns></returns>
        public virtual Int32 Delete(Type entityType, String[] names, Object[] values)
        {
            var op = EntityFactory.CreateOperate(entityType);

            return Delete(entityType, op.MakeCondition(names, values, "And"));
        }
        #endregion

        #region 获取语句
        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <returns>SQL字符串</returns>
        public virtual String GetSql(IEntity entity, DataObjectMethodType methodType) { DbParameter[] dps = null; return SQL(entity, methodType, ref dps); }

        /// <summary>把SQL模版格式化为SQL语句</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="methodType"></param>
        /// <param name="parameters"></param>
        /// <returns>SQL字符串</returns>
        String SQL(IEntity entity, DataObjectMethodType methodType, ref DbParameter[] parameters)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());
            var formatedTalbeName = op.FormatedTableName;

            String sql;

            switch (methodType)
            {
                case DataObjectMethodType.Fill:
                    return String.Format("Select * From {0}", formatedTalbeName);
                case DataObjectMethodType.Select:
                    sql = DefaultCondition(entity);
                    // 没有标识列和主键，返回取所有数据的语句
                    if (String.IsNullOrEmpty(sql)) throw new XCodeException("实体类缺少主键！");
                    return String.Format("Select * From {0} Where {1}", formatedTalbeName, sql);
                case DataObjectMethodType.Insert:
                    return InsertSQL(entity, ref parameters);
                case DataObjectMethodType.Update:
                    return UpdateSQL(entity, ref parameters);
                case DataObjectMethodType.Delete:
                    // 标识列作为删除关键字
                    sql = DefaultCondition(entity);
                    if (String.IsNullOrEmpty(sql))
                        return null;
                    return String.Format("Delete From {0} Where {1}", formatedTalbeName, sql);
            }
            return null;
        }

        static String InsertSQL(IEntity entity, ref DbParameter[] parameters)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());

            /*
            * 插入数据原则：
            * 1，没有脏数据的字段一律不参与
            * 
            */

            var sbNames = new StringBuilder();
            var sbValues = new StringBuilder();
            //sbParams = new StringBuilder();
            var dps = new List<DbParameter>();
            // 只读列没有插入操作
            foreach (var fi in op.Fields)
            {
                var value = entity[fi.Name];
                // 标识列不需要插入，别的类型都需要
                String idv = null;
                if (fi.IsIdentity)
                {
                    if (op.AllowInsertIdentity)
                        idv = "" + entity[fi.Name];
                    else
                        idv = DAL.Create(op.ConnName).Db.FormatIdentity(fi.Field, value);
                    //if (String.IsNullOrEmpty(idv)) continue;
                    // 允许返回String.Empty作为插入空
                    if (idv == null) continue;
                }

                // 有默认值，并且没有设置值时，不参与插入操作
                // 20120509增加，同时还得判断是否相同数据库或者数据库默认值，比如MSSQL数据库默认值不是GetDate，那么其它数据库是不可能使用的
                if (!String.IsNullOrEmpty(fi.DefaultValue) && !entity.Dirtys[fi.Name] && CanUseDefault(fi, op)) continue;

                sbNames.AppendExceptStart(", ");
                sbNames.Append(op.FormatName(fi.ColumnName));
                sbValues.AppendExceptStart(", ");

                //// 可空类型插入空
                //if (!obj.Dirtys[fi.Name] && fi.DataObjectField.IsNullable)
                //    sbValues.Append("null");
                //else
                //sbValues.Append(SqlDataFormat(obj[fi.Name], fi)); // 数据

                if (fi.IsIdentity)
                    sbValues.Append(idv);
                else
                {
                    if (!UseParam(fi))
                        sbValues.Append(op.FormatValue(fi, value));
                    else
                        dps.Add(CreateParameter(sbValues, op, fi, value));
                }
            }

            if (sbNames.Length <= 0) return null;

            if (dps.Count > 0) parameters = dps.ToArray();
            return String.Format("Insert Into {0}({1}) Values({2})", op.FormatedTableName, sbNames, sbValues);
        }

        static String UpdateSQL(IEntity entity, ref DbParameter[] parameters)
        {
            /*
             * 实体更新原则：
             * 1，自增不参与
             * 2，没有脏数据不参与
             * 3，大字段参数化特殊处理
             * 4，累加字段特殊处理
             */

            var def = DefaultCondition(entity);
            if (String.IsNullOrEmpty(def)) return null;

            var op = EntityFactory.CreateOperate(entity.GetType());

            var sb = new StringBuilder();
            var dps = new List<DbParameter>();
            // 只读列没有更新操作
            foreach (var fi in op.Fields)
            {
                if (fi.IsIdentity) continue;

                //脏数据判断
                if (!entity.Dirtys[fi.Name]) continue;

                var value = entity[fi.Name];

                sb.AppendExceptStart(", "); // 加逗号

                var name = op.FormatName(fi.ColumnName);
                sb.Append(name);
                sb.Append("=");

                if (UseParam(fi))
                    dps.Add(CreateParameter(sb, op, fi, value));
                else
                {
                    // 检查累加
                    if (!CheckAdditionalValue(sb, entity, fi.Name, name))
                        sb.Append(op.FormatValue(fi, value)); // 数据
                }
            }

            if (sb.Length <= 0) return null;

            if (dps.Count > 0) parameters = dps.ToArray();
            return String.Format("Update {0} Set {1} Where {2}", op.FormatedTableName, sb, def);
        }

        static Boolean UseParam(FieldItem fi)
        {
            return (fi.Length <= 0 || fi.Length >= 4000) && (fi.Type == typeof(Byte[]) || fi.Type == typeof(String));
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
                    return DAL.Create(eop.ConnName).Db.DateTimeMin;
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

        static DbParameter CreateParameter(StringBuilder sb, IEntityOperate op, FieldItem fi, Object value)
        {
            var paraname = op.FormatParameterName(fi.ColumnName);
            sb.Append(paraname);

            var dp = op.CreateParameter();
            dp.ParameterName = paraname;
            dp.Value = FormatParamValue(fi, value, op);
            dp.IsNullable = fi.IsNullable;

            return dp;
        }

        static Boolean CheckAdditionalValue(StringBuilder sb, IEntity entity, String name, String cname)
        {
            Object addvalue = null;
            Boolean sign;
            if (!entity.TryGetAdditionalValue(name, out addvalue, out sign)) return false;

            if (sign)
                sb.AppendFormat("{0}+{1}", cname, addvalue);
            else
                sb.AppendFormat("{0}-{1}", cname, addvalue);

            return true;
        }

        static Boolean CanUseDefault(FieldItem fi, IEntityOperate eop)
        {
            var dbType = fi.Table.Table.DbType;
            var dal = DAL.Create(eop.ConnName);
            if (dbType == dal.DbType) return true;

            // 原始数据库类型
            var db = DbFactory.Create(dbType);
            if (db == null) return false;

            var tc = Type.GetTypeCode(fi.Type);
            // 特殊处理时间
            if (tc == TypeCode.DateTime)
            {
                if (String.Equals(db.DateTimeNow, fi.DefaultValue, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>获取主键条件</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual String GetPrimaryCondition(IEntity entity) { return DefaultCondition(entity); }

        /// <summary>
        /// 默认条件。
        /// 若有标识列，则使用一个标识列作为条件；
        /// 如有主键，则使用全部主键作为条件。
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>条件</returns>
        static String DefaultCondition(IEntity entity)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());

            // 标识列作为查询关键字
            var fi = op.Table.Identity;
            if (fi != null) return op.MakeCondition(fi, entity[fi.Name], "=");

            // 主键作为查询关键字
            var ps = op.Table.PrimaryKeys;
            // 没有标识列和主键，返回取所有数据的语句
            if (ps == null || ps.Length < 1)
            {
                if (DAL.Debug) throw new XCodeException("因为没有主键，无法给实体类构造默认条件！");
                return null;
            }

            var sb = new StringBuilder();
            foreach (var item in ps)
            {
                if (sb.Length > 0) sb.Append(" And ");
                sb.Append(op.FormatName(item.ColumnName));
                sb.Append("=");
                sb.Append(op.FormatValue(item, entity[item.Name]));
            }
            return sb.ToString();
        }
        #endregion
    }
}