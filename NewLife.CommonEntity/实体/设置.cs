/*
 * XCoder v4.8.4526.33278
 * 作者：nnhy/NEWLIFE
 * 时间：2012-05-24 16:59:45
 * 版权：版权所有 (C) 新生命开发团队 2012
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>设置</summary>
    [Serializable]
    [DataObject]
    [Description("设置")]
    [BindIndex("IX_ParentID_Name", true, "ParentID,Name")]
    [BindIndex("PK__Setting__3214EC277F60ED59", true, "ID")]
    [BindTable("Setting", Description = "设置", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Setting<TEntity> : ISetting
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

        private Int32 _ParentID;
        /// <summary>父编号</summary>
        [DisplayName("父编号")]
        [Description("父编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(2, "ParentID", "父编号", null, "int", 10, 0, false)]
        public virtual Int32 ParentID
        {
            get { return _ParentID; }
            set { if (OnPropertyChanging("ParentID", value)) { _ParentID = value; OnPropertyChanged("ParentID"); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private Int32 _Kind;
        /// <summary>值类型</summary>
        [DisplayName("值类型")]
        [Description("值类型")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "Kind", "值类型", null, "int", 10, 0, false)]
        public virtual Int32 Kind
        {
            get { return _Kind; }
            set { if (OnPropertyChanging("Kind", value)) { _Kind = value; OnPropertyChanged("Kind"); } }
        }

        private String _Value;
        /// <summary>值</summary>
        [DisplayName("值")]
        [Description("值")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn(5, "Value", "值", null, "nvarchar(500)", 0, 0, true)]
        public virtual String Value
        {
            get { return _Value; }
            set { if (OnPropertyChanging("Value", value)) { _Value = value; OnPropertyChanged("Value"); } }
        }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [DisplayName("显示名")]
        [Description("显示名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(6, "DisplayName", "显示名", null, "nvarchar(50)", 0, 0, true)]
        public virtual String DisplayName
        {
            get { return _DisplayName; }
            set { if (OnPropertyChanging("DisplayName", value)) { _DisplayName = value; OnPropertyChanged("DisplayName"); } }
        }

        private Int32 _Sort;
        /// <summary>排序</summary>
        [DisplayName("排序")]
        [Description("排序")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(7, "Sort", "排序", null, "int", 10, 0, false)]
        public virtual Int32 Sort
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
                    case "ParentID" : return _ParentID;
                    case "Name" : return _Name;
                    case "Kind" : return _Kind;
                    case "Value" : return _Value;
                    case "DisplayName" : return _DisplayName;
                    case "Sort" : return _Sort;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "ParentID" : _ParentID = Convert.ToInt32(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    case "Kind" : _Kind = Convert.ToInt32(value); break;
                    case "Value" : _Value = Convert.ToString(value); break;
                    case "DisplayName" : _DisplayName = Convert.ToString(value); break;
                    case "Sort" : _Sort = Convert.ToInt32(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得设置字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            ///<summary>父编号</summary>
            public static readonly Field ParentID = FindByName("ParentID");

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName("Name");

            ///<summary>值类型</summary>
            public static readonly Field Kind = FindByName("Kind");

            ///<summary>值</summary>
            public static readonly Field Value = FindByName("Value");

            ///<summary>显示名</summary>
            public static readonly Field DisplayName = FindByName("DisplayName");

            ///<summary>排序</summary>
            public static readonly Field Sort = FindByName("Sort");

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }
        #endregion
    }

    /// <summary>设置接口</summary>
    public partial interface ISetting
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>父编号</summary>
        Int32 ParentID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>值类型</summary>
        Int32 Kind { get; set; }

        /// <summary>值</summary>
        String Value { get; set; }

        /// <summary>显示名</summary>
        String DisplayName { get; set; }

        /// <summary>排序</summary>
        Int32 Sort { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}