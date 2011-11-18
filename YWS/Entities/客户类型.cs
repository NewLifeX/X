/*
 * XCoder v4.3.2011.0915
 * 作者：nnhy/NEWLIFE
 * 时间：2011-09-28 11:04:30
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.YWS.Entities
{
    /// <summary>客户类型</summary>
    [Serializable]
    [DataObject]
    [Description("客户类型")]
    [BindIndex("IX_CustomerType", true, "Name,ParentID")]
    [BindIndex("IX_CustomerType_1", false, "Name")]
    [BindIndex("IX_CustomerType_2", false, "ParentID")]
    [BindIndex("PK__Customer__3214EC27164452B1", true, "ID")]
    [BindRelation("ID", true, "Customer", "CustomerTypeID")]
    [BindTable("CustomerType", Description = "客户类型", ConnName = "YWS", DbType = DatabaseType.SqlServer)]
    public partial class CustomerType : ICustomerType
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
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private Int32 _ParentID;
        /// <summary>ParentID</summary>
        [DisplayName("ParentID")]
        [Description("ParentID")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "ParentID", "ParentID", null, "int", 10, 0, false)]
        public Int32 ParentID
        {
            get { return _ParentID; }
            set { if (OnPropertyChanging("ParentID", value)) { _ParentID = value; OnPropertyChanged("ParentID"); } }
        }

        private DateTime _AddTime;
        /// <summary>添加时间</summary>
        [DisplayName("添加时间")]
        [Description("添加时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(4, "AddTime", "添加时间", null, "datetime", 3, 0, false)]
        public DateTime AddTime
        {
            get { return _AddTime; }
            set { if (OnPropertyChanging("AddTime", value)) { _AddTime = value; OnPropertyChanged("AddTime"); } }
        }

        private String _Operator2;
        /// <summary>添加人</summary>
        [DisplayName("添加人")]
        [Description("添加人")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "Operator", "添加人", null, "nvarchar(50)", 0, 0, true)]
        public String Operator2
        {
            get { return _Operator2; }
            set { if (OnPropertyChanging("Operator2", value)) { _Operator2 = value; OnPropertyChanged("Operator2"); } }
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
                    case "AddTime" : return _AddTime;
                    case "Operator2" : return _Operator2;
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
                    case "AddTime" : _AddTime = Convert.ToDateTime(value); break;
                    case "Operator2" : _Operator2 = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得客户类型字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>ParentID</summary>
            public static readonly Field ParentID = Meta.Table.FindByName("ParentID");

            ///<summary>添加时间</summary>
            public static readonly Field AddTime = Meta.Table.FindByName("AddTime");

            ///<summary>添加人</summary>
            public static readonly Field Operator2 = Meta.Table.FindByName("Operator2");
        }
        #endregion
    }

    /// <summary>客户类型接口</summary>
    public partial interface ICustomerType
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>ParentID</summary>
        Int32 ParentID { get; set; }

        /// <summary>添加时间</summary>
        DateTime AddTime { get; set; }

        /// <summary>添加人</summary>
        String Operator2 { get; set; }
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