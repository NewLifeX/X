﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Membership
{
    /// <summary>用户</summary>
    [Serializable]
    [DataObject]
    [Description("用户")]
    [BindIndex("IU_User_Name", true, "Name")]
    [BindIndex("IX_User_RoleID", false, "RoleID")]
    [BindRelation("RoleID", false, "Role", "ID")]
    [BindTable("User", Description = "用户", ConnName = "Membership", DbType = DatabaseType.SqlServer)]
    public abstract partial class User<TEntity> : IUser
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
        /// <summary>名称。登录用户名</summary>
        [DisplayName("名称")]
        [Description("名称。登录用户名")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(2, "Name", "名称。登录用户名", null, "nvarchar(50)", 0, 0, true, Master=true)]
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
        /// <summary>显示名。昵称、中文名等</summary>
        [DisplayName("显示名")]
        [Description("显示名。昵称、中文名等")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "DisplayName", "显示名。昵称、中文名等", null, "nvarchar(50)", 0, 0, true)]
        public virtual String DisplayName
        {
            get { return _DisplayName; }
            set { if (OnPropertyChanging(__.DisplayName, value)) { _DisplayName = value; OnPropertyChanged(__.DisplayName); } }
        }

        private Int32 _Sex;
        /// <summary>性别。未知、男、女</summary>
        [DisplayName("性别")]
        [Description("性别。未知、男、女")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(5, "Sex", "性别。未知、男、女", null, "int", 10, 0, false)]
        public virtual Int32 Sex
        {
            get { return _Sex; }
            set { if (OnPropertyChanging(__.Sex, value)) { _Sex = value; OnPropertyChanged(__.Sex); } }
        }

        private String _Mail;
        /// <summary>邮件</summary>
        [DisplayName("邮件")]
        [Description("邮件")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(6, "Mail", "邮件", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Mail
        {
            get { return _Mail; }
            set { if (OnPropertyChanging(__.Mail, value)) { _Mail = value; OnPropertyChanged(__.Mail); } }
        }

        private String _Phone;
        /// <summary>电话</summary>
        [DisplayName("电话")]
        [Description("电话")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "Phone", "电话", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Phone
        {
            get { return _Phone; }
            set { if (OnPropertyChanging(__.Phone, value)) { _Phone = value; OnPropertyChanged(__.Phone); } }
        }

        private String _Code;
        /// <summary>代码。身份证、员工编号等</summary>
        [DisplayName("代码")]
        [Description("代码。身份证、员工编号等")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(8, "Code", "代码。身份证、员工编号等", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Code
        {
            get { return _Code; }
            set { if (OnPropertyChanging(__.Code, value)) { _Code = value; OnPropertyChanged(__.Code); } }
        }

        private Int32 _RoleID;
        /// <summary>角色</summary>
        [DisplayName("角色")]
        [Description("角色")]
        [DataObjectField(false, false, false, 10)]
        [BindColumn(9, "RoleID", "角色", null, "int", 10, 0, false)]
        public virtual Int32 RoleID
        {
            get { return _RoleID; }
            set { if (OnPropertyChanging(__.RoleID, value)) { _RoleID = value; OnPropertyChanged(__.RoleID); } }
        }

        private Boolean _Enable;
        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        [Description("是否启用")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(10, "Enable", "是否启用", null, "bit", 0, 0, false)]
        public virtual Boolean Enable
        {
            get { return _Enable; }
            set { if (OnPropertyChanging(__.Enable, value)) { _Enable = value; OnPropertyChanged(__.Enable); } }
        }

        private DateTime _StartTime;
        /// <summary>开始时间</summary>
        [DisplayName("开始时间")]
        [Description("开始时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(11, "StartTime", "开始时间", null, "datetime", 3, 0, false)]
        public virtual DateTime StartTime
        {
            get { return _StartTime; }
            set { if (OnPropertyChanging(__.StartTime, value)) { _StartTime = value; OnPropertyChanged(__.StartTime); } }
        }

        private DateTime _EndTime;
        /// <summary>结束时间</summary>
        [DisplayName("结束时间")]
        [Description("结束时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(12, "EndTime", "结束时间", null, "datetime", 3, 0, false)]
        public virtual DateTime EndTime
        {
            get { return _EndTime; }
            set { if (OnPropertyChanging(__.EndTime, value)) { _EndTime = value; OnPropertyChanged(__.EndTime); } }
        }

        private DateTime _RegisterTime;
        /// <summary>注册时间</summary>
        [DisplayName("注册时间")]
        [Description("注册时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(13, "RegisterTime", "注册时间", null, "datetime", 3, 0, false)]
        public virtual DateTime RegisterTime
        {
            get { return _RegisterTime; }
            set { if (OnPropertyChanging(__.RegisterTime, value)) { _RegisterTime = value; OnPropertyChanged(__.RegisterTime); } }
        }

        private Int32 _Logins;
        /// <summary>登录次数</summary>
        [DisplayName("登录次数")]
        [Description("登录次数")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(14, "Logins", "登录次数", null, "int", 10, 0, false)]
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
        [BindColumn(15, "LastLogin", "最后登录", null, "datetime", 3, 0, false)]
        public virtual DateTime LastLogin
        {
            get { return _LastLogin; }
            set { if (OnPropertyChanging(__.LastLogin, value)) { _LastLogin = value; OnPropertyChanged(__.LastLogin); } }
        }

        private String _LastLoginIP;
        /// <summary>最后登录IP</summary>
        [DisplayName("最后登录IP")]
        [Description("最后登录IP")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(16, "LastLoginIP", "最后登录IP", null, "nvarchar(50)", 0, 0, true)]
        public virtual String LastLoginIP
        {
            get { return _LastLoginIP; }
            set { if (OnPropertyChanging(__.LastLoginIP, value)) { _LastLoginIP = value; OnPropertyChanged(__.LastLoginIP); } }
        }

        private String _Quertion;
        /// <summary>问题</summary>
        [DisplayName("问题")]
        [Description("问题")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(17, "Quertion", "问题", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Quertion
        {
            get { return _Quertion; }
            set { if (OnPropertyChanging(__.Quertion, value)) { _Quertion = value; OnPropertyChanged(__.Quertion); } }
        }

        private String _Answer;
        /// <summary>答案</summary>
        [DisplayName("答案")]
        [Description("答案")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(18, "Answer", "答案", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Answer
        {
            get { return _Answer; }
            set { if (OnPropertyChanging(__.Answer, value)) { _Answer = value; OnPropertyChanged(__.Answer); } }
        }

        private String _Profile;
        /// <summary>配置信息</summary>
        [DisplayName("配置信息")]
        [Description("配置信息")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn(19, "Profile", "配置信息", null, "nvarchar(500)", 0, 0, true)]
        public virtual String Profile
        {
            get { return _Profile; }
            set { if (OnPropertyChanging(__.Profile, value)) { _Profile = value; OnPropertyChanged(__.Profile); } }
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
                    case __.Sex : return _Sex;
                    case __.Mail : return _Mail;
                    case __.Phone : return _Phone;
                    case __.Code : return _Code;
                    case __.RoleID : return _RoleID;
                    case __.Enable : return _Enable;
                    case __.StartTime : return _StartTime;
                    case __.EndTime : return _EndTime;
                    case __.RegisterTime : return _RegisterTime;
                    case __.Logins : return _Logins;
                    case __.LastLogin : return _LastLogin;
                    case __.LastLoginIP : return _LastLoginIP;
                    case __.Quertion : return _Quertion;
                    case __.Answer : return _Answer;
                    case __.Profile : return _Profile;
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
                    case __.Sex : _Sex = Convert.ToInt32(value); break;
                    case __.Mail : _Mail = Convert.ToString(value); break;
                    case __.Phone : _Phone = Convert.ToString(value); break;
                    case __.Code : _Code = Convert.ToString(value); break;
                    case __.RoleID : _RoleID = Convert.ToInt32(value); break;
                    case __.Enable : _Enable = Convert.ToBoolean(value); break;
                    case __.StartTime : _StartTime = Convert.ToDateTime(value); break;
                    case __.EndTime : _EndTime = Convert.ToDateTime(value); break;
                    case __.RegisterTime : _RegisterTime = Convert.ToDateTime(value); break;
                    case __.Logins : _Logins = Convert.ToInt32(value); break;
                    case __.LastLogin : _LastLogin = Convert.ToDateTime(value); break;
                    case __.LastLoginIP : _LastLoginIP = Convert.ToString(value); break;
                    case __.Quertion : _Quertion = Convert.ToString(value); break;
                    case __.Answer : _Answer = Convert.ToString(value); break;
                    case __.Profile : _Profile = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得用户字段信息的快捷方式</summary>
        partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>名称。登录用户名</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>密码</summary>
            public static readonly Field Password = FindByName(__.Password);

            ///<summary>显示名。昵称、中文名等</summary>
            public static readonly Field DisplayName = FindByName(__.DisplayName);

            ///<summary>性别。未知、男、女</summary>
            public static readonly Field Sex = FindByName(__.Sex);

            ///<summary>邮件</summary>
            public static readonly Field Mail = FindByName(__.Mail);

            ///<summary>电话</summary>
            public static readonly Field Phone = FindByName(__.Phone);

            ///<summary>代码。身份证、员工编号等</summary>
            public static readonly Field Code = FindByName(__.Code);

            ///<summary>角色</summary>
            public static readonly Field RoleID = FindByName(__.RoleID);

            ///<summary>是否启用</summary>
            public static readonly Field Enable = FindByName(__.Enable);

            ///<summary>开始时间</summary>
            public static readonly Field StartTime = FindByName(__.StartTime);

            ///<summary>结束时间</summary>
            public static readonly Field EndTime = FindByName(__.EndTime);

            ///<summary>注册时间</summary>
            public static readonly Field RegisterTime = FindByName(__.RegisterTime);

            ///<summary>登录次数</summary>
            public static readonly Field Logins = FindByName(__.Logins);

            ///<summary>最后登录</summary>
            public static readonly Field LastLogin = FindByName(__.LastLogin);

            ///<summary>最后登录IP</summary>
            public static readonly Field LastLoginIP = FindByName(__.LastLoginIP);

            ///<summary>问题</summary>
            public static readonly Field Quertion = FindByName(__.Quertion);

            ///<summary>答案</summary>
            public static readonly Field Answer = FindByName(__.Answer);

            ///<summary>配置信息</summary>
            public static readonly Field Profile = FindByName(__.Profile);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得用户字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>名称。登录用户名</summary>
            public const String Name = "Name";

            ///<summary>密码</summary>
            public const String Password = "Password";

            ///<summary>显示名。昵称、中文名等</summary>
            public const String DisplayName = "DisplayName";

            ///<summary>性别。未知、男、女</summary>
            public const String Sex = "Sex";

            ///<summary>邮件</summary>
            public const String Mail = "Mail";

            ///<summary>电话</summary>
            public const String Phone = "Phone";

            ///<summary>代码。身份证、员工编号等</summary>
            public const String Code = "Code";

            ///<summary>角色</summary>
            public const String RoleID = "RoleID";

            ///<summary>是否启用</summary>
            public const String Enable = "Enable";

            ///<summary>开始时间</summary>
            public const String StartTime = "StartTime";

            ///<summary>结束时间</summary>
            public const String EndTime = "EndTime";

            ///<summary>注册时间</summary>
            public const String RegisterTime = "RegisterTime";

            ///<summary>登录次数</summary>
            public const String Logins = "Logins";

            ///<summary>最后登录</summary>
            public const String LastLogin = "LastLogin";

            ///<summary>最后登录IP</summary>
            public const String LastLoginIP = "LastLoginIP";

            ///<summary>问题</summary>
            public const String Quertion = "Quertion";

            ///<summary>答案</summary>
            public const String Answer = "Answer";

            ///<summary>配置信息</summary>
            public const String Profile = "Profile";

        }
        #endregion
    }

    /// <summary>用户接口</summary>
    public partial interface IUser
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称。登录用户名</summary>
        String Name { get; set; }

        /// <summary>密码</summary>
        String Password { get; set; }

        /// <summary>显示名。昵称、中文名等</summary>
        String DisplayName { get; set; }

        /// <summary>性别。未知、男、女</summary>
        Int32 Sex { get; set; }

        /// <summary>邮件</summary>
        String Mail { get; set; }

        /// <summary>电话</summary>
        String Phone { get; set; }

        /// <summary>代码。身份证、员工编号等</summary>
        String Code { get; set; }

        /// <summary>角色</summary>
        Int32 RoleID { get; set; }

        /// <summary>是否启用</summary>
        Boolean Enable { get; set; }

        /// <summary>开始时间</summary>
        DateTime StartTime { get; set; }

        /// <summary>结束时间</summary>
        DateTime EndTime { get; set; }

        /// <summary>注册时间</summary>
        DateTime RegisterTime { get; set; }

        /// <summary>登录次数</summary>
        Int32 Logins { get; set; }

        /// <summary>最后登录</summary>
        DateTime LastLogin { get; set; }

        /// <summary>最后登录IP</summary>
        String LastLoginIP { get; set; }

        /// <summary>问题</summary>
        String Quertion { get; set; }

        /// <summary>答案</summary>
        String Answer { get; set; }

        /// <summary>配置信息</summary>
        String Profile { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}