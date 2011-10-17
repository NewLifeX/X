using System;
using NewLife.Collections;
using NewLife.Model;
using NewLife.Reflection;
using XCode.Model;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库工厂
    /// </summary>
    public static class DbFactory
    {
        #region 属性
        ///// <summary>内建数据库</summary>
        //private static Dictionary<DatabaseType, IDatabase> dbs = new Dictionary<DatabaseType, IDatabase>();
        #endregion

        #region 创建
        ///// <summary>
        ///// 注册数据库提供者
        ///// </summary>
        ///// <param name="dbType"></param>
        ///// <param name="db"></param>
        //public static void Register(DatabaseType dbType, IDatabase db)
        //{
        //    lock (dbs)
        //    {
        //        //if (dbs.ContainsKey(dbType))
        //        dbs[dbType] = db;
        //        //else
        //        //    dbs.Add(dbType, db);
        //    }
        //}

        /// <summary>
        /// 根据数据库类型创建提供者
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static IDatabase Create(DatabaseType dbType)
        {
            return GetDefault(dbType.ToString());

            //if (dbType == DatabaseType.Other) return null;

            //IDatabase db = null;
            //if (dbs.TryGetValue(dbType, out db)) return db;

            //db = BuildinCreate(dbType);
            //if (db == null) return null;

            //Register(dbType, db);

            //return db;
        }

        //private static IDatabase BuildinCreate(DatabaseType dbType)
        //{
        //    switch (dbType)
        //    {
        //        case DatabaseType.Access:
        //            return new Access();
        //        case DatabaseType.SqlServer:
        //            return new SqlServer();
        //        case DatabaseType.Oracle:
        //            return new Oracle();
        //        case DatabaseType.MySql:
        //            return new MySql();
        //        case DatabaseType.SqlCe:
        //            return new SqlCe();
        //        case DatabaseType.SQLite:
        //            return new SQLite();
        //        case DatabaseType.Firebird:
        //            return new Firebird();
        //        case DatabaseType.PostgreSQL:
        //            return new PostgreSQL();
        //        default:
        //            break;
        //    }

        //    return null;
        //}
        #endregion

        #region 静态构造
        internal static void Reg(IObjectContainer container)
        {
            container
                .Reg<Access>()
                .Reg<SqlServer>()
                .Reg<Oracle>()
                .Reg<MySql>()
                .Reg<SQLite>()
                .Reg<Firebird>()
                .Reg<PostgreSQL>()
                .Reg<SqlCe>();
        }

        private static IObjectContainer Reg<T>(this IObjectContainer container)
        {
            //return container.Register<IDatabase, T>(typeof(T).Name);
            IDatabase db = TypeX.CreateInstance(typeof(T)) as IDatabase;
            //return container.Register<IDatabase, T>(db.DbType.ToString());

            // 把这个实例注册进去，作为默认实现
            return container.Register(typeof(IDatabase), null, db, db.DbType.ToString(), false);
        }
        #endregion

        #region 默认提供者
        private static DictionaryCache<String, IDatabase> defaultDbs = new DictionaryCache<String, IDatabase>();
        /// <summary>
        /// 根据名称获取默认提供者
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IDatabase GetDefault(String providerName)
        {
            if (String.IsNullOrEmpty(providerName)) throw new ArgumentNullException("providerName");

            return defaultDbs.GetItem(providerName, name => (IDatabase)TypeX.CreateInstance(XCodeService.ResolveType<IDatabase>(name)));

            //return XCodeService.Resolve<IDatabase>(providerName);
        }
        #endregion

        #region 方法
        /// <summary>从提供者和连接字符串猜测数据库处理器</summary>
        /// <param name="connStr"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        internal static Type GetProviderType(String connStr, String provider)
        {
            //IObjectContainer container = XCodeService.Container;
            //String pname = provider.ToLower();
            //foreach (IDatabase item in container.ResolveAll<IDatabase>())
            //{
            //    if (item.Support(pname)) return item.GetType();
            //}
            //foreach (KeyValuePair<String, Type> item in container.ResolveAllNameTypes(typeof(IDatabase)))
            //{
            //    IDatabase db = DbFactory.GetDefault(item.Key);
            //    if (db.Support(pname)) return item.Value;
            //}

            Type type = XCodeService.ResolveType<IDatabase>(m => GetDefault(m.Name).Support(provider));
            if (type != null) return type;

            if (!String.IsNullOrEmpty(provider))
            {
                type = TypeX.GetType(provider, true);
                XCodeService.Register<IDatabase>(type, provider);
                return type;
            }
            else
            {
                return XCodeService.ResolveType<IDatabase>(String.Empty);
            }
        }
        #endregion
    }
}