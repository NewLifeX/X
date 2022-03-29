﻿using System;
using System.Data;
using System.Data.Common;
using NewLife;
using NewLife.Log;

namespace XCode.DataAccessLayer
{
    /// <summary>远程数据库。一般是分为客户端服务器的中大型数据库，该类数据库支持完整的SQL92</summary>
    abstract class RemoteDb : DbBase
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public virtual String SystemDatabaseName => "master";

        private String _User;
        /// <summary>用户名UserID</summary>
        public String User
        {
            get
            {
                if (_User != null) return _User;

                var connStr = ConnectionString;

                if (String.IsNullOrEmpty(connStr)) return null;

                var ocsb = Factory.CreateConnectionStringBuilder();
                ocsb.ConnectionString = connStr;

                if (ocsb.ContainsKey("User ID"))
                    _User = (String)ocsb["User ID"];
                else if (ocsb.ContainsKey("User"))
                    _User = (String)ocsb["User"];
                else if (ocsb.ContainsKey("uid"))
                    _User = (String)ocsb["uid"];
                else
                    _User = String.Empty;

                return _User;
            }
        }
        #endregion

        #region 分页
        /// <summary>已重写。获取分页</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">主键列。用于not in分页</param>
        /// <returns></returns>
        public override String PageSplit(String sql, Int64 startRowIndex, Int64 maximumRows, String keyColumn) => PageSplitByLimit(sql, startRowIndex, maximumRows);

        /// <summary>构造分页SQL</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public override SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows) => PageSplitByLimit(builder, startRowIndex, maximumRows);

        /// <summary>已重写。获取分页</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        public static String PageSplitByLimit(String sql, Int64 startRowIndex, Int64 maximumRows)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1) return sql;

                return $"{sql} limit {maximumRows}";
            }
            if (maximumRows < 1) throw new NotSupportedException("不支持取第几条数据之后的所有数据！");

            return $"{sql} limit {startRowIndex}, {maximumRows}";
        }

        /// <summary>构造分页SQL</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public static SelectBuilder PageSplitByLimit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows > 0) builder.Limit = $"limit {maximumRows}";
                return builder;
            }
            if (maximumRows < 1) throw new NotSupportedException("不支持取第几条数据之后的所有数据！");

            builder.Limit = $"limit {startRowIndex}, {maximumRows}";
            return builder;
        }
        #endregion
    }

    /// <summary>远程数据库会话</summary>
    abstract class RemoteDbSession : DbSession
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public String SystemDatabaseName => (Database as RemoteDb)?.SystemDatabaseName;
        #endregion

        #region 构造函数
        public RemoteDbSession(IDatabase db) : base(db) { }
        #endregion

        #region 架构
        public override DataTable GetSchema(DbConnection conn, String collectionName, String[] restrictionValues)
        {
            try
            {
                return base.GetSchema(conn, collectionName, restrictionValues);
            }
            catch (Exception ex)
            {
                DAL.WriteLog("[{2}]GetSchema({0})异常重试！{1}", collectionName, ex.Message, Database.ConnName);

                // 如果没有数据库，登录会失败，需要切换到系统数据库再试试
                return ProcessWithSystem((s, c) => base.GetSchema(c, collectionName, restrictionValues)) as DataTable;
            }
        }
        #endregion

        #region 系统权限处理
        public Object ProcessWithSystem(Func<IDbSession, DbConnection, Object> callback)
        {
            var dbname = Database.DatabaseName;
            var sysdbname = SystemDatabaseName;

            // 如果指定了数据库名，并且不是master，则切换到master
            if (!dbname.IsNullOrEmpty() && !dbname.EqualIgnoreCase(sysdbname))
            {
                if (DAL.Debug) WriteLog("切换到系统库[{0}]", sysdbname);
                using var conn = Database.Factory.CreateConnection();
                try
                {
                    //conn.ConnectionString = Database.ConnectionString;

                    OpenDatabase(conn, Database.ConnectionString, sysdbname);

                    return callback(this, conn);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                    throw;
                }
                finally
                {
                    if (DAL.Debug) WriteLog("退出系统库[{0}]，回到[{1}]", sysdbname, dbname);
                }
            }
            else
            {
                using var conn = Database.OpenConnection();
                return callback(this, conn);
            }
        }

        private static void OpenDatabase(IDbConnection conn, String connStr, String dbName)
        {
            // 如果没有打开，则改变链接字符串
            var builder = new ConnectionStringBuilder(connStr);
            var flag = false;
            if (builder["Database"] != null)
            {
                builder["Database"] = dbName;
                flag = true;
            }
            else if (builder["Initial Catalog"] != null)
            {
                builder["Initial Catalog"] = dbName;
                flag = true;
            }
            if (flag)
            {
                connStr = builder.ToString();
                //WriteLog("系统级：{0}", connStr);
            }

            conn.ConnectionString = connStr;
            conn.Open();
        }
        #endregion
    }

    /// <summary>远程数据库元数据</summary>
    abstract class RemoteDbMetaData : DbMetaData
    {
        #region 属性
        #endregion

        #region 架构定义
        public override Object SetSchema(DDLSchema schema, params Object[] values)
        {
            var session = Database.CreateSession();
            var databaseName = Database.DatabaseName;

            // ahuang 2014.06.12  类型强制转string的bug
            if (values != null && values.Length > 0 && values[0] is String str && !str.IsNullOrEmpty()) databaseName = str;

            switch (schema)
            {
                //case DDLSchema.TableExist:
                //    return session.QueryCount(GetSchemaSQL(schema, values)) > 0;

                case DDLSchema.DatabaseExist:
                    return DatabaseExist(databaseName);

                case DDLSchema.CreateDatabase:
                    values = new Object[] { databaseName, values == null || values.Length < 2 ? null : values[1] };

                    var sql = base.GetSchemaSQL(schema, values);
                    if (sql.IsNullOrEmpty()) return null;

                    if (session is RemoteDbSession ss)
                    {
                        ss.WriteSQL(sql);
                        return ss.ProcessWithSystem((s, c) =>
                        {
                            using var cmd = Database.Factory.CreateCommand();
                            cmd.Connection = c;
                            cmd.CommandText = sql;

                            return cmd.ExecuteNonQuery();
                        });
                    }

                    return 0;

                //case DDLSchema.DropDatabase:
                //    return DropDatabase(databaseName);

                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }

        protected virtual Boolean DatabaseExist(String databaseName)
        {
            var session = Database.CreateSession();
            return session.QueryCount(GetSchemaSQL(DDLSchema.DatabaseExist, new Object[] { databaseName })) > 0;
        }

        //protected virtual Boolean DropDatabase(String databaseName)
        //{
        //    var session = Database.CreateSession();
        //    var sql = DropDatabaseSQL(databaseName);
        //    if (sql.IsNullOrEmpty()) return session.Execute(sql) > 0;

        //    return true;
        //}

        //Object ProcessWithSystem(Func<IDbSession, Object> callback) => (Database.CreateSession() as RemoteDbSession).ProcessWithSystem((s, c) => callback(s));
        #endregion
    }
}