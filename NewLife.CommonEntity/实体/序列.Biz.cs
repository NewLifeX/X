/*
 * XCoder v3.4.2011.0329
 * 作者：nnhy/X
 * 时间：2011-06-21 21:07:11
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>序列种类</summary>
    public enum SequenceKinds
    {
        /// <summary>全局</summary>
        Global,

        /// <summary>年</summary>
        Year,

        /// <summary>月</summary>
        Month,

        /// <summary>日</summary>
        Day
    }

    /// <summary>序列</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Sequence : Sequence<Sequence> { }

    /// <summary>序列</summary>
    [BindIndex("IX_Sequence_Name", true, "Name")]
    [BindIndex("PK__Sequence__3214EC270EA330E9", true, "ID")]
    public partial class Sequence<TEntity> : Entity<TEntity> where TEntity : Sequence<TEntity>, new()
    {
        #region 对象操作
        static Sequence()
        {
            Meta.SingleCache.FindKeyMethod = delegate(Object name) { return Find(_.Name, name); };
        }

        /// <summary>验证</summary>
        /// <param name="isNew"></param>
        public override void Valid(bool isNew)
        {
            base.Valid(isNew);

            if (isNew || Dirtys[_.Name] || Dirtys[_.Kind] || Dirtys[_.Num]) LastUpdate = DateTime.Now;
        }
        #endregion

        #region 扩展属性
        // 本类与哪些类有关联，可以在这里放置一个属性，使用延迟加载的方式获取关联对象

        /*
        private Category _Category;
        /// <summary>该商品所对应的类别</summary>
        [XmlIgnore]
        public Category Category
        {
            get
            {
                if (_Category == null && CategoryID > 0 && !Dirtys.ContainsKey("Category"))
                {
                    _Category = Category.FindByKey(CategoryID);
                    Dirtys.Add("Category", true);
                }
                return _Category;
            }
            set { _Category = value; }
        }
         * */
        #endregion

        #region 扩展查询
        /// <summary>根据主键查询一个序列实体对象用于表单编辑</summary>
        ///<param name="__ID">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKeyForEdit(Int32 __ID)
        {
            TEntity entity = Find(new String[] { _.ID }, new Object[] { __ID });
            if (entity == null)
            {
                entity = new TEntity();
            }
            return entity;
        }

        /// <summary>根据编号查找</summary>
        /// <param name="__ID"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 __ID)
        {
            return Find(_.ID, __ID);
            // 实体缓存
            //return Meta.Cache.Entities.Find(_.ID, __ID);
            // 单对象缓存
            //return Meta.SingleCache[__ID];
        }

        /// <summary>根据名称查找</summary>
        /// <param name="__Name"></param>
        /// <returns></returns>
        public static TEntity FindByName(String __Name)
        {
            if (String.IsNullOrEmpty(__Name)) return null;
            //取消缓存，防止出现缓存过期无法获得对象
            return Find(_.Name, __Name);
            // 实体缓存
            //return Meta.Cache.Entities.Find(_.Name, __Name);
            // 单对象缓存
            //return Meta.SingleCache[__Name];
        }
        #endregion

        #region 高级查询
        /// <summary>查询满足条件的记录集，分页、排序</summary>
        /// <param name="key">关键字</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体集</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public static new EntityList<TEntity> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAll(SearchWhere(key), orderClause, null, startRowIndex, maximumRows);
        }

        /// <summary>查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一</summary>
        /// <param name="key">关键字</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>记录数</returns>
        public static new Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCount(SearchWhere(key), null, null, 0, 0);
        }

        /// <summary>构造搜索条件</summary>
        /// <param name="key">关键字</param>
        /// <returns></returns>
        private static String SearchWhere(String key)
        {
            // WhereExpression重载&和|运算符，作为And和Or的替代
            var exp = SearchWhereByKeys(key);

            // 以下仅为演示，2、3行是同一个意思的不同写法，FieldItem重载了等于以外的运算符（第4行）
            //exp &= _.Name.Equal("testName")
            //    & !String.IsNullOrEmpty(key) & _.Name.Equal(key)
            //    .AndIf(!String.IsNullOrEmpty(key), _.Name.Equal(key))
            //    | _.ID > 0;

            return exp;
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        private static Object objLock = new Object();
        /// <summary>获取</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Int32 Acquire(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            lock (objLock)
            {
                TEntity entity = FindByName(name);
                //if (entity == null) throw new ArgumentOutOfRangeException("name", "无法找到名为" + name + "的序列！");
                if (entity == null)
                {
                    entity = new TEntity();
                    entity.Name = name;
                    entity.Save();

                    // 再拿一次，让它进入单对象缓存
                    entity = FindByName(name);
                }

                //return entity.Acquire();

                SequenceKinds kind = (SequenceKinds)entity.Kind;
                switch (kind)
                {
                    case SequenceKinds.Global:
                        break;
                    case SequenceKinds.Year:
                        if (entity.LastUpdate.Year != DateTime.Now.Year) entity.Num = 0;
                        break;
                    case SequenceKinds.Month:
                        if (entity.LastUpdate.Year != DateTime.Now.Year ||
                            entity.LastUpdate.Month != DateTime.Now.Month) entity.Num = 0;
                        break;
                    case SequenceKinds.Day:
                        if (entity.LastUpdate.Year != DateTime.Now.Year ||
                            entity.LastUpdate.Month != DateTime.Now.Month ||
                            entity.LastUpdate.Day != DateTime.Now.Day) entity.Num = 0;
                        break;
                    default:
                        break;
                }

                entity.Num++;
                entity.Save();

                return entity.Num;
            }
        }

        ///// <summary>
        ///// 获取
        ///// </summary>
        ///// <returns></returns>
        //public Int32 Acquire()
        //{
        //    lock (objLock)
        //    {
        //        Num++;
        //        Save();

        //        return Num;
        //    }
        //}

        /// <summary>设置序列类型，序列不存在时自动增加</summary>
        /// <param name="name">名称</param>
        /// <param name="kind"></param>
        public static void SetKind(String name, SequenceKinds kind)
        {
            TEntity entity = FindByName(name);
            if (entity == null)
            {
                entity = new TEntity();
                entity.Name = name;
            }
            entity.Kind = (Int32)kind;
            entity.Save();
        }
        #endregion
    }
}