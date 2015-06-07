using System;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Membership;

namespace XCode.Membership
{
    /// <summary>用户时间实体基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class UserTimeEntity<TEntity> : Entity<TEntity>, IUserInfo, ITimeInfo where TEntity : UserTimeEntity<TEntity>, new()
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
        public String UpdateUserName { get { return UpdateUser + ""; } }

        [XmlIgnore]
        Int32 IUserInfo.CreateUserID { get { return (Int32)this[__Name.CreateUserID]; } set { SetItem(__Name.CreateUserID, value); } }
        [XmlIgnore]
        Int32 IUserInfo.UpdateUserID { get { return (Int32)this[__Name.UpdateUserID]; } set { SetItem(__Name.UpdateUserID, value); } }

        [XmlIgnore]
        DateTime ITimeInfo.CreateTime { get { return (DateTime)this[__Name.CreateTime]; } set { SetItem(__Name.CreateTime, value); } }
        [XmlIgnore]
        DateTime ITimeInfo.UpdateTime { get { return (DateTime)this[__Name.UpdateTime]; } set { SetItem(__Name.UpdateTime, value); } }
        #endregion
    }

    /// <summary>用户时间实体基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class UserTimeEntityTree<TEntity> : EntityTree<TEntity>, IUserInfo, ITimeInfo where TEntity : UserTimeEntityTree<TEntity>, new()
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
        public String UpdateUserName { get { return UpdateUser + ""; } }

        [XmlIgnore]
        Int32 IUserInfo.CreateUserID { get { return (Int32)this[__Name.CreateUserID]; } set { SetItem(__Name.CreateUserID, value); } }
        [XmlIgnore]
        Int32 IUserInfo.UpdateUserID { get { return (Int32)this[__Name.UpdateUserID]; } set { SetItem(__Name.UpdateUserID, value); } }

        [XmlIgnore]
        DateTime ITimeInfo.CreateTime { get { return (DateTime)this[__Name.CreateTime]; } set { SetItem(__Name.CreateTime, value); } }
        [XmlIgnore]
        DateTime ITimeInfo.UpdateTime { get { return (DateTime)this[__Name.UpdateTime]; } set { SetItem(__Name.UpdateTime, value); } }
        #endregion
    }

    /// <summary>用户信息接口。包含创建用户和更新用户</summary>
    public interface IUserInfo
    {
        /// <summary>创建用户ID</summary>
        Int32 CreateUserID { get; set; }

        /// <summary>创建用户</summary>
        IManageUser CreateUser { get; set; }

        /// <summary>创建用户名</summary>
        String CreateUserName { get; }

        /// <summary>更新用户ID</summary>
        Int32 UpdateUserID { get; set; }

        /// <summary>更新用户</summary>
        IManageUser UpdateUser { get; set; }

        /// <summary>更新用户名</summary>
        String UpdateUserName { get; }
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