using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Web.Services;
using System.Xml.Serialization;
using NewLife.IO;
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
            EntityFactory.Register(Meta.ThisType, new EntityOperate());

            // 1，可以初始化该实体类型的操作工厂
            // 2，CreateOperate将会实例化一个TEntity对象，从而引发TEntity的静态构造函数，
            // 避免实际应用中，直接调用Entity的静态方法时，没有引发TEntity的静态构造函数。
            TEntity entity = new TEntity();

            ////! 大石头 2011-03-14 以下过程改为异步处理
            ////  已确认，当实体类静态构造函数中使用了EntityFactory.CreateOperate(Type)方法时，可能出现死锁。
            ////  因为两者都会争夺EntityFactory中的op_cache，而CreateOperate(Type)拿到op_cache后，还需要等待当前静态构造函数执行完成。
            ////  不确定这样子是否带来后遗症
            //ThreadPool.QueueUserWorkItem(delegate
            //{
            //    EntityFactory.CreateOperate(Meta.ThisType, entity);
            //});
        }

        /// <summary>
        /// 创建实体
        /// </summary>
        /// <returns></returns>
        protected virtual TEntity CreateInstance()
        {
            //return new TEntity();
            // new TEntity会被编译为Activator.CreateInstance<TEntity>()，还不如Activator.CreateInstance()呢
            // Activator.CreateInstance()有缓存功能，而泛型的那个没有
            return Activator.CreateInstance(Meta.ThisType) as TEntity;
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
        public static EntityList<TEntity> LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count < 1) return null;

            // 准备好实体列表
            EntityList<TEntity> list = new EntityList<TEntity>(dt.Rows.Count);

            // 计算都有哪些字段可以加载数据，默认是使用了BindColumn特性的属性，然后才是别的属性
            // 当然，要数据集中有这个列才行，也就是取实体类和数据集的交集
            List<FieldItem> ps = CheckColumn(dt);

            // 创建实体操作者，将由实体操作者创建实体对象
            IEntityOperate factory = Meta.Factory;

            // 遍历每一行数据，填充成为实体
            foreach (DataRow dr in dt.Rows)
            {
                //TEntity obj = new TEntity();
                // 由实体操作者创建实体对象，因为实体操作者可能更换
                TEntity obj = factory.Create() as TEntity;
                obj.LoadData(dr, ps);
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

            // 计算都有哪些字段可以加载数据
            List<FieldItem> ps = CheckColumn(dr.Table);
            LoadData(dr, ps);
        }

        static String[] TrueString = new String[] { "true", "y", "yes", "1" };
        static String[] FalseString = new String[] { "false", "n", "no", "0" };

        /// <summary>
        /// 从一个数据行对象加载数据。指定要加载数据的字段。
        /// </summary>
        /// <param name="dr">数据行</param>
        /// <param name="ps">要加载数据的字段</param>
        /// <returns></returns>
        private void LoadData(DataRow dr, IList<FieldItem> ps)
        {
            if (dr == null) return;

            // 如果没有传入要加载数据的字段，则使用全部数据属性
            // 这种情况一般不会发生，最好也不好发生，因为很有可能导致报错
            if (ps == null || ps.Count < 1) ps = Meta.Fields;

            foreach (FieldItem fi in ps)
            {
                // 两次dr[fi.ColumnName]简化为一次
                Object v = dr[fi.ColumnName];
                Object v2 = this[fi.Name];

                // 不处理相同数据的赋值
                if (Object.Equals(v, v2)) continue;

                if (fi.Type == typeof(String))
                {
                    // 不处理空字符串对空字符串的赋值
                    if (v != null && String.IsNullOrEmpty(v.ToString()))
                    {
                        if (v2 == null || String.IsNullOrEmpty(v2.ToString())) continue;
                    }
                }
                else if (fi.Type == typeof(Boolean))
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
        }

        /// <summary>
        /// 检查实体类中的哪些字段在数据表中
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <returns></returns>
        private static List<FieldItem> CheckColumn(DataTable dt)
        {
            List<FieldItem> ps = new List<FieldItem>();
            foreach (FieldItem item in Meta.AllFields)
            {
                if (String.IsNullOrEmpty(item.ColumnName)) continue;

                if (dt.Columns.Contains(item.ColumnName)) ps.Add(item);
            }
            return ps;
        }

        /// <summary>
        /// 把数据复制到数据行对象中。
        /// </summary>
        /// <param name="dr">数据行</param>
        public virtual DataRow ToData(ref DataRow dr)
        {
            if (dr == null) return null;

            foreach (FieldItem fi in Meta.AllFields)
            {
                // 检查dr中是否有该属性的列。考虑到Select可能是不完整的，此时，只需要局部填充
                if (dr.Table.Columns.Contains(fi.ColumnName))
                    dr[fi.ColumnName] = this[fi.Name];
            }
            return dr;
        }
        #endregion

        #region 操作
        /// <summary>
        /// 插入数据，通过调用OnInsert实现，另外增加了数据验证和事务保护支持，将来可能实现事件支持。
        /// </summary>
        /// <returns></returns>
        public override Int32 Insert()
        {
            Valid(true);

            Meta.BeginTrans();
            try
            {
                Int32 rs = OnInsert();

                Meta.Commit();

                return rs;
            }
            catch { Meta.Rollback(); throw; }
        }

        /// <summary>
        /// 把该对象持久化到数据库。该方法提供原生的数据操作，不建议重载，建议重载Insert代替。
        /// </summary>
        /// <returns></returns>
        protected virtual Int32 OnInsert()
        {
            String sql = SQL(this, DataObjectMethodType.Insert);
            if (String.IsNullOrEmpty(sql)) return 0;

            Int32 rs = 0;

            //检查是否有标识列，标识列需要特殊处理
            FieldItem field = Meta.Table.Identity;
            if (field != null && field.IsIdentity)
            {
                Int64 res = Meta.InsertAndGetIdentity(sql);
                if (res > 0) this[field.Name] = res;
                rs = res > 0 ? 1 : 0;
            }
            else
            {
                rs = Meta.Execute(sql);
            }

            //清除脏数据，避免连续两次调用Save造成重复提交
            if (Dirtys != null)
            {
                Dirtys.Clear();
                Dirtys = null;
            }
            return rs;
        }

        /// <summary>
        /// 更新数据，通过调用OnUpdate实现，另外增加了数据验证和事务保护支持，将来可能实现事件支持。
        /// </summary>
        /// <returns></returns>
        public override Int32 Update()
        {
            Valid(false);

            Meta.BeginTrans();
            try
            {
                Int32 rs = OnUpdate();

                Meta.Commit();

                return rs;
            }
            catch { Meta.Rollback(); throw; }
        }

        /// <summary>
        /// 更新数据库
        /// </summary>
        /// <returns></returns>
        protected virtual Int32 OnUpdate()
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
        /// 删除数据，通过调用OnDelete实现，另外增加了数据验证和事务保护支持，将来可能实现事件支持。
        /// </summary>
        /// <returns></returns>
        public override Int32 Delete()
        {
            Meta.BeginTrans();
            try
            {
                Int32 rs = OnDelete();

                Meta.Commit();

                return rs;
            }
            catch { Meta.Rollback(); throw; }
        }

        /// <summary>
        /// 从数据库中删除该对象
        /// </summary>
        /// <returns></returns>
        protected virtual Int32 OnDelete()
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
            FieldItem fi = Meta.Table.Identity;
            if (fi != null) return Convert.ToInt64(this[fi.Name]) > 0 ? Update() : Insert();

            fi = Meta.Unique;
            if (fi != null) return IsNullKey(this[fi.Name]) ? Insert() : Update();

            return FindCount(DefaultCondition(this), null, null, 0, 0) > 0 ? Update() : Insert();
        }

        /// <summary>
        /// 验证数据，通过抛出异常的方式提示验证失败。建议重写者调用基类的实现，因为将来可能根据数据字段的特性进行数据验证。
        /// </summary>
        /// <param name="isNew">是否新数据</param>
        public virtual void Valid(Boolean isNew)
        {
        }

        /// <summary>
        /// 根据指定键检查数据是否已存在，若不存在，抛出ArgumentOutOfRangeException异常
        /// </summary>
        /// <param name="names"></param>
        public virtual void CheckExist(params String[] names)
        {
            if (Exist(names))
            {
                StringBuilder sb = new StringBuilder();
                String name = null;
                for (int i = 0; i < names.Length; i++)
                {
                    if (sb.Length > 0) sb.Append("，");

                    FieldItem field = Meta.Table.FindByName(names[i]);
                    if (field != null) name = field.Description;
                    if (String.IsNullOrEmpty(name)) name = names[i];

                    sb.AppendFormat("{0}={1}", name, this[names[i]]);
                }

                name = Meta.Table.Description;
                if (String.IsNullOrEmpty(name)) name = Meta.ThisType.Name;
                sb.AppendFormat(" 的{0}已存在！", name);

                throw new ArgumentOutOfRangeException(names[0], this[names[0]], sb.ToString());
            }
        }

        /// <summary>
        /// 根据指定键检查数据，返回数据是否已存在
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public virtual Boolean Exist(params String[] names)
        {
            // 根据指定键查找所有符合的数据，然后比对。
            // 当然，也可以通过指定键和主键配合，找到拥有指定键，但是不是当前主键的数据，只查记录数。
            Object[] values = new Object[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                values[i] = this[names[i]];
            }

            FieldItem field = Meta.Unique;
            // 如果是空主键，则采用直接判断记录数的方式，以加快速度
            if (IsNullKey(this[field.Name])) return FindCount(names, values) > 0;

            EntityList<TEntity> list = FindAll(names, values);
            if (list == null) return false;
            if (list.Count > 1) return true;

            return !Object.Equals(this[field.Name], list[0][field.Name]);
        }

        //public event EventHandler<CancelEventArgs> Inserting;
        //public event EventHandler<EventArgs<Int32>> Inserted;
        #endregion

        #region 查找单个实体
        /// <summary>
        /// 根据属性以及对应的值，查找单个实体
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <param name="value">属性值</param>
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
        /// <param name="names">属性名称集合</param>
        /// <param name="values">属性值集合</param>
        /// <returns></returns>
        public static TEntity Find(String[] names, Object[] values)
        {
            if (names.Length == 1)
            {
                FieldItem field = Meta.Table.FindByName(names[0]);
                if (field != null && (field.IsIdentity || field.PrimaryKey))
                {
                    // 唯一键为自增且参数小于等于0时，返回空
                    if (IsNullKey(values[0])) return null;

                    // 自增或者主键查询，记录集肯定是唯一的，不需要指定记录数和排序
                    //IList<TEntity> list = FindAll(MakeCondition(field, values[0], "="), null, null, 0, 0);
                    SelectBuilder builder = new SelectBuilder();
                    builder.Table = Meta.FormatName(Meta.TableName);
                    builder.Where = MakeCondition(field, values[0], "=");
                    IList<TEntity> list = FindAll(builder.ToString());
                    if (list == null || list.Count < 1)
                        return null;
                    else
                        return list[0];
                }
            }

            return Find(MakeCondition(names, values, "And"));
        }

        /// <summary>
        /// 根据条件查找单个实体
        /// </summary>
        /// <param name="whereClause">查询条件</param>
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
        /// <param name="key">唯一主键的值</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKey(Object key)
        {
            FieldItem field = Meta.Unique;
            if (field == null) throw new ArgumentNullException("Meta.Unique", "FindByKey方法要求该表有唯一主键！");

            // 唯一键为自增且参数小于等于0时，返回空
            if (IsNullKey(key)) return null;

            return Find(field.Name, key);
        }

        /// <summary>
        /// 根据主键查询一个实体对象用于表单编辑
        /// </summary>
        /// <param name="key">唯一主键的值</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKeyForEdit(Object key)
        {
            FieldItem field = Meta.Unique;
            if (field == null) throw new ArgumentNullException("Meta.Unique", "FindByKeyForEdit方法要求该表有唯一主键！");

            // 参数为空时，返回新实例
            if (key == null)
            {
                //IEntityOperate factory = EntityFactory.CreateOperate(Meta.ThisType);
                return Meta.Factory.Create() as TEntity;
            }

            Type type = field.Type;

            // 唯一键为自增且参数小于等于0时，返回新实例
            if (IsNullKey(key))
            {
                if (IsInt(type) && !field.IsIdentity && DAL.Debug) DAL.WriteLog("{0}的{1}字段是整型主键，你是否忘记了设置自增？", Meta.TableName, field.ColumnName);

                return Meta.Factory.Create() as TEntity;
            }

            // 此外，一律返回 查找值，即使可能是空。而绝不能在找不到数据的情况下给它返回空，因为可能是找不到数据而已，而返回新实例会导致前端以为这里是新增数据
            TEntity entity = Find(field.Name, key);

            // 判断实体
            if (entity == null)
            {
                String msg = null;
                if (IsNullKey(key))
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！可能未设置自增主键！", key, Meta.Table.Description);
                else
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！", key, Meta.Table.Description);

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

        /// <summary>
        /// 指定键是否为空。一般业务系统设计不允许主键为空，包括自增的0和字符串的空
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        static Boolean IsNullKey(Object key)
        {
            if (key == null) return true;

            Type type = key.GetType();

            //if (IsInt(type))
            //{
            //    int i = (int)key;
            //    //这里需要转换城明确类型否则会引发类型转换异常
            //    return ((Int64)i) <= 0;
            //}
            //if (IsInt(type))
            //{
            //由于key的实际类型是由类型推倒而来，所以必须根据实际传入的参数类型分别进行装箱操作
            //如果不根据类型分别进行会导致类型转换失败抛出异常
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16: return ((Int16)key) <= 0;
                case TypeCode.Int32: return ((Int32)key) <= 0;
                case TypeCode.Int64: return ((Int64)key) <= 0;
                case TypeCode.UInt16: return ((UInt16)key) <= 0;
                case TypeCode.UInt32: return ((UInt32)key) <= 0;
                case TypeCode.UInt64: return ((UInt64)key) <= 0;
                case TypeCode.String: return String.IsNullOrEmpty((String)key);
                default: break;
            }
            //}
            //if (type == typeof(String)) return String.IsNullOrEmpty((String)key);

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
            return FindAll(SQL(null, DataObjectMethodType.Fill));
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
            //Int32 count = Meta.Count;
            //if (startRowIndex > 500000 && count > 1000000)

            // 如下优化，避免了每次都调用Meta.Count而导致形成一次查询，虽然这次查询时间损耗不大
            // 但是绝大多数查询，都不需要进行类似的海量数据优化，显然，这个startRowIndex将会挡住99%以上的浪费
            Int32 count = 0;
            if (startRowIndex > 500000 && (count = Meta.Count) > 1000000)
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
                    FieldItem fi = Meta.Unique;
                    if (String.IsNullOrEmpty(order) && fi != null && fi.IsIdentity) order = fi.Name + " Desc";

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
            if (String.IsNullOrEmpty(name)) return FindAll(null, orderClause, null, startRowIndex, maximumRows);

            FieldItem field = Meta.Table.FindByName(name);
            if (field != null && (field.IsIdentity || field.PrimaryKey))
            {
                // 唯一键为自增且参数小于等于0时，返回空
                if (IsNullKey(value)) return null;

                // 自增或者主键查询，记录集肯定是唯一的，不需要指定记录数和排序
                //return FindAll(MakeCondition(field, value, "="), null, null, 0, 0);
                SelectBuilder builder = new SelectBuilder();
                builder.Table = Meta.FormatName(Meta.TableName);
                builder.Where = MakeCondition(field, value, "=");
                return FindAll(builder.ToString());
            }

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
        #endregion

        #region 缓存查询
        /// <summary>
        /// 根据属性以及对应的值，在缓存中查找单个实体
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindWithCache(String name, Object value)
        {
            return Entity<TEntity>.Meta.Cache.Entities.Find(name, value);
        }

        /// <summary>
        /// 查找所有缓存
        /// </summary>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllWithCache()
        {
            return Entity<TEntity>.Meta.Cache.Entities;
        }

        /// <summary>
        /// 根据属性以及对应的值，在缓存中获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllWithCache(String name, Object value)
        {
            return Entity<TEntity>.Meta.Cache.Entities;
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

            //return Meta.QueryCount(SQL(null, DataObjectMethodType.Fill));
            //return Meta.Count;

            //SelectBuilder sb = new SelectBuilder(Meta.DbType);
            //sb.Column = "Count(*)";
            //sb.Table = Meta.FormatName(Meta.TableName);

            //return Meta.QueryCount(sb);

            return FindCount(null, null, null, 0, 0);
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
            ////如果不带Where字句，直接调用FindCount，可以借助快速算法取得总记录数
            //if (String.IsNullOrEmpty(whereClause)) return FindCount();

            //String sql = PageSplitSQL(whereClause, null, selects, 0, 0);
            //return Meta.QueryCount(sql);

            SelectBuilder sb = new SelectBuilder(Meta.DbType);
            //sb.Column = "Count(*)";
            sb.Table = Meta.FormatName(Meta.TableName);
            sb.Where = whereClause;

            return Meta.QueryCount(sb);
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
        [DisplayName("插入")]
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
        [DisplayName("更新")]
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
        //[DataObjectMethod(DataObjectMethodType.Update, true)]
        public static Int32 Save(TEntity obj)
        {
            return obj.Save();
        }
        #endregion

        #region 辅助方法
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
                        if (fi.IsIdentity)
                        {
                            idv = Meta.DBO.Db.FormatIdentity(fi.Field, obj[fi.Name]);
                            //if (String.IsNullOrEmpty(idv)) continue;
                            // 允许返回String.Empty作为插入空
                            if (idv == null) continue;
                        }

                        // 有默认值，并且没有设置值时，不参与插入操作
                        if (!String.IsNullOrEmpty(fi.DefaultValue) && !obj.Dirtys[fi.Name]) continue;

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

                        if (!fi.IsIdentity)
                            sbValues.Append(Meta.FormatValue(fi, obj[fi.Name])); // 数据
                        else
                            sbValues.Append(idv);
                    }
                    return String.Format("Insert Into {0}({1}) Values({2})", Meta.FormatName(Meta.TableName), sbNames.ToString(), sbValues.ToString());
                case DataObjectMethodType.Update:
                    sbNames = new StringBuilder();
                    // 只读列没有更新操作
                    foreach (FieldItem fi in Meta.Fields)
                    {
                        if (fi.IsIdentity) continue;

                        //脏数据判断
                        if (!obj.Dirtys[fi.Name]) continue;

                        if (!isFirst)
                            sbNames.Append(", "); // 加逗号
                        else
                            isFirst = false;
                        sbNames.Append(Meta.FormatName(fi.ColumnName));
                        sbNames.Append("=");
                        //sbNames.Append(SqlDataFormat(obj[fi.Name], fi)); // 数据
                        sbNames.Append(Meta.FormatValue(fi, obj[fi.Name])); // 数据
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

            StringBuilder sb = new StringBuilder();
            for (Int32 i = 0; i < names.Length; i++)
            {
                FieldItem fi = Meta.Table.FindByName(names[i]);
                if (fi == null) throw new ArgumentException("类[" + Meta.ThisType.FullName + "]中不存在[" + names[i] + "]属性");

                // 同时构造SQL语句。names是属性列表，必须转换成对应的字段列表
                if (i > 0) sb.AppendFormat(" {0} ", action);
                //sb.AppendFormat("{0}={1}", Meta.FormatName(fi.ColumnName), Meta.FormatValue(fi, values[i]));
                sb.Append(MakeCondition(fi, values[i], "="));
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
            FieldItem field = Meta.Table.FindByName(name);
            if (field == null) return String.Format("{0}{1}{2}", Meta.FormatName(name), action, Meta.FormatValue(name, value));

            return MakeCondition(field, value, action);
        }

        /// <summary>
        /// 构造条件
        /// </summary>
        /// <param name="field">名称</param>
        /// <param name="value">值</param>
        /// <param name="action">大于小于等符号</param>
        /// <returns></returns>
        public static String MakeCondition(FieldItem field, Object value, String action)
        {
            if (!String.IsNullOrEmpty(action) && action.Contains("{0}"))
                return Meta.FormatName(field.ColumnName) + String.Format(action, Meta.FormatValue(field, value));
            else
                return String.Format("{0}{1}{2}", Meta.FormatName(field.ColumnName), action, Meta.FormatValue(field, value));
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
            // 标识列作为查询关键字
            FieldItem fi = Meta.Table.Identity;
            if (fi != null) return MakeCondition(fi, obj[fi.Name], "=");

            // 主键作为查询关键字
            FieldItem[] ps = Meta.Table.PrimaryKeys;
            // 没有标识列和主键，返回取所有数据的语句
            if (ps == null || ps.Length < 1) return null;

            StringBuilder sb = new StringBuilder();
            foreach (FieldItem item in ps)
            {
                if (sb.Length > 0) sb.Append(" And ");
                sb.Append(Meta.FormatName(item.ColumnName));
                sb.Append("=");
                sb.Append(Meta.FormatValue(item, obj[item.Name]));
            }
            return sb.ToString();
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
            SelectBuilder builder = new SelectBuilder();
            builder.Column = selects;
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
                if (fi.IsIdentity || IsInt(fi.Type))
                {
                    keyColumn += " Desc";

                    // 默认获取数据时，还是需要指定安装自增字段降序，符合使用习惯
                    // 有GroupBy也不能加排序
                    if (String.IsNullOrEmpty(builder.OrderBy) && String.IsNullOrEmpty(builder.GroupBy)) builder.OrderBy = keyColumn;
                }
                //if (fi.IsIdentity || IsInt(fi.Type)) keyColumn += " Unknown";

                //if (String.IsNullOrEmpty(builder.OrderBy)) builder.OrderBy = keyColumn;
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

                Object obj = null;
                if (Extends.TryGetValue(name, out obj)) return obj;

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

                //foreach (FieldItem fi in Meta.AllFields)
                //    if (fi.Name == name) { fi.Property.SetValue(this, value, null); return; }

                if (Extends.ContainsKey(name))
                    Extends[name] = value;
                else
                    Extends.Add(name, value);

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
            // 给每一个数据属性加上Xml默认值特性，让Xml序列化时避开数据与默认值相同的数据属性，减少Xml大小
            XmlAttributeOverrides ovs = new XmlAttributeOverrides();
            TEntity entity = new TEntity();
            foreach (FieldItem item in Meta.Fields)
            {
                XmlAttributes atts = new XmlAttributes();
                atts.XmlAttribute = new XmlAttributeAttribute();
                atts.XmlDefaultValue = entity[item.Name];
                ovs.Add(item.DeclaringType, item.Name, atts);
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
                //IEntityOperate factory = EntityFactory.CreateOperate(typeof(TEntity));
                XmlSerializer serial = ((TEntity)Meta.Factory).CreateXmlSerializer();
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

        #region 导入导出Json
        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static TEntity FromJson(String json)
        {
            return new Json().Deserialize<TEntity>(json);
        }
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
        #endregion

        #region 扩展属性
        /// <summary>
        /// 获取依赖于当前实体类的扩展属性
        /// </summary>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        protected TResult GetExtend<TResult>(String key, Func<String, Object> func)
        {
            return GetExtend<TEntity, TResult>(key, func);
        }

        /// <summary>
        /// 获取依赖于当前实体类的扩展属性
        /// </summary>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="func">回调</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        protected TResult GetExtend<TResult>(String key, Func<String, Object> func, Boolean cacheDefault)
        {
            return GetExtend<TEntity, TResult>(key, func);
        }

        /// <summary>
        /// 设置依赖于当前实体类的扩展属性
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        protected void SetExtend(String key, Object value)
        {
            SetExtend<TEntity>(key, value);
        }
        #endregion
    }
}