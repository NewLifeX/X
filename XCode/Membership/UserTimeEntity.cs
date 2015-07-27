using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace XCode.Membership
{
    /// <summary>用户模型</summary>
    public class UserModule : EntityModule
    {
        #region 静态引用
        /// <summary>字段名</summary>
        public class __
        {
            /// <summary>创建人</summary>
            public static String CreateUserID = "CreateUserID";

            /// <summary>更新人</summary>
            public static String UpdateUserID = "UpdateUserID";
        }
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public override Boolean Init(Type entityType)
        {
            var fact = EntityFactory.CreateOperate(entityType);
            if (fact == null) return false;

            var fs = fact.FieldNames;
            if (fs.Contains(__.CreateUserID)) return true;
            if (fs.Contains(__.UpdateUserID)) return true;

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="isNew"></param>
        public override Boolean Valid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var fact = EntityFactory.CreateOperate(entity.GetType());
            var fs = fact.FieldNames;

            // 当前登录用户
            var user = ManageProvider.Provider.Current;
            if (user != null)
            {
                var name = __.CreateUserID;
                if (isNew)
                {
                    if (fs.Contains(name) && !entity.Dirtys[name]) entity.SetItem(name, user.ID);
                }
                name = __.UpdateUserID;
                if (fs.Contains(name) && !entity.Dirtys[name]) entity.SetItem(name, user.ID);
            }

            return true;
        }
    }

    /// <summary>时间模型</summary>
    public class TimeModule : EntityModule
    {
        #region 静态引用
        /// <summary>字段名</summary>
        public class __
        {
            /// <summary>创建时间</summary>
            public static String CreateTime = "CreateTime";

            /// <summary>更新时间</summary>
            public static String UpdateTime = "UpdateTime";
        }
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public override bool Init(Type entityType)
        {
            var fact = EntityFactory.CreateOperate(entityType);
            if (fact == null) return false;

            var fs = fact.FieldNames;
            if (fs.Contains(__.CreateTime)) return true;
            if (fs.Contains(__.UpdateTime)) return true;

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="isNew"></param>
        public override Boolean Valid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var fact = EntityFactory.CreateOperate(entity.GetType());
            var fs = fact.FieldNames;

            var name = __.CreateTime;
            if (isNew)
            {
                if (fs.Contains(name) && !entity.Dirtys[name]) entity.SetItem(name, DateTime.Now);
            }

            // 不管新建还是更新，都改变更新时间
            name = __.UpdateTime;
            if (fs.Contains(name) && !entity.Dirtys[name]) entity.SetItem(name, DateTime.Now);

            return true;
        }
    }

    /// <summary>IP地址模型</summary>
    public class IPModule : EntityModule
    {
        #region 静态引用
        /// <summary>字段名</summary>
        public class __
        {
            /// <summary>创建人</summary>
            public static String CreateIP = "CreateIP";

            /// <summary>更新人</summary>
            public static String UpdateIP = "UpdateIP";
        }
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public override bool Init(Type entityType)
        {
            var fact = EntityFactory.CreateOperate(entityType);
            if (fact == null) return false;

            var fs = fact.FieldNames;
            if (fs.Contains(__.CreateIP)) return true;
            if (fs.Contains(__.UpdateIP)) return true;

            // 任意以IP结尾的字段都要，仅在创建时生效
            foreach (var item in fs)
            {
                if (item.EndsWith("IP")) return true;
            }

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="isNew"></param>
        public override Boolean Valid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var fact = EntityFactory.CreateOperate(entity.GetType());
            var fs = fact.FieldNames;

            var ip = WebHelper.UserHost;
            if (!ip.IsNullOrEmpty())
            {
                var name = __.CreateIP;
                if (isNew)
                {
                    if (fs.Contains(name) && !entity.Dirtys[name]) entity.SetItem(name, ip);

                    // 任意以IP结尾的字段都要，仅在创建时生效
                    foreach (var item in fs)
                    {
                        if (item.EndsWith("IP"))
                        {
                            name = item;
                            if (fs.Contains(name) && !entity.Dirtys[name]) entity.SetItem(name, ip);
                        }
                    }
                }

                // 不管新建还是更新，都改变更新时间
                name = __.UpdateIP;
                if (fs.Contains(name) && !entity.Dirtys[name]) entity.SetItem(name, ip);
            }

            return true;
        }
    }

    /// <summary>用户时间实体基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class UserTimeEntity<TEntity> : Entity<TEntity>, IUserInfo2, ITimeInfo where TEntity : UserTimeEntity<TEntity>, new()
    {
        #region 静态引用
        /// <summary>字段名</summary>
        public class __Name
        {
            /// <summary>创建人</summary>
            public static String CreateUserID = "CreateUserID";
            /// <summary>更新人</summary>
            public static String UpdateUserID = "UpdateUserID";
            /// <summary>创建人</summary>
            public static String CreateUserName = "CreateUserName";
            /// <summary>更新人</summary>
            public static String UpdateUserName = "UpdateUserName";
            /// <summary>创建时间</summary>
            public static String CreateTime = "CreateTime";
            /// <summary>更新时间</summary>
            public static String UpdateTime = "UpdateTime";
        }
        #endregion

        #region 验证数据
        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="isNew"></param>
        public override void Valid(bool isNew)
        {
            if (!isNew && !HasDirty) return;

            base.Valid(isNew);

            var fs = Meta.FieldNames;

            // 当前登录用户
            var user = ManageProvider.Provider.Current;
            if (user != null)
            {
                if (isNew)
                {
                    SetDirtyItem(__Name.CreateUserID, user.ID);
                    SetDirtyItem(__Name.CreateUserName, user + "");
                }
                SetDirtyItem(__Name.UpdateUserID, user.ID);
                SetDirtyItem(__Name.UpdateUserName, user + "");
            }
            if (isNew)
                SetDirtyItem(__Name.CreateTime, DateTime.Now);

            // 不管新建还是更新，都改变更新时间
            SetDirtyItem(__Name.UpdateTime, DateTime.Now);
        }

        /// <summary>设置脏数据项。如果某个键存在并且数据没有脏，则设置</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void SetDirtyItem(String name, Object value)
        {
            if (Meta.FieldNames.Contains(name) && !Dirtys[name]) SetItem(name, value);
        }
        #endregion

        #region 扩展属性
        private IManageUser _CreateUser;
        /// <summary>创建人</summary>
        [XmlIgnore]
        [DisplayName("创建人")]
        //[BindRelation("CreateUserID", false, "User", "ID")]
        public IManageUser CreateUser
        {
            get
            {
                var CreateUserID = this[__Name.CreateUserID].ToInt();
                if (_CreateUser == null && CreateUserID > 0 && !Dirtys.ContainsKey("CreateUser"))
                {
                    _CreateUser = ManageProvider.Provider.FindByID(CreateUserID);
                    Dirtys["CreateUser"] = true;
                }
                return _CreateUser;
            }
            set { _CreateUser = value; }
        }

        /// <summary>创建人名称</summary>
        [XmlIgnore]
        [DisplayName("创建人")]
        [BindRelation("CreateUserID")]
        public String CreateUserName { get { return CreateUser + ""; } }

        private IManageUser _UpdateUser;
        /// <summary>更新人</summary>
        [XmlIgnore]
        [DisplayName("更新人")]
        //[BindRelation("UpdateUserID", false, "User", "ID")]
        public IManageUser UpdateUser
        {
            get
            {
                var UpdateUserID = this[__Name.UpdateUserID].ToInt();
                if (_UpdateUser == null && UpdateUserID > 0 && !Dirtys.ContainsKey("UpdateUser"))
                {
                    _UpdateUser = ManageProvider.Provider.FindByID(UpdateUserID);
                    Dirtys["UpdateUser"] = true;
                }
                return _UpdateUser;
            }
            set { _UpdateUser = value; }
        }

        /// <summary>更新人名称</summary>
        [XmlIgnore]
        [DisplayName("更新人")]
        [BindRelation("UpdateUserID")]
        public String UpdateUserName { get { return UpdateUser + ""; } }

        [XmlIgnore]
        Int32 IUserInfo2.CreateUserID { get { return (Int32)this[__Name.CreateUserID]; } set { SetItem(__Name.CreateUserID, value); } }
        [XmlIgnore]
        Int32 IUserInfo2.UpdateUserID { get { return (Int32)this[__Name.UpdateUserID]; } set { SetItem(__Name.UpdateUserID, value); } }

        [XmlIgnore]
        DateTime ITimeInfo.CreateTime { get { return (DateTime)this[__Name.CreateTime]; } set { SetItem(__Name.CreateTime, value); } }
        [XmlIgnore]
        DateTime ITimeInfo.UpdateTime { get { return (DateTime)this[__Name.UpdateTime]; } set { SetItem(__Name.UpdateTime, value); } }
        #endregion
    }

    /// <summary>用户时间实体基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class UserTimeEntityTree<TEntity> : EntityTree<TEntity>, IUserInfo2, ITimeInfo where TEntity : UserTimeEntityTree<TEntity>, new()
    {
        #region 静态引用
        /// <summary>字段名</summary>
        public class __Name
        {
            /// <summary>创建人</summary>
            public static String CreateUserID = "CreateUserID";
            /// <summary>更新人</summary>
            public static String UpdateUserID = "UpdateUserID";
            /// <summary>创建人</summary>
            public static String CreateUserName = "CreateUserName";
            /// <summary>更新人</summary>
            public static String UpdateUserName = "UpdateUserName";
            /// <summary>创建时间</summary>
            public static String CreateTime = "CreateTime";
            /// <summary>更新时间</summary>
            public static String UpdateTime = "UpdateTime";
        }
        #endregion

        #region 验证数据
        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="isNew"></param>
        public override void Valid(bool isNew)
        {
            if (!isNew && !HasDirty) return;

            base.Valid(isNew);

            var fs = Meta.FieldNames;

            // 当前登录用户
            var user = ManageProvider.Provider.Current;
            if (user != null)
            {
                if (isNew)
                {
                    SetDirtyItem(__Name.CreateUserID, user.ID);
                    SetDirtyItem(__Name.CreateUserName, user + "");
                }
                SetDirtyItem(__Name.UpdateUserID, user.ID);
                SetDirtyItem(__Name.UpdateUserName, user + "");
            }
            if (isNew)
                SetDirtyItem(__Name.CreateTime, DateTime.Now);

            // 不管新建还是更新，都改变更新时间
            SetDirtyItem(__Name.UpdateTime, DateTime.Now);
        }

        /// <summary>设置脏数据项。如果某个键存在并且数据没有脏，则设置</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void SetDirtyItem(String name, Object value)
        {
            if (Meta.FieldNames.Contains(name) && !Dirtys[name]) SetItem(name, value);
        }
        #endregion

        #region 扩展属性
        private IManageUser _CreateUser;
        /// <summary>创建人</summary>
        [XmlIgnore]
        [DisplayName("创建人")]
        [BindRelation("CreateUserID", false, "User", "ID")]
        public IManageUser CreateUser
        {
            get
            {
                var CreateUserID = this[__Name.CreateUserID].ToInt();
                if (_CreateUser == null && CreateUserID > 0 && !Dirtys.ContainsKey("CreateUser"))
                {
                    _CreateUser = ManageProvider.Provider.FindByID(CreateUserID);
                    Dirtys["CreateUser"] = true;
                }
                return _CreateUser;
            }
            set { _CreateUser = value; }
        }

        /// <summary>创建人名称</summary>
        [XmlIgnore]
        [DisplayName("创建人")]
        [BindRelation("CreateUserID")]
        public String CreateUserName { get { return CreateUser + ""; } }

        private IManageUser _UpdateUser;
        /// <summary>更新人</summary>
        [XmlIgnore]
        [DisplayName("更新人")]
        [BindRelation("UpdateUserID", false, "User", "ID")]
        public IManageUser UpdateUser
        {
            get
            {
                var UpdateUserID = this[__Name.UpdateUserID].ToInt();
                if (_UpdateUser == null && UpdateUserID > 0 && !Dirtys.ContainsKey("UpdateUser"))
                {
                    _UpdateUser = ManageProvider.Provider.FindByID(UpdateUserID);
                    Dirtys["UpdateUser"] = true;
                }
                return _UpdateUser;
            }
            set { _UpdateUser = value; }
        }

        /// <summary>更新人名称</summary>
        [XmlIgnore]
        [DisplayName("更新人")]
        [BindRelation("UpdateUserID")]
        public String UpdateUserName { get { return UpdateUser + ""; } }

        [XmlIgnore]
        Int32 IUserInfo2.CreateUserID { get { return (Int32)this[__Name.CreateUserID]; } set { SetItem(__Name.CreateUserID, value); } }
        [XmlIgnore]
        Int32 IUserInfo2.UpdateUserID { get { return (Int32)this[__Name.UpdateUserID]; } set { SetItem(__Name.UpdateUserID, value); } }

        [XmlIgnore]
        DateTime ITimeInfo.CreateTime { get { return (DateTime)this[__Name.CreateTime]; } set { SetItem(__Name.CreateTime, value); } }
        [XmlIgnore]
        DateTime ITimeInfo.UpdateTime { get { return (DateTime)this[__Name.UpdateTime]; } set { SetItem(__Name.UpdateTime, value); } }
        #endregion
    }

    /// <summary>用户信息接口。包含创建用户和更新用户</summary>
    public interface IUserInfo
    {
        /// <summary>创建用户</summary>
        IManageUser CreateUser { get; set; }

        /// <summary>创建用户名</summary>
        String CreateUserName { get; }

        /// <summary>更新用户</summary>
        IManageUser UpdateUser { get; set; }

        /// <summary>更新用户名</summary>
        String UpdateUserName { get; }
    }

    /// <summary>用户信息接口。包含创建用户和更新用户</summary>
    public interface IUserInfo2 : IUserInfo
    {
        /// <summary>创建用户ID</summary>
        Int32 CreateUserID { get; set; }

        /// <summary>更新用户ID</summary>
        Int32 UpdateUserID { get; set; }
    }

    /// <summary>时间信息接口。包含创建时间和更新时间</summary>
    public interface ITimeInfo
    {
        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        DateTime UpdateTime { get; set; }
    }
}