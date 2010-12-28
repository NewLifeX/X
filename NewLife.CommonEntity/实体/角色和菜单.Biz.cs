using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using XCode;
using System.Threading;
using NewLife.Log;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 操作权限
    /// </summary>
    [Flags]
    [Description("操作权限")]
    public enum PermissionFlags
    {
        /// <summary>
        /// 无权限
        /// </summary>
        [Description("无")]
        None = 0,

        /// <summary>
        /// 所有权限
        /// </summary>
        [Description("所有")]
        All = 1,

        /// <summary>
        /// 添加权限
        /// </summary>
        [Description("添加")]
        Insert = 2,

        /// <summary>
        /// 修改权限
        /// </summary>
        [Description("修改")]
        Update = 4,

        /// <summary>
        /// 删除权限
        /// </summary>
        [Description("删除")]
        Delete = 8,

        /// <summary>
        /// 自定义1权限
        /// </summary>
        /// <remarks>这里没有接着排16，为了保留给上面使用</remarks>
        [Description("自定义1")]
        Custom1 = 0x20,

        /// <summary>
        /// 自定义2权限
        /// </summary>
        [Description("自定义2")]
        Custom2 = Custom1 * 2,

        /// <summary>
        /// 自定义3权限
        /// </summary>
        [Description("自定义3")]
        Custom3 = Custom2 * 2,

        /// <summary>
        /// 自定义4权限
        /// </summary>
        [Description("自定义4")]
        Custom4 = Custom3 * 2,

        /// <summary>
        /// 自定义5权限
        /// </summary>
        [Description("自定义5")]
        Custom5 = Custom4 * 2,

        /// <summary>
        /// 自定义6权限
        /// </summary>
        [Description("自定义6")]
        Custom6 = Custom5 * 2,

        /// <summary>
        /// 自定义7权限
        /// </summary>
        [Description("自定义7")]
        Custom7 = Custom6 * 2,

        /// <summary>
        /// 自定义8权限
        /// </summary>
        [Description("自定义8")]
        Custom8 = Custom7 * 2
    }

    /// <summary>
    /// 角色和菜单
    /// </summary>
    public partial class RoleMenu<TEntity> : CommonEntityBase<TEntity> where TEntity : RoleMenu<TEntity>, new()
    {
        #region 对象操作
        /// <summary>
        /// 已重载。调用Save时写日志，而调用Insert和Update时不写日志
        /// </summary>
        /// <returns></returns>
        public override int Save()
        {
            if (ID == 0)
                WriteLog("添加", String.Format("Role={0},Menu={1},Permission={2}", RoleID, MenuID, PermissionFlag));
            else
                WriteLog("修改", String.Format("Role={0},Menu={1},Permission={2}", RoleID, MenuID, PermissionFlag));

            return base.Save();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override int Delete()
        {
            String remark = String.Format("Role={0},Menu={1},Permission={2}", RoleID, MenuID, PermissionFlag);
            if (RoleID <= 0 && MenuID <= 0)
            {
                TEntity entity = Find(_.ID, ID);
                if (entity != null) remark = String.Format("Role={0},Menu={1},Permission={2}", RoleID, MenuID, PermissionFlag);
            }
            WriteLog("删除", remark);
            //WriteLog("删除", String.Format("Role={0},Menu={1},Permission={2}", RoleID, MenuID, PermissionFlag));

            return base.Delete();
        }

        static RoleMenu()
        {
            // 检查是否所有人都没有权限
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    CheckNonePerssion(0);
                }
                catch (Exception ex)
                {
                    if (XTrace.Debug) XTrace.WriteLine(ex.ToString());
                }
            });
        }

        /// <summary>
        /// 检查指定菜单编号的权限，保证至少有一个角色有权限控制该菜单
        /// </summary>
        /// <param name="menuID"></param>
        internal static void CheckNonePerssion(Int32 menuID)
        {
            if (Meta.Cache.Entities.Count < 1) return;

            EntityList<TEntity> list = Meta.Cache.Entities;
            if (menuID > 0) list = list.FindAll(_.MenuID, menuID);
            if (list == null || list.Count < 1) return;

            EntityList<TEntity> list2 = list.FindAll(_.Permission, (Int32)PermissionFlags.None);
            // 判断是否所有实体都是None权限
            if (list2 != null && list2.Count == list.Count)
            {
                WriteLog("授权检查", String.Format("没有任何角色拥有菜单{0}的权限，准备授权所有角色拥有该菜单的所有权限！", menuID));

                // 授权所有实体为All权限
                list.SetItem(_.Permission, (Int32)PermissionFlags.All);
                list.Save();
            }
        }
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

        /// <summary>操作权限</summary>
        public PermissionFlags PermissionFlag
        {
            get { return (PermissionFlags)Permission; }
            set { Permission = (Int32)value; }
        }
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
        /// 根据角色编号查询所有角色和菜单实体对象
        /// </summary>
        /// <param name="roleID">编号</param>
        /// <returns>角色和菜单 实体对象</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllByRoleID(Int32 roleID)
        {
            if (roleID <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.FindAll(_.RoleID, roleID);
        }

        /// <summary>
        /// 根据菜单编号查询所有角色和菜单实体对象
        /// </summary>
        /// <param name="menuID">编号</param>
        /// <returns>角色和菜单 实体对象</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllByMenuID(Int32 menuID)
        {
            if (menuID <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.FindAll(_.MenuID, menuID);
        }

        /// <summary>
        /// 根据角色编号和菜单查询一个角色和菜单实体对象
        /// </summary>
        /// <param name="roleID">编号</param>
        /// <param name="menuID">编号</param>
        /// <returns>角色和菜单 实体对象</returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByRoleAndMenu(Int32 roleID, Int32 menuID)
        {
            if (roleID <= 0 || menuID <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            // 分两次查询
            EntityList<TEntity> list = FindAllByRoleID(roleID);
            if (list == null || list.Count < 1) return null;

            return list.Find(_.MenuID, menuID);
        }
        #endregion

        #region 扩展操作
        /// <summary>
        /// 根据角色编号和菜单编号创建一个角色菜单实体，默认授予完全控制权限
        /// </summary>
        /// <param name="roleID"></param>
        /// <param name="menuID"></param>
        /// <returns></returns>
        public static TEntity Create(Int32 roleID, Int32 menuID)
        {
            TEntity entity = new TEntity();
            entity.RoleID = roleID;
            entity.MenuID = menuID;
            entity.PermissionFlag = PermissionFlags.All;
            return entity;
        }
        #endregion

        #region 业务
        /// <summary>
        /// 检查是否有指定权限
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Boolean Acquire(PermissionFlags flag)
        {
            if (PermissionFlag == PermissionFlags.None) return false;
            //if (PermissionFlag == PermissionFlags.All) return true;
            if ((PermissionFlag & PermissionFlags.All) == PermissionFlags.All) return true;

            return (PermissionFlag & flag) == flag;
        }

        /// <summary>
        /// 添加权限
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public RoleMenu<TEntity> Add(PermissionFlags flag)
        {
            PermissionFlag |= flag;

            return this;
        }

        /// <summary>
        /// 删除权限
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public RoleMenu<TEntity> Remove(PermissionFlags flag)
        {
            // 必须先检查是否包含这个标识位，因为异或的操作仅仅是取反
            if ((PermissionFlag & flag) == flag) PermissionFlag ^= flag;

            return this;
        }
        #endregion
    }
}