using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using XCode;

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

            //EntityFactory.Register(typeof(TEntity), new MenuFactory<TEntity>());
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

        /// <summary>已重载。调用Save时写日志，而调用Insert和Update时不写日志</summary>
        /// <returns></returns>
        public override int Save()
        {
            if (!String.IsNullOrEmpty(Url))
            {
                // 删除两端空白
                if (Url != Url.Trim()) Url = Url.Trim();
            }

            if (ID == 0)
                WriteLog("添加", Name + " " + Url);
            else if (HasDirty)
                WriteLog("修改", Name + " " + Url);

            return base.Save();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override int Delete()
        {
            var name = Name;
            if (String.IsNullOrEmpty(name))
            {
                var entity = FindByID(ID);
                if (entity != null) name = entity.Name;
            }
            WriteLog("删除", name + " " + Url);

            return base.Delete();
        }

        #endregion

        #region 扩展属性
        /// <summary>父菜单名</summary>
        [XmlIgnore]
        public virtual String ParentMenuName { get { return Parent == null ? null : Parent.Name; } set { } }

        /// <summary>当前页所对应的菜单项</summary>
        public static TEntity Current
        {
            get
            {
                var context = HttpContext.Current;
                if (context == null || context.Request == null) return null;

                var key = "CurrentMenu";
                var entity = context.Items[key] as TEntity;
                if (entity == null)
                {
                    entity = GetCurrentMenu();
                    context.Items[key] = entity;
                }

                return entity;
            }
        }

        static TEntity GetCurrentMenu()
        {
            var context = HttpContext.Current;
            if (context == null || context.Request == null) return null;

            // 计算当前文件路径
            var p = context.Request.PhysicalPath;
            var di = new DirectoryInfo(Path.GetDirectoryName(p));
            var fileName = Path.GetFileName(p);

            // 查找所有以该文件名结尾的菜单
            var list = Meta.Cache.Entities;
            list = list.FindAll(item => !String.IsNullOrEmpty(item.Url) && item.Url.Trim().EndsWithIgnoreCase(fileName));
            if ((list == null || list.Count < 1) && Path.GetFileNameWithoutExtension(p).EndsWithIgnoreCase("Form"))
            {
                fileName = Path.GetFileNameWithoutExtension(p);
                fileName = fileName.Substring(0, fileName.Length - "Form".Length);
                fileName += Path.GetExtension(p);

                // 有可能是表单
                list = Meta.Cache.Entities.FindAll(item => !String.IsNullOrEmpty(item.Url) && item.Url.Trim().EndsWithIgnoreCase(fileName));
            }
            if (list == null || list.Count < 1) return null;
            if (list.Count == 1) return list[0];

            // 查找所有以该文件名结尾的菜单
            var list2 = list.FindAll(e => !String.IsNullOrEmpty(e.Url) && e.Url.Trim().EndsWithIgnoreCase(@"/" + fileName));
            if (list2 == null || list2.Count < 1) return list[0];
            if (list2.Count == 1) return list2[0];

            // 优先全路径
            var url = String.Format(@"../../{0}/{1}/{2}", di.Parent.Name, di.Name, fileName);
            var entity = FindByUrl(url);
            if (entity != null) return entity;

            // 兼容旧版本
            url = String.Format(@"../{0}/{1}", di.Name, fileName);
            return FindByUrl(url);
        }

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

            //return FindByPath(Meta.Cache.Entities, name, _.Name);
            return Root.FindByPath(name, _.Name, _.Permission, _.Remark);
        }

        /// <summary>根据权限名查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByPerssion(String name) { return Meta.Cache.Entities.Find(__.Permission, name); }

        /// <summary>为了权限而查找，支持路径查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindForPerssion(String name)
        {
            // 计算集合，为了处理同名的菜单
            EntityList<TEntity> list = Meta.Cache.Entities.FindAll(__.Permission, name);
            if (list != null && list.Count == 1) return list[0];

            // 如果菜单同名，则使用当前页
            TEntity current = null;
            // 看来以后要把list != null && list.Count > 0判断作为常态，养成好习惯呀
            if (list != null && list.Count > 0)
            {
                if (current == null) current = Current;
                if (current != null)
                {
                    foreach (TEntity item in list)
                    {
                        if (current.ID == item.ID) return item;
                    }
                }

                if (XTrace.Debug) XTrace.WriteLine("存在多个名为" + name + "的菜单，系统无法区分，请修改为不同的权限名，以免发生授权干扰！");

                return list[0];
            }

            return Root.FindByPath(name, _.Permission);
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
            if (list == null || list.Count < 1) return null;

            //list = list.FindAll(Menu<TEntity>._.ParentID, parentID);
            //if (list == null || list.Count < 1) return null;
            list = list.FindAll(Menu<TEntity>._.Visible, true);
            if (list == null || list.Count < 1) return null;

            return list.ToList().Where(e => filters.Contains(e.ID)).Cast<IMenu>().ToList();
        }
        #endregion

        #region 扩展操作
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var path = FullPath;
            if (!String.IsNullOrEmpty(path))
                return path;
            else
                return base.ToString();
        }
        #endregion

        #region 业务
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
                        this.ParentID = 0;
                        this.Url = m.Url;
                        this.Remark = m.Remark;

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

        /// <summary>添加子菜单</summary>
        /// <param name="name">名称</param>
        /// <param name="url"></param>
        /// <param name="sort"></param>
        /// <param name="reamark"></param>
        /// <returns></returns>
        public virtual TEntity Create(String name, String url, Int32 sort = 0, String reamark = null)
        {
            var entity = new TEntity();
            entity.ParentID = ID;
            entity.Name = name;
            entity.Permission = name;
            entity.Url = url;
            entity.Sort = sort;
            entity.Visible = true;
            entity.Remark = reamark ?? name;
            //entity.Save();

            return entity;
        }

        /// <summary>扫描配置文件中指定的目录</summary>
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
                if (top == null) top = Meta.Cache.Entities.Find(__.Remark, item);
                if (top == null)
                {
                    if (!IsBizDir(p)) continue;

                    top = Root.Create(item, null, 0, item);
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
                        parent.Name = parent.Permission = title;
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
                    entity = parent.Create(name, url);
                    entity.Name = entity.Permission = title;
                    entity.Save();

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
                    var menu = parent.Create(dirname, null, 0, dirname);
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
            ManageProvider.Provider.WriteLog(typeof(TEntity), action, remark);
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

        /// <summary>检查菜单名称，修改为新名称。返回自身，支持链式写法</summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        IMenu IMenu.CheckMenuName(String oldName, String newName)
        {
            //IMenu menu = FindByPath(AllChilds, oldName, _.Name);
            IMenu menu = FindByPath(oldName, _.Name, _.Permission, _.Remark);
            if (menu != null && menu.Name != newName)
            {
                menu.Name = menu.Permission = newName;
                menu.Save();
            }

            return this;
        }

        /// <summary>当前菜单</summary>
        IMenu IMenu.Current { get { return Current; } }

        /// <summary>父菜单</summary>
        IMenu IMenu.Parent { get { return Parent; } }

        /// <summary>子菜单</summary>
        IList<IMenu> IMenu.Childs { get { return Childs.OfType<IMenu>().ToList(); } }

        /// <summary>子孙菜单</summary>
        IList<IMenu> IMenu.AllChilds { get { return AllChilds.OfType<IMenu>().ToList(); } }

        /// <summary>根据层次路径查找</summary>
        /// <param name="path">层次路径</param>
        /// <returns></returns>
        IMenu IMenu.FindByPath(String path) { return FindByPath(path, _.Name, _.Permission, _.Remark); }

        /// <summary>根据权限查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IMenu IMenu.FindForPerssion(String name) { return FindForPerssion(name); }
        #endregion
    }

    public partial interface IMenu
    {
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        String GetFullPath(Boolean includeSelf, String separator, Func<IMenu, String> func);

        /// <summary>检查菜单名称，修改为新名称。返回自身，支持链式写法</summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        IMenu CheckMenuName(String oldName, String newName);

        /// <summary>当前菜单</summary>
        IMenu Current { get; }

        /// <summary>父菜单</summary>
        new IMenu Parent { get; }

        /// <summary>子菜单</summary>
        new IList<IMenu> Childs { get; }

        /// <summary>子孙菜单</summary>
        new IList<IMenu> AllChilds { get; }

        ///// <summary>深度</summary>
        //Int32 Deepth { get; }

        /// <summary>根据层次路径查找</summary>
        /// <param name="path">层次路径</param>
        /// <returns></returns>
        IMenu FindByPath(String path);

        /// <summary>根据权限查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IMenu FindForPerssion(String name);

        /// <summary>排序上升</summary>
        void Up();

        /// <summary>排序下降</summary>
        void Down();

        ///// <summary>保存</summary>
        ///// <returns></returns>
        //Int32 Save();

        /// <summary></summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        IList<IMenu> GetMySubMenus(Int32[] filters);
    }

    //public interface IMenuFactory : IEntityOperate
    //{
    //    IMenu Root { get; }

    //    ///// <summary>必要的菜单。必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
    //    //Int32[] Necessary { get; }
    //}

    ///// <summary>菜单实体工厂</summary>
    ///// <typeparam name="TEntity"></typeparam>
    //public class MenuFactory<TEntity> : Menu<TEntity>.EntityOperate where TEntity : Menu<TEntity>, new()
    //{
    //    public IMenu Root { get { return Menu<TEntity>.Root; } }
    //}
}