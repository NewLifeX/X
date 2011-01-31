using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 网络数据库。一般是分为客户端服务器的中大型数据库，该类数据库支持完整的SQL92
    /// </summary>
    abstract class NetDb : DbBase
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public virtual String SystemDatabaseName { get { return "master"; } }
        #endregion
    }

    /// <summary>
    /// 网络数据库会话
    /// </summary>
    abstract class NetDbSession : DbSession
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public String SystemDatabaseName { get { return Database is NetDb ? (Database as NetDb).SystemDatabaseName : null; } }
        #endregion

        #region 架构
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            try
            {
                return base.GetSchema(collectionName, restrictionValues);
            }
            catch
            {
                String dbname = DatabaseName;
                if (dbname != SystemDatabaseName) DatabaseName = SystemDatabaseName;
                DataTable dt = base.GetSchema(collectionName, restrictionValues);
                if (dbname != SystemDatabaseName) DatabaseName = dbname;
                return dt;
            }
        }
        #endregion
    }

    /// <summary>
    /// 网络数据库元数据
    /// </summary>
    abstract class NetDbMetaData : DbMetaData
    {
        #region 属性
        /// <summary>系统数据库名</summary>
        public String SystemDatabaseName { get { return Database is NetDb ? (Database as NetDb).SystemDatabaseName : null; } }
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
                        obj = session.QueryCount(GetSchemaSQL(schema, values)) > 0;
                        session.DatabaseName = dbname;
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
                    obj = base.SetSchema(schema, values);
                    session.DatabaseName = dbname;
                    return obj;
                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }
        #endregion
    }
}
