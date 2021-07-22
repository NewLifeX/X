using System;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using NewLife.Collections;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库工厂</summary>
    public static class DbFactory
    {
        #region 静态构造
        static DbFactory()
        {
            Register<SQLite>(DatabaseType.SQLite);
            Register<MySql>(DatabaseType.MySql);
            Register<Oracle>(DatabaseType.Oracle);
            Register<SqlServer>(DatabaseType.SqlServer);
            Register<PostgreSQL>(DatabaseType.PostgreSQL);
            Register<DaMeng>(DatabaseType.DaMeng);
            Register<DB2>(DatabaseType.DB2);
#if !__CORE__
            Register<Access>(DatabaseType.Access);
            Register<SqlCe>(DatabaseType.SqlCe);
#endif
            Register<Network>(DatabaseType.Network);
        }
        #endregion

        #region 提供者
        private static readonly IDictionary<DatabaseType, IDatabase> _dbs = new NullableDictionary<DatabaseType, IDatabase>();
        /// <summary>注册数据库提供者</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbType"></param>
        public static void Register<T>(DatabaseType dbType) where T : IDatabase, new() => _dbs[dbType] = new T();

        /// <summary>根据数据库类型创建提供者</summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static IDatabase Create(DatabaseType dbType) => _dbs[dbType]?.GetType().CreateInstance() as IDatabase;

        /// <summary>根据名称获取默认提供者</summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        internal static IDatabase GetDefault(Type dbType) => _dbs.Values.FirstOrDefault(e => e.GetType() == dbType);

        internal static IDatabase GetDefault(DatabaseType dbType) => _dbs[dbType];
        #endregion

        #region 方法
        /// <summary>从提供者和连接字符串猜测数据库处理器</summary>
        /// <param name="connStr"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static Type GetProviderType(String connStr, String provider)
        {
            // 尝试从连接字符串获取优先提供者
            if (!connStr.IsNullOrWhiteSpace())
            {
                var builder = new ConnectionStringBuilder(connStr);
                if (builder.TryGetValue("provider", out var prv))
                {
                    foreach (var item in _dbs)
                    {
                        if (item.Value.Support(prv)) return item.Value.GetType();
                    }
                }
            }

            // 尝试解析提供者
            if (!provider.IsNullOrEmpty())
            {
                var n = 0;
                foreach (var item in _dbs)
                {
                    n++;

                    if (item.Value.Support(provider)) return item.Value.GetType();
                }

                if (DAL.Debug) DAL.WriteLog("无法从{0}个默认数据库提供者中识别到{1}！", n, provider);

                // 注册外部提供者
                var type = provider.GetTypeEx(true);
                if (type != null)
                {
                    if (type.CreateInstance() is IDatabase db) _dbs[db.Type] = db;
                }

                return type;
            }

            // 默认SQLite
            return _dbs[DatabaseType.SQLite].GetType();
        }
        #endregion
    }
}