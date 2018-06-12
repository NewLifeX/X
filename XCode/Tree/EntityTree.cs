using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Collections;

namespace XCode
{
    /// <summary>主键为整型的实体树基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable]
    public class EntityTree<TEntity> : EntityTree<Int32, TEntity> where TEntity : EntityTree<TEntity>, new()
    { }

    /// <summary>实体树基类，具有树形结构的实体继承该类即可得到各种树操作功能</summary>
    /// <remarks>
    /// 实体树很神奇，子类可以通过KeyName、ParentKeyName、SortingKeyName、NameKeyName等设置型属性，
    /// 指定关联键、关联父键、排序键、名称键，其中前两个是必须的，它们是构造一棵树的根基！
    /// 
    /// 整个表会形成一颗实体树，同时也是一个实体列表，子级紧靠父级，同级排序，<see cref="Root"/>就是这棵树的根。
    /// 所以，Root.Childs可以得到顶级节点集合，Root.AllChilds得到整棵树。
    /// </remarks>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    [Serializable]
    public abstract partial class EntityTree<TKey, TEntity> : Entity<TEntity>, IEntityTree where TEntity : EntityTree<TKey, TEntity>, new()
    {
        #region 静态构造
        static EntityTree()
        {
            // 避免实际应用中，直接调用Entity的静态方法时，没有引发TEntity的静态构造函数。
            var entity = new TEntity();

            // 更方便实体树子类重载树形设置
            if (Setting == null) Setting = new EntityTreeSetting<TEntity> { Factory = Meta.Factory };
        }

        /// <summary>实体树操作者</summary>
        protected static IEntityTreeSetting Setting;
        #endregion

        #region 扩展属性
        /// <summary>排序值</summary>
        private Int32 Sort
        {
            get { return String.IsNullOrEmpty(Setting.Sort) ? 0 : (Int32)this[Setting.Sort]; }
            set { if (!String.IsNullOrEmpty(Setting.Sort) && (Int32)this[Setting.Sort] != value) SetItem(Setting.Sort, value); }
        }

        /// <summary>子节点</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual IList<TEntity> Childs
        {
            get { return Extends.Get(nameof(Childs), e => FindChilds()); }
            set { Extends.Set(nameof(Childs), value); }
        }

        /// <summary>子节点</summary>
        protected virtual IList<TEntity> FindChilds() { return FindAllByParent((TKey)this[Setting.Key]); }

        /// <summary>父节点</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual TEntity Parent
        {
            get { return Extends.Get(nameof(Parent), e => FindParent()); }
            set { Extends.Set(nameof(Parent), value); }
        }

        /// <summary>父节点</summary>
        protected virtual TEntity FindParent()
        {
            return FindByKeyWithCache((TKey)this[Setting.Parent]);
        }

        /// <summary>在缓存中查找节点</summary>
        protected static TEntity FindByKeyWithCache(TKey key)
        {
            return Meta.Session.Cache.Find(e => Equals(e[Setting.Key], key));
        }

        /// <summary>子孙节点</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual IList<TEntity> AllChilds
        {
            get { return Extends.Get(nameof(AllChilds), e => FindAllChilds(this)); }
            set { Extends.Set(nameof(AllChilds), value); }
        }

        /// <summary>子孙节点，包含自己</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual IList<TEntity> MyAllChilds
        {
            get { return Extends.Get(nameof(MyAllChilds), e => FindAllChilds(this, true)); }
            set { Extends.Set(nameof(MyAllChilds), value); }
        }

        /// <summary>父节点集合</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual IList<TEntity> AllParents
        {
            get { return Extends.Get(nameof(AllParents), e => FindAllParents(this)); }
            set { Extends.Set(nameof(AllParents), value); }
        }

        /// <summary>深度</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual Int32 Deepth
        {
            get
            {
                if (IsNullKey) return 0;

                var _Deepth = 1;
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
                    Meta.Session.OnDataChange += delegate { _Root = null; };
                }
                return _Root;
            }
            set { _Root = null; }
        }

