using System;
using NewLife.Model;
using NewLife.Reflection;
using XCode.Accessors;
using XCode.DataAccessLayer;

namespace XCode.Model
{
    /// <summary>XCode服务对象提供者</summary>
    class XCodeService : ServiceContainer<XCodeService>
    {
        static XCodeService()
        {
            IObjectContainer container = Container;
            container.Register<IDataTable, XTable>()
                .Register<IDataRowEntityAccessorProvider>(new DataRowEntityAccessorProvider())
                .Register<IEntityPersistence>(new EntityPersistence());

            DbFactory.Reg(container);

            EntityAccessorFactory.Reg(container);
        }

        #region 方法
        public static Type ResolveType<TInterface>(Func<IObjectMap, Boolean> func)
        {
            foreach (var item in Container.ResolveAllMaps(typeof(TInterface)))
            {
                if (func(item)) return item.ImplementType;
            }

            return null;
        }
        #endregion

        #region 使用
        /// <summary>创建模型数据表</summary>
        /// <returns></returns>
        public static IDataTable CreateTable()
        {
            return Container.Resolve<IDataTable>();
        }

        /// <summary>创建实体类的数据行访问器</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static IDataRowEntityAccessor CreateDataRowEntityAccessor(Type entityType)
        {
            return Container.Resolve<IDataRowEntityAccessorProvider>().CreateDataRowEntityAccessor(entityType);
        }
        #endregion
    }
}