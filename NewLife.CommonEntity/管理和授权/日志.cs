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
	/// <summary>
	/// 日志
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("日志")]
	[BindTable("Log", Description = "日志", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Log<TEntity> : ILog
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

		private String _Category;
		/// <summary>
		/// 类别
		/// </summary>
		[Description("类别")]
		[DataObjectField(false, false, false, 50)]
		[BindColumn(2, "Category", "类别", "", "nvarchar(50)", 0, 0, true)]
		public String Category
		{
			get { return _Category; }
			set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } }
		}

		private String _Action;
		/// <summary>
		/// 操作
		/// </summary>
		[Description("操作")]
		[DataObjectField(false, false, false, 50)]
		[BindColumn(3, "Action", "操作", "", "nvarchar(50)", 0, 0, true)]
		public String Action
		{
			get { return _Action; }
			set { if (OnPropertyChanging("Action", value)) { _Action = value; OnPropertyChanged("Action"); } }
		}

		private Int32 _UserID;
		/// <summary>
		/// 用户编号
		/// </summary>
		[Description("用户编号")]
		[DataObjectField(false, false, false, 10)]
		[BindColumn(4, "UserID", "用户编号", "0", "int", 10, 0, false)]
		public Int32 UserID
		{
			get { return _UserID; }
			set { if (OnPropertyChanging("UserID", value)) { _UserID = value; OnPropertyChanged("UserID"); } }
		}

		private String _UserName;
		/// <summary>
		/// 用户名
		/// </summary>
		[Description("用户名")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn(5, "UserName", "用户名", "", "nvarchar(50)", 0, 0, true)]
		public String UserName
		{
			get { return _UserName; }
			set { if (OnPropertyChanging("UserName", value)) { _UserName = value; OnPropertyChanged("UserName"); } }
		}

		private String _IP;
		/// <summary>
		/// IP地址
		/// </summary>
		[Description("IP地址")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn(6, "IP", "IP地址", "", "nvarchar(50)", 0, 0, true)]
		public String IP
		{
			get { return _IP; }
			set { if (OnPropertyChanging("IP", value)) { _IP = value; OnPropertyChanged("IP"); } }
		}

		private DateTime _OccurTime;
		/// <summary>
		/// 时间
		/// </summary>
		[Description("时间")]
		[DataObjectField(false, false, false, 3)]
		[BindColumn(7, "OccurTime", "时间", "getdate()", "datetime", 3, 0, false)]
		public DateTime OccurTime
		{
			get { return _OccurTime; }
			set { if (OnPropertyChanging("OccurTime", value)) { _OccurTime = value; OnPropertyChanged("OccurTime"); } }
		}

		private String _Remark;
		/// <summary>
		/// 详细信息
		/// </summary>
		[Description("详细信息")]
		[DataObjectField(false, false, true, 500)]
		[BindColumn(8, "Remark", "详细信息", "", "nvarchar(500)", 0, 0, true)]
		public String Remark
		{
			get { return _Remark; }
			set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } }
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
					case "Category" : return _Category;
					case "Action" : return _Action;
					case "UserID" : return _UserID;
					case "UserName" : return _UserName;
					case "IP" : return _IP;
					case "OccurTime" : return _OccurTime;
					case "Remark" : return _Remark;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID" : _ID = Convert.ToInt32(value); break;
					case "Category" : _Category = Convert.ToString(value); break;
					case "Action" : _Action = Convert.ToString(value); break;
					case "UserID" : _UserID = Convert.ToInt32(value); break;
					case "UserName" : _UserName = Convert.ToString(value); break;
					case "IP" : _IP = Convert.ToString(value); break;
					case "OccurTime" : _OccurTime = Convert.ToDateTime(value); break;
					case "Remark" : _Remark = Convert.ToString(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
		/// <summary>
		/// 取得日志字段名的快捷方式
		/// </summary>
		public class _
		{
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>类别</summary>
            public static readonly Field Category = Meta.Table.FindByName("Category");

            ///<summary>操作</summary>
            public static readonly Field Action = Meta.Table.FindByName("Action");

            ///<summary>用户编号</summary>
            public static readonly Field UserID = Meta.Table.FindByName("UserID");

            ///<summary>用户名</summary>
            public static readonly Field UserName = Meta.Table.FindByName("UserName");

            ///<summary>IP地址</summary>
            public static readonly Field IP = Meta.Table.FindByName("IP");

            ///<summary>时间</summary>
            public static readonly Field OccurTime = Meta.Table.FindByName("OccurTime");

            ///<summary>详细信息</summary>
            public static readonly Field Remark = Meta.Table.FindByName("Remark");
		}
		#endregion
	}

	/// <summary>
	/// 日志接口
	/// </summary>
	public partial interface ILog
	{
		#region 属性
		/// <summary>
		/// 编号
		/// </summary>
		Int32 ID { get; set; }

		/// <summary>
		/// 类别
		/// </summary>
		String Category { get; set; }

		/// <summary>
		/// 操作
		/// </summary>
		String Action { get; set; }

		/// <summary>
		/// 用户编号
		/// </summary>
		Int32 UserID { get; set; }

		/// <summary>
		/// 用户名
		/// </summary>
		String UserName { get; set; }

		/// <summary>
		/// IP地址
		/// </summary>
		String IP { get; set; }

		/// <summary>
		/// 时间
		/// </summary>
		DateTime OccurTime { get; set; }

		/// <summary>
		/// 详细信息
		/// </summary>
		String Remark { get; set; }
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