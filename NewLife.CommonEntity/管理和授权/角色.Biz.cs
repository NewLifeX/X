using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using NewLife.Log;
using XCode;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace NewLife.CommonEntity
{
    /// <summary>角色</summary>
    public partial class Role<TEntity, TMenuEntity, TRoleMenuEntity> : Role<TEntity>
        where TEntity : Role<TEntity, TMenuEntity, TRoleMenuEntity>, new()
        where TMenuEntity : Menu<TMenuEntity>, new()
        where TRoleMenuEntity : RoleMenu<TRoleMenuEntity>, new()
    {
        #region 对象操作
        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            // 如果角色菜单对应关系为空或者只有一个，则授权第一个角色访问所有菜单
            if (RoleMenu<TRoleMenuEntity>.Meta.Count > 0)
            {
                if (XTrace.Debug) XTrace.WriteLine("如果某一个菜单对应的RoleMenu（角色菜单对应关系）为空或者只有一个，则授权第一个角色访问所有菜单！");

                foreach (var item in Menu<TMenuEntity>.Root.AllChilds)
                {
                    RoleMenu<TRoleMenuEntity>.CheckNonePerssion(item.ID);
                }

                ClearRoleMenu();
                return;
            }

            Menu<TMenuEntity>.Meta.WaitForInitData(10000);
            var ms = Menu<TMenuEntity>.Meta.Cache.Entities;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}授权数据……", typeof(TRoleMenuEntity).Name);

            Meta.BeginTrans();
            try
            {
                Int32 id = 1;
                var rs = Meta.Cache.Entities;
                if (rs != null && rs.Count > 0)
                {
                    id = rs[0].ID;
                }

                // 授权访问所有菜单
                if (ms != null && ms.Count > 0) RoleMenu<TRoleMenuEntity>.GrantAll(id, ms.GetItem<Int32>(Menu<TMenuEntity>._.ID));

                Meta.Commit();
                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}授权数据！", typeof(TRoleMenuEntity).Name);
            }
            catch { Meta.Rollback(); throw; }
        }

        /// <summary>删除RoleMenu中无效的RoleID和无效的MenuID</summary>
        public static void ClearRoleMenu()
        {
            // 等待RoleMenu初始化完成
            Int32 count = RoleMenu<TRoleMenuEntity>.Meta.Count;
            count = Menu<TMenuEntity>.Meta.Count;

            //// 统计所有RoleID和MenuID
            //var list1 = Meta.Cache.Entities.ToList();
            //var list2 = Menu<TMenuEntity>.Meta.Cache.Entities.ToList();

            //var exp = new WhereExpression();
            //exp &= RoleMenu<TRoleMenuEntity>._.RoleID.NotIn(list1.Select(e => e.ID));
            //exp |= RoleMenu<TRoleMenuEntity>._.MenuID.NotIn(list2.Select(e => e.ID));

            // 查询所有。之所以不是调用Delete删除，是为了引发RoleMenu里面的Delete写日志
            var rms = RoleMenu<TRoleMenuEntity>.FindAllInvalid(FindSQLWithKey(), Menu<TMenuEntity>.FindSQLWithKey());
            if (rms == null || rms.Count < 1) return;

            if (XTrace.Debug) XTrace.WriteLine("删除RoleMenu中无效的RoleID和无效的MenuID！");

            rms.Delete();
        }

        /// <summary>已重载。关联删除权限项。</summary>
        /// <returns></returns>
        protected override int OnDelete()
        {
            if (Menus != null) Menus.Delete();
            return base.OnDelete();
        }
        #endregion

        #region 扩展属性
        /// <summary>菜单</summary>
        [XmlIgnore]
        public virtual EntityList<TRoleMenuEntity> Menus
        {
            get { return GetExtend<TRoleMenuEntity, EntityList<TRoleMenuEntity>>("Menus", e => RoleMenu<TRoleMenuEntity>.FindAllByRoleID(ID), false); }
            set { Extends["Menus"] = value; }
        }

        /// <summary>拥有权限的菜单</summary>
        [XmlIgnore]
        public virtual EntityList<TMenuEntity> MenuList
        {
            get
            {
                return GetExtend<TMenuEntity, EntityList<TMenuEntity>>("MenuList", m =>
                {
                    //var list = EntityList<TMenuEntity>.From<TRoleMenuEntity>(Menus, item => Menu<TMenuEntity>.FindByID(item.MenuID));
                    //// 先按Sort降序，再按ID升序，的确更加完善
                    //if (list != null) list.Sort(new String[] { Menu<TMenuEntity>._.Sort, Menu<TMenuEntity>._.ID }, new bool[] { true, false });
                    //return list;
                    var list = Menus.ToList()
                        .Select(e => Menu<TMenuEntity>.FindByID(e.MenuID))
                        .OrderByDescending(e => e.Sort)
                        .ThenBy(e => e.ID);
                    return new EntityList<TMenuEntity>(list);
                }, false);
            }
            set { Extends["MenuList"] = value; }
        }
        #endregion

        #region 业务
        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public override Boolean Acquire(Int32 menuID, PermissionFlags flag)
        {
            if (menuID <= 0 || MenuList == null || MenuList.Count < 1) return false;

            // 找到菜单。自下而上递归查找，任意一级没有权限即视为无权限
            Int32 id = menuID;
            // 避免可能的死循环
            var list = new List<Int32>();
            while (id > 0)
            {
                var entity = MenuList.Find(Menu<TMenuEntity>._.ID, id);
                if (entity == null) return false;

                if (entity.Parent == null) break;

                id = entity.ParentID;
                if (list.Contains(id)) return false;
                list.Add(id);
            }

            // 申请权限
            if (flag == PermissionFlags.None) return true;

            TRoleMenuEntity rm = Menus.Find(RoleMenu<TRoleMenuEntity>._.MenuID, menuID);
            if (rm == null) return false;

            return rm.Acquire(flag);
        }

        /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        public EntityList<TMenuEntity> GetMySubMenus(Int32 parentID)
        {
            var list = MenuList;
            if (list == null || list.Count < 1) return null;

            list = list.FindAll(Menu<TMenuEntity>._.ParentID, parentID);
            if (list == null || list.Count < 1) return null;
            list = list.FindAll(Menu<TMenuEntity>._.IsShow, true);
            if (list == null || list.Count < 1) return null;

            return list;
        }

        /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        protected internal override List<IMenu> GetMySubMenusInternal(int parentID)
        {
            var list = GetMySubMenus(parentID);
            if (list == null || list.Count < 1) return null;

            return list.Cast<IMenu>().ToList();
        }

        /// <summary>当前角色拥有的权限</summary>
        public override List<IRoleMenu> RoleMenus { get { return Menus.ToList().Cast<IRoleMenu>().ToList(); } }

        /// <summary>当前角色拥有的菜单</summary>
        internal protected override List<IMenu> MenusInternal { get { return MenuList.ToList().Cast<IMenu>().ToList(); } }
        #endregion
    }

    /// <summary>角色</summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract partial class Role<TEntity> : CommonEntityBase<TEntity>
          where TEntity : Role<TEntity>, new()
    {
        #region 对象操作
        static Role()
        {
            // 用于引发基类的静态构造函数
            TEntity entity = new TEntity();
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}角色数据……", typeof(TEntity).Name);

            TEntity entity = new TEntity();
            entity.Name = "管理员";
            entity.Save();

            if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}角色数据！", typeof(TEntity).Name);
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。建议重写者调用基类的实现，因为将来可能根据数据字段的特性进行数据验证。</summary>
        /// <param name="isNew">是否新数据</param>
        public override void Valid(bool isNew)
        {
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(_.Name, _.Name.DisplayName + "不能为空！");

            base.Valid(isNew);
        }

        /// <summary>已重载。调用Save时写日志，而调用Insert和Update时不写日志</summary>
        /// <returns></returns>
        public override int Save()
        {
            if (ID == 0)
                WriteLog("添加", Name);
            else
                WriteLog("修改", Name);

            return base.Save();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override int Delete()
        {
            String name = Name;
            if (String.IsNullOrEmpty(name))
            {
                var entity = FindByID(ID);
                if (entity != null) name = entity.Name;
            }
            WriteLog("删除", name);

            return base.Delete();
        }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找角色</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.Find(_.ID, id);
        }

        /// <summary>根据名称查找角色</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindByName(String name)
        {
            if (String.IsNullOrEmpty(name) || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.Find(_.Name, name);
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public abstract Boolean Acquire(Int32 menuID, PermissionFlags flag);

        /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        List<IMenu> IRole.GetMySubMenus(Int32 parentID) { return GetMySubMenusInternal(parentID); }

        /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        internal protected abstract List<IMenu> GetMySubMenusInternal(Int32 parentID);

        /// <summary>当前角色拥有的权限</summary>
        public abstract List<IRoleMenu> RoleMenus { get; }

        /// <summary>当前角色拥有的菜单</summary>
        List<IMenu> IRole.Menus { get { return MenusInternal; } }

        /// <summary>当前角色拥有的菜单</summary>
        internal protected abstract List<IMenu> MenusInternal { get; }

        /// <summary>从另一个角色上复制权限</summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual Int32 CopyRoleMenuFrom(IRole role)
        {
            var rms = role.RoleMenus;
            if (rms == null || rms.Count < 1) return 0;

            var myrms = RoleMenus;

            var n = 0;
            foreach (var item in rms)
            {
                var rm = myrms.FirstOrDefault(r => r.MenuID == item.MenuID);
                if (rm == null)
                {
                    rm = (item as IEntity).CloneEntity() as IRoleMenu;
                    rm.ID = 0;
                    rm.RoleID = this.ID;
                }
                else
                    rm.Permission = item.Permission;
                rm.Save();

                n++;
            }
            return n;
        }
        #endregion
    }

    public partial interface IRole
    {
        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Acquire(Int32 menuID, PermissionFlags flag);

        /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        List<IMenu> GetMySubMenus(Int32 parentID);

        /// <summary>当前角色拥有的权限</summary>
        List<IRoleMenu> RoleMenus { get; }

        /// <summary>当前角色拥有的菜单</summary>
        List<IMenu> Menus { get; }

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();

        /// <summary>从另一个角色上复制权限</summary>
        /// <param name="role"></param>
        /// <returns></returns>
        Int32 CopyRoleMenuFrom(IRole role);
    }
}