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
    /// <summary>日志</summary>
    [Serializable]
    [DataObject]
    [Description("日志")]
    [BindIndex("IX_Log2_Category", false, "Category")]
    [BindIndex("IX_Log2_CreateUserID", false, "CreateUserID")]
    [BindIndex("IX_Log2_CreateTime", false, "CreateTime")]
    [BindTable("Log2", Description = "日志", ConnName = "test", DbType = DatabaseType.None)]
    public partial class Log2
    {
        #region 属性
        private Int64 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, false, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int64 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

        private String _Category;
        /// <summary>类别</summary>
        [DisplayName("类别")]
        [Description("类别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Category", "类别", "")]
        public String Category { get => _Category; set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } } }

        private String _Action;
        /// <summary>操作</summary>
        [DisplayName("操作")]
        [Description("操作")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Action", "操作", "")]
        public String Action { get => _Action; set { if (OnPropertyChanging("Action", value)) { _Action = value; OnPropertyChanged("Action"); } } }

        private Int32 _LinkID;
        /// <summary>链接</summary>
        [DisplayName("链接")]
        [Description("链接")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("LinkID", "链接", "")]
        public Int32 LinkID { get => _LinkID; set { if (OnPropertyChanging("LinkID", value)) { _LinkID = value; OnPropertyChanged("LinkID"); } } }

        private String _UserName;
        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        [Description("用户名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UserName", "用户名", "")]
        public String UserName { get => _UserName; set { if (OnPropertyChanging("UserName", value)) { _UserName = value; OnPropertyChanged("UserName"); } } }

        private Int32 _CreateUserID;
        /// <summary>用户编号</summary>
        [DisplayName("用户编号")]
        [Description("用户编号")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserID", "用户编号", "")]
        public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

        private String _CreateIP;
        /// <summary>IP地址</summary>
        [DisplayName("IP地址")]
        [Description("IP地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "IP地址", "")]
        public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

        private DateTime _CreateTime;
        /// <summary>时间</summary>
        [DisplayName("时间")]
        [Description("时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private Byte[] _Remark;
        /// <summary>详细信息</summary>
        [DisplayName("详细信息")]
        [Description("详细信息")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("Remark", "详细信息", "")]
        public Byte[] Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
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
                    case "Category": return _Category;
                    case "Action": return _Action;
                    case "LinkID": return _LinkID;
                    case "UserName": return _UserName;
                    case "CreateUserID": return _CreateUserID;
                    case "CreateIP": return _CreateIP;
                    case "CreateTime": return _CreateTime;
                    case "Remark": return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = value.ToLong(); break;
                    case "Category": _Category = Convert.ToString(value); break;
                    case "Action": _Action = Convert.ToString(value); break;
                    case "LinkID": _LinkID = value.ToInt(); break;
                    case "UserName": _UserName = Convert.ToString(value); break;
                    case "CreateUserID": _CreateUserID = value.ToInt(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "Remark": _Remark = (Byte[])value; break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得日志字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>类别</summary>
            public static readonly Field Category = FindByName("Category");

            /// <summary>操作</summary>
            public static readonly Field Action = FindByName("Action");

            /// <summary>链接</summary>
            public static readonly Field LinkID = FindByName("LinkID");

            /// <summary>用户名</summary>
            public static readonly Field UserName = FindByName("UserName");

            /// <summary>用户编号</summary>
            public static readonly Field CreateUserID = FindByName("CreateUserID");

            /// <summary>IP地址</summary>
            public static readonly Field CreateIP = FindByName("CreateIP");

            /// <summary>时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>详细信息</summary>
            public static readonly Field Remark = FindByName("Remark");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得日志字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>类别</summary>
            public const String Category = "Category";

            /// <summary>操作</summary>
            public const String Action = "Action";

            /// <summary>链接</summary>
            public const String LinkID = "LinkID";

            /// <summary>用户名</summary>
            public const String UserName = "UserName";

            /// <summary>用户编号</summary>
            public const String CreateUserID = "CreateUserID";

            /// <summary>IP地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>详细信息</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }
}