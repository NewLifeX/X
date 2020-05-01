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
    /// <summary>地区。行政区划数据</summary>
    [Serializable]
    [DataObject]
    [Description("地区。行政区划数据")]
    [BindIndex("IX_Area_ParentID", false, "ParentID")]
    [BindIndex("IX_Area_Name", false, "Name")]
    [BindIndex("IX_Area_UpdateTime_ID", false, "UpdateTime,ID")]
    [BindTable("Area", Description = "地区。行政区划数据", ConnName = "Membership", DbType = DatabaseType.None)]
    public partial class Area : IArea
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编码。行政区划编码</summary>
        [DisplayName("编码")]
        [Description("编码。行政区划编码")]
        [DataObjectField(true, false, false, 0)]
        [BindColumn("ID", "编码。行政区划编码", "")]
        public Int32 ID { get => _ID; set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } } }

        private String _FullName;
        /// <summary>全名</summary>
        [DisplayName("全名")]
        [Description("全名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("FullName", "全名", "", Master = true)]
        public String FullName { get => _FullName; set { if (OnPropertyChanging(__.FullName, value)) { _FullName = value; OnPropertyChanged(__.FullName); } } }

        private Int32 _ParentID;
        /// <summary>父级</summary>
        [DisplayName("父级")]
        [Description("父级")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ParentID", "父级", "")]
        public Int32 ParentID { get => _ParentID; set { if (OnPropertyChanging(__.ParentID, value)) { _ParentID = value; OnPropertyChanged(__.ParentID); } } }

        private Int32 _Level;
        /// <summary>层级</summary>
        [DisplayName("层级")]
        [Description("层级")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Level", "层级", "")]
        public Int32 Level { get => _Level; set { if (OnPropertyChanging(__.Level, value)) { _Level = value; OnPropertyChanged(__.Level); } } }

        private Double _Longitude;
        /// <summary>经度</summary>
        [DisplayName("经度")]
        [Description("经度")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Longitude", "经度", "")]
        public Double Longitude { get => _Longitude; set { if (OnPropertyChanging(__.Longitude, value)) { _Longitude = value; OnPropertyChanged(__.Longitude); } } }

        private Double _Latitude;
        /// <summary>纬度</summary>
        [DisplayName("纬度")]
        [Description("纬度")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Latitude", "纬度", "")]
        public Double Latitude { get => _Latitude; set { if (OnPropertyChanging(__.Latitude, value)) { _Latitude = value; OnPropertyChanged(__.Latitude); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get => _Enable; set { if (OnPropertyChanging(__.Enable, value)) { _Enable = value; OnPropertyChanged(__.Enable); } } }

        private String _UpdateUser;
        /// <summary>更新者</summary>
        [DisplayName("更新者")]
        [Description("更新者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateUser", "更新者", "")]
        public String UpdateUser { get => _UpdateUser; set { if (OnPropertyChanging(__.UpdateUser, value)) { _UpdateUser = value; OnPropertyChanged(__.UpdateUser); } } }

        private Int32 _UpdateUserID;
        /// <summary>更新用户</summary>
        [DisplayName("更新用户")]
        [Description("更新用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateUserID", "更新用户", "")]
        public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging(__.UpdateUserID, value)) { _UpdateUserID = value; OnPropertyChanged(__.UpdateUserID); } } }

        private String _UpdateIP;
        /// <summary>更新地址</summary>
        [DisplayName("更新地址")]
        [Description("更新地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateIP", "更新地址", "")]
        public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging(__.UpdateIP, value)) { _UpdateIP = value; OnPropertyChanged(__.UpdateIP); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging(__.UpdateTime, value)) { _UpdateTime = value; OnPropertyChanged(__.UpdateTime); } } }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Remark", "备注", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging(__.Remark, value)) { _Remark = value; OnPropertyChanged(__.Remark); } } }
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
                    case __.ID: return _ID;
                    case __.Name: return _Name;
                    case __.FullName: return _FullName;
                    case __.ParentID: return _ParentID;
                    case __.Level: return _Level;
                    case __.Longitude: return _Longitude;
                    case __.Latitude: return _Latitude;
                    case __.Enable: return _Enable;
                    case __.UpdateUser: return _UpdateUser;
                    case __.UpdateUserID: return _UpdateUserID;
                    case __.UpdateIP: return _UpdateIP;
                    case __.UpdateTime: return _UpdateTime;
                    case __.Remark: return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID: _ID = value.ToInt(); break;
                    case __.Name: _Name = Convert.ToString(value); break;
                    case __.FullName: _FullName = Convert.ToString(value); break;
                    case __.ParentID: _ParentID = value.ToInt(); break;
                    case __.Level: _Level = value.ToInt(); break;
                    case __.Longitude: _Longitude = value.ToDouble(); break;
                    case __.Latitude: _Latitude = value.ToDouble(); break;
                    case __.Enable: _Enable = value.ToBoolean(); break;
                    case __.UpdateUser: _UpdateUser = Convert.ToString(value); break;
                    case __.UpdateUserID: _UpdateUserID = value.ToInt(); break;
                    case __.UpdateIP: _UpdateIP = Convert.ToString(value); break;
                    case __.UpdateTime: _UpdateTime = value.ToDateTime(); break;
                    case __.Remark: _Remark = Convert.ToString(value); break;
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
            public static readonly Field ID = FindByName(__.ID);

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            /// <summary>全名</summary>
            public static readonly Field FullName = FindByName(__.FullName);

            /// <summary>父级</summary>
            public static readonly Field ParentID = FindByName(__.ParentID);

            /// <summary>层级</summary>
            public static readonly Field Level = FindByName(__.Level);

            /// <summary>经度</summary>
            public static readonly Field Longitude = FindByName(__.Longitude);

            /// <summary>纬度</summary>
            public static readonly Field Latitude = FindByName(__.Latitude);

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName(__.Enable);

            /// <summary>更新者</summary>
            public static readonly Field UpdateUser = FindByName(__.UpdateUser);

            /// <summary>更新用户</summary>
            public static readonly Field UpdateUserID = FindByName(__.UpdateUserID);

            /// <summary>更新地址</summary>
            public static readonly Field UpdateIP = FindByName(__.UpdateIP);

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName(__.UpdateTime);

            /// <summary>备注</summary>
            public static readonly Field Remark = FindByName(__.Remark);

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

            /// <summary>经度</summary>
            public const String Longitude = "Longitude";

            /// <summary>纬度</summary>
            public const String Latitude = "Latitude";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>更新者</summary>
            public const String UpdateUser = "UpdateUser";

            /// <summary>更新用户</summary>
            public const String UpdateUserID = "UpdateUserID";

            /// <summary>更新地址</summary>
            public const String UpdateIP = "UpdateIP";

            /// <summary>更新时间</summary>
            public const String UpdateTime = "UpdateTime";

            /// <summary>备注</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }

    /// <summary>地区。行政区划数据接口</summary>
    public partial interface IArea
    {
        #region 属性
        /// <summary>编码。行政区划编码</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>全名</summary>
        String FullName { get; set; }

        /// <summary>父级</summary>
        Int32 ParentID { get; set; }

        /// <summary>层级</summary>
        Int32 Level { get; set; }

        /// <summary>经度</summary>
        Double Longitude { get; set; }

        /// <summary>纬度</summary>
        Double Latitude { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }

        /// <summary>更新者</summary>
        String UpdateUser { get; set; }

        /// <summary>更新用户</summary>
        Int32 UpdateUserID { get; set; }

        /// <summary>更新地址</summary>
        String UpdateIP { get; set; }

        /// <summary>更新时间</summary>
        DateTime UpdateTime { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}