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
    /// <summary>汽车指标。时序数据</summary>
    [Serializable]
    [DataObject]
    [Description("汽车指标。时序数据")]
    [BindTable("Car_Meter", Description = "汽车指标。时序数据", ConnName = "test", DbType = DatabaseType.None)]
    public partial class CarMeter
    {
        #region 属性
        private DateTime _Ts;
        /// <summary>时间戳</summary>
        [DisplayName("时间戳")]
        [Description("时间戳")]
        [DataObjectField(true, false, true, 0)]
        [BindColumn("Ts", "时间戳", "")]
        public DateTime Ts { get => _Ts; set { if (OnPropertyChanging("Ts", value)) { _Ts = value; OnPropertyChanged("Ts"); } } }

        private Int32 _Speed;
        /// <summary>速度</summary>
        [DisplayName("速度")]
        [Description("速度")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Speed", "速度", "")]
        public Int32 Speed { get => _Speed; set { if (OnPropertyChanging("Speed", value)) { _Speed = value; OnPropertyChanged("Speed"); } } }

        private Single _Temp;
        /// <summary>温度</summary>
        [DisplayName("温度")]
        [Description("温度")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Temp", "温度", "")]
        public Single Temp { get => _Temp; set { if (OnPropertyChanging("Temp", value)) { _Temp = value; OnPropertyChanged("Temp"); } } }
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
                    case "Speed": return _Speed;
                    case "Temp": return _Temp;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Ts": _Ts = value.ToDateTime(); break;
                    case "Speed": _Speed = value.ToInt(); break;
                    case "Temp": _Temp = Convert.ToSingle(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得汽车指标字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>时间戳</summary>
            public static readonly Field Ts = FindByName("Ts");

            /// <summary>速度</summary>
            public static readonly Field Speed = FindByName("Speed");

            /// <summary>温度</summary>
            public static readonly Field Temp = FindByName("Temp");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得汽车指标字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>时间戳</summary>
            public const String Ts = "Ts";

            /// <summary>速度</summary>
            public const String Speed = "Speed";

            /// <summary>温度</summary>
            public const String Temp = "Temp";
        }
        #endregion
    }
}