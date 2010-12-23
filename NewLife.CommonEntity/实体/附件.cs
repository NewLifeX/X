/*
 * XCoder v3.2.2010.1014
 * 作者：nnhy/NEWLIFE
 * 时间：2010-12-08 16:22:52
 * 版权：版权所有 (C) 新生命开发团队 2010
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
	/// <summary>
	/// 附件
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("附件")]
	[BindTable("Attachment", Description = "附件", ConnName = "Common")]
    public partial class Attachment<TEntity>
	{
		#region 属性
		private Int32 _ID;
		/// <summary>
		/// 编号
		/// </summary>
		[Description("编号")]
		[DataObjectField(true, true, false, 10)]
		[BindColumn("ID", Description = "编号", DefaultValue = "", Order = 1)]
		public Int32 ID
		{
			get { return _ID; }
			set { if (OnPropertyChange("ID", value)) _ID = value; }
		}

		private String _FileName;
		/// <summary>
		/// 文件名
		/// </summary>
		[Description("文件名")]
		[DataObjectField(false, false, true, 200)]
		[BindColumn("FileName", Description = "文件名", DefaultValue = "", Order = 2)]
		public String FileName
		{
			get { return _FileName; }
			set { if (OnPropertyChange("FileName", value)) _FileName = value; }
		}

		private Int32 _Size;
		/// <summary>
		/// 大小
		/// </summary>
		[Description("大小")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("Size", Description = "大小", DefaultValue = "", Order = 3)]
		public Int32 Size
		{
			get { return _Size; }
			set { if (OnPropertyChange("Size", value)) _Size = value; }
		}

		private String _Extension;
		/// <summary>
		/// 扩展名
		/// </summary>
		[Description("扩展名")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn("Extension", Description = "扩展名", DefaultValue = "", Order = 4)]
		public String Extension
		{
			get { return _Extension; }
			set { if (OnPropertyChange("Extension", value)) _Extension = value; }
		}

		private String _Category;
		/// <summary>
		/// 分类
		/// </summary>
		[Description("分类")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn("Category", Description = "分类", DefaultValue = "", Order = 5)]
		public String Category
		{
			get { return _Category; }
			set { if (OnPropertyChange("Category", value)) _Category = value; }
		}

		private String _FilePath;
		/// <summary>
		/// 文件路径
		/// </summary>
		[Description("文件路径")]
		[DataObjectField(false, false, true, 200)]
		[BindColumn("FilePath", Description = "文件路径", DefaultValue = "", Order = 6)]
		public String FilePath
		{
			get { return _FilePath; }
			set { if (OnPropertyChange("FilePath", value)) _FilePath = value; }
		}

		private String _UserName;
		/// <summary>
		/// 上传者
		/// </summary>
		[Description("上传者")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn("UserName", Description = "上传者", DefaultValue = "", Order = 7)]
		public String UserName
		{
			get { return _UserName; }
			set { if (OnPropertyChange("UserName", value)) _UserName = value; }
		}

		private DateTime _UploadTime;
		/// <summary>
		/// 上传时间
		/// </summary>
		[Description("上传时间")]
		[DataObjectField(false, false, true, 23)]
		[BindColumn("UploadTime", Description = "上传时间", DefaultValue = "", Order = 8)]
		public DateTime UploadTime
		{
			get { return _UploadTime; }
			set { if (OnPropertyChange("UploadTime", value)) _UploadTime = value; }
		}

		private String _ContentType;
		/// <summary>
		/// 内容类型
		/// </summary>
		[Description("内容类型")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn("ContentType", Description = "内容类型", DefaultValue = "", Order = 9)]
		public String ContentType
		{
			get { return _ContentType; }
			set { if (OnPropertyChange("ContentType", value)) _ContentType = value; }
		}

		private Int32 _StatID;
		/// <summary>
		/// 访问统计
		/// </summary>
		[Description("访问统计")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("StatID", Description = "访问统计", DefaultValue = "", Order = 10)]
		public Int32 StatID
		{
			get { return _StatID; }
			set { if (OnPropertyChange("StatID", value)) _StatID = value; }
		}
        private Boolean _IsEnable;
        /// <summary>
        /// 是否启用
        /// </summary>
        [Description("是否启用")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn("IsEnable", Description = "是否启用", DefaultValue = "", Order = 6)]
        public Boolean IsEnable
        {
            get { return _IsEnable; }
            set { if (OnPropertyChange("IsEnable", value)) _IsEnable = value; }
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
					case "ID" : return ID;
					case "FileName" : return FileName;
					case "Size" : return Size;
					case "Extension" : return Extension;
					case "Category" : return Category;
					case "FilePath" : return FilePath;
					case "UserName" : return UserName;
					case "UploadTime" : return UploadTime;
					case "ContentType" : return ContentType;
					case "StatID" : return StatID;
                    case "IsEnable": return IsEnable;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID" : _ID = Convert.ToInt32(value); break;
					case "FileName" : _FileName = Convert.ToString(value); break;
					case "Size" : _Size = Convert.ToInt32(value); break;
					case "Extension" : _Extension = Convert.ToString(value); break;
					case "Category" : _Category = Convert.ToString(value); break;
					case "FilePath" : _FilePath = Convert.ToString(value); break;
					case "UserName" : _UserName = Convert.ToString(value); break;
					case "UploadTime" : _UploadTime = Convert.ToDateTime(value); break;
					case "ContentType" : _ContentType = Convert.ToString(value); break;
					case "StatID" : _StatID = Convert.ToInt32(value); break;
                    case "IsEnable": _IsEnable = Convert.ToBoolean(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
		/// <summary>
		/// 取得附件字段名的快捷方式
		/// </summary>
		public class _
		{
			///<summary>
			/// 编号
			///</summary>
			public const String ID = "ID";

			///<summary>
			/// 文件名
			///</summary>
			public const String FileName = "FileName";

			///<summary>
			/// 大小
			///</summary>
			public const String Size = "Size";

			///<summary>
			/// 扩展名
			///</summary>
			public const String Extension = "Extension";

			///<summary>
			/// 分类
			///</summary>
			public const String Category = "Category";

			///<summary>
			/// 文件路径
			///</summary>
			public const String FilePath = "FilePath";

			///<summary>
			/// 上传者
			///</summary>
			public const String UserName = "UserName";

			///<summary>
			/// 上传时间
			///</summary>
			public const String UploadTime = "UploadTime";

			///<summary>
			/// 内容类型
			///</summary>
			public const String ContentType = "ContentType";

			///<summary>
			/// 访问统计
			///</summary>
			public const String StatID = "StatID";

            ///<summary>
            /// 是否过期
            ///</summary>
            public const String IsEnable = "IsEnable";
		}
		#endregion
	}
}