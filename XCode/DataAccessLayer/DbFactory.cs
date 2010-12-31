//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace XCode.DataAccessLayer
//{
//    /// <summary>
//    /// 数据库工厂
//    /// </summary>
//    public static class DbFactory
//    {
//        #region 属性
//        /// <summary>内建数据库</summary>
//        private static Dictionary<DatabaseType, IDatabaseMeta> dbs = new Dictionary<DatabaseType, IDatabaseMeta>();
//        #endregion

//        #region 创建
//        public static void Register(DatabaseType dbType, IDatabaseMeta db)
//        {
//            lock (dbs)
//            {
//                if (dbs.ContainsKey(dbType))
//                    dbs[dbType] = db;
//                else
//                    dbs.Add(dbType, db);
//            }
//        }

//        public static IDatabaseMeta Create(DatabaseType dbType)
//        {
//            IDatabaseMeta db = null;
//            if (dbs.TryGetValue(dbType, out db)) return db;

//            db = BuildinCreate(dbType);
//            if (db == null) return null;

//            Register(dbType, db);

//            return db;
//        }

//        private static IDatabaseMeta BuildinCreate(DatabaseType dbType)
//        {
//            switch (dbType)
//            {
//                case DatabaseType.Access:
//                    return new Access();
//                case DatabaseType.SqlServer:
//                    return new SqlServer();
//                case DatabaseType.Oracle:
//                    return new Oracle();
//                case DatabaseType.MySql:
//                    return new MySql();
//                case DatabaseType.SqlServer2005:
//                    return new SqlServer2005();
//                case DatabaseType.SQLite:
//                    return new SQLite();
//                default:
//                    break;
//            }

//            return null;
//        }
//        #endregion
//    }
//}