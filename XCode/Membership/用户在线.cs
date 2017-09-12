using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public Int32 ID { get { return _ID; } set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } } }

        private Int32 _UserID;
        /// <summary>用户</summary>
        [DisplayName("用户")]
        [Description("用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UserID", "用户", "")]
        public Int32 UserID { get { return _UserID; } set { if (OnPropertyChanging(__.UserID, value)) { _UserID = value; OnPropertyChanged(__.UserID); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get { return _Name; } set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } } }

        private String _SessionID;
        /// <summary>会话。Web的SessionID或Server的会话编号</summary>
        [DisplayName("会话")]
        [Description("会话。Web的SessionID或Server的会话编号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("SessionID", "会话。Web的SessionID或Server的会话编号", "")]
        public String SessionID { get { return _SessionID; } set { if (OnPropertyChanging(__.SessionID, value)) { _SessionID = value; OnPropertyChanged(__.SessionID); } } }

        private Int32 _Times;
        /// <summary>次数</summary>
        [DisplayName("次数")]
        [Description("次数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Times", "次数", "")]
        public Int32 Times { get { return _Times; } set { if (OnPropertyChanging(__.Times, value)) { _Times = value; OnPropertyChanged(__.Times); } } }

        private String _Page;
        /// <summary>页面</summary>
        [DisplayName("页面")]
        [Description("页面")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Page", "页面", "")]
        public String Page { get { return _Page; } set { if (OnPropertyChanging(__.Page, value)) { _Page = value; OnPropertyChanged(__.Page); } } }

        private String _Status;
        /// <summary>状态</summary>
        [DisplayName("状态")]
        [Description("状态")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Status", "状态", "")]
        public String Status { get { return _Status; } set { if (OnPropertyChanging(__.Status, value)) { _Status = value; OnPropertyChanged(__.Status); } } }

        private Int32 _OnlineTime;
        /// <summary>在线时间。本次在线总时间，秒</summary>
        [DisplayName("在线时间")]
        [Description("在线时间。本次在线总时间，秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("OnlineTime", "在线时间。本次在线总时间，秒", "")]
        public Int32 OnlineTime { get { return _OnlineTime; } set { if (OnPropertyChanging(__.OnlineTime, value)) { _OnlineTime = value; OnPropertyChanged(__.OnlineTime); } } }

        private String _CreateIP;
        /// <summary>创建地址</summary>
        [DisplayName("创建地址")]
        [Description("创建地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "创建地址", "")]
        public String CreateIP { get { return _CreateIP; } set { if (OnPropertyChanging(__.CreateIP, value)) { _CreateIP = value; OnPropertyChanged(__.CreateIP); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get { return _CreateTime; } set { if (OnPropertyChanging(__.CreateTime, value)) { _CreateTime = value; OnPropertyChanged(__.CreateTime); } } }

        private DateTime _UpdateTime;
        /// <summary>修改时间</summary>
        [DisplayName("修改时间")]
        [Description("修改时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "修改时间", "")]
        public DateTime UpdateTime { get { return _UpdateTime; } set { if (OnPropertyChanging(__.UpdateTime, value)) { _UpdateTime = value; OnPropertyChanged(__.UpdateTime); } } }
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
                    case __.ID : return _ID;
                    case __.UserID : return _UserID;
                    case __.Name : return _Name;
                    case __.SessionID : return _SessionID;
                    case __.Times : return _Times;
                    case __.Page : return _Page;
                    case __.Status : return _Status;
                    case __.OnlineTime : return _OnlineTime;
                    case __.CreateIP : return _CreateIP;
                    case __.CreateTime : return _CreateTime;
                    case __.UpdateTime : return _UpdateTime;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.UserID : _UserID = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.SessionID : _SessionID = Convert.ToString(value); break;
                    case __.Times : _Times = Convert.ToInt32(value); break;
                    case __.Page : _Page = Convert.ToString(value); break;
                    case __.Status : _Status = Convert.ToString(value); break;
                    case __.OnlineTime : _OnlineTime = Convert.ToInt32(value); break;
                    case __.CreateIP : _CreateIP = Convert.ToString(value); break;
                    case __.CreateTime : _CreateTime = Convert.ToDateTime(value); break;
                    case __.UpdateTime : _UpdateTime = Convert.ToDateTime(value); break;
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
            public static readonly Field ID = FindByName(__.ID);

            /// <summary>用户</summary>
            public static readonly Field UserID = FindByName(__.UserID);

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            /// <summary>会话。Web的SessionID或Server的会话编号</summary>
            public static readonly Field SessionID = FindByName(__.SessionID);

            /// <summary>次数</summary>
            public static readonly Field Times = FindByName(__.Times);

            /// <summary>页面</summary>
            public static readonly Field Page = FindByName(__.Page);

            /// <summary>状态</summary>
            public static readonly Field Status = FindByName(__.Status);

            /// <summary>在线时间。本次在线总时间，秒</summary>
            public static readonly Field OnlineTime = FindByName(__.OnlineTime);

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName(__.CreateIP);

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName(__.CreateTime);

            /// <summary>修改时间</summary>
            public static readonly Field UpdateTime = FindByName(__.UpdateTime);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
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