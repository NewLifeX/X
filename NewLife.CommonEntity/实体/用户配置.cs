﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>用户配置</summary>
    [Serializable]
    [DataObject]
    [Description("用户配置")]
    [BindIndex("IX_UserProfile_UserID", false, "UserID")]
    [BindIndex("IU_UserProfile_Name_UserID", true, "Name,UserID")]
    [BindTable("UserProfile", Description = "用户配置", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public abstract partial class UserProfile<TEntity> : IUserProfile
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

        private Int32 _UserID;
        /// <summary>父编号</summary>
        [DisplayName("父编号")]
        [Description("父编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(2, "UserID", "父编号", null, "int", 10, 0, false)]
        public virtual Int32 UserID
        {
            get { return _UserID; }
            set { if (OnPropertyChanging(__.UserID, value)) { _UserID = value; OnPropertyChanged(__.UserID); } }
        }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Name", "名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } }
        }

        private Int32 _Kind;
        /// <summary>值类型</summary>
        [DisplayName("值类型")]
        [Description("值类型")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "Kind", "值类型", null, "int", 10, 0, false)]
        public virtual Int32 Kind
        {
            get { return _Kind; }
            set { if (OnPropertyChanging(__.Kind, value)) { _Kind = value; OnPropertyChanged(__.Kind); } }
        }

        private String _Value;
        /// <summary>值</summary>
        [DisplayName("值")]
        [Description("值")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn(5, "Value", "值", null, "nvarchar(500)", 0, 0, true)]
        public virtual String Value
        {
            get { return _Value; }
            set { if (OnPropertyChanging(__.Value, value)) { _Value = value; OnPropertyChanged(__.Value); } }
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
                    case __.UserID : return _UserID;
                    case __.Name : return _Name;
                    case __.Kind : return _Kind;
                    case __.Value : return _Value;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.UserID : _UserID = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.Kind : _Kind = Convert.ToInt32(value); break;
                    case __.Value : _Value = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得用户配置字段信息的快捷方式</summary>
        partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>父编号</summary>
            public static readonly Field UserID = FindByName(__.UserID);

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>值类型</summary>
            public static readonly Field Kind = FindByName(__.Kind);

            ///<summary>值</summary>
            public static readonly Field Value = FindByName(__.Value);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得用户配置字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>父编号</summary>
            public const String UserID = "UserID";

            ///<summary>名称</summary>
            public const String Name = "Name";

            ///<summary>值类型</summary>
            public const String Kind = "Kind";

            ///<summary>值</summary>
            public const String Value = "Value";

        }
        #endregion
    }

    /// <summary>用户配置接口</summary>
    public partial interface IUserProfile
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>父编号</summary>
        Int32 UserID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>值类型</summary>
        Int32 Kind { get; set; }

        /// <summary>值</summary>
        String Value { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}