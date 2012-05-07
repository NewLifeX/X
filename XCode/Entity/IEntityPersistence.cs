using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text;
using NewLife.Collections;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Exceptions;

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

        //#region 事件
        ///// <summary>设置实体Guid之前触发事件，通过Cancel控制是否取消Guid的自动设置</summary>
        //event EventHandler<EntityPersistEventArgs> OnSetGuid;

        ///// <summary>插入自增之前触发事件，通过Cancel控制是否插入自增</summary>
        //event EventHandler<EntityPersistEventArgs> OnInsertIdentity;
        //#endregion
    }

    /// <summary>默认实体持久化</summary>
    public class EntityPersistence : IEntityPersistence
    {
        //#region 事件
        ///// <summary>设置实体Guid之前触发事件，通过Cancel控制是否取消Guid的自动设置</summary>
        //public event EventHandler<EntityPersistEventArgs> OnSetGuid;

        //Boolean AllowSetGuid(IEntity entity)
        //{
        //    if (OnSetGuid != null)
        //    {
        //        var e = new EntityPersistEventArgs() { Entity = entity };
        //        OnSetGuid(this, e);
        //        return !e.Cancel;
        //    }
        //    return false;
        //}

        ///// <summary>插入自增之前触发事件，通过Cancel控制是否插入自增</summary>
        //public event EventHandler<EntityPersistEventArgs> OnInsertIdentity;

        //Boolean AllowInsertIdentity(IEntity entity)
        //{
        //    if (OnInsertIdentity != null)
        //    {
        //        var e = new EntityPersistEventArgs() { Entity = entity };
        //        OnInsertIdentity(this, e);
        //        return !e.Cancel;
        //    }
        //    return false;
        //}
        //#endregion

        #region 添删改方法
        /// <summary>插入</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 Insert(IEntity entity)
        {
            var op = EntityFactory.CreateOperate(entity.GetType());

            // 添加数据前，处理Guid
            var fi = op.AutoSetGuidField;
            if (fi != null)
            {
                //SetGuid(entity);

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
                        sql = String.Format("SET IDENTITY_INSERT {1} ON;{0};SET IDENTITY_INSERT {1} OFF", sql, op.FormatName(op.TableName));
                }
                rs = dps != null && dps.Length > 0 ? op.Execute(sql, CommandType.Text, dps) : op.Execute(sql);
            }

            //清除脏数据，避免连续两次调用Save造成重复提交
            if (entity.Dirtys != null) entity.Dirtys.Clear();

            return rs;
        }

        /// <summary>更新</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 Update(IEntity entity)
        {
            //没有脏数据，不需要更新
            if (entity.Dirtys == null || entity.Dirtys.Count <= 0) return 0;

            DbParameter[] dps = null;
            String sql = SQL(entity, DataObjectMethodType.Update, ref dps);
            if (String.IsNullOrEmpty(sql)) return 0;

            IEntityOperate op = EntityFactory.CreateOperate(entity.GetType());
            Int32 rs = dps != null && dps.Length > 0 ? op.Execute(sql, CommandType.Text, dps) : op.Execute(sql);

            //清除脏数据，避免重复提交
            if (entity.Dirtys != null) entity.Dirtys.Clear();

            return rs;
        }

        /// <summary>删除</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntity entity)
        {
            IEntityOperate op = EntityFactory.CreateOperate(entity.GetType());

            String sql = DefaultCondition(entity);
            if (String.IsNullOrEmpty(sql)) return 0;

            return op.Execute(String.Format("Delete From {0} Where {1}", op.FormatName(op.TableName), sql));
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
            return op.Execute(String.Format("Insert Into {2}({0}) values({1})", sbn.ToString(), sbv.ToString(), op.FormatName(op.TableName)));
        }

        /// <summary>更新一批实体数据</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        public virtual Int32 Update(Type entityType, String setClause, String whereClause)
        {
            if (String.IsNullOrEmpty(setClause) || !setClause.Contains("=")) throw new ArgumentException("非法参数");

            IEntityOperate op = EntityFactory.CreateOperate(entityType);
            String sql = String.Format("Update {0} Set {1}", op.FormatName(op.TableName), setClause);
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
            IEntityOperate op = EntityFactory.CreateOperate(entityType);

            String sc = op.MakeCondition(setNames, setValues, ", ");
            String wc = op.MakeCondition(whereNames, whereValues, " And ");
            return Update(entityType, sc, wc);
        }

        /// <summary>从数据库中删除指定条件的实体对象。</summary>
        /// <param name="entityType">实体类</param>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        public virtual Int32 Delete(Type entityType, String whereClause)
        {
            IEntityOperate op = EntityFactory.CreateOperate(entityType);

            String sql = String.Format("Delete From {0}", op.FormatName(op.TableName));
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
            IEntityOperate op = EntityFactory.CreateOperate(entityType);

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
            IEntityOperate op = EntityFactory.CreateOperate(entity.GetType());

            String sql;
            StringBuilder sbNames;
            StringBuilder sbValues;

            // sbParams用于存储参数化操作时格式化的参数名，参数化和非参数化同时使用，如果存在大字段是，才使用参数化
            //StringBuilder sbParams;
            List<DbParameter> dps;
            //Boolean hasBigField = false;

            Boolean isFirst = true;
            switch (methodType)
            {
                case DataObjectMethodType.Fill:
                    return String.Format("Select * From {0}", op.FormatName(op.TableName));
                case DataObjectMethodType.Select:
                    sql = DefaultCondition(entity);
                    // 没有标识列和主键，返回取所有数据的语句
                    if (String.IsNullOrEmpty(sql)) throw new XCodeException("实体类缺少主键！");
                    return String.Format("Select * From {0} Where {1}", op.FormatName(op.TableName), sql);
                case DataObjectMethodType.Insert:
                    sbNames = new StringBuilder();
                    sbValues = new StringBuilder();
                    //sbParams = new StringBuilder();
                    dps = new List<DbParameter>();
                    // 只读列没有插入操作
                    foreach (var fi in op.Fields)
                    {
                        // 标识列不需要插入，别的类型都需要
                        String idv = null;
                        if (fi.IsIdentity)
                        {
                            if (op.AllowInsertIdentity)
                                idv = "" + entity[fi.Name];
                            else
                                idv = DAL.Create(op.ConnName).Db.FormatIdentity(fi.Field, entity[fi.Name]);
                            //if (String.IsNullOrEmpty(idv)) continue;
                            // 允许返回String.Empty作为插入空
                            if (idv == null) continue;
                        }

                        // 有默认值，并且没有设置值时，不参与插入操作
                        if (!String.IsNullOrEmpty(fi.DefaultValue) && !entity.Dirtys[fi.Name]) continue;

                        if (!isFirst) sbNames.Append(", ");
                        var name = op.FormatName(fi.ColumnName);
                        sbNames.Append(name);
                        if (!isFirst)
                            sbValues.Append(", ");
                        else
                            isFirst = false;

                        //// 可空类型插入空
                        //if (!obj.Dirtys[fi.Name] && fi.DataObjectField.IsNullable)
                        //    sbValues.Append("null");
                        //else
                        //sbValues.Append(SqlDataFormat(obj[fi.Name], fi)); // 数据

                        if (!fi.IsIdentity)
                        {
                            if (!UseParam(fi))
                                sbValues.Append(op.FormatValue(fi, entity[fi.Name]));
                            else
                            {
                                var paraname = op.FormatParameterName(fi.ColumnName);
                                sbValues.Append(paraname);

                                var dp = op.CreateParameter();
                                dp.ParameterName = paraname;
                                dp.Value = entity[fi.Name] ?? DBNull.Value;
                                dp.IsNullable = fi.IsNullable;
                                dps.Add(dp);
                            }
                        }
                        else
                            sbValues.Append(idv);
                    }

                    if (sbNames.Length <= 0) return null;

                    if (dps.Count > 0) parameters = dps.ToArray();
                    return String.Format("Insert Into {0}({1}) Values({2})", op.FormatName(op.TableName), sbNames, sbValues);
                case DataObjectMethodType.Update:
                    sbNames = new StringBuilder();
                    //sbParams = new StringBuilder();
                    dps = new List<DbParameter>();
                    // 只读列没有更新操作
                    foreach (FieldItem fi in op.Fields)
                    {
                        if (fi.IsIdentity) continue;

                        //脏数据判断
                        if (!entity.Dirtys[fi.Name]) continue;

                        if (!isFirst)
                            sbNames.Append(", "); // 加逗号
                        else
                            isFirst = false;

                        var name = op.FormatName(fi.ColumnName);
                        sbNames.Append(name);
                        sbNames.Append("=");
                        //sbNames.Append(SqlDataFormat(obj[fi.Name], fi)); // 数据

                        if (!UseParam(fi))
                            sbNames.Append(op.FormatValue(fi, entity[fi.Name])); // 数据
                        else
                        {
                            var paraname = op.FormatParameterName(fi.ColumnName);
                            sbNames.Append(paraname);

                            var dp = op.CreateParameter();
                            dp.ParameterName = paraname;
                            dp.Value = entity[fi.Name] ?? DBNull.Value;
                            dp.IsNullable = fi.IsNullable;
                            dps.Add(dp);
                        }
                    }

                    if (sbNames.Length <= 0) return null;

                    sql = DefaultCondition(entity);
                    if (String.IsNullOrEmpty(sql)) return null;

                    if (dps.Count > 0) parameters = dps.ToArray();
                    return String.Format("Update {0} Set {1} Where {2}", op.FormatName(op.TableName), sbNames, sql);
                case DataObjectMethodType.Delete:
                    // 标识列作为删除关键字
                    sql = DefaultCondition(entity);
                    if (String.IsNullOrEmpty(sql))
                        return null;
                    return String.Format("Delete From {0} Where {1}", op.FormatName(op.TableName), sql);
            }
            return null;
        }

        static Boolean UseParam(FieldItem fi)
        {
            return (fi.Length <= 0 || fi.Length >= 4000) && (fi.Type == typeof(Byte[]) || fi.Type == typeof(String));
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
            IEntityOperate op = EntityFactory.CreateOperate(entity.GetType());

            // 标识列作为查询关键字
            FieldItem fi = op.Table.Identity;
            if (fi != null) return op.MakeCondition(fi, entity[fi.Name], "=");

            // 主键作为查询关键字
            FieldItem[] ps = op.Table.PrimaryKeys;
            // 没有标识列和主键，返回取所有数据的语句
            if (ps == null || ps.Length < 1)
            {
                if (DAL.Debug) throw new XCodeException("因为没有主键，无法给实体类构造默认条件！");
                return null;
            }

            StringBuilder sb = new StringBuilder();
            foreach (FieldItem item in ps)
            {
                if (sb.Length > 0) sb.Append(" And ");
                sb.Append(op.FormatName(item.ColumnName));
                sb.Append("=");
                sb.Append(op.FormatValue(item, entity[item.Name]));
            }
            return sb.ToString();
        }
        #endregion

        #region 设置Guid
        ///// <summary>指定了默认值而没有赋值的Guid字段附上默认值</summary>
        ///// <param name="entity"></param>
        //public virtual void SetGuid(IEntity entity)
        //{
        //    var fis = GetGuidFieldItems(entity.GetType());
        //    if (fis != null & fis.Length > 0)
        //    {
        //        foreach (var item in fis)
        //        {
        //            // 判断是否设置了数据
        //            if (!entity.Dirtys[item.Name])
        //            {
        //                // 如果没有设置，这里给它设置
        //                if (item.Type == typeof(Guid))
        //                    entity.SetItem(item.Name, Guid.NewGuid());
        //                else
        //                    entity.SetItem(item.Name, Guid.NewGuid().ToString());
        //            }
        //        }
        //    }
        //}

        //static DictionaryCache<Type, FieldItem[]> _guidFields = new DictionaryCache<Type, FieldItem[]>();
        ///// <summary>找到设定了默认值的Guid字段</summary>
        ///// <param name="type"></param>
        ///// <returns></returns>
        //protected static FieldItem[] GetGuidFieldItems(Type type)
        //{
        //    return _guidFields.GetItem(type, key =>
        //    {
        //        var eop = EntityFactory.CreateOperate(key);
        //        // 只检查默认设计的数据库
        //        var db = DbFactory.Create(eop.Table.DataTable.DbType);
        //        if (String.IsNullOrEmpty(db.NewGuid)) return null;

        //        var list = new List<FieldItem>();
        //        foreach (var item in eop.AllFields)
        //        {
        //            //if (String.IsNullOrEmpty(item.DefaultValue)) continue;

        //            var tc = Type.GetTypeCode(item.Type);
        //            if (tc == TypeCode.String)
        //            {
        //                if (item.DefaultValue.EqualIgnoreCase(db.NewGuid)) list.Add(item);
        //            }
        //            else if (item.Type == typeof(Guid))
        //            {
        //                if (item.DefaultValue.EqualIgnoreCase(db.NewGuid) || String.IsNullOrEmpty(item.DefaultValue)) list.Add(item);
        //            }
        //        }
        //        return list.ToArray();
        //    });
        //}
        #endregion
    }

    //public class EntityPersistEventArgs : EventArgs
    //{
    //    private IEntity _Entity;
    //    /// <summary>实体</summary>
    //    public IEntity Entity { get { return _Entity; } set { _Entity = value; } }

    //    private Boolean _Cancel;
    //    /// <summary>是否取消</summary>
    //    public Boolean Cancel { get { return _Cancel; } set { _Cancel = value; } }
    //}
}