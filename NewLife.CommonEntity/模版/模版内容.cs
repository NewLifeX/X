/*
 * XCoder v4.5.2011.1108
 * 作者：nnhy/NEWLIFE
 * 时间：2011-11-14 16:58:16
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
    /// <summary>模版内容</summary>
    [Serializable]
    [DataObject]
    [Description("模版内容")]
    [BindIndex("IX_TemplateContent", true, "TemplateItemID,Version")]
    [BindIndex("IX_TemplateContent_1", false, "UserID")]
    [BindIndex("IX_TemplateContent_TemplateItemID", false, "TemplateItemID")]
    [BindIndex("PK__Template__3214EC27182C9B23", true, "ID")]
    [BindRelation("TemplateItemID", false, "TemplateItem", "ID")]
    [BindTable("TemplateContent", Description = "模版内容", ConnName = "Template", DbType = DatabaseType.SqlServer)]
    public partial class TemplateContent<TEntity> : ITemplateContent
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
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private Int32 _TemplateItemID;
        /// <summary>模版项</summary>
        [DisplayName("模版项")]
        [Description("模版项")]
        [DataObjectField(false, false, false, 10)]
        [BindColumn(2, "TemplateItemID", "模版项", null, "int", 10, 0, false)]
        public virtual Int32 TemplateItemID
        {
            get { return _TemplateItemID; }
            set { if (OnPropertyChanging("TemplateItemID", value)) { _TemplateItemID = value; OnPropertyChanged("TemplateItemID"); } }
        }

        private String _Content;
        /// <summary>模版内容</summary>
        [DisplayName("模版内容")]
        [Description("模版内容")]
        [DataObjectField(false, false, true, 1073741823)]
        [BindColumn(3, "Content", "模版内容", null, "ntext", 0, 0, true)]
        public virtual String Content
        {
            get { return _Content; }
            set { if (OnPropertyChanging("Content", value)) { _Content = value; OnPropertyChanged("Content"); } }
        }

        private String _ContentBackup;
        /// <summary>内容备份</summary>
        [DisplayName("内容备份")]
        [Description("内容备份")]
        [DataObjectField(false, false, true, 1073741823)]
        [BindColumn(4, "ContentBackup", "内容备份", null, "ntext", 0, 0, true)]
        public virtual String ContentBackup
        {
            get { return _ContentBackup; }
            set { if (OnPropertyChanging("ContentBackup", value)) { _ContentBackup = value; OnPropertyChanged("ContentBackup"); } }
        }

        private Int32 _Version;
        /// <summary>版本</summary>
        [DisplayName("版本")]
        [Description("版本")]
        [DataObjectField(false, false, false, 10)]
        [BindColumn(5, "Version", "版本", null, "int", 10, 0, false)]
        public virtual Int32 Version
        {
            get { return _Version; }
            set { if (OnPropertyChanging("Version", value)) { _Version = value; OnPropertyChanged("Version"); } }
        }

        private Int32 _UserID;
        /// <summary>作者编号</summary>
        [DisplayName("作者编号")]
        [Description("作者编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(6, "UserID", "作者编号", null, "int", 10, 0, false)]
        public virtual Int32 UserID
        {
            get { return _UserID; }
            set { if (OnPropertyChanging("UserID", value)) { _UserID = value; OnPropertyChanged("UserID"); } }
        }

        private String _UserName;
        /// <summary>作者</summary>
        [DisplayName("作者")]
        [Description("作者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "UserName", "作者", null, "nvarchar(50)", 0, 0, true)]
        public virtual String UserName
        {
            get { return _UserName; }
            set { if (OnPropertyChanging("UserName", value)) { _UserName = value; OnPropertyChanged("UserName"); } }
        }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(8, "CreateTime", "创建时间", null, "datetime", 3, 0, false)]
        public virtual DateTime CreateTime
        {
            get { return _CreateTime; }
            set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } }
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
                    case "TemplateItemID" : return _TemplateItemID;
                    case "Content" : return _Content;
                    case "ContentBackup" : return _ContentBackup;
                    case "Version" : return _Version;
                    case "UserID" : return _UserID;
                    case "UserName" : return _UserName;
                    case "CreateTime" : return _CreateTime;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "TemplateItemID" : _TemplateItemID = Convert.ToInt32(value); break;
                    case "Content" : _Content = Convert.ToString(value); break;
                    case "ContentBackup" : _ContentBackup = Convert.ToString(value); break;
                    case "Version" : _Version = Convert.ToInt32(value); break;
                    case "UserID" : _UserID = Convert.ToInt32(value); break;
                    case "UserName" : _UserName = Convert.ToString(value); break;
                    case "CreateTime" : _CreateTime = Convert.ToDateTime(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得模版内容字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly FieldItem ID = Meta.Table.FindByName("ID");

            ///<summary>模版项</summary>
            public static readonly FieldItem TemplateItemID = Meta.Table.FindByName("TemplateItemID");

            ///<summary>模版内容</summary>
            public static readonly FieldItem Content = Meta.Table.FindByName("Content");

            ///<summary>内容备份</summary>
            public static readonly FieldItem ContentBackup = Meta.Table.FindByName("ContentBackup");

            ///<summary>版本</summary>
            public static readonly FieldItem Version = Meta.Table.FindByName("Version");

            ///<summary>作者编号</summary>
            public static readonly FieldItem UserID = Meta.Table.FindByName("UserID");

            ///<summary>作者</summary>
            public static readonly FieldItem UserName = Meta.Table.FindByName("UserName");

            ///<summary>创建时间</summary>
            public static readonly FieldItem CreateTime = Meta.Table.FindByName("CreateTime");
        }
        #endregion
    }

    /// <summary>模版内容接口</summary>
    public partial interface ITemplateContent
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>模版项</summary>
        Int32 TemplateItemID { get; set; }

        /// <summary>模版内容</summary>
        String Content { get; set; }

        /// <summary>内容备份</summary>
        String ContentBackup { get; set; }

        /// <summary>版本</summary>
        Int32 Version { get; set; }

        /// <summary>作者编号</summary>
        Int32 UserID { get; set; }

        /// <summary>作者</summary>
        String UserName { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}
#pragma warning restore 3008
#pragma warning restore 3021