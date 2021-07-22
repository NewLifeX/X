using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Common;
using NewLife.Data;
using NewLife.Log;

namespace XCode.Membership
{
    /// <summary>部门。组织机构，多级树状结构</summary>
    public partial class Department : Entity<Department>
    {
        #region 对象操作
        static Department()
        {
            //// 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个
            //var entity = new Department();

            // 累加字段
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(__.ParentID);

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

            if (Code.IsNullOrEmpty()) Code = PinYin.GetFirst(Name);
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected override void InitData()
        {
            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}数据……", typeof(Department).Name);

            var root = Add("总公司", "001", 0);
            Add("行政部", "011", root.ID);
            Add("技术部", "012", root.ID);
            Add("生产部", "013", root.ID);

            root = Add("上海分公司", "101", 0);
            Add("行政部", "111", root.ID);
            Add("市场部", "112", root.ID);

            if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}数据！", typeof(Department).Name);
        }

        /// <summary>添加用户，如果存在则直接返回</summary>
        /// <param name="name"></param>
        /// <param name="code"></param>
        /// <param name="parentid"></param>
        /// <returns></returns>
        public static Department Add(String name, String code, Int32 parentid)
        {
            var entity = new Department
            {
                Name = name,
                Code = code,
                ParentID = parentid,
                Enable = true,
                Visible = true,
            };

            entity.Save();

            return entity;
        }
        #endregion

        #region 扩展属性
        /// <summary>管理者</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public User Manager => Extends.Get(nameof(Manager), k => User.FindByID(ManagerID));

        /// <summary>管理者</summary>
        [Map(__.ManagerID, typeof(User), __.ID)]
        public String ManagerName => Manager?.ToString();

        /// <summary>父级</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public Department Parent => Extends.Get(nameof(Department), k => FindByID(ParentID));

        /// <summary>父级</summary>
        [Map(__.ParentID, typeof(Department), __.ID)]
        public String ParentName => Parent?.ToString();
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static Department FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns>实体列表</returns>
        public static IList<Department> FindAllByName(String name)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.Name == name);

            return FindAll(_.Name == name);
        }

        /// <summary>根据名称、父级查找</summary>
        /// <param name="name">名称</param>
        /// <param name="parentid">父级</param>
        /// <returns>实体对象</returns>
        public static Department FindByNameAndParentID(String name, Int32 parentid)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Name == name && e.ParentID == parentid);

            return Find(_.Name == name & _.ParentID == parentid);
        }

        /// <summary>根据代码查找</summary>
        /// <param name="code">代码</param>
        /// <returns>实体对象</returns>
        public static Department FindByCode(String code)
        {
            if (code.IsNullOrEmpty()) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Code == code);

            return Find(_.Code == code);
        }
        #endregion

        #region 高级查询
        /// <summary>高级搜索</summary>
        /// <param name="parentId"></param>
        /// <param name="enable"></param>
        /// <param name="visible"></param>
        /// <param name="key"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static IList<Department> Search(Int32 parentId, Boolean? enable, Boolean? visible, String key, PageParameter page)
        {
            var exp = new WhereExpression();
            if (parentId >= 0) exp &= _.ParentID == parentId;
            if (enable != null) exp &= _.Enable == enable.Value;
            if (visible != null) exp &= _.Visible == visible.Value;
            if (!key.IsNullOrEmpty()) exp &= _.Code.StartsWith(key) | _.Name.StartsWith(key) | _.FullName.StartsWith(key);

            return FindAll(exp, page);
        }
        #endregion

        #region 业务操作
        #endregion
    }
}