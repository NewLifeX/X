using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using NewLife.IO;
using NewLife.Linq;
using NewLife.Reflection;
using XCode.Common;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Exceptions;
using XCode.Model;

namespace XCode
{
    /// <summary>数据实体类基类。所有数据实体类都必须继承该类。</summary>
    [Serializable]
    public partial class Entity<TEntity> : EntityBase where TEntity : Entity<TEntity>, new()
    {
        #region 构造函数
        /// <summary>静态构造</summary>
        static Entity()
        {
            DAL.WriteDebugLog("开始初始化实体类{0}", Meta.ThisType.Name);

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

            DAL.WriteDebugLog("完成初始化实体类{0}", Meta.ThisType.Name);
        }

        /// <summary>
        /// 创建实体。可以重写改方法以实现实体对象的一些初始化工作。
        /// 切记，写为实例方法仅仅是为了方便重载，所要返回的实例绝对不会是当前实例。
        /// </summary>
        /// <param name="forEdit">是否为了编辑而创建，如果是，可以再次做一些相关的初始化工作</param>
        /// <returns></returns>
        protected virtual TEntity CreateInstance(Boolean forEdit = false)
        {
            //return new TEntity();
            // new TEntity会被编译为Activator.CreateInstance<TEntity>()，还不如Activator.CreateInstance()呢
            // Activator.CreateInstance()有缓存功能，而泛型的那个没有
            return Activator.CreateInstance(Meta.ThisType) as TEntity;
        }
        #endregion

        #region 填充数据
        /// <summary>
        /// 加载记录集。无数据时返回空集合而不是null。
        /// </summary>
        /// <param name="ds">记录集</param>
        /// <returns>实体数组</returns>
        public static EntityList<TEntity> LoadData(DataSet ds) { return LoadData(ds.Tables[0]); }

        /// <summary>
        /// 加载数据表。无数据时返回空集合而不是null。
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <returns>实体数组</returns>
        public static EntityList<TEntity> LoadData(DataTable dt)
        {
            IEntityList list = dreAccessor.LoadData(dt);
            if (list is EntityList<TEntity>) return list as EntityList<TEntity>;

            return new EntityList<TEntity>(list);
        }

        /// <summary>
        /// 从一个数据行对象加载数据。不加载关联对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        public override void LoadData(DataRow dr) { if (dr != null) dreAccessor.LoadData(dr, this); }

        /// <summary>
        /// 加载数据读写器。无数据时返回空集合而不是null。
        /// </summary>
        /// <param name="dr">数据读写器</param>
        /// <returns>实体数组</returns>
        public static EntityList<TEntity> LoadData(IDataReader dr)
        {
            IEntityList list = dreAccessor.LoadData(dr);
            if (list is EntityList<TEntity>) return list as EntityList<TEntity>;

            return new EntityList<TEntity>(list);
        }

        /// <summary>
        /// 从一个数据行对象加载数据。不加载关联对象。
        /// </summary>
        /// <param name="dr">数据读写器</param>
        public override void LoadDataReader(IDataReader dr) { if (dr != null)  dreAccessor.LoadData(dr, this); }

        /// <summary>
        /// 把数据复制到数据行对象中。
        /// </summary>
        /// <param name="dr">数据行</param>
        public virtual DataRow ToData(ref DataRow dr) { return dr == null ? null : dreAccessor.ToData(this, ref dr); }

        private static IDataRowEntityAccessor dreAccessor { get { return XCodeService.CreateDataRowEntityAccessor(Meta.ThisType); } }
        #endregion

        #region 操作
        private static IEntityPersistence persistence { get { return XCodeService.Resolve<IEntityPersistence>(); } }

        /// <summary>
        /// 插入数据，通过调用OnInsert实现，另外增加了数据验证和事务保护支持，将来可能实现事件支持。
        /// </summary>
        /// <returns></returns>
        public override Int32 Insert() { return DoAction(OnInsert, true); }

