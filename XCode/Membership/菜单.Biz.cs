using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Collections;
using NewLife.Threading;

namespace XCode.Membership
{
    /// <summary>菜单</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Menu : Menu<Menu> { }

    /// <summary>菜单</summary>
    public partial class Menu<TEntity> : EntityTree<TEntity>, IMenu where TEntity : Menu<TEntity>, new()
    {
        #region 对象操作
        static Menu()
        {
            var entity = new TEntity();

            EntityFactory.Register(typeof(TEntity), new MenuFactory());

            ObjectContainer.Current.AutoRegister<IMenuFactory, MenuFactory>();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否新数据</param>
        public override void Valid(Boolean isNew)
        {
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(__.Name, _.Name.DisplayName + "不能为空！");

            base.Valid(isNew);

            SavePermission();
        }

        /// <summary>已重载。调用Save时写日志，而调用Insert和Update时不写日志</summary>
        /// <returns></returns>
        public override Int32 Save()
        {
            // 先处理一次，否则可能因为别的字段没有修改而没有脏数据
            SavePermission();
            if (Icon.IsNullOrWhiteSpace()) Icon = "&#xe63f;";
            // 更改日志保存顺序，先保存才能获取到id
            var action = "添加";
            var isNew = IsNullKey;
            if (!isNew)
            {
                // 没有修改时不写日志
                if (!HasDirty) return 0;

                action = "修改";

                // 必须提前写修改日志，否则修改后脏数据失效，保存的日志为空
                LogProvider.Provider.WriteLog(action, this);
            }

            var result = base.Save();

            if (isNew) LogProvider.Provider.WriteLog(action, this);

            return result;
        }

        /// <summary>删除。</summary>
        /// <returns></returns>
        protected override Int32 OnDelete()
        {
            LogProvider.Provider.WriteLog("删除", this);

            // 递归删除子菜单
            var rs = 0;
            using (var ts = Meta.CreateTrans())
            {
                rs += base.OnDelete();

                var ms = Childs;
                if (ms != null && ms.Count > 0)
                {
                    foreach (var item in ms)
                    {
                        rs += item.Delete();
                    }
                }

                ts.Commit();

                return rs;
            }
        }

        /// <summary>加载权限字典</summary>
        protected override void OnLoad()
        {
            base.OnLoad();

            // 构造权限字典
            LoadPermission();
        }

        /// <summary>如果Permission被修改，则重新加载</summary>
        /// <param name="fieldName"></param>
        protected override void OnPropertyChanged(String fieldName)
        {
            base.OnPropertyChanged(fieldName);

            if (fieldName == __.Permission) LoadPermission();
        }
        #endregion

        #region 扩展属性
        /// <summary></summary>
        [XmlIgnore, ScriptIgnore]
        public String Url2 => Url?.Replace("~", "");

        /// <summary>父菜单名</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual String ParentMenuName { get { return Parent?.Name; } set { } }

        /// <summary>必要的菜单。必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
        internal static Int32[] Necessaries
        {
            get
            {
                // 找出所有的必要菜单，如果没有，则表示全部都是必要
                var list = FindAllWithCache();
                var list2 = list.Where(e => e.Necessary).ToList();
                if (list2.Count > 0) list = list2;

                return list.Select(e => e.ID).ToArray();
            }
        }

        /// <summary>友好名称。优先显示名</summary>
        [XmlIgnore, ScriptIgnore]
        public String FriendName => DisplayName.IsNullOrWhiteSpace() ? Name : DisplayName;
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;

            return Meta.Cache.Find(e => e.ID == id);
        }

        /// <summary>根据名字查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByName(String name) => Meta.Cache.Find(e => e.Name.EqualIgnoreCase(name));

        /// <summary>根据全名查找</summary>
        /// <param name="name">全名</param>
        /// <returns></returns>
        public static TEntity FindByFullName(String name) => Meta.Cache.Find(e => e.FullName.EqualIgnoreCase(name));

        /// <summary>根据Url查找</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static TEntity FindByUrl(String url) => Meta.Cache.Find(e => e.Url.EqualIgnoreCase(url));

