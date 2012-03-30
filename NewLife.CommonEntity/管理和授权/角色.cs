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
    /// <summary>角色</summary>
    [Serializable]
    [DataObject]
    [BindIndex("IX_Role_Name", true, "Name")]
    [BindIndex("PK__Role", true, "ID")]
    [BindRelation("ID", true, "RoleMenu", "RoleID")]
    [BindRelation("ID", true, "Administrator", "RoleID")]
    [Description("角色")]
    [BindTable("Role", Description = "角色", ConnName = "Common", DbType = DatabaseType.SqlServer)]
    public partial class Role<TEntity> : IRole
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

        private String _Name;
        /// <summary>角色名称</summary>
        [Description("角色名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(2, "Name", "角色名称", "", "nvarchar(50)", 0, 0, true)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } }
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
                    case "Name": return _Name;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = Convert.ToInt32(value); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得角色字段名的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly Field ID = Meta.Table.FindByName("ID");

            ///<summary>角色名称</summary>
            public static readonly Field Name = Meta.Table.FindByName("Name");
        }
        #endregion
    }

    /// <summary>角色接口</summary>
    public partial interface IRole
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>角色名称</summary>
        String Name { get; set; }
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