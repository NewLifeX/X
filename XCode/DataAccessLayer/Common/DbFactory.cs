using System;
using NewLife.Collections;
using NewLife.Model;
using NewLife.Reflection;
using XCode.Model;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库工厂</summary>
    public static class DbFactory
    {
        #region 创建
        /// <summary>根据数据库类型创建提供者</summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static IDatabase Create(DatabaseType dbType)
        {
            return GetDefault(dbType);
        }
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
                .Reg<SqlCe>()
                .Reg<Access>(String.Empty);
            // Access作为默认实现
        }

        private static IObjectContainer Reg<T>(this IObjectContainer container, Object id = null)
        {
            IDatabase db = TypeX.CreateInstance(typeof(T)) as IDatabase;
            if (id == null) id = db.DbType;

            // 把这个实例注册进去，作为默认实现
            return container.Register(typeof(IDatabase), null, db, id);
        }
        #endregion

        #region 默认提供者
        private static DictionaryCache<DatabaseType, IDatabase> defaultDbs = new DictionaryCache<DatabaseType, IDatabase>();
        /// <summary>根据名称获取默认提供者</summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static IDatabase GetDefault(DatabaseType dbType)
        {
            return defaultDbs.GetItem(dbType, dt => (IDatabase)TypeX.CreateInstance(XCodeService.ResolveType<IDatabase>(dt)));
        }

        private static DictionaryCache<Type, IDatabase> defaultDbs2 = new DictionaryCache<Type, IDatabase>();
        /// <summary>根据名称获取默认提供者</summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static IDatabase GetDefault(Type dbType)
        {
            if (dbType == null) return null;
            return defaultDbs2.GetItem(dbType, dt => (IDatabase)TypeX.CreateInstance(dt));
        }
        #endregion

        #region 方法
        /// <summary>从提供者和连接字符串猜测数据库处理器</summary>
        /// <param name="connStr"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        internal static Type GetProviderType(String connStr, String provider)
        {
            if (!String.IsNullOrEmpty(provider))
            {
                Type type = XCodeService.ResolveType<IDatabase>(m => "" + m.Identity != "" && GetDefault((DatabaseType)m.Identity).Support(provider));
                if (type != null) return type;

                type = TypeX.GetType(provider, true);
                XCodeService.Register<IDatabase>(type, provider);
                return type;
            }
            else
            {
                // 这里的默认值来自于上面Reg里面的最后那个
                return XCodeService.ResolveType<IDatabase>(String.Empty);
            }
        }
        #endregion
    }
}