        /// <summary>根据名字查找，支持路径查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindForName(String name)
        {
            var entity = FindByName(name);
            if (entity != null) return entity;

            return Root.FindByPath(name, _.Name, _.DisplayName);
        }

        /// <summary>查找指定菜单的子菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<TEntity> FindAllByParentID(Int32 id) => Meta.Cache.FindAll(e => e.ParentID == id).OrderByDescending(e => e.Sort).ThenBy(e => e.ID).ToList();

        /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public IList<IMenu> GetSubMenus(Int32[] filters)
        {
            var list = Childs;
            if (list == null || list.Count < 1) return new List<IMenu>();

            list = list.Where(e => e.Visible).ToList();
            if (list == null || list.Count < 1) return new List<IMenu>();

            return list.Where(e => filters.Contains(e.ID)).Cast<IMenu>().ToList();
        }
        #endregion

        #region 扩展操作
        /// <summary>添加子菜单</summary>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        /// <param name="fullName"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public IMenu Add(String name, String displayName, String fullName, String url)
        {
            var entity = new TEntity
            {
                Name = name,
                DisplayName = displayName,
                FullName = fullName,
                Url = url,
                ParentID = ID,
                Parent = this as TEntity,

                Visible = ID == 0 || displayName != null
            };

            entity.Save();

            return entity;
        }
        #endregion

        #region 扩展权限
        /// <summary>可选权限子项</summary>
        [XmlIgnore, ScriptIgnore]
        public Dictionary<Int32, String> Permissions { get; set; } = new Dictionary<Int32, String>();

        void LoadPermission()
        {
            Permissions.Clear();
            if (String.IsNullOrEmpty(Permission)) return;

            var dic = Permission.SplitAsDictionary("#", ",");
            foreach (var item in dic)
            {
                var resid = item.Key.ToInt();
                Permissions[resid] = item.Value;
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

            var sb = Pool.StringBuilder.Get();
            // 根据资源按照从小到大排序一下
            foreach (var item in Permissions.OrderBy(e => e.Key))
            {
                if (sb.Length > 0) sb.Append(",");
                sb.AppendFormat("{0}#{1}", item.Key, item.Value);
            }
            SetItem(__.Permission, sb.Put(true));
        }
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark) => LogProvider.Provider.WriteLog(typeof(TEntity), action, remark);
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var path = GetFullPath(true, "\\", e => e.FriendName);
            if (!path.IsNullOrEmpty()) return path;

            return FriendName;
        }
        #endregion

        #region IMenu 成员
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        String IMenu.GetFullPath(Boolean includeSelf, String separator, Func<IMenu, String> func)
        {
            Func<TEntity, String> d = null;
            if (func != null) d = item => func(item);

            return GetFullPath(includeSelf, separator, d);
        }

        //IMenu IMenu.Add(String name, String displayName, String fullName, String url) => Add(name, displayName, fullName, url);

        /// <summary>父菜单</summary>
        IMenu IMenu.Parent => Parent;

        /// <summary>子菜单</summary>
        IList<IMenu> IMenu.Childs => Childs.OfType<IMenu>().ToList();

        /// <summary>子孙菜单</summary>
        IList<IMenu> IMenu.AllChilds => AllChilds.OfType<IMenu>().ToList();

        /// <summary>根据层次路径查找</summary>
        /// <param name="path">层次路径</param>
        /// <returns></returns>
        IMenu IMenu.FindByPath(String path) => FindByPath(path, _.Name, _.DisplayName);
        #endregion

        #region 菜单工厂
        /// <summary>菜单工厂</summary>
        public class MenuFactory : EntityOperate, IMenuFactory
        {
            #region IMenuFactory 成员
            IMenu IMenuFactory.Root => Root;

