﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>地区</summary>
    [Serializable]
    [DataObject]
    [Description("地区")]
    [BindIndex("IU_Area_Code", true, "Code")]
    [BindIndex("IX_Area_Name", false, "Name")]
    [BindTable("Area", Description = "地区", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public abstract partial class Area<TEntity> : IArea
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

        private Int32 _Code;
        /// <summary>代码</summary>
        [DisplayName("代码")]
        [Description("代码")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(2, "Code", "代码", null, "int", 10, 0, false)]
        public virtual Int32 Code
        {
            get { return _Code; }
            set { if (OnPropertyChanging(__.Code, value)) { _Code = value; OnPropertyChanged(__.Code); } }
        }

        private Int32 _OldCode;
        /// <summary>旧版本代码</summary>
        [DisplayName("旧版本代码")]
        [Description("旧版本代码")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "OldCode", "旧版本代码", null, "int", 10, 0, false)]
        public virtual Int32 OldCode
        {
            get { return _OldCode; }
            set { if (OnPropertyChanging(__.OldCode, value)) { _OldCode = value; OnPropertyChanged(__.OldCode); } }
        }

        private Int32 _OldCode2;
        /// <summary>旧版本代码2</summary>
        [DisplayName("旧版本代码2")]
        [Description("旧版本代码2")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "OldCode2", "旧版本代码2", null, "int", 10, 0, false)]
        public virtual Int32 OldCode2
        {
            get { return _OldCode2; }
            set { if (OnPropertyChanging(__.OldCode2, value)) { _OldCode2 = value; OnPropertyChanged(__.OldCode2); } }
        }

        private Int32 _OldCode3;
        /// <summary>旧版本代码3</summary>
        [DisplayName("旧版本代码3")]
        [Description("旧版本代码3")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(5, "OldCode3", "旧版本代码3", null, "int", 10, 0, false)]
        public virtual Int32 OldCode3
        {
            get { return _OldCode3; }
            set { if (OnPropertyChanging(__.OldCode3, value)) { _OldCode3 = value; OnPropertyChanged(__.OldCode3); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(6, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } }
        }

        private Int32 _ParentCode;
        /// <summary>父地区代码</summary>
        [DisplayName("父地区代码")]
        [Description("父地区代码")]
        [DataObjectField(false, false, false, 10)]
        [BindColumn(7, "ParentCode", "父地区代码", "0", "int", 10, 0, false)]
        public virtual Int32 ParentCode
        {
            get { return _ParentCode; }
            set { if (OnPropertyChanging(__.ParentCode, value)) { _ParentCode = value; OnPropertyChanged(__.ParentCode); } }
        }

        private Int32 _Sort;
        /// <summary>序号</summary>
        [DisplayName("序号")]
        [Description("序号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(8, "Sort", "序号", "0", "int", 10, 0, false)]
        public virtual Int32 Sort
        {
            get { return _Sort; }
            set { if (OnPropertyChanging(__.Sort, value)) { _Sort = value; OnPropertyChanged(__.Sort); } }
        }

        private String _Description;
        /// <summary>描述</summary>
        [DisplayName("描述")]
        [Description("描述")]
        [DataObjectField(false, false, true, -1)]
        [BindColumn(9, "Description", "描述", null, "ntext", 0, 0, true)]
        public virtual String Description
        {
            get { return _Description; }
            set { if (OnPropertyChanging(__.Description, value)) { _Description = value; OnPropertyChanged(__.Description); } }
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
                    case __.Code : return _Code;
                    case __.OldCode : return _OldCode;
                    case __.OldCode2 : return _OldCode2;
                    case __.OldCode3 : return _OldCode3;
                    case __.Name : return _Name;
                    case __.ParentCode : return _ParentCode;
                    case __.Sort : return _Sort;
                    case __.Description : return _Description;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Code : _Code = Convert.ToInt32(value); break;
                    case __.OldCode : _OldCode = Convert.ToInt32(value); break;
                    case __.OldCode2 : _OldCode2 = Convert.ToInt32(value); break;
                    case __.OldCode3 : _OldCode3 = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.ParentCode : _ParentCode = Convert.ToInt32(value); break;
                    case __.Sort : _Sort = Convert.ToInt32(value); break;
                    case __.Description : _Description = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得地区字段信息的快捷方式</summary>
        partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>代码</summary>
            public static readonly Field Code = FindByName(__.Code);

            ///<summary>旧版本代码</summary>
            public static readonly Field OldCode = FindByName(__.OldCode);

            ///<summary>旧版本代码2</summary>
            public static readonly Field OldCode2 = FindByName(__.OldCode2);

            ///<summary>旧版本代码3</summary>
            public static readonly Field OldCode3 = FindByName(__.OldCode3);

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>父地区代码</summary>
            public static readonly Field ParentCode = FindByName(__.ParentCode);

            ///<summary>序号</summary>
            public static readonly Field Sort = FindByName(__.Sort);

            ///<summary>描述</summary>
            public static readonly Field Description = FindByName(__.Description);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得地区字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>代码</summary>
            public const String Code = "Code";

            ///<summary>旧版本代码</summary>
            public const String OldCode = "OldCode";

            ///<summary>旧版本代码2</summary>
            public const String OldCode2 = "OldCode2";

            ///<summary>旧版本代码3</summary>
            public const String OldCode3 = "OldCode3";

            ///<summary>名称</summary>
            public const String Name = "Name";

            ///<summary>父地区代码</summary>
            public const String ParentCode = "ParentCode";

            ///<summary>序号</summary>
            public const String Sort = "Sort";

            ///<summary>描述</summary>
            public const String Description = "Description";

        }
        #endregion
    }

    /// <summary>地区接口</summary>
    public partial interface IArea
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>代码</summary>
        Int32 Code { get; set; }

        /// <summary>旧版本代码</summary>
        Int32 OldCode { get; set; }

        /// <summary>旧版本代码2</summary>
        Int32 OldCode2 { get; set; }

        /// <summary>旧版本代码3</summary>
        Int32 OldCode3 { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>父地区代码</summary>
        Int32 ParentCode { get; set; }

        /// <summary>序号</summary>
        Int32 Sort { get; set; }

        /// <summary>描述</summary>
        String Description { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}