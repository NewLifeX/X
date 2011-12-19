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
	/// 角色和菜单
	/// </summary>
	[Serializable]
	[DataObject]
    [BindIndex("IX_RoleMenu_MenuID_RoleID", true, "MenuID,RoleID")]
    [BindIndex("PK__RoleMenu", true, "ID")]
    [BindIndex("IX_RoleMenu_MenuID", false, "MenuID")]
    [BindIndex("IX_RoleMenu_RoleID", false, "RoleID")]
    [BindRelation("MenuID", false, "Menu", "ID")]
    [BindRelation("RoleID", false, "Role", "ID")]
    [Description("角色和菜单")]
	[BindTable("RoleMenu", Description = "角色和菜单", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class RoleMenu<TEntity> : IRoleMenu
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

		private Int32 _RoleID;
		/// <summary>
		/// 角色编号
		/// </summary>
		[Description("角色编号")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(2, "RoleID", "角色编号", "", "int", 10, 0, false)]
		public Int32 RoleID
		{
			get { return _RoleID; }
			set { if (OnPropertyChanging("RoleID", value)) { _RoleID = value; OnPropertyChanged("RoleID"); } }
		}

		private Int32 _MenuID;
		/// <summary>
		/// 菜单编号
		/// </summary>
		[Description("菜单编号")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(3, "MenuID", "菜单编号", "", "int", 10, 0, false)]
		public Int32 MenuID
		{
			get { return _MenuID; }
			set { if (OnPropertyChanging("MenuID", value)) { _MenuID = value; OnPropertyChanged("MenuID"); } }
		}

		private Int32 _Permission;
		/// <summary>
		/// 权限
		/// </summary>
		[Description("权限")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn(4, "Permission", "权限", "", "int", 10, 0, false)]
		public Int32 Permission
		{
			get { return _Permission; }
			set { if (OnPropertyChanging("Permission", value)) { _Permission = value; OnPropertyChanged("Permission"); } }
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
					case "RoleID" : return _RoleID;
					case "MenuID" : return _MenuID;
					case "Permission" : return _Permission;
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID" : _ID = Convert.ToInt32(value); break;
					case "RoleID" : _RoleID = Convert.ToInt32(value); break;
					case "MenuID" : _MenuID = Convert.ToInt32(value); break;
					case "Permission" : _Permission = Convert.ToInt32(value); break;
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
		/// <summary>
		/// 取得角色和菜单字段名的快捷方式
		/// </summary>
		public class _
		{
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>角色编号</summary>
            public static readonly Field RoleID = Meta.Table.FindByName("RoleID");

            ///<summary>菜单编号</summary>
            public static readonly Field MenuID = Meta.Table.FindByName("MenuID");

            ///<summary>权限</summary>
            public static readonly Field Permission = Meta.Table.FindByName("Permission");
		}
		#endregion
	}

	/// <summary>
	/// 角色和菜单接口
	/// </summary>
	public partial interface IRoleMenu
	{
		#region 属性
		/// <summary>
		/// 编号
		/// </summary>
		Int32 ID { get; set; }

		/// <summary>
		/// 角色编号
		/// </summary>
		Int32 RoleID { get; set; }

		/// <summary>
		/// 菜单编号
		/// </summary>
		Int32 MenuID { get; set; }

		/// <summary>
		/// 权限
		/// </summary>
		Int32 Permission { get; set; }
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