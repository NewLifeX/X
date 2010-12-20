using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
	/// <summary>
	/// 日志
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("日志")]
	[BindTable("Log", Description = "日志", ConnName = "Common")]
    public partial class Log<TEntity>
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
		
		private String _Category;
		/// <summary>
		/// 类别
		/// </summary>
		[Description("类别")]
		[DataObjectField(false, false, false, 50)]
		[BindColumn("Category", Description = "类别", DefaultValue = "", Order = 2)]
		public String Category
		{
			get { return _Category; }
			set { if (OnPropertyChange("Category", value)) _Category = value; }
		}
		
		private String _Action;
		/// <summary>
		/// 操作
		/// </summary>
		[Description("操作")]
		[DataObjectField(false, false, false, 50)]
		[BindColumn("Action", Description = "操作", DefaultValue = "", Order = 3)]
		public String Action
		{
			get { return _Action; }
			set { if (OnPropertyChange("Action", value)) _Action = value; }
		}
		
		private Int32 _UserID;
		/// <summary>
		/// 用户编号
		/// </summary>
		[Description("用户编号")]
		[DataObjectField(false, false, false, 10)]
		[BindColumn("UserID", Description = "用户编号", DefaultValue = "0", Order = 4)]
		public Int32 UserID
		{
			get { return _UserID; }
			set { if (OnPropertyChange("UserID", value)) _UserID = value; }
		}
		
		private String _UserName;
		/// <summary>
		/// 用户名
		/// </summary>
		[Description("用户名")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn("UserName", Description = "用户名", DefaultValue = "", Order = 5)]
		public String UserName
		{
			get { return _UserName; }
			set { if (OnPropertyChange("UserName", value)) _UserName = value; }
		}
		
		private String _IP;
		/// <summary>
		/// IP地址
		/// </summary>
		[Description("IP地址")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn("IP", Description = "IP地址", DefaultValue = "", Order = 6)]
		public String IP
		{
			get { return _IP; }
			set { if (OnPropertyChange("IP", value)) _IP = value; }
		}
		
		private DateTime _OccurTime;
		/// <summary>
		/// 时间
		/// </summary>
		[Description("时间")]
		[DataObjectField(false, false, false, 23)]
		[BindColumn("OccurTime", Description = "时间", DefaultValue = "getdate()", Order = 7)]
		public DateTime OccurTime
		{
			get { return _OccurTime; }
			set { if (OnPropertyChange("OccurTime", value)) _OccurTime = value; }
		}
		
		private String _Remark;
		/// <summary>
		/// 详细信息
		/// </summary>
		[Description("详细信息")]
		[DataObjectField(false, false, true, 500)]
		[BindColumn("Remark", Description = "详细信息", DefaultValue = "", Order = 8)]
		public String Remark
		{
			get { return _Remark; }
			set { if (OnPropertyChange("Remark", value)) _Remark = value; }
		}
		#endregion

		#region 构造函数
		/// <summary>
		/// 初始化一个日志实例。
		/// </summary>
		public Log()
		{
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
					case "ID": return ID;
					case "Category": return Category;
					case "Action": return Action;
					case "UserID": return UserID;
					case "UserName": return UserName;
					case "IP": return IP;
					case "OccurTime": return OccurTime;
					case "Remark": return Remark;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID": _ID = Convert.ToInt32(value); break;
					case "Category": _Category = Convert.ToString(value); break;
					case "Action": _Action = Convert.ToString(value); break;
					case "UserID": _UserID = Convert.ToInt32(value); break;
					case "UserName": _UserName = Convert.ToString(value); break;
					case "IP": _IP = Convert.ToString(value); break;
					case "OccurTime": _OccurTime = Convert.ToDateTime(value); break;
					case "Remark": _Remark = Convert.ToString(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
		/// <summary>
		/// 取得字段名的快捷方式
		/// </summary>
		public class _
		{
			/// <summary>
			/// 编号
			/// </summary>
			public const String ID = "ID";
			
			/// <summary>
			/// 类别
			/// </summary>
			public const String Category = "Category";
			
			/// <summary>
			/// 操作
			/// </summary>
			public const String Action = "Action";
			
			/// <summary>
			/// 用户编号
			/// </summary>
			public const String UserID = "UserID";
			
			/// <summary>
			/// 用户名
			/// </summary>
			public const String UserName = "UserName";
			
			/// <summary>
			/// IP地址
			/// </summary>
			public const String IP = "IP";
			
			/// <summary>
			/// 时间
			/// </summary>
			public const String OccurTime = "OccurTime";
			
			/// <summary>
			/// 详细信息
			/// </summary>
			public const String Remark = "Remark";
		}
		#endregion
	}
}