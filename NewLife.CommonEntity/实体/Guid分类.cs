﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>Guid分类</summary>
    [Serializable]
    [DataObject]
    [Description("Guid分类")]
    [BindIndex("IU_GuidCategory_Name", true, "Name")]
    [BindIndex("IX_GuidCategory_ParentGuid", false, "ParentGuid")]
    [BindTable("GuidCategory", Description = "Guid分类", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public abstract partial class GuidCategory<TEntity> : IGuidCategory
    {
        #region 属性
        private Guid _Guid;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, false, false, 0)]
        [BindColumn(1, "Guid", "编号", null, "uniqueidentifier", 0, 0, false)]
        public virtual Guid Guid
        {
            get { return _Guid; }
            set { if (OnPropertyChanging(__.Guid, value)) { _Guid = value; OnPropertyChanged(__.Guid); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(2, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } }
        }

        private String _ParentGuid;
        /// <summary>父分类</summary>
        [DisplayName("父分类")]
        [Description("父分类")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "ParentGuid", "父分类", null, "nvarchar(50)", 0, 0, true)]
        public virtual String ParentGuid
        {
            get { return _ParentGuid; }
            set { if (OnPropertyChanging(__.ParentGuid, value)) { _ParentGuid = value; OnPropertyChanged(__.ParentGuid); } }
        }

        private Int32 _Sort;
        /// <summary>排序</summary>
        [DisplayName("排序")]
        [Description("排序")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "Sort", "排序", null, "int", 10, 0, false)]
        public virtual Int32 Sort
        {
            get { return _Sort; }
            set { if (OnPropertyChanging(__.Sort, value)) { _Sort = value; OnPropertyChanged(__.Sort); } }
        }

        private String _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 250)]
        [BindColumn(5, "Remark", "备注", null, "nvarchar(250)", 0, 0, true)]
        public virtual String Remark
        {
            get { return _Remark; }
            set { if (OnPropertyChanging(__.Remark, value)) { _Remark = value; OnPropertyChanged(__.Remark); } }
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
                    case __.Guid : return _Guid;
                    case __.Name : return _Name;
                    case __.ParentGuid : return _ParentGuid;
                    case __.Sort : return _Sort;
                    case __.Remark : return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.Guid : _Guid = (Guid)value; break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.ParentGuid : _ParentGuid = Convert.ToString(value); break;
                    case __.Sort : _Sort = Convert.ToInt32(value); break;
                    case __.Remark : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得Guid分类字段信息的快捷方式</summary>
        partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field Guid = FindByName(__.Guid);

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>父分类</summary>
            public static readonly Field ParentGuid = FindByName(__.ParentGuid);

            ///<summary>排序</summary>
            public static readonly Field Sort = FindByName(__.Sort);

            ///<summary>备注</summary>
            public static readonly Field Remark = FindByName(__.Remark);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得Guid分类字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String Guid = "Guid";

            ///<summary>名称</summary>
            public const String Name = "Name";

            ///<summary>父分类</summary>
            public const String ParentGuid = "ParentGuid";

            ///<summary>排序</summary>
            public const String Sort = "Sort";

            ///<summary>备注</summary>
            public const String Remark = "Remark";

        }
        #endregion
    }

    /// <summary>Guid分类接口</summary>
    public partial interface IGuidCategory
    {
        #region 属性
        /// <summary>编号</summary>
        Guid Guid { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>父分类</summary>
        String ParentGuid { get; set; }

        /// <summary>排序</summary>
        Int32 Sort { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}