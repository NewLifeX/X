using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;
using NewLife;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;

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

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}菜单数据……", typeof(TEntity).Name);

            using (var trans = new EntityTransaction<TEntity>())
            {
                // 准备增加Admin目录下的所有页面
                ScanAndAdd();

                trans.Commit();
                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}菜单数据！", typeof(TEntity).Name);
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
            // 先处理一次，否则可能因为别的字段没有修改而没有脏数据
            SavePermission();

            string action = "添加";
            if (!(this as IEntity).IsNullKey)
            {
                if (!HasDirty) return 0;

                action = "修改";
            }

            int result = base.Save();

            WriteLog(action, this);

            return result;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override int Delete()
        {
            WriteLog("删除", this);

            return base.Delete();
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

        #region 扩展属性
        /// <summary>父菜单名</summary>
        [XmlIgnore]
        public virtual String ParentMenuName { get { return Parent == null ? null : Parent.Name; } set { } }

        /// <summary>必要的菜单。必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
        internal static Int32[] Necessaries
        {
            get
            {
                // 找出所有的必要菜单，如果没有，则表示全部都是必要
                var list = FindAllWithCache(__.Necessary, true);
                if (list.Count <= 0) list = Meta.Cache.Entities;

                return list.GetItem<Int32>(__.ID).ToArray();
            }
        }

        /// <summary>友好名称。优先显示名</summary>
        public String FriendName { get { return DisplayName.IsNullOrWhiteSpace() ? Name : DisplayName; } }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;
            return Meta.Cache.Entities.Find(__.ID, id);
        }

        /// <summary>根据名字查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByName(String name) { return Meta.Cache.Entities.Find(__.Name, name); }

        /// <summary>根据Url查找</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static TEntity FindByUrl(String url) { return Meta.Cache.Entities.FindIgnoreCase(__.Url, url); }

        /// <summary>根据名字查找，支持路径查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindForName(String name)
        {
            TEntity entity = FindByName(name);
            if (entity != null) return entity;

            return Root.FindByPath(name, _.Name, _.DisplayName);
        }

        /// <summary>查找指定菜单的子菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EntityList<TEntity> FindAllByParentID(Int32 id)
        {
            var list = Meta.Cache.Entities.FindAll(__.ParentID, id);
            if (list != null && list.Count > 0) list.Sort(new String[] { _.Sort, _.ID }, new Boolean[] { true, false });
            return list;
        }

        /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public IList<IMenu> GetMySubMenus(Int32[] filters)
        {
            var list = Childs;
            if (list == null || list.Count < 1) return new List<IMenu>();

            list = list.FindAll(_.Visible, true);
            if (list == null || list.Count < 1) return new List<IMenu>();

            return list.ToList().Where(e => filters.Contains(e.ID)).Cast<IMenu>().ToList();
        }
        #endregion

        #region 扩展操作
        /// <summary>添加子菜单</summary>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public TEntity Add(String name, String displayName, String url)
        {
            var entity = new TEntity();
            entity.Name = name;
            entity.DisplayName = displayName;
            entity.Url = url;
            entity.ParentID = this.ID;

            entity.Visible = ID == 0 || displayName != null;

            entity.Insert();

            return entity;
        }
        #endregion

        #region 扩展权限
        private Dictionary<Int32, String> _Permissions = new Dictionary<Int32, String>();
        /// <summary>可选权限子项</summary>
        public Dictionary<Int32, String> Permissions { get { return _Permissions; } set { _Permissions = value; } }

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

            var sb = new StringBuilder();
            // 根据资源按照从小到大排序一下
            foreach (var item in Permissions.OrderBy(e => e.Key))
            {
                if (sb.Length > 0) sb.Append(",");
                sb.AppendFormat("{0}#{1}", item.Key, item.Value);
            }
            SetItem(__.Permission, sb.ToString());
        }
        #endregion

        #region 扫描菜单Aspx
        /// <summary>扫描配置文件中指定的目录，仅为兼容旧版本，将来不做功能更新</summary>
        /// <returns></returns>
        public static Int32 ScanAndAdd()
        {
            // 扫描目录
            var appDirs = new List<String>(Config.GetConfigSplit<String>("NewLife.CommonEntity.AppDirs", null));
            // 过滤文件
            var appDirsFileFilter = Config.GetConfigSplit<String>("NewLife.CommonEntity.AppDirsFileFilter", null);
            // 是否在子目中过滤
            var appDirsIsAllFilter = Config.GetConfig<Boolean>("NewLife.CommonEntity.AppDirsIsAllDirs", false);

            var filters = new HashSet<String>(appDirsFileFilter, StringComparer.OrdinalIgnoreCase);

            // 如果不包含Admin，以它开头
            //if (!appDirs.Contains("Admin")) appDirs.Insert(0, "Admin");
            if (appDirs.Count == 0 || appDirs.Count == 1 && appDirs[0] == "Admin")
            {
                var dis = Directory.GetDirectories(".".GetFullPath());
                appDirs.AddRange(dis.Select(e => Path.GetFileName(e)));
            }

            Int32 total = 0;
            foreach (var item in appDirs)
            {
                // 如果目录不存在，就没必要扫描了
                var p = item.GetFullPath();
                if (!Directory.Exists(p))
                {
                    // 有些旧版本系统，会把目录放到Admin目录之下
                    p = "Admin".CombinePath(item);
                    if (!Directory.Exists(p)) continue;
                }

                XTrace.WriteLine("扫描目录生成菜单 {0}", p);

                // 根据目录找菜单，它将作为顶级菜单
                var top = FindForName(item);
                //if (top == null) top = Meta.Cache.Entities.Find(__.DisplayName, item);
                if (top == null)
                {
                    if (!IsBizDir(p)) continue;

                    top = Root.Add(item, null, null);
                    // 内层用到了再保存
                    //top.Save();
                }
                total += ScanAndAdd(p, top, filters, appDirsIsAllFilter);
            }
            XTrace.WriteLine("扫描目录共生成菜单 {0} 个", total);

            return total;
        }

        /// <summary>扫描指定目录并添加文件到顶级菜单之下</summary>
        /// <param name="dir">扫描目录</param>
        /// <param name="parent">父级</param>
        /// <param name="fileFilter">过滤文件名</param>
        /// <param name="isFilterChildDir">是否在子目录中过滤</param>
        /// <returns></returns>
        static Int32 ScanAndAdd(String dir, TEntity parent, ICollection<String> fileFilter = null, Boolean isFilterChildDir = false)
        {
            if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("dir");

            // 要扫描的目录
            var p = dir.GetFullPath();
            if (!Directory.Exists(p)) return 0;

            // 本目录aspx页面
            var fs = Directory.GetFiles(p, "*.aspx", SearchOption.TopDirectoryOnly);
            // 本目录子子录
            var dis = Directory.GetDirectories(p);
            // 如没有页面和子目录
            if ((fs == null || fs.Length < 1) && (dis == null || dis.Length < 1)) return 0;

            // 添加
            var num = 0;

            XTrace.WriteLine("分析菜单 {0} 下的页面 {1} 共有文件{2}个 子目录{3}个", parent.Name, dir, fs.Length, dis.Length);

            //aspx
            if (fs != null && fs.Length > 0)
            {
                var currentPath = GetPathForScan(p);
                foreach (var elm in fs)
                {
                    // 获取页面标题，如果没有标题则认定不是业务页面
                    var title = GetPageTitle(elm);
                    if (title.IsNullOrWhiteSpace()) continue;

                    // 修正上一级添加的菜单信息
                    var file = Path.GetFileName(elm);
                    if (file.EqualIgnoreCase("Default.aspx"))
                    {
                        if (parent.ID == 0) num++;
                        parent.Url = currentPath.CombinePath("Default.aspx");
                        parent.DisplayName = title;
                        parent.Save();
                        continue;
                    }

                    // 过滤特定文件名文件
                    // 采用哈希集合查询字符串更快
                    if (fileFilter != null && fileFilter.Contains(file)) continue;

                    var name = Path.GetFileNameWithoutExtension(elm);
                    // 过滤掉表单页面
                    if (name.EndsWithIgnoreCase("Form")) continue;
                    // 过滤掉选择页面
                    if (name.StartsWithIgnoreCase("Select")) continue;

                    // 全部使用全路径
                    var url = currentPath.CombinePath(file);
                    var entity = FindByUrl(url);
                    if (entity != null) continue;

                    if (parent.ID == 0) { parent.Save(); num++; }
                    entity = parent.Add(name, title, url);

                    num++;
                }
            }

            // 子级目录
            if (dis == null || dis.Length > 0)
            {
                if (!isFilterChildDir) fileFilter = null;
                foreach (var item in dis)
                {
                    if (!IsBizDir(item)) continue;

                    var dirname = Path.GetFileName(item);
                    var menu = parent.Add(dirname, null, null);
                    // 内层用到了再保存
                    //menu.Save(); 
                    //num++;

                    num += ScanAndAdd(item, menu, fileFilter, isFilterChildDir);
                }
            }

            return num;
        }

        /// <summary>获取目录层级</summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        static String GetPathForScan(String dir)
        {
            if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("dir");

            // 要扫描的目录
            var p = dir.GetFullPath();
            if (!Directory.Exists(dir)) return "";

            var dirPath = p.Replace(AppDomain.CurrentDomain.BaseDirectory, null);
            //获取层级
            var ss = dirPath.Split("\\");
            var sb = new StringBuilder();
            for (int i = 0; i < ss.Length; i++)
            {
                sb.Append("../");
            }
            var currentPath = sb.ToString();
            currentPath = currentPath.CombinePath(dirPath);

            return currentPath.Replace("\\", "/").EnsureEnd("/");
        }

        /// <summary>非业务的目录列表</summary>
        static HashSet<String> _NotBizDirs = new HashSet<string>(
            new String[] { 
                "Frame", "Asc", "Ascx", "images", "js", "css", "scripts" ,
                "Bin","App_Code","App_Data","Config","Log"
            },
            StringComparer.OrdinalIgnoreCase);
        /// <summary>是否业务目录</summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        static Boolean IsBizDir(String dir)
        {
            var dirName = new DirectoryInfo(dir).Name;

            if (_NotBizDirs.Contains(dirName)) return false;
            if (dirName.StartsWithIgnoreCase("img")) return false;

            // 判断是否存在aspx文件
            var fs = Directory.GetFiles(dir, "*.aspx", SearchOption.AllDirectories);
            return fs != null && fs.Length > 0;
        }

        static Regex reg_PageTitle = new Regex("\\bTitle=\"([^\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex reg_PageTitle2 = new Regex("<title>([^<]*)</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static String GetPageTitle(String pagefile)
        {
            if (String.IsNullOrEmpty(pagefile) || !".aspx".EqualIgnoreCase(Path.GetExtension(pagefile)) || !File.Exists(pagefile)) return null;

            // 读取aspx的第一行，里面有Title=""
            String line = null;
            using (var reader = new StreamReader(pagefile))
            {
                while (!reader.EndOfStream && line.IsNullOrWhiteSpace()) line = reader.ReadLine();
                // 有时候Title跑到第二第三行去了
                if (!reader.EndOfStream) line += Environment.NewLine + reader.ReadLine();
                if (!reader.EndOfStream) line += Environment.NewLine + reader.ReadLine();
            }
            if (!String.IsNullOrEmpty(line))
            {
                // 正则
                Match m = reg_PageTitle.Match(line);
                if (m != null && m.Success) return m.Groups[1].Value;
            }

            // 第二正则
            String content = File.ReadAllText(pagefile);
            Match m2 = reg_PageTitle2.Match(content);
            if (m2 != null && m2.Success) return m2.Groups[1].Value;

            return null;
        }
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark)
        {
            LogProvider.Provider.WriteLog(typeof(TEntity), action, remark);
        }

        /// <summary>输出实体对象日志</summary>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        protected static void WriteLog(String action, IEntity entity)
        {
            // 构造字段数据的字符串表示形式
            var sb = new StringBuilder();
            foreach (var fi in Meta.Fields)
            {
                if (action == "修改" && !entity.Dirtys[fi.Name]) continue;

                sb.Separate(",").AppendFormat("{0}={1}", fi.Name, entity[fi.Name]);
            }

            WriteLog(action, sb.ToString());
        }
        #endregion

        #region 导入导出
        /// <summary>导出</summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static String Export(IList<IMenu> list)
        {
            return Export(new EntityList<TEntity>(list));
        }

        /// <summary>导出</summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static String Export(EntityList<TEntity> list)
        {
            return list.ToXml();
        }

        /// <summary>导入</summary>
        public virtual void Import()
        {
            using (var trans = new EntityTransaction<TEntity>())
            {
                //顶级节点根据名字合并
                if (ParentID == 0)
                {
                    var m = Find(__.Name, Name);
                    if (m != null)
                    {
                        this.ID = m.ID;
                        this.Name = m.Name;
                        this.DisplayName = m.DisplayName;
                        this.ParentID = 0;
                        this.Url = m.Url;

                        this.Update();
                    }
                    else
                        this.Insert();
                }
                else
                {
                    this.Insert();
                }

                //更新编号
                var list = Childs;
                if (list != null && list.Count > 0)
                {
                    foreach (TEntity item in list)
                    {
                        item.ParentID = ID;

                        item.Import();
                    }
                }

                trans.Commit();
            }
        }

        /// <summary>导入</summary>
        /// <param name="xml"></param>
        public static void Import(String xml)
        {
            var list = new EntityList<TEntity>();
            list.FromXml(xml);
            foreach (var item in list)
            {
                item.Import();
            }
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var path = GetFullPath(true, "\\", e => e.FriendName);
            if (!String.IsNullOrEmpty(path))
                return path;
            else
                return FriendName;
        }
        #endregion

        #region IMenu 成员
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        string IMenu.GetFullPath(bool includeSelf, string separator, Func<IMenu, string> func)
        {
            Func<TEntity, String> d = null;
            if (func != null) d = item => func(item);

            return GetFullPath(includeSelf, separator, d);
        }

        IMenu IMenu.Add(String name, String displayName, String url) { return Add(name, displayName, url); }

        /// <summary>父菜单</summary>
        IMenu IMenu.Parent { get { return Parent; } }

        /// <summary>子菜单</summary>
        IList<IMenu> IMenu.Childs { get { return Childs.OfType<IMenu>().ToList(); } }

        /// <summary>子孙菜单</summary>
        IList<IMenu> IMenu.AllChilds { get { return AllChilds.OfType<IMenu>().ToList(); } }

        /// <summary>根据层次路径查找</summary>
        /// <param name="path">层次路径</param>
        /// <returns></returns>
        IMenu IMenu.FindByPath(String path) { return FindByPath(path, _.Name, _.DisplayName); }
        #endregion

        #region 菜单工厂
        /// <summary>菜单工厂</summary>
        public class MenuFactory : EntityOperate, IMenuFactory
        {
            #region IMenuFactory 成员
            IMenu IMenuFactory.Root { get { return Root; } }

            /// <summary>当前请求所在菜单。自动根据当前请求的文件路径定位</summary>
            IMenu IMenuFactory.Current
            {
                get
                {
                    var context = HttpContext.Current;
                    if (context == null) return null;

                    var menu = context.Items["CurrentMenu"] as IMenu;
                    if (menu == null && !context.Items.Contains("CurrentMenu"))
                    {
                        var ss = context.Request.AppRelativeCurrentExecutionFilePath.Split("/");
                        // 默认路由包括区域、控制器、动作，Url有时候会省略动作，再往后的就是参数了，动作和参数不参与菜单匹配
                        var max = 2;
                        if (ss[0] == "~") max++;
                        var url = ss.Take(max).Join("/");

                        menu = FindByUrl(url);
                        context.Items["CurrentMenu"] = menu;
                    }
                    return menu;
                }
                set { HttpContext.Current.Items["CurrentMenu"] = value; }
            }

            /// <summary>根据编号找到菜单</summary>
            /// <param name="id"></param>
            /// <returns></returns>
            IMenu IMenuFactory.FindByID(Int32 id) { return FindByID(id); }

            /// <summary>根据Url找到菜单</summary>
            /// <param name="url"></param>
            /// <returns></returns>
            IMenu IMenuFactory.FindByUrl(String url) { return FindByUrl(url); }

            /// <summary>获取指定菜单下，当前用户有权访问的子菜单。</summary>
            /// <param name="menuid"></param>
            /// <returns></returns>
            IList<IMenu> IMenuFactory.GetMySubMenus(Int32 menuid)
            {
                var factory = this as IMenuFactory;
                var root = factory.Root;

                // 当前用户
                var admin = ManageProvider.Provider.Current as IUser;
                if (admin == null || admin.Role == null) return new List<IMenu>();

                IMenu menu = null;

                // 找到菜单
                if (menuid > 0) menu = FindByID(menuid);

                if (menu == null)
                {
                    menu = root;
                    if (menu == null || menu.Childs == null || menu.Childs.Count < 1) return new List<IMenu>();
                }

                return menu.GetMySubMenus(admin.Role.Resources);
            }

            /// <summary>扫描命名空间下的控制器并添加为菜单</summary>
            /// <param name="rootName">根菜单名称，所有菜单附属在其下</param>
            /// <param name="asm">要扫描的程序集</param>
            /// <param name="nameSpace">要扫描的命名空间</param>
            /// <returns></returns>
            public virtual IList<IMenu> ScanController(String rootName, Assembly asm, String nameSpace)
            {
                var list = new List<IMenu>();

                // 如果根菜单不存在，则添加
                var root = Root.FindByPath(rootName);
                if (root == null)
                {
                    root = Root.Add(rootName, null, "~/" + rootName);
                    list.Add(root);
                }

                // 遍历该程序集所有类型
                foreach (var type in asm.GetTypes())
                {
                    var name = type.Name;
                    if (!name.EndsWith("Controller")) continue;

                    name = name.TrimEnd("Controller");
                    if (type.Namespace != nameSpace) continue;

                    var url = root.Url;

                    var node = root;

                    // 添加Controller
                    var controller = node.FindByPath(name);
                    if (controller == null)
                    {
                        url += "/" + name;
                        // DisplayName特性作为中文名
                        controller = node.Add(name, type.GetDisplayName(), url);
                        list.Add(node);
                    }

                    // 反射调用控制器的GetActions方法来获取动作
                    var func = type.GetMethodEx("ScanActionMenu");
                    if (func == null) continue;

                    //var acts = type.Invoke(func) as MethodInfo[];
                    var acts = func.As<Func<IMenu, IDictionary<MethodInfo, Int32>>>(type.CreateInstance()).Invoke(controller);
                    if (acts == null || acts.Count == 0) continue;

                    // 可选权限子项
                    controller.Permissions.Clear();
                    var dic = new Dictionary<String, Int32>();
                    var mask = 0;

                    // 添加该类型下的所有Action作为可选权限子项
                    foreach (var item in acts)
                    {
                        var method = item.Key;

                        var dn = method.GetDisplayName();
                        if (!dn.IsNullOrEmpty()) dn = dn.Replace("{type}", controller.FriendName);

                        var pmName = !dn.IsNullOrEmpty() ? dn : method.Name;
                        if (item.Value == 0)
                            dic.Add(pmName, item.Value);
                        else
                        {
                            if (item.Value < 0x10) pmName = ((PermissionFlags)item.Value).GetDescription();
                            mask |= item.Value;
                            controller.Permissions[item.Value] = pmName;
                        }
                    }

                    // 分配权限位
                    var idx = 0x10;
                    foreach (var item in dic)
                    {
                        while ((mask & idx) != 0)
                        {
                            if (idx >= 0x80) throw new XException("控制器{0}的Action过多，不够分配权限位", type.Name);
                            idx <<= 1;
                        }
                        mask |= idx;
                        controller.Permissions[idx] = item.Key;
                    }
                    controller.Save();
                }

                // 如果新增了菜单，需要检查权限
                if (list.Count > 0)
                {
                    var eop = ManageProvider.GetFactory<IRole>();
                    eop.EntityType.Invoke("CheckRole");
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

        /// <summary>根据Url找到菜单</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        IMenu FindByUrl(String url);

        /// <summary>获取指定菜单下，当前用户有权访问的子菜单。</summary>
        /// <param name="menuid"></param>
        /// <returns></returns>
        IList<IMenu> GetMySubMenus(Int32 menuid);

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
        /// <param name="url"></param>
        /// <returns></returns>
        IMenu Add(String name, String displayName, String url);

        /// <summary>父菜单</summary>
        new IMenu Parent { get; }

        /// <summary>子菜单</summary>
        new IList<IMenu> Childs { get; }

        /// <summary>子孙菜单</summary>
        new IList<IMenu> AllChilds { get; }

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
        IList<IMenu> GetMySubMenus(Int32[] filters);

        /// <summary>可选权限子项</summary>
        Dictionary<Int32, String> Permissions { get; }
    }
}