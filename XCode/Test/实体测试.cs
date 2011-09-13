/*
 * XCoder v4.2.2011.0911
 * 作者：nnhy/X
 * 时间：2011-09-13 07:18:02
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
    [BindIndex("PK_EntityTest", true, "Guid,guid2")]
    [BindTable("EntityTest", Description = "实体测试", ConnName = "XCodeTest", DbType = DatabaseType.SqlServer)]
    public partial class EntityTest : IEntityTest
    {
        #region 属性
        private Guid _Guid;
        /// <summary>主键一</summary>
        [DisplayName("主键一")]
        [Description("主键一")]
        [DataObjectField(true, false, false, 16)]
        [BindColumn(1, "Guid", "主键一", "newid()", "uniqueidentifier", 0, 0, false)]
        public Guid Guid
        {
            get { return _Guid; }
            set { if (OnPropertyChanging("Guid", value)) { _Guid = value; OnPropertyChanged("Guid"); } }
        }

        private String _guid2;
        /// <summary>主键二</summary>
        [DisplayName("主键二")]
        [Description("主键二")]
        [DataObjectField(true, false, false, 16)]
        [BindColumn(2, "guid2", "主键二", "'NEWID()'", "char(16)", 0, 0, false)]
        public String guid2
        {
            get { return _guid2; }
            set { if (OnPropertyChanging("guid2", value)) { _guid2 = value; OnPropertyChanged("guid2"); } }
        }

        private SByte _ID;
        /// <summary>自增编号</summary>
        [DisplayName("自增编号")]
        [Description("自增编号")]
        [DataObjectField(false, true, false, 3)]
        [BindColumn(3, "ID", "自增编号", "", "tinyint", 3, 0, false)]
        [CLSCompliant(false)]
        public SByte ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(4, "Name", "名称", "'admin'", "varchar(50)", 0, 0, false)]
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
        [BindColumn(5, "Password", "密码", "N'密‘admin’码'", "nchar(32)", 0, 0, true)]
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
        [BindColumn(6, "DisplayName", "显示名", "", "nvarchar(50)", 0, 0, true)]
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
        [BindColumn(7, "IsEnable", "启用", "", "bit", 0, 0, false)]
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
        [BindColumn(8, "Logins", "登录次数", "-999999999999999999.", "bigint", 19, 0, false)]
        public Int64 Logins
        {
            get { return _Logins; }
            set { if (OnPropertyChanging("Logins", value)) { _Logins = value; OnPropertyChanged("Logins"); } }
        }

        private DateTime _LastLogin;
        /// <summary>最后登陆</summary>
        [DisplayName("最后登陆")]
        [Description("最后登陆")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(9, "LastLogin", "最后登陆", "getdate()", "datetime", 3, 0, false)]
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
        [BindColumn(10, "StartDate", "开始日期", "getdate()", "date", 0, 0, false)]
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
        [BindColumn(11, "EndTime", "结束时间", "getdate()", "smalldatetime", 0, 0, false)]
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
        [BindColumn(12, "Total", "总数", "pi()", "decimal(18,0)", 18, 0, false)]
        public Decimal Total
        {
            get { return _Total; }
            set { if (OnPropertyChanging("Total", value)) { _Total = value; OnPropertyChanged("Total"); } }
        }

        private Double _persent;
        /// <summary>百分比</summary>
        [DisplayName("百分比")]
        [Description("百分比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(13, "persent", "百分比", "sqrt((2))/(10)", "float", 53, 0, false)]
        public Double persent
        {
            get { return _persent; }
            set { if (OnPropertyChanging("persent", value)) { _persent = value; OnPropertyChanged("persent"); } }
        }

        private Single _real;
        /// <summary>实数</summary>
        [DisplayName("实数")]
        [Description("实数")]
        [DataObjectField(false, false, true, 24)]
        [BindColumn(14, "real", "实数", "pi()", "real", 24, 0, false)]
        public Single real
        {
            get { return _real; }
            set { if (OnPropertyChanging("real", value)) { _real = value; OnPropertyChanged("real"); } }
        }

        private Decimal _money;
        /// <summary>金额</summary>
        [DisplayName("金额")]
        [Description("金额")]
        [DataObjectField(false, false, true, 19)]
        [BindColumn(15, "money", "金额", "", "money", 19, 4, false)]
        public Decimal money
        {
            get { return _money; }
            set { if (OnPropertyChanging("money", value)) { _money = value; OnPropertyChanged("money"); } }
        }

        private Byte[] _file;
        /// <summary>文件</summary>
        [DisplayName("文件")]
        [Description("文件")]
        [DataObjectField(false, false, true, 2147483647)]
        [BindColumn(16, "file", "文件", "", "image", 0, 0, false)]
        public Byte[] file
        {
            get { return _file; }
            set { if (OnPropertyChanging("file", value)) { _file = value; OnPropertyChanged("file"); } }
        }

        private String _remark;
        /// <summary>备注一</summary>
        [DisplayName("备注一")]
        [Description("备注一")]
        [DataObjectField(false, false, true, 2147483647)]
        [BindColumn(17, "remark", "备注一", "'备注'", "text", 0, 0, false)]
        public String remark
        {
            get { return _remark; }
            set { if (OnPropertyChanging("remark", value)) { _remark = value; OnPropertyChanged("remark"); } }
        }

        private String _remark2;
        /// <summary>备注二</summary>
        [DisplayName("备注二")]
        [Description("备注二")]
        [DataObjectField(false, false, true, -1)]
        [BindColumn(18, "remark2", "备注二", "", "nvarchar(-1)", 0, 0, true)]
        public String remark2
        {
            get { return _remark2; }
            set { if (OnPropertyChanging("remark2", value)) { _remark2 = value; OnPropertyChanged("remark2"); } }
        }

        private String _Description;
        /// <summary>说明</summary>
        [DisplayName("说明")]
        [Description("说明")]
        [DataObjectField(false, false, true, 1073741823)]
        [BindColumn(19, "Description", "说明", "", "ntext", 0, 0, true)]
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
                    case "Guid": return _Guid;
                    case "guid2": return _guid2;
                    case "ID": return _ID;
                    case "Name": return _Name;
                    case "Password": return _Password;
                    case "DisplayName": return _DisplayName;
                    case "IsEnable": return _IsEnable;
                    case "Logins": return _Logins;
                    case "LastLogin": return _LastLogin;
                    case "StartDate": return _StartDate;
                    case "EndTime": return _EndTime;
                    case "Total": return _Total;
                    case "persent": return _persent;
                    case "real": return _real;
                    case "money": return _money;
                    case "file": return _file;
                    case "remark": return _remark;
                    case "remark2": return _remark2;
                    case "Description": return _Description;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Guid": _Guid = (Guid)value; break;
                    case "guid2": _guid2 = Convert.ToString(value); break;
                    case "ID": _ID = Convert.ToSByte(value); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "Password": _Password = Convert.ToString(value); break;
                    case "DisplayName": _DisplayName = Convert.ToString(value); break;
                    case "IsEnable": _IsEnable = Convert.ToBoolean(value); break;
                    case "Logins": _Logins = Convert.ToInt64(value); break;
                    case "LastLogin": _LastLogin = Convert.ToDateTime(value); break;
                    case "StartDate": _StartDate = Convert.ToDateTime(value); break;
                    case "EndTime": _EndTime = Convert.ToDateTime(value); break;
                    case "Total": _Total = Convert.ToDecimal(value); break;
                    case "persent": _persent = Convert.ToDouble(value); break;
                    case "real": _real = Convert.ToSingle(value); break;
                    case "money": _money = Convert.ToDecimal(value); break;
                    case "file": _file = (Byte[])value; break;
                    case "remark": _remark = Convert.ToString(value); break;
                    case "remark2": _remark2 = Convert.ToString(value); break;
                    case "Description": _Description = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得实体测试字段信息的快捷方式</summary>
        [CLSCompliant(false)]
        public class _
        {
            ///<summary>主键一</summary>
            public static readonly FieldItem Guid = Meta.Table.FindByName("Guid");

            ///<summary>主键二</summary>
            public static readonly FieldItem guid2 = Meta.Table.FindByName("guid2");

            ///<summary>自增编号</summary>
            public static readonly FieldItem ID = Meta.Table.FindByName("ID");

            ///<summary>名称</summary>
            public static readonly FieldItem Name = Meta.Table.FindByName("Name");

            ///<summary>密码</summary>
            public static readonly FieldItem Password = Meta.Table.FindByName("Password");

            ///<summary>显示名</summary>
            public static readonly FieldItem DisplayName = Meta.Table.FindByName("DisplayName");

            ///<summary>启用</summary>
            public static readonly FieldItem IsEnable = Meta.Table.FindByName("IsEnable");

            ///<summary>登录次数</summary>
            public static readonly FieldItem Logins = Meta.Table.FindByName("Logins");

            ///<summary>最后登陆</summary>
            public static readonly FieldItem LastLogin = Meta.Table.FindByName("LastLogin");

            ///<summary>开始日期</summary>
            public static readonly FieldItem StartDate = Meta.Table.FindByName("StartDate");

            ///<summary>结束时间</summary>
            public static readonly FieldItem EndTime = Meta.Table.FindByName("EndTime");

            ///<summary>总数</summary>
            public static readonly FieldItem Total = Meta.Table.FindByName("Total");

            ///<summary>百分比</summary>
            public static readonly FieldItem persent = Meta.Table.FindByName("persent");

            ///<summary>实数</summary>
            public static readonly FieldItem real = Meta.Table.FindByName("real");

            ///<summary>金额</summary>
            public static readonly FieldItem money = Meta.Table.FindByName("money");

            ///<summary>文件</summary>
            public static readonly FieldItem file = Meta.Table.FindByName("file");

            ///<summary>备注一</summary>
            public static readonly FieldItem remark = Meta.Table.FindByName("remark");

            ///<summary>备注二</summary>
            public static readonly FieldItem remark2 = Meta.Table.FindByName("remark2");

            ///<summary>说明</summary>
            public static readonly FieldItem Description = Meta.Table.FindByName("Description");
        }
        #endregion
    }

    /// <summary>实体测试接口</summary>
    [CLSCompliant(false)]
    public interface IEntityTest
    {
        #region 属性
        /// <summary>主键一</summary>
        Guid Guid { get; set; }

        /// <summary>主键二</summary>
        String guid2 { get; set; }

        /// <summary>自增编号</summary>
        SByte ID { get; set; }

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
        Double persent { get; set; }

        /// <summary>实数</summary>
        Single real { get; set; }

        /// <summary>金额</summary>
        Decimal money { get; set; }

        /// <summary>文件</summary>
        Byte[] file { get; set; }

        /// <summary>备注一</summary>
        String remark { get; set; }

        /// <summary>备注二</summary>
        String remark2 { get; set; }

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