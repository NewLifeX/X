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
    /// <summary>用户在线</summary>
    [Serializable]
    [DataObject]
    [Description("用户在线")]
    [BindIndex("IX_UserOnline_UserID", false, "UserID")]
    [BindIndex("IX_UserOnline_SessionID", false, "SessionID")]
    [BindIndex("IX_UserOnline_CreateTime", false, "CreateTime")]
    [BindTable("UserOnline", Description = "用户在线", ConnName = "Log", DbType = DatabaseType.None)]
    public partial class UserOnline : IUserOnline
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

        private Int32 _UserID;
        /// <summary>用户</summary>
        [DisplayName("用户")]
        [Description("用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UserID", "用户", "")]
        public Int32 UserID { get => _UserID; set { if (OnPropertyChanging("UserID", value)) { _UserID = value; OnPropertyChanged("UserID"); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private String _SessionID;
        /// <summary>会话。Web的SessionID或Server的会话编号</summary>
        [DisplayName("会话")]
        [Description("会话。Web的SessionID或Server的会话编号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("SessionID", "会话。Web的SessionID或Server的会话编号", "")]
        public String SessionID { get => _SessionID; set { if (OnPropertyChanging("SessionID", value)) { _SessionID = value; OnPropertyChanged("SessionID"); } } }

        private Int32 _Times;
        /// <summary>次数</summary>
        [DisplayName("次数")]
        [Description("次数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Times", "次数", "")]
        public Int32 Times { get => _Times; set { if (OnPropertyChanging("Times", value)) { _Times = value; OnPropertyChanged("Times"); } } }

        private String _Page;
        /// <summary>页面</summary>
        [DisplayName("页面")]
        [Description("页面")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Page", "页面", "")]
        public String Page { get => _Page; set { if (OnPropertyChanging("Page", value)) { _Page = value; OnPropertyChanged("Page"); } } }

        private String _Status;
        /// <summary>状态</summary>
        [DisplayName("状态")]
        [Description("状态")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Status", "状态", "")]
        public String Status { get => _Status; set { if (OnPropertyChanging("Status", value)) { _Status = value; OnPropertyChanged("Status"); } } }

        private Int32 _OnlineTime;
        /// <summary>在线时间。本次在线总时间，秒</summary>
        [DisplayName("在线时间")]
        [Description("在线时间。本次在线总时间，秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("OnlineTime", "在线时间。本次在线总时间，秒", "")]
        public Int32 OnlineTime { get => _OnlineTime; set { if (OnPropertyChanging("OnlineTime", value)) { _OnlineTime = value; OnPropertyChanged("OnlineTime"); } } }

        private String _CreateIP;
        /// <summary>创建地址</summary>
        [DisplayName("创建地址")]
        [Description("创建地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "创建地址", "")]
        public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private DateTime _UpdateTime;
        /// <summary>修改时间</summary>
        [DisplayName("修改时间")]
        [Description("修改时间")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateTime", "修改时间", "")]
        public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }
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
                    case "UserID": return _UserID;
                    case "Name": return _Name;
                    case "SessionID": return _SessionID;
                    case "Times": return _Times;
                    case "Page": return _Page;
                    case "Status": return _Status;
                    case "OnlineTime": return _OnlineTime;
                    case "CreateIP": return _CreateIP;
                    case "CreateTime": return _CreateTime;
                    case "UpdateTime": return _UpdateTime;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = value.ToInt(); break;
                    case "UserID": _UserID = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "SessionID": _SessionID = Convert.ToString(value); break;
                    case "Times": _Times = value.ToInt(); break;
                    case "Page": _Page = Convert.ToString(value); break;
                    case "Status": _Status = Convert.ToString(value); break;
                    case "OnlineTime": _OnlineTime = value.ToInt(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得用户在线字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>用户</summary>
            public static readonly Field UserID = FindByName("UserID");

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>会话。Web的SessionID或Server的会话编号</summary>
            public static readonly Field SessionID = FindByName("SessionID");

            /// <summary>次数</summary>
            public static readonly Field Times = FindByName("Times");

            /// <summary>页面</summary>
            public static readonly Field Page = FindByName("Page");

            /// <summary>状态</summary>
            public static readonly Field Status = FindByName("Status");

            /// <summary>在线时间。本次在线总时间，秒</summary>
            public static readonly Field OnlineTime = FindByName("OnlineTime");

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName("CreateIP");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>修改时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得用户在线字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>用户</summary>
            public const String UserID = "UserID";

            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>会话。Web的SessionID或Server的会话编号</summary>
            public const String SessionID = "SessionID";

            /// <summary>次数</summary>
            public const String Times = "Times";

            /// <summary>页面</summary>
            public const String Page = "Page";

            /// <summary>状态</summary>
            public const String Status = "Status";

            /// <summary>在线时间。本次在线总时间，秒</summary>
            public const String OnlineTime = "OnlineTime";

            /// <summary>创建地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>修改时间</summary>
            public const String UpdateTime = "UpdateTime";
        }
        #endregion
    }

    /// <summary>用户在线接口</summary>
    public partial interface IUserOnline
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>用户</summary>
        Int32 UserID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>会话。Web的SessionID或Server的会话编号</summary>
        String SessionID { get; set; }

        /// <summary>次数</summary>
        Int32 Times { get; set; }

        /// <summary>页面</summary>
        String Page { get; set; }

        /// <summary>状态</summary>
        String Status { get; set; }

        /// <summary>在线时间。本次在线总时间，秒</summary>
        Int32 OnlineTime { get; set; }

        /// <summary>创建地址</summary>
        String CreateIP { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>修改时间</summary>
        DateTime UpdateTime { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}