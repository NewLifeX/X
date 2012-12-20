﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>菜单</summary>
    [Serializable]
    [DataObject]
    [Description("菜单")]
    [BindIndex("IX_Menu_Name", false, "Name")]
    [BindIndex("IX_Menu_ParentID_Name", false, "ParentID,Name")]
    [BindRelation("ID", true, "RoleMenu", "MenuID")]
    [BindTable("Menu", Description = "菜单", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Menu<TEntity> : IMenu
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

        private Int32 _ParentID;
        /// <summary>父编号</summary>
        [DisplayName("父编号")]
        [Description("父编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "ParentID", "父编号", null, "int", 10, 0, false)]
        public virtual Int32 ParentID
        {
            get { return _ParentID; }
            set { if (OnPropertyChanging(__.ParentID, value)) { _ParentID = value; OnPropertyChanged(__.ParentID); } }
        }

        private String _Url;
        /// <summary>链接</summary>
        [DisplayName("链接")]
        [Description("链接")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(4, "Url", "链接", null, "nvarchar(200)", 0, 0, true)]
        public virtual String Url
        {
            get { return _Url; }
            set { if (OnPropertyChanging(__.Url, value)) { _Url = value; OnPropertyChanged(__.Url); } }
        }

        private Int32 _Sort;
        /// <summary>序号</summary>
        [DisplayName("序号")]
        [Description("序号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(5, "Sort", "序号", null, "int", 10, 0, false)]
        public virtual Int32 Sort
        {
            get { return _Sort; }
            set { if (OnPropertyChanging(__.Sort, value)) { _Sort = value; OnPropertyChanged(__.Sort); } }
        }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn(6, "Remark", "备注", null, "nvarchar(500)", 0, 0, true)]
        public virtual String Remark
        {
            get { return _Remark; }
            set { if (OnPropertyChanging(__.Remark, value)) { _Remark = value; OnPropertyChanged(__.Remark); } }
        }

        private String _Permission;
        /// <summary>权限</summary>
        [DisplayName("权限")]
        [Description("权限")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "Permission", "权限", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Permission
        {
            get { return _Permission; }
            set { if (OnPropertyChanging(__.Permission, value)) { _Permission = value; OnPropertyChanged(__.Permission); } }
        }

        private Boolean _IsShow;
        /// <summary>是否显示</summary>
        [DisplayName("是否显示")]
        [Description("是否显示")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(8, "IsShow", "是否显示", null, "bit", 0, 0, false)]
        public virtual Boolean IsShow
        {
            get { return _IsShow; }
            set { if (OnPropertyChanging(__.IsShow, value)) { _IsShow = value; OnPropertyChanged(__.IsShow); } }
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
                    case __.ParentID : return _ParentID;
                    case __.Url : return _Url;
                    case __.Sort : return _Sort;
                    case __.Remark : return _Remark;
                    case __.Permission : return _Permission;
                    case __.IsShow : return _IsShow;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.ParentID : _ParentID = Convert.ToInt32(value); break;
                    case __.Url : _Url = Convert.ToString(value); break;
                    case __.Sort : _Sort = Convert.ToInt32(value); break;
                    case __.Remark : _Remark = Convert.ToString(value); break;
                    case __.Permission : _Permission = Convert.ToString(value); break;
                    case __.IsShow : _IsShow = Convert.ToBoolean(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得菜单字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>父编号</summary>
            public static readonly Field ParentID = FindByName(__.ParentID);

            ///<summary>链接</summary>
            public static readonly Field Url = FindByName(__.Url);

            ///<summary>序号</summary>
            public static readonly Field Sort = FindByName(__.Sort);

            ///<summary>备注</summary>
            public static readonly Field Remark = FindByName(__.Remark);

            ///<summary>权限</summary>
            public static readonly Field Permission = FindByName(__.Permission);

            ///<summary>是否显示</summary>
            public static readonly Field IsShow = FindByName(__.IsShow);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得菜单字段名称的快捷方式</summary>
        class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>名称</summary>
            public const String Name = "Name";

            ///<summary>父编号</summary>
            public const String ParentID = "ParentID";

            ///<summary>链接</summary>
            public const String Url = "Url";

            ///<summary>序号</summary>
            public const String Sort = "Sort";

            ///<summary>备注</summary>
            public const String Remark = "Remark";

            ///<summary>权限</summary>
            public const String Permission = "Permission";

            ///<summary>是否显示</summary>
            public const String IsShow = "IsShow";

        }
        #endregion
    }

    /// <summary>菜单接口</summary>
    public partial interface IMenu
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>父编号</summary>
        Int32 ParentID { get; set; }

        /// <summary>链接</summary>
        String Url { get; set; }

        /// <summary>序号</summary>
        Int32 Sort { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }

        /// <summary>权限</summary>
        String Permission { get; set; }

        /// <summary>是否显示</summary>
        Boolean IsShow { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}