        /// <summary>
        /// 把该对象持久化到数据库。该方法提供原生的数据操作，不建议重载，建议重载Insert代替。
        /// </summary>
        /// <returns></returns>
        protected virtual Int32 OnInsert() { return persistence.Insert(this); }

        /// <summary>
        /// 更新数据，通过调用OnUpdate实现，另外增加了数据验证和事务保护支持，将来可能实现事件支持。
        /// </summary>
        /// <returns></returns>
        public override Int32 Update() { return DoAction(OnUpdate, false); }

        /// <summary>
        /// 更新数据库
        /// </summary>
        /// <returns></returns>
        protected virtual Int32 OnUpdate() { return persistence.Update(this); }

        /// <summary>
        /// 删除数据，通过调用OnDelete实现，另外增加了数据验证和事务保护支持，将来可能实现事件支持。
        /// </summary>
        /// <returns></returns>
        public override Int32 Delete() { return DoAction(OnDelete, null); }

        /// <summary>
        /// 从数据库中删除该对象
        /// </summary>
        /// <returns></returns>
        protected virtual Int32 OnDelete() { return persistence.Delete(this); }

        Int32 DoAction(Func<Int32> func, Boolean? isnew)
        {
            if (isnew != null) Valid(isnew.Value);

            Meta.BeginTrans();
            try
            {
                Int32 rs = func();

                Meta.Commit();

                return rs;
            }
            catch { Meta.Rollback(); throw; }
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
            if (fi != null) return Helper.IsNullKey(this[fi.Name]) ? Insert() : Update();

            return FindCount(persistence.GetPrimaryCondition(this), null, null, 0, 0) > 0 ? Update() : Insert();
        }

        /// <summary>
        /// 验证数据，通过抛出异常的方式提示验证失败。建议重写者调用基类的实现，因为将来可能根据数据字段的特性进行数据验证。
        /// </summary>
        /// <param name="isNew">是否新数据</param>
        public virtual void Valid(Boolean isNew)
        {
            // 根据索引，判断唯一性
            IDataTable table = Meta.Table.DataTable;
            if (table.Indexes != null && table.Indexes.Count > 0)
            {
                // 遍历所有索引
                foreach (IDataIndex item in table.Indexes)
                {
                    // 只处理唯一索引
                    if (!item.Unique) continue;

                    // 需要转为别名，也就是字段名
                    IDataColumn[] columns = table.GetColumns(item.Columns);
                    if (columns == null || columns.Length < 1) continue;

                    // 不处理自增
                    if (columns.All(c => c.Identity)) continue;

                    // 记录字段是否有更新
                    Boolean changed = false;
                    if (!isNew) changed = columns.Any(c => Dirtys[c.Alias]);

                    // 存在检查
                    if (isNew || changed) CheckExist(columns.Select(c => c.Alias).Distinct().ToArray());
                }
            }
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

                throw new ArgumentOutOfRangeException(String.Join(",", names), this[names[0]], sb.ToString());
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
            if (Helper.IsNullKey(this[field.Name])) return FindCount(names, values) > 0;

            EntityList<TEntity> list = FindAll(names, values);
            if (list == null || list.Count < 1) return false;
            if (list.Count > 1) return true;

            return !Object.Equals(this[field.Name], list[0][field.Name]);
        }
        #endregion

        #region 查找单个实体
        /// <summary>
        /// 根据属性以及对应的值，查找单个实体
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity Find(String name, Object value) { return Find(new String[] { name }, new Object[] { value }); }

        /// <summary>
        /// 根据属性列表以及对应的值列表，查找单个实体
        /// </summary>
        /// <param name="names">属性名称集合</param>
        /// <param name="values">属性值集合</param>
        /// <returns></returns>
        public static TEntity Find(String[] names, Object[] values)
        {
            // 判断自增和主键
            if (names != null && names.Length == 1)
            {
                FieldItem field = Meta.Table.FindByName(names[0]);
                if (field != null && (field.IsIdentity || field.PrimaryKey))
                {
                    // 唯一键为自增且参数小于等于0时，返回空
                    if (Helper.IsNullKey(values[0])) return null;

                    return FindUnique(MakeCondition(field, values[0], "="));
                }
            }

            // 判断唯一索引，唯一索引也不需要分页
            IDataIndex di = Meta.Table.DataTable.GetIndex(names);
            if (di != null && di.Unique) return FindUnique(MakeCondition(names, values, "And"));

            return Find(MakeCondition(names, values, "And"));
        }

