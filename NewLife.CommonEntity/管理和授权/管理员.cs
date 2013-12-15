﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>管理员</summary>
    [Serializable]
    [DataObject]
    [Description("管理员")]
    [BindIndex("IX_Administrator_Name", true, "Name")]
    [BindIndex("IX_Administrator_RoleID", false, "RoleID")]
    [BindRelation("RoleID", false, "Role", "ID")]
    [BindTable("Administrator", Description = "管理员", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public abstract partial class Administrator<TEntity> : IAdministrator
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn(1, "ID", "编号", null, "int", 10, 0, false)]
        public virtual Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } }
        }

        private String _Password;
        /// <summary>密码</summary>
        [DisplayName("密码")]
        [Description("密码")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Password", "密码", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Password
        {
            get { return _Password; }
            set { if (OnPropertyChanging(__.Password, value)) { _Password = value; OnPropertyChanged(__.Password); } }
        }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [DisplayName("显示名")]
        [Description("显示名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "DisplayName", "显示名", null, "nvarchar(50)", 0, 0, true)]
        public virtual String DisplayName
        {
            get { return _DisplayName; }
            set { if (OnPropertyChanging(__.DisplayName, value)) { _DisplayName = value; OnPropertyChanged(__.DisplayName); } }
        }

        private Int32 _RoleID;
        /// <summary>角色</summary>
        [DisplayName("角色")]
        [Description("角色")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(5, "RoleID", "角色", null, "int", 10, 0, false)]
        public virtual Int32 RoleID
        {
            get { return _RoleID; }
            set { if (OnPropertyChanging(__.RoleID, value)) { _RoleID = value; OnPropertyChanged(__.RoleID); } }
        }

        private Int32 _Logins;
        /// <summary>登录次数</summary>
        [DisplayName("登录次数")]
        [Description("登录次数")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(6, "Logins", "登录次数", null, "int", 10, 0, false)]
        public virtual Int32 Logins
        {
            get { return _Logins; }
            set { if (OnPropertyChanging(__.Logins, value)) { _Logins = value; OnPropertyChanged(__.Logins); } }
        }

        private DateTime _LastLogin;
        /// <summary>最后登录</summary>
        [DisplayName("最后登录")]
        [Description("最后登录")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(7, "LastLogin", "最后登录", null, "datetime", 3, 0, false)]
        public virtual DateTime LastLogin
        {
            get { return _LastLogin; }
            set { if (OnPropertyChanging(__.LastLogin, value)) { _LastLogin = value; OnPropertyChanged(__.LastLogin); } }
        }

        private String _LastLoginIP;
        /// <summary>最后登陆IP</summary>
        [DisplayName("最后登陆IP")]
        [Description("最后登陆IP")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(8, "LastLoginIP", "最后登陆IP", null, "nvarchar(50)", 0, 0, true)]
        public virtual String LastLoginIP
        {
            get { return _LastLoginIP; }
            set { if (OnPropertyChanging(__.LastLoginIP, value)) { _LastLoginIP = value; OnPropertyChanged(__.LastLoginIP); } }
        }

        private Int32 _SSOUserID;
        /// <summary>登录用户编号</summary>
        [DisplayName("登录用户编号")]
        [Description("登录用户编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(9, "SSOUserID", "登录用户编号", null, "int", 10, 0, false)]
        public virtual Int32 SSOUserID
        {
            get { return _SSOUserID; }
            set { if (OnPropertyChanging(__.SSOUserID, value)) { _SSOUserID = value; OnPropertyChanged(__.SSOUserID); } }
        }

        private Boolean _IsEnable;
        /// <summary>是否使用</summary>
        [DisplayName("是否使用")]
        [Description("是否使用")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(10, "IsEnable", "是否使用", null, "bit", 0, 0, false)]
        public virtual Boolean IsEnable
        {
            get { return _IsEnable; }
            set { if (OnPropertyChanging(__.IsEnable, value)) { _IsEnable = value; OnPropertyChanged(__.IsEnable); } }
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
                    case __.ID : return _ID;
                    case __.Name : return _Name;
                    case __.Password : return _Password;
                    case __.DisplayName : return _DisplayName;
                    case __.RoleID : return _RoleID;
                    case __.Logins : return _Logins;
                    case __.LastLogin : return _LastLogin;
                    case __.LastLoginIP : return _LastLoginIP;
                    case __.SSOUserID : return _SSOUserID;
                    case __.IsEnable : return _IsEnable;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.Password : _Password = Convert.ToString(value); break;
                    case __.DisplayName : _DisplayName = Convert.ToString(value); break;
                    case __.RoleID : _RoleID = Convert.ToInt32(value); break;
                    case __.Logins : _Logins = Convert.ToInt32(value); break;
                    case __.LastLogin : _LastLogin = Convert.ToDateTime(value); break;
                    case __.LastLoginIP : _LastLoginIP = Convert.ToString(value); break;
                    case __.SSOUserID : _SSOUserID = Convert.ToInt32(value); break;
                    case __.IsEnable : _IsEnable = Convert.ToBoolean(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得管理员字段信息的快捷方式</summary>
        public partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>密码</summary>
            public static readonly Field Password = FindByName(__.Password);

            ///<summary>显示名</summary>
            public static readonly Field DisplayName = FindByName(__.DisplayName);

            ///<summary>角色</summary>
            public static readonly Field RoleID = FindByName(__.RoleID);

            ///<summary>登录次数</summary>
            public static readonly Field Logins = FindByName(__.Logins);

            ///<summary>最后登录</summary>
            public static readonly Field LastLogin = FindByName(__.LastLogin);

            ///<summary>最后登陆IP</summary>
            public static readonly Field LastLoginIP = FindByName(__.LastLoginIP);

            ///<summary>登录用户编号</summary>
            public static readonly Field SSOUserID = FindByName(__.SSOUserID);

            ///<summary>是否使用</summary>
            public static readonly Field IsEnable = FindByName(__.IsEnable);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得管理员字段名称的快捷方式</summary>
        public partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>名称</summary>
            public const String Name = "Name";

            ///<summary>密码</summary>
            public const String Password = "Password";

            ///<summary>显示名</summary>
            public const String DisplayName = "DisplayName";

            ///<summary>角色</summary>
            public const String RoleID = "RoleID";

            ///<summary>登录次数</summary>
            public const String Logins = "Logins";

            ///<summary>最后登录</summary>
            public const String LastLogin = "LastLogin";

            ///<summary>最后登陆IP</summary>
            public const String LastLoginIP = "LastLoginIP";

            ///<summary>登录用户编号</summary>
            public const String SSOUserID = "SSOUserID";

            ///<summary>是否使用</summary>
            public const String IsEnable = "IsEnable";

        }
        #endregion
    }

    /// <summary>管理员接口</summary>
    public partial interface IAdministrator
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>密码</summary>
        String Password { get; set; }

        /// <summary>显示名</summary>
        String DisplayName { get; set; }

        /// <summary>角色</summary>
        Int32 RoleID { get; set; }

        /// <summary>登录次数</summary>
        Int32 Logins { get; set; }

        /// <summary>最后登录</summary>
        DateTime LastLogin { get; set; }

        /// <summary>最后登陆IP</summary>
        String LastLoginIP { get; set; }

        /// <summary>登录用户编号</summary>
        Int32 SSOUserID { get; set; }

        /// <summary>是否使用</summary>
        Boolean IsEnable { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}