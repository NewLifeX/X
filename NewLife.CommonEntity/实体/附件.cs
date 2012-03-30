/*
 * XCoder v3.4.2011.0329
 * 作者：nnhy/NEWLIFE
 * 时间：2011-05-06 10:35:57
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>附件</summary>>
    [Serializable]
    [DataObject]
    [Description("附件")]
    [BindTable("Attachment", Description = "附件", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Attachment<TEntity> : IAttachment
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>>
        [Description("编号")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn(1, "ID", "编号", "", "int", 10, 0, false)]
        public Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private String _FileName;
        /// <summary>文件名</summary>>
        [Description("文件名")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(2, "FileName", "文件名", "", "nvarchar(200)", 0, 0, true)]
        public String FileName
        {
            get { return _FileName; }
            set { if (OnPropertyChanging("FileName", value)) { _FileName = value; OnPropertyChanged("FileName"); } }
        }

        private Int32 _Size;
        /// <summary>大小</summary>>
        [Description("大小")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "Size", "大小", "", "int", 10, 0, false)]
        public Int32 Size
        {
            get { return _Size; }
            set { if (OnPropertyChanging("Size", value)) { _Size = value; OnPropertyChanged("Size"); } }
        }

        private String _Extension;
        /// <summary>扩展名</summary>>
        [Description("扩展名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Extension", "扩展名", "", "nvarchar(50)", 0, 0, true)]
        public String Extension
        {
            get { return _Extension; }
            set { if (OnPropertyChanging("Extension", value)) { _Extension = value; OnPropertyChanged("Extension"); } }
        }

        private String _Category;
        /// <summary>分类</summary>>
        [Description("分类")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "Category", "分类", "", "nvarchar(50)", 0, 0, true)]
        public String Category
        {
            get { return _Category; }
            set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } }
        }

        private String _FilePath;
        /// <summary>文件路径</summary>>
        [Description("文件路径")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(6, "FilePath", "文件路径", "", "nvarchar(200)", 0, 0, true)]
        public String FilePath
        {
            get { return _FilePath; }
            set { if (OnPropertyChanging("FilePath", value)) { _FilePath = value; OnPropertyChanged("FilePath"); } }
        }

        private String _UserName;
        /// <summary>上传者</summary>>
        [Description("上传者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "UserName", "上传者", "", "nvarchar(50)", 0, 0, true)]
        public String UserName
        {
            get { return _UserName; }
            set { if (OnPropertyChanging("UserName", value)) { _UserName = value; OnPropertyChanged("UserName"); } }
        }

        private DateTime _UploadTime;
        /// <summary>上传时间</summary>>
        [Description("上传时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(8, "UploadTime", "上传时间", "", "datetime", 3, 0, false)]
        public DateTime UploadTime
        {
            get { return _UploadTime; }
            set { if (OnPropertyChanging("UploadTime", value)) { _UploadTime = value; OnPropertyChanged("UploadTime"); } }
        }

        private String _ContentType;
        /// <summary>内容类型</summary>>
        [Description("内容类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(9, "ContentType", "内容类型", "", "nvarchar(50)", 0, 0, true)]
        public String ContentType
        {
            get { return _ContentType; }
            set { if (OnPropertyChanging("ContentType", value)) { _ContentType = value; OnPropertyChanged("ContentType"); } }
        }

        private Int32 _StatID;
        /// <summary>访问统计</summary>>
        [Description("访问统计")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(10, "StatID", "访问统计", "", "int", 10, 0, false)]
        public Int32 StatID
        {
            get { return _StatID; }
            set { if (OnPropertyChanging("StatID", value)) { _StatID = value; OnPropertyChanged("StatID"); } }
        }

        private Boolean _IsEnable;
        /// <summary>是否启用</summary>>
        [Description("是否启用")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn(11, "IsEnable", "是否启用", "", "bit", 0, 0, false)]
        public Boolean IsEnable
        {
            get { return _IsEnable; }
            set { if (OnPropertyChanging("IsEnable", value)) { _IsEnable = value; OnPropertyChanged("IsEnable"); } }
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
                    case "ID": return _ID;
                    case "FileName": return _FileName;
                    case "Size": return _Size;
                    case "Extension": return _Extension;
                    case "Category": return _Category;
                    case "FilePath": return _FilePath;
                    case "UserName": return _UserName;
                    case "UploadTime": return _UploadTime;
                    case "ContentType": return _ContentType;
                    case "StatID": return _StatID;
                    case "IsEnable": return _IsEnable;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = Convert.ToInt32(value); break;
                    case "FileName": _FileName = Convert.ToString(value); break;
                    case "Size": _Size = Convert.ToInt32(value); break;
                    case "Extension": _Extension = Convert.ToString(value); break;
                    case "Category": _Category = Convert.ToString(value); break;
                    case "FilePath": _FilePath = Convert.ToString(value); break;
                    case "UserName": _UserName = Convert.ToString(value); break;
                    case "UploadTime": _UploadTime = Convert.ToDateTime(value); break;
                    case "ContentType": _ContentType = Convert.ToString(value); break;
                    case "StatID": _StatID = Convert.ToInt32(value); break;
                    case "IsEnable": _IsEnable = Convert.ToBoolean(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得附件字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>文件名</summary>
            public static readonly Field FileName = Meta.Table.FindByName("FileName");

            ///<summary>大小</summary>
            public static readonly Field Size = Meta.Table.FindByName("Size");

            ///<summary>扩展名</summary>
            public static readonly Field Extension = Meta.Table.FindByName("Extension");

            ///<summary>分类</summary>
            public static readonly Field Category = Meta.Table.FindByName("Category");

            ///<summary>文件路径</summary>
            public static readonly Field FilePath = Meta.Table.FindByName("FilePath");

            ///<summary>上传者</summary>
            public static readonly Field UserName = Meta.Table.FindByName("UserName");

            ///<summary>上传时间</summary>
            public static readonly Field UploadTime = Meta.Table.FindByName("UploadTime");

            ///<summary>内容类型</summary>
            public static readonly Field ContentType = Meta.Table.FindByName("ContentType");

            ///<summary>访问统计</summary>
            public static readonly Field StatID = Meta.Table.FindByName("StatID");

            ///<summary>是否启用</summary>
            public static readonly Field IsEnable = Meta.Table.FindByName("IsEnable");
        }
        #endregion
    }

    /// <summary>附件接口</summary>>
    public partial interface IAttachment
    {
        #region 属性
        /// <summary>编号</summary>>
        Int32 ID { get; set; }

        /// <summary>文件名</summary>>
        String FileName { get; set; }

        /// <summary>大小</summary>>
        Int32 Size { get; set; }

        /// <summary>扩展名</summary>>
        String Extension { get; set; }

        /// <summary>分类</summary>>
        String Category { get; set; }

        /// <summary>文件路径</summary>>
        String FilePath { get; set; }

        /// <summary>上传者</summary>>
        String UserName { get; set; }

        /// <summary>上传时间</summary>>
        DateTime UploadTime { get; set; }

        /// <summary>内容类型</summary>>
        String ContentType { get; set; }

        /// <summary>访问统计</summary>>
        Int32 StatID { get; set; }

        /// <summary>是否启用</summary>>
        Boolean IsEnable { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，基类使用反射实现。
        /// 派生实体类可重写该索引，以避免反射带来的性能损耗
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}