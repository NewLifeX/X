using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Membership
{
    /// <summary>菜单</summary>
    [Serializable]
    [DataObject]
    [Description("菜单")]
    [BindIndex("IX_Menu2_Name", false, "Name")]
    [BindIndex("IU_Menu2_ParentID_Name", true, "ParentID,Name")]
    [BindTable("Menu2", Description = "菜单", ConnName = "test", DbType = DatabaseType.None)]
    public partial class Menu2 : IMenu2
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get => _ID; set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } } }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [DisplayName("显示名")]
        [Description("显示名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("DisplayName", "显示名", "")]
        public String DisplayName { get => _DisplayName; set { if (OnPropertyChanging(__.DisplayName, value)) { _DisplayName = value; OnPropertyChanged(__.DisplayName); } } }

        private String _FullName;
        /// <summary>全名</summary>
        [DisplayName("全名")]
        [Description("全名")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("FullName", "全名", "")]
        public String FullName { get => _FullName; set { if (OnPropertyChanging(__.FullName, value)) { _FullName = value; OnPropertyChanged(__.FullName); } } }

        private Int32 _ParentID;
        /// <summary>父编号</summary>
        [DisplayName("父编号")]
        [Description("父编号")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ParentID", "父编号", "")]
        public Int32 ParentID { get => _ParentID; set { if (OnPropertyChanging(__.ParentID, value)) { _ParentID = value; OnPropertyChanged(__.ParentID); } } }

        private String _Url;
        /// <summary>链接</summary>
        [DisplayName("链接")]
        [Description("链接")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Url", "链接", "")]
        public String Url { get => _Url; set { if (OnPropertyChanging(__.Url, value)) { _Url = value; OnPropertyChanged(__.Url); } } }

        private Int32 _Sort;
        /// <summary>排序</summary>
        [DisplayName("排序")]
        [Description("排序")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Sort", "排序", "")]
        public Int32 Sort { get => _Sort; set { if (OnPropertyChanging(__.Sort, value)) { _Sort = value; OnPropertyChanged(__.Sort); } } }

        private String _Icon;
        /// <summary>图标</summary>
        [DisplayName("图标")]
        [Description("图标")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Icon", "图标", "")]
        public String Icon { get => _Icon; set { if (OnPropertyChanging(__.Icon, value)) { _Icon = value; OnPropertyChanged(__.Icon); } } }

        private Boolean _Visible;
        /// <summary>可见</summary>
        [DisplayName("可见")]
        [Description("可见")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Visible", "可见", "")]
        public Boolean Visible { get => _Visible; set { if (OnPropertyChanging(__.Visible, value)) { _Visible = value; OnPropertyChanged(__.Visible); } } }

        private Boolean _Necessary;
        /// <summary>必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
        [DisplayName("必要")]
        [Description("必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Necessary", "必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色", "")]
        public Boolean Necessary { get => _Necessary; set { if (OnPropertyChanging(__.Necessary, value)) { _Necessary = value; OnPropertyChanged(__.Necessary); } } }

        private String _Permission;
        /// <summary>权限子项。逗号分隔，每个权限子项名值竖线分隔</summary>
        [DisplayName("权限子项")]
        [Description("权限子项。逗号分隔，每个权限子项名值竖线分隔")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Permission", "权限子项。逗号分隔，每个权限子项名值竖线分隔", "")]
        public String Permission { get => _Permission; set { if (OnPropertyChanging(__.Permission, value)) { _Permission = value; OnPropertyChanged(__.Permission); } } }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Remark", "备注", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging(__.Remark, value)) { _Remark = value; OnPropertyChanged(__.Remark); } } }
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
                    case __.ID: return _ID;
                    case __.Name: return _Name;
                    case __.DisplayName: return _DisplayName;
                    case __.FullName: return _FullName;
                    case __.ParentID: return _ParentID;
                    case __.Url: return _Url;
                    case __.Sort: return _Sort;
                    case __.Icon: return _Icon;
                    case __.Visible: return _Visible;
                    case __.Necessary: return _Necessary;
                    case __.Permission: return _Permission;
                    case __.Remark: return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID: _ID = value.ToInt(); break;
                    case __.Name: _Name = Convert.ToString(value); break;
                    case __.DisplayName: _DisplayName = Convert.ToString(value); break;
                    case __.FullName: _FullName = Convert.ToString(value); break;
                    case __.ParentID: _ParentID = value.ToInt(); break;
                    case __.Url: _Url = Convert.ToString(value); break;
                    case __.Sort: _Sort = value.ToInt(); break;
                    case __.Icon: _Icon = Convert.ToString(value); break;
                    case __.Visible: _Visible = value.ToBoolean(); break;
                    case __.Necessary: _Necessary = value.ToBoolean(); break;
                    case __.Permission: _Permission = Convert.ToString(value); break;
                    case __.Remark: _Remark = Convert.ToString(value); break;
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
            public static readonly Field ID = FindByName(__.ID);

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            /// <summary>显示名</summary>
            public static readonly Field DisplayName = FindByName(__.DisplayName);

            /// <summary>全名</summary>
            public static readonly Field FullName = FindByName(__.FullName);

            /// <summary>父编号</summary>
            public static readonly Field ParentID = FindByName(__.ParentID);

            /// <summary>链接</summary>
            public static readonly Field Url = FindByName(__.Url);

            /// <summary>排序</summary>
            public static readonly Field Sort = FindByName(__.Sort);

            /// <summary>图标</summary>
            public static readonly Field Icon = FindByName(__.Icon);

            /// <summary>可见</summary>
            public static readonly Field Visible = FindByName(__.Visible);

            /// <summary>必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
            public static readonly Field Necessary = FindByName(__.Necessary);

            /// <summary>权限子项。逗号分隔，每个权限子项名值竖线分隔</summary>
            public static readonly Field Permission = FindByName(__.Permission);

            /// <summary>备注</summary>
            public static readonly Field Remark = FindByName(__.Remark);

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

    /// <summary>菜单接口</summary>
    public partial interface IMenu2
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>显示名</summary>
        String DisplayName { get; set; }

        /// <summary>全名</summary>
        String FullName { get; set; }

        /// <summary>父编号</summary>
        Int32 ParentID { get; set; }

        /// <summary>链接</summary>
        String Url { get; set; }

        /// <summary>排序</summary>
        Int32 Sort { get; set; }

        /// <summary>图标</summary>
        String Icon { get; set; }

        /// <summary>可见</summary>
        Boolean Visible { get; set; }

        /// <summary>必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
        Boolean Necessary { get; set; }

        /// <summary>权限子项。逗号分隔，每个权限子项名值竖线分隔</summary>
        String Permission { get; set; }

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