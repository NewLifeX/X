/*
 * XCoder v4.3.2011.0920
 * 作者：nnhy/NEWLIFE
 * 时间：2011-09-28 13:08:56
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
namespace NewLife.YWS.Entities
{
    /// <summary>交易记录</summary>
    [Serializable]
    [DataObject]
    [Description("交易记录")]
    [BindIndex("IX_Record_1", true, "MachineID")]
    [BindIndex("IX_Record_2", false, "CustomerID")]
    [BindIndex("PK__Record__3214EC2707020F21", true, "ID")]
    [BindRelation("CustomerID", false, "Customer", "ID")]
    [BindRelation("MachineID", true, "Machine", "ID")]
    [BindTable("Record", Description = "交易记录", ConnName = "YWS", DbType = DatabaseType.SqlServer)]
    public partial class Record : IRecord
    
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

        private Int32 _CustomerID;
        /// <summary>客户ID</summary>
        [DisplayName("客户ID")]
        [Description("客户ID")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "CustomerID", "客户ID", null, "int", 10, 0, false)]
        public Int32 CustomerID
        {
            get { return _CustomerID; }
            set { if (OnPropertyChanging("CustomerID", value)) { _CustomerID = value; OnPropertyChanged("CustomerID"); } }
        }

        private Int32 _MachineID;
        /// <summary>机器ID</summary>
        [DisplayName("机器ID")]
        [Description("机器ID")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "MachineID", "机器ID", null, "int", 10, 0, false)]
        public Int32 MachineID
        {
            get { return _MachineID; }
            set { if (OnPropertyChanging("MachineID", value)) { _MachineID = value; OnPropertyChanged("MachineID"); } }
        }

        private String _Transactor;
        /// <summary>经手人</summary>
        [DisplayName("经手人")]
        [Description("经手人")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "Transactor", "经手人", null, "nvarchar(50)", 0, 0, true)]
        public String Transactor
        {
            get { return _Transactor; }
            set { if (OnPropertyChanging("Transactor", value)) { _Transactor = value; OnPropertyChanged("Transactor"); } }
        }

        private DateTime _LeaveTime;
        /// <summary>出厂日期</summary>
        [DisplayName("出厂日期")]
        [Description("出厂日期")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(6, "LeaveTime", "出厂日期", null, "datetime", 3, 0, false)]
        public DateTime LeaveTime
        {
            get { return _LeaveTime; }
            set { if (OnPropertyChanging("LeaveTime", value)) { _LeaveTime = value; OnPropertyChanged("LeaveTime"); } }
        }

        private String _OutlineSize;
        /// <summary>机器外形尺寸</summary>
        [DisplayName("机器外形尺寸")]
        [Description("机器外形尺寸")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(7, "OutlineSize", "机器外形尺寸", null, "nvarchar(10)", 0, 0, true)]
        public String OutlineSize
        {
            get { return _OutlineSize; }
            set { if (OnPropertyChanging("OutlineSize", value)) { _OutlineSize = value; OnPropertyChanged("OutlineSize"); } }
        }

        private String _Attachment;
        /// <summary>附送配件</summary>
        [DisplayName("附送配件")]
        [Description("附送配件")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(8, "Attachment", "附送配件", null, "nvarchar(50)", 0, 0, true)]
        public String Attachment
        {
            get { return _Attachment; }
            set { if (OnPropertyChanging("Attachment", value)) { _Attachment = value; OnPropertyChanged("Attachment"); } }
        }

        private String _Type;
        /// <summary>点胶阀门类型</summary>
        [DisplayName("点胶阀门类型")]
        [Description("点胶阀门类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(9, "Type", "点胶阀门类型", null, "nvarchar(50)", 0, 0, true)]
        public String Type
        {
            get { return _Type; }
            set { if (OnPropertyChanging("Type", value)) { _Type = value; OnPropertyChanged("Type"); } }
        }

        private String _Model;
        /// <summary>混合管型号</summary>
        [DisplayName("混合管型号")]
        [Description("混合管型号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(10, "Model", "混合管型号", null, "nvarchar(50)", 0, 0, true)]
        public String Model
        {
            get { return _Model; }
            set { if (OnPropertyChanging("Model", value)) { _Model = value; OnPropertyChanged("Model"); } }
        }

        private String _VacuumpumpSpec;
        /// <summary>真空泵规格</summary>
        [DisplayName("真空泵规格")]
        [Description("真空泵规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(11, "VacuumpumpSpec", "真空泵规格", null, "nvarchar(50)", 0, 0, true)]
        public String VacuumpumpSpec
        {
            get { return _VacuumpumpSpec; }
            set { if (OnPropertyChanging("VacuumpumpSpec", value)) { _VacuumpumpSpec = value; OnPropertyChanged("VacuumpumpSpec"); } }
        }

        private String _Kind;
        /// <summary>数据显示屏种类</summary>
        [DisplayName("数据显示屏种类")]
        [Description("数据显示屏种类")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(12, "Kind", "数据显示屏种类", null, "nvarchar(50)", 0, 0, true)]
        public String Kind
        {
            get { return _Kind; }
            set { if (OnPropertyChanging("Kind", value)) { _Kind = value; OnPropertyChanged("Kind"); } }
        }

        private String _Groupings;
        /// <summary>计量泵组别</summary>
        [DisplayName("计量泵组别")]
        [Description("计量泵组别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(13, "Groupings", "计量泵组别", null, "nvarchar(50)", 0, 0, true)]
        public String Groupings
        {
            get { return _Groupings; }
            set { if (OnPropertyChanging("Groupings", value)) { _Groupings = value; OnPropertyChanged("Groupings"); } }
        }

        private Double _Size;
        /// <summary>计量泵尺寸</summary>
        [DisplayName("计量泵尺寸")]
        [Description("计量泵尺寸")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(14, "Size", "计量泵尺寸", null, "float", 53, 0, false)]
        public Double Size
        {
            get { return _Size; }
            set { if (OnPropertyChanging("Size", value)) { _Size = value; OnPropertyChanged("Size"); } }
        }

        private String _MeteringpumpSpec;
        /// <summary>计量泵密封件规格</summary>
        [DisplayName("计量泵密封件规格")]
        [Description("计量泵密封件规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(15, "MeteringpumpSpec", "计量泵密封件规格", null, "nvarchar(50)", 0, 0, true)]
        public String MeteringpumpSpec
        {
            get { return _MeteringpumpSpec; }
            set { if (OnPropertyChanging("MeteringpumpSpec", value)) { _MeteringpumpSpec = value; OnPropertyChanged("MeteringpumpSpec"); } }
        }

        private Double _PresSize;
        /// <summary>压力桶大小</summary>
        [DisplayName("压力桶大小")]
        [Description("压力桶大小")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(16, "PresSize", "压力桶大小", null, "float", 53, 0, false)]
        public Double PresSize
        {
            get { return _PresSize; }
            set { if (OnPropertyChanging("PresSize", value)) { _PresSize = value; OnPropertyChanged("PresSize"); } }
        }

        private String _SupplypipeSpec;
        /// <summary>进料管规格</summary>
        [DisplayName("进料管规格")]
        [Description("进料管规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(17, "SupplypipeSpec", "进料管规格", null, "nvarchar(50)", 0, 0, true)]
        public String SupplypipeSpec
        {
            get { return _SupplypipeSpec; }
            set { if (OnPropertyChanging("SupplypipeSpec", value)) { _SupplypipeSpec = value; OnPropertyChanged("SupplypipeSpec"); } }
        }

        private String _DischargeSpec;
        /// <summary>出料管规格</summary>
        [DisplayName("出料管规格")]
        [Description("出料管规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(18, "DischargeSpec", "出料管规格", null, "nvarchar(50)", 0, 0, true)]
        public String DischargeSpec
        {
            get { return _DischargeSpec; }
            set { if (OnPropertyChanging("DischargeSpec", value)) { _DischargeSpec = value; OnPropertyChanged("DischargeSpec"); } }
        }

        private String _GroupingsB;
        /// <summary>B料计量泵组别</summary>
        [DisplayName("B料计量泵组别")]
        [Description("B料计量泵组别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(19, "GroupingsB", "B料计量泵组别", null, "nvarchar(50)", 0, 0, true)]
        public String GroupingsB
        {
            get { return _GroupingsB; }
            set { if (OnPropertyChanging("GroupingsB", value)) { _GroupingsB = value; OnPropertyChanged("GroupingsB"); } }
        }

        private Double _SizeB;
        /// <summary>B料计量泵尺寸</summary>
        [DisplayName("B料计量泵尺寸")]
        [Description("B料计量泵尺寸")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(20, "SizeB", "B料计量泵尺寸", null, "float", 53, 0, false)]
        public Double SizeB
        {
            get { return _SizeB; }
            set { if (OnPropertyChanging("SizeB", value)) { _SizeB = value; OnPropertyChanged("SizeB"); } }
        }

        private String _MeteringpumpSpecB;
        /// <summary>B料计量泵密封件规格</summary>
        [DisplayName("B料计量泵密封件规格")]
        [Description("B料计量泵密封件规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(21, "MeteringpumpSpecB", "B料计量泵密封件规格", null, "nvarchar(50)", 0, 0, true)]
        public String MeteringpumpSpecB
        {
            get { return _MeteringpumpSpecB; }
            set { if (OnPropertyChanging("MeteringpumpSpecB", value)) { _MeteringpumpSpecB = value; OnPropertyChanged("MeteringpumpSpecB"); } }
        }

        private Double _PresSizeB;
        /// <summary>B料压力桶大小</summary>
        [DisplayName("B料压力桶大小")]
        [Description("B料压力桶大小")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(22, "PresSizeB", "B料压力桶大小", null, "float", 53, 0, false)]
        public Double PresSizeB
        {
            get { return _PresSizeB; }
            set { if (OnPropertyChanging("PresSizeB", value)) { _PresSizeB = value; OnPropertyChanged("PresSizeB"); } }
        }

        private String _SupplypipeSpecB;
        /// <summary>B料进料管规格</summary>
        [DisplayName("B料进料管规格")]
        [Description("B料进料管规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(23, "SupplypipeSpecB", "B料进料管规格", null, "nvarchar(50)", 0, 0, true)]
        public String SupplypipeSpecB
        {
            get { return _SupplypipeSpecB; }
            set { if (OnPropertyChanging("SupplypipeSpecB", value)) { _SupplypipeSpecB = value; OnPropertyChanged("SupplypipeSpecB"); } }
        }

        private String _DischargeSpecB;
        /// <summary>B料出料管规格</summary>
        [DisplayName("B料出料管规格")]
        [Description("B料出料管规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(24, "DischargeSpecB", "B料出料管规格", null, "nvarchar(50)", 0, 0, true)]
        public String DischargeSpecB
        {
            get { return _DischargeSpecB; }
            set { if (OnPropertyChanging("DischargeSpecB", value)) { _DischargeSpecB = value; OnPropertyChanged("DischargeSpecB"); } }
        }

        private String _Pic;
        /// <summary></summary>
        [DisplayName("")]
        [Description("")]
        [DataObjectField(false, false, true, 100)]
        [BindColumn(25, "Pic", "", null, "nvarchar(100)", 0, 0, true)]
        public String Pic
        {
            get { return _Pic; }
            set { if (OnPropertyChanging("Pic", value)) { _Pic = value; OnPropertyChanged("Pic"); } }
        }

        private DateTime _AddTime;
        /// <summary>添加时间</summary>
        [DisplayName("添加时间")]
        [Description("添加时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(26, "AddTime", "添加时间", null, "datetime", 3, 0, false)]
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
        [BindColumn(27, "Remark", "备注", null, "nvarchar(100)", 0, 0, true)]
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
                    case "CustomerID" : return _CustomerID;
                    case "MachineID" : return _MachineID;
                    case "Transactor" : return _Transactor;
                    case "LeaveTime" : return _LeaveTime;
                    case "OutlineSize" : return _OutlineSize;
                    case "Attachment" : return _Attachment;
                    case "Type" : return _Type;
                    case "Model" : return _Model;
                    case "VacuumpumpSpec" : return _VacuumpumpSpec;
                    case "Kind" : return _Kind;
                    case "Groupings" : return _Groupings;
                    case "Size" : return _Size;
                    case "MeteringpumpSpec" : return _MeteringpumpSpec;
                    case "PresSize" : return _PresSize;
                    case "SupplypipeSpec" : return _SupplypipeSpec;
                    case "DischargeSpec" : return _DischargeSpec;
                    case "GroupingsB" : return _GroupingsB;
                    case "SizeB" : return _SizeB;
                    case "MeteringpumpSpecB" : return _MeteringpumpSpecB;
                    case "PresSizeB" : return _PresSizeB;
                    case "SupplypipeSpecB" : return _SupplypipeSpecB;
                    case "DischargeSpecB" : return _DischargeSpecB;
                    case "Pic" : return _Pic;
                    case "AddTime" : return _AddTime;
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
                    case "CustomerID" : _CustomerID = Convert.ToInt32(value); break;
                    case "MachineID" : _MachineID = Convert.ToInt32(value); break;
                    case "Transactor" : _Transactor = Convert.ToString(value); break;
                    case "LeaveTime" : _LeaveTime = Convert.ToDateTime(value); break;
                    case "OutlineSize" : _OutlineSize = Convert.ToString(value); break;
                    case "Attachment" : _Attachment = Convert.ToString(value); break;
                    case "Type" : _Type = Convert.ToString(value); break;
                    case "Model" : _Model = Convert.ToString(value); break;
                    case "VacuumpumpSpec" : _VacuumpumpSpec = Convert.ToString(value); break;
                    case "Kind" : _Kind = Convert.ToString(value); break;
                    case "Groupings" : _Groupings = Convert.ToString(value); break;
                    case "Size" : _Size = Convert.ToDouble(value); break;
                    case "MeteringpumpSpec" : _MeteringpumpSpec = Convert.ToString(value); break;
                    case "PresSize" : _PresSize = Convert.ToDouble(value); break;
                    case "SupplypipeSpec" : _SupplypipeSpec = Convert.ToString(value); break;
                    case "DischargeSpec" : _DischargeSpec = Convert.ToString(value); break;
                    case "GroupingsB" : _GroupingsB = Convert.ToString(value); break;
                    case "SizeB" : _SizeB = Convert.ToDouble(value); break;
                    case "MeteringpumpSpecB" : _MeteringpumpSpecB = Convert.ToString(value); break;
                    case "PresSizeB" : _PresSizeB = Convert.ToDouble(value); break;
                    case "SupplypipeSpecB" : _SupplypipeSpecB = Convert.ToString(value); break;
                    case "DischargeSpecB" : _DischargeSpecB = Convert.ToString(value); break;
                    case "Pic" : _Pic = Convert.ToString(value); break;
                    case "AddTime" : _AddTime = Convert.ToDateTime(value); break;
                    case "Remark" : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得交易记录字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>客户ID</summary>
            public static readonly Field CustomerID = Meta.Table.FindByName("CustomerID");

            ///<summary>机器ID</summary>
            public static readonly Field MachineID = Meta.Table.FindByName("MachineID");

            ///<summary>经手人</summary>
            public static readonly Field Transactor = Meta.Table.FindByName("Transactor");

            ///<summary>出厂日期</summary>
            public static readonly Field LeaveTime = Meta.Table.FindByName("LeaveTime");

            ///<summary>机器外形尺寸</summary>
            public static readonly Field OutlineSize = Meta.Table.FindByName("OutlineSize");

            ///<summary>附送配件</summary>
            public static readonly Field Attachment = Meta.Table.FindByName("Attachment");

            ///<summary>点胶阀门类型</summary>
            public static readonly Field Type = Meta.Table.FindByName("Type");

            ///<summary>混合管型号</summary>
            public static readonly Field Model = Meta.Table.FindByName("Model");

            ///<summary>真空泵规格</summary>
            public static readonly Field VacuumpumpSpec = Meta.Table.FindByName("VacuumpumpSpec");

            ///<summary>数据显示屏种类</summary>
            public static readonly Field Kind = Meta.Table.FindByName("Kind");

            ///<summary>计量泵组别</summary>
            public static readonly Field Groupings = Meta.Table.FindByName("Groupings");

            ///<summary>计量泵尺寸</summary>
            public static readonly Field Size = Meta.Table.FindByName("Size");

            ///<summary>计量泵密封件规格</summary>
            public static readonly Field MeteringpumpSpec = Meta.Table.FindByName("MeteringpumpSpec");

            ///<summary>压力桶大小</summary>
            public static readonly Field PresSize = Meta.Table.FindByName("PresSize");

            ///<summary>进料管规格</summary>
            public static readonly Field SupplypipeSpec = Meta.Table.FindByName("SupplypipeSpec");

            ///<summary>出料管规格</summary>
            public static readonly Field DischargeSpec = Meta.Table.FindByName("DischargeSpec");

            ///<summary>B料计量泵组别</summary>
            public static readonly Field GroupingsB = Meta.Table.FindByName("GroupingsB");

            ///<summary>B料计量泵尺寸</summary>
            public static readonly Field SizeB = Meta.Table.FindByName("SizeB");

            ///<summary>B料计量泵密封件规格</summary>
            public static readonly Field MeteringpumpSpecB = Meta.Table.FindByName("MeteringpumpSpecB");

            ///<summary>B料压力桶大小</summary>
            public static readonly Field PresSizeB = Meta.Table.FindByName("PresSizeB");

            ///<summary>B料进料管规格</summary>
            public static readonly Field SupplypipeSpecB = Meta.Table.FindByName("SupplypipeSpecB");

            ///<summary>B料出料管规格</summary>
            public static readonly Field DischargeSpecB = Meta.Table.FindByName("DischargeSpecB");

            ///<summary></summary>
            public static readonly Field Pic = Meta.Table.FindByName("Pic");

            ///<summary>添加时间</summary>
            public static readonly Field AddTime = Meta.Table.FindByName("AddTime");

            ///<summary>备注</summary>
            public static readonly Field Remark = Meta.Table.FindByName("Remark");
        }
        #endregion
    }

    /// <summary>交易记录接口</summary>
    public partial interface IRecord
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>客户ID</summary>
        Int32 CustomerID { get; set; }

        /// <summary>机器ID</summary>
        Int32 MachineID { get; set; }

        /// <summary>经手人</summary>
        String Transactor { get; set; }

        /// <summary>出厂日期</summary>
        DateTime LeaveTime { get; set; }

        /// <summary>机器外形尺寸</summary>
        String OutlineSize { get; set; }

        /// <summary>附送配件</summary>
        String Attachment { get; set; }

        /// <summary>点胶阀门类型</summary>
        String Type { get; set; }

        /// <summary>混合管型号</summary>
        String Model { get; set; }

        /// <summary>真空泵规格</summary>
        String VacuumpumpSpec { get; set; }

        /// <summary>数据显示屏种类</summary>
        String Kind { get; set; }

        /// <summary>计量泵组别</summary>
        String Groupings { get; set; }

        /// <summary>计量泵尺寸</summary>
        Double Size { get; set; }

        /// <summary>计量泵密封件规格</summary>
        String MeteringpumpSpec { get; set; }

        /// <summary>压力桶大小</summary>
        Double PresSize { get; set; }

        /// <summary>进料管规格</summary>
        String SupplypipeSpec { get; set; }

        /// <summary>出料管规格</summary>
        String DischargeSpec { get; set; }

        /// <summary>B料计量泵组别</summary>
        String GroupingsB { get; set; }

        /// <summary>B料计量泵尺寸</summary>
        Double SizeB { get; set; }

        /// <summary>B料计量泵密封件规格</summary>
        String MeteringpumpSpecB { get; set; }

        /// <summary>B料压力桶大小</summary>
        Double PresSizeB { get; set; }

        /// <summary>B料进料管规格</summary>
        String SupplypipeSpecB { get; set; }

        /// <summary>B料出料管规格</summary>
        String DischargeSpecB { get; set; }

        /// <summary></summary>
        String Pic { get; set; }

        /// <summary>添加时间</summary>
        DateTime AddTime { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}
#pragma warning restore 3021