using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Membership
{
    /// <summary>部门。组织机构，多级树状结构</summary>
    [Serializable]
    [DataObject]
    [Description("部门。组织机构，多级树状结构")]
    [BindIndex("IX_Department_Name", false, "Name")]
    [BindIndex("IU_Department_ParentID_Name", true, "ParentID,Name")]
    [BindIndex("IX_Department_Code", false, "Code")]
    [BindIndex("IX_Department_UpdateTime", false, "UpdateTime")]
    [BindTable("Department", Description = "部门。组织机构，多级树状结构", ConnName = "Membership", DbType = DatabaseType.None)]
    public partial class Department<TEntity> : IDepartment
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get { return _ID; } set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } } }

        private String _Code;
        /// <summary>代码</summary>
        [DisplayName("代码")]
        [Description("代码")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Code", "代码", "")]
        public String Code { get { return _Code; } set { if (OnPropertyChanging(__.Code, value)) { _Code = value; OnPropertyChanged(__.Code); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get { return _Name; } set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } } }

        private String _FullName;
        /// <summary>全名</summary>
        [DisplayName("全名")]
        [Description("全名")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("FullName", "全名", "")]
        public String FullName { get { return _FullName; } set { if (OnPropertyChanging(__.FullName, value)) { _FullName = value; OnPropertyChanged(__.FullName); } } }

        private Int32 _ParentID;
        /// <summary>父级</summary>
        [DisplayName("父级")]
        [Description("父级")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ParentID", "父级", "")]
        public Int32 ParentID { get { return _ParentID; } set { if (OnPropertyChanging(__.ParentID, value)) { _ParentID = value; OnPropertyChanged(__.ParentID); } } }

        private Int32 _Level;
        /// <summary>层级。树状结构的层级</summary>
        [DisplayName("层级")]
        [Description("层级。树状结构的层级")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Level", "层级。树状结构的层级", "")]
        public Int32 Level { get { return _Level; } set { if (OnPropertyChanging(__.Level, value)) { _Level = value; OnPropertyChanged(__.Level); } } }

        private Int32 _Sort;
        /// <summary>排序。同级内排序</summary>
        [DisplayName("排序")]
        [Description("排序。同级内排序")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Sort", "排序。同级内排序", "")]
        public Int32 Sort { get { return _Sort; } set { if (OnPropertyChanging(__.Sort, value)) { _Sort = value; OnPropertyChanged(__.Sort); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get { return _Enable; } set { if (OnPropertyChanging(__.Enable, value)) { _Enable = value; OnPropertyChanged(__.Enable); } } }

        private Boolean _Visible;
        /// <summary>可见</summary>
        [DisplayName("可见")]
        [Description("可见")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Visible", "可见", "")]
        public Boolean Visible { get { return _Visible; } set { if (OnPropertyChanging(__.Visible, value)) { _Visible = value; OnPropertyChanged(__.Visible); } } }

        private Int32 _Ex1;
        /// <summary>扩展1</summary>
        [DisplayName("扩展1")]
        [Description("扩展1")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Ex1", "扩展1", "")]
        public Int32 Ex1 { get { return _Ex1; } set { if (OnPropertyChanging(__.Ex1, value)) { _Ex1 = value; OnPropertyChanged(__.Ex1); } } }

        private Int32 _Ex2;
        /// <summary>扩展2</summary>
        [DisplayName("扩展2")]
        [Description("扩展2")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Ex2", "扩展2", "")]
        public Int32 Ex2 { get { return _Ex2; } set { if (OnPropertyChanging(__.Ex2, value)) { _Ex2 = value; OnPropertyChanged(__.Ex2); } } }

        private Double _Ex3;
        /// <summary>扩展3</summary>
        [DisplayName("扩展3")]
        [Description("扩展3")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Ex3", "扩展3", "")]
        public Double Ex3 { get { return _Ex3; } set { if (OnPropertyChanging(__.Ex3, value)) { _Ex3 = value; OnPropertyChanged(__.Ex3); } } }

        private String _Ex4;
        /// <summary>扩展4</summary>
        [DisplayName("扩展4")]
        [Description("扩展4")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Ex4", "扩展4", "")]
        public String Ex4 { get { return _Ex4; } set { if (OnPropertyChanging(__.Ex4, value)) { _Ex4 = value; OnPropertyChanged(__.Ex4); } } }

        private String _Ex5;
        /// <summary>扩展5</summary>
        [DisplayName("扩展5")]
        [Description("扩展5")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Ex5", "扩展5", "")]
        public String Ex5 { get { return _Ex5; } set { if (OnPropertyChanging(__.Ex5, value)) { _Ex5 = value; OnPropertyChanged(__.Ex5); } } }

        private String _Ex6;
        /// <summary>扩展6</summary>
        [DisplayName("扩展6")]
        [Description("扩展6")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Ex6", "扩展6", "")]
        public String Ex6 { get { return _Ex6; } set { if (OnPropertyChanging(__.Ex6, value)) { _Ex6 = value; OnPropertyChanged(__.Ex6); } } }

        private String _CreateUser;
        /// <summary>创建用户</summary>
        [DisplayName("创建用户")]
        [Description("创建用户")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateUser", "创建用户", "")]
        public String CreateUser { get { return _CreateUser; } set { if (OnPropertyChanging(__.CreateUser, value)) { _CreateUser = value; OnPropertyChanged(__.CreateUser); } } }

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
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get { return _CreateTime; } set { if (OnPropertyChanging(__.CreateTime, value)) { _CreateTime = value; OnPropertyChanged(__.CreateTime); } } }

        private String _UpdateUser;
        /// <summary>更新用户</summary>
        [DisplayName("更新用户")]
        [Description("更新用户")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateUser", "更新用户", "")]
        public String UpdateUser { get { return _UpdateUser; } set { if (OnPropertyChanging(__.UpdateUser, value)) { _UpdateUser = value; OnPropertyChanged(__.UpdateUser); } } }

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
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get { return _UpdateTime; } set { if (OnPropertyChanging(__.UpdateTime, value)) { _UpdateTime = value; OnPropertyChanged(__.UpdateTime); } } }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 200)]
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
                    case __.Code : return _Code;
                    case __.Name : return _Name;
                    case __.FullName : return _FullName;
                    case __.ParentID : return _ParentID;
                    case __.Level : return _Level;
                    case __.Sort : return _Sort;
                    case __.Enable : return _Enable;
                    case __.Visible : return _Visible;
                    case __.Ex1 : return _Ex1;
                    case __.Ex2 : return _Ex2;
                    case __.Ex3 : return _Ex3;
                    case __.Ex4 : return _Ex4;
                    case __.Ex5 : return _Ex5;
                    case __.Ex6 : return _Ex6;
                    case __.CreateUser : return _CreateUser;
                    case __.CreateUserID : return _CreateUserID;
                    case __.CreateIP : return _CreateIP;
                    case __.CreateTime : return _CreateTime;
                    case __.UpdateUser : return _UpdateUser;
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
                    case __.ID : _ID = value.ToInt(); break;
                    case __.Code : _Code = Convert.ToString(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.FullName : _FullName = Convert.ToString(value); break;
                    case __.ParentID : _ParentID = value.ToInt(); break;
                    case __.Level : _Level = value.ToInt(); break;
                    case __.Sort : _Sort = value.ToInt(); break;
                    case __.Enable : _Enable = value.ToBoolean(); break;
                    case __.Visible : _Visible = value.ToBoolean(); break;
                    case __.Ex1 : _Ex1 = value.ToInt(); break;
                    case __.Ex2 : _Ex2 = value.ToInt(); break;
                    case __.Ex3 : _Ex3 = value.ToDouble(); break;
                    case __.Ex4 : _Ex4 = Convert.ToString(value); break;
                    case __.Ex5 : _Ex5 = Convert.ToString(value); break;
                    case __.Ex6 : _Ex6 = Convert.ToString(value); break;
                    case __.CreateUser : _CreateUser = Convert.ToString(value); break;
                    case __.CreateUserID : _CreateUserID = value.ToInt(); break;
                    case __.CreateIP : _CreateIP = Convert.ToString(value); break;
                    case __.CreateTime : _CreateTime = value.ToDateTime(); break;
                    case __.UpdateUser : _UpdateUser = Convert.ToString(value); break;
                    case __.UpdateUserID : _UpdateUserID = value.ToInt(); break;
                    case __.UpdateIP : _UpdateIP = Convert.ToString(value); break;
                    case __.UpdateTime : _UpdateTime = value.ToDateTime(); break;
                    case __.Remark : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得部门字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            /// <summary>代码</summary>
            public static readonly Field Code = FindByName(__.Code);

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            /// <summary>全名</summary>
            public static readonly Field FullName = FindByName(__.FullName);

            /// <summary>父级</summary>
            public static readonly Field ParentID = FindByName(__.ParentID);

            /// <summary>层级。树状结构的层级</summary>
            public static readonly Field Level = FindByName(__.Level);

            /// <summary>排序。同级内排序</summary>
            public static readonly Field Sort = FindByName(__.Sort);

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName(__.Enable);

            /// <summary>可见</summary>
            public static readonly Field Visible = FindByName(__.Visible);

            /// <summary>扩展1</summary>
            public static readonly Field Ex1 = FindByName(__.Ex1);

            /// <summary>扩展2</summary>
            public static readonly Field Ex2 = FindByName(__.Ex2);

            /// <summary>扩展3</summary>
            public static readonly Field Ex3 = FindByName(__.Ex3);

            /// <summary>扩展4</summary>
            public static readonly Field Ex4 = FindByName(__.Ex4);

            /// <summary>扩展5</summary>
            public static readonly Field Ex5 = FindByName(__.Ex5);

            /// <summary>扩展6</summary>
            public static readonly Field Ex6 = FindByName(__.Ex6);

            /// <summary>创建用户</summary>
            public static readonly Field CreateUser = FindByName(__.CreateUser);

            /// <summary>创建用户</summary>
            public static readonly Field CreateUserID = FindByName(__.CreateUserID);

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName(__.CreateIP);

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName(__.CreateTime);

            /// <summary>更新用户</summary>
            public static readonly Field UpdateUser = FindByName(__.UpdateUser);

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

        /// <summary>取得部门字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>代码</summary>
            public const String Code = "Code";

            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>全名</summary>
            public const String FullName = "FullName";

            /// <summary>父级</summary>
            public const String ParentID = "ParentID";

            /// <summary>层级。树状结构的层级</summary>
            public const String Level = "Level";

            /// <summary>排序。同级内排序</summary>
            public const String Sort = "Sort";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>可见</summary>
            public const String Visible = "Visible";

            /// <summary>扩展1</summary>
            public const String Ex1 = "Ex1";

            /// <summary>扩展2</summary>
            public const String Ex2 = "Ex2";

            /// <summary>扩展3</summary>
            public const String Ex3 = "Ex3";

            /// <summary>扩展4</summary>
            public const String Ex4 = "Ex4";

            /// <summary>扩展5</summary>
            public const String Ex5 = "Ex5";

            /// <summary>扩展6</summary>
            public const String Ex6 = "Ex6";

            /// <summary>创建用户</summary>
            public const String CreateUser = "CreateUser";

            /// <summary>创建用户</summary>
            public const String CreateUserID = "CreateUserID";

            /// <summary>创建地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>更新用户</summary>
            public const String UpdateUser = "UpdateUser";

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

    /// <summary>部门。组织机构，多级树状结构接口</summary>
    public partial interface IDepartment
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>代码</summary>
        String Code { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>全名</summary>
        String FullName { get; set; }

        /// <summary>父级</summary>
        Int32 ParentID { get; set; }

        /// <summary>层级。树状结构的层级</summary>
        Int32 Level { get; set; }

        /// <summary>排序。同级内排序</summary>
        Int32 Sort { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }

        /// <summary>可见</summary>
        Boolean Visible { get; set; }

        /// <summary>扩展1</summary>
        Int32 Ex1 { get; set; }

        /// <summary>扩展2</summary>
        Int32 Ex2 { get; set; }

        /// <summary>扩展3</summary>
        Double Ex3 { get; set; }

        /// <summary>扩展4</summary>
        String Ex4 { get; set; }

        /// <summary>扩展5</summary>
        String Ex5 { get; set; }

        /// <summary>扩展6</summary>
        String Ex6 { get; set; }

        /// <summary>创建用户</summary>
        String CreateUser { get; set; }

        /// <summary>创建用户</summary>
        Int32 CreateUserID { get; set; }

        /// <summary>创建地址</summary>
        String CreateIP { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>更新用户</summary>
        String UpdateUser { get; set; }

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