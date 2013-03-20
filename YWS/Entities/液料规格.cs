﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
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
        public virtual Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } }
        }

        private Int32 _CustomerID;
        /// <summary>客户ID</summary>
        [DisplayName("客户ID")]
        [Description("客户ID")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(2, "CustomerID", "客户ID", null, "int", 10, 0, false)]
        public virtual Int32 CustomerID
        {
            get { return _CustomerID; }
            set { if (OnPropertyChanging(__.CustomerID, value)) { _CustomerID = value; OnPropertyChanged(__.CustomerID); } }
        }

        private String _Manufacturer;
        /// <summary>制造商</summary>
        [DisplayName("制造商")]
        [Description("制造商")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Manufacturer", "制造商", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Manufacturer
        {
            get { return _Manufacturer; }
            set { if (OnPropertyChanging(__.Manufacturer, value)) { _Manufacturer = value; OnPropertyChanged(__.Manufacturer); } }
        }

        private String _Tel;
        /// <summary>联系电话</summary>
        [DisplayName("联系电话")]
        [Description("联系电话")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Tel", "联系电话", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Tel
        {
            get { return _Tel; }
            set { if (OnPropertyChanging(__.Tel, value)) { _Tel = value; OnPropertyChanged(__.Tel); } }
        }

        private String _Address;
        /// <summary>联系地址</summary>
        [DisplayName("联系地址")]
        [Description("联系地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "Address", "联系地址", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Address
        {
            get { return _Address; }
            set { if (OnPropertyChanging(__.Address, value)) { _Address = value; OnPropertyChanged(__.Address); } }
        }

        private String _CementGroup;
        /// <summary>胶水组别</summary>
        [DisplayName("胶水组别")]
        [Description("胶水组别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(6, "CementGroup", "胶水组别", null, "nvarchar(50)", 0, 0, true)]
        public virtual String CementGroup
        {
            get { return _CementGroup; }
            set { if (OnPropertyChanging(__.CementGroup, value)) { _CementGroup = value; OnPropertyChanged(__.CementGroup); } }
        }

        private String _ProductNo;
        /// <summary>产品编号</summary>
        [DisplayName("产品编号")]
        [Description("产品编号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "ProductNo", "产品编号", null, "nvarchar(50)", 0, 0, true)]
        public virtual String ProductNo
        {
            get { return _ProductNo; }
            set { if (OnPropertyChanging(__.ProductNo, value)) { _ProductNo = value; OnPropertyChanged(__.ProductNo); } }
        }

        private Double _WeightRatio;
        /// <summary>重量比</summary>
        [DisplayName("重量比")]
        [Description("重量比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(8, "WeightRatio", "重量比", null, "float", 53, 0, false)]
        public virtual Double WeightRatio
        {
            get { return _WeightRatio; }
            set { if (OnPropertyChanging(__.WeightRatio, value)) { _WeightRatio = value; OnPropertyChanged(__.WeightRatio); } }
        }

        private Double _VolumeRatio;
        /// <summary>体积比</summary>
        [DisplayName("体积比")]
        [Description("体积比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(9, "VolumeRatio", "体积比", null, "float", 53, 0, false)]
        public virtual Double VolumeRatio
        {
            get { return _VolumeRatio; }
            set { if (OnPropertyChanging(__.VolumeRatio, value)) { _VolumeRatio = value; OnPropertyChanged(__.VolumeRatio); } }
        }

        private String _Viscosity;
        /// <summary>黏稠度</summary>
        [DisplayName("黏稠度")]
        [Description("黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(10, "Viscosity", "黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Viscosity
        {
            get { return _Viscosity; }
            set { if (OnPropertyChanging(__.Viscosity, value)) { _Viscosity = value; OnPropertyChanged(__.Viscosity); } }
        }

        private String _MixViscosity;
        /// <summary>A/B混合后黏稠度</summary>
        [DisplayName("A_B混合后黏稠度")]
        [Description("A/B混合后黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(11, "MixViscosity", "A/B混合后黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public virtual String MixViscosity
        {
            get { return _MixViscosity; }
            set { if (OnPropertyChanging(__.MixViscosity, value)) { _MixViscosity = value; OnPropertyChanged(__.MixViscosity); } }
        }

        private String _SpecificGravity;
        /// <summary>比重</summary>
        [DisplayName("比重")]
        [Description("比重")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(12, "SpecificGravity", "比重", null, "nvarchar(50)", 0, 0, true)]
        public virtual String SpecificGravity
        {
            get { return _SpecificGravity; }
            set { if (OnPropertyChanging(__.SpecificGravity, value)) { _SpecificGravity = value; OnPropertyChanged(__.SpecificGravity); } }
        }

        private String _Temperature;
        /// <summary>工作温度</summary>
        [DisplayName("工作温度")]
        [Description("工作温度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(13, "Temperature", "工作温度", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Temperature
        {
            get { return _Temperature; }
            set { if (OnPropertyChanging(__.Temperature, value)) { _Temperature = value; OnPropertyChanged(__.Temperature); } }
        }

        private String _WViscosity;
        /// <summary>工作温度下的黏稠度</summary>
        [DisplayName("工作温度下的黏稠度")]
        [Description("工作温度下的黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(14, "WViscosity", "工作温度下的黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public virtual String WViscosity
        {
            get { return _WViscosity; }
            set { if (OnPropertyChanging(__.WViscosity, value)) { _WViscosity = value; OnPropertyChanged(__.WViscosity); } }
        }

        private Boolean _IsFillers;
        /// <summary>是否有填充剂</summary>
        [DisplayName("是否有填充剂")]
        [Description("是否有填充剂")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(15, "IsFillers", "是否有填充剂", null, "bit", 0, 0, false)]
        public virtual Boolean IsFillers
        {
            get { return _IsFillers; }
            set { if (OnPropertyChanging(__.IsFillers, value)) { _IsFillers = value; OnPropertyChanged(__.IsFillers); } }
        }

        private String _FillersType;
        /// <summary>填充剂类型</summary>
        [DisplayName("填充剂类型")]
        [Description("填充剂类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(16, "FillersType", "填充剂类型", null, "nvarchar(50)", 0, 0, true)]
        public virtual String FillersType
        {
            get { return _FillersType; }
            set { if (OnPropertyChanging(__.FillersType, value)) { _FillersType = value; OnPropertyChanged(__.FillersType); } }
        }

        private Double _FillersAmount;
        /// <summary>填充剂分量</summary>
        [DisplayName("填充剂分量")]
        [Description("填充剂分量")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(17, "FillersAmount", "填充剂分量", null, "float", 53, 0, false)]
        public virtual Double FillersAmount
        {
            get { return _FillersAmount; }
            set { if (OnPropertyChanging(__.FillersAmount, value)) { _FillersAmount = value; OnPropertyChanged(__.FillersAmount); } }
        }

        private Boolean _IsAbradability;
        /// <summary>是否磨损</summary>
        [DisplayName("是否磨损")]
        [Description("是否磨损")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(18, "IsAbradability", "是否磨损", null, "bit", 0, 0, false)]
        public virtual Boolean IsAbradability
        {
            get { return _IsAbradability; }
            set { if (OnPropertyChanging(__.IsAbradability, value)) { _IsAbradability = value; OnPropertyChanged(__.IsAbradability); } }
        }

        private Boolean _IsCorrosivity;
        /// <summary>是否腐蚀</summary>
        [DisplayName("是否腐蚀")]
        [Description("是否腐蚀")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(19, "IsCorrosivity", "是否腐蚀", null, "bit", 0, 0, false)]
        public virtual Boolean IsCorrosivity
        {
            get { return _IsCorrosivity; }
            set { if (OnPropertyChanging(__.IsCorrosivity, value)) { _IsCorrosivity = value; OnPropertyChanged(__.IsCorrosivity); } }
        }

        private Boolean _IsSensitivity;
        /// <summary>材料是否潮湿敏感</summary>
        [DisplayName("材料是否潮湿敏感")]
        [Description("材料是否潮湿敏感")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(20, "IsSensitivity", "材料是否潮湿敏感", null, "bit", 0, 0, false)]
        public virtual Boolean IsSensitivity
        {
            get { return _IsSensitivity; }
            set { if (OnPropertyChanging(__.IsSensitivity, value)) { _IsSensitivity = value; OnPropertyChanged(__.IsSensitivity); } }
        }

        private Boolean _IsAgitation;
        /// <summary>是否需要搅拌</summary>
        [DisplayName("是否需要搅拌")]
        [Description("是否需要搅拌")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(21, "IsAgitation", "是否需要搅拌", null, "bit", 0, 0, false)]
        public virtual Boolean IsAgitation
        {
            get { return _IsAgitation; }
            set { if (OnPropertyChanging(__.IsAgitation, value)) { _IsAgitation = value; OnPropertyChanged(__.IsAgitation); } }
        }

        private Boolean _IsExcept;
        /// <summary>是否需要真空初除泡</summary>
        [DisplayName("是否需要真空初除泡")]
        [Description("是否需要真空初除泡")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(22, "IsExcept", "是否需要真空初除泡", null, "bit", 0, 0, false)]
        public virtual Boolean IsExcept
        {
            get { return _IsExcept; }
            set { if (OnPropertyChanging(__.IsExcept, value)) { _IsExcept = value; OnPropertyChanged(__.IsExcept); } }
        }

        private Int32 _WorkingHours;
        /// <summary>材料混合后可工作时间(单位:分钟)</summary>
        [DisplayName("材料混合后可工作时间单位:分钟")]
        [Description("材料混合后可工作时间(单位:分钟)")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(23, "WorkingHours", "材料混合后可工作时间(单位:分钟)", null, "int", 10, 0, false)]
        public virtual Int32 WorkingHours
        {
            get { return _WorkingHours; }
            set { if (OnPropertyChanging(__.WorkingHours, value)) { _WorkingHours = value; OnPropertyChanged(__.WorkingHours); } }
        }

        private Boolean _IsSolventName;
        /// <summary>有无溶剂名称</summary>
        [DisplayName("有无溶剂名称")]
        [Description("有无溶剂名称")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(24, "IsSolventName", "有无溶剂名称", null, "bit", 0, 0, false)]
        public virtual Boolean IsSolventName
        {
            get { return _IsSolventName; }
            set { if (OnPropertyChanging(__.IsSolventName, value)) { _IsSolventName = value; OnPropertyChanged(__.IsSolventName); } }
        }

        private String _Hardening;
        /// <summary>材料混合后完全硬化时间</summary>
        [DisplayName("材料混合后完全硬化时间")]
        [Description("材料混合后完全硬化时间")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(25, "Hardening", "材料混合后完全硬化时间", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Hardening
        {
            get { return _Hardening; }
            set { if (OnPropertyChanging(__.Hardening, value)) { _Hardening = value; OnPropertyChanged(__.Hardening); } }
        }

        private String _CementGroupB;
        /// <summary>B组胶水组别</summary>
        [DisplayName("B组胶水组别")]
        [Description("B组胶水组别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(26, "CementGroupB", "B组胶水组别", null, "nvarchar(50)", 0, 0, true)]
        public virtual String CementGroupB
        {
            get { return _CementGroupB; }
            set { if (OnPropertyChanging(__.CementGroupB, value)) { _CementGroupB = value; OnPropertyChanged(__.CementGroupB); } }
        }

        private String _ProductNoB;
        /// <summary>B组产品编号</summary>
        [DisplayName("B组产品编号")]
        [Description("B组产品编号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(27, "ProductNoB", "B组产品编号", null, "nvarchar(50)", 0, 0, true)]
        public virtual String ProductNoB
        {
            get { return _ProductNoB; }
            set { if (OnPropertyChanging(__.ProductNoB, value)) { _ProductNoB = value; OnPropertyChanged(__.ProductNoB); } }
        }

        private Double _WeightRatioB;
        /// <summary>B组重量比</summary>
        [DisplayName("B组重量比")]
        [Description("B组重量比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(28, "WeightRatioB", "B组重量比", null, "float", 53, 0, false)]
        public virtual Double WeightRatioB
        {
            get { return _WeightRatioB; }
            set { if (OnPropertyChanging(__.WeightRatioB, value)) { _WeightRatioB = value; OnPropertyChanged(__.WeightRatioB); } }
        }

        private Double _VolumeRatioB;
        /// <summary>B组体积比</summary>
        [DisplayName("B组体积比")]
        [Description("B组体积比")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(29, "VolumeRatioB", "B组体积比", null, "float", 53, 0, false)]
        public virtual Double VolumeRatioB
        {
            get { return _VolumeRatioB; }
            set { if (OnPropertyChanging(__.VolumeRatioB, value)) { _VolumeRatioB = value; OnPropertyChanged(__.VolumeRatioB); } }
        }

        private String _ViscosityB;
        /// <summary>B组黏稠度</summary>
        [DisplayName("B组黏稠度")]
        [Description("B组黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(30, "ViscosityB", "B组黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public virtual String ViscosityB
        {
            get { return _ViscosityB; }
            set { if (OnPropertyChanging(__.ViscosityB, value)) { _ViscosityB = value; OnPropertyChanged(__.ViscosityB); } }
        }

        private String _MixViscosityB;
        /// <summary>B组A/B混合后黏稠度</summary>
        [DisplayName("B组A_B混合后黏稠度")]
        [Description("B组A/B混合后黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(31, "MixViscosityB", "B组A/B混合后黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public virtual String MixViscosityB
        {
            get { return _MixViscosityB; }
            set { if (OnPropertyChanging(__.MixViscosityB, value)) { _MixViscosityB = value; OnPropertyChanged(__.MixViscosityB); } }
        }

        private String _SpecificGravityB;
        /// <summary>B组比重</summary>
        [DisplayName("B组比重")]
        [Description("B组比重")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(32, "SpecificGravityB", "B组比重", null, "nvarchar(50)", 0, 0, true)]
        public virtual String SpecificGravityB
        {
            get { return _SpecificGravityB; }
            set { if (OnPropertyChanging(__.SpecificGravityB, value)) { _SpecificGravityB = value; OnPropertyChanged(__.SpecificGravityB); } }
        }

        private String _TemperatureB;
        /// <summary>B组工作温度</summary>
        [DisplayName("B组工作温度")]
        [Description("B组工作温度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(33, "TemperatureB", "B组工作温度", null, "nvarchar(50)", 0, 0, true)]
        public virtual String TemperatureB
        {
            get { return _TemperatureB; }
            set { if (OnPropertyChanging(__.TemperatureB, value)) { _TemperatureB = value; OnPropertyChanged(__.TemperatureB); } }
        }

        private String _WViscosityB;
        /// <summary>B组工作温度下的黏稠度</summary>
        [DisplayName("B组工作温度下的黏稠度")]
        [Description("B组工作温度下的黏稠度")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(34, "WViscosityB", "B组工作温度下的黏稠度", null, "nvarchar(50)", 0, 0, true)]
        public virtual String WViscosityB
        {
            get { return _WViscosityB; }
            set { if (OnPropertyChanging(__.WViscosityB, value)) { _WViscosityB = value; OnPropertyChanged(__.WViscosityB); } }
        }

        private Boolean _IsFillersB;
        /// <summary>B组是否有填充剂</summary>
        [DisplayName("B组是否有填充剂")]
        [Description("B组是否有填充剂")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(35, "IsFillersB", "B组是否有填充剂", null, "bit", 0, 0, false)]
        public virtual Boolean IsFillersB
        {
            get { return _IsFillersB; }
            set { if (OnPropertyChanging(__.IsFillersB, value)) { _IsFillersB = value; OnPropertyChanged(__.IsFillersB); } }
        }

        private String _FillersTypeB;
        /// <summary>B组填充剂类型</summary>
        [DisplayName("B组填充剂类型")]
        [Description("B组填充剂类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(36, "FillersTypeB", "B组填充剂类型", null, "nvarchar(50)", 0, 0, true)]
        public virtual String FillersTypeB
        {
            get { return _FillersTypeB; }
            set { if (OnPropertyChanging(__.FillersTypeB, value)) { _FillersTypeB = value; OnPropertyChanged(__.FillersTypeB); } }
        }

        private Double _FillersAmountB;
        /// <summary>B组填充剂分量</summary>
        [DisplayName("B组填充剂分量")]
        [Description("B组填充剂分量")]
        [DataObjectField(false, false, true, 53)]
        [BindColumn(37, "FillersAmountB", "B组填充剂分量", null, "float", 53, 0, false)]
        public virtual Double FillersAmountB
        {
            get { return _FillersAmountB; }
            set { if (OnPropertyChanging(__.FillersAmountB, value)) { _FillersAmountB = value; OnPropertyChanged(__.FillersAmountB); } }
        }

        private Boolean _IsAbradabilityB;
        /// <summary>B组是否磨损</summary>
        [DisplayName("B组是否磨损")]
        [Description("B组是否磨损")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(38, "IsAbradabilityB", "B组是否磨损", null, "bit", 0, 0, false)]
        public virtual Boolean IsAbradabilityB
        {
            get { return _IsAbradabilityB; }
            set { if (OnPropertyChanging(__.IsAbradabilityB, value)) { _IsAbradabilityB = value; OnPropertyChanged(__.IsAbradabilityB); } }
        }

        private Boolean _IsCorrosivityB;
        /// <summary>B组是否腐蚀</summary>
        [DisplayName("B组是否腐蚀")]
        [Description("B组是否腐蚀")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(39, "IsCorrosivityB", "B组是否腐蚀", null, "bit", 0, 0, false)]
        public virtual Boolean IsCorrosivityB
        {
            get { return _IsCorrosivityB; }
            set { if (OnPropertyChanging(__.IsCorrosivityB, value)) { _IsCorrosivityB = value; OnPropertyChanged(__.IsCorrosivityB); } }
        }

        private Boolean _IsSensitivityB;
        /// <summary>B组材料是否潮湿敏感</summary>
        [DisplayName("B组材料是否潮湿敏感")]
        [Description("B组材料是否潮湿敏感")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(40, "IsSensitivityB", "B组材料是否潮湿敏感", null, "bit", 0, 0, false)]
        public virtual Boolean IsSensitivityB
        {
            get { return _IsSensitivityB; }
            set { if (OnPropertyChanging(__.IsSensitivityB, value)) { _IsSensitivityB = value; OnPropertyChanged(__.IsSensitivityB); } }
        }

        private Boolean _IsAgitationB;
        /// <summary>B组是否需要搅拌</summary>
        [DisplayName("B组是否需要搅拌")]
        [Description("B组是否需要搅拌")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(41, "IsAgitationB", "B组是否需要搅拌", null, "bit", 0, 0, false)]
        public virtual Boolean IsAgitationB
        {
            get { return _IsAgitationB; }
            set { if (OnPropertyChanging(__.IsAgitationB, value)) { _IsAgitationB = value; OnPropertyChanged(__.IsAgitationB); } }
        }

        private Boolean _IsExceptB;
        /// <summary>B组是否需要真空初除泡</summary>
        [DisplayName("B组是否需要真空初除泡")]
        [Description("B组是否需要真空初除泡")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(42, "IsExceptB", "B组是否需要真空初除泡", null, "bit", 0, 0, false)]
        public virtual Boolean IsExceptB
        {
            get { return _IsExceptB; }
            set { if (OnPropertyChanging(__.IsExceptB, value)) { _IsExceptB = value; OnPropertyChanged(__.IsExceptB); } }
        }

        private Int32 _WorkingHoursB;
        /// <summary>B组材料混合后可工作时间(单位:分钟)</summary>
        [DisplayName("B组材料混合后可工作时间单位:分钟")]
        [Description("B组材料混合后可工作时间(单位:分钟)")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(43, "WorkingHoursB", "B组材料混合后可工作时间(单位:分钟)", null, "int", 10, 0, false)]
        public virtual Int32 WorkingHoursB
        {
            get { return _WorkingHoursB; }
            set { if (OnPropertyChanging(__.WorkingHoursB, value)) { _WorkingHoursB = value; OnPropertyChanged(__.WorkingHoursB); } }
        }

        private Boolean _IsSolventNameB;
        /// <summary>B组有无溶剂名称</summary>
        [DisplayName("B组有无溶剂名称")]
        [Description("B组有无溶剂名称")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(44, "IsSolventNameB", "B组有无溶剂名称", null, "bit", 0, 0, false)]
        public virtual Boolean IsSolventNameB
        {
            get { return _IsSolventNameB; }
            set { if (OnPropertyChanging(__.IsSolventNameB, value)) { _IsSolventNameB = value; OnPropertyChanged(__.IsSolventNameB); } }
        }

        private String _HardeningB;
        /// <summary>B组材料混合后完全硬化时间</summary>
        [DisplayName("B组材料混合后完全硬化时间")]
        [Description("B组材料混合后完全硬化时间")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(45, "HardeningB", "B组材料混合后完全硬化时间", null, "nvarchar(50)", 0, 0, true)]
        public virtual String HardeningB
        {
            get { return _HardeningB; }
            set { if (OnPropertyChanging(__.HardeningB, value)) { _HardeningB = value; OnPropertyChanged(__.HardeningB); } }
        }

        private DateTime _AddTime;
        /// <summary>发布时间</summary>
        [DisplayName("发布时间")]
        [Description("发布时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(46, "AddTime", "发布时间", null, "datetime", 3, 0, false)]
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
        [BindColumn(47, "Remark", "备注", null, "nvarchar(100)", 0, 0, true)]
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
                    case __.CustomerID : return _CustomerID;
                    case __.Manufacturer : return _Manufacturer;
                    case __.Tel : return _Tel;
                    case __.Address : return _Address;
                    case __.CementGroup : return _CementGroup;
                    case __.ProductNo : return _ProductNo;
                    case __.WeightRatio : return _WeightRatio;
                    case __.VolumeRatio : return _VolumeRatio;
                    case __.Viscosity : return _Viscosity;
                    case __.MixViscosity : return _MixViscosity;
                    case __.SpecificGravity : return _SpecificGravity;
                    case __.Temperature : return _Temperature;
                    case __.WViscosity : return _WViscosity;
                    case __.IsFillers : return _IsFillers;
                    case __.FillersType : return _FillersType;
                    case __.FillersAmount : return _FillersAmount;
                    case __.IsAbradability : return _IsAbradability;
                    case __.IsCorrosivity : return _IsCorrosivity;
                    case __.IsSensitivity : return _IsSensitivity;
                    case __.IsAgitation : return _IsAgitation;
                    case __.IsExcept : return _IsExcept;
                    case __.WorkingHours : return _WorkingHours;
                    case __.IsSolventName : return _IsSolventName;
                    case __.Hardening : return _Hardening;
                    case __.CementGroupB : return _CementGroupB;
                    case __.ProductNoB : return _ProductNoB;
                    case __.WeightRatioB : return _WeightRatioB;
                    case __.VolumeRatioB : return _VolumeRatioB;
                    case __.ViscosityB : return _ViscosityB;
                    case __.MixViscosityB : return _MixViscosityB;
                    case __.SpecificGravityB : return _SpecificGravityB;
                    case __.TemperatureB : return _TemperatureB;
                    case __.WViscosityB : return _WViscosityB;
                    case __.IsFillersB : return _IsFillersB;
                    case __.FillersTypeB : return _FillersTypeB;
                    case __.FillersAmountB : return _FillersAmountB;
                    case __.IsAbradabilityB : return _IsAbradabilityB;
                    case __.IsCorrosivityB : return _IsCorrosivityB;
                    case __.IsSensitivityB : return _IsSensitivityB;
                    case __.IsAgitationB : return _IsAgitationB;
                    case __.IsExceptB : return _IsExceptB;
                    case __.WorkingHoursB : return _WorkingHoursB;
                    case __.IsSolventNameB : return _IsSolventNameB;
                    case __.HardeningB : return _HardeningB;
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
                    case __.CustomerID : _CustomerID = Convert.ToInt32(value); break;
                    case __.Manufacturer : _Manufacturer = Convert.ToString(value); break;
                    case __.Tel : _Tel = Convert.ToString(value); break;
                    case __.Address : _Address = Convert.ToString(value); break;
                    case __.CementGroup : _CementGroup = Convert.ToString(value); break;
                    case __.ProductNo : _ProductNo = Convert.ToString(value); break;
                    case __.WeightRatio : _WeightRatio = Convert.ToDouble(value); break;
                    case __.VolumeRatio : _VolumeRatio = Convert.ToDouble(value); break;
                    case __.Viscosity : _Viscosity = Convert.ToString(value); break;
                    case __.MixViscosity : _MixViscosity = Convert.ToString(value); break;
                    case __.SpecificGravity : _SpecificGravity = Convert.ToString(value); break;
                    case __.Temperature : _Temperature = Convert.ToString(value); break;
                    case __.WViscosity : _WViscosity = Convert.ToString(value); break;
                    case __.IsFillers : _IsFillers = Convert.ToBoolean(value); break;
                    case __.FillersType : _FillersType = Convert.ToString(value); break;
                    case __.FillersAmount : _FillersAmount = Convert.ToDouble(value); break;
                    case __.IsAbradability : _IsAbradability = Convert.ToBoolean(value); break;
                    case __.IsCorrosivity : _IsCorrosivity = Convert.ToBoolean(value); break;
                    case __.IsSensitivity : _IsSensitivity = Convert.ToBoolean(value); break;
                    case __.IsAgitation : _IsAgitation = Convert.ToBoolean(value); break;
                    case __.IsExcept : _IsExcept = Convert.ToBoolean(value); break;
                    case __.WorkingHours : _WorkingHours = Convert.ToInt32(value); break;
                    case __.IsSolventName : _IsSolventName = Convert.ToBoolean(value); break;
                    case __.Hardening : _Hardening = Convert.ToString(value); break;
                    case __.CementGroupB : _CementGroupB = Convert.ToString(value); break;
                    case __.ProductNoB : _ProductNoB = Convert.ToString(value); break;
                    case __.WeightRatioB : _WeightRatioB = Convert.ToDouble(value); break;
                    case __.VolumeRatioB : _VolumeRatioB = Convert.ToDouble(value); break;
                    case __.ViscosityB : _ViscosityB = Convert.ToString(value); break;
                    case __.MixViscosityB : _MixViscosityB = Convert.ToString(value); break;
                    case __.SpecificGravityB : _SpecificGravityB = Convert.ToString(value); break;
                    case __.TemperatureB : _TemperatureB = Convert.ToString(value); break;
                    case __.WViscosityB : _WViscosityB = Convert.ToString(value); break;
                    case __.IsFillersB : _IsFillersB = Convert.ToBoolean(value); break;
                    case __.FillersTypeB : _FillersTypeB = Convert.ToString(value); break;
                    case __.FillersAmountB : _FillersAmountB = Convert.ToDouble(value); break;
                    case __.IsAbradabilityB : _IsAbradabilityB = Convert.ToBoolean(value); break;
                    case __.IsCorrosivityB : _IsCorrosivityB = Convert.ToBoolean(value); break;
                    case __.IsSensitivityB : _IsSensitivityB = Convert.ToBoolean(value); break;
                    case __.IsAgitationB : _IsAgitationB = Convert.ToBoolean(value); break;
                    case __.IsExceptB : _IsExceptB = Convert.ToBoolean(value); break;
                    case __.WorkingHoursB : _WorkingHoursB = Convert.ToInt32(value); break;
                    case __.IsSolventNameB : _IsSolventNameB = Convert.ToBoolean(value); break;
                    case __.HardeningB : _HardeningB = Convert.ToString(value); break;
                    case __.AddTime : _AddTime = Convert.ToDateTime(value); break;
                    case __.Remark : _Remark = Convert.ToString(value); break;
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
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>客户ID</summary>
            public static readonly Field CustomerID = FindByName(__.CustomerID);

            ///<summary>制造商</summary>
            public static readonly Field Manufacturer = FindByName(__.Manufacturer);

            ///<summary>联系电话</summary>
            public static readonly Field Tel = FindByName(__.Tel);

            ///<summary>联系地址</summary>
            public static readonly Field Address = FindByName(__.Address);

            ///<summary>胶水组别</summary>
            public static readonly Field CementGroup = FindByName(__.CementGroup);

            ///<summary>产品编号</summary>
            public static readonly Field ProductNo = FindByName(__.ProductNo);

            ///<summary>重量比</summary>
            public static readonly Field WeightRatio = FindByName(__.WeightRatio);

            ///<summary>体积比</summary>
            public static readonly Field VolumeRatio = FindByName(__.VolumeRatio);

            ///<summary>黏稠度</summary>
            public static readonly Field Viscosity = FindByName(__.Viscosity);

            ///<summary>A/B混合后黏稠度</summary>
            public static readonly Field MixViscosity = FindByName(__.MixViscosity);

            ///<summary>比重</summary>
            public static readonly Field SpecificGravity = FindByName(__.SpecificGravity);

            ///<summary>工作温度</summary>
            public static readonly Field Temperature = FindByName(__.Temperature);

            ///<summary>工作温度下的黏稠度</summary>
            public static readonly Field WViscosity = FindByName(__.WViscosity);

            ///<summary>是否有填充剂</summary>
            public static readonly Field IsFillers = FindByName(__.IsFillers);

            ///<summary>填充剂类型</summary>
            public static readonly Field FillersType = FindByName(__.FillersType);

            ///<summary>填充剂分量</summary>
            public static readonly Field FillersAmount = FindByName(__.FillersAmount);

            ///<summary>是否磨损</summary>
            public static readonly Field IsAbradability = FindByName(__.IsAbradability);

            ///<summary>是否腐蚀</summary>
            public static readonly Field IsCorrosivity = FindByName(__.IsCorrosivity);

            ///<summary>材料是否潮湿敏感</summary>
            public static readonly Field IsSensitivity = FindByName(__.IsSensitivity);

            ///<summary>是否需要搅拌</summary>
            public static readonly Field IsAgitation = FindByName(__.IsAgitation);

            ///<summary>是否需要真空初除泡</summary>
            public static readonly Field IsExcept = FindByName(__.IsExcept);

            ///<summary>材料混合后可工作时间(单位:分钟)</summary>
            public static readonly Field WorkingHours = FindByName(__.WorkingHours);

            ///<summary>有无溶剂名称</summary>
            public static readonly Field IsSolventName = FindByName(__.IsSolventName);

            ///<summary>材料混合后完全硬化时间</summary>
            public static readonly Field Hardening = FindByName(__.Hardening);

            ///<summary>B组胶水组别</summary>
            public static readonly Field CementGroupB = FindByName(__.CementGroupB);

            ///<summary>B组产品编号</summary>
            public static readonly Field ProductNoB = FindByName(__.ProductNoB);

            ///<summary>B组重量比</summary>
            public static readonly Field WeightRatioB = FindByName(__.WeightRatioB);

            ///<summary>B组体积比</summary>
            public static readonly Field VolumeRatioB = FindByName(__.VolumeRatioB);

            ///<summary>B组黏稠度</summary>
            public static readonly Field ViscosityB = FindByName(__.ViscosityB);

            ///<summary>B组A/B混合后黏稠度</summary>
            public static readonly Field MixViscosityB = FindByName(__.MixViscosityB);

            ///<summary>B组比重</summary>
            public static readonly Field SpecificGravityB = FindByName(__.SpecificGravityB);

            ///<summary>B组工作温度</summary>
            public static readonly Field TemperatureB = FindByName(__.TemperatureB);

            ///<summary>B组工作温度下的黏稠度</summary>
            public static readonly Field WViscosityB = FindByName(__.WViscosityB);

            ///<summary>B组是否有填充剂</summary>
            public static readonly Field IsFillersB = FindByName(__.IsFillersB);

            ///<summary>B组填充剂类型</summary>
            public static readonly Field FillersTypeB = FindByName(__.FillersTypeB);

            ///<summary>B组填充剂分量</summary>
            public static readonly Field FillersAmountB = FindByName(__.FillersAmountB);

            ///<summary>B组是否磨损</summary>
            public static readonly Field IsAbradabilityB = FindByName(__.IsAbradabilityB);

            ///<summary>B组是否腐蚀</summary>
            public static readonly Field IsCorrosivityB = FindByName(__.IsCorrosivityB);

            ///<summary>B组材料是否潮湿敏感</summary>
            public static readonly Field IsSensitivityB = FindByName(__.IsSensitivityB);

            ///<summary>B组是否需要搅拌</summary>
            public static readonly Field IsAgitationB = FindByName(__.IsAgitationB);

            ///<summary>B组是否需要真空初除泡</summary>
            public static readonly Field IsExceptB = FindByName(__.IsExceptB);

            ///<summary>B组材料混合后可工作时间(单位:分钟)</summary>
            public static readonly Field WorkingHoursB = FindByName(__.WorkingHoursB);

            ///<summary>B组有无溶剂名称</summary>
            public static readonly Field IsSolventNameB = FindByName(__.IsSolventNameB);

            ///<summary>B组材料混合后完全硬化时间</summary>
            public static readonly Field HardeningB = FindByName(__.HardeningB);

            ///<summary>发布时间</summary>
            public static readonly Field AddTime = FindByName(__.AddTime);

            ///<summary>备注</summary>
            public static readonly Field Remark = FindByName(__.Remark);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得液料规格字段名称的快捷方式</summary>
        class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>客户ID</summary>
            public const String CustomerID = "CustomerID";

            ///<summary>制造商</summary>
            public const String Manufacturer = "Manufacturer";

            ///<summary>联系电话</summary>
            public const String Tel = "Tel";

            ///<summary>联系地址</summary>
            public const String Address = "Address";

            ///<summary>胶水组别</summary>
            public const String CementGroup = "CementGroup";

            ///<summary>产品编号</summary>
            public const String ProductNo = "ProductNo";

            ///<summary>重量比</summary>
            public const String WeightRatio = "WeightRatio";

            ///<summary>体积比</summary>
            public const String VolumeRatio = "VolumeRatio";

            ///<summary>黏稠度</summary>
            public const String Viscosity = "Viscosity";

            ///<summary>A/B混合后黏稠度</summary>
            public const String MixViscosity = "MixViscosity";

            ///<summary>比重</summary>
            public const String SpecificGravity = "SpecificGravity";

            ///<summary>工作温度</summary>
            public const String Temperature = "Temperature";

            ///<summary>工作温度下的黏稠度</summary>
            public const String WViscosity = "WViscosity";

            ///<summary>是否有填充剂</summary>
            public const String IsFillers = "IsFillers";

            ///<summary>填充剂类型</summary>
            public const String FillersType = "FillersType";

            ///<summary>填充剂分量</summary>
            public const String FillersAmount = "FillersAmount";

            ///<summary>是否磨损</summary>
            public const String IsAbradability = "IsAbradability";

            ///<summary>是否腐蚀</summary>
            public const String IsCorrosivity = "IsCorrosivity";

            ///<summary>材料是否潮湿敏感</summary>
            public const String IsSensitivity = "IsSensitivity";

            ///<summary>是否需要搅拌</summary>
            public const String IsAgitation = "IsAgitation";

            ///<summary>是否需要真空初除泡</summary>
            public const String IsExcept = "IsExcept";

            ///<summary>材料混合后可工作时间(单位:分钟)</summary>
            public const String WorkingHours = "WorkingHours";

            ///<summary>有无溶剂名称</summary>
            public const String IsSolventName = "IsSolventName";

            ///<summary>材料混合后完全硬化时间</summary>
            public const String Hardening = "Hardening";

            ///<summary>B组胶水组别</summary>
            public const String CementGroupB = "CementGroupB";

            ///<summary>B组产品编号</summary>
            public const String ProductNoB = "ProductNoB";

            ///<summary>B组重量比</summary>
            public const String WeightRatioB = "WeightRatioB";

            ///<summary>B组体积比</summary>
            public const String VolumeRatioB = "VolumeRatioB";

            ///<summary>B组黏稠度</summary>
            public const String ViscosityB = "ViscosityB";

            ///<summary>B组A/B混合后黏稠度</summary>
            public const String MixViscosityB = "MixViscosityB";

            ///<summary>B组比重</summary>
            public const String SpecificGravityB = "SpecificGravityB";

            ///<summary>B组工作温度</summary>
            public const String TemperatureB = "TemperatureB";

            ///<summary>B组工作温度下的黏稠度</summary>
            public const String WViscosityB = "WViscosityB";

            ///<summary>B组是否有填充剂</summary>
            public const String IsFillersB = "IsFillersB";

            ///<summary>B组填充剂类型</summary>
            public const String FillersTypeB = "FillersTypeB";

            ///<summary>B组填充剂分量</summary>
            public const String FillersAmountB = "FillersAmountB";

            ///<summary>B组是否磨损</summary>
            public const String IsAbradabilityB = "IsAbradabilityB";

            ///<summary>B组是否腐蚀</summary>
            public const String IsCorrosivityB = "IsCorrosivityB";

            ///<summary>B组材料是否潮湿敏感</summary>
            public const String IsSensitivityB = "IsSensitivityB";

            ///<summary>B组是否需要搅拌</summary>
            public const String IsAgitationB = "IsAgitationB";

            ///<summary>B组是否需要真空初除泡</summary>
            public const String IsExceptB = "IsExceptB";

            ///<summary>B组材料混合后可工作时间(单位:分钟)</summary>
            public const String WorkingHoursB = "WorkingHoursB";

            ///<summary>B组有无溶剂名称</summary>
            public const String IsSolventNameB = "IsSolventNameB";

            ///<summary>B组材料混合后完全硬化时间</summary>
            public const String HardeningB = "HardeningB";

            ///<summary>发布时间</summary>
            public const String AddTime = "AddTime";

            ///<summary>备注</summary>
            public const String Remark = "Remark";

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