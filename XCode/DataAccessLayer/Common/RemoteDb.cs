using System;
using System.Data;
using System.Threading;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>远程数据库。一般是分为客户端服务器的中大型数据库，该类数据库支持完整的SQL92</summary>
    abstract class RemoteDb : DbBase
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public virtual String SystemDatabaseName { get { return "master"; } }

        private String _ServerVersion;
        /// <summary>数据库服务器版本</summary>
        public override String ServerVersion
        {
            get
            {
                if (_ServerVersion != null) return _ServerVersion;
                _ServerVersion = String.Empty;

                var session = CreateSession() as RemoteDbSession;
                _ServerVersion = session.ProcessWithSystem(s =>
                {
                    if (!session.Opened) session.Open();
                    try
                    {
                        return session.Conn.ServerVersion;
                    }
                    finally
                    {
                        session.AutoClose();
                    }
                }) as String;

                return _ServerVersion;
            }
        }

        protected override string DefaultConnectionString
        {
            get
            {
                var builder = Factory.CreateConnectionStringBuilder();
                if (builder != null)
                {
                    builder["Server"] = "127.0.0.1";
                    // Oracle连接字符串不支持Database关键字
                    if (DbType != DatabaseType.Oracle) builder["Database"] = SystemDatabaseName;
                    return builder.ToString();
                }

                return base.DefaultConnectionString;
            }
        }
        #endregion
    }

    /// <summary>远程数据库会话</summary>
    abstract class RemoteDbSession : DbSession
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public String SystemDatabaseName
        {
            get
            {
                //return Database is RemoteDb ? (Database as RemoteDb).SystemDatabaseName : null;
                // 减少一步类型转换
                var remotedb = Database as RemoteDb;
                return remotedb != null ? remotedb.SystemDatabaseName : null;
            }
        }
        #endregion

        #region 架构
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            //try
            //{
            return base.GetSchema(collectionName, restrictionValues);
            //}
            //catch (Exception ex)
            //{
            //    DAL.WriteDebugLog("GetSchema({0})异常重试！{1},连接字符串 {2}", collectionName, ex.Message, ConnectionString);
            //}
        }
        #endregion

        #region 系统权限处理
        public Object ProcessWithSystem(Func<IDbSession, Object> callback)
        {
            var session = this;
            var dbname = session.DatabaseName;
            var sysdbname = SystemDatabaseName;

            //如果指定了数据库名，并且不是master，则切换到master
            if (!String.IsNullOrEmpty(dbname) && !dbname.EqualIgnoreCase(sysdbname))
            {
                session.DatabaseName = sysdbname;
                try
                {
                    return callback(session);
                }
                finally
                {
                    session.DatabaseName = dbname;
                }
            }
            else
            {
                return callback(session);
            }
        }
        #endregion
    }

    /// <summary>远程数据库元数据</summary>
    abstract class RemoteDbMetaData : DbMetaData
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public String SystemDatabaseName
        {
            get
            {
                //return Database is RemoteDb ? (Database as RemoteDb).SystemDatabaseName : null;
                // 减少一步类型转换
                var remotedb = Database as RemoteDb;
                return remotedb != null ? remotedb.SystemDatabaseName : null;
            }
        }
        #endregion

        #region 架构定义
        public override object SetSchema(DDLSchema schema, params object[] values)
        {
            var session = Database.CreateSession();
            var databaseName = session.DatabaseName;

            if (values != null && values.Length > 0 && values[0] is String && values[0] + "" != "") databaseName = (String)values[0];

            switch (schema)
            {
                case DDLSchema.TableExist:
                    return session.QueryCount(GetSchemaSQL(schema, values)) > 0;

                case DDLSchema.DatabaseExist:
                    return ProcessWithSystem(s => DatabaseExist(databaseName));

                case DDLSchema.CreateDatabase:
                    values = new Object[] { databaseName, values == null || values.Length < 2 ? null : values[1] };

                    var obj = ProcessWithSystem(s => base.SetSchema(schema, values));

                    // 创建数据库后，需要等待它初始化
                    Thread.Sleep(5000);

                    return obj;

                case DDLSchema.DropDatabase:
                    return ProcessWithSystem(s => DropDatabase(databaseName));

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

        protected virtual Boolean DropDatabase(String databaseName)
        {
            return (Boolean)base.SetSchema(DDLSchema.DropDatabase, new Object[] { databaseName });
        }

        Object ProcessWithSystem(Func<IDbSession, Object> callback)
        {
            return (Database.CreateSession() as RemoteDbSession).ProcessWithSystem(callback);
        }
        #endregion
    }
}
