using System;
using System.ComponentModel;
using System.Text;
using NewLife.Collections;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Cache;

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

            // 处理过长的备注
            if (!String.IsNullOrEmpty(Remark) && Remark.Length > 500)
            {
                Remark = Remark.Substring(0, 500);
            }

            return base.Insert();
        }
        #endregion

        #region 扩展属性
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
        /// 查询
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
        /// 查询
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

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="key"></param>
        /// <param name="adminid"></param>
        /// <param name="category"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static String SearchWhere(String key, Int32 adminid, String category, DateTime start, DateTime end)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("1=1");
            if (!String.IsNullOrEmpty(key)) sb.AppendFormat(" And {0} like '%{1}%'", _.Remark, key.Replace("'", "''"));
            if (!String.IsNullOrEmpty(category) && category != "全部") sb.AppendFormat(" And {0}='{1}'", _.Category, category.Replace("'", "''"));
            if (adminid > 0) sb.AppendFormat(" And {0}={1}", _.UserID, adminid);
            if (start > DateTime.MinValue) sb.AppendFormat(" And {0}>={1}", _.OccurTime, Meta.FormatDateTime(start));
            if (end > DateTime.MinValue) sb.AppendFormat(" And {0}<{1}", _.OccurTime, Meta.FormatDateTime(end.Date.AddDays(1)));

            if (sb.ToString() == "1=1")
                return null;
            else
                return sb.ToString();
        }
        #endregion

        #region 扩展操作
        static EntityCache<TEntity> _categoryCache;
        /// <summary>
        /// 类别名实体缓存，异步，缓存10分钟
        /// </summary>
        static EntityCache<TEntity> CategoryCache
        {
            get
            {
                if (_categoryCache == null)
                {
                    // 缓存查询所有类别名，并缓存10分钟，缓存过期时将使用异步查询，不影响返回速度
                    _categoryCache = new EntityCache<TEntity>();
                    _categoryCache.Asynchronous = true;
                    _categoryCache.Expriod = 10 * 60;
                    _categoryCache.FillListMethod = delegate { return FindAll("1=1 Group By " + _.Category, null, _.Category, 0, 0); };
                }
                return _categoryCache;
            }
        }

        /// <summary>
        /// 查找所有类别名
        /// </summary>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllCategory()
        {
            return CategoryCache.Entities;
        }
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
            String name = GetDescription(type);
            if (String.IsNullOrEmpty(name)) name = type.Name;

            return Create(name, action);
        }

        static DictionaryCache<Type, String> desCache = new DictionaryCache<Type, string>();
        /// <summary>
        /// 获取实体类的描述名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static String GetDescription(Type type)
        {
            return desCache.GetItem(type, delegate(Type key)
            {
                if (!typeof(IEntity).IsAssignableFrom(key)) return null;

                BindColumnAttribute att = AttributeX.GetCustomAttribute<BindColumnAttribute>(key, true);
                if (att != null && !String.IsNullOrEmpty(att.Description)) return att.Description;

                DescriptionAttribute att2 = AttributeX.GetCustomAttribute<DescriptionAttribute>(key, true);
                if (att2 != null && !String.IsNullOrEmpty(att2.Description)) return att2.Description;

                return null;
            });
        }
        #endregion
    }
}