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
    /// <summary>模版项</summary>
    [Serializable]
    [DataObject]
    [Description("模版项")]
    [BindIndex("IX_TemplateItem", true, "TemplateID,Name")]
    [BindIndex("IX_TemplateItem_TemplateID", false, "TemplateID")]
    [BindIndex("PK__Template__3214EC271BFD2C07", true, "ID")]
    [BindRelation("ID", true, "TemplateContent", "TemplateItemID")]
    [BindRelation("TemplateID", false, "Template", "ID")]
    [BindTable("TemplateItem", Description = "模版项", ConnName = "Template", DbType = DatabaseType.SqlServer)]
    public partial class TemplateItem<TEntity> : ITemplateItem
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

        private Int32 _TemplateID;
        /// <summary>模版</summary>
        [DisplayName("模版")]
        [Description("模版")]
        [DataObjectField(false, false, false, 10)]
        [BindColumn(2, "TemplateID", "模版", null, "int", 10, 0, false)]
        public virtual Int32 TemplateID
        {
            get { return _TemplateID; }
            set { if (OnPropertyChanging("TemplateID", value)) { _TemplateID = value; OnPropertyChanged("TemplateID"); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(3, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private String _Kind;
        /// <summary>模版种类</summary>
        [DisplayName("模版种类")]
        [Description("模版种类")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Kind", "模版种类", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Kind
        {
            get { return _Kind; }
            set { if (OnPropertyChanging("Kind", value)) { _Kind = value; OnPropertyChanged("Kind"); } }
        }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn(5, "Remark", "备注", null, "nvarchar(500)", 0, 0, true)]
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
                    case "TemplateID" : return _TemplateID;
                    case "Name" : return _Name;
                    case "Kind" : return _Kind;
                    case "Remark" : return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "TemplateID" : _TemplateID = Convert.ToInt32(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    case "Kind" : _Kind = Convert.ToString(value); break;
                    case "Remark" : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得模版项字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>模版</summary>
            public static readonly Field TemplateID = Meta.Table.FindByName("TemplateID");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>模版种类</summary>
            public static readonly Field Kind = Meta.Table.FindByName("Kind");

            ///<summary>备注</summary>
            public static readonly Field Remark = Meta.Table.FindByName("Remark");
        }
        #endregion
    }

    /// <summary>模版项接口</summary>
    public partial interface ITemplateItem
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>模版</summary>
        Int32 TemplateID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>模版种类</summary>
        String Kind { get; set; }

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