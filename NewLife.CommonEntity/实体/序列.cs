/*
 * XCoder v3.4.2011.0329
 * 作者：nnhy/X
 * 时间：2011-06-21 23:11:58
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
	/// 序列
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("序列")]
	[BindTable("Sequence", Description = "序列", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Sequence<TEntity>
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

		private Int32 _Kind;
		/// <summary>
		/// 种类
		/// </summary>
		[Description("种类")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(3, "Kind", "种类", "", "int", 10, 0, false)]
		public Int32 Kind
		{
			get { return _Kind; }
			set { if (OnPropertyChanging("Kind", value)) { _Kind = value; OnPropertyChanged("Kind"); } }
		}

		private Int32 _Num;
		/// <summary>
		/// 数字
		/// </summary>
		[Description("数字")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(4, "Num", "数字", "", "int", 10, 0, false)]
		public Int32 Num
		{
			get { return _Num; }
			set { if (OnPropertyChanging("Num", value)) { _Num = value; OnPropertyChanged("Num"); } }
		}

		private DateTime _LastUpdate;
		/// <summary>
		/// 最后更新
		/// </summary>
		[Description("最后更新")]
		[DataObjectField(false, false, true, 3)]
		[BindColumn(5, "LastUpdate", "最后更新", "", "datetime", 3, 0, false)]
		public DateTime LastUpdate
		{
			get { return _LastUpdate; }
			set { if (OnPropertyChanging("LastUpdate", value)) { _LastUpdate = value; OnPropertyChanged("LastUpdate"); } }
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
					case "Kind" : return _Kind;
					case "Num" : return _Num;
					case "LastUpdate" : return _LastUpdate;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID" : _ID = Convert.ToInt32(value); break;
					case "Name" : _Name = Convert.ToString(value); break;
					case "Kind" : _Kind = Convert.ToInt32(value); break;
					case "Num" : _Num = Convert.ToInt32(value); break;
					case "LastUpdate" : _LastUpdate = Convert.ToDateTime(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
        /// <summary>取得序列字段信息的快捷方式</summary>
        [CLSCompliant(false)]
		public class _
		{
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>种类</summary>
            public static readonly Field Kind = Meta.Table.FindByName("Kind");

            ///<summary>数字</summary>
            public static readonly Field Num = Meta.Table.FindByName("Num");

            ///<summary>最后更新</summary>
            public static readonly Field LastUpdate = Meta.Table.FindByName("LastUpdate");
		}
		#endregion
	}
}