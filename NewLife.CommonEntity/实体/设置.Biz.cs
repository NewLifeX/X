/*
 * XCoder v3.4.2011.0329
 * 作者：nnhy/X
 * 时间：2011-06-21 21:07:14
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.ComponentModel;
using XCode;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace NewLife.CommonEntity
{
    /// <summary>设置</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Setting : Setting<Setting> { }

    /// <summary>设置</summary>
    public partial class Setting<TEntity> : EntityTree<TEntity> where TEntity : Setting<TEntity>, new()
    {
        #region 对象操作
        static Setting()
        {
            // 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个
            TEntity entity = new TEntity();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew"></param>
        public override void Valid(bool isNew)
        {
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(_.Name, _.Name.DisplayName + "不能为空！");

            base.Valid(isNew);
        }
        #endregion

        #region 扩展属性
        /// <summary>类型编码</summary>
        public TypeCode KindCode { get { return (TypeCode)Kind; } set { Kind = (Int32)value; } }

        /// <summary>父级</summary>
        public String ParentName { get { return Parent != null ? Parent.Name : null; } }
        #endregion

        #region 扩展查询
        /// <summary>根据父编号、名称查找</summary>
        /// <param name="parentid">父编号</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByParentIDAndName(Int32 parentid, String name)
        {
            if (Meta.Count >= 1000)
                return Find(new String[] { _.ParentID, _.Name }, new Object[] { parentid, name });
            else // 实体缓存
                return Meta.Cache.Entities.Find(e => e.ParentID == parentid && e.Name == name);
        }

        ///// <summary>根据编号查找</summary>
        ///// <param name="id">编号</param>
        ///// <returns></returns>
        //[DataObjectMethod(DataObjectMethodType.Select, false)]
        //public static TEntity FindByID(Int32 id)
        //{
        //    if (Meta.Count >= 1000)
        //        return Find(_.ID, id);
        //    else // 实体缓存
        //        return Meta.Cache.Entities.Find(_.ID, id);
        //    // 单对象缓存
        //    //return Meta.SingleCache[id];
        //}
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
        //    if (String.IsNullOrEmpty(_.Name)) throw new ArgumentNullException(_.Name, _.Name.Description + "无效！");
        //    if (!isNew && ID < 1) throw new ArgumentOutOfRangeException(_.ID, _.ID.Description + "必须大于0！");

        //    // 在新插入数据或者修改了指定字段时进行唯一性验证，CheckExist内部抛出参数异常
        //    if (isNew || Dirtys[_.Name]) CheckExist(_.Name);
        //    if (isNew || Dirtys[_.Name] || Dirtys[_.DbType]) CheckExist(_.Name, _.DbType);
        //    if ((isNew || Dirtys[_.Name]) && Exist(_.Name)) throw new ArgumentException(_.Name, "值为" + Name + "的" + _.Name.Description + "已存在！");
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
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual ISetting Create(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            var entity = FindByParentIDAndName(ID, name);
            if (entity == null)
            {
                entity = new TEntity();
                entity.ParentID = ID;
                entity.Name = name;
                entity.Save();
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
            Value = "" + val;
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
        /// <param name="name"></param>
        /// <param name="defval"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public virtual ISetting Ensure<T>(String name, T defval, String displayName)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            var entity = FindByParentIDAndName(ID, name);
            if (entity == null)
            {
                entity = new TEntity();
                entity.ParentID = ID;
                entity.Name = name;
            }
            if (String.IsNullOrEmpty(entity.Value)) entity.Set<T>(defval);
            if (String.IsNullOrEmpty(entity.DisplayName)) entity.DisplayName = displayName;
            entity.Save();

            return this;
        }
        #endregion
    }

    partial interface ISetting
    {
        /// <summary>若不存在则创建指定名称的子级</summary>
        /// <param name="name"></param>
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
        /// <param name="name"></param>
        /// <param name="defval"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        ISetting Ensure<T>(String name, T defval, String displayName);

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }
}