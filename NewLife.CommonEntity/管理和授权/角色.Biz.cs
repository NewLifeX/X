using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
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
        #region 对象操作
        //static Role()
        //{
        //    // 删除RoleMenu中无效的RoleID和无效的MenuID
        //    //ClearRoleMenu();
        //    ThreadPool.QueueUserWorkItem(delegate { ClearRoleMenu(); });
        //}

        /// <summary>
        /// 首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            ClearRoleMenu();

            // 如果角色菜单对应关系为空或者只有一个，则授权第一个角色访问所有菜单
            //if (RoleMenu<TRoleMenuEntity>.Meta.Count > 1) return;

            EntityList<TMenuEntity> ms = null;
            // 等一下菜单那边初始化
            for (int i = 0; i < 100; i++)
            {
                ms = Menu<TMenuEntity>.Meta.Cache.Entities;
                if (ms != null && ms.Count > 0) break;

                Thread.Sleep(100);
            }
            if (ms == null || ms.Count < 1) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}授权数据……", typeof(TEntity).Name);

            Meta.BeginTrans();
            try
            {
                Int32 id = 1;
                EntityList<TEntity> rs = Meta.Cache.Entities;
                if (rs != null && rs.Count > 0)
                {
                    id = rs[0].ID;
                }

                // 授权访问所有菜单
                //EntityList<TMenuEntity> ms = Menu<TMenuEntity>.FindAll();
                if (ms != null && ms.Count > 0)
                {
                    EntityList<TRoleMenuEntity> rms = RoleMenu<TRoleMenuEntity>.FindAllByRoleID(id);
                    foreach (TMenuEntity item in ms)
                    {
                        // 是否已存在
                        if (rms != null && rms.Exists(RoleMenu<TRoleMenuEntity>._.MenuID, item.ID)) continue;

                        //TRoleMenuEntity entity = new TRoleMenuEntity();
                        //entity.RoleID = id;
                        //entity.MenuID = item.ID;
                        TRoleMenuEntity entity = RoleMenu<TRoleMenuEntity>.Create(id, item.ID);
                        entity.Save();
                    }
                }

                Meta.Commit();
                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}授权数据！", typeof(TEntity).Name);
            }
            catch { Meta.Rollback(); throw; }
        }

        /// <summary>
        /// 删除RoleMenu中无效的RoleID和无效的MenuID
        /// </summary>
        public static void ClearRoleMenu()
        {
            // 统计所有RoleID和MenuID
            List<TEntity> list1 = Meta.Cache.Entities;
            List<TMenuEntity> list2 = Menu<TMenuEntity>.Meta.Cache.Entities;

            StringBuilder sb = new StringBuilder();
            if (list1 != null && list1.Count > 0)
            {
                sb.Append("RoleID Not in(");
                for (int i = 0; i < list1.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(list1[i].ID);
                }
                sb.Append(")");
            }
            if (list2 != null && list2.Count > 0)
            {
                if (sb.Length > 0) sb.Append(" Or ");

                sb.Append("MenuID Not in(");
                for (int i = 0; i < list2.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(list2[i].ID);
                }
                sb.Append(")");
            }
            if (sb.Length < 1) return;

            // 查询所有。之所以不是调用Delete删除，是为了引发RoleMenu里面的Delete写日志
            EntityList<TRoleMenuEntity> rms = RoleMenu<TRoleMenuEntity>.FindAll(sb.ToString(), null, null, 0, 0);
            if (rms == null || rms.Count < 1) return;

            rms.Delete();
        }

        ///// <summary>
        ///// 检查是否所有人都没有权限
        ///// </summary>
        //static void CheckNonePerssion()
        //{
        //    TMenuEntity entity = Menu<TMenuEntity>.FindByName("权限管理");
        //    if (entity != null) RoleMenu<TRoleMenuEntity>.CheckNonePerssion(entity.ID);
        //}

        /// <summary>
        /// 已重载。关联删除权限项。
        /// </summary>
        /// <returns></returns>
        public override int Delete()
        {
            Meta.BeginTrans();
            try
            {
                if (Menus != null) Menus.Delete();
                Int32 rs = base.Delete();

                Meta.Commit();

                return rs;
            }
            catch
            {
                Meta.Rollback();
                throw;
            }
        }
        #endregion

        #region 扩展属性
        //private List<String> hasLoaded = new List<string>();

        /// <summary>
        /// 菜单
        /// </summary>
        [XmlIgnore]
        public virtual EntityList<TRoleMenuEntity> Menus
        {
            get { return GetExtend<TRoleMenuEntity, EntityList<TRoleMenuEntity>>("Menus", delegate { return RoleMenu<TRoleMenuEntity>.FindAllByRoleID(ID); }, false); }
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
                }, false);
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

            TRoleMenuEntity rm = Menus.Find(RoleMenu<TRoleMenuEntity>._.MenuID, menuID);
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

        /// <summary>
        /// 取得当前角色的子菜单，有权限、可显示、排序
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        protected internal override List<IMenu> GetMySubMenusInternal(int parentID)
        {
            EntityList<TMenuEntity> list = GetMySubMenus(parentID);
            if (list == null || list.Count < 1) return null;

            List<IMenu> list2 = new List<IMenu>();
            foreach (TMenuEntity item in list)
            {
                list2.Add(item);
            }

            return list2;
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
        //static Role()
        //{
        //    if (Meta.Count < 1)
        //    {
        //        if (XTrace.Debug) XTrace.WriteLine("开始初始化角色数据……");

        //        TEntity entity = new TEntity();
        //        entity.Name = "管理员";
        //        entity.Save();

        //        if (XTrace.Debug) XTrace.WriteLine("完成初始化角色数据！");
        //    }
        //}

        /// <summary>
        /// 首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化角色数据……");

            TEntity entity = new TEntity();
            entity.Name = "管理员";
            entity.Save();

            if (XTrace.Debug) XTrace.WriteLine("完成初始化角色数据！");
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
            String name = Name;
            if (String.IsNullOrEmpty(name))
            {
                TEntity entity = FindByID(ID);
                if (entity != null) name = entity.Name;
            }
            WriteLog("删除", name);

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

        /// <summary>
        /// 取得当前角色的子菜单，有权限、可显示、排序
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        List<IMenu> IRole.GetMySubMenus(Int32 parentID) { return GetMySubMenusInternal(parentID); }

        /// <summary>
        /// 取得当前角色的子菜单，有权限、可显示、排序
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        internal protected abstract List<IMenu> GetMySubMenusInternal(Int32 parentID);
        #endregion
    }

    public partial interface IRole
    {
        /// <summary>
        /// 申请指定菜单指定操作的权限
        /// </summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Acquire(Int32 menuID, PermissionFlags flag);

        /// <summary>
        /// 取得当前角色的子菜单，有权限、可显示、排序
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        List<IMenu> GetMySubMenus(Int32 parentID);
    }
}