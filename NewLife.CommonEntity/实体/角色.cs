using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 角色
    /// </summary>
    [Serializable]
    [DataObject]
    [Description("角色")]
    [BindTable("Role", Description = "角色", ConnName = "Common")]
    public partial class Role<TEntity>
    {
        #region 属性
        private Int32 _ID;
        /// <summary>
        /// 角色编号
        /// </summary>
        [Description("角色编号")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn("ID", Description = "角色编号", DefaultValue = "", Order = 1)]
        public Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChange("ID", value)) _ID = value; }
        }

        private String _Name;
        /// <summary>
        /// 角色名称
        /// </summary>
        [Description("角色名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", Description = "角色名称", DefaultValue = "", Order = 2)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChange("Name", value)) _Name = value; }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化一个角色实例。
        /// </summary>
        public Role()
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
                    case "Name": return Name;
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

        #region 主键
        /// <summary>
        /// 根据主键查询一个角色实体对象
        /// </summary>
        /// <param name="__ID">角色编号</param>
        /// <returns>角色 实体对象</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKey(Int32 __ID)
        {
            IList<TEntity> list = FindAll(new String[] { "ID" }, new Object[] { __ID });
            if (list != null && list.Count > 0) return list[0];
            return null;
        }
        #endregion

        #region 删除
        /// <summary>
        /// 根据唯一键ID从数据库中删除指定实体对象。
        /// </summary>
        /// <param name="__ID">唯一键</param>
        /// <returns>角色 实体对象</returns>
        [DataObjectMethod(DataObjectMethodType.Delete, false)]
        public static Int32 DeleteByKey(Int32 __ID)
        {
            return Delete(new String[] { _.ID }, new Object[] { __ID });
        }
        #endregion

        #region 字段名
        /// <summary>
        /// 取得字段名的快捷方式
        /// </summary>
        public class _
        {
            /// <summary>
            /// 角色编号
            /// </summary>
            public const String ID = "ID";

            /// <summary>
            /// 角色名称
            /// </summary>
            public const String Name = "Name";
        }
        #endregion
    }
}