        /// <summary>节点名</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual String NodeName
        {
            get
            {
                var key = Setting.Name;
                if (String.IsNullOrEmpty(key)) return String.Empty;

                return (String)this[key];
            }
        }

        /// <summary>父级节点名</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual String ParentNodeName
        {
            get
            {
                var key = Setting.Name;
                if (String.IsNullOrEmpty(key)) return String.Empty;

                var parent = Parent;
                if (parent == null) return String.Empty;

                return (String)parent[key];
            }
        }

        /// <summary>树形节点名，根据深度带全角空格前缀</summary>
        [DisplayName("节点名")]
        [XmlIgnore, ScriptIgnore]
        public virtual String TreeNodeName
        {
            get
            {
                var key = Setting.Name;
                var v = "";
                if (!String.IsNullOrEmpty(key)) v = this[key] + "";

                var d = Deepth;
                if (d <= 0) return String.Empty;

                return new String('　', (d - 1) * 2) + v;
            }
        }

        /// <summary>树形节点名，根据深度带全角空格前缀</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual String TreeNodeName2
        {
            get
            {
                var key = Setting.Name;
                var v = "";
                if (!String.IsNullOrEmpty(key)) v = this[key] + "";

                var d = Deepth;
                if (d <= 0) return "|- 根";

                return new String('　', d) + "|- " + v;
            }
        }

        /// <summary>树形节点名，根据深度带全角空格前缀</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual String TreeNodeText
        {
            get
            {
                var key = Setting.Text;
                var v = "";
                if (!String.IsNullOrEmpty(key)) v = this[key] + "";
                if (String.IsNullOrEmpty(v))
                {
                    key = Setting.Name;
                    if (!String.IsNullOrEmpty(key)) v = this[key] + "";
                }

                var d = Deepth;
                if (d <= 0) return "|- 根";

                return new String('　', d) + "|- " + v;
            }
        }

