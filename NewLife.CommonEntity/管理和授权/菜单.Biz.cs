using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Xml.Serialization;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace NewLife.CommonEntity
{
    /// <summary>菜单</summary>
    public partial class Menu<TEntity> : EntityTree<TEntity>, IMenu where TEntity : Menu<TEntity>, new()
    {
        #region 对象操作
        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}菜单数据……", typeof(TEntity).Name);

            Meta.BeginTrans();
            try
            {
                //Int32 sort = 1000;
                //TEntity top = Root.AddChild("管理平台", null, sort -= 10, "Admin");
                //TEntity entity = top.AddChild("系统管理", null, sort -= 10, "System");
                //entity.AddChild("菜单管理", "../../Admin/System/Menu.aspx", sort -= 10, "菜单管理");
                //entity.AddChild("管理员管理", "../../Admin/System/Admin.aspx", sort -= 10, "管理员管理");
                //entity.AddChild("角色管理", "../../Admin/System/Role.aspx", sort -= 10, "角色管理");
                //entity.AddChild("权限管理", "../../Admin/System/RoleMenu.aspx", sort -= 10, "权限管理");
                //entity.AddChild("日志查看", "../../Admin/System/Log.aspx", sort -= 10, "日志查看");

                // 准备增加Admin目录下的所有页面
                //ScanAndAdd(top);
                ScanAndAdd();

                Meta.Commit();
                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}菜单数据！", typeof(TEntity).Name);
            }
            catch { Meta.Rollback(); throw; }
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
                TEntity entity = FindByID(ID);
                if (entity != null) name = entity.Name;
            }
            WriteLog("删除", name);

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
                if (HttpContext.Current == null || HttpContext.Current.Request == null) return null;

                String key = "CurrentMenu";
                TEntity entity = HttpContext.Current.Items[key] as TEntity;
                if (entity == null)
                {
                    entity = GetCurrentMenu();
                    if (entity != null)
                    {
                        // 根据页面标题，修正菜单名
                        Page page = HttpContext.Current.Handler as Page;
                        if (page != null && !String.IsNullOrEmpty(page.Title)) entity.ResetName(page.Title);
                    }
                    HttpContext.Current.Items[key] = entity;
                }

                return entity;
            }
        }

        /// <summary>当前页所对应的菜单项。通过实体资格提供者，保证取得正确的菜单项</summary>
        [Obsolete("该成员在后续版本中讲不再被支持！")]
        public static IMenu CurrentMenu
        {
            get
            {
                //return TypeResolver.GetPropertyValue(typeof(IMenu), "Current") as IMenu;

                ICommonManageProvider provider = CommonManageProvider.Provider;
                if (provider == null) return null;

                Type type = provider.MenuType;
                return PropertyInfoX.Create(type, "Current").GetValue() as IMenu;
            }
        }

        static TEntity GetCurrentMenu()
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null) return null;

            // 计算当前文件路径
            String p = HttpContext.Current.Request.PhysicalPath;
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(p));
            String fileName = Path.GetFileName(p);

            // 查找所有以该文件名结尾的菜单
            EntityList<TEntity> list = Meta.Cache.Entities;
            list = list.FindAll(delegate(TEntity item)
            {
                return !String.IsNullOrEmpty(item.Url) && item.Url.Trim().EndsWith(fileName, StringComparison.OrdinalIgnoreCase);
            });
            if ((list == null || list.Count < 1) && Path.GetFileNameWithoutExtension(p).EndsWith("Form", StringComparison.OrdinalIgnoreCase))
            {
                fileName = Path.GetFileNameWithoutExtension(p);
                fileName = fileName.Substring(0, fileName.Length - "Form".Length);
                fileName += Path.GetExtension(p);

                // 有可能是表单
                list = Meta.Cache.Entities.FindAll(delegate(TEntity item)
                {
                    return !String.IsNullOrEmpty(item.Url) && item.Url.Trim().EndsWith(fileName, StringComparison.OrdinalIgnoreCase);
                });
            }
            if (list == null || list.Count < 1) return null;
            if (list.Count == 1) return list[0];

            // 查找所有以该文件名结尾的菜单
            EntityList<TEntity> list2 = list.FindAll(delegate(TEntity item)
            {
                return !String.IsNullOrEmpty(item.Url) && item.Url.Trim().EndsWith(@"/" + fileName, StringComparison.OrdinalIgnoreCase);
            });
            if (list2 == null || list2.Count < 1) return list[0];
            if (list2.Count == 1) return list2[0];

            // 优先全路径
            String url = String.Format(@"../../{0}/{1}/{2}", di.Parent.Name, di.Name, fileName);
            TEntity entity = Meta.Cache.Entities.FindIgnoreCase(_.Url, url);
            if (entity != null) return entity;

            // 兼容旧版本
            url = String.Format(@"../{0}/{1}", di.Name, fileName);
            return Meta.Cache.Entities.FindIgnoreCase(_.Url, url);
        }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;
            return Meta.Cache.Entities.Find(_.ID, id);
        }

        /// <summary>根据名字查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindByName(String name) { return Meta.Cache.Entities.Find(_.Name, name); }

        /// <summary>根据Url查找</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static TEntity FindByUrl(String url) { return Meta.Cache.Entities.FindIgnoreCase(_.Url, url); }

        /// <summary>根据名字查找，支持路径查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindForName(String name)
        {
            TEntity entity = FindByName(name);
            if (entity != null) return entity;

            //return FindByPath(Meta.Cache.Entities, name, _.Name);
            return Root.FindByPath(name, _.Name, _.Permission, _.Remark);
        }

        /// <summary>根据权限名查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindByPerssion(String name) { return Meta.Cache.Entities.Find(_.Permission, name); }

        /// <summary>为了权限而查找，支持路径查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindForPerssion(String name)
        {
            // 计算集合，为了处理同名的菜单
            EntityList<TEntity> list = Meta.Cache.Entities.FindAll(_.Permission, name);
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

            //TEntity entity = FindByPerssion(name);
            //if (entity != null) return entity;

            //TEntity entity = FindByPath(Meta.Cache.Entities, name, _.Permission);
            TEntity entity = Root.FindByPath(name, _.Permission);
            // 找不到的时候，修改当前页面
            if (entity == null)
            {
                if (current == null) current = Current;
                if (current != null)
                {
                    if (current.ResetName(name)) entity = current;
                }
            }
            return entity;
        }

        ///// <summary>
        ///// 路径查找
        ///// </summary>
        ///// <param name="list"></param>
        ///// <param name="path"></param>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public static TEntity FindByPath(EntityList<TEntity> list, String path, String name)
        //{
        //    if (list == null || list.Count < 1) return null;
        //    if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(name)) return null;

        //    // 尝试一次性查找
        //    TEntity entity = list.Find(name, path);
        //    if (entity != null) return entity;

        //    String[] ss = path.Split(new Char[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        //    if (ss == null || ss.Length < 1) return null;

        //    // 找第一级
        //    entity = list.Find(name, ss[0]);
        //    if (entity == null) entity = list.Find(_.Remark, ss[0]);
        //    if (entity == null) return null;

        //    // 是否还有下级
        //    if (ss.Length == 1) return entity;

        //    // 递归找下级
        //    return FindByPath(entity.Childs, String.Join("\\", ss, 1, ss.Length - 1), name);

        //    //EntityList<TEntity> list3 = new EntityList<TEntity>();
        //    //for (int i = 0; i < ss.Length; i++)
        //    //{
        //    //    // 找到符合当前级别的所有节点
        //    //    EntityList<TEntity> list2 = list.FindAll(name, ss[i]);
        //    //    if (list2 == null || list2.Count < 1) return null;

        //    //    // 是否到了最后
        //    //    if (i == ss.Length - 1)
        //    //    {
        //    //        list3 = list2;
        //    //        break;
        //    //    }

        //    //    // 找到它们的子节点
        //    //    list3.Clear();
        //    //    foreach (TEntity item in list2)
        //    //    {
        //    //        if (item.Childs != null && item.Childs.Count > 0) list3.AddRange(item.Childs);
        //    //    }
        //    //    if (list3 == null || list3.Count < 1) return null;
        //    //}
        //    //if (list3 != null && list3.Count > 0)
        //    //    return list[0];
        //    //else
        //    //    return null;
        //}

        /// <summary>查找指定菜单的子菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EntityList<TEntity> FindAllByParentID(Int32 id)
        {
            EntityList<TEntity> list = Meta.Cache.Entities.FindAll(_.ParentID, id);
            if (list != null && list.Count > 0) list.Sort(new String[] { _.Sort, _.ID }, new Boolean[] { true, false });
            return list;
        }
        #endregion

        #region 扩展操作
        /// <summary>检查并重新设置名称和权限项</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Boolean ResetName(String name)
        {
            if (name.Contains(@".") || name.Contains(@"/") || name.Contains(@"\")) return false;

            // 没有设置权限项或者权限项和名字相同时
            // 注意比较添加权限名称是否跟页面上所写Title相同（包括中文，英文）
            // 如果权限名称与页面中的Title不相同时修改权限名称为页面Title名称，疏漏可能造成日志重复写入
            if (String.IsNullOrEmpty(Permission) || IsEnglish(Permission) || !String.Equals(Permission, name, StringComparison.OrdinalIgnoreCase)) Permission = name;
            if (String.IsNullOrEmpty(Name) || IsEnglish(Name)) Name = name;
            //检查是否有菜单名称或权限是否有变动如无变动不做记录
            if (Dirtys[_.Permission] || Dirtys[_.Name])
                return Save() > 0;
            return false;
        }

        /// <summary>是否全英文</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static Boolean IsEnglish(String str)
        {
            if (String.IsNullOrEmpty(str)) return false;

            return Encoding.UTF8.GetByteCount(str) == str.Length;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var path = FullPath;
            if (!String.IsNullOrEmpty(path))
                return path;
            return base.ToString();
        }
        #endregion

        #region 业务
        /// <summary>导入</summary>
        public virtual void Import()
        {
            Meta.BeginTrans();
            try
            {
                //顶级节点根据名字合并
                if (ParentID == 0)
                {
                    TEntity m = Find(_.Name, Name);
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

                Meta.Commit();
            }
            catch
            {
                Meta.Rollback();
                throw;
            }
        }

        /// <summary>添加子菜单</summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual TEntity AddChild(String name, String url)
        {
            TEntity entity = new TEntity();
            entity.ParentID = ID;
            entity.Name = name;
            entity.Permission = name;
            entity.Url = url;
            entity.IsShow = true;
            entity.Remark = name;
            entity.Save();

            return entity;
        }

        /// <summary>添加子菜单</summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <param name="sort"></param>
        /// <param name="reamark"></param>
        /// <returns></returns>
        public virtual TEntity AddChild(String name, String url, Int32 sort, String reamark)
        {
            TEntity entity = new TEntity();
            entity.ParentID = ID;
            entity.Name = name;
            entity.Permission = name;
            entity.Url = url;
            entity.Sort = sort;
            entity.IsShow = true;
            entity.Remark = reamark;
            entity.Save();

            return entity;
        }

        /// <summary>扫描配置文件中指定的目录</summary>
        /// <returns></returns>
        public static Int32 ScanAndAdd()
        {
            TEntity top = null;

            //扫描目录
            String[] AppDirs = Config.GetConfigSplit<String>("NewLife.CommonEntity.AppDirs", null);
            //过滤文件
            String[] AppDirsFileFilter = Config.GetConfigSplit<String>("NewLife.CommonEntity.AppDirsFileFilter", null);
            //是否在子目中过滤
            Boolean AppDirsIsAllFilter = Config.GetConfig<Boolean>("NewLife.CommonEntity.AppDirsIsAllDirs", false);

            List<String> AppDirsFileFilterList = AppDirsFileFilter == null ? null : new List<String>(AppDirsFileFilter);

            if (AppDirs == null || AppDirs.Length == 0)
                AppDirs = new String[] { "Admin" };
            else if (AppDirs.Length > 0)
            {
                List<String> list = new List<string>(AppDirs);
                if (!list.Contains("Admin")) list.Add("Admin");
                AppDirs = list.ToArray();
            }

            Int32 total = 0;
            foreach (String item in AppDirs)
            {
                // 如果目录不存在，就没必要扫描了
                String p = item;
                if (!Path.IsPathRooted(p)) p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p);
                if (!Directory.Exists(p))
                {
                    p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("Admin", item));
                    if (!Directory.Exists(p)) continue;
                }

                // 根据目录找菜单，它将作为顶级菜单
                top = FindForName(item);
                if (top == null) top = Meta.Cache.Entities.Find(_.Remark, item);
                if (top == null) top = Root.AddChild(item, null, 0, item);
                //total += ScanAndAdd(item, top);
                total += ScanAndAdd(item, top, AppDirsFileFilterList, AppDirsIsAllFilter);
            }

            return total;
        }

        static TEntity GetTopForDir(String dir)
        {
            // 根据目录找菜单，它将作为顶级菜单
            TEntity top = FindForName(dir);
            if (top == null) top = Meta.Cache.Entities.Find(_.Remark, dir);

            // 如果找不到，就取第一个作为顶级
            if (top == null)
            {
                var childs = Root.Childs;
                if (childs != null && childs.Count > 0)
                    top = childs[0];
                else
                {
                    EntityList<TEntity> list = FindAllByName(_.ParentID, 0, _.ID + " Desc", 0, 1);
                    if (list != null && list.Count > 1) top = list[0];
                }
            }
            return top;
        }

        /// <summary>扫描指定目录并添加文件到第一个顶级菜单之下</summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Int32 ScanAndAdd(String dir)
        {
            return ScanAndAdd(dir, GetTopForDir(dir));
        }

        /// <summary>获取目录层级</summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static String GetPathForScan(String dir)
        {
            if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("dir");

            // 要扫描的目录
            String p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
            if (!Directory.Exists(p)) return "";

            //获取层级
            String currentPath = Path.Combine("../../", p.Replace(AppDomain.CurrentDomain.BaseDirectory, null)).Replace("\\", "/");

            if (!currentPath.EndsWith("/"))
                currentPath += "/";

            return currentPath;
        }

        /// <summary>
        /// 扫描指定目录并添加文件到顶级菜单之下
        /// </summary>
        /// <param name="dir">扫描目录</param>
        /// <param name="top">父级</param>
        /// <param name="fileFilter">过滤文件名</param>
        /// <param name="isFilterChildDir">是否在子目录中过滤</param>
        /// <returns></returns>
        public static Int32 ScanAndAdd(String dir, TEntity top, List<String> fileFilter, Boolean isFilterChildDir)
        {
            if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("dir");
            if (top == null) throw new ArgumentNullException("top");

            // 添加
            Int32 num = 0;
            //目录是否做为新菜单
            Boolean isAddDir = false;

            // 要扫描的目录
            String p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
            if (!Directory.Exists(p)) return num;

            //本意，获取目录名
            String dirName = new DirectoryInfo(p).Name;


            if (dirName.Equals("Frame", StringComparison.OrdinalIgnoreCase)) return num;
            //if (dirName.Equals("System", StringComparison.OrdinalIgnoreCase)) continue;
            if (dirName.StartsWith("img", StringComparison.OrdinalIgnoreCase)) return num;

            //本目录aspx页面
            String[] fs = Directory.GetFiles(p, "*.aspx", SearchOption.TopDirectoryOnly);
            //本目录子子录
            String[] dis = Directory.GetDirectories(p);
            //如没有页面和子目录
            if ((fs == null || fs.Length < 1) && (dis == null || dis.Length < 1))
                return num;

            //本目录菜单
            TEntity parent = Find(_.Name, dirName);
            if (parent == null) parent = Find(_.Remark, dirName);
            if (parent == null)
            {
                parent = top.AddChild(dirName, null, 0, dirName);
                parent.Save();
                num++;
                //目录为新增菜单
                isAddDir = true;
            }

            //aspx
            if (fs != null && fs.Length > 0)
            {

                List<String> files = new List<String>();
                foreach (String elm in fs)
                {
                    //过滤特定文件名文件
                    if (fileFilter != null && fileFilter.Count() > 0 && null != fileFilter.Find(delegate(String item)
                    {
                        return item.Equals(Path.GetFileName(elm), StringComparison.CurrentCultureIgnoreCase);
                    }))
                        continue;

                    // 过滤掉表单页面
                    if (Path.GetFileNameWithoutExtension(elm).EndsWith("Form", StringComparison.OrdinalIgnoreCase)) continue;
                    // 过滤掉选择页面
                    if (Path.GetFileNameWithoutExtension(elm).StartsWith("Select", StringComparison.OrdinalIgnoreCase)) continue;
                    //if (elm.EndsWith("Default.aspx", StringComparison.OrdinalIgnoreCase)) continue;

                    files.Add(elm);
                }

                if (files.Count > 0)
                {
                    String currentPath = GetPathForScan(p);
                    //aspx页面
                    foreach (String elm in files)
                    {
                        String url = null;
                        if (Path.GetFileName(elm).Equals("Default.aspx", StringComparison.OrdinalIgnoreCase))
                        {
                            parent.Url = Path.Combine(currentPath, "Default.aspx");
                            String title = GetPageTitle(elm);
                            if (!String.IsNullOrEmpty(title)) parent.Name = parent.Permission = title;
                            parent.Save();
                        }

                        // 全部使用全路径
                        //if (String.Equals(dir, "Admin", StringComparison.OrdinalIgnoreCase))
                        //    url = String.Format(@"../{0}/{1}", dirName, Path.GetFileName(elm));
                        //else
                        url = Path.Combine(currentPath, Path.GetFileName(elm));
                        TEntity entity = Find(_.Url, url);
                        if (entity != null) continue;

                        entity = parent.AddChild(Path.GetFileNameWithoutExtension(elm), url);
                        String elmTitle = GetPageTitle(elm);
                        if (!String.IsNullOrEmpty(elmTitle)) entity.Name = entity.Permission = elmTitle;
                        entity.Save();

                        num++;
                    }
                }
            }

            // 子级目录
            if (dis == null || dis.Length > 0)
                foreach (String item in dis)
                {
                    num += isFilterChildDir ? ScanAndAdd(item, parent, fileFilter, isFilterChildDir) : ScanAndAdd(item, parent, null, !isFilterChildDir);
                }

            //如果目录中没有菜单，移除目录
            //if (parent != null && parent.ID > 0 && FindCount(_.ParentID, parent.ID) == 0)
            //目录为新增加菜单且本级以下num为1则认为只增加了目录，并无子级
            if (isAddDir && num == 1)
            {
                TEntity remove = top.Childs.Find(_.ID, parent.ID);
                if (remove != null)
                    top.Childs.Remove(remove);
                parent.Delete();
                num = num - 1;
            }

            return num;
        }

        /// <summary>扫描指定目录并添加文件到顶级菜单之下</summary>
        /// <param name="dir"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        public static Int32 ScanAndAdd(String dir, TEntity top)
        {
            return ScanAndAdd(dir, top, null, false);
        }

        ///// <summary>
        ///// 扫描指定目录并添加文件到顶级菜单之下
        ///// </summary>
        ///// <param name="dir"></param>
        ///// <param name="top"></param>
        ///// <returns></returns>
        //public static Int32 ScanAndAdd(String dir, TEntity top)
        //{
        //    if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("dir");
        //    if (top == null) throw new ArgumentNullException("top");

        //    // 要扫描的目录
        //    String p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
        //    if (!Directory.Exists(p)) return 0;

        //    // 处理Default.aspx，该顶级目录下的首页，一般是工作台
        //    String defFile = Path.Combine(p, "Default.aspx");
        //    if (File.Exists(defFile))
        //    {
        //        if (String.IsNullOrEmpty(top.Url))
        //        {
        //            top.Url = String.Format(@"../{0}/{1}", dir, Path.GetFileName(defFile));
        //            String title = GetPageTitle(defFile);
        //            if (!String.IsNullOrEmpty(title)) top.Name = top.Permission = title;
        //            top.Save();
        //        }
        //    }

        //    // 找到子级目录
        //    String[] dis = Directory.GetDirectories(p);
        //    if (dis == null || dis.Length <= 0)
        //        //return 0;
        //        //没有子目录时，将自身设置为扫述目录
        //        dis = new String[] { p };

        //    Int32 num = 0;
        //    foreach (String item in dis)
        //    {
        //        String dirName = new DirectoryInfo(item).Name;
        //        if (dirName.Equals("Frame", StringComparison.OrdinalIgnoreCase)) continue;
        //        //if (dirName.Equals("System", StringComparison.OrdinalIgnoreCase)) continue;
        //        if (dirName.StartsWith("img", StringComparison.OrdinalIgnoreCase)) continue;

        //        String[] fs = Directory.GetFiles(item, "*.aspx", SearchOption.TopDirectoryOnly);
        //        if (fs == null || fs.Length < 1) continue;

        //        List<String> files = new List<String>();
        //        foreach (String elm in fs)
        //        {
        //            // 过滤掉表单页面
        //            if (Path.GetFileNameWithoutExtension(elm).EndsWith("Form", StringComparison.OrdinalIgnoreCase)) continue;
        //            // 过滤掉选择页面
        //            if (Path.GetFileNameWithoutExtension(elm).StartsWith("Select", StringComparison.OrdinalIgnoreCase)) continue;
        //            //if (elm.EndsWith("Default.aspx", StringComparison.OrdinalIgnoreCase)) continue;

        //            files.Add(elm);
        //        }
        //        if (files.Count < 1) continue;

        //        // 添加
        //        TEntity parent = Find(_.Name, dirName);
        //        if (parent == null) parent = Find(_.Remark, dirName);
        //        if (parent == null)
        //        {
        //            parent = top.AddChild(dirName, null, 0, dirName);
        //            num++;
        //        }

        //        // 处理Default.aspx
        //        defFile = Path.Combine(item, "Default.aspx");
        //        if (File.Exists(defFile))
        //        {
        //            if (String.IsNullOrEmpty(parent.Url))
        //            {
        //                parent.Url = String.Format(@"../../{0}/{1}/{2}", dir, item, Path.GetFileName(defFile));
        //                String title = GetPageTitle(defFile);
        //                if (!String.IsNullOrEmpty(title)) parent.Name = parent.Permission = title;
        //                parent.Save();
        //            }

        //            continue;
        //        }

        //        foreach (String elm in files)
        //        {
        //            String url = null;
        //            if (elm.EndsWith("Default.aspx", StringComparison.OrdinalIgnoreCase))
        //            {

        //            }

        //            // 全部使用全路径
        //            //if (String.Equals(dir, "Admin", StringComparison.OrdinalIgnoreCase))
        //            //    url = String.Format(@"../{0}/{1}", dirName, Path.GetFileName(elm));
        //            //else
        //            url = String.Format(@"../{0}/{1}", dirName, Path.GetFileName(elm));
        //            TEntity entity = Find(_.Url, url);
        //            if (entity != null) continue;

        //            url = String.Format(@"../../{2}/{0}/{1}", dirName, Path.GetFileName(elm), dir);
        //            entity = Find(_.Url, url);
        //            if (entity != null) continue;

        //            entity = parent.AddChild(Path.GetFileNameWithoutExtension(elm), url);
        //            String title = GetPageTitle(elm);
        //            if (!String.IsNullOrEmpty(title)) entity.Name = entity.Permission = title;
        //            entity.Save();

        //            num++;
        //        }
        //    }

        //    return num;
        //}

        static Regex reg_PageTitle = new Regex("\\bTitle=\"([^\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex reg_PageTitle2 = new Regex("<title>([^<]*)</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static String GetPageTitle(String pagefile)
        {
            if (String.IsNullOrEmpty(pagefile) || !".aspx".EqualIgnoreCase(Path.GetExtension(pagefile)) || !File.Exists(pagefile)) return null;

            // 读取aspx的第一行，里面有Title=""
            String line = null;
            using (StreamReader reader = new StreamReader(pagefile))
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
            var admin = ManageProvider.Provider.Current as IAdministrator;
            if (admin != null) admin.WriteLog(typeof(TEntity), action, remark);
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
                (menu as IEntity).Save();
            }

            return this;
        }

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

        /// <summary>父菜单</summary>
        IMenu Parent { get; }

        /// <summary>子菜单</summary>
        IList<IMenu> Childs { get; }

        /// <summary>子孙菜单</summary>
        IList<IMenu> AllChilds { get; }

        /// <summary>深度</summary>
        Int32 Deepth { get; }

        /// <summary>根据层次路径查找</summary>
        /// <param name="path">层次路径</param>
        /// <returns></returns>
        IMenu FindByPath(String path);
    }
}