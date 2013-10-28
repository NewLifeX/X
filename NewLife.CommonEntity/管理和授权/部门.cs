﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>部门</summary>
    [Serializable]
    [DataObject]
    [Description("部门")]
    [BindIndex("IX_Department_Name", false, "Name")]
    [BindIndex("IX_Department_Code", false, "Code")]
    [BindIndex("IX_Department_ParentID_Name", true, "ParentID,Name")]
    [BindIndex("IX_Department_ParentID_Code", false, "ParentID,Code")]
    [BindTable("Department", Description = "部门", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Department<TEntity> : IDepartment
    {
        #region 属性

        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn(1, "ID", "编号", null, "int", 10, 0, false)]
        public virtual Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } }
        }

        private String _Code;
        /// <summary>代码</summary>
        [DisplayName("代码")]
        [Description("代码")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Code", "代码", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Code
        {
            get { return _Code; }
            set { if (OnPropertyChanging(__.Code, value)) { _Code = value; OnPropertyChanged(__.Code); } }
        }

        private Int32 _ParentID;
        /// <summary>父编号</summary>
        [DisplayName("父编号")]
        [Description("父编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "ParentID", "父编号", null, "int", 10, 0, false)]
        public virtual Int32 ParentID
        {
            get { return _ParentID; }
            set { if (OnPropertyChanging(__.ParentID, value)) { _ParentID = value; OnPropertyChanged(__.ParentID); } }
        }

        private Int32 _Sort;
        /// <summary>排序</summary>
        [DisplayName("排序")]
        [Description("排序")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(5, "Sort", "排序", null, "int", 10, 0, false)]
        public virtual Int32 Sort
        {
            get { return _Sort; }
            set { if (OnPropertyChanging(__.Sort, value)) { _Sort = value; OnPropertyChanged(__.Sort); } }
        }

        private Int32 _ManagerID;
        /// <summary>管理者编号</summary>
        [DisplayName("管理者编号")]
        [Description("管理者编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(6, "ManagerID", "管理者编号", null, "int", 10, 0, false)]
        public virtual Int32 ManagerID
        {
            get { return _ManagerID; }
            set { if (OnPropertyChanging(__.ManagerID, value)) { _ManagerID = value; OnPropertyChanged(__.ManagerID); } }
        }

        private String _Manager;
        /// <summary>管理者</summary>
        [DisplayName("管理者")]
        [Description("管理者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "Manager", "管理者", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Manager
        {
            get { return _Manager; }
            set { if (OnPropertyChanging(__.Manager, value)) { _Manager = value; OnPropertyChanged(__.Manager); } }
        }

        private Int32 _Level;
        /// <summary>等级</summary>
        [DisplayName("等级")]
        [Description("等级")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(8, "Level", "等级", null, "int", 10, 0, false)]
        public virtual Int32 Level
        {
            get { return _Level; }
            set { if (OnPropertyChanging(__.Level, value)) { _Level = value; OnPropertyChanged(__.Level); } }
        }

        private String _LevelName;
        /// <summary>等级名称</summary>
        [DisplayName("等级名称")]
        [Description("等级名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(9, "LevelName", "等级名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String LevelName
        {
            get { return _LevelName; }
            set { if (OnPropertyChanging(__.LevelName, value)) { _LevelName = value; OnPropertyChanged(__.LevelName); } }
        }

        private String _Profile;
        /// <summary>配置文件</summary>
        [DisplayName("配置文件")]
        [Description("配置文件")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn(10, "Profile", "配置文件", null, "nvarchar(500)", 0, 0, true)]
        public virtual String Profile
        {
            get { return _Profile; }
            set { if (OnPropertyChanging(__.Profile, value)) { _Profile = value; OnPropertyChanged(__.Profile); } }
        }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，基类使用反射实现。
        /// 派生实体类可重写该索引，以避免反射带来的性能损耗
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case __.ID : return _ID;
                    case __.Name : return _Name;
                    case __.Code : return _Code;
                    case __.ParentID : return _ParentID;
                    case __.Sort : return _Sort;
                    case __.ManagerID : return _ManagerID;
                    case __.Manager : return _Manager;
                    case __.Level : return _Level;
                    case __.LevelName : return _LevelName;
                    case __.Profile : return _Profile;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.Code : _Code = Convert.ToString(value); break;
                    case __.ParentID : _ParentID = Convert.ToInt32(value); break;
                    case __.Sort : _Sort = Convert.ToInt32(value); break;
                    case __.ManagerID : _ManagerID = Convert.ToInt32(value); break;
                    case __.Manager : _Manager = Convert.ToString(value); break;
                    case __.Level : _Level = Convert.ToInt32(value); break;
                    case __.LevelName : _LevelName = Convert.ToString(value); break;
                    case __.Profile : _Profile = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得部门字段信息的快捷方式</summary>
        public partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>代码</summary>
            public static readonly Field Code = FindByName(__.Code);

            ///<summary>父编号</summary>
            public static readonly Field ParentID = FindByName(__.ParentID);

            ///<summary>排序</summary>
            public static readonly Field Sort = FindByName(__.Sort);

            ///<summary>管理者编号</summary>
            public static readonly Field ManagerID = FindByName(__.ManagerID);

            ///<summary>管理者</summary>
            public static readonly Field Manager = FindByName(__.Manager);

            ///<summary>等级</summary>
            public static readonly Field Level = FindByName(__.Level);

            ///<summary>等级名称</summary>
            public static readonly Field LevelName = FindByName(__.LevelName);

            ///<summary>配置文件</summary>
            public static readonly Field Profile = FindByName(__.Profile);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得部门字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>名称</summary>
            public const String Name = "Name";

            ///<summary>代码</summary>
            public const String Code = "Code";

            ///<summary>父编号</summary>
            public const String ParentID = "ParentID";

            ///<summary>排序</summary>
            public const String Sort = "Sort";

            ///<summary>管理者编号</summary>
            public const String ManagerID = "ManagerID";

            ///<summary>管理者</summary>
            public const String Manager = "Manager";

            ///<summary>等级</summary>
            public const String Level = "Level";

            ///<summary>等级名称</summary>
            public const String LevelName = "LevelName";

            ///<summary>配置文件</summary>
            public const String Profile = "Profile";

        }
        #endregion
    }

    /// <summary>部门接口</summary>
    public partial interface IDepartment
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>代码</summary>
        String Code { get; set; }

        /// <summary>父编号</summary>
        Int32 ParentID { get; set; }

        /// <summary>排序</summary>
        Int32 Sort { get; set; }

        /// <summary>管理者编号</summary>
        Int32 ManagerID { get; set; }

        /// <summary>管理者</summary>
        String Manager { get; set; }

        /// <summary>等级</summary>
        Int32 Level { get; set; }

        /// <summary>等级名称</summary>
        String LevelName { get; set; }

        /// <summary>配置文件</summary>
        String Profile { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}