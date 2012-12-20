﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>部门架构</summary>
    [Serializable]
    [DataObject]
    [Description("部门架构")]
    [BindIndex("IX_Code", false, "Code")]
    [BindIndex("IX_Name", false, "Name")]
    [BindIndex("PK_DepartmentStructure", true, "ID")]
    [BindTable("Department", Description = "部门架构", ConnName = "massql20_XCodeTest", DbType = DatabaseType.SqlServer)]
    public partial class Department<TEntity> : IDepartmentStructure
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
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private String _Name;
        /// <summary>护照或者签证</summary>
        [DisplayName("护照或者签证")]
        [Description("护照或者签证")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "护照或者签证", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private String _Code;
        /// <summary>货号</summary>
        [DisplayName("货号")]
        [Description("货号")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Code", "货号", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Code
        {
            get { return _Code; }
            set { if (OnPropertyChanging("Code", value)) { _Code = value; OnPropertyChanged("Code"); } }
        }

        private Int32 _ParentID;
        /// <summary>父分类</summary>
        [DisplayName("父分类")]
        [Description("父分类")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "ParentID", "父分类", null, "int", 10, 0, false)]
        public virtual Int32 ParentID
        {
            get { return _ParentID; }
            set { if (OnPropertyChanging("ParentID", value)) { _ParentID = value; OnPropertyChanged("ParentID"); } }
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
            set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } }
        }

        private String _Profile;
        /// <summary>配置文件</summary>
        [DisplayName("配置文件")]
        [Description("配置文件")]
        [DataObjectField(false, false, true, 1073741823)]
        [BindColumn(6, "Profile", "配置文件", null, "ntext", 0, 0, true)]
        public virtual String Profile
        {
            get { return _Profile; }
            set { if (OnPropertyChanging("Profile", value)) { _Profile = value; OnPropertyChanged("Profile"); } }
        }

        private String _Manager;
        /// <summary>管理器</summary>
        [DisplayName("管理器")]
        [Description("管理器")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "Manager", "管理器", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Manager
        {
            get { return _Manager; }
            set { if (OnPropertyChanging("Manager", value)) { _Manager = value; OnPropertyChanged("Manager"); } }
        }

        private String _Tel;
        /// <summary>固定电话</summary>
        [DisplayName("固定电话")]
        [Description("固定电话")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(8, "Tel", "固定电话", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Tel
        {
            get { return _Tel; }
            set { if (OnPropertyChanging("Tel", value)) { _Tel = value; OnPropertyChanged("Tel"); } }
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
                    case "ID" : return _ID;
                    case "Name" : return _Name;
                    case "Code" : return _Code;
                    case "ParentID" : return _ParentID;
                    case "Sort" : return _Sort;
                    case "Profile" : return _Profile;
                    case "Manager" : return _Manager;
                    case "Tel" : return _Tel;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    case "Code" : _Code = Convert.ToString(value); break;
                    case "ParentID" : _ParentID = Convert.ToInt32(value); break;
                    case "Sort" : _Sort = Convert.ToInt32(value); break;
                    case "Profile" : _Profile = Convert.ToString(value); break;
                    case "Manager" : _Manager = Convert.ToString(value); break;
                    case "Tel" : _Tel = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得部门架构字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            ///<summary>护照或者签证</summary>
            public static readonly Field Name = FindByName("Name");

            ///<summary>货号</summary>
            public static readonly Field Code = FindByName("Code");

            ///<summary>父分类</summary>
            public static readonly Field ParentID = FindByName("ParentID");

            ///<summary>排序</summary>
            public static readonly Field Sort = FindByName("Sort");

            ///<summary>配置文件</summary>
            public static readonly Field Profile = FindByName("Profile");

            ///<summary>管理器</summary>
            public static readonly Field Manager = FindByName("Manager");

            ///<summary>固定电话</summary>
            public static readonly Field Tel = FindByName("Tel");

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }
        #endregion
    }

    /// <summary>部门架构接口</summary>
    public partial interface IDepartmentStructure
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>护照或者签证</summary>
        String Name { get; set; }

        /// <summary>货号</summary>
        String Code { get; set; }

        /// <summary>父分类</summary>
        Int32 ParentID { get; set; }

        /// <summary>排序</summary>
        Int32 Sort { get; set; }

        /// <summary>配置文件</summary>
        String Profile { get; set; }

        /// <summary>管理器</summary>
        String Manager { get; set; }

        /// <summary>固定电话</summary>
        String Tel { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}