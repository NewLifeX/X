/*asdfasdfadfawerawer2wrwrwa
 * XCoder v4.5.2011.1108
 * 作者：nnhy/NEWLIFEasdf
 * 时间：2011-11-24 13:13:40
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

#pragma warning disable 3021
#pragma warning disable 3008
namespace NewLife.CommonEntity
{
    /// <summary>地区编码</summary>
    [Serializable]
    [DataObject]
    [Description("地区编码")]
    [BindIndex("IX_AreaCode", true, "Code")]
    [BindIndex("IX_AreaCode_1", false, "Name")]
    [BindIndex("PK_AreaCode", true, "ID")]
    [BindTable("AreaCode", Description = "地区编码", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class AreaCode<TEntity> : IAreaCode
    {
        #region 属性
        private Int32 _ID;
        /// <summary></summary>
        [DisplayName("")]
        [Description("")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn(1, "ID", "", null, "int", 10, 0, false)]
        public virtual Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private Int32 _Code;
        /// <summary></summary>
        [DisplayName("")]
        [Description("")]
        [DataObjectField(false, false, false, 10)]
        [BindColumn(2, "Code", "", null, "int", 10, 0, false)]
        public virtual Int32 Code
        {
            get { return _Code; }
            set { if (OnPropertyChanging("Code", value)) { _Code = value; OnPropertyChanged("Code"); } }
        }

        private String _Name;
        /// <summary></summary>
        [DisplayName("")]
        [Description("")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn(3, "Name", "", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private String _FullName;
        /// <summary></summary>
        [DisplayName("")]
        [Description("")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "FullName", "", null, "nvarchar(50)", 0, 0, true)]
        public virtual String FullName
        {
            get { return _FullName; }
            set { if (OnPropertyChanging("FullName", value)) { _FullName = value; OnPropertyChanged("FullName"); } }
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
                    case "Code" : return _Code;
                    case "Name" : return _Name;
                    case "FullName" : return _FullName;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "Code" : _Code = Convert.ToInt32(value); break;
                    case "Name" : _Name = Convert.ToString(value); break;
                    case "FullName" : _FullName = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得地区编码字段信息的快捷方式</summary>
        public class _
        {
            ///<summary></summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary></summary>
            public static readonly Field Code = Meta.Table.FindByName("Code");

            ///<summary></summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary></summary>
            public static readonly Field FullName = Meta.Table.FindByName("FullName");
        }
        #endregion
    }

    /// <summary>地区编码接口</summary>
    public partial interface IAreaCode
    {
        #region 属性
        /// <summary></summary>
        Int32 ID { get; set; }

        /// <summary></summary>
        Int32 Code { get; set; }

        /// <summary></summary>
        String Name { get; set; }

        /// <summary></summary>
        String FullName { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}
#pragma warning restore 3008
#pragma warning restore 3021