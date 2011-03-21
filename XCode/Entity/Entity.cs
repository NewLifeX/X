using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Services;
using System.Xml.Serialization;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Exceptions;

namespace XCode
{
    /// <summary>
    /// 数据实体类基类。所有数据实体类都必须继承该类。
    /// </summary>
    [Serializable]
    public partial class Entity<TEntity> : EntityBase where TEntity : Entity<TEntity>, new()
    {
        #region 构造函数
        /// <summary>
        /// 静态构造
        /// </summary>
        static Entity()
        {
            // 1，可以初始化该实体类型的操作工厂
            // 2，CreateOperate将会实例化一个TEntity对象，从而引发TEntity的静态构造函数，
            // 避免实际应用中，直接调用Entity的静态方法时，没有引发TEntity的静态构造函数。
            TEntity entity = new TEntity();

            //! 大石头 2011-03-14 以下过程改为异步处理
            //  已确认，当实体类静态构造函数中使用了EntityFactory.CreateOperate(Type)方法时，可能出现死锁。
            //  因为两者都会争夺EntityFactory中的op_cache，而CreateOperate(Type)拿到op_cache后，还需要等待当前静态构造函数执行完成。
            //  不确定这样子是否带来后遗症
            //ThreadPool.QueueUserWorkItem(delegate
            //{
            EntityFactory.CreateOperate(Meta.ThisType, entity);
            //});
        }

        /// <summary>
        /// 创建实体
        /// </summary>
        /// <returns></returns>
        internal override IEntity CreateInternal()
        {
            return CreateInstance();
        }

        /// <summary>
        /// 创建实体
        /// </summary>
        /// <returns></returns>
        protected virtual TEntity CreateInstance()
        {
            return new TEntity();
        }
        #endregion

        #region 填充数据
        /// <summary>
        /// 加载记录集
        /// </summary>
        /// <param name="ds">记录集</param>
        /// <returns>实体数组</returns>
        public static EntityList<TEntity> LoadData(DataSet ds)
        {
            if (ds == null || ds.Tables.Count < 1 || ds.Tables[0].Rows.Count < 1) return null;
            return LoadData(ds.Tables[0]);
        }

