using System;
using NewLife.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using NewLife.Log;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>操作权限</summary>
    [Flags]
    [Description("操作权限")]
    public enum PermissionFlags
    {
        /// <summary>无权限</summary>
        [Description("无")]
        None = 0,

        /// <summary>所有权限</summary>
        [Description("所有")]
        All = 1,

        /// <summary>添加权限</summary>
        [Description("添加")]
        Insert = 2,

        /// <summary>修改权限</summary>
        [Description("修改")]
        Update = 4,

        /// <summary>删除权限</summary>
        [Description("删除")]
        Delete = 8,
    }

    ///// <summary>角色</summary>
    //public partial class Role<TEntity, TMenuEntity> : Role<TEntity>
    //    where TEntity : Role<TEntity, TMenuEntity>, new()
    //    where TMenuEntity : Menu<TMenuEntity>, new()
    //{
    //    #region 对象操作
    //    ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
    //    //[EditorBrowsable(EditorBrowsableState.Never)]
    //    //protected override void InitData()
    //    //{
    //    //    base.InitData();

    //    //    // 如果角色菜单对应关系为空或者只有一个，则授权第一个角色访问所有菜单
    //    //    if (RoleMenu<TRoleMenuEntity>.Meta.Count > 0)
    //    //    {
    //    //        if (XTrace.Debug) XTrace.WriteLine("如果某一个菜单对应的RoleMenu（角色菜单对应关系）为空或者只有一个，则授权第一个角色访问所有菜单！");

    //    //        foreach (var item in Menu<TMenuEntity>.Root.AllChilds)
    //    //        {
    //    //            RoleMenu<TRoleMenuEntity>.CheckNonePerssion(item.ID);
    //    //        }

    //    //        ClearRoleMenu();
    //    //        return;
    //    //    }

    //    //    Menu<TMenuEntity>.Meta.Session.WaitForInitData(10000);
    //    //    var ms = Menu<TMenuEntity>.Meta.Cache.Entities;

    //    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}授权数据……", typeof(TRoleMenuEntity).Name);

    //    //    using (var trans = new EntityTransaction<TEntity>())
    //    //    {
    //    //        Int32 id = 1;
    //    //        var rs = Meta.Cache.Entities;
    //    //        if (rs != null && rs.Count > 0)
    //    //        {
    //    //            id = rs[0].ID;
    //    //        }

    //    //        // 授权访问所有菜单
    //    //        if (ms != null && ms.Count > 0) RoleMenu<TRoleMenuEntity>.GrantAll(id, ms.GetItem<Int32>("ID"));

    //    //        trans.Commit();
    //    //        if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}授权数据！", typeof(TRoleMenuEntity).Name);
    //    //    }
    //    //}

    //    ///// <summary>删除RoleMenu中无效的RoleID和无效的MenuID</summary>
    //    //public static void ClearRoleMenu()
    //    //{
    //    //    // 等待RoleMenu初始化完成
    //    //    Int32 count = RoleMenu<TRoleMenuEntity>.Meta.Count;
    //    //    count = Menu<TMenuEntity>.Meta.Count;

    //    //    //// 统计所有RoleID和MenuID
    //    //    //var list1 = Meta.Cache.Entities.ToList();
    //    //    //var list2 = Menu<TMenuEntity>.Meta.Cache.Entities.ToList();

    //    //    //var exp = new WhereExpression();
    //    //    //exp &= RoleMenu<TRoleMenuEntity>._.RoleID.NotIn(list1.Select(e => e.ID));
    //    //    //exp |= RoleMenu<TRoleMenuEntity>._.MenuID.NotIn(list2.Select(e => e.ID));

    //    //    // 查询所有。之所以不是调用Delete删除，是为了引发RoleMenu里面的Delete写日志
    //    //    var rms = RoleMenu<TRoleMenuEntity>.FindAllInvalid(FindSQLWithKey(), Menu<TMenuEntity>.FindSQLWithKey());
    //    //    if (rms == null || rms.Count < 1) return;

    //    //    if (XTrace.Debug) XTrace.WriteLine("删除RoleMenu中无效的RoleID和无效的MenuID！");

    //    //    rms.Delete();
    //    //}

    //    ///// <summary>已重载。关联删除权限项。</summary>
    //    ///// <returns></returns>
    //    //protected override int OnDelete()
    //    //{
    //    //    if (Menus != null) Menus.Delete();
    //    //    return base.OnDelete();
    //    //}
    //    #endregion

    //    #region 扩展属性
    //    ///// <summary>菜单</summary>
    //    //[XmlIgnore]
    //    //public virtual EntityList<TRoleMenuEntity> Menus
    //    //{
    //    //    get { return Extends.GetExtend<TRoleMenuEntity, EntityList<TRoleMenuEntity>>("Menus", e => RoleMenu<TRoleMenuEntity>.FindAllByRoleID(ID), false); }
    //    //    set { Extends["Menus"] = value; }
    //    //}

    //    /// <summary>拥有权限的菜单</summary>
    //    [XmlIgnore]
    //    public virtual EntityList<TMenuEntity> MenuList
    //    {
    //        get
    //        {
    //            return Extends.GetExtend<TMenuEntity, EntityList<TMenuEntity>>("MenuList", m =>
    //            {
    //                var list = EntityList<TMenuEntity>.From<TRoleMenuEntity>(Menus, item => Menu<TMenuEntity>.FindByID(item.MenuID));
    //                // 先按Sort降序，再按ID升序，的确更加完善
    //                if (list != null) list.Sort(new String[] { Menu<TMenuEntity>._.Sort, Menu<TMenuEntity>._.ID }, new bool[] { true, false });
    //                return list;

    //                // 新代码很不稳定，先用旧的
    //                //if (Menus == null || Menus.Count < 1) return null;

    //                //var list = Menus.ToList()
    //                //    .Select(e => Menu<TMenuEntity>.FindByID(e.MenuID))
    //                //    .OrderByDescending(e => e.Sort)
    //                //    .ThenBy(e => e.ID);
    //                //return new EntityList<TMenuEntity>(list);
    //            }, false);
    //        }
    //        set { Extends["MenuList"] = value; }
    //    }
    //    #endregion

    //    #region 业务
    //    /// <summary>申请指定菜单指定操作的权限</summary>
    //    /// <param name="menuID"></param>
    //    /// <param name="flag"></param>
    //    /// <returns></returns>
    //    public override Boolean Acquire(Int32 menuID, PermissionFlags flag)
    //    {
    //        if (menuID <= 0 || MenuList == null || MenuList.Count < 1) return false;

    //        // 找到菜单。自下而上递归查找，任意一级没有权限即视为无权限
    //        Int32 id = menuID;
    //        // 避免可能的死循环
    //        var list = new List<Int32>();
    //        while (id > 0)
    //        {
    //            var entity = MenuList.Find(Menu<TMenuEntity>._.ID, id);
    //            if (entity == null) return false;

    //            if (entity.Parent == null) break;

    //            id = entity.ParentID;
    //            if (list.Contains(id)) return false;
    //            list.Add(id);
    //        }

    //        // 申请权限
    //        if (flag == PermissionFlags.None) return true;

    //        TRoleMenuEntity rm = Menus.Find(RoleMenu<TRoleMenuEntity>._.MenuID, menuID);
    //        if (rm == null) return false;

    //        return rm.Acquire(flag);
    //    }

    //    /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
    //    /// <param name="parentID"></param>
    //    /// <returns></returns>
    //    public EntityList<TMenuEntity> GetMySubMenus(Int32 parentID)
    //    {
    //        var list = MenuList;
    //        if (list == null || list.Count < 1) return null;

    //        list = list.FindAll(Menu<TMenuEntity>._.ParentID, parentID);
    //        if (list == null || list.Count < 1) return null;
    //        list = list.FindAll(Menu<TMenuEntity>._.IsShow, true);
    //        if (list == null || list.Count < 1) return null;

    //        return list;
    //    }

    //    /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
    //    /// <param name="parentID"></param>
    //    /// <returns></returns>
    //    protected internal override List<IMenu> GetMySubMenusInternal(int parentID)
    //    {
    //        var list = GetMySubMenus(parentID);
    //        if (list == null || list.Count < 1) return null;

    //        return list.Cast<IMenu>().ToList();
    //    }

    //    ///// <summary>当前角色拥有的权限</summary>
    //    //public override List<IRoleMenu> RoleMenus { get { return Menus.ToList().Cast<IRoleMenu>().ToList(); } }

    //    /// <summary>当前角色拥有的菜单</summary>
    //    internal protected override List<IMenu> MenusInternal { get { return MenuList.ToList().Cast<IMenu>().ToList(); } }
    //    #endregion
    //}

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

            if (Meta.Count > 0)
            {
                // 必须有至少一个可用的系统角色
                var list = Meta.Cache.Entities.ToList();
                if (list.Count > 0 && !list.Any(e => e.IsSystem))
                {
                    // 如果没有，让第一个角色作为系统角色
                    var role = list[0];
                    role.IsSystem = true;

                    XTrace.WriteLine("必须有至少一个可用的系统角色，修改{0}为系统角色！", role.Name);

                    role.Save();
                }

                CheckRole();

                return;
            }

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}角色数据……", typeof(TEntity).Name);

            var entity = new TEntity();
            entity.Name = "管理员";
            entity.IsSystem = true;
            entity.Save();

            if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}角色数据！", typeof(TEntity).Name);

            CheckRole();
        }

        /// <summary>初始化时执行必要的权限检查，以防万一管理员无法操作</summary>
        static void CheckRole()
        {
            var rs = Meta.Cache.Entities;
            if (rs.Count <= 0) return;
            var list = rs.ToList();

            // 如果某些菜单已经被删除，但是角色权限表仍然存在，则删除
            var factory = ManageProvider.Get<IMenu>();
            var eop = ManageProvider.GetFactory<IMenu>();
            var ids = eop.FindAllWithCache().GetItem<Int32>("ID").ToArray();
            foreach (var role in rs)
            {
                if (!role.CheckValid(ids))
                {
                    XTrace.WriteLine("删除[{0}]中的无效资源权限！", role);
                    role.Save();
                }
            }

            var sys = list.FirstOrDefault(e => e.IsSystem);
            if (sys == null) return;

            // 如果没有任何角色拥有权限管理的权限，那是很悲催的事情
            var count = 0;
            var nes = factory.GetType().GetValue("Necessaries", false) as Int32[];
            foreach (var item in nes)
            {
                if (!list.Any(e => e.Has(item, PermissionFlags.All)))
                {
                    count++;
                    sys.Set(item, PermissionFlags.All);
                }
            }
            if (count > 0)
            {
                XTrace.WriteLine("共有{0}个必要菜单，没有任何角色拥有权限，准备授权第一系统角色[{1}]拥有其完全管理权！", count, sys);
                sys.Save();
            }
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否新数据</param>
        public override void Valid(bool isNew)
        {
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(__.Name, _.Name.DisplayName + "不能为空！");

            base.Valid(isNew);

            SavePermission();
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
            var entity = this;
            var name = entity.Name;
            if (String.IsNullOrEmpty(name))
            {
                entity = FindByID(ID);
                if (entity != null) name = entity.Name;
            }

            if (Meta.Count <= 1 && FindCount() <= 1)
            {
                var msg = String.Format("至少保留一个角色[{0}]禁止删除！", name);
                WriteLog("删除", msg);

                throw new XException(msg);
            }

            if (entity.IsSystem)
            {
                var msg = String.Format("系统角色[{0}]禁止删除！", name);
                WriteLog("删除", msg);

                throw new XException(msg);
            }

            WriteLog("删除", name);

            return base.Delete();
        }

        /// <summary>加载权限字典</summary>
        protected override void OnLoad()
        {
            base.OnLoad();

            // 构造权限字典
            LoadPermission();
        }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找角色</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.Find(__.ID, id);
        }

        /// <summary>根据编号查找角色</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IRole IRole.FindByID(Int32 id) { return FindByID(id); }

        /// <summary>根据名称查找角色</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByName(String name)
        {
            if (String.IsNullOrEmpty(name) || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.Find(__.Name, name);
        }
        #endregion

        #region 扩展权限
        private Dictionary<Int32, PermissionFlags> _Permissions = new Dictionary<Int32, PermissionFlags>();
        /// <summary>本角色权限集合</summary>
        public Dictionary<Int32, PermissionFlags> Permissions { get { return _Permissions; } set { _Permissions = value; } }

        /// <summary>是否拥有指定资源的指定权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Boolean Has(Int32 resid, PermissionFlags flag = PermissionFlags.None)
        {
            var pf = PermissionFlags.None;
            if (!Permissions.TryGetValue(resid, out pf)) return false;

            return pf.Has(flag);
        }

        /// <summary>删除指定资源的权限</summary>
        /// <param name="resid"></param>
        public void Remove(Int32 resid)
        {
            if (Permissions.ContainsKey(resid)) Permissions.Remove(resid);
        }

        /// <summary>获取权限</summary>
        /// <param name="resid"></param>
        /// <returns></returns>
        public PermissionFlags Get(Int32 resid)
        {
            PermissionFlags pf;
            if (!Permissions.TryGetValue(resid, out pf)) return PermissionFlags.None;

            return pf;
        }

        /// <summary>设置该角色拥有指定资源的指定权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        public void Set(Int32 resid, PermissionFlags flag = PermissionFlags.All)
        {
            var pf = PermissionFlags.None;
            if (!Permissions.TryGetValue(resid, out pf))
            {
                Permissions.Add(resid, flag);
            }
            else
            {
                Permissions[resid] = pf | flag;
            }
        }

        /// <summary>检查是否有无效权限项，有则删除</summary>
        /// <param name="resids"></param>
        internal Boolean CheckValid(Int32[] resids)
        {
            if (resids == null || resids.Length == 0) return true;

            var count = Permissions.Count;

            var list = new List<Int32>();
            foreach (var item in Permissions)
            {
                if (!resids.Contains(item.Key)) list.Add(item.Key);
            }
            // 删除无效项
            foreach (var item in list)
            {
                Permissions.Remove(item);
            }

            return count == Permissions.Count;
        }

        void LoadPermission()
        {
            Permissions.Clear();
            if (String.IsNullOrEmpty(Permission)) return;

            var dic = Permission.SplitAsDictionary("#", ",");
            foreach (var item in dic)
            {
                var resid = item.Key.ToInt();
                Permissions[resid] = (PermissionFlags)item.Value.ToInt();
            }
        }

        void SavePermission()
        {
            // 不能这样子直接清空，因为可能没有任何改变，而这么做会两次改变脏数据，让系统以为有改变
            //Permission = null;
            if (Permissions.Count <= 0)
            {
                Permission = null;
                return;
            }

            var sb = new StringBuilder();
            // 根据资源按照从小到大排序一下
            foreach (var item in Permissions.OrderBy(e => e.Key))
            {
                //// 跳过None
                //if (item.Value == PermissionFlags.None) continue;
                // 不要跳过None，因为None表示只读

                if (sb.Length > 0) sb.Append(",");
                sb.AppendFormat("{0}#{1}", item.Key, (Int32)item.Value);
            }
            Permission = sb.ToString();
        }

        ///// <summary>设置资源权限</summary>
        ///// <param name="resid"></param>
        ///// <param name="ps"></param>
        //public void SetPermission(Int32 resid, PermissionFlags ps)
        //{
        //    if (ps == PermissionFlags.None)
        //    {
        //        Permissions.Remove(resid);
        //    }
        //    else
        //    {
        //        Permissions[resid] = ps;
        //    }
        //}

        /// <summary>当前角色拥有的资源</summary>
        public Int32[] Resources { get { return Permissions.Keys.ToArray(); } }
        #endregion

        #region 业务
        ///// <summary>申请指定菜单指定操作的权限</summary>
        ///// <param name="menuID"></param>
        ///// <param name="flag"></param>
        ///// <returns></returns>
        //public abstract Boolean Acquire(Int32 menuID, PermissionFlags flag);

        ///// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        ///// <param name="parentID"></param>
        ///// <returns></returns>
        //List<IMenu> IRole.GetMySubMenus(Int32 parentID) { return GetMySubMenusInternal(parentID); }

        ///// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        ///// <param name="parentID"></param>
        ///// <returns></returns>
        //internal protected abstract List<IMenu> GetMySubMenusInternal(Int32 parentID);

        ///// <summary>当前角色拥有的权限</summary>
        //public abstract List<IRoleMenu> RoleMenus { get; }

        ///// <summary>当前角色拥有的菜单</summary>
        //List<IMenu> IRole.Menus { get { return MenusInternal; } }

        ///// <summary>当前角色拥有的菜单</summary>
        //internal protected abstract List<IMenu> MenusInternal { get; }

        ///// <summary>从另一个角色上复制权限</summary>
        ///// <param name="role"></param>
        ///// <returns></returns>
        //public virtual Int32 CopyRoleMenuFrom(IRole role)
        //{
        //    var rms = role.RoleMenus;
        //    if (rms == null || rms.Count < 1) return 0;

        //    var myrms = RoleMenus;

        //    var n = 0;
        //    foreach (var item in rms)
        //    {
        //        var rm = myrms.FirstOrDefault(r => r.MenuID == item.MenuID);
        //        if (rm == null)
        //        {
        //            rm = (item as IEntity).CloneEntity() as IRoleMenu;
        //            rm.ID = 0;
        //            rm.RoleID = this.ID;
        //        }
        //        else
        //            rm.Permission = item.Permission;
        //        rm.Save();

        //        n++;
        //    }
        //    return n;
        //}
        #endregion
    }

    public partial interface IRole
    {
        ///// <summary>申请指定菜单指定操作的权限</summary>
        ///// <param name="menuID"></param>
        ///// <param name="flag"></param>
        ///// <returns></returns>
        //Boolean Acquire(Int32 menuID, PermissionFlags flag);

        ///// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        ///// <param name="parentID"></param>
        ///// <returns></returns>
        //List<IMenu> GetMySubMenus(Int32 parentID);

        ///// <summary>当前角色拥有的权限</summary>
        //List<IRoleMenu> RoleMenus { get; }

        ///// <summary>当前角色拥有的菜单</summary>
        //List<IMenu> Menus { get; }

        /// <summary>当前角色拥有的资源</summary>
        Int32[] Resources { get; }

        /// <summary>根据编号查找角色</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IRole FindByID(Int32 id);

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();

        ///// <summary>从另一个角色上复制权限</summary>
        ///// <param name="role"></param>
        ///// <returns></returns>
        //Int32 CopyRoleMenuFrom(IRole role);
    }
}