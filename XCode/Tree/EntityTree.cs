using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web.UI.WebControls;
using System.Xml.Serialization;
using NewLife.Exceptions;
using NewLife.Reflection;
using NewLife.Linq;

namespace XCode
{
    /// <summary>主键为整型的实体树基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EntityTree<TEntity> : EntityTree<Int32, TEntity> where TEntity : EntityTree<TEntity>, new()
    { }

    /// <summary>实体树基类，具有树形结构的实体继承该类即可得到各种树操作功能</summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public abstract class EntityTree<TKey, TEntity> : Entity<TEntity>, IEntityTree where TEntity : EntityTree<TKey, TEntity>, new()
    {
        #region 构造
        //static EntityTree()
        //{
        //    Meta.OnDataChange += delegate { Root = null; };
        //}
        #endregion

        #region 设置型属性
        /// <summary>关联键名称，一般是主键，如ID</summary>
        protected virtual String KeyName { get { return Meta.Unique.Name; } }

        /// <summary>关联父键名，一般是Parent加主键，如ParentID</summary>
        protected virtual String ParentKeyName
        {
            get
            {
                String name = "Parent" + KeyName;
                //if (Meta.FieldNames.Contains(name)) return name;
                // 不区分大小写的比较
                if (Meta.FieldNames.Contains(name, StringComparer.OrdinalIgnoreCase)) return name;

                return null;
            }
        }

        private static String _SortingKeyName;
        /// <summary>排序字段，默认是"Sorting", "Sort", "Rank"之一</summary>
        protected virtual String SortingKeyName
        {
            get
            {
                if (_SortingKeyName == null)
                {
                    // Empty与null不同，可用于区分是否已计算
                    _SortingKeyName = String.Empty;

                    String[] names = new String[] { "Sorting", "Sort", "Rank" };
                    IList<String> fs = Meta.FieldNames;
                    foreach (String name in names)
                    {
                        //if (fs.Contains(name))
                        // 不区分大小写的比较
                        if (fs.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            _SortingKeyName = name;
                            break;
                        }
                    }
                }
                return _SortingKeyName;
            }
        }

        /// <summary>名称键名，如Name，否则使用第二个字段</summary>
        /// <remarks>影响NodeName、TreeNodeName、TreeNodeName2、FindByPath、GetFullPath、GetFullPath2等</remarks>
        protected virtual String NameKeyName { get { return Meta.FieldNames.Contains("Name", StringComparer.OrdinalIgnoreCase) ? "Name" : Meta.Fields.Where(f => !f.IsIdentity).FirstOrDefault().Name; } }

        /// <summary>是否缓存Childs、AllChilds、Parent等</summary>
        protected virtual Boolean EnableCaching { get { return true; } }
        #endregion

        #region 扩展属性
        /// <summary>排序值</summary>
        private Int32 Sort
        {
            get { return String.IsNullOrEmpty(SortingKeyName) ? 0 : (Int32)this[SortingKeyName]; }
            set { if (!String.IsNullOrEmpty(SortingKeyName) && (Int32)this[SortingKeyName] != value) SetItem(SortingKeyName, value); }
        }

        /// <summary>子节点</summary>
        public virtual EntityList<TEntity> Childs
        {
            get { return EnableCaching ? GetExtend<EntityList<TEntity>>("Childs", e => FindChilds(), !IsNull((TKey)this[KeyName])) : FindChilds(); }
            set { SetExtend("Childs", value); }
        }

        /// <summary>子节点</summary>
        protected virtual EntityList<TEntity> FindChilds() { return FindAllByParent((TKey)this[KeyName]); }

        /// <summary>父节点</summary>
        [XmlIgnore]
        public virtual TEntity Parent
        {
            get { return EnableCaching ? GetExtend<TEntity>("Parent", e => FindParent()) : FindParent(); }
            set { SetExtend("Parent", value); }
        }

        /// <summary>父节点</summary>
        protected virtual TEntity FindParent() { return Meta.Cache.Entities.Find(KeyName, this[ParentKeyName]); }

        /// <summary>父节点</summary>
        protected static TEntity FindByKeyWithCache(TKey key)
        {
            return Meta.Cache.Entities.Find((Meta.Factory.Default as TEntity).KeyName, key);
        }

        /// <summary>子孙节点</summary>
        [XmlIgnore]
        public virtual EntityList<TEntity> AllChilds
        {
            get { return EnableCaching ? GetExtend<EntityList<TEntity>>("AllChilds", e => FindAllChilds(this), !IsNull((TKey)this[KeyName])) : FindAllChilds(this); }
            set { SetExtend("AllChilds", value); }
        }

        /// <summary>父节点集合</summary>
        [XmlIgnore]
        public virtual EntityList<TEntity> AllParents
        {
            get { return EnableCaching ? GetExtend<EntityList<TEntity>>("AllParents", e => FindAllParents(this)) : FindAllParents(this); }
            set { SetExtend("AllParents", value); }
        }

        /// <summary>深度</summary>
        [XmlIgnore]
        public virtual Int32 Deepth
        {
            get
            {
                if (IsNull((TKey)this[KeyName])) return 0;

                Int32 _Deepth = 1;
                var list = AllParents;
                if (list != null && list.Count > 0) _Deepth += list.Count;
                return _Deepth;
            }
        }

        private static TEntity _Root;
        /// <summary>根</summary>
        public static TEntity Root
        {
            get
            {
                if (_Root == null)
                {
                    _Root = new TEntity();
                    Meta.OnDataChange += delegate { _Root = null; };
                }
                return _Root;
            }
            set { _Root = null; }
        }

        /// <summary>节点名</summary>
        [XmlIgnore]
        public virtual String NodeName
        {
            get
            {
                String key = NameKeyName;
                if (String.IsNullOrEmpty(key)) return String.Empty;

                return (String)this[key];
            }
        }

        /// <summary>树形节点名，根据深度带全角空格前缀</summary>
        [XmlIgnore]
        public virtual String TreeNodeName
        {
            get
            {
                String key = NameKeyName;
                if (String.IsNullOrEmpty(key)) return String.Empty;

                Int32 d = Deepth;
                if (d <= 0) return String.Empty;

                return new String('　', (d - 1) * 2) + this[key];
            }
        }

        /// <summary>树形节点名，根据深度带全角空格前缀</summary>
        [XmlIgnore]
        public virtual String TreeNodeName2
        {
            get
            {
                String key = NameKeyName;
                if (String.IsNullOrEmpty(key)) return String.Empty;

                Int32 d = Deepth;
                if (d <= 0) return "|- 根";

                return new String('　', d) + "|- " + this[key];
            }
        }

        /// <summary>斜杠分隔的全路径</summary>
        [XmlIgnore]
        public String FullPath { get { return GetFullPath2(true); } }

        /// <summary>斜杠分隔的全父路径</summary>
        [XmlIgnore]
        public String FullParentPath { get { return GetFullPath2(false); } }
        #endregion

        #region 查询
        /// <summary>根据父级查找所有子级，带排序功能</summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static EntityList<TEntity> FindAllByParent(TKey parentKey)
        {
            TEntity entity = Meta.Factory.Default as TEntity;

            EntityList<TEntity> list = Meta.Cache.Entities.FindAll(entity.ParentKeyName, parentKey);
            // 如果是顶级，那么包含所有无头节点
            if (IsNull(parentKey))
            {
                EntityList<TEntity> noParents = FindAllNoParent();
                if (noParents != null && noParents.Count > 0)
                {
                    if (list == null || list.Count < 1)
                        list = noParents;
                    else
                        list.AddRange(noParents);
                }
            }
            if (list == null) return new EntityList<TEntity>();
            if (list.Count < 1) return list;

            if (!String.IsNullOrEmpty(entity.SortingKeyName))
            {
                list.Sort(delegate(TEntity item1, TEntity item2)
                {
                    if (item1.Sort != item2.Sort)
                        return -1 * item1.Sort.CompareTo(item2.Sort);
                    else
                        return (item1[entity.KeyName] as IComparable).CompareTo(item2[entity.KeyName]);
                });
            }
            return list;
        }

        /// <summary>查找所有没有父节点的节点集合（其实就是父节点已经被删掉了的非法节点）</summary>
        /// <returns></returns>
        public static EntityList<TEntity> FindAllNoParent()
        {
            TEntity entity = Meta.Factory.Default as TEntity;

            EntityList<TEntity> list = new EntityList<TEntity>();
            foreach (TEntity item in Meta.Cache.Entities)
            {
                // 有父节点的跳过
                if (item.Parent != null) continue;
                // 父节点为空的跳过
                if (IsNull((TKey)item[entity.ParentKeyName])) continue;

                list.Add(item);
            }
            return list;
        }

        /// <summary>查找指定键的所有子节点，以深度层次树结构输出，包括当前节点，并作为根节点</summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static EntityList<TEntity> FindAllChildsByParent(TKey parentKey)
        {
            TEntity entity = FindByKeyWithCache(parentKey);
            if (entity == null) entity = Root;

            EntityList<TEntity> list = FindAllChilds(entity);
            list.Insert(0, entity);
            return list;
        }

        /// <summary>查找指定键的所有子节点，以深度层次树结构输出</summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static EntityList<TEntity> FindAllChildsNoParent(TKey parentKey)
        {
            TEntity entity = FindByKeyWithCache(parentKey);
            if (entity == null) entity = Root;

            return FindAllChilds(entity);
        }

        /// <summary>查找指定键的所有父节点，从高到底以深度层次树结构输出</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static EntityList<TEntity> FindAllParentsByKey(TKey key)
        {
            TEntity entity = FindByKeyWithCache(key);
            if (entity == null) entity = Root;

            return FindAllParents(entity);
        }
        #endregion

        #region 树形计算
        /// <summary>查找指定节点的所有子节点，以深度层次树结构输出</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected static EntityList<TEntity> FindAllChilds(IEntityTree entity)
        {
            if (entity == null) return new EntityList<TEntity>();
            IEntityList childlist = entity.Childs;
            if (childlist == null) return new EntityList<TEntity>();

            EntityList<TEntity> list = new EntityList<TEntity>();
            // 使用队列而不使用递归，避免死循环
            // 使用队列而不使用堆栈，因为树的构造一般是深度搜索而不是广度搜索
            Stack<TEntity> stack = new Stack<TEntity>();
            stack.Push(entity as TEntity);

            while (stack.Count > 0)
            {
                TEntity item = stack.Pop();
                if (list.Contains(item)) continue;
                list.Add(item);

                EntityList<TEntity> childs = item.Childs;
                if (childs == null || childs.Count < 1) continue;

                // 反向入队
                for (int i = childs.Count - 1; i >= 0; i--)
                {
                    // 已计算到结果的，不再处理
                    if (list.Contains(childs[i])) continue;
                    // 已进入待处理队列的，不再处理
                    if (stack.Contains(childs[i])) continue;

                    stack.Push(childs[i]);
                }
            }
            // 去掉第一个，那是自身
            list.RemoveAt(0);

            return list;
        }

        /// <summary>查找指定节点的所有父节点，从高到底以深度层次树结构输出</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected static EntityList<TEntity> FindAllParents(IEntityTree entity)
        {
            if (entity == null || entity.Parent == null) return new EntityList<TEntity>();

            EntityList<TEntity> list = new EntityList<TEntity>();
            TEntity item = entity as TEntity;
            while (item != null)
            {
                // 形成了死循环，就此中断
                if (list.Contains(item)) break;

                list.Add(item);

                item = item.Parent;
            }
            // 去掉第一个自己
            list.RemoveAt(0);

            // 反转
            if (list.Count > 0) list.Reverse();

            return list;
        }

        /// <summary>根据层次路径查找</summary>
        /// <param name="path">层次路径</param>
        /// <param name="keys">用于在每一层匹配实体的键值，默认是NameKeyName</param>
        /// <returns></returns>
        public TEntity FindByPath(String path, params String[] keys)
        {
            if (String.IsNullOrEmpty(path)) return null;

            if (keys == null || keys.Length < 1)
            {
                if (String.IsNullOrEmpty(NameKeyName)) return null;

                keys = new String[] { NameKeyName };
            }

            EntityList<TEntity> list = Childs;
            if (list == null || list.Count < 1) return null;

            //// 尝试一次性查找
            //TEntity entity = list.Find(name, path);
            //if (entity != null) return entity;

            String[] ss = path.Split(new Char[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            // 找第一级
            TEntity entity = null;
            foreach (String item in keys)
            {
                entity = list.Find(item, ss[0]);
                if (entity != null) break;
            }
            if (entity == null) return null;

            // 是否还有下级
            if (ss.Length == 1) return entity;

            // 递归找下级
            return entity.FindByPath(String.Join("\\", ss, 1, ss.Length - 1), keys);
        }
        #endregion

        #region 集合运算
        /// <summary>
        /// 是否包含子节点
        /// </summary>
        /// <param name="key">子节点键值</param>
        /// <returns></returns>
        public Boolean Contains(TKey key)
        {
            // 判断空
            if (IsNull(key)) return false;

            // 自身
            if (Object.Equals((TKey)this[KeyName], key)) return true;

            // 子级
            var list = Childs;
            if (list != null && list.Exists(KeyName, key)) return true;

            // 子孙
            list = AllChilds;
            if (list != null && list.Exists(KeyName, key)) return true;

            return false;
        }

        /// <summary>
        /// 子级键值集合
        /// </summary>
        [XmlIgnore]
        public List<TKey> ChildKeys
        {
            get
            {
                var list = Childs;
                if (list == null || list.Count < 1) return new List<TKey>();

                return list.GetItem<TKey>(KeyName);
            }
        }

        /// <summary>
        /// 逗号分隔的子级键值字符串，一般可用于SQL语句中
        /// </summary>
        [XmlIgnore]
        public String ChildKeyString
        {
            get
            {
                List<TKey> list = ChildKeys;
                if (list == null || list.Count < 1) return null;

                StringBuilder sb = new StringBuilder();
                foreach (TKey item in list)
                {
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(item.ToString());
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 子孙键值集合
        /// </summary>
        [XmlIgnore]
        public List<TKey> AllChildKeys
        {
            get
            {
                var list = AllChilds;
                if (list == null || list.Count < 1) return new List<TKey>();

                return list.GetItem<TKey>(KeyName);
            }
        }

        /// <summary>
        /// 逗号分隔的子孙键值字符串，一般可用于SQL语句中
        /// </summary>
        [XmlIgnore]
        public String AllChildKeyString
        {
            get
            {
                List<TKey> list = AllChildKeys;
                if (list == null || list.Count < 1) return null;

                StringBuilder sb = new StringBuilder();
                foreach (TKey item in list)
                {
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(item.ToString());
                }
                return sb.ToString();
            }
        }
        #endregion

        #region 业务
        /// <summary>
        /// 创建菜单树
        /// </summary>
        /// <param name="nodes">父集合</param>
        /// <param name="list">菜单列表</param>
        /// <param name="url">格式化地址，可以使用{ID}和{Name}</param>
        /// <param name="func">由菜单项创建树节点的委托</param>
        public static void MakeTree(TreeNodeCollection nodes, EntityList<TEntity> list, String url, Func<TEntity, TreeNode> func)
        {
            if (list == null || list.Count < 1) return;

            // 使用内层递归，避免死循环
            MakeTree(nodes, list, url, func, new EntityList<TEntity>());
        }

        private static void MakeTree(TreeNodeCollection nodes, EntityList<TEntity> list, String url, Func<TEntity, TreeNode> func, EntityList<TEntity> parents)
        {
            String id = Meta.FieldNames[0];
            String name = Meta.FieldNames[1];
            if (Meta.FieldNames.Contains("Name")) name = "Name";
            if (Meta.Unique != null)
                id = Meta.Unique.Name;
            else if (Meta.FieldNames.Contains("ID"))
                id = "ID";

            foreach (TEntity item in list)
            {
                if (parents.Contains(item) || parents.Exists(id, item[id])) continue;
                parents.Add(item);

                TreeNode node = null;
                if (func == null)
                {
                    node = new TreeNode((String)item[name]);
                    node.Value = "" + item[id];
                    if (!String.IsNullOrEmpty(url))
                    {
                        foreach (String elm in Meta.FieldNames)
                        {
                            url = url.Replace("{" + elm + "}", "" + item[elm]);
                        }
                        node.NavigateUrl = url;
                    }
                }
                else
                {
                    node = func(item);
                }

                var list2 = item.Childs;
                if (list2 != null && list2.Count > 0) MakeTree(node.ChildNodes, list2, url, func, parents);

                if (node != null) nodes.Add(node);
            }
        }

        /// <summary>
        /// 取得全路径的实体，由上向下排序
        /// </summary>
        /// <param name="includeSelf"></param>
        /// <returns></returns>
        public EntityList<TEntity> GetFullPath(Boolean includeSelf)
        {
            EntityList<TEntity> list = AllParents;

            if (!includeSelf) return list;

            // 绝对不能让list直接等于AllParents，否则后面会加一项进去，导致改变它的值

            EntityList<TEntity> list2 = new EntityList<TEntity>();
            if (list != null && list.Count > 0) list2.AddRange(list);
            list2.Add(this as TEntity);

            return list2;
        }

        /// <summary>
        /// 取得全路径的实体，由上向下排序
        /// </summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        public String GetFullPath(Boolean includeSelf = true, String separator = @"\", Func<TEntity, String> func = null)
        {
            EntityList<TEntity> list = GetFullPath(includeSelf);
            if (list == null || list.Count < 1) return null;

            String namekey = NameKeyName;

            StringBuilder sb = new StringBuilder();
            foreach (TEntity item in list)
            {
                if (sb.Length > 0 && !String.IsNullOrEmpty(separator)) sb.Append(separator);
                if (func != null)
                    sb.Append(func(item));
                else
                {
                    if (String.IsNullOrEmpty(namekey))
                        sb.Append(item.ToString());
                    else
                        sb.Append(item[namekey]);
                }
            }
            return sb.ToString();
        }

        String GetFullPath2(Boolean includeSelf = true, String separator = @"\", Func<TEntity, String> func = null)
        {
            EntityList<TEntity> list = GetFullPath(includeSelf);
            if (list == null || list.Count < 1) return separator;

            String namekey = NameKeyName;

            StringBuilder sb = new StringBuilder();
            foreach (TEntity item in list)
            {
                if (!String.IsNullOrEmpty(separator)) sb.Append(separator);
                if (func != null)
                    sb.Append(func(item));
                else
                {
                    if (String.IsNullOrEmpty(namekey))
                        sb.Append(item.ToString());
                    else
                        sb.Append(item[namekey]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 删除子级到本级的关系。导出数据前可以先删除关系，以减少导出的大小
        /// </summary>
        public virtual void ClearRelation()
        {
            var list = Childs;
            if (list == null || list.Count < 1) return;

            foreach (TEntity item in list)
            {
                item[KeyName] = default(TKey);
                item[ParentKeyName] = default(TKey);

                item.ClearRelation();
            }
        }

        /// <summary>
        /// 批量保存，保存整棵树
        /// </summary>
        /// <param name="saveSelf">是否保存自己</param>
        /// <returns></returns>
        public virtual Int32 BatchSave(Boolean saveSelf)
        {
            Int32 count = 0;

            Meta.BeginTrans();
            try
            {
                EntityList<TEntity> list = Childs;
                if (saveSelf) count += Save();
                // 上面保存数据后，可能会引起扩展属性抖动（不断更新）
                if (list != null && list.Count > 0)
                {
                    foreach (TEntity item in list)
                    {
                        item[ParentKeyName] = this[KeyName];
                        count += item.BatchSave(true);
                    }
                }

                Meta.Commit();

                return count;
            }
            catch
            {
                Meta.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 排序上升
        /// </summary>
        public void Up()
        {
            EntityList<TEntity> list = FindAllByParent((TKey)this[ParentKeyName]);
            if (list == null || list.Count < 1) return;

            for (int i = 0; i < list.Count; i++)
            {
                Int32 s = list.Count - i;
                // 当前项，排序增加。原来比较实体相等有问题，也许新旧实体类不对应，现在改为比较主键值
                if (this.EqualTo(list[i])) s++;
                // 下一项是当前项，排序减少
                if (i < list.Count - 1 && this.EqualTo(list[i + 1])) s--;
                list[i].Sort = s;
            }
            list.Save();
        }

        /// <summary>
        /// 排序下降
        /// </summary>
        public void Down()
        {
            EntityList<TEntity> list = FindAllByParent((TKey)this[ParentKeyName]);
            if (list == null || list.Count < 1) return;

            for (int i = 0; i < list.Count; i++)
            {
                Int32 s = list.Count - i;
                // 当前项，排序减少
                if (this.EqualTo(list[i])) s--;
                // 上一项是当前项，排序增加
                if (i >= 1 && this.EqualTo(list[i - 1])) s++;
                list[i].Sort = s;
            }
            list.Save();
        }

        Boolean EqualTo(TEntity entity)
        {
            if (entity == null) return false;

            Object v1 = this[KeyName];
            Object v2 = entity[KeyName];
            if (typeof(TKey) == typeof(String)) return "" + v1 == "" + v2;

            return Object.Equals(v1, v2);
        }
        #endregion

        #region 数据检查
        /// <summary>
        /// 验证树形数据是否有效
        /// </summary>
        /// <param name="isNew">是否新数据</param>
        public override void Valid(Boolean isNew)
        {
            base.Valid(isNew);

            TKey key = (TKey)this[KeyName];
            TKey pkey = (TKey)this[ParentKeyName];

            Boolean isnull = IsNull(key);
            Boolean pisnull = IsNull(pkey);

            // 无主检查
            //if (!pisnull && !Meta.Cache.Entities.Exists(KeyName, pkey)) throw new Exception("无效上级[" + pkey + "]！");
            if (!pisnull && FindCount(KeyName, pkey) <= 0) throw new XException("无效上级[" + pkey + "]！");

            // 死循环检查
            if (isnull)
            {
                // 插入状态，key为空，pkey可以是任何值
            }
            else
            {
                // 更新状态，且pkey不为空时，判断两者是否相等
                if (!pisnull && Object.Equals(pkey, key)) throw new XException("上级不能是当前节点！");
            }

            // 编辑状态且设置了父节点时才处理
            if (!isnull && !pisnull)
            {
                EntityList<TEntity> list = this.AllChilds;
                if (list != null && list.Exists(KeyName, pkey))
                    throw new XException("上级[" + pkey + "]是当前节点的子孙节点！");
            }
        }

        private static Boolean IsNull(TKey value)
        {
            // 为空或者默认值，返回空
            if (value == null || Object.Equals(value, default(TKey))) return true;

            // 字符串的空
            if (typeof(TKey) == typeof(String) && String.IsNullOrEmpty(value.ToString())) return true;

            return false;
        }
        #endregion

        #region IEntityTree 成员
        /// <summary>父实体</summary>
        IEntity IEntityTree.Parent { get { return Parent; } }

        /// <summary>子实体集合</summary>
        IEntityList IEntityTree.Childs { get { return Childs; } }

        /// <summary>子孙实体集合。以深度层次树结构输出</summary>
        IEntityList IEntityTree.AllChilds { get { return AllChilds; } }

        /// <summary>父亲实体集合。以深度层次树结构输出</summary>
        IEntityList IEntityTree.AllParents { get { return AllParents; } }
        #endregion
    }
}