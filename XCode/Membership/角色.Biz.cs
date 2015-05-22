using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;

namespace XCode.Membership
{
    /// <summary>操作权限</summary>
    [Flags]
    [Description("操作权限")]
    public enum PermissionFlags
    {
        /// <summary>无权限</summary>
        [Description("无权限")]
        None = 0,

        /// <summary>查看权限</summary>
        [Description("查看")]
        Detail = 1,

        /// <summary>添加权限</summary>
        [Description("添加")]
        Insert = 2,

        /// <summary>修改权限</summary>
        [Description("修改")]
        Update = 4,

        /// <summary>删除权限</summary>
        [Description("删除")]
        Delete = 8,

        /// <summary>所有权限</summary>
        [Description("所有")]
        All = 0xFF,
    }

    /// <summary>角色</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Role : Role<Role> { }

    /// <summary>角色</summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract partial class Role<TEntity> : LogEntity<TEntity>
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
        internal protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0)
            {
                // 必须有至少一个可用的系统角色
                //var list = Meta.Cache.Entities.ToList();
                // InitData中用缓存将会导致二次调用InitData，从而有一定几率死锁
                var list = FindAll().ToList();
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

            Add("管理员", true);
            Add("高级用户", true);
            Add("普通用户", true);

            if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}角色数据！", typeof(TEntity).Name);

            CheckRole();
        }

        /// <summary>初始化时执行必要的权限检查，以防万一管理员无法操作</summary>
        static void CheckRole()
        {
            //var rs = Meta.Cache.Entities;
            // InitData中用缓存将会导致二次调用InitData，从而有一定几率死锁
            var rs = FindAll();
            var list = rs.ToList();

            // 如果某些菜单已经被删除，但是角色权限表仍然存在，则删除
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

            var sys = list.LastOrDefault(e => e.IsSystem);
            if (sys == null) return;

            // 如果没有任何角色拥有权限管理的权限，那是很悲催的事情
            var count = 0;
            var nes = eop.EntityType.GetValue("Necessaries", false) as Int32[];
            foreach (var item in nes)
            {
                if (!list.Any(e => e.Has(item, PermissionFlags.Detail)))
                {
                    count++;
                    sys.Set(item, PermissionFlags.All);
                }
            }
            if (count > 0)
            {
                XTrace.WriteLine("共有{0}个必要菜单，没有任何角色拥有权限，准备授权第一系统角色[{1}]拥有其完全管理权！", count, sys);
                sys.Save();

                // 更新缓存
                Meta.Cache.Clear("CheckRole");
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

            WriteLog("删除", this);

            return base.Delete();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override int Save()
        {
            // 先处理一次，否则可能因为别的字段没有修改而没有脏数据
            SavePermission();

            return base.Save();
        }

        /// <summary>加载权限字典</summary>
        internal protected override void OnLoad()
        {
            base.OnLoad();

            // 构造权限字典
            LoadPermission();
        }

        /// <summary>如果Permission被修改，则重新加载</summary>
        /// <param name="fieldName"></param>
        protected override void OnPropertyChanged(string fieldName)
        {
            base.OnPropertyChanged(fieldName);

            if (fieldName == __.Permission) LoadPermission();
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
            if (flag == PermissionFlags.None) return true;

            return pf.Has(flag);
        }

        void Remove(Int32 resid)
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
                if (flag != PermissionFlags.None)
                    Permissions.Add(resid, flag);
            }
            else
            {
                Permissions[resid] = pf | flag;
            }
        }

        /// <summary>重置该角色指定的权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        public void Reset(Int32 resid, PermissionFlags flag)
        {
            var pf = PermissionFlags.None;
            if (Permissions.TryGetValue(resid, out pf))
            {
                Permissions[resid] = pf & ~flag;
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
                //Permission = null;
                SetItem(__.Permission, null);
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
            SetItem(__.Permission, sb.ToString());
        }

        /// <summary>当前角色拥有的资源</summary>
        public Int32[] Resources { get { return Permissions.Keys.ToArray(); } }
        #endregion

        #region 业务
        /// <summary>根据名称查找角色，若不存在则创建</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IRole IRole.FindOrCreateByName(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return Add(name, false);
        }

        /// <summary>添加角色，如果存在，则直接返回，否则创建</summary>
        /// <param name="name"></param>
        /// <param name="issys"></param>
        /// <returns></returns>
        public static TEntity Add(String name, Boolean issys)
        {
            var entity = FindByName(name);
            if (entity != null) return entity;

            entity = new TEntity();
            entity.Name = name;
            entity.IsSystem = issys;
            entity.Save();

            return entity;
        }
        #endregion

        #region 前端页面
        /// <summary>绑定权限项列表时，二次绑定权限子项</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="role"></param>
        /// <param name="isfull"></param>
        public static void RowDataBound(object sender, GridViewRowEventArgs e, IRole role, Boolean isfull)
        {
            if (e.Row == null) return;

            // 当前菜单项
            var menu = e.Row.DataItem as IMenu;
            if (menu == null) return;

            var cb = e.Row.FindControl("CheckBox1") as CheckBox;
            var cblist = e.Row.FindControl("CheckBoxList1") as CheckBoxList;

            // 检查权限
            var pf = role.Get(menu.ID);

            //cb.Checked = role.Permissions.ContainsKey(menu.ID);
            cb.Checked = role.Has(menu.ID);
            cb.ToolTip = pf.ToString();

            // 如果有子节点，则不显示
            if (menu.Childs != null && menu.Childs.Count > 0)
            {
                //cb.Visible = false;
                cblist.Visible = false;
                return;
            }

            // 检查权限
            var flags = EnumHelper.GetDescriptions<PermissionFlags>();
            cblist.Items.Clear();
            foreach (var item in flags.Keys)
            {
                if (item == PermissionFlags.None) continue;
                if (!isfull && item > PermissionFlags.Delete) continue;

                var li = new ListItem(flags[item], ((Int32)item).ToString());
                if ((pf & item) == item) li.Selected = true;
                cblist.Items.Add(li);
            }
        }

        /// <summary>修改单个功能项权限时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="role"></param>
        /// <param name="resname">当前页权限名称</param>
        /// <returns></returns>
        public static Boolean CheckedChanged(object sender, EventArgs e, IRole role, String resname)
        {
            var cb = sender as CheckBox;
            if (cb == null) return false;

            var row = cb.BindingContainer as GridViewRow;
            if (row == null) return false;

            //var provider = ManageProvider.GetFactory<IMenu>() as IMenuFactory;
            var menuid = (Int32)(row.NamingContainer as GridView).DataKeys[row.DataItemIndex].Value;
            var menu = ManageProvider.Menu.FindByID(menuid);
            if (menu == null) return false;
            var res = ManageProvider.Menu.Root.FindByPath(resname);

            //var Manager = cb.Page.GetValue("Manager") as IManagePage;
            var user = ManageProvider.User as IUser;

            // 检查权限
            var pf = role.Get(menu.ID);

            if (cb.Checked)
            {
                // 没有权限，增加
                if (pf == PermissionFlags.None)
                {
                    if (!user.Role.Has(res.ID, PermissionFlags.Insert))
                    {
                        WebHelper.Alert("没有添加权限！");
                        return false;
                    }

                    role.Set(menu.ID, PermissionFlags.Detail);

                    // 如果父级没有授权，则授权
                    CheckAndAddParent(role, menu);

                    role.Save();
                }
            }
            else
            {
                // 如果有权限，删除
                if (pf != PermissionFlags.None)
                {
                    if (!user.Role.Has(res.ID, PermissionFlags.Delete))
                    {
                        WebHelper.Alert("没有删除权限！");
                        return false;
                    }

                    role.Permissions.Remove(menu.ID);
                    role.Save();
                }
            }

            return true;
        }

        /// <summary>选择改变时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="role"></param>
        /// <param name="resname">当前页权限名称</param>
        /// <returns></returns>
        public static Boolean SelectedIndexChanged(object sender, EventArgs e, IRole role, String resname)
        {
            var cb = sender as CheckBoxList;

            //只需判断cb是否为空，该角色只有“查看”权限时cb.SelectedItem为空。
            //if (cb == null || cb.SelectedItem == null) return;
            if (cb == null) return false;

            var row = cb.BindingContainer as GridViewRow;
            if (row == null) return false;

            //var provider = ManageProvider.GetFactory<IMenu>() as IMenuFactory;
            var menuid = (Int32)(row.NamingContainer as GridView).DataKeys[row.DataItemIndex].Value;
            var menu = ManageProvider.Menu.FindByID(menuid);
            if (menu == null) return false;
            var res = ManageProvider.Menu.Root.FindByPath(resname);

            //var Manager = cb.Page.GetValue("Manager") as IManagePage;
            var user = ManageProvider.User as IUser;

            var pf = role.Get(menu.ID);

            // 没有权限，增加
            if (pf == PermissionFlags.None)
            {
                if (!user.Role.Has(res.ID, PermissionFlags.Insert))
                {
                    WebHelper.Alert("没有添加权限！");
                    return false;
                }

                role.Set(menu.ID, PermissionFlags.None);
            }

            // 遍历权限项
            var flag = PermissionFlags.None;
            foreach (ListItem item in cb.Items)
            {
                if (item.Selected) flag |= (PermissionFlags)(Int32.Parse(item.Value));
            }

            if (pf != flag)
            {
                if (!user.Role.Has(res.ID, PermissionFlags.Update))
                {
                    WebHelper.Alert("没有编辑权限！");
                    return false;
                }

                //role.Permissions[menu.ID] = flag;
                role.Permissions.Remove(menu.ID);
                role.Set(menu.ID, flag);

                // 如果父级没有授权，则授权
                CheckAndAddParent(role, menu);
            }
            role.Save();

            return true;
        }

        static void CheckAndAddParent(IRole role, IMenu menu)
        {
            // 如果父级没有授权，则授权
            while ((menu = menu.Parent) != null && menu.ID != 0)
            {
                role.Set(menu.ID, PermissionFlags.Detail);
            }
        }
        #endregion
    }

    public partial interface IRole
    {
        /// <summary>本角色权限集合</summary>
        Dictionary<Int32, PermissionFlags> Permissions { get; }

        /// <summary>是否拥有指定资源的指定权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Has(Int32 resid, PermissionFlags flag = PermissionFlags.None);

        /// <summary>获取权限</summary>
        /// <param name="resid"></param>
        /// <returns></returns>
        PermissionFlags Get(Int32 resid);

        /// <summary>设置该角色拥有指定资源的指定权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        void Set(Int32 resid, PermissionFlags flag = PermissionFlags.Detail);

        /// <summary>重置该角色指定的权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        void Reset(Int32 resid, PermissionFlags flag);

        /// <summary>当前角色拥有的资源</summary>
        Int32[] Resources { get; }

        /// <summary>根据编号查找角色</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IRole FindByID(Int32 id);

        /// <summary>根据名称查找角色，若不存在则创建</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IRole FindOrCreateByName(String name);

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }
}