            /// <summary>当前请求所在菜单。自动根据当前请求的文件路径定位</summary>
            IMenu IMenuFactory.Current
            {
#if !__CORE__
                get
                {
                    var context = HttpContext.Current;
                    if (context == null) return null;

                    var menu = context.Items["CurrentMenu"] as IMenu;
                    if (menu == null && !context.Items.Contains("CurrentMenu"))
                    {
                        var ss = context.Request.AppRelativeCurrentExecutionFilePath.Split("/");
                        // 默认路由包括区域、控制器、动作，Url有时候会省略动作，再往后的就是参数了，动作和参数不参与菜单匹配
                        var max = ss.Length - 1;
                        if (ss[0] == "~") max++;

                        // 寻找当前所属菜单，路径倒序，从最长Url路径查起
                        for (var i = max; i > 0 && menu == null; i--)
                        {
                            var url = ss.Take(i).Join("/");
                            menu = FindByUrl(url);
                        }

                        context.Items["CurrentMenu"] = menu;
                    }
                    return menu;
                }
                set
                {
                    HttpContext.Current.Items["CurrentMenu"] = value;
                }
#else
                get { return null; }
                set { }
#endif
            }

            /// <summary>根据编号找到菜单</summary>
            /// <param name="id"></param>
            /// <returns></returns>
            IMenu IMenuFactory.FindByID(Int32 id) => FindByID(id);

            /// <summary>根据Url找到菜单</summary>
            /// <param name="url"></param>
            /// <returns></returns>
            IMenu IMenuFactory.FindByUrl(String url) => FindByUrl(url);

            /// <summary>根据全名找到菜单</summary>
            /// <param name="fullName"></param>
            /// <returns></returns>
            IMenu IMenuFactory.FindByFullName(String fullName) => FindByFullName(fullName);

            /// <summary>获取指定菜单下，当前用户有权访问的子菜单。</summary>
            /// <param name="menuid"></param>
            /// <param name="user"></param>
            /// <returns></returns>
            IList<IMenu> IMenuFactory.GetMySubMenus(Int32 menuid, IUser user)
            {
                var factory = this as IMenuFactory;
                var root = factory.Root;

                // 当前用户
                //var user = ManageProvider.Provider.Current as IUser;
                var rs = user?.Roles;
                if (rs == null || rs.Length == 0) return new List<IMenu>();

                IMenu menu = null;

                // 找到菜单
                if (menuid > 0) menu = FindByID(menuid);

                if (menu == null)
                {
                    menu = root;
                    if (menu == null || menu.Childs == null || menu.Childs.Count < 1) return new List<IMenu>();
                }

                return menu.GetSubMenus(rs.SelectMany(e => e.Resources).ToArray());
            }

