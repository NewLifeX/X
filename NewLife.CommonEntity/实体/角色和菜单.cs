using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
	/// <summary>
	/// 角色和菜单
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("角色和菜单")]
	[BindTable("RoleMenu", Description = "角色和菜单", ConnName = "Common")]
    public partial class RoleMenu<TEntity>
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
		
		private Int32 _RoleID;
		/// <summary>
		/// 角色编号
		/// </summary>
		[Description("角色编号")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("RoleID", Description = "角色编号", DefaultValue = "", Order = 2)]
		public Int32 RoleID
		{
			get { return _RoleID; }
			set { if (OnPropertyChange("RoleID", value)) _RoleID = value; }
		}
		
		private Int32 _MenuID;
		/// <summary>
		/// 菜单编号
		/// </summary>
		[Description("菜单编号")]
		[DataObjectField(false, false, true, 10)]
		[BindColumn("MenuID", Description = "菜单编号", DefaultValue = "", Order = 3)]
		public Int32 MenuID
		{
			get { return _MenuID; }
			set { if (OnPropertyChange("MenuID", value)) _MenuID = value; }
		}

        private Int32 _Permission;
        /// <summary>
        /// 权限
        /// </summary>
        [Description("权限")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn("Permission", Description = "权限", DefaultValue = "", Order = 4)]
        public Int32 Permission
        {
            get { return _Permission; }
            set { if (OnPropertyChange("Permission", value)) _Permission = value; }
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
                    case "ID": return _ID;
                    case "RoleID": return _RoleID;
                    case "MenuID": return _MenuID;
                    case "Permission": return _Permission;
                    default: return base[name];
				}
			}
			set
			{
				switch (name)
				{
					case "ID": _ID = Convert.ToInt32(value); break;
					case "RoleID": _RoleID = Convert.ToInt32(value); break;
					case "MenuID": _MenuID = Convert.ToInt32(value); break;
                    case "Permission": _Permission = Convert.ToInt32(value); break;
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
			/// 角色编号
			/// </summary>
			public const String RoleID = "RoleID";
			
			/// <summary>
			/// 菜单编号
			/// </summary>
			public const String MenuID = "MenuID";

            /// <summary>
            /// 权限
            /// </summary>
            public const String Permission = "Permission";
        }
		#endregion
	}
}