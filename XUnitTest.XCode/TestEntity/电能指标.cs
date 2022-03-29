using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XUnitTest.XCode.TestEntity
{
    /// <summary>电能指标。时序数据</summary>
    [Serializable]
    [DataObject]
    [Description("电能指标。时序数据")]
    [BindTable("power_meter", Description = "电能指标。时序数据", ConnName = "test", DbType = DatabaseType.None)]
    public partial class PowerMeter
    {
        #region 属性
        private DateTime _Ts;
        /// <summary>时间戳</summary>
        [DisplayName("时间戳")]
        [Description("时间戳")]
        [DataObjectField(true, false, true, 0)]
        [BindColumn("Ts", "时间戳", "")]
        public DateTime Ts { get => _Ts; set { if (OnPropertyChanging("Ts", value)) { _Ts = value; OnPropertyChanged("Ts"); } } }

        private Single _Current;
        /// <summary>电流</summary>
        [DisplayName("电流")]
        [Description("电流")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Current", "电流", "")]
        public Single Current { get => _Current; set { if (OnPropertyChanging("Current", value)) { _Current = value; OnPropertyChanged("Current"); } } }

        private Int32 _Voltage;
        /// <summary>电压</summary>
        [DisplayName("电压")]
        [Description("电压")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Voltage", "电压", "")]
        public Int32 Voltage { get => _Voltage; set { if (OnPropertyChanging("Voltage", value)) { _Voltage = value; OnPropertyChanged("Voltage"); } } }

        private Single _Phase;
        /// <summary>相位</summary>
        [DisplayName("相位")]
        [Description("相位")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Phase", "相位", "")]
        public Single Phase { get => _Phase; set { if (OnPropertyChanging("Phase", value)) { _Phase = value; OnPropertyChanged("Phase"); } } }

        private String _Location;
        /// <summary>位置</summary>
        [DisplayName("位置")]
        [Description("位置")]
        [DataObjectField(false, false, true, 64)]
        [BindColumn("Location", "位置", "", Master = true)]
        public String Location { get => _Location; set { if (OnPropertyChanging("Location", value)) { _Location = value; OnPropertyChanged("Location"); } } }

        private Int32 _GroupId;
        /// <summary>分组</summary>
        [DisplayName("分组")]
        [Description("分组")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("GroupId", "分组", "", Master = true)]
        public Int32 GroupId { get => _GroupId; set { if (OnPropertyChanging("GroupId", value)) { _GroupId = value; OnPropertyChanged("GroupId"); } } }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case "Ts": return _Ts;
                    case "Current": return _Current;
                    case "Voltage": return _Voltage;
                    case "Phase": return _Phase;
                    case "Location": return _Location;
                    case "GroupId": return _GroupId;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Ts": _Ts = value.ToDateTime(); break;
                    case "Current": _Current = Convert.ToSingle(value); break;
                    case "Voltage": _Voltage = value.ToInt(); break;
                    case "Phase": _Phase = Convert.ToSingle(value); break;
                    case "Location": _Location = Convert.ToString(value); break;
                    case "GroupId": _GroupId = value.ToInt(); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得电能指标字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>时间戳</summary>
            public static readonly Field Ts = FindByName("Ts");

            /// <summary>电流</summary>
            public static readonly Field Current = FindByName("Current");

            /// <summary>电压</summary>
            public static readonly Field Voltage = FindByName("Voltage");

            /// <summary>相位</summary>
            public static readonly Field Phase = FindByName("Phase");

            /// <summary>位置</summary>
            public static readonly Field Location = FindByName("Location");

            /// <summary>分组</summary>
            public static readonly Field GroupId = FindByName("GroupId");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得电能指标字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>时间戳</summary>
            public const String Ts = "Ts";

            /// <summary>电流</summary>
            public const String Current = "Current";

            /// <summary>电压</summary>
            public const String Voltage = "Voltage";

            /// <summary>相位</summary>
            public const String Phase = "Phase";

            /// <summary>位置</summary>
            public const String Location = "Location";

            /// <summary>分组</summary>
            public const String GroupId = "GroupId";
        }
        #endregion
    }
}