            /// <summary>扫描命名空间下的控制器并添加为菜单</summary>
            /// <param name="rootName">根菜单名称，所有菜单附属在其下</param>
            /// <param name="asm">要扫描的程序集</param>
            /// <param name="nameSpace">要扫描的命名空间</param>
            /// <returns></returns>
            public virtual IList<IMenu> ScanController(String rootName, Assembly asm, String nameSpace)
            {
                var list = new List<IMenu>();
                var mf = this as IMenuFactory;

                // 所有控制器
                var types = asm.GetTypes().Where(e => e.Name.EndsWith("Controller") && e.Namespace == nameSpace).ToList();
                if (types.Count == 0) return list;

                // 如果根菜单不存在，则添加
                var r = Root as IMenu;
                var root = mf.FindByFullName(nameSpace);
                if (root == null) root = r.FindByPath(rootName);
                //if (root == null) root = r.Childs.FirstOrDefault(e => e.Name.EqualIgnoreCase(rootName));
                //if (root == null) root = r.Childs.FirstOrDefault(e => e.Url.EqualIgnoreCase("~/" + rootName));
                if (root == null)
                {
                    root = r.Add(rootName, null, nameSpace, "~/" + rootName);
                    list.Add(root);
                }
                if (root.FullName != nameSpace)
                {
                    root.FullName = nameSpace;
                    (root as IEntity).Save();
                }

                var ms = new List<IMenu>();

                // 遍历该程序集所有类型
                foreach (var type in types)
                {
                    var name = type.Name.TrimEnd("Controller");
                    var url = root.Url;
                    var node = root;

                    // 添加Controller
                    var controller = node.FindByPath(name);
                    if (controller == null)
                    {
                        url += "/" + name;
                        controller = FindByUrl(url);
                        if (controller == null)
                        {
                            // DisplayName特性作为中文名
                            controller = node.Add(name, type.GetDisplayName(), type.FullName, url);

                            //list.Add(controller);
                        }
                    }
                    if (controller.FullName.IsNullOrEmpty()) controller.FullName = type.FullName;
                    if (controller.Remark.IsNullOrEmpty()) controller.Remark = type.GetDescription();

                    ms.Add(controller);
                    list.Add(controller);

                    // 反射调用控制器的方法来获取动作
                    var func = type.GetMethodEx("ScanActionMenu");
                    if (func == null) continue;

                    var acts = func.As<Func<IMenu, IDictionary<MethodInfo, Int32>>>(type.CreateInstance()).Invoke(controller);
                    if (acts == null || acts.Count == 0) continue;

                    // 可选权限子项
                    controller.Permissions.Clear();

                    // 添加该类型下的所有Action作为可选权限子项
                    foreach (var item in acts)
                    {
                        var method = item.Key;

                        var dn = method.GetDisplayName();
                        if (!dn.IsNullOrEmpty()) dn = dn.Replace("{type}", (controller as TEntity)?.FriendName);

                        var pmName = !dn.IsNullOrEmpty() ? dn : method.Name;
                        if (item.Value <= (Int32)PermissionFlags.Delete) pmName = ((PermissionFlags)item.Value).GetDescription();
                        controller.Permissions[item.Value] = pmName;
                    }

                    // 排序
                    if (controller.Sort == 0)
                    {
                        var pi = type.GetPropertyEx("MenuOrder");
                        if (pi != null) controller.Sort = pi.GetValue(null).ToInt();
                    }
                }

                for (var i = 0; i < ms.Count; i++)
                {
                    (ms[i] as IEntity).Save();
                }

                // 如果新增了菜单，需要检查权限
                if (list.Count > 0)
                {
                    ThreadPoolX.QueueUserWorkItem(() =>
                    {
                        XTrace.WriteLine("新增了菜单，需要检查权限");
                        var eop = ManageProvider.GetFactory<IRole>();
                        eop.EntityType.Invoke("CheckRole");
                    });
                }

                return list;
            }
            #endregion
        }
        #endregion
    }

    /// <summary>菜单工厂接口</summary>
    public interface IMenuFactory
    {
        /// <summary>根菜单</summary>
        IMenu Root { get; }

        /// <summary>当前请求所在菜单。自动根据当前请求的文件路径定位</summary>
        IMenu Current { get; set; }

        /// <summary>根据编号找到菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IMenu FindByID(Int32 id);

        /// <summary>根据全名找到菜单</summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        IMenu FindByFullName(String fullName);

        /// <summary>根据Url找到菜单</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        IMenu FindByUrl(String url);

        /// <summary>获取指定菜单下，当前用户有权访问的子菜单。</summary>
        /// <param name="menuid"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        IList<IMenu> GetMySubMenus(Int32 menuid, IUser user);

        /// <summary>扫描命名空间下的控制器并添加为菜单</summary>
        /// <param name="rootName"></param>
        /// <param name="asm"></param>
        /// <param name="nameSpace"></param>
        /// <returns></returns>
        IList<IMenu> ScanController(String rootName, Assembly asm, String nameSpace);
    }

    public partial interface IMenu
    {
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        String GetFullPath(Boolean includeSelf, String separator, Func<IMenu, String> func);

        /// <summary>添加子菜单</summary>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        /// <param name="fullName"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        IMenu Add(String name, String displayName, String fullName, String url);

        /// <summary>父菜单</summary>
        IMenu Parent { get; }

        /// <summary>子菜单</summary>
        IList<IMenu> Childs { get; }

        /// <summary>子孙菜单</summary>
        IList<IMenu> AllChilds { get; }

        /// <summary>根据层次路径查找。因为需要指定在某个菜单子级查找路径，所以是成员方法而不是静态方法</summary>
        /// <param name="path">层次路径</param>
        /// <returns></returns>
        IMenu FindByPath(String path);

        /// <summary>排序上升</summary>
        void Up();

        /// <summary>排序下降</summary>
        void Down();

        /// <summary></summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        IList<IMenu> GetSubMenus(Int32[] filters);

        /// <summary>可选权限子项</summary>
        Dictionary<Int32, String> Permissions { get; }
    }
}