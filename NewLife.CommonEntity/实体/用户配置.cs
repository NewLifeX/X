/*
 * XCoder v3.4.2011.0329
 * 作者：nnhy/X
 * 时间：2011-06-21 21:06:45
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
	/// <summary>用户配置</summary>
	[Serializable]
	[DataObject]
	[Description("用户配置")]
	[BindTable("UserProfile", Description = "用户配置", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class UserProfile<TEntity>
	{
		#region 属性
		private Int32 _ID;
		/// <summary>编号</summary>
		[Description("编号")]
		[DataObjectField(true, true, false, 10)]
		[BindColumn(1, "ID", "编号", "", "int", 10, 0, false)]
		public Int32 ID
		{
			get { return _ID; }
			set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
		}

		private Int32 _UserID;
		/// <summary>父编号</summary>
		[Description("父编号")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(2, "UserID", "父编号", "", "int", 10, 0, false)]
		public Int32 UserID
		{
			get { return _UserID; }
			set { if (OnPropertyChanging("UserID", value)) { _UserID = value; OnPropertyChanged("UserID"); } }
		}

		private String _Name;
		/// <summary>名称</summary>
		[Description("名称")]
		[DataObjectField(false, false, true, 50)]
		[BindColumn(3, "Name", "名称", "", "nvarchar(50)", 0, 0, true)]
		public String Name
		{
			get { return _Name; }
			set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
		}

		private Int32 _Kind;
		/// <summary>值类型</summary>
		[Description("值类型")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(4, "Kind", "值类型", "", "int", 10, 0, false)]
		public Int32 Kind
		{
			get { return _Kind; }
			set { if (OnPropertyChanging("Kind", value)) { _Kind = value; OnPropertyChanged("Kind"); } }
		}

		private String _Value;
		/// <summary>值</summary>
		[Description("值")]
		[DataObjectField(false, false, true, 500)]
		[BindColumn(5, "Value", "值", "", "nvarchar(500)", 0, 0, true)]
		public String Value
		{
			get { return _Value; }
			set { if (OnPropertyChanging("Value", value)) { _Value = value; OnPropertyChanged("Value"); } }
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
					case "UserID" : return _UserID;
					case "Name" : return _Name;
					case "Kind" : return _Kind;
					case "Value" : return _Value;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID" : _ID = Convert.ToInt32(value); break;
					case "UserID" : _UserID = Convert.ToInt32(value); break;
					case "Name" : _Name = Convert.ToString(value); break;
					case "Kind" : _Kind = Convert.ToInt32(value); break;
					case "Value" : _Value = Convert.ToString(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
        /// <summary>取得用户配置字段信息的快捷方式</summary>
		public class _
		{
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>父编号</summary>
            public static readonly Field UserID = Meta.Table.FindByName("UserID");

            ///<summary>名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");

            ///<summary>值类型</summary>
            public static readonly Field Kind = Meta.Table.FindByName("Kind");

            ///<summary>值</summary>
            public static readonly Field Value = Meta.Table.FindByName("Value");
		}
		#endregion
	}
}