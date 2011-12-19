/*
 * XCoder v4.3.2011.0915
 * 作者：nnhy/X
 * 时间：2011-09-25 12:57:49
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>分类</summary>
    [Serializable]
    [DataObject]
    [Description("分类")]
    [BindIndex("IX_Category", true, "Name")]
    [BindIndex("IX_Category_1", false, "ParentID")]
    [BindIndex("PK__Category__3214EC2747DBAE45", true, "ID")]
    [BindTable("Category", Description = "分类", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Category<TEntity> : ICategory
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

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(2, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private Int32 _ParentID;
        /// <summary>父分类</summary>
        [DisplayName("父分类")]
        [Description("父分类")]
        [DataObjectField(false, false, false, 10)]
        [BindColumn(3, "ParentID", "父分类", null, "int", 10, 0, false)]
        public Int32 ParentID
        {
            get { return _ParentID; }
            set { if (OnPropertyChanging("ParentID", value)) { _ParentID = value; OnPropertyChanged("ParentID"); } }
        }

        private Int32 _Sort;
        /// <summary>排序</summary>
        [DisplayName("排序")]
        [Description("排序")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "Sort", "排序", null, "int", 10, 0, false)]
        public Int32 Sort
        {
            get { return _Sort; }
            set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } }
        }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 250)]
        [BindColumn(5, "Remark", "备注", null, "nvarchar(250)", 0, 0, true)]
        public String Remark
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
                    case "ParentID" : return _ParentID;
                    case "Sort" : return _Sort;
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
                    case "ParentID" : _ParentID = Convert.ToInt32(value); break;
                    case "Sort" : _Sort = Convert.ToInt32(value); break;
                    case "Remark" : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得分类字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>父分类</summary>
            public static readonly Field ParentID = Meta.Table.FindByName("ParentID");

            ///<summary>排序</summary>
            public static readonly Field Sort = Meta.Table.FindByName("Sort");

            ///<summary>备注</summary>
            public static readonly Field Remark = Meta.Table.FindByName("Remark");
        }
        #endregion
    }

    /// <summary>分类接口</summary>
    public partial interface ICategory
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>父分类</summary>
        Int32 ParentID { get; set; }

        /// <summary>排序</summary>
        Int32 Sort { get; set; }

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