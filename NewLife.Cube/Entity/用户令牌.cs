using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Entity
{
    /// <summary>用户令牌。授权其他人直接拥有指定用户的身份，支持有效期，支持数据接口</summary>
    [Serializable]
    [DataObject]
    [Description("用户令牌。授权其他人直接拥有指定用户的身份，支持有效期，支持数据接口")]
    [BindIndex("IU_UserToken_Token", true, "Token")]
    [BindIndex("IX_UserToken_UserID", false, "UserID")]
    [BindTable("UserToken", Description = "用户令牌。授权其他人直接拥有指定用户的身份，支持有效期，支持数据接口", ConnName = "Membership", DbType = DatabaseType.None)]
    public partial class UserToken : IUserToken
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get { return _ID; } set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } } }

        private String _Token;
        /// <summary>令牌</summary>
        [DisplayName("令牌")]
        [Description("令牌")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Token", "令牌", "")]
        public String Token { get { return _Token; } set { if (OnPropertyChanging(__.Token, value)) { _Token = value; OnPropertyChanged(__.Token); } } }

        private String _Url;
        /// <summary>地址。锁定该令牌只能访问该资源路径</summary>
        [DisplayName("地址")]
        [Description("地址。锁定该令牌只能访问该资源路径")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Url", "地址。锁定该令牌只能访问该资源路径", "")]
        public String Url { get { return _Url; } set { if (OnPropertyChanging(__.Url, value)) { _Url = value; OnPropertyChanged(__.Url); } } }

        private Int32 _UserID;
        /// <summary>用户。本地用户</summary>
        [DisplayName("用户")]
        [Description("用户。本地用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UserID", "用户。本地用户", "")]
        public Int32 UserID { get { return _UserID; } set { if (OnPropertyChanging(__.UserID, value)) { _UserID = value; OnPropertyChanged(__.UserID); } } }

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

        private Int32 _Times;
        /// <summary>次数。该令牌使用次数</summary>
        [DisplayName("次数")]
        [Description("次数。该令牌使用次数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Times", "次数。该令牌使用次数", "")]
        public Int32 Times { get { return _Times; } set { if (OnPropertyChanging(__.Times, value)) { _Times = value; OnPropertyChanged(__.Times); } } }

        private String _LastIP;
        /// <summary>最后地址</summary>
        [DisplayName("最后地址")]
        [Description("最后地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("LastIP", "最后地址", "")]
        public String LastIP { get { return _LastIP; } set { if (OnPropertyChanging(__.LastIP, value)) { _LastIP = value; OnPropertyChanged(__.LastIP); } } }

        private DateTime _LastTime;
        /// <summary>最后时间</summary>
        [DisplayName("最后时间")]
        [Description("最后时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("LastTime", "最后时间", "")]
        public DateTime LastTime { get { return _LastTime; } set { if (OnPropertyChanging(__.LastTime, value)) { _LastTime = value; OnPropertyChanged(__.LastTime); } } }

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
                    case __.Token : return _Token;
                    case __.Url : return _Url;
                    case __.UserID : return _UserID;
                    case __.Expire : return _Expire;
                    case __.Enable : return _Enable;
                    case __.Times : return _Times;
                    case __.LastIP : return _LastIP;
                    case __.LastTime : return _LastTime;
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
                    case __.Token : _Token = Convert.ToString(value); break;
                    case __.Url : _Url = Convert.ToString(value); break;
                    case __.UserID : _UserID = Convert.ToInt32(value); break;
                    case __.Expire : _Expire = Convert.ToDateTime(value); break;
                    case __.Enable : _Enable = Convert.ToBoolean(value); break;
                    case __.Times : _Times = Convert.ToInt32(value); break;
                    case __.LastIP : _LastIP = Convert.ToString(value); break;
                    case __.LastTime : _LastTime = Convert.ToDateTime(value); break;
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
        /// <summary>取得用户令牌字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            /// <summary>令牌</summary>
            public static readonly Field Token = FindByName(__.Token);

            /// <summary>地址。锁定该令牌只能访问该资源路径</summary>
            public static readonly Field Url = FindByName(__.Url);

            /// <summary>用户。本地用户</summary>
            public static readonly Field UserID = FindByName(__.UserID);

            /// <summary>过期时间</summary>
            public static readonly Field Expire = FindByName(__.Expire);

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName(__.Enable);

            /// <summary>次数。该令牌使用次数</summary>
            public static readonly Field Times = FindByName(__.Times);

            /// <summary>最后地址</summary>
            public static readonly Field LastIP = FindByName(__.LastIP);

            /// <summary>最后时间</summary>
            public static readonly Field LastTime = FindByName(__.LastTime);

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

        /// <summary>取得用户令牌字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>令牌</summary>
            public const String Token = "Token";

            /// <summary>地址。锁定该令牌只能访问该资源路径</summary>
            public const String Url = "Url";

            /// <summary>用户。本地用户</summary>
            public const String UserID = "UserID";

            /// <summary>过期时间</summary>
            public const String Expire = "Expire";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>次数。该令牌使用次数</summary>
            public const String Times = "Times";

            /// <summary>最后地址</summary>
            public const String LastIP = "LastIP";

            /// <summary>最后时间</summary>
            public const String LastTime = "LastTime";

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

    /// <summary>用户令牌。授权其他人直接拥有指定用户的身份，支持有效期，支持数据接口接口</summary>
    public partial interface IUserToken
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>令牌</summary>
        String Token { get; set; }

        /// <summary>地址。锁定该令牌只能访问该资源路径</summary>
        String Url { get; set; }

        /// <summary>用户。本地用户</summary>
        Int32 UserID { get; set; }

        /// <summary>过期时间</summary>
        DateTime Expire { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }

        /// <summary>次数。该令牌使用次数</summary>
        Int32 Times { get; set; }

        /// <summary>最后地址</summary>
        String LastIP { get; set; }

        /// <summary>最后时间</summary>
        DateTime LastTime { get; set; }

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