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
	/// 地区
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("地区")]
	[BindTable("Area", Description = "地区", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Area<TEntity> : IArea
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

		private Int32 _Code;
		/// <summary>
		/// 代码
		/// </summary>
		[Description("代码")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(2, "Code", "代码", "", "int", 10, 0, false)]
		public Int32 Code
		{
			get { return _Code; }
			set { if (OnPropertyChanging("Code", value)) { _Code = value; OnPropertyChanged("Code"); } }
		}

		private String _Name;
		/// <summary>
		/// 名称
		/// </summary>
		[Description("名称")]
		[DataObjectField(false, false, false, 50)]
		[BindColumn(3, "Name", "名称", "", "nvarchar(50)", 0, 0, true)]
		public String Name
		{
			get { return _Name; }
			set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
		}

		private Int32 _ParentCode;
		/// <summary>
		/// 父地区代码
		/// </summary>
		[Description("父地区代码")]
		[DataObjectField(false, false, false, 10)]
		[BindColumn(4, "ParentCode", "父地区代码", "0", "int", 10, 0, false)]
		public Int32 ParentCode
		{
			get { return _ParentCode; }
			set { if (OnPropertyChanging("ParentCode", value)) { _ParentCode = value; OnPropertyChanged("ParentCode"); } }
		}

		private String _Description;
		/// <summary>
		/// 描述
		/// </summary>
		[Description("描述")]
		[DataObjectField(false, false, true, 1073741823)]
		[BindColumn(5, "Description", "描述", "", "ntext", 0, 0, true)]
		public String Description
		{
			get { return _Description; }
			set { if (OnPropertyChanging("Description", value)) { _Description = value; OnPropertyChanged("Description"); } }
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
					case "Code" : return _Code;
					case "Name" : return _Name;
					case "ParentCode" : return _ParentCode;
					case "Description" : return _Description;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID" : _ID = Convert.ToInt32(value); break;
					case "Code" : _Code = Convert.ToInt32(value); break;
					case "Name" : _Name = Convert.ToString(value); break;
					case "ParentCode" : _ParentCode = Convert.ToInt32(value); break;
					case "Description" : _Description = Convert.ToString(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

        #region 字段名
        /// <summary>取得地区字段信息的快捷方式</summary>
        [CLSCompliant(false)]
        public class _
        {
            ///<summary>编号</summary>
            public static readonly FieldItem ID = Meta.Table.FindByName("ID");

            ///<summary>代码</summary>
            public static readonly FieldItem Code = Meta.Table.FindByName("Code");

            ///<summary>名称</summary>
            public static readonly FieldItem Name = Meta.Table.FindByName("Name");

            ///<summary>父地区代码</summary>
            public static readonly FieldItem ParentCode = Meta.Table.FindByName("ParentCode");

            ///<summary>描述</summary>
            public static readonly FieldItem Description = Meta.Table.FindByName("Description");
        }
        #endregion
    }

	/// <summary>
	/// 地区接口
	/// </summary>
	public partial interface IArea
	{
		#region 属性
		/// <summary>
		/// 编号
		/// </summary>
		Int32 ID { get; set; }

		/// <summary>
		/// 代码
		/// </summary>
		Int32 Code { get; set; }

		/// <summary>
		/// 名称
		/// </summary>
		String Name { get; set; }

		/// <summary>
		/// 父地区代码
		/// </summary>
		Int32 ParentCode { get; set; }

		/// <summary>
		/// 描述
		/// </summary>
		String Description { get; set; }
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