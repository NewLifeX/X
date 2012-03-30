/*
 * XCoder v3.4.2011.0329
 * 作者：nnhy/NEWLIFE
 * 时间：2011-05-06 10:35:57
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>管理员</summary>>
    [Serializable]
    [DataObject]
    [BindIndex("IX_Administrator_Name", true, "Name")]
    [BindIndex("PK__Administrator", true, "ID")]
    [BindIndex("IX_Administrator_RoleID", false, "RoleID")]
    [BindRelation("RoleID", false, "Role", "ID")]
    [Description("管理员")]
    [BindTable("Administrator", Description = "管理员", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Administrator<TEntity> : IAdministrator
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>>
        [Description("编号")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn(1, "ID", "编号", "", "int", 10, 0, false)]
        public Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private String _Name;
        /// <summary>名称</summary>>
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "名称", "", "nvarchar(50)", 0, 0, true)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private String _Password;
        /// <summary>密码</summary>>
        [Description("密码")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Password", "密码", "", "nvarchar(50)", 0, 0, true)]
        public String Password
        {
            get { return _Password; }
            set { if (OnPropertyChanging("Password", value)) { _Password = value; OnPropertyChanged("Password"); } }
        }

        private String _DisplayName;
        /// <summary>显示名</summary>>
        [Description("显示名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "DisplayName", "显示名", "", "nvarchar(50)", 0, 0, true)]
        public String DisplayName
        {
            get { return _DisplayName; }
            set { if (OnPropertyChanging("DisplayName", value)) { _DisplayName = value; OnPropertyChanged("DisplayName"); } }
        }

        private Int32 _RoleID;
        /// <summary>角色</summary>>
        [Description("角色")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(5, "RoleID", "角色", "", "int", 10, 0, false)]
        public Int32 RoleID
        {
            get { return _RoleID; }
            set { if (OnPropertyChanging("RoleID", value)) { _RoleID = value; OnPropertyChanged("RoleID"); } }
        }

        private Int32 _Logins;
        /// <summary>登录次数</summary>>
        [Description("登录次数")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(6, "Logins", "登录次数", "", "int", 10, 0, false)]
        public Int32 Logins
        {
            get { return _Logins; }
            set { if (OnPropertyChanging("Logins", value)) { _Logins = value; OnPropertyChanged("Logins"); } }
        }

        private DateTime _LastLogin;
        /// <summary>最后登录</summary>>
        [Description("最后登录")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(7, "LastLogin", "最后登录", "", "datetime", 3, 0, false)]
        public DateTime LastLogin
        {
            get { return _LastLogin; }
            set { if (OnPropertyChanging("LastLogin", value)) { _LastLogin = value; OnPropertyChanged("LastLogin"); } }
        }

        private String _LastLoginIP;
        /// <summary>最后登陆IP</summary>>
        [Description("最后登陆IP")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(8, "LastLoginIP", "最后登陆IP", "", "nvarchar(50)", 0, 0, true)]
        public String LastLoginIP
        {
            get { return _LastLoginIP; }
            set { if (OnPropertyChanging("LastLoginIP", value)) { _LastLoginIP = value; OnPropertyChanged("LastLoginIP"); } }
        }

        private Int32 _SSOUserID;
        /// <summary>登录用户编号</summary>>
        [Description("登录用户编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(9, "SSOUserID", "登录用户编号", "", "int", 10, 0, false)]
        public Int32 SSOUserID
        {
            get { return _SSOUserID; }
            set { if (OnPropertyChanging("SSOUserID", value)) { _SSOUserID = value; OnPropertyChanged("SSOUserID"); } }
        }

        private Boolean _IsEnable;
        /// <summary>是否使用</summary>>
        [Description("是否使用")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(10, "IsEnable", "是否使用", "", "bit", 0, 0, false)]
        public Boolean IsEnable
        {
            get { return _IsEnable; }
            set { if (OnPropertyChanging("IsEnable", value)) { _IsEnable = value; OnPropertyChanged("IsEnable"); } }
        }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，基类使用反射实现。
        /// 派生实体类可重写该索引，以避免反射带来的性能损耗
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case "ID": return _ID;
                    case "Name": return _Name;
                    case "Password": return _Password;
                    case "DisplayName": return _DisplayName;
                    case "RoleID": return _RoleID;
                    case "Logins": return _Logins;
                    case "LastLogin": return _LastLogin;
                    case "LastLoginIP": return _LastLoginIP;
                    case "SSOUserID": return _SSOUserID;
                    case "IsEnable": return _IsEnable;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = Convert.ToInt32(value); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "Password": _Password = Convert.ToString(value); break;
                    case "DisplayName": _DisplayName = Convert.ToString(value); break;
                    case "RoleID": _RoleID = Convert.ToInt32(value); break;
                    case "Logins": _Logins = Convert.ToInt32(value); break;
                    case "LastLogin": _LastLogin = Convert.ToDateTime(value); break;
                    case "LastLoginIP": _LastLoginIP = Convert.ToString(value); break;
                    case "SSOUserID": _SSOUserID = Convert.ToInt32(value); break;
                    case "IsEnable": _IsEnable = Convert.ToBoolean(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得管理员字段名的快捷方式</summary>>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>密码</summary>
            public static readonly Field Password = Meta.Table.FindByName("Password");

            ///<summary>显示名</summary>
            public static readonly Field DisplayName = Meta.Table.FindByName("DisplayName");

            ///<summary>角色</summary>
            public static readonly Field RoleID = Meta.Table.FindByName("RoleID");

            ///<summary>登录次数</summary>
            public static readonly Field Logins = Meta.Table.FindByName("Logins");

            ///<summary>最后登录</summary>
            public static readonly Field LastLogin = Meta.Table.FindByName("LastLogin");

            ///<summary>最后登陆IP</summary>
            public static readonly Field LastLoginIP = Meta.Table.FindByName("LastLoginIP");

            ///<summary>登录用户编号</summary>
            public static readonly Field SSOUserID = Meta.Table.FindByName("SSOUserID");

            ///<summary>是否使用</summary>
            public static readonly Field IsEnable = Meta.Table.FindByName("IsEnable");
        }
        #endregion
    }

    /// <summary>管理员接口</summary>>
    public partial interface IAdministrator
    {
        #region 属性
        /// <summary>编号</summary>>
        Int32 ID { get; set; }

        /// <summary>名称</summary>>
        String Name { get; set; }

        /// <summary>密码</summary>>
        String Password { get; set; }

        /// <summary>显示名</summary>>
        String DisplayName { get; set; }

        /// <summary>角色</summary>>
        Int32 RoleID { get; set; }

        /// <summary>登录次数</summary>>
        Int32 Logins { get; set; }

        /// <summary>最后登录</summary>>
        DateTime LastLogin { get; set; }

        /// <summary>最后登陆IP</summary>>
        String LastLoginIP { get; set; }

        /// <summary>登录用户编号</summary>>
        Int32 SSOUserID { get; set; }

        /// <summary>是否使用</summary>>
        Boolean IsEnable { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，基类使用反射实现。
        /// 派生实体类可重写该索引，以避免反射带来的性能损耗
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}