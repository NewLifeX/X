using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using NewLife.Collections;
using XCode.Cache;

namespace XCode.DataAccessLayer
{
    partial class DAL
    {
        #region 统计属性
        private Boolean _EnableCache = true;
        /// <summary>
        /// 是否启用缓存。
        /// <remarks>设为false可清空缓存</remarks>
        /// </summary>
        public Boolean EnableCache
        {
            get { return _EnableCache; }
            set
            {
                _EnableCache = value;
                if (!_EnableCache) XCache.RemoveAll();
            }
        }

        /// <summary>
        /// 缓存个数
        /// </summary>
        public Int32 CacheCount
        {
            get
            {
                return XCache.Count;
            }
        }

        [ThreadStatic]
        private static Int32 _QueryTimes;
        /// <summary>
        /// 查询次数
        /// </summary>
        public static Int32 QueryTimes
        {
            //get { return DB != null ? DB.QueryTimes : 0; }
            get { return _QueryTimes; }
        }

        [ThreadStatic]
        private static Int32 _ExecuteTimes;
        /// <summary>
        /// 执行次数
        /// </summary>
        public static Int32 ExecuteTimes
        {
            //get { return DB != null ? DB.ExecuteTimes : 0; }
            get { return _ExecuteTimes; }
        }
        #endregion

        #region 使用缓存后的数据操作方法
        private static DictionaryCache<String, String> _PageSplitCache = new DictionaryCache<String, String>(StringComparer.OrdinalIgnoreCase);
        /// <summary>根据条件把普通查询SQL格式化为分页SQL。</summary>
        /// <remarks>
        /// 因为需要继承重写的原因，在数据类中并不方便缓存分页SQL。
        /// 所以在这里做缓存。
        /// </remarks>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        [Obsolete("请优先考虑使用SelectBuilder参数做分页！")]
        public String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            String cacheKey = String.Format("{0}_{1}_{2}_{3}_{4}", sql, startRowIndex, maximumRows, ConnName, keyColumn);

            return _PageSplitCache.GetItem<String, Int32, Int32, String>(cacheKey, sql, startRowIndex, maximumRows, keyColumn, (k, b, s, m, kc) =>
            {
                return Db.PageSplit(b, s, m, kc);
            });
        }

        private static DictionaryCache<String, SelectBuilder> _PageSplitCache2 = new DictionaryCache<String, SelectBuilder>(StringComparer.OrdinalIgnoreCase);
        /// <summary>根据条件把普通查询SQL格式化为分页SQL。</summary>
        /// <remarks>
        /// 因为需要继承重写的原因，在数据类中并不方便缓存分页SQL。
        /// 所以在这里做缓存。
        /// </remarks>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public SelectBuilder PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
        {
            String cacheKey = String.Format("{0}_{1}_{2}_{3}_", builder, startRowIndex, maximumRows, ConnName);

            return _PageSplitCache2.GetItem<SelectBuilder, Int32, Int32>(cacheKey, builder, startRowIndex, maximumRows, (k, b, s, m) =>
            {
                return Db.PageSplit(b, s, m);
            });
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        public DataSet Select(String sql, params String[] tableNames)
        {
            String cacheKey = sql + "_" + ConnName;
            DataSet ds = null;
            if (EnableCache && XCache.TryGetItem(cacheKey, out ds)) return ds;

            Interlocked.Increment(ref _QueryTimes);
            ds = Session.Query(sql);

            if (EnableCache) XCache.Add(cacheKey, ds, tableNames);

            return ds;
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        public DataSet Select(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows, params String[] tableNames)
        {
            builder = PageSplit(builder, startRowIndex, maximumRows);
            return Select(builder.ToString(), tableNames);
        }

        ///// <summary>执行SQL查询，返回分页记录集</summary>
        ///// <param name="sql">SQL语句</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <param name="keyColumn">唯一键。用于not in分页</param>
        ///// <param name="tableNames">所依赖的表的表名</param>
        ///// <returns></returns>
        //public DataSet Select(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn, params String[] tableNames)
        //{
        //    return Select(PageSplit(sql, startRowIndex, maximumRows, keyColumn), tableNames);
        //}

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        [Obsolete("请优先考虑使用SelectBuilder参数做查询！")]
        public Int32 SelectCount(String sql, params String[] tableNames)
        {
            String cacheKey = sql + "_SelectCount" + "_" + ConnName;
            Int32 rs = 0;
            if (EnableCache && XCache.TryGetItem(cacheKey, out rs)) return rs;

            Interlocked.Increment(ref _QueryTimes);
            // 为了向前兼容，这里转为Int32，如果需要获取Int64，可直接调用Session
            rs = (Int32)Session.QueryCount(sql);

            if (EnableCache) XCache.Add(cacheKey, rs, tableNames);

            return rs;
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sb">查询生成器</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        public Int32 SelectCount(SelectBuilder sb, params String[] tableNames)
        {
            String sql = sb.ToString();
            String cacheKey = sql + "_SelectCount" + "_" + ConnName;
            Int32 rs = 0;
            if (EnableCache && XCache.TryGetItem(cacheKey, out rs)) return rs;

            Interlocked.Increment(ref _QueryTimes);
            rs = (Int32)Session.QueryCount(sb);

            if (EnableCache) XCache.Add(cacheKey, rs, tableNames);

            return rs;
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableNames">受影响的表的表名</param>
        /// <returns></returns>
        public Int32 Execute(String sql, params String[] tableNames)
        {
            // 移除所有和受影响表有关的缓存
            if (EnableCache) XCache.Remove(tableNames);
            Interlocked.Increment(ref _ExecuteTimes);
            return Session.Execute(sql);
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql"></param>
        /// <param name="tableNames">受影响的表的表名</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql, params String[] tableNames)
        {
            // 移除所有和受影响表有关的缓存
            if (EnableCache) XCache.Remove(tableNames);
            Interlocked.Increment(ref _ExecuteTimes);
            return Session.InsertAndGetIdentity(sql);
        }

        /// <summary>执行CMD，返回记录集</summary>
        /// <param name="cmd">CMD</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        public DataSet Select(DbCommand cmd, String[] tableNames)
        {
            String cacheKey = cmd.CommandText + "_" + ConnName;
            DataSet ds = null;
            if (EnableCache && XCache.TryGetItem(cacheKey, out ds)) return ds;

            Interlocked.Increment(ref _QueryTimes);
            ds = Session.Query(cmd);

            if (EnableCache) XCache.Add(cacheKey, ds, tableNames);

            Session.AutoClose();
            return ds;
        }

        /// <summary>执行CMD，返回受影响的行数</summary>
        /// <param name="cmd"></param>
        /// <param name="tableNames"></param>
        /// <returns></returns>
        public Int32 Execute(DbCommand cmd, String[] tableNames)
        {
            // 移除所有和受影响表有关的缓存
            if (EnableCache) XCache.Remove(tableNames);

            Interlocked.Increment(ref _ExecuteTimes);
            Int32 ret = Session.Execute(cmd);
            Session.AutoClose();
            return ret;
        }
        #endregion

        #region 事务
        /// <summary>开始事务</summary>
        public Int32 BeginTransaction() { return Session.BeginTransaction(); }

        /// <summary>提交事务</summary>
        public Int32 Commit() { return Session.Commit(); }

        /// <summary>回滚事务</summary>
        public Int32 Rollback() { return Session.Rollback(); }
        #endregion
    }
}