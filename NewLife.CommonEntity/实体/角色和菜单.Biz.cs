using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 角色和菜单
    /// </summary>
    public partial class RoleMenu<TEntity> : Entity<TEntity> where TEntity : RoleMenu<TEntity>, new()
    {
        #region 对象操作
        //基类Entity中包含三个对象操作：Insert、Update、Delete
        //你可以重载它们，以改变它们的行为
        //如：
        /*
        /// <summary>
        /// 已重载。把该对象插入到数据库。这里可以做数据插入前的检查
        /// </summary>
        /// <returns>影响的行数</returns>
        public override Int32 Insert()
        {
            return base.Insert();
        }
         * */
        #endregion

        #region 扩展属性
        //TODO: 本类与哪些类有关联，可以在这里放置一个属性，使用延迟加载的方式获取关联对象
        /*
        private List<String> hasLoaded = new List<string>();
        private Category _Category;
        /// <summary>该商品所对应的类别</summary>
        public Category Category
        {
            get
            {
                if (_Category == null && CategoryID > 0 && !hasLoaded.Contains("Category"))
                {
                    _Category = Category.FindByKey(CategoryID);
                    hasLoaded.Add("Category");
                }
                return _Category;
            }
            set { _Category = value; }
        }
         * */
        #endregion

        #region 扩展查询
        /// <summary>
        /// 根据主键查询一个角色和菜单实体对象用于表单编辑
        /// </summary>
        /// <param name="__ID">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKeyForEdit(Int32 __ID)
        {
            TEntity entity = FindByKey(__ID);
            if (entity == null)
            {
                entity = new TEntity();
            }
            return entity;
        }

        /// <summary>
        /// 根据角色编号查询一个角色和菜单实体对象
        /// </summary>
        /// <param name="roleID">编号</param>
        /// <returns>角色和菜单 实体对象</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllByRoleID(Int32 roleID)
        {
            if (roleID <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.FindAll(_.RoleID, roleID);
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        #endregion
    }
}