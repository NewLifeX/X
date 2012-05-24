/*
 * XCoder v4.8.4526.33278
 * 作者：nnhy/NEWLIFE
 * 时间：2012-05-24 16:52:27
 * 版权：版权所有 (C) 新生命开发团队 2012
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>简单信息</summary>
    [Serializable]
    [DataObject]
    [Description("简单信息")]
    [BindIndex("IX_Simple", true, "Name")]
    [BindIndex("PK__Simple__3214EC270AD2A005", true, "ID")]
    [BindIndex("PK_Simple", true, "ID")]
    [BindTable("Simple", Description = "简单信息", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Simple<TEntity> : ISimple
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
        /// <summary>信息名称</summary>
        [DisplayName("信息名称")]
        [Description("信息名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "信息名称", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
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
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得简单信息字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            ///<summary>信息名称</summary>
            public static readonly Field Name = FindByName("Name");

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }
        #endregion
    }

    /// <summary>简单信息接口</summary>
    public partial interface ISimple
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>信息名称</summary>
        String Name { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}