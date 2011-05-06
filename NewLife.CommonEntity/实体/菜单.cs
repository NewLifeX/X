/*
 * XCoder v3.4.2011.0329
 * 作者：nnhy/NEWLIFE
 * 时间：2011-05-06 10:35:57
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
	/// <summary>
	/// 菜单
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("菜单")]
	[BindTable("Menu", Description = "菜单", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Menu<TEntity> : IMenu
	{
		#region 属性
		private Int32 _ID;
		/// <summary>
		/// 编号
		/// </summary>
		[Description("编号")]
		[DataObjectField(true, true, false, 10)]
		[BindColumn(1, "ID", "编号", "", "int", 10, 0, false)]
		public Int32 ID
		{
			get { return _ID; }
			set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
		}

		private String _Name;
		/// <summary>
		/// 名称
		/// </summary>
		[Description("名称")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn(2, "Name", "名称", "", "nvarchar(50)", 0, 0, true)]
		public String Name
		{
			get { return _Name; }
			set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
		}

		private Int32 _ParentID;
		/// <summary>
		/// 父编号
		/// </summary>
		[Description("父编号")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(3, "ParentID", "父编号", "", "int", 10, 0, false)]
		public Int32 ParentID
		{
			get { return _ParentID; }
			set { if (OnPropertyChanging("ParentID", value)) { _ParentID = value; OnPropertyChanged("ParentID"); } }
		}

		private String _Url;
		/// <summary>
		/// 链接
		/// </summary>
		[Description("链接")]
		[DataObjectField(false, false, true, 200)]
		[BindColumn(4, "Url", "链接", "", "nvarchar(200)", 0, 0, true)]
		public String Url
		{
			get { return _Url; }
			set { if (OnPropertyChanging("Url", value)) { _Url = value; OnPropertyChanged("Url"); } }
		}

		private Int32 _Sort;
		/// <summary>
		/// 序号
		/// </summary>
		[Description("序号")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(5, "Sort", "序号", "", "int", 10, 0, false)]
		public Int32 Sort
		{
			get { return _Sort; }
			set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } }
		}

		private String _Remark;
		/// <summary>
		/// 备注
		/// </summary>
		[Description("备注")]
		[DataObjectField(false, false, true, 500)]
		[BindColumn(6, "Remark", "备注", "", "nvarchar(500)", 0, 0, true)]
		public String Remark
		{
			get { return _Remark; }
			set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } }
		}

		private String _Permission;
		/// <summary>
		/// 权限
		/// </summary>
		[Description("权限")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn(7, "Permission", "权限", "", "nvarchar(50)", 0, 0, true)]
		public String Permission
		{
			get { return _Permission; }
			set { if (OnPropertyChanging("Permission", value)) { _Permission = value; OnPropertyChanged("Permission"); } }
		}

		private Boolean _IsShow;
		/// <summary>
		/// 是否显示
		/// </summary>
		[Description("是否显示")]
		[DataObjectField(false, false, true, 1)]
		[BindColumn(8, "IsShow", "是否显示", "", "bit", 0, 0, false)]
		public Boolean IsShow
		{
			get { return _IsShow; }
			set { if (OnPropertyChanging("IsShow", value)) { _IsShow = value; OnPropertyChanged("IsShow"); } }
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
					case "ParentID" : return _ParentID;
					case "Url" : return _Url;
					case "Sort" : return _Sort;
					case "Remark" : return _Remark;
					case "Permission" : return _Permission;
					case "IsShow" : return _IsShow;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID" : _ID = Convert.ToInt32(value); break;
					case "Name" : _Name = Convert.ToString(value); break;
					case "ParentID" : _ParentID = Convert.ToInt32(value); break;
					case "Url" : _Url = Convert.ToString(value); break;
					case "Sort" : _Sort = Convert.ToInt32(value); break;
					case "Remark" : _Remark = Convert.ToString(value); break;
					case "Permission" : _Permission = Convert.ToString(value); break;
					case "IsShow" : _IsShow = Convert.ToBoolean(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
		/// <summary>
		/// 取得菜单字段名的快捷方式
		/// </summary>
        [CLSCompliant(false)]
		public class _
		{
			///<summary>
			/// 编号
			///</summary>
			public const String ID = "ID";

			///<summary>
			/// 名称
			///</summary>
			public const String Name = "Name";

			///<summary>
			/// 父编号
			///</summary>
			public const String ParentID = "ParentID";

			///<summary>
			/// 链接
			///</summary>
			public const String Url = "Url";

			///<summary>
			/// 序号
			///</summary>
			public const String Sort = "Sort";

			///<summary>
			/// 备注
			///</summary>
			public const String Remark = "Remark";

			///<summary>
			/// 权限
			///</summary>
			public const String Permission = "Permission";

			///<summary>
			/// 是否显示
			///</summary>
			public const String IsShow = "IsShow";
		}
		#endregion
	}

	/// <summary>
	/// 菜单接口
	/// </summary>
	public partial interface IMenu
	{
		#region 属性
		/// <summary>
		/// 编号
		/// </summary>
		Int32 ID { get; set; }

		/// <summary>
		/// 名称
		/// </summary>
		String Name { get; set; }

		/// <summary>
		/// 父编号
		/// </summary>
		Int32 ParentID { get; set; }

		/// <summary>
		/// 链接
		/// </summary>
		String Url { get; set; }

		/// <summary>
		/// 序号
		/// </summary>
		Int32 Sort { get; set; }

		/// <summary>
		/// 备注
		/// </summary>
		String Remark { get; set; }

		/// <summary>
		/// 权限
		/// </summary>
		String Permission { get; set; }

		/// <summary>
		/// 是否显示
		/// </summary>
		Boolean IsShow { get; set; }
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