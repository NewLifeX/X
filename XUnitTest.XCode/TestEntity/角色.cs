using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XUnitTest.XCode.TestEntity
{
    /// <summary>角色</summary>
    [Serializable]
    [DataObject]
    [Description("角色")]
    [BindIndex("IU_Role2_Name", true, "Name")]
    [BindTable("Role2", Description = "角色", ConnName = "test", DbType = DatabaseType.None)]
    public partial class Role2
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private Boolean _IsSystem;
        /// <summary>系统。用于业务系统开发使用，不受数据权限约束，禁止修改名称或删除</summary>
        [DisplayName("系统")]
        [Description("系统。用于业务系统开发使用，不受数据权限约束，禁止修改名称或删除")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("IsSystem", "系统。用于业务系统开发使用，不受数据权限约束，禁止修改名称或删除", "")]
        public Boolean IsSystem { get => _IsSystem; set { if (OnPropertyChanging("IsSystem", value)) { _IsSystem = value; OnPropertyChanged("IsSystem"); } } }

        private String _Remark;
        /// <summary>说明</summary>
        [DisplayName("说明")]
        [Description("说明")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Remark", "说明", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }

        private String _Permission;
        /// <summary>权限。对不同资源的权限，逗号分隔，每个资源的权限子项竖线分隔</summary>
        [DisplayName("权限")]
        [Description("权限。对不同资源的权限，逗号分隔，每个资源的权限子项竖线分隔")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Permission", "权限。对不同资源的权限，逗号分隔，每个资源的权限子项竖线分隔", "")]
        public String Permission { get => _Permission; set { if (OnPropertyChanging("Permission", value)) { _Permission = value; OnPropertyChanged("Permission"); } } }

        private Int32 _CreateUserID;
        /// <summary>创建用户</summary>
        [DisplayName("创建用户")]
        [Description("创建用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserID", "创建用户", "")]
        public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

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
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private Int32 _UpdateUserID;
        /// <summary>更新用户</summary>
        [DisplayName("更新用户")]
        [Description("更新用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateUserID", "更新用户", "")]
        public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

        private String _UpdateIP;
        /// <summary>更新地址</summary>
        [DisplayName("更新地址")]
        [Description("更新地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateIP", "更新地址", "")]
        public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
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
                    case "Name": return _Name;
                    case "IsSystem": return _IsSystem;
                    case "Remark": return _Remark;
                    case "Permission": return _Permission;
                    case "CreateUserID": return _CreateUserID;
                    case "CreateIP": return _CreateIP;
                    case "CreateTime": return _CreateTime;
                    case "UpdateUserID": return _UpdateUserID;
                    case "UpdateIP": return _UpdateIP;
                    case "UpdateTime": return _UpdateTime;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "IsSystem": _IsSystem = value.ToBoolean(); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    case "Permission": _Permission = Convert.ToString(value); break;
                    case "CreateUserID": _CreateUserID = value.ToInt(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                    case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得角色字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>系统。用于业务系统开发使用，不受数据权限约束，禁止修改名称或删除</summary>
            public static readonly Field IsSystem = FindByName("IsSystem");

            /// <summary>说明</summary>
            public static readonly Field Remark = FindByName("Remark");

            /// <summary>权限。对不同资源的权限，逗号分隔，每个资源的权限子项竖线分隔</summary>
            public static readonly Field Permission = FindByName("Permission");

            /// <summary>创建用户</summary>
            public static readonly Field CreateUserID = FindByName("CreateUserID");

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName("CreateIP");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>更新用户</summary>
            public static readonly Field UpdateUserID = FindByName("UpdateUserID");

            /// <summary>更新地址</summary>
            public static readonly Field UpdateIP = FindByName("UpdateIP");

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得角色字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>系统。用于业务系统开发使用，不受数据权限约束，禁止修改名称或删除</summary>
            public const String IsSystem = "IsSystem";

            /// <summary>说明</summary>
            public const String Remark = "Remark";

            /// <summary>权限。对不同资源的权限，逗号分隔，每个资源的权限子项竖线分隔</summary>
            public const String Permission = "Permission";

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
        }
        #endregion
    }
}