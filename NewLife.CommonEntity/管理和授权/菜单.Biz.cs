using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Web;
using XCode;
using NewLife.Reflection;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 菜单
    /// </summary>
    public partial class Menu<TEntity> : EntityTree<TEntity>, IMenu where TEntity : Menu<TEntity>, new()
    {
        #region 对象操作
        ///// <summary>已重载。</summary>
        //protected override EntityList<TEntity> FindChilds()
        //{
        //    return FindAllByParentID(ID);
        //}

        ///// <summary>已重载。</summary>
        //protected override TEntity FindParent()
        //{
        //    return FindByID(ParentID);
        //}

        //static Menu()
        //{
        //}

        /// <summary>
        /// 首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}菜单数据……", typeof(TEntity).Name);

            Meta.BeginTrans();
            try
            {
                Int32 sort = 1000;
                TEntity top = Root.AddChild("管理平台", null, sort -= 10, null);
                TEntity entity = top.AddChild("系统管理", null, sort -= 10, "System");
                entity.AddChild("菜单管理", "../System/Menu.aspx", sort -= 10, "菜单管理");
                entity.AddChild("管理员管理", "../System/Admin.aspx", sort -= 10, "管理员管理");
                entity.AddChild("角色管理", "../System/Role.aspx", sort -= 10, "角色管理");
                entity.AddChild("权限管理", "../System/RoleMenu.aspx", sort -= 10, "权限管理");
                entity.AddChild("日志查看", "../System/Log.aspx", sort -= 10, "日志查看");

                // 准备增加Admin目录下的所有页面
                ScanAndAdd("Admin", top);

                Meta.Commit();
                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}菜单数据！", typeof(TEntity).Name);
            }
            catch { Meta.Rollback(); throw; }
        }

        /// <summary>
        /// 已重载。调用Save时写日志，而调用Insert和Update时不写日志
        /// </summary>
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

        #region 扩展属性
        /// <summary>
        /// 父菜单名
        /// </summary>
        [XmlIgnore]
        public virtual String ParentMenuName { get { return Parent == null ? null : Parent.Name; } set { } }

        ///// <summary>
        ///// 完整文件路径
        ///// </summary>
        //public String FullFilePath
        //{
        //    get
        //    {
        //        if (String.IsNullOrEmpty(Url)) return Url;


        //    }
        //}

        /// <summary>
        /// 当前页所对应的菜单项
        /// </summary>
        public static TEntity Current
        {
            get
            {
                if (HttpContext.Current == null || HttpContext.Current.Request == null) return null;

                // 计算当前文件路径
                String p = HttpContext.Current.Request.PhysicalPath;
                String dirName = new DirectoryInfo(Path.GetDirectoryName(p)).Name;
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

                String url = String.Format(@"../{0}/{1}", dirName, fileName);
                return Meta.Cache.Entities.FindIgnoreCase(_.Url, url);
            }
        }

        /// <summary>当前页所对应的菜单项。通过实体资格提供者，保证取得正确的菜单项</summary>
        public static IMenu CurrentMenu
        {
            get
            {
                return TypeResolver.GetPropertyValue(typeof(IMenu), "Current") as IMenu;
            }
        }
        #endregion

        #region 扩展查询
        /// <summary>
        /// 根据主键查询一个菜单实体对象用于表单编辑
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
        /// 根据编号查找
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;
            return Meta.Cache.Entities.Find(_.ID, id);
        }

        /// <summary>
        /// 根据名字查找
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindByName(String name)
        {
            return Meta.Cache.Entities.Find(_.Name, name);
        }

        /// <summary>
        /// 根据Url查找
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static TEntity FindByUrl(String url)
        {
            return Meta.Cache.Entities.FindIgnoreCase(_.Url, url);
        }

        /// <summary>
        /// 根据名字查找，支持路径查找
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindForName(String name)
        {
            TEntity entity = FindByName(name);
            if (entity != null) return entity;

            return FindByPath(Meta.Cache.Entities, name, _.Name);
        }

        /// <summary>
        /// 根据权限名查找
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindByPerssion(String name)
        {
            return Meta.Cache.Entities.Find(_.Permission, name);
        }

        /// <summary>
        /// 为了权限而查找，支持路径查找
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindForPerssion(String name)
        {
            TEntity entity = FindByPerssion(name);
            if (entity != null) return entity;

            entity = FindByPath(Meta.Cache.Entities, name, _.Permission);
            // 找不到的时候，修改当前页面
            if (entity == null && Current != null)
            {
                if (Current.ResetName(name)) entity = Current;
            }
            return entity;
        }

        /// <summary>
        /// 路径查找
        /// </summary>
        /// <param name="list"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindByPath(EntityList<TEntity> list, String path, String name)
        {
            if (list == null || list.Count < 1) return null;
            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(name)) return null;

            String[] ss = path.Split(new Char[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            // 尝试一次性查找
            TEntity entity = list.Find(name, path);
            if (entity != null) return entity;

            EntityList<TEntity> list3 = new EntityList<TEntity>();
            for (int i = 0; i < ss.Length; i++)
            {
                // 找到符合当前级别的所有节点
                EntityList<TEntity> list2 = list.FindAll(name, ss[i]);
                if (list2 == null || list2.Count < 1) return null;

                // 是否到了最后
                if (i == ss.Length - 1)
                {
                    list3 = list2;
                    break;
                }

                // 找到它们的子节点
                list3.Clear();
                foreach (TEntity item in list2)
                {
                    if (item.Childs != null && item.Childs.Count > 0) list3.AddRange(item.Childs);
                }
                if (list3 == null || list3.Count < 1) return null;
            }
            if (list3 != null && list3.Count > 0)
                return list[0];
            else
                return null;
        }

        /// <summary>
        /// 查找指定菜单的子菜单
        /// </summary>
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
        /// <summary>
        /// 检查并重新设置名称和权限项
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Boolean ResetName(String name)
        {
            if (name.Contains(@".") || name.Contains(@"/") || name.Contains(@"\")) return false;

            // 没有设置权限项或者权限项和名字相同时
            //注意比较添加权限名称是否跟页面上所写Title相同（包括中文，英文）
            //如果权限名称与页面中的Title不相同时修改权限名称为页面Title名称，疏漏可能造成日志重复写入
            if (String.IsNullOrEmpty(Permission) || IsEnglish(Permission) || !String.Equals(Permission, name, StringComparison.OrdinalIgnoreCase)) Permission = name;
            if (String.IsNullOrEmpty(Name) || IsEnglish(Name)) Name = name;

            return Save() > 0;
        }

        /// <summary>
        /// 是否全英文
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static Boolean IsEnglish(String str)
        {
            if (String.IsNullOrEmpty(str)) return false;

            return Encoding.UTF8.GetByteCount(str) == str.Length;
        }
        #endregion

        #region 业务
        /// <summary>
        /// 导入
        /// </summary>
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
                if (Childs != null && Childs.Count > 0)
                {
                    foreach (TEntity item in Childs)
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

        /// <summary>
        /// 添加子菜单
        /// </summary>
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

        /// <summary>
        /// 添加子菜单
        /// </summary>
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

        /// <summary>
        /// 扫描指定目录并添加文件到第一个顶级菜单之下
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Int32 ScanAndAdd(String dir)
        {
            TEntity top = Root;
            if (Root.Childs != null && Root.Childs.Count > 0)
                top = Root.Childs[0];
            else
            {
                EntityList<TEntity> list = FindAllByName(_.ParentID, 0, _.ID + " Desc", 0, 1);
                if (list != null && list.Count > 1) top = list[0];
            }

            return ScanAndAdd(dir, top);
        }

        /// <summary>
        /// 扫描指定目录并添加文件到顶级菜单之下
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        public static Int32 ScanAndAdd(String dir, TEntity top)
        {
            if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("dir");
            if (top == null) throw new ArgumentNullException("top");

            String p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
            if (!Directory.Exists(p)) return 0;

            String[] dis = Directory.GetDirectories(p);
            if (dis == null || dis.Length <= 0) return 0;

            Int32 num = 0;
            foreach (String item in dis)
            {
                String dirName = new DirectoryInfo(item).Name;
                if (dirName.Equals("Frame", StringComparison.OrdinalIgnoreCase)) continue;
                //if (dirName.Equals("System", StringComparison.OrdinalIgnoreCase)) continue;
                if (dirName.StartsWith("img", StringComparison.OrdinalIgnoreCase)) continue;

                String[] fs = Directory.GetFiles(item, "*.aspx", SearchOption.TopDirectoryOnly);
                if (fs == null || fs.Length < 1) continue;

                List<String> files = new List<String>();
                foreach (String elm in fs)
                {
                    // 过滤掉表单页面
                    if (Path.GetFileNameWithoutExtension(elm).EndsWith("Form", StringComparison.OrdinalIgnoreCase)) continue;

                    files.Add(elm);
                }
                if (files.Count < 1) continue;

                // 添加
                TEntity parent = Find(_.Name, dirName);
                if (parent == null) parent = Find(_.Remark, dirName);
                if (parent == null)
                {
                    parent = top.AddChild(dirName, null);
                    num++;
                }
                foreach (String elm in files)
                {
                    String url = null;
                    if (String.Equals(dir, "Admin", StringComparison.OrdinalIgnoreCase))
                        url = String.Format(@"../{0}/{1}", dirName, Path.GetFileName(elm));
                    else
                        url = String.Format(@"../../{2}/{0}/{1}", dirName, Path.GetFileName(elm), dir);

                    TEntity entity = Find(_.Url, url);
                    if (entity != null) continue;

                    parent.AddChild(Path.GetFileNameWithoutExtension(elm), url);
                    num++;
                }
            }

            return num;
        }
        #endregion

        #region 日志
        ///// <summary>
        ///// Http状态，名称必须和管理员类中一致
        ///// </summary>
        //static HttpState<IAdministrator> http = new HttpState<IAdministrator>("Admin");
        //internal static IAdministrator DefaultAdministrator;
        ///// <summary>
        ///// 创建指定动作的日志实体。通过Http状态访问当前管理员对象，创建日志实体
        ///// </summary>
        ///// <param name="action"></param>
        ///// <returns></returns>
        //public static ILog CreateLog(String action)
        //{
        //    //IAdministrator admin = http.Current;
        //    //if (admin == null) admin = DefaultAdministrator;
        //    IAdministrator admin = Administrator.CurrentAdministrator;
        //    if (admin == null) return null;

        //    return admin.CreateLog(typeof(TEntity), action);
        //}

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark)
        {
            IAdministrator admin = Administrator.CurrentAdministrator;
            if (admin != null) admin.WriteLog(typeof(TEntity), action, remark);
        }
        #endregion

        #region IMenu 成员
        /// <summary>
        /// 取得全路径的实体，由上向下排序
        /// </summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        string IMenu.GetFullPath(bool includeSelf, string separator, Func<IMenu, string> func)
        {
            Func<TEntity, String> d = null;
            if (func != null) d = delegate(TEntity item) { return func(item); };

            return GetFullPath(includeSelf, separator, d);
        }
        #endregion
    }

    public partial interface IMenu
    {
        /// <summary>
        /// 取得全路径的实体，由上向下排序
        /// </summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        String GetFullPath(Boolean includeSelf, String separator, Func<IMenu, String> func);
    }
}