using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Extension
{
    /// <summary>数据缓存</summary>
    [Serializable]
    [DataObject]
    [Description("数据缓存")]
    [BindIndex("IU_TableCache_Name", true, "Name")]
    [BindIndex("IX_MyDbCache_ExpiredTime", false, "ExpiredTime")]
    [BindTable("MyDbCache", Description = "数据缓存", ConnName = "DbCache", DbType = DatabaseType.None)]
    public partial class MyDbCache : IMyDbCache
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(true, false, false, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get { return _Name; } set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } } }

        private String _Value;
        /// <summary>键值</summary>
        [DisplayName("键值")]
        [Description("键值")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("Value", "键值", "")]
        public String Value { get { return _Value; } set { if (OnPropertyChanging(__.Value, value)) { _Value = value; OnPropertyChanged(__.Value); } } }

        private Int32 _Visits;
        /// <summary>访问次数</summary>
        [DisplayName("访问次数")]
        [Description("访问次数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Visits", "访问次数", "")]
        public Int32 Visits { get { return _Visits; } set { if (OnPropertyChanging(__.Visits, value)) { _Visits = value; OnPropertyChanged(__.Visits); } } }

        private DateTime _LastVisit;
        /// <summary>最后访问</summary>
        [DisplayName("最后访问")]
        [Description("最后访问")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("LastVisit", "最后访问", "")]
        public DateTime LastVisit { get { return _LastVisit; } set { if (OnPropertyChanging(__.LastVisit, value)) { _LastVisit = value; OnPropertyChanged(__.LastVisit); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get { return _CreateTime; } set { if (OnPropertyChanging(__.CreateTime, value)) { _CreateTime = value; OnPropertyChanged(__.CreateTime); } } }

        private DateTime _ExpiredTime;
        /// <summary>过期时间</summary>
        [DisplayName("过期时间")]
        [Description("过期时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("ExpiredTime", "过期时间", "")]
        public DateTime ExpiredTime { get { return _ExpiredTime; } set { if (OnPropertyChanging(__.ExpiredTime, value)) { _ExpiredTime = value; OnPropertyChanged(__.ExpiredTime); } } }
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
                    case __.Name : return _Name;
                    case __.Value : return _Value;
                    case __.Visits : return _Visits;
                    case __.LastVisit : return _LastVisit;
                    case __.CreateTime : return _CreateTime;
                    case __.ExpiredTime : return _ExpiredTime;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.Value : _Value = Convert.ToString(value); break;
                    case __.Visits : _Visits = Convert.ToInt32(value); break;
                    case __.LastVisit : _LastVisit = Convert.ToDateTime(value); break;
                    case __.CreateTime : _CreateTime = Convert.ToDateTime(value); break;
                    case __.ExpiredTime : _ExpiredTime = Convert.ToDateTime(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得数据缓存字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            /// <summary>键值</summary>
            public static readonly Field Value = FindByName(__.Value);

            /// <summary>访问次数</summary>
            public static readonly Field Visits = FindByName(__.Visits);

            /// <summary>最后访问</summary>
            public static readonly Field LastVisit = FindByName(__.LastVisit);

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName(__.CreateTime);

            /// <summary>过期时间</summary>
            public static readonly Field ExpiredTime = FindByName(__.ExpiredTime);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得数据缓存字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>键值</summary>
            public const String Value = "Value";

            /// <summary>访问次数</summary>
            public const String Visits = "Visits";

            /// <summary>最后访问</summary>
            public const String LastVisit = "LastVisit";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>过期时间</summary>
            public const String ExpiredTime = "ExpiredTime";
        }
        #endregion
    }

    /// <summary>数据缓存接口</summary>
    public partial interface IMyDbCache
    {
        #region 属性
        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>键值</summary>
        String Value { get; set; }

        /// <summary>访问次数</summary>
        Int32 Visits { get; set; }

        /// <summary>最后访问</summary>
        DateTime LastVisit { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>过期时间</summary>
        DateTime ExpiredTime { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}