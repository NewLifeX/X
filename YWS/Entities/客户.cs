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
    /// <summary>客户</summary>
    [Serializable]
    [DataObject]
    [Description("客户")]
    [BindIndex("IX_Customer", true, "No")]
    [BindIndex("IX_Customer_1", true, "Name")]
    [BindIndex("PK__Customer__3214EC270AD2A005", true, "ID")]
    [BindIndex("IX_Customer_CustomerTypeID", false, "CustomerTypeID")]
    [BindRelation("ID", true, "Feedliquor", "CustomerID")]
    [BindRelation("CustomerTypeID", false, "CustomerType", "ID")]
    [BindRelation("ID", true, "Maintenance", "CustomerID")]
    [BindRelation("ID", true, "Machine", "CustomerID")]
    [BindRelation("ID", true, "Record", "CustomerID")]
    [BindTable("Customer", Description = "客户", ConnName = "YWS", DbType = DatabaseType.SqlServer)]
    public partial class Customer : ICustomer
    
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

        private String _No;
        /// <summary>客户编号</summary>
        [DisplayName("客户编号")]
        [Description("客户编号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "No", "客户编号", null, "nvarchar(50)", 0, 0, true)]
        public String No
        {
            get { return _No; }
            set { if (OnPropertyChanging("No", value)) { _No = value; OnPropertyChanged("No"); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private String _Linkman;
        /// <summary>联系人</summary>
        [DisplayName("联系人")]
        [Description("联系人")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Linkman", "联系人", null, "nvarchar(50)", 0, 0, true)]
        public String Linkman
        {
            get { return _Linkman; }
            set { if (OnPropertyChanging("Linkman", value)) { _Linkman = value; OnPropertyChanged("Linkman"); } }
        }

        private String _Department;
        /// <summary>部门</summary>
        [DisplayName("部门")]
        [Description("部门")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "Department", "部门", null, "nvarchar(50)", 0, 0, true)]
        public String Department
        {
            get { return _Department; }
            set { if (OnPropertyChanging("Department", value)) { _Department = value; OnPropertyChanged("Department"); } }
        }

        private String _Tel;
        /// <summary>电话</summary>
        [DisplayName("电话")]
        [Description("电话")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(6, "Tel", "电话", null, "nvarchar(50)", 0, 0, true)]
        public String Tel
        {
            get { return _Tel; }
            set { if (OnPropertyChanging("Tel", value)) { _Tel = value; OnPropertyChanged("Tel"); } }
        }

        private String _Fax;
        /// <summary>传真</summary>
        [DisplayName("传真")]
        [Description("传真")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "Fax", "传真", null, "nvarchar(50)", 0, 0, true)]
        public String Fax
        {
            get { return _Fax; }
            set { if (OnPropertyChanging("Fax", value)) { _Fax = value; OnPropertyChanged("Fax"); } }
        }

        private String _Email;
        /// <summary>邮箱</summary>
        [DisplayName("邮箱")]
        [Description("邮箱")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(8, "Email", "邮箱", null, "nvarchar(50)", 0, 0, true)]
        public String Email
        {
            get { return _Email; }
            set { if (OnPropertyChanging("Email", value)) { _Email = value; OnPropertyChanged("Email"); } }
        }

        private String _QQ;
        /// <summary>QQ</summary>
        [DisplayName("QQ")]
        [Description("QQ")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(9, "QQ", "QQ", null, "nvarchar(50)", 0, 0, true)]
        public String QQ
        {
            get { return _QQ; }
            set { if (OnPropertyChanging("QQ", value)) { _QQ = value; OnPropertyChanged("QQ"); } }
        }

        private String _MSN;
        /// <summary>MSN</summary>
        [DisplayName("MSN")]
        [Description("MSN")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(10, "MSN", "MSN", null, "nvarchar(50)", 0, 0, true)]
        public String MSN
        {
            get { return _MSN; }
            set { if (OnPropertyChanging("MSN", value)) { _MSN = value; OnPropertyChanged("MSN"); } }
        }

        private String _Address;
        /// <summary>客户地址</summary>
        [DisplayName("客户地址")]
        [Description("客户地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(11, "Address", "客户地址", null, "nvarchar(50)", 0, 0, true)]
        public String Address
        {
            get { return _Address; }
            set { if (OnPropertyChanging("Address", value)) { _Address = value; OnPropertyChanged("Address"); } }
        }

        private DateTime _AddTime;
        /// <summary>添加时间</summary>
        [DisplayName("添加时间")]
        [Description("添加时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(12, "AddTime", "添加时间", null, "datetime", 3, 0, false)]
        public DateTime AddTime
        {
            get { return _AddTime; }
            set { if (OnPropertyChanging("AddTime", value)) { _AddTime = value; OnPropertyChanged("AddTime"); } }
        }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 100)]
        [BindColumn(13, "Remark", "备注", null, "nvarchar(100)", 0, 0, true)]
        public String Remark
        {
            get { return _Remark; }
            set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } }
        }

        private Int32 _CustomerTypeID;
        /// <summary>类别</summary>
        [DisplayName("类别")]
        [Description("类别")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(14, "CustomerTypeID", "类别", null, "int", 10, 0, false)]
        public Int32 CustomerTypeID
        {
            get { return _CustomerTypeID; }
            set { if (OnPropertyChanging("CustomerTypeID", value)) { _CustomerTypeID = value; OnPropertyChanged("CustomerTypeID"); } }
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
                    case "No" : return _No;
                    case "Name" : return _Name;
                    case "Linkman" : return _Linkman;
                    case "Department" : return _Department;
                    case "Tel" : return _Tel;
                    case "Fax" : return _Fax;
                    case "Email" : return _Email;
                    case "QQ" : return _QQ;
                    case "MSN" : return _MSN;
                    case "Address" : return _Address;
                    case "AddTime" : return _AddTime;
                    case "Remark" : return _Remark;
                    case "CustomerTypeID" : return _CustomerTypeID;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "No" : _No = Convert.ToString(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    case "Linkman" : _Linkman = Convert.ToString(value); break;
                    case "Department" : _Department = Convert.ToString(value); break;
                    case "Tel" : _Tel = Convert.ToString(value); break;
                    case "Fax" : _Fax = Convert.ToString(value); break;
                    case "Email" : _Email = Convert.ToString(value); break;
                    case "QQ" : _QQ = Convert.ToString(value); break;
                    case "MSN" : _MSN = Convert.ToString(value); break;
                    case "Address" : _Address = Convert.ToString(value); break;
                    case "AddTime" : _AddTime = Convert.ToDateTime(value); break;
                    case "Remark" : _Remark = Convert.ToString(value); break;
                    case "CustomerTypeID" : _CustomerTypeID = Convert.ToInt32(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得客户字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>客户编号</summary>
            public static readonly Field No = Meta.Table.FindByName("No");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>联系人</summary>
            public static readonly Field Linkman = Meta.Table.FindByName("Linkman");

            ///<summary>部门</summary>
            public static readonly Field Department = Meta.Table.FindByName("Department");

            ///<summary>电话</summary>
            public static readonly Field Tel = Meta.Table.FindByName("Tel");

            ///<summary>传真</summary>
            public static readonly Field Fax = Meta.Table.FindByName("Fax");

            ///<summary>邮箱</summary>
            public static readonly Field Email = Meta.Table.FindByName("Email");

            ///<summary>QQ</summary>
            public static readonly Field QQ = Meta.Table.FindByName("QQ");

            ///<summary>MSN</summary>
            public static readonly Field MSN = Meta.Table.FindByName("MSN");

            ///<summary>客户地址</summary>
            public static readonly Field Address = Meta.Table.FindByName("Address");

            ///<summary>添加时间</summary>
            public static readonly Field AddTime = Meta.Table.FindByName("AddTime");

            ///<summary>备注</summary>
            public static readonly Field Remark = Meta.Table.FindByName("Remark");

            ///<summary>类别</summary>
            public static readonly Field CustomerTypeID = Meta.Table.FindByName("CustomerTypeID");
        }
        #endregion
    }

    /// <summary>客户接口</summary>
    public partial interface ICustomer
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>客户编号</summary>
        String No { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>联系人</summary>
        String Linkman { get; set; }

        /// <summary>部门</summary>
        String Department { get; set; }

        /// <summary>电话</summary>
        String Tel { get; set; }

        /// <summary>传真</summary>
        String Fax { get; set; }

        /// <summary>邮箱</summary>
        String Email { get; set; }

        /// <summary>QQ</summary>
        String QQ { get; set; }

        /// <summary>MSN</summary>
        String MSN { get; set; }

        /// <summary>客户地址</summary>
        String Address { get; set; }

        /// <summary>添加时间</summary>
        DateTime AddTime { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }

        /// <summary>类别</summary>
        Int32 CustomerTypeID { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}