        /// <summary>斜杠分隔的全路径</summary>
        [XmlIgnore, ScriptIgnore]
        public String FullPath { get { return @"\" + GetFullPath(true); } }

        /// <summary>斜杠分隔的全父路径</summary>
        [XmlIgnore, ScriptIgnore]
        public String FullParentPath { get { return @"\" + GetFullPath(false); } }
        #endregion

        #region 查询
        /// <summary>根据父级查找所有子级，带排序功能，先排序字段再主键</summary>
        /// <remarks>如果是顶级，那么包含所有无头节点，无头节点由错误数据造成</remarks>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static IList<TEntity> FindAllByParent(TKey parentKey)
        {
            var list = Meta.Session.Cache.FindAll(e => Equals(e[Setting.Parent], parentKey)).ToList();
            // 如果是顶级，那么包含所有无头节点，无头节点由错误数据造成
            if (IsNull(parentKey)) list.AddRange(FindAllNoParent());
            // 一个元素不需要排序
            if (list.Count <= 1) return list;

            if (!String.IsNullOrEmpty(Setting.Sort))
            {
                var n = Setting.BigSort ? 1 : -1;
                list.Sort((item1, item2) =>
                {
                    if (item1.Sort != item2.Sort)
                        return -n * item1.Sort.CompareTo(item2.Sort);
                    else
                        return (item1[Setting.Key] as IComparable).CompareTo(item2[Setting.Key]);
                });
            }
            return list;
        }

        /// <summary>查找所有无头节点（没有父节点的节点）集合（其实就是父节点已经被删掉了的非法节点）</summary>
        /// <returns></returns>
        public static IList<TEntity> FindAllNoParent()
        {
            // 有父节点的跳过，父节点为空的跳过
            return Meta.Session.Cache.FindAll(e => !IsNull((TKey)e[Setting.Parent]) && e.Parent == null);
        }

        /// <summary>查找指定键的所有子节点，以深度层次树结构输出，包括当前节点作为根节点。空父节点返回顶级列表，无效父节点返回空列表</summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static IList<TEntity> FindAllChildsByParent(TKey parentKey)
        {
            var entity = IsNull(parentKey) ? Root : FindByKeyWithCache(parentKey);
            if (entity == null) return new List<TEntity>();

            return FindAllChilds(entity, true);
        }

        /// <summary>查找指定键的所有子节点，以深度层次树结构输出。空父节点返回顶级列表，无效父节点返回空列表</summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static IList<TEntity> FindAllChildsNoParent(TKey parentKey)
        {
            var entity = IsNull(parentKey) ? Root : FindByKeyWithCache(parentKey);
            if (entity == null) return new List<TEntity>();

            return FindAllChilds(entity, false);
        }

        /// <summary>获取完整树，包含根节点，排除指定分支。多用于树节点父级选择</summary>
        /// <param name="exclude"></param>
        /// <returns></returns>
        public IList<TEntity> FindAllChildsExcept(IEntityTree exclude)
        {
            return FindAllChilds(this, true, exclude);
        }

        /// <summary>查找指定键的所有父节点，从高到底以深度层次树结构输出</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static IList<TEntity> FindAllParentsByKey(TKey key)
        {
            if (IsNull(key)) return new List<TEntity>();

            var entity = FindByKeyWithCache(key);
            if (entity == null) return new List<TEntity>();

            return FindAllParents(entity);
        }
        #endregion

        #region 树形计算
        /// <summary>查找指定节点的所有子节点，以深度层次树结构输出</summary>
        /// <param name="entity">根节点</param>
        /// <param name="includeSelf">返回列表是否包含根节点，默认false</param>
        /// <param name="exclude">要排除的节点</param>
        /// <returns></returns>
        protected static IList<TEntity> FindAllChilds(IEntityTree entity, Boolean includeSelf = false, IEntityTree exclude = null)
        {
            if (entity == null) return new List<TEntity>();
            var childlist = entity.Childs;
            if (childlist == null) return new List<TEntity>();

            var list = new List<TEntity>();
            // 不使用递归，避免死循环
            // 使用堆栈而不使用队列，因为树的构造一般是深度搜索而不是广度搜索
            var stack = new Stack<TEntity>();
            stack.Push(entity as TEntity);

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                if (list.Contains(item)) continue;
                // 排除某节点以及它的子孙节点。不能直接判断对象相等，因为其中一边可能来自缓存
                if (exclude != null && !exclude.IsNullKey && item.EqualTo(exclude)) continue;
                // 去掉第一个，那是自身
                if (includeSelf || item != entity) list.Add(item);

                var childs = item.Childs;
                if (childs == null || childs.Count < 1) continue;

                // 反向入队
                for (var i = childs.Count - 1; i >= 0; i--)
                {
                    // 已计算到结果的，不再处理
                    if (list.Contains(childs[i])) continue;
                    // 已进入待处理队列的，不再处理
                    if (stack.Contains(childs[i])) continue;

                    stack.Push(childs[i]);
                }
            }
            //// 去掉第一个，那是自身
            //list.RemoveAt(0);

            return list;
        }

        /// <summary>查找指定节点的所有父节点，从高到底以深度层次树结构输出</summary>
        /// <param name="entity"></param>
        /// <param name="includeSelf">返回列表是否包含根节点，默认false</param>
        /// <returns></returns>
        protected static IList<TEntity> FindAllParents(IEntityTree entity, Boolean includeSelf = false)
        {
            if (entity == null || IsNull((TKey)entity[Setting.Parent]) || entity.Parent == null) return new List<TEntity>();

            var list = new List<TEntity>();
            var item = entity as TEntity;
            while (item != null)
            {
                // 形成了死循环，就此中断
                if (list.Contains(item)) break;

                if (includeSelf || item != entity) list.Add(item);

                item = item.Parent;
            }
            //// 去掉第一个自己
            //list.RemoveAt(0);

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
                if (String.IsNullOrEmpty(Setting.Name)) return null;

                keys = new String[] { Setting.Name };
            }

            //var list = Childs;
            var list = FindChilds();
            if (list == null || list.Count < 1) return null;

            //// 尝试一次性查找
            //TEntity entity = list.Find(name, path);
            //if (entity != null) return entity;

            var ss = path.Split(new Char[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            // 找第一级
            TEntity entity = null;
            foreach (var item in keys)
            {
                //entity = list.Find(item, ss[0]);
                entity = list.FirstOrDefault(e => (String)e[item] == ss[0]);
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
        /// <summary>是否包含子节点</summary>
        /// <param name="key">子节点键值</param>
        /// <returns></returns>
        public Boolean Contains(TKey key)
        {
            // 判断空
            if (IsNull(key)) return false;

            // 自身
            if (Equals((TKey)this[Setting.Key], key)) return true;

            // 子级
            var list = Childs;
            if (list != null && list.Any(e => Equals(e[Setting.Key], key))) return true;

            // 子孙
            list = AllChilds;
            if (list != null && list.Any(e => Equals(e[Setting.Key], key))) return true;

            return false;
        }
        #endregion

        #region 业务
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        public String GetFullPath(Boolean includeSelf = true, String separator = @"\", Func<TEntity, String> func = null)
        {
            var list = FindAllParents(this, includeSelf);
            if (list == null || list.Count < 1) return null;

            var namekey = Setting.Name;

            var sb = Pool.StringBuilder.Get();
            foreach (var item in list)
            {
                sb.Separate(separator);
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
            return sb.Put(true);
        }

        /// <summary>删除子级到本级的关系。导出数据前可以先删除关系，以减少导出的大小</summary>
        public virtual void ClearRelation()
        {
            var list = Childs;
            if (list == null || list.Count < 1) return;

            foreach (var item in list)
            {
                item[Setting.Key] = default(TKey);
                item[Setting.Parent] = default(TKey);

                item.ClearRelation();
            }
        }

        ///// <summary>批量保存，保存整棵树</summary>
        ///// <param name="saveSelf">是否保存自己</param>
        ///// <returns></returns>
        //public virtual Int32 BatchSave(Boolean saveSelf)
        //{
        //    var count = 0;

        //    using (var trans = new EntityTransaction<TEntity>())
        //    {
        //        var list = Childs;
        //        if (saveSelf) count += Save();
        //        // 上面保存数据后，可能会引起扩展属性抖动（不断更新）
        //        if (list != null && list.Count > 0)
        //        {
        //            foreach (var item in list)
        //            {
        //                item[Setting.Parent] = this[Setting.Key];
        //                count += item.BatchSave(true);
        //            }
        //        }

        //        trans.Commit();

        //        return count;
        //    }
        //}

        /// <summary>排序上升</summary>
        public void Up()
        {
            var list = FindAllByParent((TKey)this[Setting.Parent]);
            if (list == null || list.Count < 1) return;

            var n = Setting.BigSort ? 1 : -1;

            for (var i = 0; i < list.Count; i++)
            {
                var s = list.Count - i;
                // 当前项，排序增加。原来比较实体相等有问题，也许新旧实体类不对应，现在改为比较主键值
                if (EqualTo(list[i])) s += n;
                // 下一项是当前项，排序减少
                if (i < list.Count - 1 && EqualTo(list[i + 1])) s -= n;
                list[i].Sort = s;
            }
            list.Save();
        }

        /// <summary>排序下降</summary>
        public void Down()
        {
            var list = FindAllByParent((TKey)this[Setting.Parent]);
            if (list == null || list.Count < 1) return;

            var n = Setting.BigSort ? 1 : -1;

            for (var i = 0; i < list.Count; i++)
            {
                var s = list.Count - i;
                // 当前项，排序减少
                if (EqualTo(list[i])) s -= n;
                // 上一项是当前项，排序增加
                if (i >= 1 && EqualTo(list[i - 1])) s += n;
                list[i].Sort = s;
            }
            list.Save();
        }

        Boolean EqualTo(IEntity entity)
        {
            if (entity == null) return false;

            var v1 = this[Setting.Key];
            var v2 = entity[Setting.Key];
            if (typeof(TKey) == typeof(String)) return "" + v1 == "" + v2;

            return Equals(v1, v2);
        }
        #endregion

        #region 数据检查
        /// <summary>验证树形数据是否有效</summary>
        /// <param name="isNew">是否新数据</param>
        public override void Valid(Boolean isNew)
        {
            base.Valid(isNew);

            var key = (TKey)this[Setting.Key];
            var pkey = (TKey)this[Setting.Parent];

            var isnull = IsNull(key);
            var pisnull = IsNull(pkey);

            // 无主检查
            //if (!pisnull && !Meta.Cache.Entities.Exists(KeyName, pkey)) throw new Exception("无效上级[" + pkey + "]！");
            // 先检查实体缓存，可以命中绝大部分，因为绝大多数时候父级都存在
            if (!pisnull)
            {
                //if (!Meta.Cache.Entities.Exists(KeyName, pkey) && FindCount(KeyName, pkey) <= 0) throw new XException("无效上级[" + pkey + "]！");

                // 先查缓存再查数据库
                //var parent = FindByKeyWithCache(pkey);
                //if (parent == null) parent = Find(Setting.Key, pkey);
                //if (parent == null) throw new XException("无效上级[" + pkey + "]！");

                // 检查最大深度
                //var maxdeepth = Setting.MaxDeepth;
                //if (maxdeepth > 0)
                //{
                //    if (parent.Deepth >= maxdeepth) throw new XException("已达到最大深度" + maxdeepth + "层！");
                //}
            }

            // 死循环检查
            if (isnull)
            {
                // 插入状态，key为空，pkey可以是任何值
            }
            else
            {
                // 更新状态，且pkey不为空时，判断两者是否相等
                if (!pisnull && Equals(pkey, key)) throw new XException("上级不能是当前节点！");
            }

            // 编辑状态且设置了父节点时才处理
            if (!isnull && !pisnull)
            {
                var list = AllChilds;
                if (list != null && list.Any(e => Equals(e[Setting.Key], pkey)))
                    throw new XException("上级[" + pkey + "]是当前节点的子孙节点！");
            }
        }

        private static Boolean IsNull(TKey value)
        {
            // 为空或者默认值，返回空
            if (value == null || Equals(value, default(TKey))) return true;

            // 字符串的空
            if (typeof(TKey) == typeof(String) && String.IsNullOrEmpty(value.ToString())) return true;

            return false;
        }

        //private Boolean IsNullKey { get { return IsNull((TKey)this[Setting.Key]); } }
        #endregion

        #region IEntityTree 成员
        /// <summary>父实体</summary>
        IEntity IEntityTree.Parent { get { return Parent; } }

        /// <summary>子实体集合</summary>
        IList<IEntity> IEntityTree.Childs { get { return Childs.Cast<IEntity>().ToList(); } }

        /// <summary>子孙实体集合。以深度层次树结构输出</summary>
        IList<IEntity> IEntityTree.AllChilds { get { return AllChilds.Cast<IEntity>().ToList(); } }

        /// <summary>父亲实体集合。以深度层次树结构输出</summary>
        IList<IEntity> IEntityTree.AllParents { get { return AllParents.Cast<IEntity>().ToList(); } }

        /// <summary>获取完整树，包含根节点，排除指定分支。多用于树节点父级选择</summary>
        /// <param name="exclude"></param>
        /// <returns></returns>
        IList<IEntity> IEntityTree.FindAllChildsExcept(IEntityTree exclude) { return FindAllChildsExcept(exclude).Cast<IEntity>().ToList(); }
        #endregion
    }
}