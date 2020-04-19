using System;
using System.Collections.Generic;
using NewLife.Data;
using XCode.Cache;

namespace XCode.Membership
{
    /// <summary>日志</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Log : Log<Log> { }

    /// <summary>日志</summary>
    public partial class Log<TEntity> : Entity<TEntity> where TEntity : Log<TEntity>, new()
    {
        #region 对象操作
        static Log()
        {
            Meta.Table.DataTable.InsertOnly = true;
            //Meta.Factory.FullInsert = false;

            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<IPModule>();

#if !DEBUG
            // 关闭SQL日志
            Meta.Session.Dal.Db.ShowSQL = false;
#endif
        }

        /// <summary>已重载。记录当前管理员</summary>
        /// <param name="isNew"></param>
        public override void Valid(Boolean isNew)
        {
            base.Valid(isNew);

            if (isNew)
            {
                // 自动设置当前登录用户
                if (!IsDirty(__.UserName)) UserName = ManageProvider.Provider?.Current + "";
            }

            // 处理过长的备注
            if (!Remark.IsNullOrEmpty() && Remark.Length > 500)
            {
                Remark = Remark.Substring(0, 500);
            }

            // 时间
            if (isNew && CreateTime.Year < 2000 && !IsDirty(__.CreateTime)) CreateTime = DateTime.Now;
        }

        /// <summary></summary>
        /// <returns></returns>
        protected override Int32 OnUpdate() => throw new Exception("禁止修改日志！");

        /// <summary></summary>
        /// <returns></returns>
        protected override Int32 OnDelete() => throw new Exception("禁止删除日志！");
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>查询</summary>
        /// <param name="key"></param>
        /// <param name="userid"></param>
        /// <param name="category"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        [Obsolete]
        public static IList<TEntity> Search(String key, Int32 userid, String category, DateTime start, DateTime end, PageParameter p)
        {
            var exp = new WhereExpression();
            //if (!key.IsNullOrEmpty()) exp &= (_.Action == key | _.Remark.Contains(key));
            if (!category.IsNullOrEmpty() && category != "全部") exp &= _.Category == category;
            if (userid >= 0) exp &= _.CreateUserID == userid;
            exp &= _.CreateTime.Between(start, end);

            // 先精确查询，再模糊
            if (!key.IsNullOrEmpty())
            {
                var list = FindAll(exp & _.Action == key, p);
                if (list.Count > 0) return list;

                exp &= _.Action.Contains(key) | _.Remark.Contains(key);
            }

            return FindAll(exp, p);
        }

        /// <summary>查询</summary>
        /// <param name="category"></param>
        /// <param name="action"></param>
        /// <param name="success"></param>
        /// <param name="userid"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="key"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<TEntity> Search(String category, String action, Boolean? success, Int32 userid, DateTime start, DateTime end, String key, PageParameter p)
        {
            var exp = new WhereExpression();

            if (!category.IsNullOrEmpty() && category != "全部") exp &= _.Category == category;
            if (!action.IsNullOrEmpty() && action != "全部") exp &= _.Action == action;
            if (success != null) exp &= _.Success == success;
            if (userid >= 0) exp &= _.CreateUserID == userid;
            exp &= _.CreateTime.Between(start, end);

            if (!key.IsNullOrEmpty()) exp &= _.Remark.Contains(key);

            return FindAll(exp, p);
        }
        #endregion

        #region 扩展操作
        // Select Count(ID) as ID,Category From Log Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By ID Desc limit 20
        static readonly FieldCache<TEntity> CategoryCache = new FieldCache<TEntity>(__.Category)
        {
            Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        };

        /// <summary>获取所有类别名称，最近30天</summary>
        /// <returns></returns>
        public static IDictionary<String, String> FindAllCategoryName() => CategoryCache.FindAllName();

        static readonly FieldCache<TEntity> ActionCache = new FieldCache<TEntity>(__.Action)
        {
            Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        };

        /// <summary>获取所有操作名称，最近30天</summary>
        /// <returns></returns>
        public static IDictionary<String, String> FindAllActionName() => ActionCache.FindAllName();
        #endregion

        #region 业务
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{Category} {Action} {UserName} {CreateTime:yyyy-MM-dd HH:mm:ss} {Remark}";
        #endregion
    }

    public partial interface ILog
    {
        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();

        /// <summary>异步保存</summary>
        /// <param name="msDelay">延迟保存的时间。默认0ms近实时保存</param>
        /// <returns></returns>
        Boolean SaveAsync(Int32 msDelay = 0);
    }
}