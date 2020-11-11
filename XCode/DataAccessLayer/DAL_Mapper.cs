using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NewLife;
using NewLife.Collections;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    public partial class DAL
    {
        #region 查询
        /// <summary>查询Sql并映射为结果集</summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="sql">Sql语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(String sql, Object param = null)
        {
            //var ps = param?.ToDictionary();
            var dt = QueryByCache(sql, param, "", (s, p, k3) => Session.Query(s, Db.CreateParameters(p)), nameof(Query));

            // 优先特殊处理基础类型，选择第一字段
            if (Type.GetTypeCode(typeof(T)) != TypeCode.Object) return dt.Rows.Select(e => e[0].ChangeType<T>());

            return dt.ReadModels<T>();
        }

        /// <summary>执行Sql</summary>
        /// <param name="sql">Sql语句</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        public Int32 Execute(String sql, Object param = null)
        {
            //var ps = param?.ToDictionary();
            return ExecuteByCache(sql, "", param, (s, t, p) => Session.Execute(s, CommandType.Text, Db.CreateParameters(p)));
        }

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
        public T ExecuteScalar<T>(String sql, Object param = null)
        {
            //var ps = param?.ToDictionary();
            //return Session.ExecuteScalar<T>(sql, CommandType.Text, Db.CreateParameters(ps));
            return QueryByCache(sql, param, "", (s, p, k3) => Session.ExecuteScalar<T>(s, CommandType.Text, Db.CreateParameters(p)), nameof(ExecuteScalar));
        }

        /// <summary>插入数据</summary>
        /// <param name="tableName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Int32 Insert(String tableName, Object data)
        {
            if (tableName.IsNullOrEmpty()) tableName = data.GetType().Name;

            var pis = data.ToDictionary();
            var dps = Db.CreateParameters(data);
            var ns = pis.Join(",", e => e.Key);
            var vs = dps.Join(",", e => e.ParameterName);
            var sql = $"Insert Into {tableName}({ns}) Values({vs})";

            return ExecuteByCache(sql, "", dps, (s, t, p) => Session.Execute(s, CommandType.Text, p));
        }

        /// <summary>更新数据</summary>
        /// <param name="tableName"></param>
        /// <param name="data"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public Int32 Update(String tableName, Object data, Object where)
        {
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

            var sql = sb.Put(true);

            return ExecuteByCache(sql, "", dps.ToArray(), (s, t, p) => Session.Execute(s, CommandType.Text, p));
        }

        /// <summary>删除数据</summary>
        /// <param name="tableName"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public Int32 Delete(String tableName, Object where)
        {
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
        #endregion
    }
}