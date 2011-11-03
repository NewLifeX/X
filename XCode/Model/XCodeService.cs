using System;
using NewLife.Model;
using NewLife.Reflection;
using XCode.Accessors;
using XCode.DataAccessLayer;

namespace XCode.Model
{
    /// <summary>
    /// XCode服务对象提供者
    /// </summary>
    class XCodeService : ServiceContainer<XCodeService>
    {
        static XCodeService()
        {
            IObjectContainer container = Container;
            container.Register<IDataTable, XTable>(null, 0)
                .Register<IDataRowEntityAccessorProvider, DataRowEntityAccessorProvider>(null, 0);

            DbFactory.Reg(container);

            EntityAccessorFactory.Reg(container);
        }

        ///// <summary>对象容器</summary>
        //static IObjectContainer Container { get { return ObjectContainer.Current; } }

        #region 方法
        //public static void Register<T>(Type impl, String name)
        //{
        //    Container.Register(typeof(T), impl, name);
        //}

        ///// <summary>
        ///// 解析类型指定名称的实例
        ///// </summary>
        ///// <typeparam name="TInterface">接口类型</typeparam>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //public static TInterface Resolve<TInterface>(String name)
        //{
        //    return Container.Resolve<TInterface>(name);
        //}

        //public static Type ResolveType<TInterface>(String name)
        //{
        //    return Container.ResolveType(typeof(TInterface), name);
        //}

        public static Type ResolveType<TInterface>(Func<IObjectMap, Boolean> func)
        {
            foreach (IObjectMap item in Container.ResolveAllMaps(typeof(TInterface)))
            {
                if (func(item)) return item.ImplementType;
            }

            return null;
        }
        #endregion

        #region 使用
        /// <summary>
        /// 创建模型数据表
        /// </summary>
        /// <returns></returns>
        public static IDataTable CreateTable()
        {
            return Container.Resolve<IDataTable>();
        }

        /// <summary>
        /// 创建实体类的数据行访问器
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static IDataRowEntityAccessor CreateDataRowEntityAccessor(Type entityType)
        {
            return Container.Resolve<IDataRowEntityAccessorProvider>().CreateDataRowEntityAccessor(entityType);
        }
        #endregion
    }
}