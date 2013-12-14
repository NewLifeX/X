using System;
using NewLife.Collections;
using NewLife.Log;
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
            return XCodeService.Container.ResolveInstance<IDatabase>(dbType);
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
            try
            {
                var db = typeof(T).CreateInstance() as IDatabase;
                if (id == null) id = db.DbType;

                // 把这个实例注册进去，作为默认实现
                return container.Register(typeof(IDatabase), null, db, id);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                throw;
            }
        }
        #endregion

        #region 默认提供者
        private static DictionaryCache<Type, IDatabase> defaultDbs2 = new DictionaryCache<Type, IDatabase>();
        /// <summary>根据名称获取默认提供者</summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        internal static IDatabase GetDefault(Type dbType)
        {
            if (dbType == null) return null;
            return defaultDbs2.GetItem(dbType, dt => (IDatabase)dt.CreateInstance());
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
                var n = 0;
                foreach (var item in XCodeService.Container.ResolveAll(typeof(IDatabase)))
                {
                    n++;
                    if ("" + item.Identity == "") continue;

                    var db = item.Instance as IDatabase;
                    if (db != null && db.Support(provider)) return item.ImplementType;
                }

                if (DAL.Debug) DAL.WriteLog("无法从{0}个默认数据库提供者中识别到{1}！", n, provider);

                var type = Reflect.GetTypeEx(provider, true);
                if (type != null) XCodeService.Register<IDatabase>(type, provider);
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