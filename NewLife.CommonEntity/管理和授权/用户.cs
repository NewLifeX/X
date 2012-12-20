﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>用户</summary>
    [Serializable]
    [DataObject]
    [Description("用户")]
    [BindIndex("IX_User_Account", true, "Account")]
    [BindTable("User", Description = "用户", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class User<TEntity> : IUser
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

        private String _Account;
        /// <summary>账号</summary>
        [DisplayName("账号")]
        [Description("账号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Account", "账号", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Account
        {
            get { return _Account; }
            set { if (OnPropertyChanging(__.Account, value)) { _Account = value; OnPropertyChanged(__.Account); } }
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

        private Boolean _IsAdmin;
        /// <summary>是否管理员</summary>
        [DisplayName("是否管理员")]
        [Description("是否管理员")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(4, "IsAdmin", "是否管理员", null, "bit", 0, 0, false)]
        public virtual Boolean IsAdmin
        {
            get { return _IsAdmin; }
            set { if (OnPropertyChanging(__.IsAdmin, value)) { _IsAdmin = value; OnPropertyChanged(__.IsAdmin); } }
        }

        private Boolean _IsEnable;
        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        [Description("是否启用")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(5, "IsEnable", "是否启用", null, "bit", 0, 0, false)]
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
                    case __.Account : return _Account;
                    case __.Password : return _Password;
                    case __.IsAdmin : return _IsAdmin;
                    case __.IsEnable : return _IsEnable;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Account : _Account = Convert.ToString(value); break;
                    case __.Password : _Password = Convert.ToString(value); break;
                    case __.IsAdmin : _IsAdmin = Convert.ToBoolean(value); break;
                    case __.IsEnable : _IsEnable = Convert.ToBoolean(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得用户字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>账号</summary>
            public static readonly Field Account = FindByName(__.Account);

            ///<summary>密码</summary>
            public static readonly Field Password = FindByName(__.Password);

            ///<summary>是否管理员</summary>
            public static readonly Field IsAdmin = FindByName(__.IsAdmin);

            ///<summary>是否启用</summary>
            public static readonly Field IsEnable = FindByName(__.IsEnable);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得用户字段名称的快捷方式</summary>
        class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>账号</summary>
            public const String Account = "Account";

            ///<summary>密码</summary>
            public const String Password = "Password";

            ///<summary>是否管理员</summary>
            public const String IsAdmin = "IsAdmin";

            ///<summary>是否启用</summary>
            public const String IsEnable = "IsEnable";

        }
        #endregion
    }

    /// <summary>用户接口</summary>
    public partial interface IUser
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>账号</summary>
        String Account { get; set; }

        /// <summary>密码</summary>
        String Password { get; set; }

        /// <summary>是否管理员</summary>
        Boolean IsAdmin { get; set; }

        /// <summary>是否启用</summary>
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