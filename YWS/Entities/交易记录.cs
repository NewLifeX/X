﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

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
        public virtual Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } }
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
            set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } }
        }

        private Int32 _CustomerID;
        /// <summary>客户ID</summary>
        [DisplayName("客户ID")]
        [Description("客户ID")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "CustomerID", "客户ID", null, "int", 10, 0, false)]
        public virtual Int32 CustomerID
        {
            get { return _CustomerID; }
            set { if (OnPropertyChanging(__.CustomerID, value)) { _CustomerID = value; OnPropertyChanged(__.CustomerID); } }
        }

        private Int32 _MachineID;
        /// <summary>机器ID</summary>
        [DisplayName("机器ID")]
        [Description("机器ID")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "MachineID", "机器ID", null, "int", 10, 0, false)]
        public virtual Int32 MachineID
        {
            get { return _MachineID; }
            set { if (OnPropertyChanging(__.MachineID, value)) { _MachineID = value; OnPropertyChanged(__.MachineID); } }
        }

        private String _Transactor;
        /// <summary>经手人</summary>
        [DisplayName("经手人")]
        [Description("经手人")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "Transactor", "经手人", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Transactor
        {
            get { return _Transactor; }
            set { if (OnPropertyChanging(__.Transactor, value)) { _Transactor = value; OnPropertyChanged(__.Transactor); } }
        }

        private DateTime _LeaveTime;
        /// <summary>出厂日期</summary>
        [DisplayName("出厂日期")]
        [Description("出厂日期")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(6, "LeaveTime", "出厂日期", null, "datetime", 3, 0, false)]
        public virtual DateTime LeaveTime
        {
            get { return _LeaveTime; }
            set { if (OnPropertyChanging(__.LeaveTime, value)) { _LeaveTime = value; OnPropertyChanged(__.LeaveTime); } }
        }

        private String _OutlineSize;
        /// <summary>机器外形尺寸</summary>
        [DisplayName("机器外形尺寸")]
        [Description("机器外形尺寸")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(7, "OutlineSize", "机器外形尺寸", null, "nvarchar(10)", 0, 0, true)]
        public virtual String OutlineSize
        {
            get { return _OutlineSize; }
            set { if (OnPropertyChanging(__.OutlineSize, value)) { _OutlineSize = value; OnPropertyChanged(__.OutlineSize); } }
        }

        private String _Attachment;
        /// <summary>附送配件</summary>
        [DisplayName("附送配件")]
        [Description("附送配件")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(8, "Attachment", "附送配件", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Attachment
        {
            get { return _Attachment; }
            set { if (OnPropertyChanging(__.Attachment, value)) { _Attachment = value; OnPropertyChanged(__.Attachment); } }
        }

        private String _Type;
        /// <summary>点胶阀门类型</summary>
        [DisplayName("点胶阀门类型")]
        [Description("点胶阀门类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(9, "Type", "点胶阀门类型", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Type
        {
            get { return _Type; }
            set { if (OnPropertyChanging(__.Type, value)) { _Type = value; OnPropertyChanged(__.Type); } }
        }

        private String _Model;
        /// <summary>混合管型号</summary>
        [DisplayName("混合管型号")]
        [Description("混合管型号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(10, "Model", "混合管型号", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Model
        {
            get { return _Model; }
            set { if (OnPropertyChanging(__.Model, value)) { _Model = value; OnPropertyChanged(__.Model); } }
        }

        private String _VacuumpumpSpec;
        /// <summary>真空泵规格</summary>
        [DisplayName("真空泵规格")]
        [Description("真空泵规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(11, "VacuumpumpSpec", "真空泵规格", null, "nvarchar(50)", 0, 0, true)]
        public virtual String VacuumpumpSpec
        {
            get { return _VacuumpumpSpec; }
            set { if (OnPropertyChanging(__.VacuumpumpSpec, value)) { _VacuumpumpSpec = value; OnPropertyChanged(__.VacuumpumpSpec); } }
        }

        private String _Kind;
        /// <summary>数据显示屏种类</summary>
        [DisplayName("数据显示屏种类")]
        [Description("数据显示屏种类")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(12, "Kind", "数据显示屏种类", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Kind
        {
            get { return _Kind; }
            set { if (OnPropertyChanging(__.Kind, value)) { _Kind = value; OnPropertyChanged(__.Kind); } }
        }

        private String _Groupings;
        /// <summary>计量泵组别</summary>
        [DisplayName("计量泵组别")]
        [Description("计量泵组别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(13, "Groupings", "计量泵组别", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Groupings
        {
            get { return _Groupings; }
            set { if (OnPropertyChanging(__.Groupings, value)) { _Groupings = value; OnPropertyChanged(__.Groupings); } }
        }

        private Double _Size;
        /// <summary>计量泵尺寸</summary>
        [DisplayName("计量泵尺寸")]
        [Description("计量泵尺寸")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(14, "Size", "计量泵尺寸", null, "float", 53, 0, false)]
        public virtual Double Size
        {
            get { return _Size; }
            set { if (OnPropertyChanging(__.Size, value)) { _Size = value; OnPropertyChanged(__.Size); } }
        }

        private String _MeteringpumpSpec;
        /// <summary>计量泵密封件规格</summary>
        [DisplayName("计量泵密封件规格")]
        [Description("计量泵密封件规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(15, "MeteringpumpSpec", "计量泵密封件规格", null, "nvarchar(50)", 0, 0, true)]
        public virtual String MeteringpumpSpec
        {
            get { return _MeteringpumpSpec; }
            set { if (OnPropertyChanging(__.MeteringpumpSpec, value)) { _MeteringpumpSpec = value; OnPropertyChanged(__.MeteringpumpSpec); } }
        }

        private Double _PresSize;
        /// <summary>压力桶大小</summary>
        [DisplayName("压力桶大小")]
        [Description("压力桶大小")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(16, "PresSize", "压力桶大小", null, "float", 53, 0, false)]
        public virtual Double PresSize
        {
            get { return _PresSize; }
            set { if (OnPropertyChanging(__.PresSize, value)) { _PresSize = value; OnPropertyChanged(__.PresSize); } }
        }

        private String _SupplypipeSpec;
        /// <summary>进料管规格</summary>
        [DisplayName("进料管规格")]
        [Description("进料管规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(17, "SupplypipeSpec", "进料管规格", null, "nvarchar(50)", 0, 0, true)]
        public virtual String SupplypipeSpec
        {
            get { return _SupplypipeSpec; }
            set { if (OnPropertyChanging(__.SupplypipeSpec, value)) { _SupplypipeSpec = value; OnPropertyChanged(__.SupplypipeSpec); } }
        }

        private String _DischargeSpec;
        /// <summary>出料管规格</summary>
        [DisplayName("出料管规格")]
        [Description("出料管规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(18, "DischargeSpec", "出料管规格", null, "nvarchar(50)", 0, 0, true)]
        public virtual String DischargeSpec
        {
            get { return _DischargeSpec; }
            set { if (OnPropertyChanging(__.DischargeSpec, value)) { _DischargeSpec = value; OnPropertyChanged(__.DischargeSpec); } }
        }

        private String _GroupingsB;
        /// <summary>B料计量泵组别</summary>
        [DisplayName("B料计量泵组别")]
        [Description("B料计量泵组别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(19, "GroupingsB", "B料计量泵组别", null, "nvarchar(50)", 0, 0, true)]
        public virtual String GroupingsB
        {
            get { return _GroupingsB; }
            set { if (OnPropertyChanging(__.GroupingsB, value)) { _GroupingsB = value; OnPropertyChanged(__.GroupingsB); } }
        }

        private Double _SizeB;
        /// <summary>B料计量泵尺寸</summary>
        [DisplayName("B料计量泵尺寸")]
        [Description("B料计量泵尺寸")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(20, "SizeB", "B料计量泵尺寸", null, "float", 53, 0, false)]
        public virtual Double SizeB
        {
            get { return _SizeB; }
            set { if (OnPropertyChanging(__.SizeB, value)) { _SizeB = value; OnPropertyChanged(__.SizeB); } }
        }

        private String _MeteringpumpSpecB;
        /// <summary>B料计量泵密封件规格</summary>
        [DisplayName("B料计量泵密封件规格")]
        [Description("B料计量泵密封件规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(21, "MeteringpumpSpecB", "B料计量泵密封件规格", null, "nvarchar(50)", 0, 0, true)]
        public virtual String MeteringpumpSpecB
        {
            get { return _MeteringpumpSpecB; }
            set { if (OnPropertyChanging(__.MeteringpumpSpecB, value)) { _MeteringpumpSpecB = value; OnPropertyChanged(__.MeteringpumpSpecB); } }
        }

        private Double _PresSizeB;
        /// <summary>B料压力桶大小</summary>
        [DisplayName("B料压力桶大小")]
        [Description("B料压力桶大小")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(22, "PresSizeB", "B料压力桶大小", null, "float", 53, 0, false)]
        public virtual Double PresSizeB
        {
            get { return _PresSizeB; }
            set { if (OnPropertyChanging(__.PresSizeB, value)) { _PresSizeB = value; OnPropertyChanged(__.PresSizeB); } }
        }

        private String _SupplypipeSpecB;
        /// <summary>B料进料管规格</summary>
        [DisplayName("B料进料管规格")]
        [Description("B料进料管规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(23, "SupplypipeSpecB", "B料进料管规格", null, "nvarchar(50)", 0, 0, true)]
        public virtual String SupplypipeSpecB
        {
            get { return _SupplypipeSpecB; }
            set { if (OnPropertyChanging(__.SupplypipeSpecB, value)) { _SupplypipeSpecB = value; OnPropertyChanged(__.SupplypipeSpecB); } }
        }

        private String _DischargeSpecB;
        /// <summary>B料出料管规格</summary>
        [DisplayName("B料出料管规格")]
        [Description("B料出料管规格")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(24, "DischargeSpecB", "B料出料管规格", null, "nvarchar(50)", 0, 0, true)]
        public virtual String DischargeSpecB
        {
            get { return _DischargeSpecB; }
            set { if (OnPropertyChanging(__.DischargeSpecB, value)) { _DischargeSpecB = value; OnPropertyChanged(__.DischargeSpecB); } }
        }

        private String _Pic;
        /// <summary>Pic</summary>
        [DisplayName("Pic")]
        [Description("Pic")]
        [DataObjectField(false, false, true, 100)]
        [BindColumn(25, "Pic", "Pic", null, "nvarchar(100)", 0, 0, true)]
        public virtual String Pic
        {
            get { return _Pic; }
            set { if (OnPropertyChanging(__.Pic, value)) { _Pic = value; OnPropertyChanged(__.Pic); } }
        }

        private DateTime _AddTime;
        /// <summary>添加时间</summary>
        [DisplayName("添加时间")]
        [Description("添加时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(26, "AddTime", "添加时间", null, "datetime", 3, 0, false)]
        public virtual DateTime AddTime
        {
            get { return _AddTime; }
            set { if (OnPropertyChanging(__.AddTime, value)) { _AddTime = value; OnPropertyChanged(__.AddTime); } }
        }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 100)]
        [BindColumn(27, "Remark", "备注", null, "nvarchar(100)", 0, 0, true)]
        public virtual String Remark
        {
            get { return _Remark; }
            set { if (OnPropertyChanging(__.Remark, value)) { _Remark = value; OnPropertyChanged(__.Remark); } }
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
                    case __.ID : return _ID;
                    case __.Name : return _Name;
                    case __.CustomerID : return _CustomerID;
                    case __.MachineID : return _MachineID;
                    case __.Transactor : return _Transactor;
                    case __.LeaveTime : return _LeaveTime;
                    case __.OutlineSize : return _OutlineSize;
                    case __.Attachment : return _Attachment;
                    case __.Type : return _Type;
                    case __.Model : return _Model;
                    case __.VacuumpumpSpec : return _VacuumpumpSpec;
                    case __.Kind : return _Kind;
                    case __.Groupings : return _Groupings;
                    case __.Size : return _Size;
                    case __.MeteringpumpSpec : return _MeteringpumpSpec;
                    case __.PresSize : return _PresSize;
                    case __.SupplypipeSpec : return _SupplypipeSpec;
                    case __.DischargeSpec : return _DischargeSpec;
                    case __.GroupingsB : return _GroupingsB;
                    case __.SizeB : return _SizeB;
                    case __.MeteringpumpSpecB : return _MeteringpumpSpecB;
                    case __.PresSizeB : return _PresSizeB;
                    case __.SupplypipeSpecB : return _SupplypipeSpecB;
                    case __.DischargeSpecB : return _DischargeSpecB;
                    case __.Pic : return _Pic;
                    case __.AddTime : return _AddTime;
                    case __.Remark : return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.CustomerID : _CustomerID = Convert.ToInt32(value); break;
                    case __.MachineID : _MachineID = Convert.ToInt32(value); break;
                    case __.Transactor : _Transactor = Convert.ToString(value); break;
                    case __.LeaveTime : _LeaveTime = Convert.ToDateTime(value); break;
                    case __.OutlineSize : _OutlineSize = Convert.ToString(value); break;
                    case __.Attachment : _Attachment = Convert.ToString(value); break;
                    case __.Type : _Type = Convert.ToString(value); break;
                    case __.Model : _Model = Convert.ToString(value); break;
                    case __.VacuumpumpSpec : _VacuumpumpSpec = Convert.ToString(value); break;
                    case __.Kind : _Kind = Convert.ToString(value); break;
                    case __.Groupings : _Groupings = Convert.ToString(value); break;
                    case __.Size : _Size = Convert.ToDouble(value); break;
                    case __.MeteringpumpSpec : _MeteringpumpSpec = Convert.ToString(value); break;
                    case __.PresSize : _PresSize = Convert.ToDouble(value); break;
                    case __.SupplypipeSpec : _SupplypipeSpec = Convert.ToString(value); break;
                    case __.DischargeSpec : _DischargeSpec = Convert.ToString(value); break;
                    case __.GroupingsB : _GroupingsB = Convert.ToString(value); break;
                    case __.SizeB : _SizeB = Convert.ToDouble(value); break;
                    case __.MeteringpumpSpecB : _MeteringpumpSpecB = Convert.ToString(value); break;
                    case __.PresSizeB : _PresSizeB = Convert.ToDouble(value); break;
                    case __.SupplypipeSpecB : _SupplypipeSpecB = Convert.ToString(value); break;
                    case __.DischargeSpecB : _DischargeSpecB = Convert.ToString(value); break;
                    case __.Pic : _Pic = Convert.ToString(value); break;
                    case __.AddTime : _AddTime = Convert.ToDateTime(value); break;
                    case __.Remark : _Remark = Convert.ToString(value); break;
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
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>客户ID</summary>
            public static readonly Field CustomerID = FindByName(__.CustomerID);

            ///<summary>机器ID</summary>
            public static readonly Field MachineID = FindByName(__.MachineID);

            ///<summary>经手人</summary>
            public static readonly Field Transactor = FindByName(__.Transactor);

            ///<summary>出厂日期</summary>
            public static readonly Field LeaveTime = FindByName(__.LeaveTime);

            ///<summary>机器外形尺寸</summary>
            public static readonly Field OutlineSize = FindByName(__.OutlineSize);

            ///<summary>附送配件</summary>
            public static readonly Field Attachment = FindByName(__.Attachment);

            ///<summary>点胶阀门类型</summary>
            public static readonly Field Type = FindByName(__.Type);

            ///<summary>混合管型号</summary>
            public static readonly Field Model = FindByName(__.Model);

            ///<summary>真空泵规格</summary>
            public static readonly Field VacuumpumpSpec = FindByName(__.VacuumpumpSpec);

            ///<summary>数据显示屏种类</summary>
            public static readonly Field Kind = FindByName(__.Kind);

            ///<summary>计量泵组别</summary>
            public static readonly Field Groupings = FindByName(__.Groupings);

            ///<summary>计量泵尺寸</summary>
            public static readonly Field Size = FindByName(__.Size);

            ///<summary>计量泵密封件规格</summary>
            public static readonly Field MeteringpumpSpec = FindByName(__.MeteringpumpSpec);

            ///<summary>压力桶大小</summary>
            public static readonly Field PresSize = FindByName(__.PresSize);

            ///<summary>进料管规格</summary>
            public static readonly Field SupplypipeSpec = FindByName(__.SupplypipeSpec);

            ///<summary>出料管规格</summary>
            public static readonly Field DischargeSpec = FindByName(__.DischargeSpec);

            ///<summary>B料计量泵组别</summary>
            public static readonly Field GroupingsB = FindByName(__.GroupingsB);

            ///<summary>B料计量泵尺寸</summary>
            public static readonly Field SizeB = FindByName(__.SizeB);

            ///<summary>B料计量泵密封件规格</summary>
            public static readonly Field MeteringpumpSpecB = FindByName(__.MeteringpumpSpecB);

            ///<summary>B料压力桶大小</summary>
            public static readonly Field PresSizeB = FindByName(__.PresSizeB);

            ///<summary>B料进料管规格</summary>
            public static readonly Field SupplypipeSpecB = FindByName(__.SupplypipeSpecB);

            ///<summary>B料出料管规格</summary>
            public static readonly Field DischargeSpecB = FindByName(__.DischargeSpecB);

            ///<summary>Pic</summary>
            public static readonly Field Pic = FindByName(__.Pic);

            ///<summary>添加时间</summary>
            public static readonly Field AddTime = FindByName(__.AddTime);

            ///<summary>备注</summary>
            public static readonly Field Remark = FindByName(__.Remark);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得交易记录字段名称的快捷方式</summary>
        class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>名称</summary>
            public const String Name = "Name";

            ///<summary>客户ID</summary>
            public const String CustomerID = "CustomerID";

            ///<summary>机器ID</summary>
            public const String MachineID = "MachineID";

            ///<summary>经手人</summary>
            public const String Transactor = "Transactor";

            ///<summary>出厂日期</summary>
            public const String LeaveTime = "LeaveTime";

            ///<summary>机器外形尺寸</summary>
            public const String OutlineSize = "OutlineSize";

            ///<summary>附送配件</summary>
            public const String Attachment = "Attachment";

            ///<summary>点胶阀门类型</summary>
            public const String Type = "Type";

            ///<summary>混合管型号</summary>
            public const String Model = "Model";

            ///<summary>真空泵规格</summary>
            public const String VacuumpumpSpec = "VacuumpumpSpec";

            ///<summary>数据显示屏种类</summary>
            public const String Kind = "Kind";

            ///<summary>计量泵组别</summary>
            public const String Groupings = "Groupings";

            ///<summary>计量泵尺寸</summary>
            public const String Size = "Size";

            ///<summary>计量泵密封件规格</summary>
            public const String MeteringpumpSpec = "MeteringpumpSpec";

            ///<summary>压力桶大小</summary>
            public const String PresSize = "PresSize";

            ///<summary>进料管规格</summary>
            public const String SupplypipeSpec = "SupplypipeSpec";

            ///<summary>出料管规格</summary>
            public const String DischargeSpec = "DischargeSpec";

            ///<summary>B料计量泵组别</summary>
            public const String GroupingsB = "GroupingsB";

            ///<summary>B料计量泵尺寸</summary>
            public const String SizeB = "SizeB";

            ///<summary>B料计量泵密封件规格</summary>
            public const String MeteringpumpSpecB = "MeteringpumpSpecB";

            ///<summary>B料压力桶大小</summary>
            public const String PresSizeB = "PresSizeB";

            ///<summary>B料进料管规格</summary>
            public const String SupplypipeSpecB = "SupplypipeSpecB";

            ///<summary>B料出料管规格</summary>
            public const String DischargeSpecB = "DischargeSpecB";

            ///<summary>Pic</summary>
            public const String Pic = "Pic";

            ///<summary>添加时间</summary>
            public const String AddTime = "AddTime";

            ///<summary>备注</summary>
            public const String Remark = "Remark";

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

        /// <summary>Pic</summary>
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