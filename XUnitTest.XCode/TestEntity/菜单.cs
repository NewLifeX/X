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
    /// <summary>菜单</summary>
    [Serializable]
    [DataObject]
    [Description("菜单")]
    [BindIndex("IX_Menu2_Name", false, "Name")]
    [BindIndex("IU_Menu2_ParentID_Name", true, "ParentID,Name")]
    [BindTable("Menu2", Description = "菜单", ConnName = "test", DbType = DatabaseType.None)]
    public partial class Menu2
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

        private String _DisplayName;
        /// <summary>显示名</summary>
        [DisplayName("显示名")]
        [Description("显示名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("DisplayName", "显示名", "")]
        public String DisplayName { get => _DisplayName; set { if (OnPropertyChanging("DisplayName", value)) { _DisplayName = value; OnPropertyChanged("DisplayName"); } } }

        private String _FullName;
        /// <summary>全名</summary>
        [DisplayName("全名")]
        [Description("全名")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("FullName", "全名", "")]
        public String FullName { get => _FullName; set { if (OnPropertyChanging("FullName", value)) { _FullName = value; OnPropertyChanged("FullName"); } } }

        private Int32 _ParentID;
        /// <summary>父编号</summary>
        [DisplayName("父编号")]
        [Description("父编号")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ParentID", "父编号", "")]
        public Int32 ParentID { get => _ParentID; set { if (OnPropertyChanging("ParentID", value)) { _ParentID = value; OnPropertyChanged("ParentID"); } } }

        private String _Url;
        /// <summary>链接</summary>
        [DisplayName("链接")]
        [Description("链接")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Url", "链接", "")]
        public String Url { get => _Url; set { if (OnPropertyChanging("Url", value)) { _Url = value; OnPropertyChanged("Url"); } } }

        private Int32 _Sort;
        /// <summary>排序</summary>
        [DisplayName("排序")]
        [Description("排序")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Sort", "排序", "")]
        public Int32 Sort { get => _Sort; set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } } }

        private String _Icon;
        /// <summary>图标</summary>
        [DisplayName("图标")]
        [Description("图标")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Icon", "图标", "")]
        public String Icon { get => _Icon; set { if (OnPropertyChanging("Icon", value)) { _Icon = value; OnPropertyChanged("Icon"); } } }

        private Boolean _Visible;
        /// <summary>可见</summary>
        [DisplayName("可见")]
        [Description("可见")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Visible", "可见", "")]
        public Boolean Visible { get => _Visible; set { if (OnPropertyChanging("Visible", value)) { _Visible = value; OnPropertyChanged("Visible"); } } }

        private Boolean _Necessary;
        /// <summary>必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
        [DisplayName("必要")]
        [Description("必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Necessary", "必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色", "")]
        public Boolean Necessary { get => _Necessary; set { if (OnPropertyChanging("Necessary", value)) { _Necessary = value; OnPropertyChanged("Necessary"); } } }

        private String _Permission;
        /// <summary>权限子项。逗号分隔，每个权限子项名值竖线分隔</summary>
        [DisplayName("权限子项")]
        [Description("权限子项。逗号分隔，每个权限子项名值竖线分隔")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Permission", "权限子项。逗号分隔，每个权限子项名值竖线分隔", "")]
        public String Permission { get => _Permission; set { if (OnPropertyChanging("Permission", value)) { _Permission = value; OnPropertyChanged("Permission"); } } }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Remark", "备注", "")]
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
                    case "Name": return _Name;
                    case "DisplayName": return _DisplayName;
                    case "FullName": return _FullName;
                    case "ParentID": return _ParentID;
                    case "Url": return _Url;
                    case "Sort": return _Sort;
                    case "Icon": return _Icon;
                    case "Visible": return _Visible;
                    case "Necessary": return _Necessary;
                    case "Permission": return _Permission;
                    case "Remark": return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "DisplayName": _DisplayName = Convert.ToString(value); break;
                    case "FullName": _FullName = Convert.ToString(value); break;
                    case "ParentID": _ParentID = value.ToInt(); break;
                    case "Url": _Url = Convert.ToString(value); break;
                    case "Sort": _Sort = value.ToInt(); break;
                    case "Icon": _Icon = Convert.ToString(value); break;
                    case "Visible": _Visible = value.ToBoolean(); break;
                    case "Necessary": _Necessary = value.ToBoolean(); break;
                    case "Permission": _Permission = Convert.ToString(value); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得菜单字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>显示名</summary>
            public static readonly Field DisplayName = FindByName("DisplayName");

            /// <summary>全名</summary>
            public static readonly Field FullName = FindByName("FullName");

            /// <summary>父编号</summary>
            public static readonly Field ParentID = FindByName("ParentID");

            /// <summary>链接</summary>
            public static readonly Field Url = FindByName("Url");

            /// <summary>排序</summary>
            public static readonly Field Sort = FindByName("Sort");

            /// <summary>图标</summary>
            public static readonly Field Icon = FindByName("Icon");

            /// <summary>可见</summary>
            public static readonly Field Visible = FindByName("Visible");

            /// <summary>必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
            public static readonly Field Necessary = FindByName("Necessary");

            /// <summary>权限子项。逗号分隔，每个权限子项名值竖线分隔</summary>
            public static readonly Field Permission = FindByName("Permission");

            /// <summary>备注</summary>
            public static readonly Field Remark = FindByName("Remark");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得菜单字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>显示名</summary>
            public const String DisplayName = "DisplayName";

            /// <summary>全名</summary>
            public const String FullName = "FullName";

            /// <summary>父编号</summary>
            public const String ParentID = "ParentID";

            /// <summary>链接</summary>
            public const String Url = "Url";

            /// <summary>排序</summary>
            public const String Sort = "Sort";

            /// <summary>图标</summary>
            public const String Icon = "Icon";

            /// <summary>可见</summary>
            public const String Visible = "Visible";

            /// <summary>必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
            public const String Necessary = "Necessary";

            /// <summary>权限子项。逗号分隔，每个权限子项名值竖线分隔</summary>
            public const String Permission = "Permission";

            /// <summary>备注</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }
}