using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Entity
{
    /// <summary>用户链接。第三方绑定</summary>
    [Serializable]
    [DataObject]
    [Description("用户链接。第三方绑定")]
    [BindIndex("IU_UserConnect_Provider_OpenID", true, "Provider,OpenID")]
    [BindIndex("IX_UserConnect_UserID", false, "UserID")]
    [BindTable("UserConnect", Description = "用户链接。第三方绑定", ConnName = "Membership", DbType = DatabaseType.None)]
    public partial class UserConnect : IUserConnect
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get { return _ID; } set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } } }

        private String _Provider;
        /// <summary>提供商</summary>
        [DisplayName("提供商")]
        [Description("提供商")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Provider", "提供商", "")]
        public String Provider { get { return _Provider; } set { if (OnPropertyChanging(__.Provider, value)) { _Provider = value; OnPropertyChanged(__.Provider); } } }

        private Int32 _UserID;
        /// <summary>用户。本地用户</summary>
        [DisplayName("用户")]
        [Description("用户。本地用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UserID", "用户。本地用户", "")]
        public Int32 UserID { get { return _UserID; } set { if (OnPropertyChanging(__.UserID, value)) { _UserID = value; OnPropertyChanged(__.UserID); } } }

        private String _OpenID;
        /// <summary>身份标识。用户名、OpenID</summary>
        [DisplayName("身份标识")]
        [Description("身份标识。用户名、OpenID")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("OpenID", "身份标识。用户名、OpenID", "")]
        public String OpenID { get { return _OpenID; } set { if (OnPropertyChanging(__.OpenID, value)) { _OpenID = value; OnPropertyChanged(__.OpenID); } } }

        private Int64 _LinkID;
        /// <summary>用户编号。第三方用户编号</summary>
        [DisplayName("用户编号")]
        [Description("用户编号。第三方用户编号")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("LinkID", "用户编号。第三方用户编号", "")]
        public Int64 LinkID { get { return _LinkID; } set { if (OnPropertyChanging(__.LinkID, value)) { _LinkID = value; OnPropertyChanged(__.LinkID); } } }

        private String _NickName;
        /// <summary>昵称</summary>
        [DisplayName("昵称")]
        [Description("昵称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("NickName", "昵称", "")]
        public String NickName { get { return _NickName; } set { if (OnPropertyChanging(__.NickName, value)) { _NickName = value; OnPropertyChanged(__.NickName); } } }

        private String _Avatar;
        /// <summary>头像</summary>
        [DisplayName("头像")]
        [Description("头像")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Avatar", "头像", "")]
        public String Avatar { get { return _Avatar; } set { if (OnPropertyChanging(__.Avatar, value)) { _Avatar = value; OnPropertyChanged(__.Avatar); } } }

        private String _AccessToken;
        /// <summary>访问令牌</summary>
        [DisplayName("访问令牌")]
        [Description("访问令牌")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("AccessToken", "访问令牌", "")]
        public String AccessToken { get { return _AccessToken; } set { if (OnPropertyChanging(__.AccessToken, value)) { _AccessToken = value; OnPropertyChanged(__.AccessToken); } } }

        private String _RefreshToken;
        /// <summary>刷新令牌</summary>
        [DisplayName("刷新令牌")]
        [Description("刷新令牌")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("RefreshToken", "刷新令牌", "")]
        public String RefreshToken { get { return _RefreshToken; } set { if (OnPropertyChanging(__.RefreshToken, value)) { _RefreshToken = value; OnPropertyChanged(__.RefreshToken); } } }

        private DateTime _Expire;
        /// <summary>过期时间</summary>
        [DisplayName("过期时间")]
        [Description("过期时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("Expire", "过期时间", "")]
        public DateTime Expire { get { return _Expire; } set { if (OnPropertyChanging(__.Expire, value)) { _Expire = value; OnPropertyChanged(__.Expire); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get { return _Enable; } set { if (OnPropertyChanging(__.Enable, value)) { _Enable = value; OnPropertyChanged(__.Enable); } } }

        private Int32 _CreateUserID;
        /// <summary>创建用户</summary>
        [DisplayName("创建用户")]
        [Description("创建用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserID", "创建用户", "")]
        public Int32 CreateUserID { get { return _CreateUserID; } set { if (OnPropertyChanging(__.CreateUserID, value)) { _CreateUserID = value; OnPropertyChanged(__.CreateUserID); } } }

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

        private Int32 _UpdateUserID;
        /// <summary>更新用户</summary>
        [DisplayName("更新用户")]
        [Description("更新用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateUserID", "更新用户", "")]
        public Int32 UpdateUserID { get { return _UpdateUserID; } set { if (OnPropertyChanging(__.UpdateUserID, value)) { _UpdateUserID = value; OnPropertyChanged(__.UpdateUserID); } } }

        private String _UpdateIP;
        /// <summary>更新地址</summary>
        [DisplayName("更新地址")]
        [Description("更新地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateIP", "更新地址", "")]
        public String UpdateIP { get { return _UpdateIP; } set { if (OnPropertyChanging(__.UpdateIP, value)) { _UpdateIP = value; OnPropertyChanged(__.UpdateIP); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get { return _UpdateTime; } set { if (OnPropertyChanging(__.UpdateTime, value)) { _UpdateTime = value; OnPropertyChanged(__.UpdateTime); } } }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Remark", "备注", "")]
        public String Remark { get { return _Remark; } set { if (OnPropertyChanging(__.Remark, value)) { _Remark = value; OnPropertyChanged(__.Remark); } } }
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
                    case __.Provider : return _Provider;
                    case __.UserID : return _UserID;
                    case __.OpenID : return _OpenID;
                    case __.LinkID : return _LinkID;
                    case __.NickName : return _NickName;
                    case __.Avatar : return _Avatar;
                    case __.AccessToken : return _AccessToken;
                    case __.RefreshToken : return _RefreshToken;
                    case __.Expire : return _Expire;
                    case __.Enable : return _Enable;
                    case __.CreateUserID : return _CreateUserID;
                    case __.CreateIP : return _CreateIP;
                    case __.CreateTime : return _CreateTime;
                    case __.UpdateUserID : return _UpdateUserID;
                    case __.UpdateIP : return _UpdateIP;
                    case __.UpdateTime : return _UpdateTime;
                    case __.Remark : return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Provider : _Provider = Convert.ToString(value); break;
                    case __.UserID : _UserID = Convert.ToInt32(value); break;
                    case __.OpenID : _OpenID = Convert.ToString(value); break;
                    case __.LinkID : _LinkID = Convert.ToInt64(value); break;
                    case __.NickName : _NickName = Convert.ToString(value); break;
                    case __.Avatar : _Avatar = Convert.ToString(value); break;
                    case __.AccessToken : _AccessToken = Convert.ToString(value); break;
                    case __.RefreshToken : _RefreshToken = Convert.ToString(value); break;
                    case __.Expire : _Expire = Convert.ToDateTime(value); break;
                    case __.Enable : _Enable = Convert.ToBoolean(value); break;
                    case __.CreateUserID : _CreateUserID = Convert.ToInt32(value); break;
                    case __.CreateIP : _CreateIP = Convert.ToString(value); break;
                    case __.CreateTime : _CreateTime = Convert.ToDateTime(value); break;
                    case __.UpdateUserID : _UpdateUserID = Convert.ToInt32(value); break;
                    case __.UpdateIP : _UpdateIP = Convert.ToString(value); break;
                    case __.UpdateTime : _UpdateTime = Convert.ToDateTime(value); break;
                    case __.Remark : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得用户链接字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            /// <summary>提供商</summary>
            public static readonly Field Provider = FindByName(__.Provider);

            /// <summary>用户。本地用户</summary>
            public static readonly Field UserID = FindByName(__.UserID);

            /// <summary>身份标识。用户名、OpenID</summary>
            public static readonly Field OpenID = FindByName(__.OpenID);

            /// <summary>用户编号。第三方用户编号</summary>
            public static readonly Field LinkID = FindByName(__.LinkID);

            /// <summary>昵称</summary>
            public static readonly Field NickName = FindByName(__.NickName);

            /// <summary>头像</summary>
            public static readonly Field Avatar = FindByName(__.Avatar);

            /// <summary>访问令牌</summary>
            public static readonly Field AccessToken = FindByName(__.AccessToken);

            /// <summary>刷新令牌</summary>
            public static readonly Field RefreshToken = FindByName(__.RefreshToken);

            /// <summary>过期时间</summary>
            public static readonly Field Expire = FindByName(__.Expire);

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName(__.Enable);

            /// <summary>创建用户</summary>
            public static readonly Field CreateUserID = FindByName(__.CreateUserID);

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName(__.CreateIP);

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName(__.CreateTime);

            /// <summary>更新用户</summary>
            public static readonly Field UpdateUserID = FindByName(__.UpdateUserID);

            /// <summary>更新地址</summary>
            public static readonly Field UpdateIP = FindByName(__.UpdateIP);

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName(__.UpdateTime);

            /// <summary>备注</summary>
            public static readonly Field Remark = FindByName(__.Remark);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得用户链接字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>提供商</summary>
            public const String Provider = "Provider";

            /// <summary>用户。本地用户</summary>
            public const String UserID = "UserID";

            /// <summary>身份标识。用户名、OpenID</summary>
            public const String OpenID = "OpenID";

            /// <summary>用户编号。第三方用户编号</summary>
            public const String LinkID = "LinkID";

            /// <summary>昵称</summary>
            public const String NickName = "NickName";

            /// <summary>头像</summary>
            public const String Avatar = "Avatar";

            /// <summary>访问令牌</summary>
            public const String AccessToken = "AccessToken";

            /// <summary>刷新令牌</summary>
            public const String RefreshToken = "RefreshToken";

            /// <summary>过期时间</summary>
            public const String Expire = "Expire";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>创建用户</summary>
            public const String CreateUserID = "CreateUserID";

            /// <summary>创建地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

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

    /// <summary>用户链接。第三方绑定接口</summary>
    public partial interface IUserConnect
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>提供商</summary>
        String Provider { get; set; }

        /// <summary>用户。本地用户</summary>
        Int32 UserID { get; set; }

        /// <summary>身份标识。用户名、OpenID</summary>
        String OpenID { get; set; }

        /// <summary>用户编号。第三方用户编号</summary>
        Int64 LinkID { get; set; }

        /// <summary>昵称</summary>
        String NickName { get; set; }

        /// <summary>头像</summary>
        String Avatar { get; set; }

        /// <summary>访问令牌</summary>
        String AccessToken { get; set; }

        /// <summary>刷新令牌</summary>
        String RefreshToken { get; set; }

        /// <summary>过期时间</summary>
        DateTime Expire { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }

        /// <summary>创建用户</summary>
        Int32 CreateUserID { get; set; }

        /// <summary>创建地址</summary>
        String CreateIP { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

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