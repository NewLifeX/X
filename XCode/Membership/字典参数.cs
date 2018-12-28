using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Membership
{
    /// <summary>字典参数</summary>
    [Serializable]
    [DataObject]
    [Description("字典参数")]
    [BindIndex("IU_Parameter_Category_Name", true, "Category,Name")]
    [BindIndex("IX_Parameter_Name", false, "Name")]
    [BindIndex("IX_Parameter_UpdateTime", false, "UpdateTime")]
    [BindTable("Parameter", Description = "字典参数", ConnName = "Membership", DbType = DatabaseType.None)]
    public partial class Parameter : IParameter
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get { return _ID; } set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } } }

        private String _Category;
        /// <summary>类别</summary>
        [DisplayName("类别")]
        [Description("类别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Category", "类别", "")]
        public String Category { get { return _Category; } set { if (OnPropertyChanging(__.Category, value)) { _Category = value; OnPropertyChanged(__.Category); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get { return _Name; } set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } } }

        private String _Value;
        /// <summary>数值</summary>
        [DisplayName("数值")]
        [Description("数值")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Value", "数值", "")]
        public String Value { get { return _Value; } set { if (OnPropertyChanging(__.Value, value)) { _Value = value; OnPropertyChanged(__.Value); } } }

        private String _LongValue;
        /// <summary>长数值</summary>
        [DisplayName("长数值")]
        [Description("长数值")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("LongValue", "长数值", "")]
        public String LongValue { get { return _LongValue; } set { if (OnPropertyChanging(__.LongValue, value)) { _LongValue = value; OnPropertyChanged(__.LongValue); } } }

        private XCode.Membership.ParameterKinds _Kind;
        /// <summary>种类。0普通，21列表，22名值</summary>
        [DisplayName("种类")]
        [Description("种类。0普通，21列表，22名值")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Kind", "种类。0普通，21列表，22名值", "")]
        public XCode.Membership.ParameterKinds Kind { get { return _Kind; } set { if (OnPropertyChanging(__.Kind, value)) { _Kind = value; OnPropertyChanged(__.Kind); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get { return _Enable; } set { if (OnPropertyChanging(__.Enable, value)) { _Enable = value; OnPropertyChanged(__.Enable); } } }

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
        [DataObjectField(false, false, true, 0)]
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
        [DataObjectField(false, false, true, 0)]
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
                    case __.Category : return _Category;
                    case __.Name : return _Name;
                    case __.Value : return _Value;
                    case __.LongValue : return _LongValue;
                    case __.Kind : return _Kind;
                    case __.Enable : return _Enable;
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
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Category : _Category = Convert.ToString(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.Value : _Value = Convert.ToString(value); break;
                    case __.LongValue : _LongValue = Convert.ToString(value); break;
                    case __.Kind : _Kind = (XCode.Membership.ParameterKinds)Convert.ToInt32(value); break;
                    case __.Enable : _Enable = Convert.ToBoolean(value); break;
                    case __.Ex1 : _Ex1 = Convert.ToInt32(value); break;
                    case __.Ex2 : _Ex2 = Convert.ToInt32(value); break;
                    case __.Ex3 : _Ex3 = Convert.ToDouble(value); break;
                    case __.Ex4 : _Ex4 = Convert.ToString(value); break;
                    case __.Ex5 : _Ex5 = Convert.ToString(value); break;
                    case __.Ex6 : _Ex6 = Convert.ToString(value); break;
                    case __.CreateUser : _CreateUser = Convert.ToString(value); break;
                    case __.CreateUserID : _CreateUserID = Convert.ToInt32(value); break;
                    case __.CreateIP : _CreateIP = Convert.ToString(value); break;
                    case __.CreateTime : _CreateTime = Convert.ToDateTime(value); break;
                    case __.UpdateUser : _UpdateUser = Convert.ToString(value); break;
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
        /// <summary>取得字典参数字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            /// <summary>类别</summary>
            public static readonly Field Category = FindByName(__.Category);

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            /// <summary>数值</summary>
            public static readonly Field Value = FindByName(__.Value);

            /// <summary>长数值</summary>
            public static readonly Field LongValue = FindByName(__.LongValue);

            /// <summary>种类。0普通，21列表，22名值</summary>
            public static readonly Field Kind = FindByName(__.Kind);

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName(__.Enable);

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

        /// <summary>取得字典参数字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>类别</summary>
            public const String Category = "Category";

            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>数值</summary>
            public const String Value = "Value";

            /// <summary>长数值</summary>
            public const String LongValue = "LongValue";

            /// <summary>种类。0普通，21列表，22名值</summary>
            public const String Kind = "Kind";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

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

    /// <summary>字典参数接口</summary>
    public partial interface IParameter
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>类别</summary>
        String Category { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>数值</summary>
        String Value { get; set; }

        /// <summary>长数值</summary>
        String LongValue { get; set; }

        /// <summary>种类。0普通，21列表，22名值</summary>
        XCode.Membership.ParameterKinds Kind { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }

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