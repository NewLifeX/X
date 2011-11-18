/*
 * XCoder v4.3.2011.0913
 * 作者：nnhy/NEWLIFE
 * 时间：2011-09-14 18:01:27
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Test
{
    /// <summary>实体测试</summary>
    [Serializable]
    [DataObject]
    [Description("实体测试")]
    [BindIndex("IX_EntityTest", true, "Name")]
    [BindIndex("IX_EntityTest_1", false, "IsEnable,StartDate,EndTime")]
    [BindIndex("IX_EntityTest_2", true, "ID")]
    [BindIndex("PK_EntityTest", true, "Guid,guid2")]
    [BindTable("EntityTest", Description = "实体测试", ConnName = "XCodeTest", DbType = DatabaseType.SqlServer)]
    public partial class EntityTest<TEntity> : IEntityTest
    {
        #region 属性
        private Guid _Guid;
        /// <summary>主键一</summary>
        [DisplayName("主键一")]
        [Description("主键一")]
        [DataObjectField(true, false, false, 16)]
        [BindColumn(1, "Guid", "主键一", null, "uniqueidentifier", 0, 0, false)]
        public Guid Guid
        {
            get { return _Guid; }
            set { if (OnPropertyChanging("Guid", value)) { _Guid = value; OnPropertyChanged("Guid"); } }
        }

        private String _Guid2;
        /// <summary>主键二</summary>
        [DisplayName("主键二")]
        [Description("主键二")]
        [DataObjectField(true, false, false, 16)]
        [BindColumn(2, "guid2", "主键二", null, "char(16)", 0, 0, false)]
        public String Guid2
        {
            get { return _Guid2; }
            set { if (OnPropertyChanging("Guid2", value)) { _Guid2 = value; OnPropertyChanged("Guid2"); } }
        }

        private SByte _ID;
        /// <summary>自增编号</summary>
        [DisplayName("自增编号")]
        [Description("自增编号")]
        [DataObjectField(false, true, false, 3)]
        [BindColumn(3, "ID", "自增编号", null, "tinyint", 3, 0, false)]
        public SByte ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private Int16 _ID2;
        /// <summary>编号二</summary>
        [DisplayName("编号二")]
        [Description("编号二")]
        [DataObjectField(false, false, true, 5)]
        [BindColumn(4, "ID2", "编号二", null, "smallint", 5, 0, false)]
        public Int16 ID2
        {
            get { return _ID2; }
            set { if (OnPropertyChanging("ID2", value)) { _ID2 = value; OnPropertyChanged("ID2"); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(5, "Name", "名称", "admin", "varchar(50)", 0, 0, false)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private String _Password;
        /// <summary>密码</summary>
        [DisplayName("密码")]
        [Description("密码")]
        [DataObjectField(false, false, true, 32)]
        [BindColumn(6, "Password", "密码", "密'admin'码", "nchar(32)", 0, 0, true)]
        public String Password
        {
            get { return _Password; }
            set { if (OnPropertyChanging("Password", value)) { _Password = value; OnPropertyChanged("Password"); } }
        }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [DisplayName("显示名")]
        [Description("显示名")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(7, "DisplayName", "显示名", null, "nvarchar(50)", 0, 0, true)]
        public String DisplayName
        {
            get { return _DisplayName; }
            set { if (OnPropertyChanging("DisplayName", value)) { _DisplayName = value; OnPropertyChanged("DisplayName"); } }
        }

        private Boolean _IsEnable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 1)]
        [BindColumn(8, "IsEnable", "启用", null, "bit", 0, 0, false)]
        public Boolean IsEnable
        {
            get { return _IsEnable; }
            set { if (OnPropertyChanging("IsEnable", value)) { _IsEnable = value; OnPropertyChanged("IsEnable"); } }
        }

        private Int64 _Logins;
        /// <summary>登录次数</summary>
        [DisplayName("登录次数")]
        [Description("登录次数")]
        [DataObjectField(false, false, true, 19)]
        [BindColumn(9, "Logins", "登录次数", "-999999999999999999.", "bigint", 19, 0, false)]
        public Int64 Logins
        {
            get { return _Logins; }
            set { if (OnPropertyChanging("Logins", value)) { _Logins = value; OnPropertyChanged("Logins"); } }
        }

        private DateTime _LastLogin;
        /// <summary>最后登陆</summary>
        [DisplayName("最后登陆")]
        [Description("最后登陆")]
        [DataObjectField(false, false, false, 3)]
        [BindColumn(10, "LastLogin", "最后登陆", "getdate()", "datetime", 3, 0, false)]
        public DateTime LastLogin
        {
            get { return _LastLogin; }
            set { if (OnPropertyChanging("LastLogin", value)) { _LastLogin = value; OnPropertyChanged("LastLogin"); } }
        }

        private DateTime _StartDate;
        /// <summary>开始日期</summary>
        [DisplayName("开始日期")]
        [Description("开始日期")]
        [DataObjectField(false, false, false, 3)]
        [BindColumn(11, "StartDate", "开始日期", "getdate()", "date", 0, 0, false)]
        public DateTime StartDate
        {
            get { return _StartDate; }
            set { if (OnPropertyChanging("StartDate", value)) { _StartDate = value; OnPropertyChanged("StartDate"); } }
        }

        private DateTime _EndTime;
        /// <summary>结束时间</summary>
        [DisplayName("结束时间")]
        [Description("结束时间")]
        [DataObjectField(false, false, false, 16)]
        [BindColumn(12, "EndTime", "结束时间", "getdate()", "smalldatetime", 0, 0, false)]
        public DateTime EndTime
        {
            get { return _EndTime; }
            set { if (OnPropertyChanging("EndTime", value)) { _EndTime = value; OnPropertyChanged("EndTime"); } }
        }

        private Decimal _Total;
        /// <summary>总数</summary>
        [DisplayName("总数")]
        [Description("总数")]
        [DataObjectField(false, false, true, 18)]
        [BindColumn(13, "Total", "总数", null, "decimal(18,0)", 18, 0, false)]
        public Decimal Total
        {
            get { return _Total; }
            set { if (OnPropertyChanging("Total", value)) { _Total = value; OnPropertyChanged("Total"); } }
        }

        private Double _Item2;
        /// <summary>百分比</summary>
        [DisplayName("百分比")]
        [Description("百分比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(14, "item", "百分比", null, "float", 53, 0, false)]
        public Double Item2
        {
            get { return _Item2; }
            set { if (OnPropertyChanging("Item2", value)) { _Item2 = value; OnPropertyChanged("Item2"); } }
        }

        private Single _EntityTest2;
        /// <summary>实数</summary>
        [DisplayName("实数")]
        [Description("实数")]
        [DataObjectField(false, false, true, 24)]
        [BindColumn(15, "EntityTest", "实数", null, "real", 24, 0, false)]
        public Single EntityTest2
        {
            get { return _EntityTest2; }
            set { if (OnPropertyChanging("EntityTest2", value)) { _EntityTest2 = value; OnPropertyChanged("EntityTest2"); } }
        }

        private Decimal _Money;
        /// <summary>金额</summary>
        [DisplayName("金额")]
        [Description("金额")]
        [DataObjectField(false, false, true, 19)]
        [BindColumn(16, "money", "金额", null, "money", 19, 4, false)]
        public Decimal Money
        {
            get { return _Money; }
            set { if (OnPropertyChanging("Money", value)) { _Money = value; OnPropertyChanged("Money"); } }
        }

        private Byte[] _File;
        /// <summary>文件</summary>
        [DisplayName("文件")]
        [Description("文件")]
        [DataObjectField(false, false, true, 2147483647)]
        [BindColumn(17, "file", "文件", null, "image", 0, 0, false)]
        public Byte[] File
        {
            get { return _File; }
            set { if (OnPropertyChanging("File", value)) { _File = value; OnPropertyChanged("File"); } }
        }

        private String _Remark;
        /// <summary>备注一</summary>
        [DisplayName("备注一")]
        [Description("备注一")]
        [DataObjectField(false, false, true, 2147483647)]
        [BindColumn(18, "remark", "备注一", "备注", "text", 0, 0, false)]
        public String Remark
        {
            get { return _Remark; }
            set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } }
        }

        private String _Remark2;
        /// <summary>备注二</summary>
        [DisplayName("备注二")]
        [Description("备注二")]
        [DataObjectField(false, false, true, -1)]
        [BindColumn(19, "remark2", "备注二", null, "nvarchar(MAX)", 0, 0, true)]
        public String Remark2
        {
            get { return _Remark2; }
            set { if (OnPropertyChanging("Remark2", value)) { _Remark2 = value; OnPropertyChanged("Remark2"); } }
        }

        private String _Description;
        /// <summary>说明</summary>
        [DisplayName("说明")]
        [Description("说明")]
        [DataObjectField(false, false, true, 1073741823)]
        [BindColumn(20, "Description", "说明", null, "ntext", 0, 0, true)]
        public String Description
        {
            get { return _Description; }
            set { if (OnPropertyChanging("Description", value)) { _Description = value; OnPropertyChanged("Description"); } }
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
                    case "Guid" : return _Guid;
                    case "Guid2" : return _Guid2;
                    case "ID" : return _ID;
                    case "ID2" : return _ID2;
                    case "Name" : return _Name;
                    case "Password" : return _Password;
                    case "DisplayName" : return _DisplayName;
                    case "IsEnable" : return _IsEnable;
                    case "Logins" : return _Logins;
                    case "LastLogin" : return _LastLogin;
                    case "StartDate" : return _StartDate;
                    case "EndTime" : return _EndTime;
                    case "Total" : return _Total;
                    case "Item2" : return _Item2;
                    case "EntityTest2" : return _EntityTest2;
                    case "Money" : return _Money;
                    case "File" : return _File;
                    case "Remark" : return _Remark;
                    case "Remark2" : return _Remark2;
                    case "Description" : return _Description;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Guid" : _Guid = (Guid)value; break;
                    case "Guid2" : _Guid2 = Convert.ToString(value); break;
                    case "ID" : _ID = Convert.ToSByte(value); break;
                    case "ID2" : _ID2 = Convert.ToInt16(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    case "Password" : _Password = Convert.ToString(value); break;
                    case "DisplayName" : _DisplayName = Convert.ToString(value); break;
                    case "IsEnable" : _IsEnable = Convert.ToBoolean(value); break;
                    case "Logins" : _Logins = Convert.ToInt64(value); break;
                    case "LastLogin" : _LastLogin = Convert.ToDateTime(value); break;
                    case "StartDate" : _StartDate = Convert.ToDateTime(value); break;
                    case "EndTime" : _EndTime = Convert.ToDateTime(value); break;
                    case "Total" : _Total = Convert.ToDecimal(value); break;
                    case "Item2" : _Item2 = Convert.ToDouble(value); break;
                    case "EntityTest2" : _EntityTest2 = Convert.ToSingle(value); break;
                    case "Money" : _Money = Convert.ToDecimal(value); break;
                    case "File" : _File = (Byte[])value; break;
                    case "Remark" : _Remark = Convert.ToString(value); break;
                    case "Remark2" : _Remark2 = Convert.ToString(value); break;
                    case "Description" : _Description = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得实体测试字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>主键一</summary>
            public static readonly Field Guid = Meta.Table.FindByName("Guid");

            ///<summary>主键二</summary>
            public static readonly Field Guid2 = Meta.Table.FindByName("Guid2");

            ///<summary>自增编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>编号二</summary>
            public static readonly Field ID2 = Meta.Table.FindByName("ID2");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>密码</summary>
            public static readonly Field Password = Meta.Table.FindByName("Password");

            ///<summary>显示名</summary>
            public static readonly Field DisplayName = Meta.Table.FindByName("DisplayName");

            ///<summary>启用</summary>
            public static readonly Field IsEnable = Meta.Table.FindByName("IsEnable");

            ///<summary>登录次数</summary>
            public static readonly Field Logins = Meta.Table.FindByName("Logins");

            ///<summary>最后登陆</summary>
            public static readonly Field LastLogin = Meta.Table.FindByName("LastLogin");

            ///<summary>开始日期</summary>
            public static readonly Field StartDate = Meta.Table.FindByName("StartDate");

            ///<summary>结束时间</summary>
            public static readonly Field EndTime = Meta.Table.FindByName("EndTime");

            ///<summary>总数</summary>
            public static readonly Field Total = Meta.Table.FindByName("Total");

            ///<summary>百分比</summary>
            public static readonly Field Item2 = Meta.Table.FindByName("Item2");

            ///<summary>实数</summary>
            public static readonly Field EntityTest2 = Meta.Table.FindByName("EntityTest2");

            ///<summary>金额</summary>
            public static readonly Field Money = Meta.Table.FindByName("Money");

            ///<summary>文件</summary>
            public static readonly Field File = Meta.Table.FindByName("File");

            ///<summary>备注一</summary>
            public static readonly Field Remark = Meta.Table.FindByName("Remark");

            ///<summary>备注二</summary>
            public static readonly Field Remark2 = Meta.Table.FindByName("Remark2");

            ///<summary>说明</summary>
            public static readonly Field Description = Meta.Table.FindByName("Description");
        }
        #endregion
    }

    /// <summary>实体测试接口</summary>
    public partial interface IEntityTest
    {
        #region 属性
        /// <summary>主键一</summary>
        Guid Guid { get; set; }

        /// <summary>主键二</summary>
        String Guid2 { get; set; }

        /// <summary>自增编号</summary>
        SByte ID { get; set; }

        /// <summary>编号二</summary>
        Int16 ID2 { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>密码</summary>
        String Password { get; set; }

        /// <summary>显示名</summary>
        String DisplayName { get; set; }

        /// <summary>启用</summary>
        Boolean IsEnable { get; set; }

        /// <summary>登录次数</summary>
        Int64 Logins { get; set; }

        /// <summary>最后登陆</summary>
        DateTime LastLogin { get; set; }

        /// <summary>开始日期</summary>
        DateTime StartDate { get; set; }

        /// <summary>结束时间</summary>
        DateTime EndTime { get; set; }

        /// <summary>总数</summary>
        Decimal Total { get; set; }

        /// <summary>百分比</summary>
        Double Item2 { get; set; }

        /// <summary>实数</summary>
        Single EntityTest2 { get; set; }

        /// <summary>金额</summary>
        Decimal Money { get; set; }

        /// <summary>文件</summary>
        Byte[] File { get; set; }

        /// <summary>备注一</summary>
        String Remark { get; set; }

        /// <summary>备注二</summary>
        String Remark2 { get; set; }

        /// <summary>说明</summary>
        String Description { get; set; }
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