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
using XCode.Configuration;

namespace NewLife.CommonEntity
{
    /// <summary>统计</summary>
    [Serializable]
	[DataObject]
	[Description("统计")]
	[BindTable("Statistics", Description = "统计", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Statistics<TEntity> : IStatistics
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

		private Int32 _Total;
		/// <summary>
		/// 总数
		/// </summary>
		[Description("总数")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(2, "Total", "总数", "", "int", 10, 0, false)]
		public Int32 Total
		{
			get { return _Total; }
			set { if (OnPropertyChanging("Total", value)) { _Total = value; OnPropertyChanged("Total"); } }
		}

		private Int32 _Today;
		/// <summary>
		/// 今天
		/// </summary>
		[Description("今天")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(3, "Today", "今天", "", "int", 10, 0, false)]
		public Int32 Today
		{
			get { return _Today; }
			set { if (OnPropertyChanging("Today", value)) { _Today = value; OnPropertyChanged("Today"); } }
		}

		private Int32 _Yesterday;
		/// <summary>
		/// 昨天
		/// </summary>
		[Description("昨天")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(4, "Yesterday", "昨天", "", "int", 10, 0, false)]
		public Int32 Yesterday
		{
			get { return _Yesterday; }
			set { if (OnPropertyChanging("Yesterday", value)) { _Yesterday = value; OnPropertyChanged("Yesterday"); } }
		}

		private Int32 _ThisWeek;
		/// <summary>
		/// 本周
		/// </summary>
		[Description("本周")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(5, "ThisWeek", "本周", "", "int", 10, 0, false)]
		public Int32 ThisWeek
		{
			get { return _ThisWeek; }
			set { if (OnPropertyChanging("ThisWeek", value)) { _ThisWeek = value; OnPropertyChanged("ThisWeek"); } }
		}

		private Int32 _LastWeek;
		/// <summary>
		/// 上周
		/// </summary>
		[Description("上周")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(6, "LastWeek", "上周", "", "int", 10, 0, false)]
		public Int32 LastWeek
		{
			get { return _LastWeek; }
			set { if (OnPropertyChanging("LastWeek", value)) { _LastWeek = value; OnPropertyChanged("LastWeek"); } }
		}

		private Int32 _ThisMonth;
		/// <summary>
		/// 本月
		/// </summary>
		[Description("本月")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(7, "ThisMonth", "本月", "", "int", 10, 0, false)]
		public Int32 ThisMonth
		{
			get { return _ThisMonth; }
			set { if (OnPropertyChanging("ThisMonth", value)) { _ThisMonth = value; OnPropertyChanged("ThisMonth"); } }
		}

		private Int32 _LastMonth;
		/// <summary>
		/// 上月
		/// </summary>
		[Description("上月")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(8, "LastMonth", "上月", "", "int", 10, 0, false)]
		public Int32 LastMonth
		{
			get { return _LastMonth; }
			set { if (OnPropertyChanging("LastMonth", value)) { _LastMonth = value; OnPropertyChanged("LastMonth"); } }
		}

		private Int32 _ThisYear;
		/// <summary>
		/// 本年
		/// </summary>
		[Description("本年")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(9, "ThisYear", "本年", "", "int", 10, 0, false)]
		public Int32 ThisYear
		{
			get { return _ThisYear; }
			set { if (OnPropertyChanging("ThisYear", value)) { _ThisYear = value; OnPropertyChanged("ThisYear"); } }
		}

		private Int32 _LastYear;
		/// <summary>
		/// 去年
		/// </summary>
		[Description("去年")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(10, "LastYear", "去年", "", "int", 10, 0, false)]
		public Int32 LastYear
		{
			get { return _LastYear; }
			set { if (OnPropertyChanging("LastYear", value)) { _LastYear = value; OnPropertyChanged("LastYear"); } }
		}

		private DateTime _LastTime;
		/// <summary>
		/// 最后时间
		/// </summary>
		[Description("最后时间")]
		[DataObjectField(false, false, true, 3)]
		[BindColumn(11, "LastTime", "最后时间", "", "datetime", 3, 0, false)]
		public DateTime LastTime
		{
			get { return _LastTime; }
			set { if (OnPropertyChanging("LastTime", value)) { _LastTime = value; OnPropertyChanged("LastTime"); } }
		}

		private String _LastIP;
		/// <summary>
		/// 最后IP
		/// </summary>
		[Description("最后IP")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn(12, "LastIP", "最后IP", "", "nvarchar(50)", 0, 0, true)]
		public String LastIP
		{
			get { return _LastIP; }
			set { if (OnPropertyChanging("LastIP", value)) { _LastIP = value; OnPropertyChanged("LastIP"); } }
		}

		private String _Remark;
		/// <summary>
		/// 备注
		/// </summary>
		[Description("备注")]
		[DataObjectField(false, false, true, 500)]
		[BindColumn(13, "Remark", "备注", "", "nvarchar(500)", 0, 0, true)]
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
					case "Total" : return _Total;
					case "Today" : return _Today;
					case "Yesterday" : return _Yesterday;
					case "ThisWeek" : return _ThisWeek;
					case "LastWeek" : return _LastWeek;
					case "ThisMonth" : return _ThisMonth;
					case "LastMonth" : return _LastMonth;
					case "ThisYear" : return _ThisYear;
					case "LastYear" : return _LastYear;
					case "LastTime" : return _LastTime;
					case "LastIP" : return _LastIP;
					case "Remark" : return _Remark;
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
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>总数</summary>
            public static readonly Field Total = Meta.Table.FindByName("Total");

            ///<summary>今天</summary>
            public static readonly Field Today = Meta.Table.FindByName("Today");

            ///<summary>昨天</summary>
            public static readonly Field Yesterday = Meta.Table.FindByName("Yesterday");

            ///<summary>本周</summary>
            public static readonly Field ThisWeek = Meta.Table.FindByName("ThisWeek");

            ///<summary>上周</summary>
            public static readonly Field LastWeek = Meta.Table.FindByName("LastWeek");

            ///<summary>本月</summary>
            public static readonly Field ThisMonth = Meta.Table.FindByName("ThisMonth");

            ///<summary>上月</summary>
            public static readonly Field LastMonth = Meta.Table.FindByName("LastMonth");

            ///<summary>本年</summary>
            public static readonly Field ThisYear = Meta.Table.FindByName("ThisYear");

            ///<summary>去年</summary>
            public static readonly Field LastYear = Meta.Table.FindByName("LastYear");

            ///<summary>最后时间</summary>
            public static readonly Field LastTime = Meta.Table.FindByName("LastTime");

            ///<summary>最后IP</summary>
            public static readonly Field LastIP = Meta.Table.FindByName("LastIP");

            ///<summary>备注</summary>
            public static readonly Field Remark = Meta.Table.FindByName("Remark");
		}
		#endregion
	}

