using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NewLife.Log;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 菜单
    /// </summary>
    public partial class Menu<TEntity> : EntityTree<TEntity> where TEntity : Menu<TEntity>, new()
    {
        #region 对象操作
        /// <summary>已重载。</summary>
        protected override EntityList<TEntity> FindChilds()
        {
            return FindAllByParentID(ID);
        }

        /// <summary>已重载。</summary>
        protected override TEntity FindParent()
        {
            return FindByID(ParentID);
        }

        static Menu()
        {
            if (Meta.Count <= 0)
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化表单数据……");

                TEntity entity = Root;
                entity = entity.AddChild("控制台", null);
                entity = entity.AddChild("系统管理", null);
                entity.AddChild("菜单管理", "../System/Menu.aspx");
                entity.AddChild("管理员管理", "../System/UserManage.aspx");
                entity.AddChild("角色管理", "../System/Role.aspx");
                entity.AddChild("权限管理", "../System/RoleMenu.aspx");

                if (XTrace.Debug) XTrace.WriteLine("完成初始化表单数据！");
            }
        }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 父菜单名
        /// </summary>
        [XmlIgnore]
        public virtual String ParentMenuName { get { return Parent == null ? null : Parent.Name; } set { } }
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

            return FindByPath(Meta.Cache.Entities, name, _.Permission);
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

        /// <summary>
        /// 查找所有没有父节点的节点集合
        /// </summary>
        /// <returns></returns>
        public static EntityList<TEntity> FindAllNoParent()
        {
            return Meta.Cache.Entities.FindAll(delegate(TEntity item)
            {
                return item.ParentID > 0 && item.Parent == null;
            });
            //return EntityList<TEntity>.From(list);
        }
        #endregion

        #region 扩展操作
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

        ///// <summary>
        ///// 取得全路径的实体，由上向下排序
        ///// </summary>
        ///// <param name="includeSelf"></param>
        ///// <returns></returns>
        //public EntityList<TEntity> GetFullPath(Boolean includeSelf)
        //{
        //    EntityList<TEntity> list = null;
        //    if (Parent != null) list = Parent.GetFullPath(true);

        //    if (!includeSelf) return list;

        //    if (list == null) list = new EntityList<TEntity>();
        //    list.Add(this as TEntity);

        //    return list;
        //}

        ///// <summary>
        ///// 创建菜单树
        ///// </summary>
        ///// <param name="nodes">父集合</param>
        ///// <param name="list">菜单列表</param>
        ///// <param name="url">格式化地址，可以使用{ID}和{Name}</param>
        ///// <param name="func">由菜单项创建树节点的委托</param>
        //public static void MakeTree(TreeNodeCollection nodes, EntityList<TEntity> list, String url, Func<TEntity, TreeNode> func)
        //{
        //    if (list == null || list.Count < 1) return;

        //    foreach (TEntity item in list)
        //    {
        //        TreeNode node = null;
        //        if (func == null)
        //        {
        //            node = new TreeNode(item.Name);
        //            node.Value = item.ID.ToString();
        //            if (!String.IsNullOrEmpty(url))
        //                node.NavigateUrl = url.Replace("{ID}", item.ID.ToString()).Replace("{Name}", item.Name);
        //        }
        //        else
        //        {
        //            node = func(item);
        //        }

        //        if (item.Childs != null && item.Childs.Count > 0) MakeTree(node.ChildNodes, item.Childs, url, func);

        //        if (node != null) nodes.Add(node);
        //    }
        //}

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
            entity.Save();

            return entity;
        }
        #endregion
    }
}