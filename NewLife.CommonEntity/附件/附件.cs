﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>附件</summary>
    [Serializable]
    [DataObject]
    [Description("附件")]
    [BindIndex("IX_Attachment_Category", false, "Category")]
    [BindTable("Attachment", Description = "附件", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public abstract partial class Attachment<TEntity> : IAttachment
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

        private String _FileName;
        /// <summary>文件名</summary>
        [DisplayName("文件名")]
        [Description("文件名")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(2, "FileName", "文件名", null, "nvarchar(200)", 0, 0, true)]
        public virtual String FileName
        {
            get { return _FileName; }
            set { if (OnPropertyChanging(__.FileName, value)) { _FileName = value; OnPropertyChanged(__.FileName); } }
        }

        private Int32 _Size;
        /// <summary>大小</summary>
        [DisplayName("大小")]
        [Description("大小")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "Size", "大小", null, "int", 10, 0, false)]
        public virtual Int32 Size
        {
            get { return _Size; }
            set { if (OnPropertyChanging(__.Size, value)) { _Size = value; OnPropertyChanged(__.Size); } }
        }

        private String _Extension;
        /// <summary>扩展名</summary>
        [DisplayName("扩展名")]
        [Description("扩展名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Extension", "扩展名", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Extension
        {
            get { return _Extension; }
            set { if (OnPropertyChanging(__.Extension, value)) { _Extension = value; OnPropertyChanged(__.Extension); } }
        }

        private String _Category;
        /// <summary>分类</summary>
        [DisplayName("分类")]
        [Description("分类")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "Category", "分类", null, "nvarchar(50)", 0, 0, true)]
        public virtual String Category
        {
            get { return _Category; }
            set { if (OnPropertyChanging(__.Category, value)) { _Category = value; OnPropertyChanged(__.Category); } }
        }

        private String _FilePath;
        /// <summary>文件路径</summary>
        [DisplayName("文件路径")]
        [Description("文件路径")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(6, "FilePath", "文件路径", null, "nvarchar(200)", 0, 0, true)]
        public virtual String FilePath
        {
            get { return _FilePath; }
            set { if (OnPropertyChanging(__.FilePath, value)) { _FilePath = value; OnPropertyChanged(__.FilePath); } }
        }

        private String _UserName;
        /// <summary>上传者</summary>
        [DisplayName("上传者")]
        [Description("上传者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "UserName", "上传者", null, "nvarchar(50)", 0, 0, true)]
        public virtual String UserName
        {
            get { return _UserName; }
            set { if (OnPropertyChanging(__.UserName, value)) { _UserName = value; OnPropertyChanged(__.UserName); } }
        }

        private DateTime _UploadTime;
        /// <summary>上传时间</summary>
        [DisplayName("上传时间")]
        [Description("上传时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(8, "UploadTime", "上传时间", null, "datetime", 3, 0, false)]
        public virtual DateTime UploadTime
        {
            get { return _UploadTime; }
            set { if (OnPropertyChanging(__.UploadTime, value)) { _UploadTime = value; OnPropertyChanged(__.UploadTime); } }
        }

        private String _ContentType;
        /// <summary>内容类型</summary>
        [DisplayName("内容类型")]
        [Description("内容类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(9, "ContentType", "内容类型", null, "nvarchar(50)", 0, 0, true)]
        public virtual String ContentType
        {
            get { return _ContentType; }
            set { if (OnPropertyChanging(__.ContentType, value)) { _ContentType = value; OnPropertyChanged(__.ContentType); } }
        }

        private Int32 _StatID;
        /// <summary>访问统计</summary>
        [DisplayName("访问统计")]
        [Description("访问统计")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(10, "StatID", "访问统计", null, "int", 10, 0, false)]
        public virtual Int32 StatID
        {
            get { return _StatID; }
            set { if (OnPropertyChanging(__.StatID, value)) { _StatID = value; OnPropertyChanged(__.StatID); } }
        }

        private Boolean _IsEnable;
        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        [Description("是否启用")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(11, "IsEnable", "是否启用", null, "bit", 0, 0, false)]
        public virtual Boolean IsEnable
        {
            get { return _IsEnable; }
            set { if (OnPropertyChanging(__.IsEnable, value)) { _IsEnable = value; OnPropertyChanged(__.IsEnable); } }
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
                    case __.FileName : return _FileName;
                    case __.Size : return _Size;
                    case __.Extension : return _Extension;
                    case __.Category : return _Category;
                    case __.FilePath : return _FilePath;
                    case __.UserName : return _UserName;
                    case __.UploadTime : return _UploadTime;
                    case __.ContentType : return _ContentType;
                    case __.StatID : return _StatID;
                    case __.IsEnable : return _IsEnable;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.FileName : _FileName = Convert.ToString(value); break;
                    case __.Size : _Size = Convert.ToInt32(value); break;
                    case __.Extension : _Extension = Convert.ToString(value); break;
                    case __.Category : _Category = Convert.ToString(value); break;
                    case __.FilePath : _FilePath = Convert.ToString(value); break;
                    case __.UserName : _UserName = Convert.ToString(value); break;
                    case __.UploadTime : _UploadTime = Convert.ToDateTime(value); break;
                    case __.ContentType : _ContentType = Convert.ToString(value); break;
                    case __.StatID : _StatID = Convert.ToInt32(value); break;
                    case __.IsEnable : _IsEnable = Convert.ToBoolean(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得附件字段信息的快捷方式</summary>
        partial class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = FindByName(__.ID);

            ///<summary>文件名</summary>
            public static readonly Field FileName = FindByName(__.FileName);

            ///<summary>大小</summary>
            public static readonly Field Size = FindByName(__.Size);

            ///<summary>扩展名</summary>
            public static readonly Field Extension = FindByName(__.Extension);

            ///<summary>分类</summary>
            public static readonly Field Category = FindByName(__.Category);

            ///<summary>文件路径</summary>
            public static readonly Field FilePath = FindByName(__.FilePath);

            ///<summary>上传者</summary>
            public static readonly Field UserName = FindByName(__.UserName);

            ///<summary>上传时间</summary>
            public static readonly Field UploadTime = FindByName(__.UploadTime);

            ///<summary>内容类型</summary>
            public static readonly Field ContentType = FindByName(__.ContentType);

            ///<summary>访问统计</summary>
            public static readonly Field StatID = FindByName(__.StatID);

            ///<summary>是否启用</summary>
            public static readonly Field IsEnable = FindByName(__.IsEnable);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得附件字段名称的快捷方式</summary>
        partial class __
        {
            ///<summary>编号</summary>
            public const String ID = "ID";

            ///<summary>文件名</summary>
            public const String FileName = "FileName";

            ///<summary>大小</summary>
            public const String Size = "Size";

            ///<summary>扩展名</summary>
            public const String Extension = "Extension";

            ///<summary>分类</summary>
            public const String Category = "Category";

            ///<summary>文件路径</summary>
            public const String FilePath = "FilePath";

            ///<summary>上传者</summary>
            public const String UserName = "UserName";

            ///<summary>上传时间</summary>
            public const String UploadTime = "UploadTime";

            ///<summary>内容类型</summary>
            public const String ContentType = "ContentType";

            ///<summary>访问统计</summary>
            public const String StatID = "StatID";

            ///<summary>是否启用</summary>
            public const String IsEnable = "IsEnable";

        }
        #endregion
    }

    /// <summary>附件接口</summary>
    public partial interface IAttachment
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>文件名</summary>
        String FileName { get; set; }

        /// <summary>大小</summary>
        Int32 Size { get; set; }

        /// <summary>扩展名</summary>
        String Extension { get; set; }

        /// <summary>分类</summary>
        String Category { get; set; }

        /// <summary>文件路径</summary>
        String FilePath { get; set; }

        /// <summary>上传者</summary>
        String UserName { get; set; }

        /// <summary>上传时间</summary>
        DateTime UploadTime { get; set; }

        /// <summary>内容类型</summary>
        String ContentType { get; set; }

        /// <summary>访问统计</summary>
        Int32 StatID { get; set; }

        /// <summary>是否启用</summary>
        Boolean IsEnable { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}