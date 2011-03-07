using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库工厂
    /// </summary>
    public static class DbFactory
    {
        #region 属性
        /// <summary>内建数据库</summary>
        private static Dictionary<DatabaseType, IDatabase> dbs = new Dictionary<DatabaseType, IDatabase>();
        #endregion

        #region 创建
        /// <summary>
        /// 注册数据库提供者
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="db"></param>
        public static void Register(DatabaseType dbType, IDatabase db)
        {
            lock (dbs)
            {
                if (dbs.ContainsKey(dbType))
                    dbs[dbType] = db;
                else
                    dbs.Add(dbType, db);
            }
        }

        /// <summary>
        /// 根据数据库类型创建提供者
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static IDatabase Create(DatabaseType dbType)
        {
            if (dbType == DatabaseType.Other) return null;

            IDatabase db = null;
            if (dbs.TryGetValue(dbType, out db)) return db;

            db = BuildinCreate(dbType);
            if (db == null) return null;

            Register(dbType, db);

            return db;
        }

        private static IDatabase BuildinCreate(DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.Access:
                    return new Access();
                case DatabaseType.SqlServer:
                    return new SqlServer();
                case DatabaseType.Oracle:
                    return new Oracle();
                case DatabaseType.MySql:
                    return new MySql();
                case DatabaseType.SqlCe:
                    return new SqlCe();
                case DatabaseType.SQLite:
                    return new SQLite();
                case DatabaseType.Firebird:
                    return new Firebird();
                case DatabaseType.PostgreSQL:
                    return new PostgreSQL();
                default:
                    break;
            }

            return null;
        }
        #endregion
    }
}