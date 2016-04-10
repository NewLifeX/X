using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
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
            var fs = GetFieldNames(entityType);
            if (fs.Contains(__.CreateUserID)) return true;
            if (fs.Contains(__.UpdateUserID)) return true;

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        public override Boolean Valid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var fs = GetFieldNames(entity.GetType());

            // 当前登录用户
            var user = ManageProvider.Provider.Current;
            if (user != null)
            {
                if (isNew) SetNoDirtyItem(fs, entity, __.CreateUserID, user.ID);
                SetNoDirtyItem(fs, entity, __.UpdateUserID, user.ID);
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
            var fs = GetFieldNames(entityType);
            if (fs.Contains(__.CreateTime)) return true;
            if (fs.Contains(__.UpdateTime)) return true;

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        public override Boolean Valid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var fs = GetFieldNames(entity.GetType());

            if (isNew) SetNoDirtyItem(fs, entity, __.CreateTime, DateTime.Now);

            // 不管新建还是更新，都改变更新时间
            SetNoDirtyItem(fs, entity, __.UpdateTime, DateTime.Now);

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
            var fs = GetFieldNames(entityType);
            if (fs.Contains(__.CreateIP)) return true;
            if (fs.Contains(__.UpdateIP)) return true;

            // 任意以IP结尾的字段都要，仅在创建时生效
            //foreach (var item in fs)
            //{
            //    if (item.EndsWith("IP")) return true;
            //}

            //return false;

            var fs2 = GetIPFieldNames(entityType);
            return fs2 != null && fs2.Count > 0;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        public override Boolean Valid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var ip = WebHelper.UserHost;
            if (!ip.IsNullOrEmpty())
            {
                var fs = GetFieldNames(entity.GetType());

                if (isNew)
                {
                    SetNoDirtyItem(fs, entity, __.CreateIP, ip);

                    // 任意以IP结尾的字段都要，仅在创建时生效
                    //foreach (var item in fs)
                    //{
                    //    if (item.EndsWith("IP")) SetNoDirtyItem(fs, entity, item, ip);
                    //}
                    var fs2 = GetIPFieldNames(entity.GetType());
                    if (fs2 != null)
                    {
                        foreach (var item in fs2)
                        {
                            SetNoDirtyItem(fs2, entity, item, ip);
                        }
                    }
                }

                // 不管新建还是更新，都改变更新时间
                SetNoDirtyItem(fs, entity, __.UpdateIP, ip);
            }

            return true;
        }
        
        private DictionaryCache<Type, ICollection<String>> _ipFieldNames = new DictionaryCache<Type, ICollection<String>>();
        /// <summary>获取实体类的字段名。带缓存</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected ICollection<String> GetIPFieldNames(Type entityType)
        {
            return _ipFieldNames.GetItem(entityType, t =>
            {
                var fs = GetFieldNames(t);
                if (fs == null || fs.Count == 0) return null;

                //return fs.Where(e => e.EndsWith("IP")).ToList();
                return new HashSet<String>(fs.Where(e => e.EndsWith("IP")));
            });
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
        ///// <summary>验证数据，自动加上创建和更新的信息</summary>
        ///// <param name="isNew"></param>
        //public override void Valid(bool isNew)
        //{
        //    if (!isNew && !HasDirty) return;

        //    base.Valid(isNew);

        //    var fs = Meta.FieldNames;

        //    // 当前登录用户
        //    var user = ManageProvider.Provider.Current;
        //    if (user != null)
        //    {
        //        if (isNew)
        //        {
        //            SetNoDirtyItem(__Name.CreateUserID, user.ID);
        //            SetNoDirtyItem(__Name.CreateUserName, user + "");
        //        }
        //        SetNoDirtyItem(__Name.UpdateUserID, user.ID);
        //        SetNoDirtyItem(__Name.UpdateUserName, user + "");
        //    }
        //    if (isNew)
        //        SetNoDirtyItem(__Name.CreateTime, DateTime.Now);

        //    // 不管新建还是更新，都改变更新时间
        //    SetNoDirtyItem(__Name.UpdateTime, DateTime.Now);
        //}
        #endregion

        #region 扩展属性
        private IManageUser _CreateUser;
        /// <summary>创建人</summary>
        [XmlIgnore, ScriptIgnore]
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
        [XmlIgnore, ScriptIgnore]
        [DisplayName("创建人")]
        [BindRelation("CreateUserID")]
        public String CreateUserName { get { return CreateUser + ""; } }

        private IManageUser _UpdateUser;
        /// <summary>更新人</summary>
        [XmlIgnore, ScriptIgnore]
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
        [XmlIgnore, ScriptIgnore]
        [DisplayName("更新人")]
        [BindRelation("UpdateUserID")]
        public String UpdateUserName { get { return UpdateUser + ""; } }

        [XmlIgnore, ScriptIgnore]
        Int32 IUserInfo2.CreateUserID { get { return (Int32)this[__Name.CreateUserID]; } set { SetItem(__Name.CreateUserID, value); } }
        [XmlIgnore, ScriptIgnore]
        Int32 IUserInfo2.UpdateUserID { get { return (Int32)this[__Name.UpdateUserID]; } set { SetItem(__Name.UpdateUserID, value); } }

        [XmlIgnore, ScriptIgnore]
        DateTime ITimeInfo.CreateTime { get { return (DateTime)this[__Name.CreateTime]; } set { SetItem(__Name.CreateTime, value); } }
        [XmlIgnore, ScriptIgnore]
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
        ///// <summary>验证数据，自动加上创建和更新的信息</summary>
        ///// <param name="isNew"></param>
        //public override void Valid(bool isNew)
        //{
        //    if (!isNew && !HasDirty) return;

        //    base.Valid(isNew);

        //    var fs = Meta.FieldNames;

        //    // 当前登录用户
        //    var user = ManageProvider.Provider.Current;
        //    if (user != null)
        //    {
        //        if (isNew)
        //        {
        //            SetDirtyItem(__Name.CreateUserID, user.ID);
        //            SetDirtyItem(__Name.CreateUserName, user + "");
        //        }
        //        SetDirtyItem(__Name.UpdateUserID, user.ID);
        //        SetDirtyItem(__Name.UpdateUserName, user + "");
        //    }
        //    if (isNew)
        //        SetDirtyItem(__Name.CreateTime, DateTime.Now);

        //    // 不管新建还是更新，都改变更新时间
        //    SetDirtyItem(__Name.UpdateTime, DateTime.Now);
        //}
        #endregion

        #region 扩展属性
        private IManageUser _CreateUser;
        /// <summary>创建人</summary>
        [XmlIgnore, ScriptIgnore]
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
        [XmlIgnore, ScriptIgnore]
        [DisplayName("创建人")]
        [BindRelation("CreateUserID")]
        public String CreateUserName { get { return CreateUser + ""; } }

        private IManageUser _UpdateUser;
        /// <summary>更新人</summary>
        [XmlIgnore, ScriptIgnore]
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
        [XmlIgnore, ScriptIgnore]
        [DisplayName("更新人")]
        [BindRelation("UpdateUserID")]
        public String UpdateUserName { get { return UpdateUser + ""; } }

        [XmlIgnore, ScriptIgnore]
        Int32 IUserInfo2.CreateUserID { get { return (Int32)this[__Name.CreateUserID]; } set { SetItem(__Name.CreateUserID, value); } }
        [XmlIgnore, ScriptIgnore]
        Int32 IUserInfo2.UpdateUserID { get { return (Int32)this[__Name.UpdateUserID]; } set { SetItem(__Name.UpdateUserID, value); } }

        [XmlIgnore, ScriptIgnore]
        DateTime ITimeInfo.CreateTime { get { return (DateTime)this[__Name.CreateTime]; } set { SetItem(__Name.CreateTime, value); } }
        [XmlIgnore, ScriptIgnore]
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

    ///// <summary>IP地址信息接口。包含创建地址和更新地址</summary>
    //public interface IIPInfo
    //{
    //    /// <summary>创建IP地址</summary>
    //    DateTime CreateIP { get; set; }

    //    /// <summary>更新IP地址</summary>
    //    DateTime UpdateIP { get; set; }
    //}
}