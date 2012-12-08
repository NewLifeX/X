/*
 * XCoder v4.3.2011.0915
 * 作者：nnhy/NEWLIFE
 * 时间：2011-09-28 11:04:30
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.YWS.Entities
{
    /// <summary>液料规格</summary>
    [Serializable]
    [DataObject]
    [Description("液料规格")]
    [BindIndex("PK__Feedliqu__3214EC2703317E3D", true, "ID")]
    [BindIndex("IX_Feedliquor_CustomerID", false, "CustomerID")]
    [BindRelation("CustomerID", false, "Customer", "ID")]
    [BindRelation("ID", true, "Machine", "FeedliquorID")]
    [BindTable("Feedliquor", Description = "液料规格", ConnName = "YWS", DbType = DatabaseType.SqlServer)]
    public partial class Feedliquor : IFeedliquor
    
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

        private Int32 _CustomerID;
        /// <summary>客户ID</summary>
        [DisplayName("客户ID")]
        [Description("客户ID")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(2, "CustomerID", "客户ID", null, "int", 10, 0, false)]
        public Int32 CustomerID
        {
            get { return _CustomerID; }
            set { if (OnPropertyChanging("CustomerID", value)) { _CustomerID = value; OnPropertyChanged("CustomerID"); } }
        }

        private String _Manufacturer;
        /// <summary>制造商</summary>
        [DisplayName("制造商")]
        [Description("制造商")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Manufacturer", "制造商", null, "nvarchar(50)", 0, 0, true)]
        public String Manufacturer
        {
            get { return _Manufacturer; }
            set { if (OnPropertyChanging("Manufacturer", value)) { _Manufacturer = value; OnPropertyChanged("Manufacturer"); } }
        }

        private String _Tel;
        /// <summary>联系电话</summary>
        [DisplayName("联系电话")]
        [Description("联系电话")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Tel", "联系电话", null, "nvarchar(50)", 0, 0, true)]
        public String Tel
        {
            get { return _Tel; }
            set { if (OnPropertyChanging("Tel", value)) { _Tel = value; OnPropertyChanged("Tel"); } }
        }

        private String _Address;
        /// <summary>联系地址</summary>
        [DisplayName("联系地址")]
        [Description("联系地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "Address", "联系地址", null, "nvarchar(50)", 0, 0, true)]
        public String Address
        {
            get { return _Address; }
            set { if (OnPropertyChanging("Address", value)) { _Address = value; OnPropertyChanged("Address"); } }
        }

        private String _CementGroup;
        /// <summary>胶水组别</summary>
        [DisplayName("胶水组别")]
        [Description("胶水组别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(6, "CementGroup", "胶水组别", null, "nvarchar(50)", 0, 0, true)]
        public String CementGroup
        {
            get { return _CementGroup; }
            set { if (OnPropertyChanging("CementGroup", value)) { _CementGroup = value; OnPropertyChanged("CementGroup"); } }
        }

        private String _ProductNo;
        /// <summary>产品编号</summary>
        [DisplayName("产品编号")]
        [Description("产品编号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "ProductNo", "产品编号", null, "nvarchar(50)", 0, 0, true)]
        public String ProductNo
        {
            get { return _ProductNo; }
            set { if (OnPropertyChanging("ProductNo", value)) { _ProductNo = value; OnPropertyChanged("ProductNo"); } }
        }

        private Double _WeightRatio;
        /// <summary>重量比</summary>
        [DisplayName("重量比")]
        [Description("重量比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(8, "WeightRatio", "重量比", null, "float", 53, 0, false)]
        public Double WeightRatio
        {
            get { return _WeightRatio; }
            set { if (OnPropertyChanging("WeightRatio", value)) { _WeightRatio = value; OnPropertyChanged("WeightRatio"); } }
        }

        private Double _VolumeRatio;
        /// <summary>体积比</summary>
        [DisplayName("体积比")]
        [Description("体积比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(9, "VolumeRatio", "体积比", null, "float", 53, 0, false)]
        public Double VolumeRatio
        {
            get { return _VolumeRatio; }
            set { if (OnPropertyChanging("VolumeRatio", value)) { _VolumeRatio = value; OnPropertyChanged("VolumeRatio"); } }
        }

        private String _Viscosity;
        /// <summary>黏稠度</summary>
        [DisplayName("黏稠度")]
        [Description("黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(10, "Viscosity", "黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public String Viscosity
        {
            get { return _Viscosity; }
            set { if (OnPropertyChanging("Viscosity", value)) { _Viscosity = value; OnPropertyChanged("Viscosity"); } }
        }

        private String _MixViscosity;
        /// <summary>A/B混合后黏稠度</summary>
        [DisplayName("A/B混合后黏稠度")]
        [Description("A/B混合后黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(11, "MixViscosity", "A/B混合后黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public String MixViscosity
        {
            get { return _MixViscosity; }
            set { if (OnPropertyChanging("MixViscosity", value)) { _MixViscosity = value; OnPropertyChanged("MixViscosity"); } }
        }

        private String _SpecificGravity;
        /// <summary>比重</summary>
        [DisplayName("比重")]
        [Description("比重")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(12, "SpecificGravity", "比重", null, "nvarchar(50)", 0, 0, true)]
        public String SpecificGravity
        {
            get { return _SpecificGravity; }
            set { if (OnPropertyChanging("SpecificGravity", value)) { _SpecificGravity = value; OnPropertyChanged("SpecificGravity"); } }
        }

        private String _Temperature;
        /// <summary>工作温度</summary>
        [DisplayName("工作温度")]
        [Description("工作温度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(13, "Temperature", "工作温度", null, "nvarchar(50)", 0, 0, true)]
        public String Temperature
        {
            get { return _Temperature; }
            set { if (OnPropertyChanging("Temperature", value)) { _Temperature = value; OnPropertyChanged("Temperature"); } }
        }

        private String _WViscosity;
        /// <summary>工作温度下的黏稠度</summary>
        [DisplayName("工作温度下的黏稠度")]
        [Description("工作温度下的黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(14, "WViscosity", "工作温度下的黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public String WViscosity
        {
            get { return _WViscosity; }
            set { if (OnPropertyChanging("WViscosity", value)) { _WViscosity = value; OnPropertyChanged("WViscosity"); } }
        }

        private Boolean _IsFillers;
        /// <summary>是否有填充剂</summary>
        [DisplayName("是否有填充剂")]
        [Description("是否有填充剂")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(15, "IsFillers", "是否有填充剂", null, "bit", 0, 0, false)]
        public Boolean IsFillers
        {
            get { return _IsFillers; }
            set { if (OnPropertyChanging("IsFillers", value)) { _IsFillers = value; OnPropertyChanged("IsFillers"); } }
        }

        private String _FillersType;
        /// <summary>填充剂类型</summary>
        [DisplayName("填充剂类型")]
        [Description("填充剂类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(16, "FillersType", "填充剂类型", null, "nvarchar(50)", 0, 0, true)]
        public String FillersType
        {
            get { return _FillersType; }
            set { if (OnPropertyChanging("FillersType", value)) { _FillersType = value; OnPropertyChanged("FillersType"); } }
        }

        private Double _FillersAmount;
        /// <summary>填充剂分量</summary>
        [DisplayName("填充剂分量")]
        [Description("填充剂分量")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(17, "FillersAmount", "填充剂分量", null, "float", 53, 0, false)]
        public Double FillersAmount
        {
            get { return _FillersAmount; }
            set { if (OnPropertyChanging("FillersAmount", value)) { _FillersAmount = value; OnPropertyChanged("FillersAmount"); } }
        }

        private Boolean _IsAbradability;
        /// <summary>是否磨损</summary>
        [DisplayName("是否磨损")]
        [Description("是否磨损")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(18, "IsAbradability", "是否磨损", null, "bit", 0, 0, false)]
        public Boolean IsAbradability
        {
            get { return _IsAbradability; }
            set { if (OnPropertyChanging("IsAbradability", value)) { _IsAbradability = value; OnPropertyChanged("IsAbradability"); } }
        }

        private Boolean _IsCorrosivity;
        /// <summary>是否腐蚀</summary>
        [DisplayName("是否腐蚀")]
        [Description("是否腐蚀")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(19, "IsCorrosivity", "是否腐蚀", null, "bit", 0, 0, false)]
        public Boolean IsCorrosivity
        {
            get { return _IsCorrosivity; }
            set { if (OnPropertyChanging("IsCorrosivity", value)) { _IsCorrosivity = value; OnPropertyChanged("IsCorrosivity"); } }
        }

        private Boolean _IsSensitivity;
        /// <summary>材料是否潮湿敏感</summary>
        [DisplayName("材料是否潮湿敏感")]
        [Description("材料是否潮湿敏感")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(20, "IsSensitivity", "材料是否潮湿敏感", null, "bit", 0, 0, false)]
        public Boolean IsSensitivity
        {
            get { return _IsSensitivity; }
            set { if (OnPropertyChanging("IsSensitivity", value)) { _IsSensitivity = value; OnPropertyChanged("IsSensitivity"); } }
        }

        private Boolean _IsAgitation;
        /// <summary>是否需要搅拌</summary>
        [DisplayName("是否需要搅拌")]
        [Description("是否需要搅拌")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(21, "IsAgitation", "是否需要搅拌", null, "bit", 0, 0, false)]
        public Boolean IsAgitation
        {
            get { return _IsAgitation; }
            set { if (OnPropertyChanging("IsAgitation", value)) { _IsAgitation = value; OnPropertyChanged("IsAgitation"); } }
        }

        private Boolean _IsExcept;
        /// <summary>是否需要真空初除泡</summary>
        [DisplayName("是否需要真空初除泡")]
        [Description("是否需要真空初除泡")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(22, "IsExcept", "是否需要真空初除泡", null, "bit", 0, 0, false)]
        public Boolean IsExcept
        {
            get { return _IsExcept; }
            set { if (OnPropertyChanging("IsExcept", value)) { _IsExcept = value; OnPropertyChanged("IsExcept"); } }
        }

        private Int32 _WorkingHours;
        /// <summary>材料混合后可工作时间(单位:分钟)</summary>
        [DisplayName("材料混合后可工作时间(单位:分钟)")]
        [Description("材料混合后可工作时间(单位:分钟)")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(23, "WorkingHours", "材料混合后可工作时间(单位:分钟)", null, "int", 10, 0, false)]
        public Int32 WorkingHours
        {
            get { return _WorkingHours; }
            set { if (OnPropertyChanging("WorkingHours", value)) { _WorkingHours = value; OnPropertyChanged("WorkingHours"); } }
        }

        private Boolean _IsSolventName;
        /// <summary>有无溶剂名称</summary>
        [DisplayName("有无溶剂名称")]
        [Description("有无溶剂名称")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(24, "IsSolventName", "有无溶剂名称", null, "bit", 0, 0, false)]
        public Boolean IsSolventName
        {
            get { return _IsSolventName; }
            set { if (OnPropertyChanging("IsSolventName", value)) { _IsSolventName = value; OnPropertyChanged("IsSolventName"); } }
        }

        private String _Hardening;
        /// <summary>材料混合后完全硬化时间</summary>
        [DisplayName("材料混合后完全硬化时间")]
        [Description("材料混合后完全硬化时间")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(25, "Hardening", "材料混合后完全硬化时间", null, "nvarchar(50)", 0, 0, true)]
        public String Hardening
        {
            get { return _Hardening; }
            set { if (OnPropertyChanging("Hardening", value)) { _Hardening = value; OnPropertyChanged("Hardening"); } }
        }

        private String _CementGroupB;
        /// <summary>B组胶水组别</summary>
        [DisplayName("B组胶水组别")]
        [Description("B组胶水组别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(26, "CementGroupB", "B组胶水组别", null, "nvarchar(50)", 0, 0, true)]
        public String CementGroupB
        {
            get { return _CementGroupB; }
            set { if (OnPropertyChanging("CementGroupB", value)) { _CementGroupB = value; OnPropertyChanged("CementGroupB"); } }
        }

        private String _ProductNoB;
        /// <summary>B组产品编号</summary>
        [DisplayName("B组产品编号")]
        [Description("B组产品编号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(27, "ProductNoB", "B组产品编号", null, "nvarchar(50)", 0, 0, true)]
        public String ProductNoB
        {
            get { return _ProductNoB; }
            set { if (OnPropertyChanging("ProductNoB", value)) { _ProductNoB = value; OnPropertyChanged("ProductNoB"); } }
        }

        private Double _WeightRatioB;
        /// <summary>B组重量比</summary>
        [DisplayName("B组重量比")]
        [Description("B组重量比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(28, "WeightRatioB", "B组重量比", null, "float", 53, 0, false)]
        public Double WeightRatioB
        {
            get { return _WeightRatioB; }
            set { if (OnPropertyChanging("WeightRatioB", value)) { _WeightRatioB = value; OnPropertyChanged("WeightRatioB"); } }
        }

        private Double _VolumeRatioB;
        /// <summary>B组体积比</summary>
        [DisplayName("B组体积比")]
        [Description("B组体积比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(29, "VolumeRatioB", "B组体积比", null, "float", 53, 0, false)]
        public Double VolumeRatioB
        {
            get { return _VolumeRatioB; }
            set { if (OnPropertyChanging("VolumeRatioB", value)) { _VolumeRatioB = value; OnPropertyChanged("VolumeRatioB"); } }
        }

        private String _ViscosityB;
        /// <summary>B组黏稠度</summary>
        [DisplayName("B组黏稠度")]
        [Description("B组黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(30, "ViscosityB", "B组黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public String ViscosityB
        {
            get { return _ViscosityB; }
            set { if (OnPropertyChanging("ViscosityB", value)) { _ViscosityB = value; OnPropertyChanged("ViscosityB"); } }
        }

        private String _MixViscosityB;
        /// <summary>B组A/B混合后黏稠度</summary>
        [DisplayName("B组A/B混合后黏稠度")]
        [Description("B组A/B混合后黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(31, "MixViscosityB", "B组A/B混合后黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public String MixViscosityB
        {
            get { return _MixViscosityB; }
            set { if (OnPropertyChanging("MixViscosityB", value)) { _MixViscosityB = value; OnPropertyChanged("MixViscosityB"); } }
        }

        private String _SpecificGravityB;
        /// <summary>B组比重</summary>
        [DisplayName("B组比重")]
        [Description("B组比重")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(32, "SpecificGravityB", "B组比重", null, "nvarchar(50)", 0, 0, true)]
        public String SpecificGravityB
        {
            get { return _SpecificGravityB; }
            set { if (OnPropertyChanging("SpecificGravityB", value)) { _SpecificGravityB = value; OnPropertyChanged("SpecificGravityB"); } }
        }

        private String _TemperatureB;
        /// <summary>B组工作温度</summary>
        [DisplayName("B组工作温度")]
        [Description("B组工作温度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(33, "TemperatureB", "B组工作温度", null, "nvarchar(50)", 0, 0, true)]
        public String TemperatureB
        {
            get { return _TemperatureB; }
            set { if (OnPropertyChanging("TemperatureB", value)) { _TemperatureB = value; OnPropertyChanged("TemperatureB"); } }
        }

        private String _WViscosityB;
        /// <summary>B组工作温度下的黏稠度</summary>
        [DisplayName("B组工作温度下的黏稠度")]
        [Description("B组工作温度下的黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(34, "WViscosityB", "B组工作温度下的黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public String WViscosityB
        {
            get { return _WViscosityB; }
            set { if (OnPropertyChanging("WViscosityB", value)) { _WViscosityB = value; OnPropertyChanged("WViscosityB"); } }
        }

        private Boolean _IsFillersB;
        /// <summary>B组是否有填充剂</summary>
        [DisplayName("B组是否有填充剂")]
        [Description("B组是否有填充剂")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(35, "IsFillersB", "B组是否有填充剂", null, "bit", 0, 0, false)]
        public Boolean IsFillersB
        {
            get { return _IsFillersB; }
            set { if (OnPropertyChanging("IsFillersB", value)) { _IsFillersB = value; OnPropertyChanged("IsFillersB"); } }
        }

        private String _FillersTypeB;
        /// <summary>B组填充剂类型</summary>
        [DisplayName("B组填充剂类型")]
        [Description("B组填充剂类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(36, "FillersTypeB", "B组填充剂类型", null, "nvarchar(50)", 0, 0, true)]
        public String FillersTypeB
        {
            get { return _FillersTypeB; }
            set { if (OnPropertyChanging("FillersTypeB", value)) { _FillersTypeB = value; OnPropertyChanged("FillersTypeB"); } }
        }

        private Double _FillersAmountB;
        /// <summary>B组填充剂分量</summary>
        [DisplayName("B组填充剂分量")]
        [Description("B组填充剂分量")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(37, "FillersAmountB", "B组填充剂分量", null, "float", 53, 0, false)]
        public Double FillersAmountB
        {
            get { return _FillersAmountB; }
            set { if (OnPropertyChanging("FillersAmountB", value)) { _FillersAmountB = value; OnPropertyChanged("FillersAmountB"); } }
        }

        private Boolean _IsAbradabilityB;
        /// <summary>B组是否磨损</summary>
        [DisplayName("B组是否磨损")]
        [Description("B组是否磨损")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(38, "IsAbradabilityB", "B组是否磨损", null, "bit", 0, 0, false)]
        public Boolean IsAbradabilityB
        {
            get { return _IsAbradabilityB; }
            set { if (OnPropertyChanging("IsAbradabilityB", value)) { _IsAbradabilityB = value; OnPropertyChanged("IsAbradabilityB"); } }
        }

        private Boolean _IsCorrosivityB;
        /// <summary>B组是否腐蚀</summary>
        [DisplayName("B组是否腐蚀")]
        [Description("B组是否腐蚀")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(39, "IsCorrosivityB", "B组是否腐蚀", null, "bit", 0, 0, false)]
        public Boolean IsCorrosivityB
        {
            get { return _IsCorrosivityB; }
            set { if (OnPropertyChanging("IsCorrosivityB", value)) { _IsCorrosivityB = value; OnPropertyChanged("IsCorrosivityB"); } }
        }

        private Boolean _IsSensitivityB;
        /// <summary>B组材料是否潮湿敏感</summary>
        [DisplayName("B组材料是否潮湿敏感")]
        [Description("B组材料是否潮湿敏感")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(40, "IsSensitivityB", "B组材料是否潮湿敏感", null, "bit", 0, 0, false)]
        public Boolean IsSensitivityB
        {
            get { return _IsSensitivityB; }
            set { if (OnPropertyChanging("IsSensitivityB", value)) { _IsSensitivityB = value; OnPropertyChanged("IsSensitivityB"); } }
        }

        private Boolean _IsAgitationB;
        /// <summary>B组是否需要搅拌</summary>
        [DisplayName("B组是否需要搅拌")]
        [Description("B组是否需要搅拌")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(41, "IsAgitationB", "B组是否需要搅拌", null, "bit", 0, 0, false)]
        public Boolean IsAgitationB
        {
            get { return _IsAgitationB; }
            set { if (OnPropertyChanging("IsAgitationB", value)) { _IsAgitationB = value; OnPropertyChanged("IsAgitationB"); } }
        }

        private Boolean _IsExceptB;
        /// <summary>B组是否需要真空初除泡</summary>
        [DisplayName("B组是否需要真空初除泡")]
        [Description("B组是否需要真空初除泡")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(42, "IsExceptB", "B组是否需要真空初除泡", null, "bit", 0, 0, false)]
        public Boolean IsExceptB
        {
            get { return _IsExceptB; }
            set { if (OnPropertyChanging("IsExceptB", value)) { _IsExceptB = value; OnPropertyChanged("IsExceptB"); } }
        }

        private Int32 _WorkingHoursB;
        /// <summary>B组材料混合后可工作时间(单位:分钟)</summary>
        [DisplayName("B组材料混合后可工作时间(单位:分钟)")]
        [Description("B组材料混合后可工作时间(单位:分钟)")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(43, "WorkingHoursB", "B组材料混合后可工作时间(单位:分钟)", null, "int", 10, 0, false)]
        public Int32 WorkingHoursB
        {
            get { return _WorkingHoursB; }
            set { if (OnPropertyChanging("WorkingHoursB", value)) { _WorkingHoursB = value; OnPropertyChanged("WorkingHoursB"); } }
        }

        private Boolean _IsSolventNameB;
        /// <summary>B组有无溶剂名称</summary>
        [DisplayName("B组有无溶剂名称")]
        [Description("B组有无溶剂名称")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(44, "IsSolventNameB", "B组有无溶剂名称", null, "bit", 0, 0, false)]
        public Boolean IsSolventNameB
        {
            get { return _IsSolventNameB; }
            set { if (OnPropertyChanging("IsSolventNameB", value)) { _IsSolventNameB = value; OnPropertyChanged("IsSolventNameB"); } }
        }

        private String _HardeningB;
        /// <summary>B组材料混合后完全硬化时间</summary>
        [DisplayName("B组材料混合后完全硬化时间")]
        [Description("B组材料混合后完全硬化时间")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(45, "HardeningB", "B组材料混合后完全硬化时间", null, "nvarchar(50)", 0, 0, true)]
        public String HardeningB
        {
            get { return _HardeningB; }
            set { if (OnPropertyChanging("HardeningB", value)) { _HardeningB = value; OnPropertyChanged("HardeningB"); } }
        }

        private DateTime _AddTime;
        /// <summary>发布时间</summary>
        [DisplayName("发布时间")]
        [Description("发布时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(46, "AddTime", "发布时间", null, "datetime", 3, 0, false)]
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
        [BindColumn(47, "Remark", "备注", null, "nvarchar(100)", 0, 0, true)]
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
                    case "CustomerID" : return _CustomerID;
                    case "Manufacturer" : return _Manufacturer;
                    case "Tel" : return _Tel;
                    case "Address" : return _Address;
                    case "CementGroup" : return _CementGroup;
                    case "ProductNo" : return _ProductNo;
                    case "WeightRatio" : return _WeightRatio;
                    case "VolumeRatio" : return _VolumeRatio;
                    case "Viscosity" : return _Viscosity;
                    case "MixViscosity" : return _MixViscosity;
                    case "SpecificGravity" : return _SpecificGravity;
                    case "Temperature" : return _Temperature;
                    case "WViscosity" : return _WViscosity;
                    case "IsFillers" : return _IsFillers;
                    case "FillersType" : return _FillersType;
                    case "FillersAmount" : return _FillersAmount;
                    case "IsAbradability" : return _IsAbradability;
                    case "IsCorrosivity" : return _IsCorrosivity;
                    case "IsSensitivity" : return _IsSensitivity;
                    case "IsAgitation" : return _IsAgitation;
                    case "IsExcept" : return _IsExcept;
                    case "WorkingHours" : return _WorkingHours;
                    case "IsSolventName" : return _IsSolventName;
                    case "Hardening" : return _Hardening;
                    case "CementGroupB" : return _CementGroupB;
                    case "ProductNoB" : return _ProductNoB;
                    case "WeightRatioB" : return _WeightRatioB;
                    case "VolumeRatioB" : return _VolumeRatioB;
                    case "ViscosityB" : return _ViscosityB;
                    case "MixViscosityB" : return _MixViscosityB;
                    case "SpecificGravityB" : return _SpecificGravityB;
                    case "TemperatureB" : return _TemperatureB;
                    case "WViscosityB" : return _WViscosityB;
                    case "IsFillersB" : return _IsFillersB;
                    case "FillersTypeB" : return _FillersTypeB;
                    case "FillersAmountB" : return _FillersAmountB;
                    case "IsAbradabilityB" : return _IsAbradabilityB;
                    case "IsCorrosivityB" : return _IsCorrosivityB;
                    case "IsSensitivityB" : return _IsSensitivityB;
                    case "IsAgitationB" : return _IsAgitationB;
                    case "IsExceptB" : return _IsExceptB;
                    case "WorkingHoursB" : return _WorkingHoursB;
                    case "IsSolventNameB" : return _IsSolventNameB;
                    case "HardeningB" : return _HardeningB;
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
                    case "CustomerID" : _CustomerID = Convert.ToInt32(value); break;
                    case "Manufacturer" : _Manufacturer = Convert.ToString(value); break;
                    case "Tel" : _Tel = Convert.ToString(value); break;
                    case "Address" : _Address = Convert.ToString(value); break;
                    case "CementGroup" : _CementGroup = Convert.ToString(value); break;
                    case "ProductNo" : _ProductNo = Convert.ToString(value); break;
                    case "WeightRatio" : _WeightRatio = Convert.ToDouble(value); break;
                    case "VolumeRatio" : _VolumeRatio = Convert.ToDouble(value); break;
                    case "Viscosity" : _Viscosity = Convert.ToString(value); break;
                    case "MixViscosity" : _MixViscosity = Convert.ToString(value); break;
                    case "SpecificGravity" : _SpecificGravity = Convert.ToString(value); break;
                    case "Temperature" : _Temperature = Convert.ToString(value); break;
                    case "WViscosity" : _WViscosity = Convert.ToString(value); break;
                    case "IsFillers" : _IsFillers = Convert.ToBoolean(value); break;
                    case "FillersType" : _FillersType = Convert.ToString(value); break;
                    case "FillersAmount" : _FillersAmount = Convert.ToDouble(value); break;
                    case "IsAbradability" : _IsAbradability = Convert.ToBoolean(value); break;
                    case "IsCorrosivity" : _IsCorrosivity = Convert.ToBoolean(value); break;
                    case "IsSensitivity" : _IsSensitivity = Convert.ToBoolean(value); break;
                    case "IsAgitation" : _IsAgitation = Convert.ToBoolean(value); break;
                    case "IsExcept" : _IsExcept = Convert.ToBoolean(value); break;
                    case "WorkingHours" : _WorkingHours = Convert.ToInt32(value); break;
                    case "IsSolventName" : _IsSolventName = Convert.ToBoolean(value); break;
                    case "Hardening" : _Hardening = Convert.ToString(value); break;
                    case "CementGroupB" : _CementGroupB = Convert.ToString(value); break;
                    case "ProductNoB" : _ProductNoB = Convert.ToString(value); break;
                    case "WeightRatioB" : _WeightRatioB = Convert.ToDouble(value); break;
                    case "VolumeRatioB" : _VolumeRatioB = Convert.ToDouble(value); break;
                    case "ViscosityB" : _ViscosityB = Convert.ToString(value); break;
                    case "MixViscosityB" : _MixViscosityB = Convert.ToString(value); break;
                    case "SpecificGravityB" : _SpecificGravityB = Convert.ToString(value); break;
                    case "TemperatureB" : _TemperatureB = Convert.ToString(value); break;
                    case "WViscosityB" : _WViscosityB = Convert.ToString(value); break;
                    case "IsFillersB" : _IsFillersB = Convert.ToBoolean(value); break;
                    case "FillersTypeB" : _FillersTypeB = Convert.ToString(value); break;
                    case "FillersAmountB" : _FillersAmountB = Convert.ToDouble(value); break;
                    case "IsAbradabilityB" : _IsAbradabilityB = Convert.ToBoolean(value); break;
                    case "IsCorrosivityB" : _IsCorrosivityB = Convert.ToBoolean(value); break;
                    case "IsSensitivityB" : _IsSensitivityB = Convert.ToBoolean(value); break;
                    case "IsAgitationB" : _IsAgitationB = Convert.ToBoolean(value); break;
                    case "IsExceptB" : _IsExceptB = Convert.ToBoolean(value); break;
                    case "WorkingHoursB" : _WorkingHoursB = Convert.ToInt32(value); break;
                    case "IsSolventNameB" : _IsSolventNameB = Convert.ToBoolean(value); break;
                    case "HardeningB" : _HardeningB = Convert.ToString(value); break;
                    case "AddTime" : _AddTime = Convert.ToDateTime(value); break;
                    case "Remark" : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得液料规格字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>客户ID</summary>
            public static readonly Field CustomerID = Meta.Table.FindByName("CustomerID");

            ///<summary>制造商</summary>
            public static readonly Field Manufacturer = Meta.Table.FindByName("Manufacturer");

            ///<summary>联系电话</summary>
            public static readonly Field Tel = Meta.Table.FindByName("Tel");

            ///<summary>联系地址</summary>
            public static readonly Field Address = Meta.Table.FindByName("Address");

            ///<summary>胶水组别</summary>
            public static readonly Field CementGroup = Meta.Table.FindByName("CementGroup");

            ///<summary>产品编号</summary>
            public static readonly Field ProductNo = Meta.Table.FindByName("ProductNo");

            ///<summary>重量比</summary>
            public static readonly Field WeightRatio = Meta.Table.FindByName("WeightRatio");

            ///<summary>体积比</summary>
            public static readonly Field VolumeRatio = Meta.Table.FindByName("VolumeRatio");

            ///<summary>黏稠度</summary>
            public static readonly Field Viscosity = Meta.Table.FindByName("Viscosity");

            ///<summary>A/B混合后黏稠度</summary>
            public static readonly Field MixViscosity = Meta.Table.FindByName("MixViscosity");

            ///<summary>比重</summary>
            public static readonly Field SpecificGravity = Meta.Table.FindByName("SpecificGravity");

            ///<summary>工作温度</summary>
            public static readonly Field Temperature = Meta.Table.FindByName("Temperature");

            ///<summary>工作温度下的黏稠度</summary>
            public static readonly Field WViscosity = Meta.Table.FindByName("WViscosity");

            ///<summary>是否有填充剂</summary>
            public static readonly Field IsFillers = Meta.Table.FindByName("IsFillers");

            ///<summary>填充剂类型</summary>
            public static readonly Field FillersType = Meta.Table.FindByName("FillersType");

            ///<summary>填充剂分量</summary>
            public static readonly Field FillersAmount = Meta.Table.FindByName("FillersAmount");

            ///<summary>是否磨损</summary>
            public static readonly Field IsAbradability = Meta.Table.FindByName("IsAbradability");

            ///<summary>是否腐蚀</summary>
            public static readonly Field IsCorrosivity = Meta.Table.FindByName("IsCorrosivity");

            ///<summary>材料是否潮湿敏感</summary>
            public static readonly Field IsSensitivity = Meta.Table.FindByName("IsSensitivity");

            ///<summary>是否需要搅拌</summary>
            public static readonly Field IsAgitation = Meta.Table.FindByName("IsAgitation");

            ///<summary>是否需要真空初除泡</summary>
            public static readonly Field IsExcept = Meta.Table.FindByName("IsExcept");

            ///<summary>材料混合后可工作时间(单位:分钟)</summary>
            public static readonly Field WorkingHours = Meta.Table.FindByName("WorkingHours");

            ///<summary>有无溶剂名称</summary>
            public static readonly Field IsSolventName = Meta.Table.FindByName("IsSolventName");

            ///<summary>材料混合后完全硬化时间</summary>
            public static readonly Field Hardening = Meta.Table.FindByName("Hardening");

            ///<summary>B组胶水组别</summary>
            public static readonly Field CementGroupB = Meta.Table.FindByName("CementGroupB");

            ///<summary>B组产品编号</summary>
            public static readonly Field ProductNoB = Meta.Table.FindByName("ProductNoB");

            ///<summary>B组重量比</summary>
            public static readonly Field WeightRatioB = Meta.Table.FindByName("WeightRatioB");

            ///<summary>B组体积比</summary>
            public static readonly Field VolumeRatioB = Meta.Table.FindByName("VolumeRatioB");

            ///<summary>B组黏稠度</summary>
            public static readonly Field ViscosityB = Meta.Table.FindByName("ViscosityB");

            ///<summary>B组A/B混合后黏稠度</summary>
            public static readonly Field MixViscosityB = Meta.Table.FindByName("MixViscosityB");

            ///<summary>B组比重</summary>
            public static readonly Field SpecificGravityB = Meta.Table.FindByName("SpecificGravityB");

            ///<summary>B组工作温度</summary>
            public static readonly Field TemperatureB = Meta.Table.FindByName("TemperatureB");

            ///<summary>B组工作温度下的黏稠度</summary>
            public static readonly Field WViscosityB = Meta.Table.FindByName("WViscosityB");

            ///<summary>B组是否有填充剂</summary>
            public static readonly Field IsFillersB = Meta.Table.FindByName("IsFillersB");

            ///<summary>B组填充剂类型</summary>
            public static readonly Field FillersTypeB = Meta.Table.FindByName("FillersTypeB");

            ///<summary>B组填充剂分量</summary>
            public static readonly Field FillersAmountB = Meta.Table.FindByName("FillersAmountB");

            ///<summary>B组是否磨损</summary>
            public static readonly Field IsAbradabilityB = Meta.Table.FindByName("IsAbradabilityB");

            ///<summary>B组是否腐蚀</summary>
            public static readonly Field IsCorrosivityB = Meta.Table.FindByName("IsCorrosivityB");

            ///<summary>B组材料是否潮湿敏感</summary>
            public static readonly Field IsSensitivityB = Meta.Table.FindByName("IsSensitivityB");

            ///<summary>B组是否需要搅拌</summary>
            public static readonly Field IsAgitationB = Meta.Table.FindByName("IsAgitationB");

            ///<summary>B组是否需要真空初除泡</summary>
            public static readonly Field IsExceptB = Meta.Table.FindByName("IsExceptB");

            ///<summary>B组材料混合后可工作时间(单位:分钟)</summary>
            public static readonly Field WorkingHoursB = Meta.Table.FindByName("WorkingHoursB");

            ///<summary>B组有无溶剂名称</summary>
            public static readonly Field IsSolventNameB = Meta.Table.FindByName("IsSolventNameB");

            ///<summary>B组材料混合后完全硬化时间</summary>
            public static readonly Field HardeningB = Meta.Table.FindByName("HardeningB");

            ///<summary>发布时间</summary>
            public static readonly Field AddTime = Meta.Table.FindByName("AddTime");

            ///<summary>备注</summary>
            public static readonly Field Remark = Meta.Table.FindByName("Remark");
        }
        #endregion
    }

    /// <summary>液料规格接口</summary>
    public partial interface IFeedliquor
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>客户ID</summary>
        Int32 CustomerID { get; set; }

        /// <summary>制造商</summary>
        String Manufacturer { get; set; }

        /// <summary>联系电话</summary>
        String Tel { get; set; }

        /// <summary>联系地址</summary>
        String Address { get; set; }

        /// <summary>胶水组别</summary>
        String CementGroup { get; set; }

        /// <summary>产品编号</summary>
        String ProductNo { get; set; }

        /// <summary>重量比</summary>
        Double WeightRatio { get; set; }

        /// <summary>体积比</summary>
        Double VolumeRatio { get; set; }

        /// <summary>黏稠度</summary>
        String Viscosity { get; set; }

        /// <summary>A/B混合后黏稠度</summary>
        String MixViscosity { get; set; }

        /// <summary>比重</summary>
        String SpecificGravity { get; set; }

        /// <summary>工作温度</summary>
        String Temperature { get; set; }

        /// <summary>工作温度下的黏稠度</summary>
        String WViscosity { get; set; }

        /// <summary>是否有填充剂</summary>
        Boolean IsFillers { get; set; }

        /// <summary>填充剂类型</summary>
        String FillersType { get; set; }

        /// <summary>填充剂分量</summary>
        Double FillersAmount { get; set; }

        /// <summary>是否磨损</summary>
        Boolean IsAbradability { get; set; }

        /// <summary>是否腐蚀</summary>
        Boolean IsCorrosivity { get; set; }

        /// <summary>材料是否潮湿敏感</summary>
        Boolean IsSensitivity { get; set; }

        /// <summary>是否需要搅拌</summary>
        Boolean IsAgitation { get; set; }

        /// <summary>是否需要真空初除泡</summary>
        Boolean IsExcept { get; set; }

        /// <summary>材料混合后可工作时间(单位:分钟)</summary>
        Int32 WorkingHours { get; set; }

        /// <summary>有无溶剂名称</summary>
        Boolean IsSolventName { get; set; }

        /// <summary>材料混合后完全硬化时间</summary>
        String Hardening { get; set; }

        /// <summary>B组胶水组别</summary>
        String CementGroupB { get; set; }

        /// <summary>B组产品编号</summary>
        String ProductNoB { get; set; }

        /// <summary>B组重量比</summary>
        Double WeightRatioB { get; set; }

        /// <summary>B组体积比</summary>
        Double VolumeRatioB { get; set; }

        /// <summary>B组黏稠度</summary>
        String ViscosityB { get; set; }

        /// <summary>B组A/B混合后黏稠度</summary>
        String MixViscosityB { get; set; }

        /// <summary>B组比重</summary>
        String SpecificGravityB { get; set; }

        /// <summary>B组工作温度</summary>
        String TemperatureB { get; set; }

        /// <summary>B组工作温度下的黏稠度</summary>
        String WViscosityB { get; set; }

        /// <summary>B组是否有填充剂</summary>
        Boolean IsFillersB { get; set; }

        /// <summary>B组填充剂类型</summary>
        String FillersTypeB { get; set; }

        /// <summary>B组填充剂分量</summary>
        Double FillersAmountB { get; set; }

        /// <summary>B组是否磨损</summary>
        Boolean IsAbradabilityB { get; set; }

        /// <summary>B组是否腐蚀</summary>
        Boolean IsCorrosivityB { get; set; }

        /// <summary>B组材料是否潮湿敏感</summary>
        Boolean IsSensitivityB { get; set; }

        /// <summary>B组是否需要搅拌</summary>
        Boolean IsAgitationB { get; set; }

        /// <summary>B组是否需要真空初除泡</summary>
        Boolean IsExceptB { get; set; }

        /// <summary>B组材料混合后可工作时间(单位:分钟)</summary>
        Int32 WorkingHoursB { get; set; }

        /// <summary>B组有无溶剂名称</summary>
        Boolean IsSolventNameB { get; set; }

        /// <summary>B组材料混合后完全硬化时间</summary>
        String HardeningB { get; set; }

        /// <summary>发布时间</summary>
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