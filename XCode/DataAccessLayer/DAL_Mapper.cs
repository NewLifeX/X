using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NewLife;
using NewLife.Collections;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>根据实体类获取表名的委托</summary>
    /// <param name="entityType">实体类</param>
    /// <returns></returns>
    public delegate String GetTableNameCallback(Type entityType);

    public partial class DAL
    {
        /// <summary>根据实体类获取表名的委托，用于Mapper的Insert/Update</summary>
        public static GetTableNameCallback GetTableName { get; set; }

        #region 添删改查
        /// <summary>查询Sql并映射为结果集</summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="sql">Sql语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(String sql, Object param = null)
        {
            if (IsValueTuple(typeof(T))) throw new InvalidOperationException($"不支持ValueTuple类型[{typeof(T).FullName}]");

            //var ps = param?.ToDictionary();
            var dt = QueryByCache(sql, param, "", (s, p, k3) => Session.Query(s, Db.CreateParameters(p)), nameof(Query));

            // 优先特殊处理基础类型，选择第一字段
            var type = typeof(T);
            var utype = Nullable.GetUnderlyingType(type);
            if (utype != null) type = utype;
            if (type.GetTypeCode() != TypeCode.Object) return dt.Rows.Select(e => e[0].ChangeType<T>());

            return dt.ReadModels<T>();
        }

        /// <summary>查询Sql并返回单个结果</summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="sql">Sql语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public T QuerySingle<T>(String sql, Object param = null) => Query<T>(sql, param).FirstOrDefault();

#if !NET40
        /// <summary>查询Sql并映射为结果集</summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="sql">Sql语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(String sql, Object param = null)
        {
            if (IsValueTuple(typeof(T))) throw new InvalidOperationException($"不支持ValueTuple类型[{typeof(T).FullName}]");

            var dt = await QueryByCacheAsync(sql, param, "", (s, p, k3) => Session.QueryAsync(s, Db.CreateParameters(p)), nameof(QueryAsync));

            // 优先特殊处理基础类型，选择第一字段
            var type = typeof(T);
            var utype = Nullable.GetUnderlyingType(type);
            if (utype != null) type = utype;
            if (type.GetTypeCode() != TypeCode.Object) return dt.Rows.Select(e => e[0].ChangeType<T>());

            return dt.ReadModels<T>();
        }

        /// <summary>查询Sql并返回单个结果</summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="sql">Sql语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public async Task<T> QuerySingleAsync<T>(String sql, Object param = null) => (await QueryAsync<T>(sql, param)).FirstOrDefault();
#endif

        private static Boolean IsValueTuple(Type type)
        {
            if ((Object)type != null && type.IsValueType)
            {
                return type.FullName.StartsWith("System.ValueTuple`", StringComparison.Ordinal);
            }
            return false;
        }

        /// <summary>执行Sql</summary>
        /// <param name="sql">Sql语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public Int32 Execute(String sql, Object param = null) =>
            //var ps = param?.ToDictionary();
            ExecuteByCache(sql, "", param, (s, t, p) => Session.Execute(s, CommandType.Text, Db.CreateParameters(p)));

        /// <summary>执行Sql并返回数据读取器</summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(String sql, Object param = null)
        {
            //var ps = param?.ToDictionary();
            var cmd = Session.CreateCommand(sql, CommandType.Text, Db.CreateParameters(param));
            cmd.Connection = Db.OpenConnection();

            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(String sql, Object param = null) =>
            QueryByCache(sql, param, "", (s, p, k3) => Session.ExecuteScalar<T>(s, CommandType.Text, Db.CreateParameters(p)), nameof(ExecuteScalar));

#if !NET40
        /// <summary>执行Sql</summary>
        /// <param name="sql">Sql语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public Task<Int32> ExecuteAsync(String sql, Object param = null) =>
            ExecuteByCacheAsync(sql, "", param, (s, t, p) => Session.ExecuteAsync(s, CommandType.Text, Db.CreateParameters(p)));

        /// <summary>执行Sql并返回数据读取器</summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public Task<DbDataReader> ExecuteReaderAsync(String sql, Object param = null)
        {
            var cmd = Session.CreateCommand(sql, CommandType.Text, Db.CreateParameters(param));
            cmd.Connection = Db.OpenConnection();

            return cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public Task<T> ExecuteScalarAsync<T>(String sql, Object param = null) =>
            QueryByCacheAsync(sql, param, "", (s, p, k3) => Session.ExecuteScalarAsync<T>(s, CommandType.Text, Db.CreateParameters(p)), nameof(ExecuteScalarAsync));
#endif

        private ConcurrentDictionary<Type, String> _tableMaps = new();
        private String GetName(Type type)
        {
            if (GetTableName == null) return null;

            return _tableMaps.GetOrAdd(type, t => GetTableName(t));
        }

        /// <summary>插入数据</summary>
        /// <param name="data">实体对象</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public Int32 Insert(Object data, String tableName = null)
        {
            if (tableName.IsNullOrEmpty() && GetTableName != null) tableName = GetName(data.GetType());
            if (tableName.IsNullOrEmpty()) tableName = data.GetType().Name;

            var pis = data.ToDictionary();
            var dps = Db.CreateParameters(data);
            var ns = pis.Join(",", e => e.Key);
            var vs = dps.Join(",", e => e.ParameterName);
            var sql = $"Insert Into {tableName}({ns}) Values({vs})";

            return ExecuteByCache(sql, "", dps, (s, t, p) => Session.Execute(s, CommandType.Text, p));
        }

        /// <summary>更新数据。不支持自动识别主键</summary>
        /// <param name="data">实体对象</param>
        /// <param name="where">查询条件。默认使用Id字段</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public Int32 Update(Object data, Object where, String tableName = null)
        {
            if (tableName.IsNullOrEmpty() && GetTableName != null) tableName = GetName(data.GetType());
            if (tableName.IsNullOrEmpty()) tableName = data.GetType().Name;

            var sb = Pool.StringBuilder.Get();
            sb.Append("Update ");
            sb.Append(tableName);

            var dps = new List<IDataParameter>();
            // Set参数
            {
                sb.Append(" Set ");
                var i = 0;
                foreach (var pi in data.GetType().GetProperties(true))
                {
                    if (i++ > 0) sb.Append(',');

                    var p = Db.CreateParameter(pi.Name, pi.GetValue(data, null), pi.PropertyType);
                    dps.Add(p);
                    sb.AppendFormat("{0}={1}", pi.Name, p.ParameterName);
                }
            }
            // Where条件
            if (where != null)
            {
                sb.Append(" Where ");
                var i = 0;
                foreach (var pi in where.GetType().GetProperties(true))
                {
                    if (i++ > 0) sb.Append(" And ");

                    var p = Db.CreateParameter(pi.Name, pi.GetValue(where, null), pi.PropertyType);
                    dps.Add(p);
                    sb.AppendFormat("{0}={1}", pi.Name, p.ParameterName);
                }
            }
            else
            {
                var pi = data.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null)
                {
                    sb.Append(" Where ");

                    var p = Db.CreateParameter(pi.Name, pi.GetValue(data, null), pi.PropertyType);
                    dps.Add(p);
                    sb.AppendFormat("{0}={1}", pi.Name, p.ParameterName);
                }
            }

            var sql = sb.Put(true);

            return ExecuteByCache(sql, "", dps.ToArray(), (s, t, p) => Session.Execute(s, CommandType.Text, p));
        }

        /// <summary>删除数据</summary>
        /// <param name="tableName">表名</param>
        /// <param name="where">查询条件</param>
        /// <returns></returns>
        public Int32 Delete(String tableName, Object where)
        {
            if (tableName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tableName));

            var sb = Pool.StringBuilder.Get();
            sb.Append("Delete From ");
            sb.Append(tableName);

            // 带上参数化的Where条件
            var dps = new List<IDataParameter>();
            if (where != null)
            {
                sb.Append(" Where ");
                var i = 0;
                foreach (var pi in where.GetType().GetProperties(true))
                {
                    if (i++ > 0) sb.Append("And ");

                    var p = Db.CreateParameter(pi.Name, pi.GetValue(where, null), pi.PropertyType);
                    dps.Add(p);
                    sb.AppendFormat("{0}={1}", pi.Name, p.ParameterName);
                }
            }
            var sql = sb.Put(true);

            return ExecuteByCache(sql, "", dps.ToArray(), (s, t, p) => Session.Execute(s, CommandType.Text, p));
        }

#if !NET40
        /// <summary>插入数据</summary>
        /// <param name="data">实体对象</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public Task<Int32> InsertAsync(Object data, String tableName = null)
        {
            if (tableName.IsNullOrEmpty() && GetTableName != null) tableName = GetName(data.GetType());
            if (tableName.IsNullOrEmpty()) tableName = data.GetType().Name;

            var pis = data.ToDictionary();
            var dps = Db.CreateParameters(data);
            var ns = pis.Join(",", e => e.Key);
            var vs = dps.Join(",", e => e.ParameterName);
            var sql = $"Insert Into {tableName}({ns}) Values({vs})";

            return ExecuteByCacheAsync(sql, "", dps, (s, t, p) => Session.ExecuteAsync(s, CommandType.Text, p));
        }

        /// <summary>更新数据。不支持自动识别主键</summary>
        /// <param name="data">实体对象</param>
        /// <param name="where">查询条件。默认使用Id字段</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public Task<Int32> UpdateAsync(Object data, Object where, String tableName = null)
        {
            if (tableName.IsNullOrEmpty() && GetTableName != null) tableName = GetName(data.GetType());
            if (tableName.IsNullOrEmpty()) tableName = data.GetType().Name;

            var sb = Pool.StringBuilder.Get();
            sb.Append("Update ");
            sb.Append(tableName);

            var dps = new List<IDataParameter>();
            // Set参数
            {
                sb.Append(" Set ");
                var i = 0;
                foreach (var pi in data.GetType().GetProperties(true))
                {
                    if (i++ > 0) sb.Append(',');

                    var p = Db.CreateParameter(pi.Name, pi.GetValue(data, null), pi.PropertyType);
                    dps.Add(p);
                    sb.AppendFormat("{0}={1}", pi.Name, p.ParameterName);
                }
            }
            // Where条件
            if (where != null)
            {
                sb.Append(" Where ");
                var i = 0;
                foreach (var pi in where.GetType().GetProperties(true))
                {
                    if (i++ > 0) sb.Append(" And ");

                    var p = Db.CreateParameter(pi.Name, pi.GetValue(where, null), pi.PropertyType);
                    dps.Add(p);
                    sb.AppendFormat("{0}={1}", pi.Name, p.ParameterName);
                }
            }
            else
            {
                var pi = data.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null)
                {
                    sb.Append(" Where ");

                    var p = Db.CreateParameter(pi.Name, pi.GetValue(data, null), pi.PropertyType);
                    dps.Add(p);
                    sb.AppendFormat("{0}={1}", pi.Name, p.ParameterName);
                }
            }

            var sql = sb.Put(true);

            return ExecuteByCacheAsync(sql, "", dps.ToArray(), (s, t, p) => Session.ExecuteAsync(s, CommandType.Text, p));
        }

        /// <summary>删除数据</summary>
        /// <param name="tableName">表名</param>
        /// <param name="where">查询条件</param>
        /// <returns></returns>
        public Task<Int32> DeleteAsync(String tableName, Object where)
        {
            if (tableName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tableName));

            var sb = Pool.StringBuilder.Get();
            sb.Append("Delete From ");
            sb.Append(tableName);

            // 带上参数化的Where条件
            var dps = new List<IDataParameter>();
            if (where != null)
            {
                sb.Append(" Where ");
                var i = 0;
                foreach (var pi in where.GetType().GetProperties(true))
                {
                    if (i++ > 0) sb.Append("And ");

                    var p = Db.CreateParameter(pi.Name, pi.GetValue(where, null), pi.PropertyType);
                    dps.Add(p);
                    sb.AppendFormat("{0}={1}", pi.Name, p.ParameterName);
                }
            }
            var sql = sb.Put(true);

            return ExecuteByCacheAsync(sql, "", dps.ToArray(), (s, t, p) => Session.ExecuteAsync(s, CommandType.Text, p));
        }
#endif

        /// <summary>插入数据</summary>
        /// <param name="tableName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [Obsolete]
        public Int32 Insert(String tableName, Object data) => Insert(data, tableName);

        /// <summary>更新数据</summary>
        /// <param name="tableName"></param>
        /// <param name="data"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        [Obsolete]
        public Int32 Update(String tableName, Object data, Object where) => Update(data, where, tableName);
        #endregion
    }
}