﻿using System;
using NewLife.Model;
using XCode.DataAccessLayer;

namespace XCode.Model
{
    /// <summary>XCode服务对象提供者</summary>
    class XCodeService
    {
        #region 当前静态服务容器
        /// <summary>当前对象容器</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        static XCodeService()
        {
            var container = Container;
            container.Register<IDataTable, XTable>()
                .AutoRegister<IDataRowEntityAccessorProvider, DataRowEntityAccessorProvider>()
                //.AutoRegister<IEntityPersistence, EntityPersistence>()
                .AutoRegister<IModelResolver, ModelResolver>()
                .AutoRegister<IEntityAddition, EntityAddition>();

            //DbFactory.Reg(container);
        }

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
            return Container.ResolveInstance<IDataRowEntityAccessorProvider>().CreateDataRowEntityAccessor(entityType);
        }
        #endregion
    }
}