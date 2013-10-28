﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>角色和菜单</summary>
    [Serializable]
    [DataObject]
    [Description("角色和菜单")]
    [BindIndex("IX_RoleMenu_MenuID", false, "MenuID")]
    [BindIndex("IX_RoleMenu_RoleID", false, "RoleID")]
    [BindIndex("IX_RoleMenu_MenuID_RoleID", true, "MenuID,RoleID")]
    [BindRelation("MenuID", false, "Menu", "ID")]
    [BindRelation("RoleID", false, "Role", "ID")]
    [BindTable("RoleMenu", Description = "角色和菜单", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class RoleMenu<TEntity> : IRoleMenu
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

        private Int32 _RoleID;
        /// <summary>角色编号</summary>
        [DisplayName("角色编号")]
        [Description("角色编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(2, "RoleID", "角色编号", null, "int", 10, 0, false)]
        public virtual Int32 RoleID
        {
            get { return _RoleID; }
            set { if (OnPropertyChanging(__.RoleID, value)) { _RoleID = value; OnPropertyChanged(__.RoleID); } }
        }

        private Int32 _MenuID;
        /// <summary>菜单编号</summary>
        [DisplayName("菜单编号")]
        [Description("菜单编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "MenuID", "菜单编号", null, "int", 10, 0, false)]
        public virtual Int32 MenuID
        {
            get { return _MenuID; }
            set { if (OnPropertyChanging(__.MenuID, value)) { _MenuID = value; OnPropertyChanged(__.MenuID); } }
        }

        private Int32 _Permission;
        /// <summary>权限</summary>
        [DisplayName("权限")]
        [Description("权限")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "Permission", "权限", null, "int", 10, 0, false)]
        public virtual Int32 Permission
        {
            get { return _Permission; }
            set { if (OnPropertyChanging(__.Permission, value)) { _Permission = value; OnPropertyChanged(__.Permission); } }
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
                    case __.RoleID : return _RoleID;
                    case __.MenuID : return _MenuID;
                    case __.Permission : return _Permission;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.RoleID : _RoleID = Convert.ToInt32(value); break;
                    case __.MenuID : _MenuID = Convert.ToInt32(value); break;
                    case __.Permission : _Permission = Convert.ToInt32(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得角色和菜单字段信息的快捷方式</summary>
        public partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>角色编号</summary>
            public static readonly Field RoleID = FindByName(__.RoleID);

            ///<summary>菜单编号</summary>
            public static readonly Field MenuID = FindByName(__.MenuID);

            ///<summary>权限</summary>
            public static readonly Field Permission = FindByName(__.Permission);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得角色和菜单字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>角色编号</summary>
            public const String RoleID = "RoleID";

            ///<summary>菜单编号</summary>
            public const String MenuID = "MenuID";

            ///<summary>权限</summary>
            public const String Permission = "Permission";

        }
        #endregion
    }

    /// <summary>角色和菜单接口</summary>
    public partial interface IRoleMenu
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>角色编号</summary>
        Int32 RoleID { get; set; }

        /// <summary>菜单编号</summary>
        Int32 MenuID { get; set; }

        /// <summary>权限</summary>
        Int32 Permission { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}