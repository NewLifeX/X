/*
 * XCoder v3.2.2010.1014
 * 作者：nnhy/NEWLIFE
 * 时间：2010-12-08 16:22:29
 * 版权：版权所有 (C) 新生命开发团队 2010
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
	/// <summary>
	/// 统计
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("统计")]
	[BindTable("Statistics", Description = "统计", ConnName = "Common")]
    public partial class Statistics<TEntity>
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

		private Int32 _Total;
		/// <summary>
		/// 总数
		/// </summary>
		[Description("总数")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("Total", Description = "总数", DefaultValue = "", Order = 2)]
		public Int32 Total
		{
			get { return _Total; }
			set { if (OnPropertyChange("Total", value)) _Total = value; }
		}

		private Int32 _Today;
		/// <summary>
		/// 今天
		/// </summary>
		[Description("今天")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("Today", Description = "今天", DefaultValue = "", Order = 3)]
		public Int32 Today
		{
			get { return _Today; }
			set { if (OnPropertyChange("Today", value)) _Today = value; }
		}

		private Int32 _Yesterday;
		/// <summary>
		/// 昨天
		/// </summary>
		[Description("昨天")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("Yesterday", Description = "昨天", DefaultValue = "", Order = 4)]
		public Int32 Yesterday
		{
			get { return _Yesterday; }
			set { if (OnPropertyChange("Yesterday", value)) _Yesterday = value; }
		}

		private Int32 _ThisWeek;
		/// <summary>
		/// 本周
		/// </summary>
		[Description("本周")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("ThisWeek", Description = "本周", DefaultValue = "", Order = 5)]
		public Int32 ThisWeek
		{
			get { return _ThisWeek; }
			set { if (OnPropertyChange("ThisWeek", value)) _ThisWeek = value; }
		}

		private Int32 _LastWeek;
		/// <summary>
		/// 上周
		/// </summary>
		[Description("上周")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("LastWeek", Description = "上周", DefaultValue = "", Order = 6)]
		public Int32 LastWeek
		{
			get { return _LastWeek; }
			set { if (OnPropertyChange("LastWeek", value)) _LastWeek = value; }
		}

		private Int32 _ThisMonth;
		/// <summary>
		/// 本月
		/// </summary>
		[Description("本月")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("ThisMonth", Description = "本月", DefaultValue = "", Order = 7)]
		public Int32 ThisMonth
		{
			get { return _ThisMonth; }
			set { if (OnPropertyChange("ThisMonth", value)) _ThisMonth = value; }
		}

		private Int32 _LastMonth;
		/// <summary>
        /// 上月
		/// </summary>
		[Description("上月")]
		[DataObjectField(false, false, true, 10)]
        [BindColumn("LastMonth", Description = "上月", DefaultValue = "", Order = 8)]
		public Int32 LastMonth
		{
			get { return _LastMonth; }
			set { if (OnPropertyChange("LastMonth", value)) _LastMonth = value; }
		}

		private Int32 _ThisYear;
		/// <summary>
		/// 本年
		/// </summary>
		[Description("本年")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("ThisYear", Description = "本年", DefaultValue = "", Order = 9)]
		public Int32 ThisYear
		{
			get { return _ThisYear; }
			set { if (OnPropertyChange("ThisYear", value)) _ThisYear = value; }
		}

		private Int32 _LastYear;
		/// <summary>
        /// 去年
		/// </summary>
		[Description("去年")]
		[DataObjectField(false, false, true, 10)]
        [BindColumn("LastYear", Description = "去年", DefaultValue = "", Order = 10)]
		public Int32 LastYear
		{
			get { return _LastYear; }
			set { if (OnPropertyChange("LastYear", value)) _LastYear = value; }
		}

		private DateTime _LastTime;
		/// <summary>
		/// 最后时间
		/// </summary>
		[Description("最后时间")]
		[DataObjectField(false, false, true, 23)]
		[BindColumn("LastTime", Description = "最后时间", DefaultValue = "", Order = 11)]
		public DateTime LastTime
		{
			get { return _LastTime; }
			set { if (OnPropertyChange("LastTime", value)) _LastTime = value; }
		}

		private String _LastIP;
		/// <summary>
		/// 最后IP
		/// </summary>
		[Description("最后IP")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn("LastIP", Description = "最后IP", DefaultValue = "", Order = 12)]
		public String LastIP
		{
			get { return _LastIP; }
			set { if (OnPropertyChange("LastIP", value)) _LastIP = value; }
		}

		private String _Remark;
		/// <summary>
		/// 备注
		/// </summary>
		[Description("备注")]
		[DataObjectField(false, false, true, 500)]
		[BindColumn("Remark", Description = "备注", DefaultValue = "", Order = 13)]
		public String Remark
		{
			get { return _Remark; }
			set { if (OnPropertyChange("Remark", value)) _Remark = value; }
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
					case "Total" : return Total;
					case "Today" : return Today;
					case "Yesterday" : return Yesterday;
					case "ThisWeek" : return ThisWeek;
					case "LastWeek" : return LastWeek;
					case "ThisMonth" : return ThisMonth;
					case "LastMonth" : return LastMonth;
					case "ThisYear" : return ThisYear;
					case "LastYear" : return LastYear;
					case "LastTime" : return LastTime;
					case "LastIP" : return LastIP;
					case "Remark" : return Remark;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID" : _ID = Convert.ToInt32(value); break;
					case "Total" : _Total = Convert.ToInt32(value); break;
					case "Today" : _Today = Convert.ToInt32(value); break;
					case "Yesterday" : _Yesterday = Convert.ToInt32(value); break;
					case "ThisWeek" : _ThisWeek = Convert.ToInt32(value); break;
					case "LastWeek" : _LastWeek = Convert.ToInt32(value); break;
					case "ThisMonth" : _ThisMonth = Convert.ToInt32(value); break;
					case "LastMonth" : _LastMonth = Convert.ToInt32(value); break;
					case "ThisYear" : _ThisYear = Convert.ToInt32(value); break;
					case "LastYear" : _LastYear = Convert.ToInt32(value); break;
					case "LastTime" : _LastTime = Convert.ToDateTime(value); break;
					case "LastIP" : _LastIP = Convert.ToString(value); break;
					case "Remark" : _Remark = Convert.ToString(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
		/// <summary>
		/// 取得统计字段名的快捷方式
		/// </summary>
        [CLSCompliant(false)]
        public class _
		{
			///<summary>
			/// 编号
			///</summary>
			public const String ID = "ID";

			///<summary>
			/// 总数
			///</summary>
			public const String Total = "Total";

			///<summary>
			/// 今天
			///</summary>
			public const String Today = "Today";

			///<summary>
			/// 昨天
			///</summary>
			public const String Yesterday = "Yesterday";

			///<summary>
			/// 本周
			///</summary>
			public const String ThisWeek = "ThisWeek";

			///<summary>
			/// 上周
			///</summary>
			public const String LastWeek = "LastWeek";

			///<summary>
			/// 本月
			///</summary>
			public const String ThisMonth = "ThisMonth";

			///<summary>
            /// 上月
			///</summary>
			public const String LastMonth = "LastMonth";

			///<summary>
			/// 本年
			///</summary>
			public const String ThisYear = "ThisYear";

			///<summary>
            /// 去年
			///</summary>
			public const String LastYear = "LastYear";

			///<summary>
			/// 最后时间
			///</summary>
			public const String LastTime = "LastTime";

			///<summary>
			/// 最后IP
			///</summary>
			public const String LastIP = "LastIP";

			///<summary>
			/// 备注
			///</summary>
			public const String Remark = "Remark";
		}
		#endregion
	}
}