        /// <summary>
        /// 加载数据表
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <returns>实体数组</returns>
        protected static EntityList<TEntity> LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count < 1) return null;
            return LoadData(dt, null);
        }

        /// <summary>
        /// 加载数据表
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="jointypes"></param>
        /// <returns>实体数组</returns>
        protected static EntityList<TEntity> LoadData(DataTable dt, Type[] jointypes)
        {
            if (dt == null || dt.Rows.Count < 1) return null;
            EntityList<TEntity> list = new EntityList<TEntity>(dt.Rows.Count);
            String prefix = null;
            TableMapAttribute[] maps = null;
            Boolean hasprefix = false;
            if (jointypes != null && jointypes.Length > 0)
            {
                maps = XCodeConfig.TableMaps(Meta.ThisType, jointypes);
                prefix = Meta.ColumnPrefix;
                hasprefix = true;
            }
            IEntityOperate factory = EntityFactory.CreateOperate(Meta.ThisType);
            List<FieldItem> ps = CheckColumn(dt, prefix);
            foreach (DataRow dr in dt.Rows)
            {
                //TEntity obj = new TEntity();
                TEntity obj = factory.Create() as TEntity;
                obj.LoadData(dr, hasprefix, ps, maps);
                list.Add(obj);
            }
            return list;
        }

        /// <summary>
        /// 从一个数据行对象加载数据。不加载关联对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        public override void LoadData(DataRow dr)
        {
            if (dr == null) return;
            LoadData(dr, null);
        }

        /// <summary>
        /// 从一个数据行对象加载数据。指定要加载哪些关联的实体类对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        /// <param name="jointypes">多表关联</param>
        protected virtual void LoadData(DataRow dr, Type[] jointypes)
        {
            if (dr == null) return;
            String prefix = null;
            TableMapAttribute[] maps = null;
            Boolean hasprefix = false;
            if (jointypes != null && jointypes.Length > 0)
            {
                maps = XCodeConfig.TableMaps(Meta.ThisType, jointypes);
                prefix = Meta.ColumnPrefix;
                hasprefix = true;
            }
            List<FieldItem> ps = CheckColumn(dr.Table, prefix);
            LoadData(dr, hasprefix, ps, maps);
        }

        /// <summary>
        /// 从一个数据行对象加载数据。带前缀。
        /// </summary>
        /// <param name="dr">数据行</param>
        /// <param name="ps">要加载数据的字段</param>
        /// <returns></returns>
        protected virtual void LoadDataWithPrefix(DataRow dr, List<FieldItem> ps)
        {
            if (dr == null) return;
            if (ps == null || ps.Count < 1) ps = Meta.Fields;
            String prefix = Meta.ColumnPrefix;
            foreach (FieldItem fi in ps)
            {
                // 两次dr[fi.ColumnName]简化为一次
                Object v = dr[prefix + fi.ColumnNameEx];
                this[fi.Name] = v == DBNull.Value ? null : v;
            }
        }

        static String[] TrueString = new String[] { "true", "y", "yes", "1" };
        static String[] FalseString = new String[] { "false", "n", "no", "0" };

        /// <summary>
        /// 从一个数据行对象加载数据。指定要加载数据的字段，以及要加载哪些关联的实体类对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        /// <param name="hasprefix">是否带有前缀</param>
        /// <param name="ps">要加载数据的字段</param>
        /// <param name="maps">要关联的实体类</param>
        /// <returns></returns>
        private void LoadData(DataRow dr, Boolean hasprefix, List<FieldItem> ps, TableMapAttribute[] maps)
        {
            if (dr == null) return;
            if (ps == null || ps.Count < 1) ps = Meta.Fields;
            String prefix = null;
            if (hasprefix) prefix = Meta.ColumnPrefix;
            foreach (FieldItem fi in ps)
            {
                // 两次dr[fi.ColumnName]简化为一次
                Object v = dr[prefix + fi.ColumnNameEx];
                Object v2 = this[fi.Name];

                // 不处理相同数据的赋值
                if (Object.Equals(v, v2)) continue;

                if (fi.Property.PropertyType == typeof(String))
                {
                    // 不处理空字符串对空字符串的赋值
                    if (v != null && String.IsNullOrEmpty(v.ToString()))
                    {
                        if (v2 == null || String.IsNullOrEmpty(v2.ToString())) continue;
                    }
                }
                else if (fi.Property.PropertyType == typeof(Boolean))
                {
                    // 处理字符串转为布尔型
                    if (v != null && v.GetType() == typeof(String))
                    {
                        String vs = v.ToString();
                        if (String.IsNullOrEmpty(vs))
                            v = false;
                        else
                        {
                            if (Array.IndexOf(TrueString, vs.ToLower()) >= 0)
                                v = true;
                            else if (Array.IndexOf(FalseString, vs.ToLower()) >= 0)
                                v = false;

                            //if (NewLife.Configuration.Config.GetConfig<Boolean>("XCode.Debug")) NewLife.Log.XTrace.WriteLine("无法把字符串{0}转为布尔型！", vs);
                            if (DAL.Debug) DAL.WriteLog("无法把字符串{0}转为布尔型！", vs);
                        }
                    }
                }

                //不影响脏数据的状态
                Boolean? b = null;
                if (Dirtys.ContainsKey(fi.Name)) b = Dirtys[fi.Name];

                this[fi.Name] = v == DBNull.Value ? null : v;

                if (b != null)
                    Dirtys[fi.Name] = b.Value;
                else
                    Dirtys.Remove(fi.Name);
            }
            //给关联属性赋值
            if (maps != null && maps.Length > 0)
            {
                foreach (TableMapAttribute item in maps)
                {
                    LoadDataEx(dr, item);
                }
            }
        }

        /// <summary>
        /// 从一个数据行对象加载数据。现在用反射实现，为了更好性能，实体类应该重载该方法。
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="map"></param>
        protected virtual void LoadDataEx(DataRow dr, TableMapAttribute map)
        {
            //创建一个对象
            Object obj = Activator.CreateInstance(map.MapEntity);
            //找到装载数据的方法
            MethodInfo method = map.MapEntity.GetMethod("LoadDataWithPrefix");
            //给这个对象装载数据
            //method.Invoke(this, new Object[] { dr, null });
            MethodInfoX.Create(method).Invoke(this, new Object[] { dr, null });
            //给关联属性赋值
            map.LocalField.SetValue(this, obj, null);
        }

        /// <summary>
        /// 检查实体类中的哪些字段在数据表中
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="prefix">字段前缀</param>
        /// <returns></returns>
        private static List<FieldItem> CheckColumn(DataTable dt, String prefix)
        {
            //// 检查dr中是否有该属性的列。考虑到Select可能是不完整的，此时，只需要局部填充
            //List<FieldItem> allps = Meta.AllFields;
            //if (allps == null || allps.Count < 1) return null;

            //这里可千万不能删除allps中的项，那样会影响到全局的Fields缓存的
            List<FieldItem> ps = new List<FieldItem>();
            //for (Int32 i = allps.Length - 1; i >= 0; i--)
            //{
            //    if (dt.Columns.Contains(prefix + allps[i].ColumnNameEx)) ps.Add(allps[i]);
            //}
            foreach (FieldItem item in Meta.AllFields)
            {
                if (dt.Columns.Contains(prefix + item.ColumnNameEx)) ps.Add(item);
            }
            return ps;

            //return Meta.AllFields.FindAll(delegate(FieldItem item)
            //{
            //    return dt.Columns.Contains(prefix + item.ColumnNameEx);
            //});
        }

        ///// <summary>
        ///// 把数据复制到数据行对象中。
        ///// </summary>
        ///// <param name="dr">数据行</param>
        //public virtual DataRow ToData(ref DataRow dr)
        //{
        //    if (dr == null) return null;
        //    List<FieldItem> ps = Meta.Fields;
        //    foreach (FieldItem fi in ps)
        //    {
        //        // 检查dr中是否有该属性的列。考虑到Select可能是不完整的，此时，只需要局部填充
        //        if (dr.Table.Columns.Contains(fi.ColumnName))
        //            dr[fi.ColumnName] = this[fi.Name];
        //    }
        //    return dr;
        //}
        #endregion

        #region 操作
        /// <summary>
        /// 把该对象持久化到数据库
        /// </summary>
        /// <returns></returns>
        public override Int32 Insert()
        {
            String sql = SQL(this, DataObjectMethodType.Insert);

            //AC和SqlServer支持获取自增字段的最新编号
            //if (Meta.DbType == DatabaseType.Access ||
            //    Meta.DbType == DatabaseType.SqlServer ||
            //    Meta.DbType == DatabaseType.SqlServer2005)
            {
                //检查是否有标识列，标识列需要特殊处理
                //FieldItem[] ps = Meta.Uniques;
                FieldItem field = Meta.Unique;
                //if (ps != null && ps.Length > 0 && ps[0].DataObjectField != null && ps[0].DataObjectField.IsIdentity)
                if (field != null && field.DataObjectField != null && field.DataObjectField.IsIdentity)
                {
                    Int64 res = Meta.InsertAndGetIdentity(sql);
                    if (res > 0) this[field.Name] = res;
                    return res > 0 ? 1 : 0;
                }
            }
            return Meta.Execute(sql);
        }

        /// <summary>
        /// 更新数据库
        /// </summary>
        /// <returns></returns>
        public override Int32 Update()
        {
            //没有脏数据，不需要更新
            if (Dirtys == null || Dirtys.Count <= 0) return 0;

            String sql = SQL(this, DataObjectMethodType.Update);
            if (String.IsNullOrEmpty(sql)) return 0;
            Int32 rs = Meta.Execute(sql);

            //清除脏数据，避免重复提交
            if (Dirtys != null)
            {
                Dirtys.Clear();
                Dirtys = null;
            }
            return rs;
        }

        /// <summary>
        /// 从数据库中删除该对象
        /// </summary>
        /// <returns></returns>
        public override Int32 Delete()
        {
            return Meta.Execute(SQL(this, DataObjectMethodType.Delete));
        }

        /// <summary>
        /// 保存。根据主键检查数据库中是否已存在该对象，再决定调用Insert或Update
        /// </summary>
        /// <returns></returns>
        public override Int32 Save()
        {
            //优先使用自增字段判断
            FieldItem fi = Meta.Unique;
            if (fi != null && fi.DataObjectField.IsIdentity || fi.Property.PropertyType == typeof(Int32))
            {
                Int64 id = Convert.ToInt64(this[Meta.Unique.Name]);
                if (id > 0)
                    return Update();
                else
                    return Insert();
            }

            Int32 count = Meta.QueryCount(SQL(this, DataObjectMethodType.Select));

            if (count > 0)
                return Update();
            else
                return Insert();
        }
        #endregion

        #region 查找单个实体
        /// <summary>
        /// 根据属性以及对应的值，查找单个实体
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [WebMethod(Description = "根据属性以及对应的值，查找单个实体")]
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity Find(String name, Object value)
        {
            return Find(new String[] { name }, new Object[] { value });
        }

        /// <summary>
        /// 根据属性列表以及对应的值列表，查找单个实体
        /// </summary>
        /// <param name="names"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static TEntity Find(String[] names, Object[] values)
        {
            return Find(MakeCondition(names, values, "And"));
        }

        /// <summary>
        /// 根据条件查找单个实体
        /// </summary>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity Find(String whereClause)
        {
            IList<TEntity> list = FindAll(whereClause, null, null, 0, 1);
            if (list == null || list.Count < 1)
                return null;
            else
                return list[0];
        }

        /// <summary>
        /// 根据主键查找单个实体
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKey(Object key)
        {
            FieldItem field = Meta.Unique;
            if (field == null) throw new ArgumentNullException("Meta.Unique", "FindByKey方法要求该表有唯一主键！");

            // 唯一键为自增且参数小于等于0时，返回空
            if (field.DataObjectField.IsIdentity && (key is Int32) && ((Int32)key) <= 0) return null;

            return Find(field.Name, key);
        }

        /// <summary>
        /// 根据主键查询一个实体对象用于表单编辑
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKeyForEdit(Object key)
        {
            FieldItem field = Meta.Unique;
            if (field == null) throw new ArgumentNullException("Meta.Unique", "FindByKeyForEdit方法要求该表有唯一主键！");

            // 参数为空时，返回新实例
            if (key == null)
            {
                IEntityOperate factory = EntityFactory.CreateOperate(Meta.ThisType);
                return factory.Create() as TEntity;
            }

            Type type = field.Property.PropertyType;

            // 唯一键为自增且参数小于等于0时，返回新实例
            if (IsInt(type) && IsInt(key.GetType()) && ((Int32)key) <= 0)
            {
                if (field.DataObjectField.IsIdentity)
                {
                    IEntityOperate factory = EntityFactory.CreateOperate(Meta.ThisType);
                    return factory.Create() as TEntity;
                }
                else
                {
                    if (DAL.Debug) DAL.WriteLog("{0}的{1}字段是整型主键，你是否忘记了设置自增？", Meta.TableName, field.ColumnName);
                }
            }

            // 唯一键是字符串且为空时，返回新实例
            if (type == typeof(String) && (key is String) && String.IsNullOrEmpty((String)key))
            {
                IEntityOperate factory = EntityFactory.CreateOperate(Meta.ThisType);
                return factory.Create() as TEntity;
            }

            // 此外，一律返回 查找值，即使可能是空。而绝不能在找不到数据的情况下给它返回空，因为可能是找不到数据而已，而返回新实例会导致前端以为这里是新增数据
            TEntity entity = Find(field.Name, key);

            // 判断实体
            if (entity == null)
            {
                String msg = null;
                if (IsInt(type) && IsInt(key.GetType()) && ((Int32)key) <= 0)
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！可能未设置自增主键！", key, Meta.Description);
                else
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！", key, Meta.Description);

                throw new XCodeException(msg);
            }

            return entity;
        }

        /// <summary>
        /// 是否整数，包括16位、32位和64位，还有无符号和有符号
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static Boolean IsInt(Type type)
        {
            if (type == null) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    break;
            }
            return false;
        }
        #endregion

        #region 静态查询
        /// <summary>
        /// 获取所有实体对象。获取大量数据时会非常慢，慎用
        /// </summary>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAll()
        {
            return LoadData(Meta.Query(SQL(null, DataObjectMethodType.Fill)));
        }

        /// <summary>
        /// 查询并返回实体对象集合。
        /// 表名以及所有字段名，请使用类名以及字段对应的属性名，方法内转换为表名和列名
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体集</returns>
        [WebMethod(Description = "查询并返回实体对象集合")]
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAll(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
        {
            #region 海量数据查询优化
            // 海量数据尾页查询优化
            // 在海量数据分页中，取越是后面页的数据越慢，可以考虑倒序的方式
            // 只有在百万数据，且开始行大于五十万时才使用
            Int32 count = Meta.Count;
            if (startRowIndex > 500000 && count > 1000000)
            {
                // 计算本次查询的结果行数
                if (!String.IsNullOrEmpty(whereClause)) count = FindCount(whereClause, orderClause, selects, startRowIndex, maximumRows);
                // 游标在中间偏后
                if (startRowIndex * 2 > count)
                {
                    String order = orderClause;
                    Boolean bk = false; // 是否跳过

                    #region 排序倒序
                    // 默认是自增字段的降序
                    if (String.IsNullOrEmpty(order) && Meta.Unique != null && Meta.Unique.DataObjectField.IsIdentity)
                        order = Meta.Unique.Name + " Desc";

                    if (!String.IsNullOrEmpty(order))
                    {
                        String[] ss = order.Split(',');
                        StringBuilder sb = new StringBuilder();
                        foreach (String item in ss)
                        {
                            String fn = item;
                            String od = "asc";

                            Int32 p = fn.LastIndexOf(" ");
                            if (p > 0)
                            {
                                od = item.Substring(p).Trim().ToLower();
                                fn = item.Substring(0, p).Trim();
                            }

                            switch (od)
                            {
                                case "asc":
                                    od = "desc";
                                    break;
                                case "desc":
                                    //od = "asc";
                                    od = null;
                                    break;
                                default:
                                    bk = true;
                                    break;
                            }
                            if (bk) break;

                            if (sb.Length > 0) sb.Append(", ");
                            sb.AppendFormat("{0} {1}", fn, od);
                        }

                        order = sb.ToString();
                    }
                    #endregion

                    // 没有排序的实在不适合这种办法，因为没办法倒序
                    if (!String.IsNullOrEmpty(order))
                    {
                        // 最大可用行数改为实际最大可用行数
                        Int32 max = Math.Min(maximumRows, count - startRowIndex);
                        if (max <= 0) return null;
                        Int32 start = count - (startRowIndex + maximumRows);

                        String sql2 = PageSplitSQL(whereClause, order, selects, start, max);
                        EntityList<TEntity> list = LoadData(Meta.Query(sql2));
                        if (list == null || list.Count < 1) return null;
                        // 因为这样取得的数据是倒过来的，所以这里需要再倒一次
                        list.Reverse();
                        return list;
                    }
                }
            }
            #endregion

            String sql = PageSplitSQL(whereClause, orderClause, selects, startRowIndex, maximumRows);
            return LoadData(Meta.Query(sql));
        }

        /// <summary>
        /// 根据属性列表以及对应的值列表，获取所有实体对象
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns>实体数组</returns>
        public static EntityList<TEntity> FindAll(String[] names, Object[] values)
        {
            return FindAll(MakeCondition(names, values, "And"), null, null, 0, 0);
        }

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAll(String name, Object value)
        {
            return FindAll(new String[] { name }, new Object[] { value });
        }

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAll(String name, Object value, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAllByName(name, value, null, startRowIndex, maximumRows);
        }

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public static EntityList<TEntity> FindAllByName(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            if (String.IsNullOrEmpty(name))
                return FindAll(null, orderClause, null, startRowIndex, maximumRows);
            else
                return FindAll(MakeCondition(new String[] { name }, new Object[] { value }, "And"), orderClause, null, startRowIndex, maximumRows);
        }

        /// <summary>
        /// 查询SQL并返回实体对象数组。
        /// Select方法将直接使用参数指定的查询语句进行查询，不进行任何转换。
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <returns>实体数组</returns>
        public static EntityList<TEntity> FindAll(String sql)
        {
            return LoadData(Meta.Query(sql));
        }

        /// <summary>
        /// 查询并返回实体对象数组。
        /// 如果指定了jointypes参数，则同时返回参数中指定的关联对象
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="jointypes">要关联的实体类型列表</param>
        /// <returns>实体数组</returns>
        public static EntityList<TEntity> FindAllMultiple(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows, Type[] jointypes)
        {
            if (jointypes == null || jointypes.Length < 1) return FindAll(whereClause, orderClause, selects, startRowIndex, maximumRows);

            //根据传入的实体类型列表来决定处理哪些多表关联
            TableMapAttribute[] maps = XCodeConfig.TableMaps(Meta.ThisType, jointypes);
            //没有找到带有映射特性的字段
            if (maps == null || maps.Length < 1) return FindAll(whereClause, orderClause, selects, startRowIndex, maximumRows);

            String LocalTableName = Meta.TableName;
            //准备拼接SQL查询语句
            StringBuilder sb = new StringBuilder();
            sb.Append("Select ");
            //sb.Append(selects);
            if (String.IsNullOrEmpty(selects) || selects == "*" || selects.Trim() == "*")
            {
                sb.Append(XCodeConfig.SelectsEx(Meta.ThisType));
            }
            else
            {
                String[] ss = selects.Split(',');
                Boolean isfirst = false;
                foreach (String item in ss)
                {
                    if (!isfirst)
                    {
                        sb.Append(", ");
                        isfirst = true;
                    }
                    sb.AppendFormat("{0}.{1} as {2}{1}", LocalTableName, OqlToSql(item), Meta.ColumnPrefix);
                }
            }

            //对于每一个关联的实体类型表进行处理
            foreach (TableMapAttribute item in maps)
            {
                sb.Append(", ");
                sb.Append(XCodeConfig.SelectsEx(item.MapEntity));
            }
            sb.Append(" From ");
            sb.Append(LocalTableName);

            List<String> tables = new List<string>();
            tables.Add(LocalTableName);
            //对于每一个关联的实体类型表进行处理
            foreach (TableMapAttribute item in maps)
            {
                String tablename = XCodeConfig.TableName(item.MapEntity);
                tables.Add(tablename);
                sb.Append(" ");
                //关联类型
                sb.Append(item.MapType.ToString().Replace("_", " "));
                sb.Append(" ");
                //关联表
                sb.Append(tablename);
                sb.Append(" On ");
                sb.AppendFormat("{0}.{1}={2}.{3}", LocalTableName, item.LocalColumn, tablename, item.MapColumn);
            }

            if (!String.IsNullOrEmpty(whereClause))
            {
                //加上前缀
                whereClause = Regex.Replace(whereClause, "(w+)", "");
                sb.AppendFormat(" Where {0} ", OqlToSql(whereClause));
            }
            if (!String.IsNullOrEmpty(orderClause))
            {
                //加上前缀
                sb.AppendFormat(" Order By {0} ", OqlToSql(orderClause));
            }

            FieldItem fi = Meta.Unique;
            String keyColumn = null;
            if (fi != null)
            {
                keyColumn = Meta.ColumnPrefix + fi.ColumnName;
                // 加上Desc标记，将使用MaxMin分页算法。标识列，单一主键且为数字类型
                if (fi.DataObjectField.IsIdentity || fi.Property.PropertyType == typeof(Int32)) keyColumn += " Desc";
            }
            String sql = Meta.PageSplit(sb.ToString(), startRowIndex, maximumRows, keyColumn);
            DataSet ds = Meta.Query(sql, tables.ToArray());
            if (ds == null || ds.Tables.Count < 1 || ds.Tables[0].Rows.Count < 1) return null;

            return LoadData(ds.Tables[0], jointypes);
        }
        #endregion

        #region 取总记录数
        /// <summary>
        /// 返回总记录数
        /// </summary>
        /// <returns></returns>
        public static Int32 FindCount()
        {
            //Int32 count = Meta.Count;
            //if (count >= 1000) return count;

            return Meta.QueryCount(SQL(null, DataObjectMethodType.Fill));
            //return Meta.Count;
        }

        /// <summary>
        /// 返回总记录数
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        [WebMethod(Description = "查询并返回总记录数")]
        public static Int32 FindCount(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
        {
            //如果不带Where字句，直接调用FindCount，可以借助快速算法取得总记录数
            if (String.IsNullOrEmpty(whereClause)) return FindCount();

            String sql = PageSplitSQL(whereClause, null, selects, 0, 0);
            return Meta.QueryCount(sql);
        }

        /// <summary>
        /// 根据属性列表以及对应的值列表，返回总记录数
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns>总行数</returns>
        public static Int32 FindCount(String[] names, Object[] values)
        {
            return FindCount(MakeCondition(names, values, "And"), null, null, 0, 0);
        }

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>总行数</returns>
        public static Int32 FindCount(String name, Object value)
        {
            return FindCount(name, value, 0, 0);
        }

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        public static Int32 FindCount(String name, Object value, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCountByName(name, value, null, startRowIndex, maximumRows);
        }

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        public static Int32 FindCountByName(String name, Object value, String orderClause, int startRowIndex, int maximumRows)
        {
            if (String.IsNullOrEmpty(name))
                return FindCount(null, null, null, 0, 0);
            else
                return FindCount(MakeCondition(new String[] { name }, new Object[] { value }, "And"), null, null, 0, 0);
        }
        #endregion

        #region 静态操作
        /// <summary>
        /// 把一个实体对象持久化到数据库
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数</returns>
        [WebMethod(Description = "插入")]
        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public static Int32 Insert(TEntity obj)
        {
            return obj.Insert();
        }

        /// <summary>
        /// 把一个实体对象持久化到数据库
        /// </summary>
        /// <param name="names">更新属性列表</param>
        /// <param name="values">更新值列表</param>
        /// <returns>返回受影响的行数</returns>
        public static Int32 Insert(String[] names, Object[] values)
        {
            if (names == null) throw new ArgumentNullException("names", "属性列表和值列表不能为空");
            if (values == null) throw new ArgumentNullException("values", "属性列表和值列表不能为空");

            if (names.Length != values.Length) throw new ArgumentException("属性列表必须和值列表一一对应");
            //FieldItem[] fis = Meta.Fields;
            Dictionary<String, FieldItem> fs = new Dictionary<String, FieldItem>();
            foreach (FieldItem fi in Meta.Fields)
                fs.Add(fi.Name, fi);
            StringBuilder sbn = new StringBuilder();
            StringBuilder sbv = new StringBuilder();
            for (Int32 i = 0; i < names.Length; i++)
            {
                if (!fs.ContainsKey(names[i])) throw new ArgumentException("类[" + Meta.ThisType.FullName + "]中不存在[" + names[i] + "]属性");
                // 同时构造SQL语句。names是属性列表，必须转换成对应的字段列表
                if (i > 0)
                {
                    sbn.Append(", ");
                    sbv.Append(", ");
                }
                sbn.Append(Meta.FormatName(fs[names[i]].Name));
                //sbv.Append(SqlDataFormat(values[i], fs[names[i]]));
                sbv.Append(Meta.FormatValue(names[i], values[i]));
            }
            return Meta.Execute(String.Format("Insert Into {2}({0}) values({1})", sbn.ToString(), sbv.ToString(), Meta.FormatName(Meta.TableName)));
        }

        /// <summary>
        /// 把一个实体对象更新到数据库
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数</returns>
        [WebMethod(Description = "更新")]
        [DataObjectMethod(DataObjectMethodType.Update, true)]
        public static Int32 Update(TEntity obj)
        {
            return obj.Update();
        }

        /// <summary>
        /// 更新一批实体数据
        /// </summary>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        public static Int32 Update(String setClause, String whereClause)
        {
            if (String.IsNullOrEmpty(setClause) || !setClause.Contains("=")) throw new ArgumentException("非法参数");
            String sql = String.Format("Update {0} Set {1}", Meta.FormatName(Meta.TableName), setClause);
            if (!String.IsNullOrEmpty(whereClause)) sql += " Where " + whereClause;
            return Meta.Execute(sql);
        }

        /// <summary>
        /// 更新一批实体数据
        /// </summary>
        /// <param name="setNames">更新属性列表</param>
        /// <param name="setValues">更新值列表</param>
        /// <param name="whereNames">条件属性列表</param>
        /// <param name="whereValues">条件值列表</param>
        /// <returns>返回受影响的行数</returns>
        public static Int32 Update(String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues)
        {
            String sc = MakeCondition(setNames, setValues, ", ");
            String wc = MakeCondition(whereNames, whereValues, " And ");
            return Update(sc, wc);
        }

        /// <summary>
        /// 从数据库中删除指定实体对象。
        /// 实体类应该实现该方法的另一个副本，以唯一键或主键作为参数
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数，可用于判断被删除了多少行，从而知道操作是否成功</returns>
        [WebMethod(Description = "删除")]
        [DataObjectMethod(DataObjectMethodType.Delete, true)]
        public static Int32 Delete(TEntity obj)
        {
            return obj.Delete();
        }

        /// <summary>
        /// 从数据库中删除指定条件的实体对象。
        /// </summary>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        public static Int32 Delete(String whereClause)
        {
            String sql = String.Format("Delete From {0}", Meta.FormatName(Meta.TableName));
            if (!String.IsNullOrEmpty(whereClause)) sql += " Where " + whereClause;
            return Meta.Execute(sql);
        }

        /// <summary>
        /// 从数据库中删除指定属性列表和值列表所限定的实体对象。
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns></returns>
        public static Int32 Delete(String[] names, Object[] values)
        {
            return Delete(MakeCondition(names, values, "And"));
        }

        /// <summary>
        /// 把一个实体对象更新到数据库
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数</returns>
        [WebMethod(Description = "保存")]
        [DataObjectMethod(DataObjectMethodType.Update, true)]
        public static Int32 Save(TEntity obj)
        {
            return obj.Save();
        }
        #endregion

        #region 辅助方法
        private static DateTime year1900 = new DateTime(1900, 1, 1);
        private static DateTime year1753 = new DateTime(1753, 1, 1);
        private static DateTime year9999 = new DateTime(9999, 1, 1);

        /// <summary>
        /// 取得一个值的Sql值。
        /// 当这个值是字符串类型时，会在该值前后加单引号；
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="field">字段特性</param>
        /// <returns>Sql值的字符串形式</returns>
        [Obsolete("请改为使用Meta.FormatValue")]
        public static String SqlDataFormat(Object obj, String field)
        {
            //foreach (FieldItem item in Meta.Fields)
            //{
            //    if (!String.Equals(item.Name, field, StringComparison.OrdinalIgnoreCase)) continue;

            //    return SqlDataFormat(obj, item);
            //}
            //return null;

            return Meta.FormatValue(field, obj);
        }

        /// <summary>
        /// 取得一个值的Sql值。
        /// 当这个值是字符串类型时，会在该值前后加单引号；
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="field">字段特性</param>
        /// <returns>Sql值的字符串形式</returns>
        [Obsolete("请改为使用Meta.FormatValue")]
        public static String SqlDataFormat(Object obj, FieldItem field)
        {
            return Meta.FormatValue(field.Name, obj);

            //Boolean isNullable = field.DataObjectField.IsNullable;
            ////String typeName = field.Property.PropertyType.FullName;
            //TypeCode code = Type.GetTypeCode(field.Property.PropertyType);
            ////if (typeName.Contains("String"))
            //if (code == TypeCode.String)
            //{
            //    if (obj == null) return isNullable ? "null" : "''";
            //    if (String.IsNullOrEmpty(obj.ToString()) && isNullable) return "null";
            //    return "'" + obj.ToString().Replace("'", "''") + "'";
            //}
            ////else if (typeName.Contains("DateTime"))
            //else if (code == TypeCode.DateTime)
            //{
            //    if (obj == null) return isNullable ? "null" : "''";
            //    DateTime dt = Convert.ToDateTime(obj);

            //    //if (Meta.DbType == DatabaseType.Access) return "#" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "#";
            //    //if (Meta.DbType == DatabaseType.Access) return Meta.FormatDateTime(dt);

            //    //if (Meta.DbType == DatabaseType.Oracle)
            //    //    return String.Format("To_Date('{0}', 'YYYYMMDDHH24MISS')", dt.ToString("yyyyMMddhhmmss"));
            //    // SqlServer拒绝所有其不能识别为 1753 年到 9999 年间的日期的值
            //    if (Meta.DbType == DatabaseType.SqlServer)// || Meta.DbType == DatabaseType.SqlServer2005)
            //    {
            //        if (dt < year1753 || dt > year9999) return isNullable ? "null" : "''";
            //    }
            //    if ((dt == DateTime.MinValue || dt == year1900) && isNullable) return "null";
            //    //return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            //    return Meta.FormatDateTime(dt);
            //}
            ////else if (typeName.Contains("Boolean"))
            //else if (code == TypeCode.Boolean)
            //{
            //    if (obj == null) return isNullable ? "null" : "";
            //    //if (Meta.DbType == DatabaseType.SqlServer || Meta.DbType == DatabaseType.SqlServer2005)
            //    //    return Convert.ToBoolean(obj) ? "1" : "0";
            //    //else
            //    //    return obj.ToString();

            //    if (Meta.DbType == DatabaseType.Access)
            //        return obj.ToString();
            //    else
            //        return Convert.ToBoolean(obj) ? "1" : "0";
            //}
            //else if (field.Property.PropertyType == typeof(Byte[]))
            //{
            //    Byte[] bts = (Byte[])obj;
            //    if (bts == null || bts.Length < 1) return "0x0";

            //    return "0x" + BitConverter.ToString(bts).Replace("-", null);
            //}
            //else
            //{
            //    if (obj == null) return isNullable ? "null" : "";
            //    return obj.ToString();
            //}
        }

        /// <summary>
        /// 把SQL模版格式化为SQL语句
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <param name="methodType"></param>
        /// <returns>SQL字符串</returns>
        public static String SQL(Entity<TEntity> obj, DataObjectMethodType methodType)
        {
            String sql;
            StringBuilder sbNames;
            StringBuilder sbValues;
            Boolean isFirst = true;
            switch (methodType)
            {
                case DataObjectMethodType.Fill:
                    //return String.Format("Select {0} From {1}", Meta.Selects, Meta.TableName);
                    return String.Format("Select * From {0}", Meta.FormatName(Meta.TableName));
                case DataObjectMethodType.Select:
                    sql = DefaultCondition(obj);
                    // 没有标识列和主键，返回取所有数据的语句
                    if (String.IsNullOrEmpty(sql)) throw new Exception("实体类缺少主键！");
                    return String.Format("Select * From {0} Where {1}", Meta.FormatName(Meta.TableName), sql);
                case DataObjectMethodType.Insert:
                    sbNames = new StringBuilder();
                    sbValues = new StringBuilder();
                    // 只读列没有插入操作
                    foreach (FieldItem fi in Meta.Fields)
                    {
                        // 标识列不需要插入，别的类型都需要
                        String idv = null;
                        if (fi.DataObjectField.IsIdentity)
                        {
                            idv = Meta.DBO.Db.FormatIdentity(XCodeConfig.GetField(Meta.ThisType, fi.Name), obj[fi.Name]);
                            //if (String.IsNullOrEmpty(idv)) continue;
                            // 允许返回String.Empty作为插入空
                            if (idv == null) continue;
                        }

                        // 有默认值，并且没有设置值时，不参与插入操作
                        if (!String.IsNullOrEmpty(fi.Column.DefaultValue) && !obj.Dirtys[fi.Name]) continue;

                        if (!isFirst) sbNames.Append(", "); // 加逗号
                        sbNames.Append(Meta.FormatName(fi.ColumnName));
                        if (!isFirst)
                            sbValues.Append(", "); // 加逗号
                        else
                            isFirst = false;

                        //// 可空类型插入空
                        //if (!obj.Dirtys[fi.Name] && fi.DataObjectField.IsNullable)
                        //    sbValues.Append("null");
                        //else
                        //sbValues.Append(SqlDataFormat(obj[fi.Name], fi)); // 数据

                        if (!fi.DataObjectField.IsIdentity)
                            sbValues.Append(Meta.FormatValue(fi.Name, obj[fi.Name])); // 数据
                        else
                            sbValues.Append(idv);
                    }
                    return String.Format("Insert Into {0}({1}) Values({2})", Meta.FormatName(Meta.TableName), sbNames.ToString(), sbValues.ToString());
                case DataObjectMethodType.Update:
                    sbNames = new StringBuilder();
                    // 只读列没有更新操作
                    foreach (FieldItem fi in Meta.Fields)
                    {
                        if (fi.DataObjectField.IsIdentity) continue;

                        //脏数据判断
                        if (!obj.Dirtys[fi.Name]) continue;

                        if (!isFirst)
                            sbNames.Append(", "); // 加逗号
                        else
                            isFirst = false;
                        sbNames.Append(Meta.FormatName(fi.ColumnName));
                        sbNames.Append("=");
                        //sbNames.Append(SqlDataFormat(obj[fi.Name], fi)); // 数据
                        sbNames.Append(Meta.FormatValue(fi.Name, obj[fi.Name])); // 数据
                    }

                    if (sbNames.Length <= 0) return null;

                    sql = DefaultCondition(obj);
                    if (String.IsNullOrEmpty(sql)) return null;
                    return String.Format("Update {0} Set {1} Where {2}", Meta.FormatName(Meta.TableName), sbNames.ToString(), sql);
                case DataObjectMethodType.Delete:
                    // 标识列作为删除关键字
                    sql = DefaultCondition(obj);
                    if (String.IsNullOrEmpty(sql))
                        return null;
                    return String.Format("Delete From {0} Where {1}", Meta.FormatName(Meta.TableName), sql);
            }
            return null;
        }

        /// <summary>
        /// 根据属性列表和值列表，构造查询条件。
        /// 例如构造多主键限制查询条件。
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <param name="action">联合方式</param>
        /// <returns>条件子串</returns>
        [WebMethod(Description = "构造查询条件")]
        public static String MakeCondition(String[] names, Object[] values, String action)
        {
            if (names == null) throw new ArgumentNullException("names", "属性列表和值列表不能为空");
            if (values == null) throw new ArgumentNullException("values", "属性列表和值列表不能为空");

            if (names.Length != values.Length) throw new ArgumentException("属性列表必须和值列表一一对应");
            Dictionary<String, FieldItem> fs = new Dictionary<String, FieldItem>();
            foreach (FieldItem fi in Meta.Fields)
                fs.Add(fi.Name.ToLower(), fi);
            StringBuilder sb = new StringBuilder();
            for (Int32 i = 0; i < names.Length; i++)
            {
                FieldItem fi = null;
                if (!fs.TryGetValue(names[i].ToLower(), out fi))
                    throw new ArgumentException("类[" + Meta.ThisType.FullName + "]中不存在[" + names[i] + "]属性");

                // 同时构造SQL语句。names是属性列表，必须转换成对应的字段列表
                if (i > 0) sb.AppendFormat(" {0} ", action);
                sb.AppendFormat("{0}={1}", Meta.FormatName(fi.ColumnName), Meta.FormatValue(fi.Name, values[i]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 构造条件
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="action">大于小于等符号</param>
        /// <returns></returns>
        public static String MakeCondition(String name, Object value, String action)
        {
            //foreach (FieldItem item in Meta.Fields)
            //{
            //    if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            //    {
            //        return String.Format("{0}{1}{2}", item.Name, action, SqlDataFormat(value, item));
            //    }
            //}

            return String.Format("{0}{1}{2}", Meta.FormatName(name), action, Meta.FormatValue(name, value));

            throw new Exception("找不到[" + name + "]属性！");
        }

        /// <summary>
        /// 默认条件。
        /// 若有标识列，则使用一个标识列作为条件；
        /// 如有主键，则使用全部主键作为条件。
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>条件</returns>
        protected static String DefaultCondition(Entity<TEntity> obj)
        {
            Type t = obj.GetType();
            // 唯一键作为查询关键字
            List<FieldItem> ps = Meta.Uniques;
            // 没有标识列和主键，返回取所有数据的语句
            if (ps == null || ps.Count < 1) return null;
            // 标识列作为查询关键字
            if (ps[0].DataObjectField.IsIdentity)
            {
                return String.Format("{0}={1}", Meta.FormatName(ps[0].ColumnName), Meta.FormatValue(ps[0].Name, obj[ps[0].Name]));
            }
            // 主键作为查询关键字
            StringBuilder sb = new StringBuilder();
            foreach (FieldItem fi in ps)
            {
                if (sb.Length > 0) sb.Append(" And ");
                sb.Append(Meta.FormatName(fi.ColumnName));
                sb.Append("=");
                //sb.Append(SqlDataFormat(obj[fi.Name], fi));
                sb.Append(Meta.FormatValue(fi.Name, obj[fi.Name]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 把对象Oql转换称为标准TSql
        /// </summary>
        /// <param name="oql">实体对象oql</param>
        /// <returns>Sql字符串</returns>
        protected static String OqlToSql(String oql)
        {
            if (String.IsNullOrEmpty(oql)) return oql;
            String sql = oql;
            if (Meta.ThisType.Name != Meta.TableName)
                sql = Regex.Replace(sql, @"\b" + Meta.ThisType.Name + @"\b", Meta.TableName, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            foreach (FieldItem fi in Meta.Fields)
                if (fi.Name != fi.ColumnName)
                    sql = Regex.Replace(sql, @"\b" + fi.Name + @"\b", fi.ColumnName, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return sql;
        }

        /// <summary>
        /// 取得指定实体类型的分页SQL
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        protected static String PageSplitSQL(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
        {
            //StringBuilder sb = new StringBuilder();
            //sb.Append("Select ");

            //// MSSQL和Access数据库，适合使用Top
            //Boolean isTop = (Meta.DbType == DatabaseType.Access || Meta.DbType == DatabaseType.SqlServer || Meta.DbType == DatabaseType.SqlServer2005) && startRowIndex <= 0 && maximumRows > 0;
            //if (isTop) sb.AppendFormat("Top {0} ", maximumRows);

            //sb.Append(String.IsNullOrEmpty(selects) ? "*" : OqlToSql(selects));
            //sb.Append(" From ");
            //sb.Append(Meta.FormatKeyWord(Meta.TableName));
            //if (!String.IsNullOrEmpty(whereClause)) sb.AppendFormat(" Where {0} ", OqlToSql(whereClause));
            //if (!String.IsNullOrEmpty(orderClause)) sb.AppendFormat(" Order By {0} ", OqlToSql(orderClause));
            //String sql = sb.ToString();

            //// 返回所有记录
            //if (startRowIndex <= 0 && maximumRows <= 0) return sql;

            //// 使用Top
            //if (isTop) return sql;

            //return PageSplitSQL(sql, startRowIndex, maximumRows);

            SelectBuilder builder = new SelectBuilder();
            builder.Column = selects;
            //builder.Table = Meta.TableName;
            builder.Table = Meta.FormatName(Meta.TableName);
            builder.OrderBy = orderClause;
            // 谨记：某些项目中可能在where中使用了GroupBy，在分页时可能报错
            builder.Where = whereClause;

            // 返回所有记录
            if (startRowIndex <= 0 && maximumRows <= 0) return builder.ToString();

            return PageSplitSQL(builder, startRowIndex, maximumRows);
        }

        /// <summary>
        /// 取得指定实体类型的分页SQL
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        protected static String PageSplitSQL(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
        {
            FieldItem fi = Meta.Unique;
            String keyColumn = null;
            if (fi != null)
            {
                keyColumn = fi.ColumnName;
                // 加上Desc标记，将使用MaxMin分页算法。标识列，单一主键且为数字类型
                if (fi.DataObjectField.IsIdentity || fi.Property.PropertyType == typeof(Int32)) keyColumn += " Desc";

                if (String.IsNullOrEmpty(builder.OrderBy)) builder.OrderBy = keyColumn;
            }
            return Meta.PageSplit(builder, startRowIndex, maximumRows, keyColumn);
        }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，反射实现。
        /// 派生实体类可重写该索引，以避免发射带来的性能损耗。
        /// 基类已经实现了通用的快速访问，但是这里仍然重写，以增加控制，
        /// 比如字段名是属性名前面加上_，并且要求是实体字段才允许这样访问，否则一律按属性处理。
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                //匹配字段
                if (Meta.FieldNames.Contains(name))
                {
                    FieldInfoX field = FieldInfoX.Create(this.GetType(), "_" + name);
                    if (field != null) return field.GetValue(this);
                }

                //尝试匹配属性
                PropertyInfoX property = PropertyInfoX.Create(this.GetType(), name);
                if (property != null) return property.GetValue(this);

                throw new ArgumentException("类[" + this.GetType().FullName + "]中不存在[" + name + "]属性");
            }
            set
            {
                //匹配字段
                if (Meta.FieldNames.Contains(name))
                {
                    FieldInfoX field = FieldInfoX.Create(this.GetType(), "_" + name);
                    if (field != null)
                    {
                        field.SetValue(this, value);
                        return;
                    }
                }

                //尝试匹配属性
                PropertyInfoX property = PropertyInfoX.Create(this.GetType(), name);
                if (property != null)
                {
                    property.SetValue(this, value);
                    return;
                }

                foreach (FieldItem fi in Meta.AllFields)
                    if (fi.Name == name) { fi.Property.SetValue(this, value, null); return; }

                throw new ArgumentException("类[" + this.GetType().FullName + "]中不存在[" + name + "]属性");
            }
        }
        #endregion

        #region 导入导出XML
        /// <summary>
        /// 建立Xml序列化器
        /// </summary>
        /// <returns></returns>
        protected override XmlSerializer CreateXmlSerializer()
        {
            XmlAttributeOverrides ovs = new XmlAttributeOverrides();
            TEntity entity = new TEntity();
            foreach (FieldItem item in Meta.Fields)
            {
                XmlAttributes atts = new XmlAttributes();
                atts.XmlAttribute = new XmlAttributeAttribute();
                atts.XmlDefaultValue = entity[item.Name];
                ovs.Add(item.Property.DeclaringType, item.Name, atts);
            }
            return new XmlSerializer(this.GetType(), ovs);
        }

        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static TEntity FromXml(String xml)
        {
            if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();

            StopExtend = true;
            try
            {
                IEntityOperate factory = EntityFactory.CreateOperate(typeof(TEntity));
                XmlSerializer serial = ((TEntity)factory).CreateXmlSerializer();
                using (StringReader reader = new StringReader(xml))
                {
                    return serial.Deserialize(reader) as TEntity;
                }
            }
            finally { StopExtend = false; }
        }

        ///// <summary>
        ///// 高级序列化
        ///// </summary>
        ///// <param name="writer">文本读写器</param>
        ///// <param name="propertyAsAttribute">属性作为Xml属性进行序列化</param>
        ///// <param name="hasNamespace"></param>
        //public virtual void Serialize(TextWriter writer, Boolean propertyAsAttribute, Boolean hasNamespace)
        //{
        //    XmlAttributeOverrides overrides = null;
        //    overrides = new XmlAttributeOverrides();
        //    Type type = this.GetType();
        //    //IList<FieldItem> fs = FieldItem.GetDataObjectFields(type);
        //    PropertyInfo[] pis = type.GetProperties();
        //    //foreach (FieldItem item in fs)
        //    foreach (PropertyInfo item in pis)
        //    {
        //        if (!item.CanRead) continue;

        //        if (propertyAsAttribute)
        //        {
        //            XmlAttributeAttribute att = new XmlAttributeAttribute();
        //            XmlAttributes xas = new XmlAttributes();
        //            xas.XmlAttribute = att;
        //            overrides.Add(type, item.Name, xas);
        //        }
        //        else
        //        {
        //            XmlAttributes xas = new XmlAttributes();
        //            xas.XmlElements.Add(new XmlElementAttribute());
        //            overrides.Add(type, item.Name, xas);
        //        }
        //    }

        //    XmlSerializer serial = new XmlSerializer(this.GetType(), overrides);
        //    using (MemoryStream stream = new MemoryStream())
        //    {
        //        serial.Serialize(writer, this);
        //        writer.Close();
        //    }
        //}

        ///// <summary>
        ///// 高级序列化
        ///// </summary>
        ///// <param name="propertyAsAttribute">属性作为Xml属性进行序列化</param>
        ///// <param name="hasNamespace"></param>
        ///// <returns></returns>
        //public virtual String Serialize(Boolean propertyAsAttribute, Boolean hasNamespace)
        //{
        //    using (MemoryStream stream = new MemoryStream())
        //    {
        //        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        //        Serialize(writer, propertyAsAttribute, hasNamespace);
        //        writer.Close();
        //        return Encoding.UTF8.GetString(stream.ToArray());
        //    }
        //}
        #endregion

        #region 克隆
        /// <summary>
        /// 创建当前对象的克隆对象，仅拷贝基本字段
        /// </summary>
        /// <returns></returns>
        public override Object Clone()
        {
            return CloneEntity();
        }

        /// <summary>
        /// 克隆实体。创建当前对象的克隆对象，仅拷贝基本字段
        /// </summary>
        /// <returns></returns>
        public virtual TEntity CloneEntity()
        {
            //TEntity obj = new TEntity();
            TEntity obj = CreateInstance();
            foreach (FieldItem fi in Meta.Fields)
            {
                obj[fi.Name] = this[fi.Name];
            }
            return obj;
        }
        #endregion

        #region 其它
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Meta.FieldNames.Contains("Name"))
                return this["Name"] == null ? null : this["Name"].ToString();
            else if (Meta.FieldNames.Contains("ID"))
                return this["ID"] == null ? null : this["ID"].ToString();
            else
                return "实体" + Meta.ThisType.Name;
        }
        #endregion

        #region 脏数据
        //[NonSerialized]
        //private DirtyCollection _Dirtys;
        ///// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        //[XmlIgnore]
        //protected DirtyCollection Dirtys
        //{
        //    get
        //    {
        //        if (_Dirtys == null) _Dirtys = new DirtyCollection();
        //        return _Dirtys;
        //    }
        //    set { _Dirtys = value; }
        //}

        /// <summary>
        /// 设置所有数据的脏属性
        /// </summary>
        /// <param name="isDirty">改变脏属性的属性个数</param>
        /// <returns></returns>
        protected override Int32 SetDirty(Boolean isDirty)
        {
            Int32 count = 0;
            foreach (String item in Meta.FieldNames)
            {
                Boolean b = false;
                if (isDirty)
                {
                    if (!Dirtys.TryGetValue(item, out b) || !b)
                    {
                        Dirtys[item] = true;
                        count++;
                    }
                }
                else
                {
                    if (Dirtys == null || Dirtys.Count < 1) break;
                    if (Dirtys.TryGetValue(item, out b) && b)
                    {
                        Dirtys[item] = false;
                        count++;
                    }
                }
            }
            return count;
        }

        ///// <summary>
        ///// 属性改变。重载时记得调用基类的该方法，以设置脏数据属性，否则数据将无法Update到数据库。
        ///// </summary>
        ///// <param name="fieldName">字段名</param>
        ///// <param name="newValue">新属性值</param>
        ///// <returns>是否允许改变</returns>
        //protected virtual Boolean OnPropertyChange(String fieldName, Object newValue)
        //{
        //    Dirtys[fieldName] = true;
        //    return true;
        //}
        #endregion

        #region 扩展属性
        /// <summary>
        /// 获取依赖于当前实体类的扩展属性
        /// </summary>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="key">键值</param>
        /// <param name="func">回调</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        protected TResult GetExtend<TResult>(String key, Func<String, Object> func, Boolean cacheDefault = true)
        {
            return GetExtend<TEntity, TResult>(key, func);
        }

        /// <summary>
        /// 设置依赖于当前实体类的扩展属性
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void SetExtend(String key, Object value)
        {
            SetExtend<TEntity>(key, value);
        }
        #endregion

        #region 自动修改数据表结构
        //private static Object schemasLock = new Object();
        //private static Boolean hasChecked = false;
        ///// <summary>
        ///// 检查数据表架构是否已被修改
        ///// </summary>
        //private static void CheckModify()
        //{
        //    if (hasChecked) return;
        //    lock (schemasLock)
        //    {
        //        if (hasChecked) return;

        //        DatabaseSchema schema = new DatabaseSchema(Meta.ConnName, Meta.ThisType);
        //        schema.BeginCheck();

        //        hasChecked = true;
        //    }
        //}
        #endregion
    }
}