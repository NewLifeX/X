﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Membership
{
    /// <summary>日志</summary>
    [Serializable]
    [DataObject]
    [Description("日志")]
    [BindIndex("IX_Log_Category", false, "Category")]
    [BindTable("Log", Description = "日志", ConnName = "Membership", DbType = DatabaseType.SqlServer)]
    public abstract partial class Log<TEntity> : ILog
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

        private String _Category;
        /// <summary>类别</summary>
        [DisplayName("类别")]
        [Description("类别")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Category", "类别", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Category
        {
            get { return _Category; }
            set { if (OnPropertyChanging(__.Category, value)) { _Category = value; OnPropertyChanged(__.Category); } }
        }

        private String _Action;
        /// <summary>操作</summary>
        [DisplayName("操作")]
        [Description("操作")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(3, "Action", "操作", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Action
        {
            get { return _Action; }
            set { if (OnPropertyChanging(__.Action, value)) { _Action = value; OnPropertyChanged(__.Action); } }
        }

        private Int32 _UserID;
        /// <summary>用户编号</summary>
        [DisplayName("用户编号")]
        [Description("用户编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(4, "UserID", "用户编号", null, "int", 10, 0, false)]
        public virtual Int32 UserID
        {
            get { return _UserID; }
            set { if (OnPropertyChanging(__.UserID, value)) { _UserID = value; OnPropertyChanged(__.UserID); } }
        }

        private String _UserName;
        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        [Description("用户名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "UserName", "用户名", null, "nvarchar(50)", 0, 0, true)]
        public virtual String UserName
        {
            get { return _UserName; }
            set { if (OnPropertyChanging(__.UserName, value)) { _UserName = value; OnPropertyChanged(__.UserName); } }
        }

        private String _IP;
        /// <summary>IP地址</summary>
        [DisplayName("IP地址")]
        [Description("IP地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(6, "IP", "IP地址", null, "nvarchar(50)", 0, 0, true)]
        public virtual String IP
        {
            get { return _IP; }
            set { if (OnPropertyChanging(__.IP, value)) { _IP = value; OnPropertyChanged(__.IP); } }
        }

        private DateTime _OccurTime;
        /// <summary>时间</summary>
        [DisplayName("时间")]
        [Description("时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(7, "OccurTime", "时间", null, "datetime", 3, 0, false)]
        public virtual DateTime OccurTime
        {
            get { return _OccurTime; }
            set { if (OnPropertyChanging(__.OccurTime, value)) { _OccurTime = value; OnPropertyChanged(__.OccurTime); } }
        }

        private String _Remark;
        /// <summary>详细信息</summary>
        [DisplayName("详细信息")]
        [Description("详细信息")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn(8, "Remark", "详细信息", null, "nvarchar(500)", 0, 0, true)]
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
                    case __.ID : return _ID;
                    case __.Category : return _Category;
                    case __.Action : return _Action;
                    case __.UserID : return _UserID;
                    case __.UserName : return _UserName;
                    case __.IP : return _IP;
                    case __.OccurTime : return _OccurTime;
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
                    case __.Action : _Action = Convert.ToString(value); break;
                    case __.UserID : _UserID = Convert.ToInt32(value); break;
                    case __.UserName : _UserName = Convert.ToString(value); break;
                    case __.IP : _IP = Convert.ToString(value); break;
                    case __.OccurTime : _OccurTime = Convert.ToDateTime(value); break;
                    case __.Remark : _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得日志字段信息的快捷方式</summary>
        partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>类别</summary>
            public static readonly Field Category = FindByName(__.Category);

            ///<summary>操作</summary>
            public static readonly Field Action = FindByName(__.Action);

            ///<summary>用户编号</summary>
            public static readonly Field UserID = FindByName(__.UserID);

            ///<summary>用户名</summary>
            public static readonly Field UserName = FindByName(__.UserName);

            ///<summary>IP地址</summary>
            public static readonly Field IP = FindByName(__.IP);

            ///<summary>时间</summary>
            public static readonly Field OccurTime = FindByName(__.OccurTime);

            ///<summary>详细信息</summary>
            public static readonly Field Remark = FindByName(__.Remark);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得日志字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>类别</summary>
            public const String Category = "Category";

            ///<summary>操作</summary>
            public const String Action = "Action";

            ///<summary>用户编号</summary>
            public const String UserID = "UserID";

            ///<summary>用户名</summary>
            public const String UserName = "UserName";

            ///<summary>IP地址</summary>
            public const String IP = "IP";

            ///<summary>时间</summary>
            public const String OccurTime = "OccurTime";

            ///<summary>详细信息</summary>
            public const String Remark = "Remark";

        }
        #endregion
    }

    /// <summary>日志接口</summary>
    public partial interface ILog
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>类别</summary>
        String Category { get; set; }

        /// <summary>操作</summary>
        String Action { get; set; }

        /// <summary>用户编号</summary>
        Int32 UserID { get; set; }

        /// <summary>用户名</summary>
        String UserName { get; set; }

        /// <summary>IP地址</summary>
        String IP { get; set; }

        /// <summary>时间</summary>
        DateTime OccurTime { get; set; }

        /// <summary>详细信息</summary>
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