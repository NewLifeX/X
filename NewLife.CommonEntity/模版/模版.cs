/*
 * XCoder v4.5.2011.1108
 * 作者：nnhy/NEWLIFE
 * 时间：2011-11-11 18:21:52
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
    /// <summary>模版</summary>
    [Serializable]
    [DataObject]
    [Description("模版")]
    [BindIndex("IX_Template", true, "Name")]
    [BindIndex("IX_Template_1", false, "AuthorID")]
    [BindIndex("PK_Template", true, "ID")]
    [BindRelation("ID", true, "TemplateItem", "TemplateID")]
    [BindTable("Template", Description = "模版", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Template<TEntity> : ITemplate
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

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private Int32 _AuthorID;
        /// <summary>作者编号</summary>
        [DisplayName("作者编号")]
        [Description("作者编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "AuthorID", "作者编号", null, "int", 10, 0, false)]
        public virtual Int32 AuthorID
        {
            get { return _AuthorID; }
            set { if (OnPropertyChanging("AuthorID", value)) { _AuthorID = value; OnPropertyChanged("AuthorID"); } }
        }

        private String _AuthorName;
        /// <summary>作者</summary>
        [DisplayName("作者")]
        [Description("作者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "AuthorName", "作者", null, "nvarchar(50)", 0, 0, true)]
        public virtual String AuthorName
        {
            get { return _AuthorName; }
            set { if (OnPropertyChanging("AuthorName", value)) { _AuthorName = value; OnPropertyChanged("AuthorName"); } }
        }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(5, "CreateTime", "创建时间", null, "datetime", 3, 0, false)]
        public virtual DateTime CreateTime
        {
            get { return _CreateTime; }
            set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } }
        }

        private DateTime _LastModify;
        /// <summary>最后修改</summary>
        [DisplayName("最后修改")]
        [Description("最后修改")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(6, "LastModify", "最后修改", null, "datetime", 3, 0, false)]
        public virtual DateTime LastModify
        {
            get { return _LastModify; }
            set { if (OnPropertyChanging("LastModify", value)) { _LastModify = value; OnPropertyChanged("LastModify"); } }
        }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(7, "Remark", "备注", null, "nvarchar(200)", 0, 0, true)]
        public virtual String Remark
        {
            get { return _Remark; }
            set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } }
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
                    case "Name" : return _Name;
                    case "AuthorID" : return _AuthorID;
                    case "AuthorName" : return _AuthorName;
                    case "CreateTime" : return _CreateTime;
                    case "LastModify" : return _LastModify;
                    case "Remark" : return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    case "AuthorID" : _AuthorID = Convert.ToInt32(value); break;
                    case "AuthorName" : _AuthorName = Convert.ToString(value); break;
                    case "CreateTime" : _CreateTime = Convert.ToDateTime(value); break;
                    case "LastModify" : _LastModify = Convert.ToDateTime(value); break;
                    case "Remark" : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得模版字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly FieldItem ID = Meta.Table.FindByName("ID");

            ///<summary>名称</summary>
            public static readonly FieldItem Name = Meta.Table.FindByName("Name");

            ///<summary>作者编号</summary>
            public static readonly FieldItem AuthorID = Meta.Table.FindByName("AuthorID");

            ///<summary>作者</summary>
            public static readonly FieldItem AuthorName = Meta.Table.FindByName("AuthorName");

            ///<summary>创建时间</summary>
            public static readonly FieldItem CreateTime = Meta.Table.FindByName("CreateTime");

            ///<summary>最后修改</summary>
            public static readonly FieldItem LastModify = Meta.Table.FindByName("LastModify");

            ///<summary>备注</summary>
            public static readonly FieldItem Remark = Meta.Table.FindByName("Remark");
        }
        #endregion
    }

    /// <summary>模版接口</summary>
    public partial interface ITemplate
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>作者编号</summary>
        Int32 AuthorID { get; set; }

        /// <summary>作者</summary>
        String AuthorName { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>最后修改</summary>
        DateTime LastModify { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }
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