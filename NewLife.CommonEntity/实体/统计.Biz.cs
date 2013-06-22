/*
 * XCoder v3.2.2010.1014
 * 作者：nnhy/NEWLIFE
 * 时间：2010-12-08 16:22:31
 * 版权：版权所有 (C) 新生命开发团队 2010
*/

using System;
using System.ComponentModel;
using System.Web;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>统计</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Statistics : Statistics<Statistics> { }

    /// <summary>统计</summary>
    [BindIndex("PK__Statisti__3214EC270AD2A005", true, "ID")]
    public partial class Statistics<TEntity> : Entity<TEntity> where TEntity : Statistics<TEntity>, new()
    {
        #region 对象操作
        static Statistics()
        {
            AdditionalFields.Add(_.Total);
            AdditionalFields.Add(_.Today);
            AdditionalFields.Add(_.ThisWeek);
            AdditionalFields.Add(_.ThisMonth);
            AdditionalFields.Add(_.ThisYear);
        }
        #endregion

        #region 扩展属性
        // 本类与哪些类有关联，可以在这里放置一个属性，使用延迟加载的方式获取关联对象

        /*
        private Category _Category;
        /// <summary>该商品所对应的类别</summary>
        public Category Category
        {
            get
            {
                if (_Category == null && CategoryID > 0 && !Dirtys.ContainKey("Category"))
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
        /// <summary>根据主键查询一个统计实体对象用于表单编辑</summary>
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
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            //return Find(_.ID, id);
            // 实体缓存
            //return Meta.Cache.Entities.Find(_.ID, id);
            // 单对象缓存
            return Meta.SingleCache[id];
        }
        #endregion

        #region 高级查询
        // 以下为自定义高级查询的例子

        ///// <summary>
        ///// 查询满足条件的记录集，分页、排序
        ///// </summary>
        ///// <param name="key">关键字</param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>实体集</returns>
        //[DataObjectMethod(DataObjectMethodType.Select, true)]
        //public static EntityList<Statistics> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindAll(SearchWhere(key), orderClause, null, startRowIndex, maximumRows);
        //}

        ///// <summary>
        ///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        ///// </summary>
        ///// <param name="key">关键字</param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>记录数</returns>
        //public static Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindCount(SearchWhere(key), null, null, 0, 0);
        //}

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
        /// <summary>增加计数</summary>
        /// <param name="remark">备注</param>
        public void Increment(String remark)
        {
            lock (this)
            {
                Total++;

                DateTime last = LastTime;
                DateTime now = DateTime.Now;
                LastTime = now;

                // 有记录，判断是否过了一天（周、月、年）
                if (last > DateTime.MinValue)
                {
                    // 去掉时分秒，避免其带来差异
                    last = last.Date;
                    now = now.Date;

                    // 是否同一天
                    Int32 diff = (now - last).Days;
                    if (diff != 0)
                    {
                        Yesterday = diff == 1 ? Today : 0;
                        Today = 0;

                        // 是否同一周
                        diff = Math.Abs(diff);
                        if (diff >= 7)
                        {
                            // 肯定不是同一周
                            LastWeek = diff <= 14 && now.DayOfWeek >= last.DayOfWeek ? ThisWeek : 0;
                            ThisWeek = 0;
                        }
                        else
                        {
                            // 当前的星期数小于上次的星期数，并且两者在七天之内，表明是新的一周了
                            if (now.DayOfWeek < last.DayOfWeek)
                            {
                                LastWeek = ThisWeek;
                                ThisWeek = 0;
                            }
                        }

                        // 是否同一个月
                        diff = now.Year * 12 + now.Month - (last.Year * 12 + last.Month);
                        if (diff != 0)
                        {
                            // 是否刚好过了一个月
                            LastMonth = diff == 1 ? ThisMonth : 0;
                            ThisMonth = 0;

                            // 是否同一年
                            diff = now.Year - last.Year;
                            if (diff != 0)
                            {
                                // 是否刚好过了一年
                                LastYear = diff == 1 ? ThisYear : 0;
                                ThisYear = 0;
                            }
                        }
                    }
                }

                Today++;
                ThisWeek++;
                ThisMonth++;
                ThisYear++;

                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                    LastIP = HttpContext.Current.Request.UserHostAddress;
                if (!String.IsNullOrEmpty(remark)) Remark = remark;
            }
        }

        /// <summary>增加指定编号的计数</summary>
        /// <param name="id"></param>
        public static void Increment(Int32 id)
        {
            TEntity entity = FindByID(id);
            if (entity != null) entity.Increment(null);
        }
        #endregion
    }
}