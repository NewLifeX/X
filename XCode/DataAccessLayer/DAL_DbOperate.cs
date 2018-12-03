using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;

namespace XCode.DataAccessLayer
{
    partial class DAL
    {
        #region 属性
        [ThreadStatic]
        private static Int32 _QueryTimes;
        /// <summary>查询次数</summary>
        public static Int32 QueryTimes => _QueryTimes;

        [ThreadStatic]
        private static Int32 _ExecuteTimes;
        /// <summary>执行次数</summary>
        public static Int32 ExecuteTimes => _ExecuteTimes;
        #endregion

        #region 数据操作方法
        /// <summary>根据条件把普通查询SQL格式化为分页SQL。</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            if (startRowIndex <= 0 && maximumRows <= 0) return builder;

            // 2016年7月2日 HUIYUE 取消分页SQL缓存，此部分缓存提升性能不多，但有可能会造成分页数据不准确，感觉得不偿失
            return Db.PageSplit(builder, startRowIndex, maximumRows);
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public DataSet Select(String sql)
        {
            return QueryByCache(sql, "", "", (s, k2, k3) => Session.Query(s), nameof(Select));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        public DataSet Select(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            return QueryByCache(builder, startRowIndex, maximumRows, (sb, start, max) =>
            {
                sb = PageSplit(sb, start, max);
                return Session.Query(sb.ToString(), CommandType.Text, sb.Parameters.ToArray());
            }, nameof(Select));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        public DbTable Query(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            return QueryByCache(builder, startRowIndex, maximumRows, (sb, start, max) =>
            {
                sb = PageSplit(sb, start, max);
                return Session.Query(sb.ToString(), sb.Parameters.ToArray());
            }, nameof(Query));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public DbTable Query(String sql, IDictionary<String, Object> ps = null)
        {
            return QueryByCache(sql, ps, "", (s, p, k3) => Session.Query(s, Db.CreateParameters(p)), nameof(Query));
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sb">查询生成器</param>
        /// <returns></returns>
        public Int32 SelectCount(SelectBuilder sb)
        {
            return (Int32)QueryByCache(sb, "", "", (s, k2, k3) => Session.QueryCount(s), nameof(SelectCount));
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public Int32 SelectCount(String sql, CommandType type, params IDataParameter[] ps)
        {
            return (Int32)QueryByCache(sql, type, ps, (s, t, p) => Session.QueryCount(s, t, p), nameof(SelectCount));
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public Int32 Execute(String sql)
        {
            return ExecuteByCache(sql, "", "", (s, t, p) => Session.Execute(s));
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql"></param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql)
        {
            return ExecuteByCache(sql, "", "", (s, t, p) => Session.InsertAndGetIdentity(s));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public DataSet Select(String sql, CommandType type, params IDataParameter[] ps)
        {
            return QueryByCache(sql, type, ps, (s, t, p) => Session.Query(s, t, p), nameof(Select));
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public Int32 Execute(String sql, CommandType type, params IDataParameter[] ps)
        {
            return ExecuteByCache(sql, type, ps, (s, t, p) => Session.Execute(s, t, p));
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql"></param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql, CommandType type, params IDataParameter[] ps)
        {
            return ExecuteByCache(sql, type, ps, (s, t, p) => Session.InsertAndGetIdentity(s, t, p));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public DataSet Select(String sql, CommandType type, IDictionary<String, Object> ps)
        {
            return QueryByCache(sql, type, ps, (s, t, p) => Session.Query(s, t, Db.CreateParameters(p)), nameof(Select));
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public Int32 Execute(String sql, CommandType type, IDictionary<String, Object> ps)
        {
            return ExecuteByCache(sql, type, ps, (s, t, p) => Session.Execute(s, t, Db.CreateParameters(p)));
        }

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(String sql, CommandType type, IDictionary<String, Object> ps)
        {
            return ExecuteByCache(sql, type, ps, (s, t, p) => Session.ExecuteScalar<T>(s, t, Db.CreateParameters(p)));
        }
        #endregion

        #region 事务
        /// <summary>开始事务</summary>
        /// <remarks>
        /// Read Uncommitted: 允许读取脏数据，一个事务能看到另一个事务还没有提交的数据。（不会阻止其它操作）
        /// Read Committed: 确保事务读取的数据都必须是已经提交的数据。它限制了读取中间的，没有提交的，脏的数据。
        /// 但是它不能确保当事务重新去读取的时候，读的数据跟上次读的数据是一样的，也就是说当事务第一次读取完数据后，
        /// 该数据是可能被其他事务修改的，当它再去读取的时候，数据可能是不一样的。（数据隐藏，不阻止）
        /// Repeatable Read: 是一个更高级别的隔离级别，如果事务再去读取同样的数据，先前的数据是没有被修改过的。（阻止其它修改）
        /// Serializable: 它做出了最有力的保证，除了每次读取的数据是一样的，它还确保每次读取没有新的数据。（阻止其它添删改）
        /// </remarks>
        /// <param name="level">事务隔离等级</param>
        /// <returns>剩下的事务计数</returns>
        public Int32 BeginTransaction(IsolationLevel level = IsolationLevel.ReadCommitted)
        {
            CheckDatabase();

            return Session.BeginTransaction(level);
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public Int32 Commit() => Session.Commit();

        /// <summary>回滚事务，忽略异常</summary>
        /// <returns>剩下的事务计数</returns>
        public Int32 Rollback() => Session.Rollback();
        #endregion

        #region 缓存
        /// <summary>缓存存储</summary>
        public DictionaryCache<String, Object> Store { get; set; }

        /// <summary>数据层缓存。默认10秒</summary>
        public Int32 Expire { get; set; }

        private DictionaryCache<String, Object> GetCache()
        {
            var st = Store;
            if (st != null) return st;

            var exp = Expire;
            if (exp == 0) exp = Db.DataCache;
            if (exp == 0) exp = Setting.Current.DataCacheExpire;
            if (exp <= 0) return null;

            Expire = exp;

            lock (this)
            {
                if (Store == null)
                {
                    var p = exp / 2;
                    if (p < 30) p = 30;

                    st = Store = new DictionaryCache<String, Object> { Period = p, Expire = exp };
                }
            }

            return st;
        }

        private TResult QueryByCache<T1, T2, T3, TResult>(T1 k1, T2 k2, T3 k3, Func<T1, T2, T3, TResult> callback, String prefix = null)
        {
            CheckDatabase();

            // 读缓存
            var cache = GetCache();
            if (cache != null)
            {
                var sb = Pool.StringBuilder.Get();
                if (!prefix.IsNullOrEmpty())
                {
                    sb.Append(prefix);
                    sb.Append("#");
                }
                Append(sb, k1);
                Append(sb, k2);
                Append(sb, k3);
                var key = sb.Put(true);

                return cache.GetItem(key, k =>
                {
                    Interlocked.Increment(ref _QueryTimes);
                    return callback(k1, k2, k3);
                }).ChangeType<TResult>();
            }

            Interlocked.Increment(ref _QueryTimes);

            return callback(k1, k2, k3);
        }

        private TResult ExecuteByCache<T1, T2, T3, TResult>(T1 k1, T2 k2, T3 k3, Func<T1, T2, T3, TResult> callback)
        {
            if (Db.Readonly) throw new InvalidOperationException($"数据连接[{ConnName}]只读，禁止执行{k1}");

            CheckDatabase();

            var rs = callback(k1, k2, k3);

            var st = GetCache();
            st?.Clear();

            Interlocked.Increment(ref _ExecuteTimes);

            return rs;
        }

        private static void Append(StringBuilder sb, Object value)
        {
            if (value == null) return;

            if (value is SelectBuilder builder)
            {
                sb.Append(builder);
                foreach (var item in builder.Parameters)
                {
                    sb.Append("#");
                    sb.Append(item.ParameterName);
                    sb.Append("#");
                    sb.Append(item.Value);
                }
            }
            else if (value is IDataParameter[] ps)
            {
                foreach (var item in ps)
                {
                    sb.Append("#");
                    sb.Append(item.ParameterName);
                    sb.Append("#");
                    sb.Append(item.Value);
                }
            }
            else if (value is IDictionary<String, Object> dic)
            {
                foreach (var item in dic)
                {
                    sb.Append("#");
                    sb.Append(item.Key);
                    sb.Append("#");
                    sb.Append(item.Value);
                }
            }
            else
            {
                sb.Append("#");
                sb.Append(value);
            }
        }
        #endregion
    }
}