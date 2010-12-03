using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using XCode;
using System.Xml.Serialization;
using XCommon;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 日志
    /// </summary>
    public partial class Log<TEntity> : Entity<TEntity> where TEntity : Log<TEntity>, new()
    {
        #region 对象操作
        /// <summary>
        /// 已重载。把该对象插入到数据库。这里可以做数据插入前的检查
        /// </summary>
        /// <returns>影响的行数</returns>
        public override Int32 Insert()
        {
            if (String.IsNullOrEmpty(IP)) IP = WebHelper.UserHost;
            if (OccurTime <= DateTime.MinValue) OccurTime = DateTime.Now;

            //Administrator user = Administrator.Current;
            //if (user != null)
            //{
            //    if (UserID <= 0) UserID = user.ID;
            //    if (String.IsNullOrEmpty(UserName)) UserName = user.Name;
            //}

            return base.Insert();
        }
        #endregion

        #region 扩展属性

        //private List<String> hasLoaded = new List<string>();
        #endregion

        #region 扩展查询
        /// <summary>
        /// 根据主键查询一个日志实体对象用于表单编辑
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
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="adminid"></param>
        /// <param name="category"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="orderClause"></param>
        /// <param name="startRowIndex"></param>
        /// <param name="maximumRows"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> Search(String key, Int32 adminid, String category, DateTime start, DateTime end, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            if (String.IsNullOrEmpty(orderClause)) orderClause = _.ID + " Desc";
            return FindAll(SearchWhere(key, adminid, category, start, end), orderClause, null, startRowIndex, maximumRows);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="adminid"></param>
        /// <param name="category"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="orderClause"></param>
        /// <param name="startRowIndex"></param>
        /// <param name="maximumRows"></param>
        /// <returns></returns>
        public static Int32 SearchCount(String key, Int32 adminid, String category, DateTime start, DateTime end, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            //return FindCount(SearchWhere(key, netbar, account, adminid, category, start, end), null, SearchSelect(), 0, 0);
            String where = SearchWhere(key, adminid, category, start, end);
            return FindCount(where, null, null, 0, 0);
        }

        private static String SearchWhere(String key, Int32 adminid, String category, DateTime start, DateTime end)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("1=1");
            if (!String.IsNullOrEmpty(key)) sb.AppendFormat(" And {0} like '%{1}%'", _.Remark, key.Replace("'", "''"));
            if (!String.IsNullOrEmpty(category) && category != "全部") sb.AppendFormat(" And {0}='{1}'", _.Category, category.Replace("'", "''"));
            if (adminid > 0) sb.AppendFormat(" And {0}={1}", _.UserID, adminid);
            if (start > DateTime.MinValue) sb.AppendFormat(" And {0}>='{1:yyyy-MM-dd HH:mm:ss}'", _.OccurTime, start);
            if (end > DateTime.MinValue) sb.AppendFormat(" And {0}<'{1:yyyy-MM-dd HH:mm:ss}'", _.OccurTime, end.Date.AddDays(1));

            if (sb.ToString() == "1=1")
                return null;
            else
                return sb.ToString();
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        /// <summary>
        /// 创建日志
        /// </summary>
        /// <param name="category"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static TEntity Create(String category, String action)
        {
            TEntity entity = new TEntity();

            entity.Category = category;
            entity.Action = action;

            return entity;
        }

        /// <summary>
        /// 创建日志
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static TEntity Create(Type type, String action)
        {
            return Create(type.Name, action);
        }
        #endregion
    }
}