        /// <summary>
        /// 根据条件查找唯一的单个实体，因为是唯一的，所以不需要分页和排序。
        /// 如果不确定是否唯一，一定不要调用该方法，否则会返回大量的数据。
        /// </summary>
        /// <param name="whereClause">查询条件</param>
        /// <returns></returns>
        static TEntity FindUnique(String whereClause)
        {
            SelectBuilder builder = new SelectBuilder();
            builder.Table = Meta.FormatName(Meta.TableName);
            // 谨记：某些项目中可能在where中使用了GroupBy，在分页时可能报错
            builder.Where = whereClause;
            IList<TEntity> list = LoadData(Meta.Query(builder.ToString()));
            if (list == null || list.Count < 1) return null;

            if (list.Count > 1 && DAL.Debug)
            {
                DAL.WriteDebugLog("调用FindUnique(\"{0}\")不合理，只有返回唯一记录的查询条件才允许调用！", whereClause);
                NewLife.Log.XTrace.DebugStack(5);
            }
            return list[0];
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
            if (Helper.IsNullKey(key)) return null;

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
                return Meta.Factory.Create(true) as TEntity;
            }

            Type type = field.Type;

            // 唯一键为自增且参数小于等于0时，返回新实例
            if (Helper.IsNullKey(key))
            {
                if (Helper.IsIntType(type) && !field.IsIdentity && DAL.Debug) DAL.WriteLog("{0}的{1}字段是整型主键，你是否忘记了设置自增？", Meta.TableName, field.ColumnName);

                return Meta.Factory.Create(true) as TEntity;
            }

            // 此外，一律返回 查找值，即使可能是空。而绝不能在找不到数据的情况下给它返回空，因为可能是找不到数据而已，而返回新实例会导致前端以为这里是新增数据
            TEntity entity = Find(field.Name, key);

            // 判断实体
            if (entity == null)
            {
                String msg = null;
                if (Helper.IsNullKey(key))
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！可能未设置自增主键！", key, Meta.Table.Description);
                else
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！", key, Meta.Table.Description);

                throw new XCodeException(msg);
            }

            return entity;
        }
        #endregion

        #region 静态查询
        /// <summary>
        /// 获取所有实体对象。获取大量数据时会非常慢，慎用
        /// </summary>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAll() { return FindAll(String.Format("Select * From {0}", Meta.FormatName(Meta.TableName))); }

        /// <summary>
        /// 查询并返回实体对象集合。
        /// 最经典的批量查询，看这个Select @selects From Table Where @whereClause Order By @orderClause Limit @startRowIndex,@maximumRows，你就明白各参数的意思了。
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体集</returns>
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
            Int64 count = 0;
            if (startRowIndex > 500000 && (count = Meta.LongCount) > 1000000)
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
                        Int32 max = (Int32)Math.Min(maximumRows, count - startRowIndex);
                        //if (max <= 0) return null;
                        if (max <= 0) return new EntityList<TEntity>();
                        Int32 start = (Int32)(count - (startRowIndex + maximumRows));

                        String sql2 = PageSplitSQL(whereClause, order, selects, start, max);
                        EntityList<TEntity> list = LoadData(Meta.Query(sql2));
                        if (list == null || list.Count < 1) return list;
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
            // 判断自增和主键
            if (names != null && names.Length == 1)
            {
                FieldItem field = Meta.Table.FindByName(names[0]);
                if (field != null && (field.IsIdentity || field.PrimaryKey))
                {
                    // 唯一键为自增且参数小于等于0时，返回空
                    if (Helper.IsNullKey(values[0])) return null;
                }
            }

