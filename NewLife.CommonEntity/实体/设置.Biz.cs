/*
 * XCoder v3.4.2011.0329
 * 作者：nnhy/X
 * 时间：2011-06-21 21:07:14
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>设置</summary>
    /// <remarks>所有设置项，都挂在单对象缓存上</remarks>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Setting : Setting<Setting> { }

    /// <summary>设置</summary>
    /// <remarks>所有设置项，都挂在单对象缓存上</remarks>
    public partial class Setting<TEntity> : Entity<TEntity> where TEntity : Setting<TEntity>, new()
    {
        #region 对象操作
        static Setting()
        {
            // 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个
            TEntity entity = new TEntity();

            //var cache = Meta.SingleCache;
            ////cache.AllowNull = false;
            ////cache.AutoSave = true;
            //cache.FindKeyMethod = k =>
            //{
            //    var key = k.ToString();
            //    var p = key.IndexOf("_");
            //    return FindByParentIDAndName(Int32.Parse(key.Substring(0, p)), key.Substring(p + 1));
            //};
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew"></param>
        public override void Valid(bool isNew)
        {
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(__.Name, _.Name.DisplayName + "不能为空！");

            base.Valid(isNew);
        }

        /// <summary>根据指定键检查数据，返回数据是否已存在</summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public override bool Exist(params string[] names)
        {
            // 采用单对象缓存判断，减少一次查询
            if (names.Length == 2 && names[0] == _.ParentID && names[1] == _.Name)
            {
                var p = FindByID(ParentID);
                return p.Childs.Exists(__.Name, Name);
            }

            return base.Exist(names);
        }

        /// <summary>添加后清理上级的子级缓存</summary>
        /// <returns></returns>
        public override int Insert()
        {
            // 清理缓存
            var ps = new TEntity[] { Parent, null };

            var rs = base.Insert();
            // 加入到单对象缓存
            Meta.SingleCache.Add(this.ID, this as TEntity);

            // 清理缓存
            ps[1] = Parent;
            foreach (var p in ps)
            {
                if (p != null)
                {
                    // 可以试试不清理，查找缓存后加入，必须查找缓存
                    var list = p._Childs;
                    if (list != null && !list.Exists(__.ID, this.ID)) list.Add(FindByID(this.ID));
                }
            }

            return rs;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        protected override int OnDelete()
        {
            var list = Childs;
            //if (list != null) list.Delete();
            if (list == null || list.Count > 0) throw new XException("设置项{0}下有{1}个子项，禁止删除！", Name, list == null ? 0 : list.Count);

            return base.OnDelete();
        }
        #endregion

        #region 扩展属性
        /// <summary>类型编码</summary>
        public TypeCode KindCode { get { return (TypeCode)Kind; } set { Kind = (Int32)value; } }

        private static Object _lock_Root = new Object();
        private static TEntity _Root;
        /// <summary>根</summary>
        public static TEntity Root
        {
            get
            {
                if (_Root != null) return _Root;
                lock (_lock_Root)
                {
                    if (_Root != null) return _Root;

                    _Root = new TEntity();
                    //Meta.OnDataChange += delegate { _Root = null; };
                }
                return _Root;
            }
            set { _Root = null; }
        }

        /// <summary>父节点</summary>
        [XmlIgnore]
        public TEntity Parent { get { return FindByID(ParentID); } }

        /// <summary>父级</summary>
        public String ParentName { get { return Parent != null ? Parent.Name : null; } }

        [NonSerialized]
        private EntityList<TEntity> _Childs;
        /// <summary>子节点</summary>
        public EntityList<TEntity> Childs
        {
            get
            {
                if (_Childs == null && !Dirtys["Childs"])
                {
                    // 先从数据库读取，然后从实体缓存读取
                    var list = FindAll(__.ParentID, ID);

                    _Childs = new EntityList<TEntity>(list.ToList().Select(e => FindByID(e.ID)));

                    Dirtys["Childs"] = true;
                }
                return _Childs;
            }
            set { _Childs = value; }
        }
        #endregion

        #region 扩展查询
        ///// <summary>根据父编号、名称查找</summary>
        ///// <param name="parentid">父编号</param>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //[DataObjectMethod(DataObjectMethodType.Select, false)]
        //static TEntity FindByParentIDAndName(Int32 parentid, String name)
        //{
        //    //if (Meta.Count >= 1000)
        //    return Find(new String[] { _.ParentID, _.Name }, new Object[] { parentid, name });
        //    //else // 实体缓存
        //    //    return Meta.Cache.Entities.Find(e => e.ParentID == parentid && e.Name == name);
        //}

        ///// <summary>根据父编号、名称查找，单对象缓存</summary>
        ///// <param name="parentid">父编号</param>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //public static TEntity FindWithCache(Int32 parentid, String name)
        //{
        //    var key = String.Format("{0}_{1}", parentid, name);
        //    return Meta.SingleCache[key];
        //}

        /// <summary>根据名称查找子节点</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public TEntity FindByName(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            var list = Childs;
            //XTrace.WriteLine("FindByName:{0}.Childs[{1}]", Name, _Childs.Count);
            if (list == null || list.Count < 1) return null;

            return list.Find(__.Name, name);
        }

        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return Root;

            //if (Meta.Count >= 1000)
            //    return Find(__.ID, id);
            //else // 实体缓存
            //    return Meta.Cache.Entities.Find(__.ID, id);
            // 单对象缓存
            return Meta.SingleCache[id];
        }
        #endregion

        #region 对象操作
        ///// <summary>
        ///// 已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert
        ///// </summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        ///// <summary>
        ///// 已重载。在事务保护范围内处理业务，位于Valid之后
        ///// </summary>
        ///// <returns></returns>
        //protected override Int32 OnInsert()
        //{
        //    return base.OnInsert();
        //}

        ///// <summary>
        ///// 验证数据，通过抛出异常的方式提示验证失败。
        ///// </summary>
        ///// <param name="isNew"></param>
        //public override void Valid(Boolean isNew)
        //{
        //    // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
        //    base.Valid(isNew);

        //    // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
        //    if (String.IsNullOrEmpty(__.Name)) throw new ArgumentNullException(__.Name, _.Name.Description + "无效！");
        //    if (!isNew && ID < 1) throw new ArgumentOutOfRangeException(__.ID, _.ID.Description + "必须大于0！");

        //    // 在新插入数据或者修改了指定字段时进行唯一性验证，CheckExist内部抛出参数异常
        //    if (isNew || Dirtys[_.Name]) CheckExist(__.Name);
        //    if (isNew || Dirtys[_.Name] || Dirtys[_.DbType]) CheckExist(__.Name, _.DbType);
        //    if ((isNew || Dirtys[_.Name]) && Exist(__.Name)) throw new ArgumentException(__.Name, "值为" + Name + "的" + _.Name.Description + "已存在！");
        //}


        ///// <summary>
        ///// 首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法
        ///// </summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    base.InitData();

        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    // Meta.Count是快速取得表记录数
        //    if (Meta.Count > 0) return;

        //    // 需要注意的是，如果该方法调用了其它实体类的首次数据库操作，目标实体类的数据初始化将会在同一个线程完成
        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}管理员数据……", typeof(TEntity).Name);

        //    TEntity user = new TEntity();
        //    user.Name = "admin";
        //    user.Password = DataHelper.Hash("admin");
        //    user.DisplayName = "管理员";
        //    user.RoleID = 1;
        //    user.IsEnable = true;
        //    user.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}管理员数据！", typeof(TEntity).Name);
        //}
        #endregion

        #region 高级查询
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        /// <summary>若不存在则创建指定名称的子级</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual ISetting Create(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            var entity = FindByName(name);
            if (entity == null)
            {
                entity = new TEntity();
                entity.ParentID = ID;
                entity.Name = name;
                entity.Save();

                // 如果空，需要重新查找，让其进入缓存
                entity = FindByID(entity.ID);
                //entity = FindByName(name);
                if (entity == null) throw new XException("设计错误！新添加的设置项{0}马上进行单对象缓存查找居然为空！", name);
            }

            return entity;
        }

        //ISetting ISetting.Create(String name) { return Create(name); }

        /// <summary>取值</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T Get<T>() { return (T)Convert.ChangeType(Value, Type.GetTypeCode(typeof(T))); }

        /// <summary>取值</summary>
        /// <returns></returns>
        public virtual Object Get()
        {
            if (KindCode == TypeCode.Empty) return null;

            return Convert.ChangeType(Value, KindCode);
        }

        /// <summary>设置值</summary>
        /// <param name="val"></param>
        public virtual void Set<T>(T val)
        {
            Value = val != null ? "" + val : null;
            KindCode = Type.GetTypeCode(typeof(T));
            Save();
        }

        /// <summary>设置值</summary>
        /// <param name="val"></param>
        public virtual void Set(Object val)
        {
            if (val == null)
            {
                KindCode = TypeCode.Empty;
                Value = null;
            }
            else
            {
                KindCode = Type.GetTypeCode(val.GetType());
                Value = "" + val;
            }
            Save();
        }

        /// <summary>确保设置项存在</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">名称</param>
        /// <param name="defval"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public virtual ISetting Ensure<T>(String name, T defval, String displayName)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            // 是否空
            var isnull = false;

            var entity = FindByName(name);
            if (entity == null)
            {
                entity = new TEntity();
                entity.ParentID = ID;
                entity.Name = name;

                isnull = true;
            }
            // Set放在最后，因为里面有个Save，避免做了一次Insert后再做一次Update
            if (String.IsNullOrEmpty(entity.DisplayName)) entity.DisplayName = displayName;
            if (String.IsNullOrEmpty(entity.Value)) entity.Set<T>(defval);
            entity.Save();

            // 如果空，需要重新查找，让其进入缓存
            if (isnull) entity = FindByName(name);

            return this;
        }
        #endregion
    }

    partial interface ISetting
    {
        /// <summary>若不存在则创建指定名称的子级</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        ISetting Create(String name);

        /// <summary>取值</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Get<T>();

        ///// <summary>取值</summary>
        ///// <returns></returns>
        //Object Get();

        /// <summary>设置值</summary>
        /// <param name="val"></param>
        void Set<T>(T val);

        ///// <summary>设置值</summary>
        ///// <param name="val"></param>
        //void Set(Object val);

        /// <summary>确保设置项存在</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">名称</param>
        /// <param name="defval"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        ISetting Ensure<T>(String name, T defval, String displayName);

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }
}