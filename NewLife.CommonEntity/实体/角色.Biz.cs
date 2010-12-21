using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NewLife.Log;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 角色
    /// </summary>
    public partial class Role<TEntity, TMenuEntity, TRoleMenuEntity> : Role<TEntity>
        where TEntity : Role<TEntity, TMenuEntity, TRoleMenuEntity>, new()
        where TMenuEntity : Menu<TMenuEntity>, new()
        where TRoleMenuEntity : RoleMenu<TRoleMenuEntity>, new()
    {
        #region 扩展属性
        //private List<String> hasLoaded = new List<string>();

        /// <summary>
        /// 菜单
        /// </summary>
        [XmlIgnore]
        public virtual EntityList<TRoleMenuEntity> Menus
        {
            get { return GetExtend<TRoleMenuEntity, EntityList<TRoleMenuEntity>>("Menus", delegate { return RoleMenu<TRoleMenuEntity>.FindAllByRoleID(ID); }); }
            set { Extends["Menus"] = value; }
        }

        //[NonSerialized]
        //private EntityList<TMenuEntity> _MenuList;
        /// <summary>
        /// 拥有权限的菜单
        /// </summary>
        [XmlIgnore]
        public virtual EntityList<TMenuEntity> MenuList
        {
            get
            {
                return GetExtend<TMenuEntity, EntityList<TMenuEntity>>("MenuList", delegate
                {
                    EntityList<TMenuEntity> list = EntityList<TMenuEntity>.From<TRoleMenuEntity>(Menus, delegate(TRoleMenuEntity item)
                    {
                        return Menu<TMenuEntity>.FindByID(item.MenuID);
                    });
                    if (list != null) list.Sort(Menu<TMenuEntity>._.Sort, true);
                    return list;
                });
            }
            set { Extends["MenuList"] = value; }
        }
        #endregion

        #region 业务
        ///// <summary>
        ///// 拥有指定菜单的权限，支持路径查找
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public override Boolean HasMenu(String name)
        //{
        //    if (String.IsNullOrEmpty(name) || MenuList == null || MenuList.Count < 1) return false;

        //    //TMenuEntity entity = Menu<TMenuEntity>.FindByPath(MenuList, name, Menu<TMenuEntity>._.Permission);
        //    TMenuEntity entity = Menu<TMenuEntity>.FindForPerssion(name);

        //    // 找不到的时候，修改当前页面
        //    if (entity == null && Menu<TMenuEntity>.Current != null)
        //    {
        //        if (Menu<TMenuEntity>.Current.ResetName(name)) entity = Menu<TMenuEntity>.Current;
        //    }

        //    //return entity != null;

        //    if (entity == null) return false;

        //    return HasMenu(entity.ID);
        //}

        ///// <summary>
        ///// 拥有指定菜单的权限
        ///// </summary>
        ///// <param name="menuID"></param>
        ///// <returns></returns>
        //public override Boolean HasMenu(Int32 menuID)
        //{
        //    //if (menu == null || MenuList == null || MenuList.Count < 1) return false;
        //    if (menuID <= 0) return false;

        //    TMenuEntity menu = Menu<TMenuEntity>.FindByID(menuID);

        //    // 当前菜单
        //    Boolean b = false;
        //    foreach (TMenuEntity item in MenuList)
        //    {
        //        if (item.ID == menu.ID)
        //        {
        //            b = true;
        //            break;
        //        }
        //    }
        //    if (!b) return false;

        //    // 判断父菜单
        //    if (menu.ParentID <= 0) return true;

        //    return HasMenu(menu.ParentID);
        //}

        /// <summary>
        /// 申请指定菜单指定操作的权限
        /// </summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public override Boolean Acquire(Int32 menuID, PermissionFlags flag)
        {
            //if (String.IsNullOrEmpty(name) || MenuList == null || MenuList.Count < 1) return false;
            if (menuID <= 0 || MenuList == null || MenuList.Count < 1) return false;

            // 找到菜单。自下而上递归查找，任意一级没有权限即视为无权限
            Int32 id = menuID;
            while (id > 0)
            {
                TMenuEntity entity = MenuList.Find(Menu<TMenuEntity>._.ID, id);
                if (entity == null) return false;

                if (entity.Parent == null) break;

                id = entity.ParentID;
            }

            // 申请权限
            if (flag == PermissionFlags.None) return true;

            TRoleMenuEntity rm = Menus.Find(Menu<TMenuEntity>._.ID, id);
            if (rm == null) return false;

            return rm.Acquire(flag);
        }

        /// <summary>
        /// 取得当前角色的子菜单，有权限、可显示、排序
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        public EntityList<TMenuEntity> GetMySubMenus(Int32 parentID)
        {
            EntityList<TMenuEntity> list = MenuList;
            if (list == null || list.Count < 1) return null;

            list = list.FindAll(Menu<TMenuEntity>._.ParentID, parentID);
            if (list == null || list.Count < 1) return null;
            list = list.FindAll(Menu<TMenuEntity>._.IsShow, true);
            if (list == null || list.Count < 1) return null;

            return list;
        }
        #endregion
    }

    /// <summary>
    /// 角色
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract partial class Role<TEntity> : CommonEntityBase<TEntity>
          where TEntity : Role<TEntity>, new()
    {
        #region 对象操作
        static Role()
        {
            if (Meta.Count < 1)
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化角色数据……");

                TEntity entity = new TEntity();
                entity.Name = "管理员";
                entity.Save();

                if (XTrace.Debug) XTrace.WriteLine("完成初始化角色数据！");
            }
        }

        /// <summary>
        /// 已重载。调用Save时写日志，而调用Insert和Update时不写日志
        /// </summary>
        /// <returns></returns>
        public override int Save()
        {
            if (ID == 0)
                WriteLog("添加", Name);
            else
                WriteLog("修改", Name);

            return base.Save();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override int Delete()
        {
            WriteLog("删除", Name);

            return base.Delete();
        }
        #endregion

        #region 扩展查询
        /// <summary>
        /// 根据主键查询一个角色实体对象用于表单编辑
        /// </summary>
        /// <param name="__ID">角色编号</param>
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
        /// 根据编号查找角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.Find(_.ID, id);
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        ///// <summary>
        ///// 拥有指定菜单的权限，支持路径查找
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public virtual Boolean HasMenu(String name) { return false; }

        ///// <summary>
        ///// 拥有指定菜单的权限
        ///// </summary>
        ///// <param name="menuID"></param>
        ///// <returns></returns>
        //public virtual Boolean HasMenu(Int32 menuID) { return false; }

        /// <summary>
        /// 申请指定菜单指定操作的权限
        /// </summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public abstract Boolean Acquire(Int32 menuID, PermissionFlags flag);
        #endregion
    }
}