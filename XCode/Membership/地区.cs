using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Membership
{
    /// <summary>地区。行政区划数据，最高支持四级地址，9位数字</summary>
    [Serializable]
    [DataObject]
    [Description("地区。行政区划数据，最高支持四级地址，9位数字")]
    [BindIndex("IX_Area_ParentID", false, "ParentID")]
    [BindIndex("IX_Area_Name", false, "Name")]
    [BindIndex("IX_Area_PinYin", false, "PinYin")]
    [BindIndex("IX_Area_JianPin", false, "JianPin")]
    [BindIndex("IX_Area_GeoHash", false, "GeoHash")]
    [BindIndex("IX_Area_UpdateTime_ID", false, "UpdateTime,ID")]
    [BindTable("Area", Description = "地区。行政区划数据，最高支持四级地址，9位数字", ConnName = "Membership", DbType = DatabaseType.None)]
    public partial class Area
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编码。行政区划编码</summary>
        [DisplayName("编码")]
        [Description("编码。行政区划编码")]
        [DataObjectField(true, false, false, 0)]
        [BindColumn("ID", "编码。行政区划编码", "")]
        public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private String _FullName;
        /// <summary>全名</summary>
        [DisplayName("全名")]
        [Description("全名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("FullName", "全名", "", Master = true)]
        public String FullName { get => _FullName; set { if (OnPropertyChanging("FullName", value)) { _FullName = value; OnPropertyChanged("FullName"); } } }

        private Int32 _ParentID;
        /// <summary>父级</summary>
        [DisplayName("父级")]
        [Description("父级")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ParentID", "父级", "")]
        public Int32 ParentID { get => _ParentID; set { if (OnPropertyChanging("ParentID", value)) { _ParentID = value; OnPropertyChanged("ParentID"); } } }

        private Int32 _Level;
        /// <summary>层级</summary>
        [DisplayName("层级")]
        [Description("层级")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Level", "层级", "")]
        public Int32 Level { get => _Level; set { if (OnPropertyChanging("Level", value)) { _Level = value; OnPropertyChanged("Level"); } } }

        private String _Kind;
        /// <summary>类型。省市县，自治州等</summary>
        [DisplayName("类型")]
        [Description("类型。省市县，自治州等")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Kind", "类型。省市县，自治州等", "")]
        public String Kind { get => _Kind; set { if (OnPropertyChanging("Kind", value)) { _Kind = value; OnPropertyChanged("Kind"); } } }

        private String _English;
        /// <summary>英文名</summary>
        [DisplayName("英文名")]
        [Description("英文名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("English", "英文名", "")]
        public String English { get => _English; set { if (OnPropertyChanging("English", value)) { _English = value; OnPropertyChanged("English"); } } }

        private String _PinYin;
        /// <summary>拼音</summary>
        [DisplayName("拼音")]
        [Description("拼音")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("PinYin", "拼音", "")]
        public String PinYin { get => _PinYin; set { if (OnPropertyChanging("PinYin", value)) { _PinYin = value; OnPropertyChanged("PinYin"); } } }

        private String _JianPin;
        /// <summary>简拼</summary>
        [DisplayName("简拼")]
        [Description("简拼")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("JianPin", "简拼", "")]
        public String JianPin { get => _JianPin; set { if (OnPropertyChanging("JianPin", value)) { _JianPin = value; OnPropertyChanged("JianPin"); } } }

        private String _TelCode;
        /// <summary>区号。电话区号</summary>
        [DisplayName("区号")]
        [Description("区号。电话区号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("TelCode", "区号。电话区号", "")]
        public String TelCode { get => _TelCode; set { if (OnPropertyChanging("TelCode", value)) { _TelCode = value; OnPropertyChanged("TelCode"); } } }

        private String _ZipCode;
        /// <summary>邮编。邮政编码</summary>
        [DisplayName("邮编")]
        [Description("邮编。邮政编码")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("ZipCode", "邮编。邮政编码", "")]
        public String ZipCode { get => _ZipCode; set { if (OnPropertyChanging("ZipCode", value)) { _ZipCode = value; OnPropertyChanged("ZipCode"); } } }

        private Double _Longitude;
        /// <summary>经度</summary>
        [DisplayName("经度")]
        [Description("经度")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Longitude", "经度", "")]
        public Double Longitude { get => _Longitude; set { if (OnPropertyChanging("Longitude", value)) { _Longitude = value; OnPropertyChanged("Longitude"); } } }

        private Double _Latitude;
        /// <summary>纬度</summary>
        [DisplayName("纬度")]
        [Description("纬度")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Latitude", "纬度", "")]
        public Double Latitude { get => _Latitude; set { if (OnPropertyChanging("Latitude", value)) { _Latitude = value; OnPropertyChanged("Latitude"); } } }

        private String _GeoHash;
        /// <summary>地址编码。字符串前缀相同越多，地理距离越近，8位精度19米，6位610米</summary>
        [DisplayName("地址编码")]
        [Description("地址编码。字符串前缀相同越多，地理距离越近，8位精度19米，6位610米")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("GeoHash", "地址编码。字符串前缀相同越多，地理距离越近，8位精度19米，6位610米", "")]
        public String GeoHash { get => _GeoHash; set { if (OnPropertyChanging("GeoHash", value)) { _GeoHash = value; OnPropertyChanged("GeoHash"); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Remark", "备注", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
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
                    case "ID": return _ID;
                    case "Name": return _Name;
                    case "FullName": return _FullName;
                    case "ParentID": return _ParentID;
                    case "Level": return _Level;
                    case "Kind": return _Kind;
                    case "English": return _English;
                    case "PinYin": return _PinYin;
                    case "JianPin": return _JianPin;
                    case "TelCode": return _TelCode;
                    case "ZipCode": return _ZipCode;
                    case "Longitude": return _Longitude;
                    case "Latitude": return _Latitude;
                    case "GeoHash": return _GeoHash;
                    case "Enable": return _Enable;
                    case "CreateTime": return _CreateTime;
                    case "UpdateTime": return _UpdateTime;
                    case "Remark": return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "FullName": _FullName = Convert.ToString(value); break;
                    case "ParentID": _ParentID = value.ToInt(); break;
                    case "Level": _Level = value.ToInt(); break;
                    case "Kind": _Kind = Convert.ToString(value); break;
                    case "English": _English = Convert.ToString(value); break;
                    case "PinYin": _PinYin = Convert.ToString(value); break;
                    case "JianPin": _JianPin = Convert.ToString(value); break;
                    case "TelCode": _TelCode = Convert.ToString(value); break;
                    case "ZipCode": _ZipCode = Convert.ToString(value); break;
                    case "Longitude": _Longitude = value.ToDouble(); break;
                    case "Latitude": _Latitude = value.ToDouble(); break;
                    case "GeoHash": _GeoHash = Convert.ToString(value); break;
                    case "Enable": _Enable = value.ToBoolean(); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得地区字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编码。行政区划编码</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>全名</summary>
            public static readonly Field FullName = FindByName("FullName");

            /// <summary>父级</summary>
            public static readonly Field ParentID = FindByName("ParentID");

            /// <summary>层级</summary>
            public static readonly Field Level = FindByName("Level");

            /// <summary>类型。省市县，自治州等</summary>
            public static readonly Field Kind = FindByName("Kind");

            /// <summary>英文名</summary>
            public static readonly Field English = FindByName("English");

            /// <summary>拼音</summary>
            public static readonly Field PinYin = FindByName("PinYin");

            /// <summary>简拼</summary>
            public static readonly Field JianPin = FindByName("JianPin");

            /// <summary>区号。电话区号</summary>
            public static readonly Field TelCode = FindByName("TelCode");

            /// <summary>邮编。邮政编码</summary>
            public static readonly Field ZipCode = FindByName("ZipCode");

            /// <summary>经度</summary>
            public static readonly Field Longitude = FindByName("Longitude");

            /// <summary>纬度</summary>
            public static readonly Field Latitude = FindByName("Latitude");

            /// <summary>地址编码。字符串前缀相同越多，地理距离越近，8位精度19米，6位610米</summary>
            public static readonly Field GeoHash = FindByName("GeoHash");

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName("Enable");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            /// <summary>备注</summary>
            public static readonly Field Remark = FindByName("Remark");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得地区字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编码。行政区划编码</summary>
            public const String ID = "ID";

            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>全名</summary>
            public const String FullName = "FullName";

            /// <summary>父级</summary>
            public const String ParentID = "ParentID";

            /// <summary>层级</summary>
            public const String Level = "Level";

            /// <summary>类型。省市县，自治州等</summary>
            public const String Kind = "Kind";

            /// <summary>英文名</summary>
            public const String English = "English";

            /// <summary>拼音</summary>
            public const String PinYin = "PinYin";

            /// <summary>简拼</summary>
            public const String JianPin = "JianPin";

            /// <summary>区号。电话区号</summary>
            public const String TelCode = "TelCode";

            /// <summary>邮编。邮政编码</summary>
            public const String ZipCode = "ZipCode";

            /// <summary>经度</summary>
            public const String Longitude = "Longitude";

            /// <summary>纬度</summary>
            public const String Latitude = "Latitude";

            /// <summary>地址编码。字符串前缀相同越多，地理距离越近，8位精度19米，6位610米</summary>
            public const String GeoHash = "GeoHash";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>更新时间</summary>
            public const String UpdateTime = "UpdateTime";

            /// <summary>备注</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }
}