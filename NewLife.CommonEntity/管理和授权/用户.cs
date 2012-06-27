/*
 * XCoder v4.3.2011.0920
 * 作者：nnhy/NEWLIFE
 * 时间：2011-10-18 10:51:07
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using System.Xml.Serialization;
using XCode.Configuration;
using XCode.DataAccessLayer;

#pragma warning disable 3021
#pragma warning disable 3008
namespace NewLife.CommonEntity
{
    /// <summary>用户</summary>
    [Serializable]
    [DataObject]
    [Description("用户")]
    [BindIndex("IX_User", true, "Account")]
    [BindIndex("PK_User", true, "ID")]
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
        public Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private String _Account;
        /// <summary>账号</summary>
        [DisplayName("账号")]
        [Description("账号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Account", "账号", null, "nvarchar(50)", 0, 0, true)]
        public String Account
        {
            get { return _Account; }
            set { if (OnPropertyChanging("Account", value)) { _Account = value; OnPropertyChanged("Account"); } }
        }

        private String _Password;
        /// <summary>密码</summary>
        [DisplayName("密码")]
        [Description("密码")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Password", "密码", null, "nvarchar(50)", 0, 0, true)]
        public String Password
        {
            get { return _Password; }
            set { if (OnPropertyChanging("Password", value)) { _Password = value; OnPropertyChanged("Password"); } }
        }

        private Boolean _IsAdmin;
        /// <summary>是否管理员</summary>
        [DisplayName("是否管理员")]
        [Description("是否管理员")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(4, "IsAdmin", "是否管理员", null, "bit", 0, 0, true)]
        public Boolean IsAdmin
        {
            get { return _IsAdmin; }
            set { if (OnPropertyChanging("IsAdmin", value)) { _IsAdmin = value; OnPropertyChanged("IsAdmin"); } }
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
                    case "ID" : return _ID;
                    case "Account" : return _Account;
                    case "Password" : return _Password;
                    case "IsAdmin": return _IsAdmin;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "Account" : _Account = Convert.ToString(value); break;
                    case "Password" : _Password = Convert.ToString(value); break;
                    case "IsAdmin": _IsAdmin = Convert.ToBoolean(value); break;
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
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>账号</summary>
            public static readonly Field Account = Meta.Table.FindByName("Account");

            ///<summary>密码</summary>
            public static readonly Field Password = Meta.Table.FindByName("Password");

            ///<summary>是否管理员</summary>
            public static readonly Field IsAdmin = Meta.Table.FindByName("IsAdmin");
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
        Boolean IsAdmin { get; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}
#pragma warning restore 3008
#pragma warning restore 3021