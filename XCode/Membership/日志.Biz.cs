using System;
using System.Collections.Generic;
using System.Web;
using NewLife.Model;
using NewLife.Web;
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
                if (!IsDirty(__.UserName))
                {
#if !__CORE__
                    var user = HttpContext.Current?.User?.Identity as IManageUser;
#else
                    var user = ManageProvider.Provider?.Current;
#endif
                    UserName = user + "";
                }
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
        ///// <summary>创建人名称</summary>
        //[XmlIgnore, ScriptIgnore]
        //[DisplayName("创建人")]
        //[Map("CreateUserID")]
        //public String CreateUserName { get { return ManageProvider.Provider.FindByID(CreateUserID) + ""; } }

        ///// <summary>物理地址</summary>
        ////[BindRelation("CreateIP")]
        //[DisplayName("物理地址")]
        //public String CreateAddress { get { return CreateIP.IPToAddress(); } }
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
        public static IList<TEntity> Search(String key, Int32 userid, String category, DateTime start, DateTime end, Pager p)
        {
            var exp = new WhereExpression();
            //if (!key.IsNullOrEmpty()) exp &= (_.Action == key | _.Remark.Contains(key));
            if (!category.IsNullOrEmpty() && category != "全部") exp &= _.Category == category;
            if (userid >= 0) exp &= _.CreateUserID == userid;
            if (start > DateTime.MinValue) exp &= _.CreateTime >= start;
            if (end > DateTime.MinValue)
            {
                if (end == end.Date) end = end.AddDays(1);
                exp &= _.CreateTime < end;
            }

            // 先精确查询，再模糊
            if (!key.IsNullOrEmpty())
            {
                var list = FindAll(exp & _.Action == key, p);
                if (list.Count > 0) return list;

                exp &= _.Action.Contains(key) | _.Remark.Contains(key);
            }

            return FindAll(exp, p);
        }
        #endregion

        #region 扩展操作
        static FieldCache<TEntity> CategoryCache = new FieldCache<TEntity>(_.Category);

        /// <summary>查找所有类别名</summary>
        /// <returns></returns>
        public static IList<TEntity> FindAllCategory()
        {
            return CategoryCache.Entities;
        }

        /// <summary>获取所有类别名称</summary>
        /// <returns></returns>
        public static IDictionary<String, String> FindAllCategoryName()
        {
            return CategoryCache.FindAllName();
        }
        #endregion

        #region 业务
        ///// <summary>创建日志</summary>
        ///// <param name="category"></param>
        ///// <param name="action"></param>
        ///// <returns></returns>
        //public static TEntity Create(String category, String action)
        //{
        //    var entity = new TEntity();

        //    entity.Category = category;
        //    entity.Action = action;

        //    return entity;
        //}

        ///// <summary>创建日志</summary>
        ///// <param name="type">类型</param>
        ///// <param name="action"></param>
        ///// <returns></returns>
        //public static TEntity Create(Type type, String action)
        //{
        //    var name = type.GetDisplayName() ?? type.GetDescription() ?? type.Name;

        //    return Create(name, action);
        //}

        ///// <summary>创建</summary>
        ///// <param name="type"></param>
        ///// <param name="action"></param>
        ///// <returns></returns>
        //ILog ILog.Create(Type type, String action) { return Create(type, action); }

        ///// <summary>写日志</summary>
        ///// <param name="type">类型</param>
        ///// <param name="action">操作</param>
        ///// <param name="remark">备注</param>
        //public void WriteLog(Type type, String action, String remark)
        //{
        //    var log = Create(type, action);
        //    if (log != null)
        //    {
        //        log.Remark = remark;
        //        log.Save();
        //    }
        //}

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return String.Format("{0} {1} {2} {3:yyyy-MM-dd HH:mm:ss} {4}", Category, Action, UserName, CreateTime, Remark);
        }
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