            return FindAll(MakeCondition(names, values, "And"), null, null, 0, 0);
        }

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAll(String name, Object value) { return FindAll(new String[] { name }, new Object[] { value }); }

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAll(String name, Object value, Int32 startRowIndex, Int32 maximumRows) { return FindAllByName(name, value, null, startRowIndex, maximumRows); }

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
                if (Helper.IsNullKey(value)) return new EntityList<TEntity>();

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
        public static EntityList<TEntity> FindAll(String sql) { return LoadData(Meta.Query(sql)); }
        #endregion

        #region 高级查询
        /// <summary>
        /// 查询满足条件的记录集，分页、排序
        /// </summary>
        /// <param name="key">关键字</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体集</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public static EntityList<TEntity> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows) { return FindAll(SearchWhereByKeys(key, null), orderClause, null, startRowIndex, maximumRows); }

        /// <summary>
        /// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        /// </summary>
        /// <param name="key">关键字</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>记录数</returns>
        public static Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows) { return FindCount(SearchWhereByKeys(key, null), null, null, 0, 0); }

        /// <summary>
        /// 构建关键字查询条件
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="keys"></param>
        public static void SearchWhereByKeys(StringBuilder sb, String keys) { SearchWhereByKeys(sb, keys, null); }

        /// <summary>
        /// 构建关键字查询条件
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="keys"></param>
        /// <param name="func"></param>
        public static void SearchWhereByKeys(StringBuilder sb, String keys, Func<String, String> func)
        {
            if (String.IsNullOrEmpty(keys)) return;

            String str = SearchWhereByKeys(keys, func);
            if (String.IsNullOrEmpty(str)) return;

            if (sb.Length > 0) sb.Append(" And ");
            if (str.Contains("Or") || str.ToLower().Contains("or"))
                sb.AppendFormat("({0})", str);
            else
                sb.Append(str);
        }

        /// <summary>
        /// 构建关键字查询条件
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static String SearchWhereByKeys(String keys, Func<String, String> func)
        {
            if (String.IsNullOrEmpty(keys)) return null;

            if (func == null) func = SearchWhereByKey;

            String[] ks = keys.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ks.Length; i++)
            {
                if (sb.Length > 0) sb.Append(" And ");

                String str = func(ks[i]);
                if (String.IsNullOrEmpty(str)) continue;

                //sb.AppendFormat("({0})", str);
                if (str.Contains("Or") || str.ToLower().Contains("or"))
                    sb.AppendFormat("({0})", str);
                else
                    sb.Append(str);
            }

            return sb.Length <= 0 ? null : sb.ToString();
        }

        /// <summary>
        /// 构建关键字查询条件
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String SearchWhereByKey(String key)
        {
            StringBuilder sb = new StringBuilder();
            foreach (FieldItem item in Meta.Fields)
            {
                if (item.Type != typeof(String)) continue;

                if (sb.Length > 0) sb.Append(" Or ");
                sb.AppendFormat("{0} like '%{1}%'", Meta.FormatName(item.Name), key);
            }

            return sb.Length <= 0 ? null : sb.ToString();
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
        public static TEntity FindWithCache(String name, Object value) { return Meta.Cache.Entities.Find(name, value); }

        /// <summary>
        /// 查找所有缓存
        /// </summary>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllWithCache() { return Meta.Cache.Entities; }

        /// <summary>
        /// 根据属性以及对应的值，在缓存中获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>实体数组</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllWithCache(String name, Object value) { return Meta.Cache.Entities.FindAll(name, value); }
        #endregion

        #region 取总记录数
        /// <summary>
        /// 返回总记录数
        /// </summary>
        /// <returns></returns>
        public static Int32 FindCount() { return FindCount(null, null, null, 0, 0); }

        /// <summary>
        /// 返回总记录数
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        public static Int32 FindCount(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
        {
            SelectBuilder sb = new SelectBuilder();
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
            // 判断自增和主键
            if (names != null && names.Length == 1)
            {
                FieldItem field = Meta.Table.FindByName(names[0]);
                if (field != null && (field.IsIdentity || field.PrimaryKey))
                {
                    // 唯一键为自增且参数小于等于0时，返回空
                    if (Helper.IsNullKey(values[0])) return 0;
                }
            }

            return FindCount(MakeCondition(names, values, "And"), null, null, 0, 0);
        }

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>总行数</returns>
        public static Int32 FindCount(String name, Object value) { return FindCount(name, value, 0, 0); }

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        public static Int32 FindCount(String name, Object value, Int32 startRowIndex, Int32 maximumRows) { return FindCountByName(name, value, null, startRowIndex, maximumRows); }

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
                return FindCount(new String[] { name }, new Object[] { value });
        }
        #endregion

        #region 静态操作
        /// <summary>
        /// 把一个实体对象持久化到数据库
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数</returns>
        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public static Int32 Insert(TEntity obj) { return obj.Insert(); }

        /// <summary>
        /// 把一个实体对象持久化到数据库
        /// </summary>
        /// <param name="names">更新属性列表</param>
        /// <param name="values">更新值列表</param>
        /// <returns>返回受影响的行数</returns>
        public static Int32 Insert(String[] names, Object[] values) { return persistence.Insert(Meta.ThisType, names, values); }

        /// <summary>
        /// 把一个实体对象更新到数据库
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数</returns>
        [DataObjectMethod(DataObjectMethodType.Update, true)]
        public static Int32 Update(TEntity obj) { return obj.Update(); }

        /// <summary>
        /// 更新一批实体数据
        /// </summary>
        /// <param name="setClause">要更新的项和数据</param>
        /// <param name="whereClause">指定要更新的实体</param>
        /// <returns></returns>
        public static Int32 Update(String setClause, String whereClause) { return persistence.Update(Meta.ThisType, setClause, whereClause); }

        /// <summary>
        /// 更新一批实体数据
        /// </summary>
        /// <param name="setNames">更新属性列表</param>
        /// <param name="setValues">更新值列表</param>
        /// <param name="whereNames">条件属性列表</param>
        /// <param name="whereValues">条件值列表</param>
        /// <returns>返回受影响的行数</returns>
        public static Int32 Update(String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues) { return persistence.Update(Meta.ThisType, setNames, setValues, whereNames, whereValues); }

        /// <summary>
        /// 从数据库中删除指定实体对象。
        /// 实体类应该实现该方法的另一个副本，以唯一键或主键作为参数
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数，可用于判断被删除了多少行，从而知道操作是否成功</returns>
        [DataObjectMethod(DataObjectMethodType.Delete, true)]
        public static Int32 Delete(TEntity obj) { return obj.Delete(); }

        /// <summary>
        /// 从数据库中删除指定条件的实体对象。
        /// </summary>
        /// <param name="whereClause">限制条件</param>
        /// <returns></returns>
        public static Int32 Delete(String whereClause) { return persistence.Delete(Meta.ThisType, whereClause); }

        /// <summary>
        /// 从数据库中删除指定属性列表和值列表所限定的实体对象。
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns></returns>
        public static Int32 Delete(String[] names, Object[] values) { return persistence.Delete(Meta.ThisType, names, values); }

        /// <summary>
        /// 把一个实体对象更新到数据库
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数</returns>
        public static Int32 Save(TEntity obj) { return obj.Save(); }
        #endregion

        #region 构造SQL语句
        /// <summary>
        /// 把SQL模版格式化为SQL语句
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <param name="methodType"></param>
        /// <returns>SQL字符串</returns>
        [Obsolete("该成员在后续版本中讲不再被支持！请使用XCodeService.Resolve<IEntityPersistence>().GetSql()！")]
        public static String SQL(Entity<TEntity> obj, DataObjectMethodType methodType) { return persistence.GetSql(obj, methodType); }

        /// <summary>
        /// 根据属性列表和值列表，构造查询条件。
        /// 例如构造多主键限制查询条件。
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <param name="action">联合方式</param>
        /// <returns>条件子串</returns>
        public static String MakeCondition(String[] names, Object[] values, String action)
        {
            //if (names == null || names.Length <= 0) throw new ArgumentNullException("names", "属性列表和值列表不能为空");
            //if (values == null || values.Length <= 0) throw new ArgumentNullException("values", "属性列表和值列表不能为空");
            if (names == null || names.Length <= 0) return null;
            if (values == null || values.Length <= 0) return null;
            if (names.Length != values.Length) throw new ArgumentException("属性列表必须和值列表一一对应");

            StringBuilder sb = new StringBuilder();
            for (Int32 i = 0; i < names.Length; i++)
            {
                FieldItem fi = Meta.Table.FindByName(names[i]);
                if (fi == null) throw new ArgumentException("类[" + Meta.ThisType.FullName + "]中不存在[" + names[i] + "]属性");

                // 同时构造SQL语句。names是属性列表，必须转换成对应的字段列表
                if (i > 0) sb.AppendFormat(" {0} ", action.Trim());
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
            {
                if (action.Contains("%"))
                    return Meta.FormatName(field.ColumnName) + " Like " + Meta.FormatValue(field, String.Format(action, value));
                else
                    return Meta.FormatName(field.ColumnName) + String.Format(action, Meta.FormatValue(field, value));
            }
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
        [Obsolete("该成员在后续版本中讲不再被支持！请使用XCodeService.Resolve<IEntityPersistence>().GetPrimaryCondition()！")]
        protected static String DefaultCondition(Entity<TEntity> obj) { return persistence.GetPrimaryCondition(obj); }

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
            if (fi != null)
            {
                builder.Key = fi.Name;
                //if (fi.IsIdentity && Helper.IsIntType(fi.Type))
                //{
                //    // 默认获取数据时，还是需要指定安装自增字段降序，符合使用习惯
                //    // 有GroupBy也不能加排序
                //    if (String.IsNullOrEmpty(builder.OrderBy) && String.IsNullOrEmpty(builder.GroupBy))
                //    {
                //        builder.IsDesc = true;
                //    }
                //}

                // 默认获取数据时，还是需要指定安装自增字段降序，符合使用习惯
                // 有GroupBy也不能加排序
                if (String.IsNullOrEmpty(builder.OrderBy) && String.IsNullOrEmpty(builder.GroupBy))
                {
                    // 数字降序，其它升序
                    builder.IsDesc = Helper.IsIntType(fi.Type);

                    builder.OrderBy = builder.KeyOrder;
                }
            }
            else
            {
                // 如果找不到唯一键，并且排序又为空，则采用全部字段一起，确保能够分页
                if (String.IsNullOrEmpty(builder.OrderBy)) builder.Keys = Meta.FieldNames.ToArray();
            }
            return Meta.PageSplit(builder, startRowIndex, maximumRows);
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
                if (property != null && property.GetMethod != null) return property.GetValue(this);

                Object obj = null;
                if (Extends.TryGetValue(name, out obj)) return obj;

                //throw new ArgumentException("类[" + this.GetType().FullName + "]中不存在[" + name + "]属性");

                return null;
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
                if (property != null && property.SetMethod != null)
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

                //throw new ArgumentException("类[" + this.GetType().FullName + "]中不存在[" + name + "]属性");
            }
        }
        #endregion

        #region 导入导出XML
        /// <summary>
        /// 建立Xml序列化器
        /// </summary>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中讲不再被支持！")]
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
        [Obsolete("该成员在后续版本中讲不再被支持！")]
        public static TEntity FromXml(String xml)
        {
            if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();

            StopExtend = true;
            try
            {
                //IEntityOperate factory = EntityFactory.CreateOperate(typeof(TEntity));
                XmlSerializer serial = ((TEntity)Meta.Factory.Default).CreateXmlSerializer();
                using (StringReader reader = new StringReader(xml))
                {
                    return serial.Deserialize(reader) as TEntity;
                }
            }
            finally { StopExtend = false; }
        }
        #endregion

        #region 导入导出Json
        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中讲不再被支持！")]
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
        public override Object Clone() { return CloneEntity(); }

        /// <summary>
        /// 克隆实体。创建当前对象的克隆对象，仅拷贝基本字段
        /// </summary>
        /// <returns></returns>
        public virtual TEntity CloneEntity()
        {
            TEntity obj = CreateInstance();
            foreach (FieldItem fi in Meta.Fields)
            {
                obj[fi.Name] = this[fi.Name];
            }
            if (Extends != null && Extends.Count > 0)
            {
                foreach (String item in Extends.Keys)
                {
                    obj.Extends[item] = Extends[item];
                }
            }
            return obj;
        }
        #endregion

        #region 其它
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            // 优先采用业务主键，也就是唯一索引
            IDataTable table = Meta.Table.DataTable;
            if (table.Indexes != null && table.Indexes.Count > 0)
            {
                IDataIndex di = null;
                foreach (IDataIndex item in table.Indexes)
                {
                    if (!item.Unique) continue;
                    if (item.Columns == null || item.Columns.Length < 1) continue;

                    IDataColumn[] columns = table.GetColumns(item.Columns);
                    if (columns == null || columns.Length < 1) continue;

                    di = item;

                    // 如果只有一个主键，并且是自增，再往下找别的。如果后面实在找不到，至少还有现在这个。
                    if (!(columns.Length == 1 && columns[0].Identity)) break;
                }

                if (di != null)
                {
                    IDataColumn[] columns = table.GetColumns(di.Columns);

                    // [v1,v2,...vn]
                    StringBuilder sb = new StringBuilder();
                    foreach (IDataColumn dc in columns)
                    {
                        if (sb.Length > 0) sb.Append(",");
                        if (Meta.FieldNames.Contains(dc.Alias)) sb.Append(this[dc.Alias]);
                    }
                    if (columns.Length > 1)
                        return String.Format("[{0}]", sb.ToString());
                    else
                        return sb.ToString();
                }
            }

            if (Meta.FieldNames.Contains("Name"))
                return this["Name"] == null ? null : this["Name"].ToString();
            else if (Meta.FieldNames.Contains("ID"))
                return this["ID"] == null ? null : this["ID"].ToString();
            else
                return "实体" + Meta.ThisType.Name;
        }
        #endregion

        #region 脏数据
        /// <summary>设置所有数据的脏属性</summary>
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

        /// <summary>
        /// 如果字段带有默认值，则需要设置脏数据，因为显然用户想设置该字段，而不是采用数据库的默认值
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        protected override bool OnPropertyChanging(string fieldName, object newValue)
        {
            // 如果返回true，表示不相同，基类已经设置了脏数据
            if (base.OnPropertyChanging(fieldName, newValue)) return true;

            // 如果该字段存在，且带有默认值，则需要设置脏数据，因为显然用户想设置该字段，而不是采用数据库的默认值
            FieldItem fi = Meta.Table.FindByName(fieldName);
            if (fi != null && !String.IsNullOrEmpty(fi.DefaultValue))
            {
                Dirtys[fieldName] = true;
                return true;
            }

            return false;
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
        protected TResult GetExtend<TResult>(String key, Func<String, Object> func) { return GetExtend<TEntity, TResult>(key, func); }

        /// <summary>
        /// 获取依赖于当前实体类的扩展属性
        /// </summary>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="func">回调</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        protected TResult GetExtend<TResult>(String key, Func<String, Object> func, Boolean cacheDefault) { return GetExtend<TEntity, TResult>(key, func, cacheDefault); }

        /// <summary>
        /// 设置依赖于当前实体类的扩展属性
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        protected void SetExtend(String key, Object value) { SetExtend<TEntity>(key, value); }
        #endregion
    }
}