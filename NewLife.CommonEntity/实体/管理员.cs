using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 管理员
    /// </summary>
    [Serializable]
    [DataObject]
    [Description("管理员")]
    [BindTable("Administrator", Description = "管理员", ConnName = "Common")]
    public partial class Administrator<TEntity>
    {
        #region 属性
        private Int32 _ID;
        /// <summary>
        /// 编号
        /// </summary>
        [Description("编号")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn("ID", Description = "编号", DefaultValue = "", Order = 1)]
        public Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChange("ID", value)) _ID = value; }
        }

        private String _Name;
        /// <summary>
        /// 名称
        /// </summary>
        [Description("名称")]
        [DisplayName("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", Description = "名称", DefaultValue = "", Order = 2)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChange("Name", value)) _Name = value; }
        }

        private String _Password;
        /// <summary>
        /// 密码
        /// </summary>
        [Description("密码")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Password", Description = "密码", DefaultValue = "", Order = 3)]
        public String Password
        {
            get { return _Password; }
            set { if (OnPropertyChange("Password", value)) _Password = value; }
        }

        private String _DisplayName;
        /// <summary>
        /// 显示名
        /// </summary>
        [Description("显示名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("DisplayName", Description = "显示名", DefaultValue = "", Order = 4)]
        public String DisplayName
        {
            get { return _DisplayName; }
            set { if (OnPropertyChange("DisplayName", value)) _DisplayName = value; }
        }

        private Int32 _RoleID;
        /// <summary>
        /// 角色
        /// </summary>
        [Description("角色")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn("RoleID", Description = "角色", DefaultValue = "", Order = 5)]
        public Int32 RoleID
        {
            get { return _RoleID; }
            set { if (OnPropertyChange("RoleID", value)) _RoleID = value; }
        }

        private Int32 _Logins;
        /// <summary>
        /// 登录次数
        /// </summary>
        [Description("登录次数")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn("Logins", Description = "登录次数", DefaultValue = "", Order = 6)]
        public Int32 Logins
        {
            get { return _Logins; }
            set { if (OnPropertyChange("Logins", value)) _Logins = value; }
        }

        private DateTime _LastLogin;
        /// <summary>
        /// 最后登录
        /// </summary>
        [Description("最后登录")]
        [DataObjectField(false, false, true, 23)]
        [BindColumn("LastLogin", Description = "最后登录", DefaultValue = "", Order = 7)]
        public DateTime LastLogin
        {
            get { return _LastLogin; }
            set { if (OnPropertyChange("LastLogin", value)) _LastLogin = value; }
        }

        private String _LastLoginIP;
        /// <summary>
        /// 最后登陆IP
        /// </summary>
        [Description("最后登陆IP")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("LastLoginIP", Description = "最后登陆IP", DefaultValue = "", Order = 8)]
        public String LastLoginIP
        {
            get { return _LastLoginIP; }
            set { if (OnPropertyChange("LastLoginIP", value)) _LastLoginIP = value; }
        }

        private Int32 _SSOUserID;
        /// <summary>
        /// 登录用户编号
        /// </summary>
        [Description("登录用户编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn("SSOUserID", Description = "登录用户编号", DefaultValue = "", Order = 9)]
        public Int32 SSOUserID
        {
            get { return _SSOUserID; }
            set { if (OnPropertyChange("SSOUserID", value)) _SSOUserID = value; }
        }

        private Boolean _IsEnable;
        /// <summary>
        /// 是否使用
        /// </summary>
        [Description("是否使用")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn("IsEnable", Description = "是否使用", DefaultValue = "", Order = 10)]
        public Boolean IsEnable
        {
            get { return _IsEnable; }
            set { if (OnPropertyChange("IsEnable", value)) _IsEnable = value; }
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
                    case "ID": return ID;
                    case "Name": return Name;
                    case "Password": return Password;
                    case "DisplayName": return DisplayName;
                    case "RoleID": return RoleID;
                    case "Logins": return Logins;
                    case "LastLogin": return LastLogin;
                    case "LastLoginIP": return LastLoginIP;
                    case "SSOUserID": return SSOUserID;
                    case "IsEnable": return IsEnable;
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
        /// <summary>
        /// 取得字段名的快捷方式
        /// </summary>
        [CLSCompliant(false)]
        public class _
        {
            /// <summary>
            /// 编号
            /// </summary>
            public const String ID = "ID";

            /// <summary>
            /// 名称
            /// </summary>
            public const String Name = "Name";

            /// <summary>
            /// 密码
            /// </summary>
            public const String Password = "Password";

            /// <summary>
            /// 显示名
            /// </summary>
            public const String DisplayName = "DisplayName";

            /// <summary>
            /// 角色
            /// </summary>
            public const String RoleID = "RoleID";

            /// <summary>
            /// 登录次数
            /// </summary>
            public const String Logins = "Logins";

            /// <summary>
            /// 最后登录
            /// </summary>
            public const String LastLogin = "LastLogin";

            /// <summary>
            /// 最后登陆IP
            /// </summary>
            public const String LastLoginIP = "LastLoginIP";

            /// <summary>
            /// 登录用户编号
            /// </summary>
            public const String SSOUserID = "SSOUserID";

            /// <summary>
            /// 是否使用
            /// </summary>
            public const String IsEnable = "IsEnable";
        }
        #endregion
    }
}