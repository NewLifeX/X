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
    /// <summary>用户统计</summary>
    [Serializable]
    [DataObject]
    [Description("用户统计")]
    [BindIndex("IU_UserStat_Date", true, "Date")]
    [BindTable("UserStat", Description = "用户统计", ConnName = "Membership", DbType = DatabaseType.None)]
    public partial class UserStat
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

        private DateTime _Date;
        /// <summary>统计日期</summary>
        [DisplayName("统计日期")]
        [Description("统计日期")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("Date", "统计日期", "")]
        public DateTime Date { get => _Date; set { if (OnPropertyChanging("Date", value)) { _Date = value; OnPropertyChanged("Date"); } } }

        private Int32 _Total;
        /// <summary>总数。总用户数</summary>
        [DisplayName("总数")]
        [Description("总数。总用户数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Total", "总数。总用户数", "")]
        public Int32 Total { get => _Total; set { if (OnPropertyChanging("Total", value)) { _Total = value; OnPropertyChanged("Total"); } } }

        private Int32 _MaxOnline;
        /// <summary>最大在线。最大在线用户数</summary>
        [DisplayName("最大在线")]
        [Description("最大在线。最大在线用户数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("MaxOnline", "最大在线。最大在线用户数", "")]
        public Int32 MaxOnline { get => _MaxOnline; set { if (OnPropertyChanging("MaxOnline", value)) { _MaxOnline = value; OnPropertyChanged("MaxOnline"); } } }

        private Int32 _Actives;
        /// <summary>活跃。今天活跃用户数</summary>
        [DisplayName("活跃")]
        [Description("活跃。今天活跃用户数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Actives", "活跃。今天活跃用户数", "")]
        public Int32 Actives { get => _Actives; set { if (OnPropertyChanging("Actives", value)) { _Actives = value; OnPropertyChanged("Actives"); } } }

        private Int32 _ActivesT7;
        /// <summary>7天活跃。7天活跃用户数</summary>
        [DisplayName("7天活跃")]
        [Description("7天活跃。7天活跃用户数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ActivesT7", "7天活跃。7天活跃用户数", "")]
        public Int32 ActivesT7 { get => _ActivesT7; set { if (OnPropertyChanging("ActivesT7", value)) { _ActivesT7 = value; OnPropertyChanged("ActivesT7"); } } }

        private Int32 _ActivesT30;
        /// <summary>30天活跃。30天活跃用户数</summary>
        [DisplayName("30天活跃")]
        [Description("30天活跃。30天活跃用户数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ActivesT30", "30天活跃。30天活跃用户数", "")]
        public Int32 ActivesT30 { get => _ActivesT30; set { if (OnPropertyChanging("ActivesT30", value)) { _ActivesT30 = value; OnPropertyChanged("ActivesT30"); } } }

        private Int32 _News;
        /// <summary>新用户。今天注册新用户数</summary>
        [DisplayName("新用户")]
        [Description("新用户。今天注册新用户数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("News", "新用户。今天注册新用户数", "")]
        public Int32 News { get => _News; set { if (OnPropertyChanging("News", value)) { _News = value; OnPropertyChanged("News"); } } }

        private Int32 _NewsT7;
        /// <summary>7天注册。7天内注册新用户数</summary>
        [DisplayName("7天注册")]
        [Description("7天注册。7天内注册新用户数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("NewsT7", "7天注册。7天内注册新用户数", "")]
        public Int32 NewsT7 { get => _NewsT7; set { if (OnPropertyChanging("NewsT7", value)) { _NewsT7 = value; OnPropertyChanged("NewsT7"); } } }

        private Int32 _NewsT30;
        /// <summary>30天注册。30天注册新用户数</summary>
        [DisplayName("30天注册")]
        [Description("30天注册。30天注册新用户数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("NewsT30", "30天注册。30天注册新用户数", "")]
        public Int32 NewsT30 { get => _NewsT30; set { if (OnPropertyChanging("NewsT30", value)) { _NewsT30 = value; OnPropertyChanged("NewsT30"); } } }

        private Int32 _OnlineTime;
        /// <summary>在线时间。累计在线总时间，秒</summary>
        [DisplayName("在线时间")]
        [Description("在线时间。累计在线总时间，秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("OnlineTime", "在线时间。累计在线总时间，秒", "")]
        public Int32 OnlineTime { get => _OnlineTime; set { if (OnPropertyChanging("OnlineTime", value)) { _OnlineTime = value; OnPropertyChanged("OnlineTime"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

        private String _Remark;
        /// <summary>详细信息</summary>
        [DisplayName("详细信息")]
        [Description("详细信息")]
        [DataObjectField(false, false, true, 1000)]
        [BindColumn("Remark", "详细信息", "")]
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
                    case "Date": return _Date;
                    case "Total": return _Total;
                    case "MaxOnline": return _MaxOnline;
                    case "Actives": return _Actives;
                    case "ActivesT7": return _ActivesT7;
                    case "ActivesT30": return _ActivesT30;
                    case "News": return _News;
                    case "NewsT7": return _NewsT7;
                    case "NewsT30": return _NewsT30;
                    case "OnlineTime": return _OnlineTime;
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
                    case "Date": _Date = value.ToDateTime(); break;
                    case "Total": _Total = value.ToInt(); break;
                    case "MaxOnline": _MaxOnline = value.ToInt(); break;
                    case "Actives": _Actives = value.ToInt(); break;
                    case "ActivesT7": _ActivesT7 = value.ToInt(); break;
                    case "ActivesT30": _ActivesT30 = value.ToInt(); break;
                    case "News": _News = value.ToInt(); break;
                    case "NewsT7": _NewsT7 = value.ToInt(); break;
                    case "NewsT30": _NewsT30 = value.ToInt(); break;
                    case "OnlineTime": _OnlineTime = value.ToInt(); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得用户统计字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>统计日期</summary>
            public static readonly Field Date = FindByName("Date");

            /// <summary>总数。总用户数</summary>
            public static readonly Field Total = FindByName("Total");

            /// <summary>最大在线。最大在线用户数</summary>
            public static readonly Field MaxOnline = FindByName("MaxOnline");

            /// <summary>活跃。今天活跃用户数</summary>
            public static readonly Field Actives = FindByName("Actives");

            /// <summary>7天活跃。7天活跃用户数</summary>
            public static readonly Field ActivesT7 = FindByName("ActivesT7");

            /// <summary>30天活跃。30天活跃用户数</summary>
            public static readonly Field ActivesT30 = FindByName("ActivesT30");

            /// <summary>新用户。今天注册新用户数</summary>
            public static readonly Field News = FindByName("News");

            /// <summary>7天注册。7天内注册新用户数</summary>
            public static readonly Field NewsT7 = FindByName("NewsT7");

            /// <summary>30天注册。30天注册新用户数</summary>
            public static readonly Field NewsT30 = FindByName("NewsT30");

            /// <summary>在线时间。累计在线总时间，秒</summary>
            public static readonly Field OnlineTime = FindByName("OnlineTime");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            /// <summary>详细信息</summary>
            public static readonly Field Remark = FindByName("Remark");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得用户统计字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>统计日期</summary>
            public const String Date = "Date";

            /// <summary>总数。总用户数</summary>
            public const String Total = "Total";

            /// <summary>最大在线。最大在线用户数</summary>
            public const String MaxOnline = "MaxOnline";

            /// <summary>活跃。今天活跃用户数</summary>
            public const String Actives = "Actives";

            /// <summary>7天活跃。7天活跃用户数</summary>
            public const String ActivesT7 = "ActivesT7";

            /// <summary>30天活跃。30天活跃用户数</summary>
            public const String ActivesT30 = "ActivesT30";

            /// <summary>新用户。今天注册新用户数</summary>
            public const String News = "News";

            /// <summary>7天注册。7天内注册新用户数</summary>
            public const String NewsT7 = "NewsT7";

            /// <summary>30天注册。30天注册新用户数</summary>
            public const String NewsT30 = "NewsT30";

            /// <summary>在线时间。累计在线总时间，秒</summary>
            public const String OnlineTime = "OnlineTime";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>更新时间</summary>
            public const String UpdateTime = "UpdateTime";

            /// <summary>详细信息</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }
}