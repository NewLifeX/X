﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>序列</summary>
    [Serializable]
    [DataObject]
    [Description("序列")]
    [BindIndex("IU_Sequence_Name", true, "Name")]
    [BindTable("Sequence", Description = "序列", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public abstract partial class Sequence<TEntity> : ISequence
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

        private Int32 _Kind;
        /// <summary>种类</summary>
        [DisplayName("种类")]
        [Description("种类")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "Kind", "种类", null, "int", 10, 0, false)]
        public virtual Int32 Kind
        {
            get { return _Kind; }
            set { if (OnPropertyChanging(__.Kind, value)) { _Kind = value; OnPropertyChanged(__.Kind); } }
        }

        private Int32 _Num;
        /// <summary>数字</summary>
        [DisplayName("数字")]
        [Description("数字")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "Num", "数字", null, "int", 10, 0, false)]
        public virtual Int32 Num
        {
            get { return _Num; }
            set { if (OnPropertyChanging(__.Num, value)) { _Num = value; OnPropertyChanged(__.Num); } }
        }

        private DateTime _LastUpdate;
        /// <summary>最后更新</summary>
        [DisplayName("最后更新")]
        [Description("最后更新")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(5, "LastUpdate", "最后更新", null, "datetime", 3, 0, false)]
        public virtual DateTime LastUpdate
        {
            get { return _LastUpdate; }
            set { if (OnPropertyChanging(__.LastUpdate, value)) { _LastUpdate = value; OnPropertyChanged(__.LastUpdate); } }
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
                    case __.Kind : return _Kind;
                    case __.Num : return _Num;
                    case __.LastUpdate : return _LastUpdate;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.Kind : _Kind = Convert.ToInt32(value); break;
                    case __.Num : _Num = Convert.ToInt32(value); break;
                    case __.LastUpdate : _LastUpdate = Convert.ToDateTime(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得序列字段信息的快捷方式</summary>
        partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>名称</summary>
            public static readonly Field Name = FindByName(__.Name);

            ///<summary>种类</summary>
            public static readonly Field Kind = FindByName(__.Kind);

            ///<summary>数字</summary>
            public static readonly Field Num = FindByName(__.Num);

            ///<summary>最后更新</summary>
            public static readonly Field LastUpdate = FindByName(__.LastUpdate);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得序列字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>名称</summary>
            public const String Name = "Name";

            ///<summary>种类</summary>
            public const String Kind = "Kind";

            ///<summary>数字</summary>
            public const String Num = "Num";

            ///<summary>最后更新</summary>
            public const String LastUpdate = "LastUpdate";

        }
        #endregion
    }

    /// <summary>序列接口</summary>
    public partial interface ISequence
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>种类</summary>
        Int32 Kind { get; set; }

        /// <summary>数字</summary>
        Int32 Num { get; set; }

        /// <summary>最后更新</summary>
        DateTime LastUpdate { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}