	/// <summary>
	/// 统计接口
	/// </summary>
	public partial interface IStatistics
	{
		#region 属性
		/// <summary>
		/// 编号
		/// </summary>
		Int32 ID { get; set; }

		/// <summary>
		/// 总数
		/// </summary>
		Int32 Total { get; set; }

		/// <summary>
		/// 今天
		/// </summary>
		Int32 Today { get; set; }

		/// <summary>
		/// 昨天
		/// </summary>
		Int32 Yesterday { get; set; }

		/// <summary>
		/// 本周
		/// </summary>
		Int32 ThisWeek { get; set; }

		/// <summary>
		/// 上周
		/// </summary>
		Int32 LastWeek { get; set; }

		/// <summary>
		/// 本月
		/// </summary>
		Int32 ThisMonth { get; set; }

		/// <summary>
		/// 上月
		/// </summary>
		Int32 LastMonth { get; set; }

		/// <summary>
		/// 本年
		/// </summary>
		Int32 ThisYear { get; set; }

		/// <summary>
		/// 去年
		/// </summary>
		Int32 LastYear { get; set; }

		/// <summary>
		/// 最后时间
		/// </summary>
		DateTime LastTime { get; set; }

		/// <summary>
		/// 最后IP
		/// </summary>
		String LastIP { get; set; }

		/// <summary>
		/// 备注
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