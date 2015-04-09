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
            if (list == null || list.Count < 1) return null;

            list = list.FindAll(_.Visible, true);
            if (list == null || list.Count < 1) return null;

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

            entity.Visible = true;

            entity.Insert();

            return entity;
        }

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
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark)
        {
            LogProvider.Provider.WriteLog(typeof(TEntity), action, remark);
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

            /// <summary>根据编号找到菜单</summary>
            /// <param name="id"></param>
            /// <returns></returns>
            IMenu IMenuFactory.FindByID(Int32 id) { return FindByID(id); }

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
            /// <param name="rootName"></param>
            /// <param name="asm"></param>
            /// <param name="nameSpace"></param>
            /// <returns></returns>
            public virtual IList<IMenu> ScanController(String rootName, Assembly asm, String nameSpace)
            {
                var list = new List<IMenu>();

                using (var dbtrans = Meta.CreateTrans())
                {
                    var root = Root.FindByPath(rootName);
                    if (root == null)
                    {
                        root = Root.Add(rootName, null, null);
                        list.Add(root);
                    }

                    var ns = nameSpace.EnsureEnd(".");
                    // 遍历该程序集所有类型
                    foreach (var type in asm.GetTypes())
                    {
                        var name = type.Name;
                        if (!name.EndsWith("Controller")) continue;
                        name = name.TrimEnd("Controller");
                        if (type.Namespace != nameSpace && !type.Namespace.StartsWith(ns)) continue;

                        var url = "~/" + rootName;

                        var node = root;
                        // 要考虑命名空间里面还有层级。这种情况比较少有，所以只考虑一级关系
                        var ns2 = type.Namespace.Substring(nameSpace.Length).TrimStart(".");
                        if (!String.IsNullOrEmpty(ns2))
                        {
                            url += "/" + ns2;
                            node = node.Add(ns2, null, url);
                            list.Add(node);
                        }

                        // 添加Controller
                        var controller = node.FindByPath(name);
                        if (controller == null)
                        {
                            url += "/" + name;
                            var att = type.GetCustomAttribute<DisplayNameAttribute>(true);
                            controller = node.Add(name, att != null ? att.DisplayName : null, url);
                            list.Add(node);
                        }

                        // 添加该类型下的所有Action
                        foreach (var method in type.GetMethods())
                        {
                            if (method.IsStatic || !method.IsPublic) continue;
                            // 跳过删除
                            if (method.Name.EqualIgnoreCase("Delete")) continue;
                            // 为了不引用Mvc，采取字符串比较
                            //if (!method.ReturnType.Name.EndsWith("")) continue;
                            var rt = method.ReturnType;
                            while (rt != null && rt.BaseType != null && rt.BaseType != typeof(Object)) rt = rt.BaseType;
                            if (rt.Name != "ActionResult") continue;

                            // 还要跳过带有HttpPost特性的方法
                            var flag = false;
                            foreach (var att in method.GetCustomAttributes(true))
                            {
                                if (att != null && att.GetType().Name == "HttpPostAttribute")
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            if (flag) continue;

                            // 查找并添加菜单
                            var action = controller.FindByPath(method.Name);
                            if (action == null)
                            {
                                var att = method.GetCustomAttribute<DisplayNameAttribute>(true);
                                action = controller.Add(method.Name, att != null ? att.DisplayName : null, url + "/" + method.Name);
                                list.Add(action);
                            }
                        }
                    }

                    // 如果新增了菜单，需要检查权限
                    if (list.Count > 0)
                    {
                        var eop = ManageProvider.GetFactory<IRole>();
                        eop.EntityType.Invoke("CheckRole");
                    }

                    dbtrans.Commit();
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
        IMenu Root { get; }

        /// <summary>根据编号找到菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IMenu FindByID(Int32 id);

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

        /// <summary>父菜单</summary>
        new IMenu Parent { get; }

        /// <summary>子菜单</summary>
        new IList<IMenu> Childs { get; }

        /// <summary>子孙菜单</summary>
        new IList<IMenu> AllChilds { get; }

        /// <summary>显示名。优先显示中文备注</summary>
        String DisplayName { get; }

        /// <summary>根据层次路径查找</summary>
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
    }
}