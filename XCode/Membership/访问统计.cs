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
    /// <summary>访问统计</summary>
    [Serializable]
    [DataObject]
    [Description("访问统计")]
    [BindIndex("IU_VisitStat_Page_Level_Time", true, "Page,Level,Time")]
    [BindIndex("IX_VisitStat_Level_Time", false, "Level,Time")]
    [BindTable("VisitStat", Description = "访问统计", ConnName = "Log", DbType = DatabaseType.None)]
    public partial class VisitStat : IVisitStat
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

        private XCode.Statistics.StatLevels _Level;
        /// <summary>层级</summary>
        [DisplayName("层级")]
        [Description("层级")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Level", "层级", "")]
        public XCode.Statistics.StatLevels Level { get => _Level; set { if (OnPropertyChanging("Level", value)) { _Level = value; OnPropertyChanged("Level"); } } }

        private DateTime _Time;
        /// <summary>时间</summary>
        [DisplayName("时间")]
        [Description("时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("Time", "时间", "")]
        public DateTime Time { get => _Time; set { if (OnPropertyChanging("Time", value)) { _Time = value; OnPropertyChanged("Time"); } } }

        private String _Page;
        /// <summary>页面</summary>
        [DisplayName("页面")]
        [Description("页面")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn("Page", "页面", "")]
        public String Page { get => _Page; set { if (OnPropertyChanging("Page", value)) { _Page = value; OnPropertyChanged("Page"); } } }

        private String _Title;
        /// <summary>标题</summary>
        [DisplayName("标题")]
        [Description("标题")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Title", "标题", "", Master = true)]
        public String Title { get => _Title; set { if (OnPropertyChanging("Title", value)) { _Title = value; OnPropertyChanged("Title"); } } }

        private Int32 _Times;
        /// <summary>次数</summary>
        [DisplayName("次数")]
        [Description("次数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Times", "次数", "")]
        public Int32 Times { get => _Times; set { if (OnPropertyChanging("Times", value)) { _Times = value; OnPropertyChanged("Times"); } } }

        private Int32 _Users;
        /// <summary>用户</summary>
        [DisplayName("用户")]
        [Description("用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Users", "用户", "")]
        public Int32 Users { get => _Users; set { if (OnPropertyChanging("Users", value)) { _Users = value; OnPropertyChanged("Users"); } } }

        private Int32 _IPs;
        /// <summary>IP</summary>
        [DisplayName("IP")]
        [Description("IP")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("IPs", "IP", "")]
        public Int32 IPs { get => _IPs; set { if (OnPropertyChanging("IPs", value)) { _IPs = value; OnPropertyChanged("IPs"); } } }

        private Int32 _Error;
        /// <summary>错误</summary>
        [DisplayName("错误")]
        [Description("错误")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Error", "错误", "")]
        public Int32 Error { get => _Error; set { if (OnPropertyChanging("Error", value)) { _Error = value; OnPropertyChanged("Error"); } } }

        private Int32 _Cost;
        /// <summary>耗时。毫秒</summary>
        [DisplayName("耗时")]
        [Description("耗时。毫秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Cost", "耗时。毫秒", "")]
        public Int32 Cost { get => _Cost; set { if (OnPropertyChanging("Cost", value)) { _Cost = value; OnPropertyChanged("Cost"); } } }

        private Int32 _MaxCost;
        /// <summary>最大耗时。毫秒</summary>
        [DisplayName("最大耗时")]
        [Description("最大耗时。毫秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("MaxCost", "最大耗时。毫秒", "")]
        public Int32 MaxCost { get => _MaxCost; set { if (OnPropertyChanging("MaxCost", value)) { _MaxCost = value; OnPropertyChanged("MaxCost"); } } }

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
        [DataObjectField(false, false, true, 5000)]
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
                    case "Level": return _Level;
                    case "Time": return _Time;
                    case "Page": return _Page;
                    case "Title": return _Title;
                    case "Times": return _Times;
                    case "Users": return _Users;
                    case "IPs": return _IPs;
                    case "Error": return _Error;
                    case "Cost": return _Cost;
                    case "MaxCost": return _MaxCost;
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
                    case "Level": _Level = (XCode.Statistics.StatLevels)value.ToInt(); break;
                    case "Time": _Time = value.ToDateTime(); break;
                    case "Page": _Page = Convert.ToString(value); break;
                    case "Title": _Title = Convert.ToString(value); break;
                    case "Times": _Times = value.ToInt(); break;
                    case "Users": _Users = value.ToInt(); break;
                    case "IPs": _IPs = value.ToInt(); break;
                    case "Error": _Error = value.ToInt(); break;
                    case "Cost": _Cost = value.ToInt(); break;
                    case "MaxCost": _MaxCost = value.ToInt(); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得访问统计字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>层级</summary>
            public static readonly Field Level = FindByName("Level");

            /// <summary>时间</summary>
            public static readonly Field Time = FindByName("Time");

            /// <summary>页面</summary>
            public static readonly Field Page = FindByName("Page");

            /// <summary>标题</summary>
            public static readonly Field Title = FindByName("Title");

            /// <summary>次数</summary>
            public static readonly Field Times = FindByName("Times");

            /// <summary>用户</summary>
            public static readonly Field Users = FindByName("Users");

            /// <summary>IP</summary>
            public static readonly Field IPs = FindByName("IPs");

            /// <summary>错误</summary>
            public static readonly Field Error = FindByName("Error");

            /// <summary>耗时。毫秒</summary>
            public static readonly Field Cost = FindByName("Cost");

            /// <summary>最大耗时。毫秒</summary>
            public static readonly Field MaxCost = FindByName("MaxCost");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            /// <summary>详细信息</summary>
            public static readonly Field Remark = FindByName("Remark");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得访问统计字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>层级</summary>
            public const String Level = "Level";

            /// <summary>时间</summary>
            public const String Time = "Time";

            /// <summary>页面</summary>
            public const String Page = "Page";

            /// <summary>标题</summary>
            public const String Title = "Title";

            /// <summary>次数</summary>
            public const String Times = "Times";

            /// <summary>用户</summary>
            public const String Users = "Users";

            /// <summary>IP</summary>
            public const String IPs = "IPs";

            /// <summary>错误</summary>
            public const String Error = "Error";

            /// <summary>耗时。毫秒</summary>
            public const String Cost = "Cost";

            /// <summary>最大耗时。毫秒</summary>
            public const String MaxCost = "MaxCost";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>更新时间</summary>
            public const String UpdateTime = "UpdateTime";

            /// <summary>详细信息</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }

    /// <summary>访问统计接口</summary>
    public partial interface IVisitStat
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>层级</summary>
        XCode.Statistics.StatLevels Level { get; set; }

        /// <summary>时间</summary>
        DateTime Time { get; set; }

        /// <summary>页面</summary>
        String Page { get; set; }

        /// <summary>标题</summary>
        String Title { get; set; }

        /// <summary>次数</summary>
        Int32 Times { get; set; }

        /// <summary>用户</summary>
        Int32 Users { get; set; }

        /// <summary>IP</summary>
        Int32 IPs { get; set; }

        /// <summary>错误</summary>
        Int32 Error { get; set; }

        /// <summary>耗时。毫秒</summary>
        Int32 Cost { get; set; }

        /// <summary>最大耗时。毫秒</summary>
        Int32 MaxCost { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        DateTime UpdateTime { get; set; }

        /// <summary>详细信息</summary>
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