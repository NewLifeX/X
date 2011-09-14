using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 远程数据库。一般是分为客户端服务器的中大型数据库，该类数据库支持完整的SQL92
    /// </summary>
    abstract class RemoteDb : DbBase
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public virtual String SystemDatabaseName { get { return "master"; } }

        private String _ServerVersion;
        /// <summary>
        /// 数据库服务器版本
        /// </summary>
        public override String ServerVersion
        {
            get
            {
                if (_ServerVersion != null) return _ServerVersion;
                _ServerVersion = String.Empty;

                IDbSession session = CreateSession();
                String dbname = session.DatabaseName;
                if (dbname != SystemDatabaseName) session.DatabaseName = SystemDatabaseName;
                if (!session.Opened) session.Open();
                try
                {
                    _ServerVersion = session.Conn.ServerVersion;

                    return _ServerVersion;
                }
                finally
                {
                    session.AutoClose();
                    if (dbname != SystemDatabaseName) session.DatabaseName = dbname;
                }
            }
        }

        protected override string DefaultConnectionString
        {
            get
            {
                DbConnectionStringBuilder builder = Factory.CreateConnectionStringBuilder();
                if (builder != null)
                {
                    builder["Data Source"] = "127.0.0.1";
                    builder["Database"] = SystemDatabaseName;
                    return builder.ToString();
                }

                return base.DefaultConnectionString;
            }
        }

        #endregion
    }

    /// <summary>
    /// 远程数据库会话
    /// </summary>
    abstract class RemoteDbSession : DbSession
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public String SystemDatabaseName { get { return Database is RemoteDb ? (Database as RemoteDb).SystemDatabaseName : null; } }
        #endregion

        #region 架构
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            try
            {
                return base.GetSchema(collectionName, restrictionValues);
            }
            catch (Exception ex)
            {
                DAL.WriteDebugLog("GetSchema({0})异常重试！{1}", collectionName, ex.Message);

                String dbname = DatabaseName;
                if (dbname != SystemDatabaseName) DatabaseName = SystemDatabaseName;
                DataTable dt = null;
                try
                {
                    dt = base.GetSchema(collectionName, restrictionValues);
                }
                finally
                {
                    if (dbname != SystemDatabaseName) DatabaseName = dbname;
                }
                return dt;
            }
        }
        #endregion
    }

    /// <summary>
    /// 远程数据库元数据
    /// </summary>
    abstract class RemoteDbMetaData : DbMetaData
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public String SystemDatabaseName { get { return Database is RemoteDb ? (Database as RemoteDb).SystemDatabaseName : null; } }
        #endregion

        #region 架构定义
        public override object SetSchema(DDLSchema schema, params object[] values)
        {
            IDbSession session = Database.CreateSession();

            Object obj = null;
            String dbname = String.Empty;
            String databaseName = String.Empty;
            String sysdbname = SystemDatabaseName;

            switch (schema)
            {
                case DDLSchema.DatabaseExist:
                    databaseName = values == null || values.Length < 1 ? null : (String)values[0];
                    if (String.IsNullOrEmpty(databaseName)) databaseName = session.DatabaseName;
                    values = new Object[] { databaseName };

                    dbname = session.DatabaseName;

                    //如果指定了数据库名，并且不是master，则切换到master
                    if (!String.IsNullOrEmpty(dbname) && !String.Equals(dbname, sysdbname, StringComparison.OrdinalIgnoreCase))
                    {
                        session.DatabaseName = sysdbname;
                        try
                        {
                            obj = session.QueryCount(GetSchemaSQL(schema, values)) > 0;
                        }
                        finally
                        {
                            session.DatabaseName = dbname;
                        }
                        return obj;
                    }
                    else
                    {
                        return session.QueryCount(GetSchemaSQL(schema, values)) > 0;
                    }
                case DDLSchema.TableExist:
                    return session.QueryCount(GetSchemaSQL(schema, values)) > 0;
                case DDLSchema.CreateDatabase:
                    databaseName = values == null || values.Length < 1 ? null : (String)values[0];
                    if (String.IsNullOrEmpty(databaseName)) databaseName = session.DatabaseName;
                    values = new Object[] { databaseName, values == null || values.Length < 2 ? null : values[1] };

                    dbname = session.DatabaseName;
                    session.DatabaseName = sysdbname;
                    try
                    {
                        obj = base.SetSchema(schema, values);
                    }
                    finally
                    {
                        session.DatabaseName = dbname;
                    }

                    // 创建数据库后，需要等待它初始化
                    Thread.Sleep(5000);

                    return obj;
                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }
        #endregion
    }
}
