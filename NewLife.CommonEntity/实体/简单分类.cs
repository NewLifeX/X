/*
 * XCoder v4.3.2011.0920
 * 作者：X/X-PC
 * 时间：2011-10-27 10:48:28
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
    /// <summary>简单分类</summary>
    [Serializable]
    [DataObject]
    [Description("简单分类")]
    [BindIndex("IX_SimpleTree", true, "Name")]
    [BindIndex("PK_SimpleTree", true, "ID")]
    [BindTable("SimpleTree", Description = "简单分类", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class SimpleTree<TEntity> : ISimpleTree
    
    {
        #region 属性
        private Int32 _ID;
        /// <summary></summary>
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
        /// <summary>名称索引</summary>
        [DisplayName("名称索引")]
        [Description("名称索引")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "名称索引", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private Int32 _ParentID;
        /// <summary></summary>
        [DisplayName("")]
        [Description("")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "ParentID", "", null, "int", 10, 0, false)]
        public virtual Int32 ParentID
        {
            get { return _ParentID; }
            set { if (OnPropertyChanging("ParentID", value)) { _ParentID = value; OnPropertyChanged("ParentID"); } }
        }

        private String _Sort;
        /// <summary></summary>
        [DisplayName("")]
        [Description("")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Sort", "", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Sort
        {
            get { return _Sort; }
            set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } }
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
                    case "ParentID" : return _ParentID;
                    case "Sort" : return _Sort;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    case "ParentID" : _ParentID = Convert.ToInt32(value); break;
                    case "Sort" : _Sort = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得简单分类字段信息的快捷方式</summary>
        public class _
        {
            ///<summary></summary>
            public static readonly FieldItem ID = Meta.Table.FindByName("ID");

            ///<summary>名称索引</summary>
            public static readonly FieldItem Name = Meta.Table.FindByName("Name");

            ///<summary></summary>
            public static readonly FieldItem ParentID = Meta.Table.FindByName("ParentID");

            ///<summary></summary>
            public static readonly FieldItem Sort = Meta.Table.FindByName("Sort");
        }
        #endregion
    }

    /// <summary>简单分类接口</summary>
    public partial interface ISimpleTree
    {
        #region 属性
        /// <summary></summary>
        Int32 ID { get; set; }

        /// <summary>名称索引</summary>
        String Name { get; set; }

        /// <summary></summary>
        Int32 ParentID { get; set; }

        /// <summary></summary>
        String Sort { get; set; }
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