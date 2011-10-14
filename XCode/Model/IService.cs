using System;
using NewLife.Configuration;
using NewLife.Model;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using System.Collections.Generic;
using NewLife.Collections;

namespace XCode.Model
{
    /// <summary>
    /// XCode服务对象提供者接口。暂时不稳定，外部请不要急于使用。
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// 创建模型数据表
        /// </summary>
        /// <returns></returns>
        IDataTable CreateTable();

        /// <summary>
        /// 创建实体类的数据行访问器
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        IDataRowEntityAccessor CreateDataRowEntityAccessor(Type entityType);
    }

    /// <summary>
    /// XCode服务对象提供者
    /// </summary>
    class XCodeService
    {
        /// <summary>对象容器</summary>
        public static IObjectContainer Conatiner { get { return ObjectContainer.Current; } }

        private static IService _Instance;
        /// <summary>提供者实例</summary>
        public static IService Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (typeof(IService))
                    {
                        if (_Instance == null)
                        {
                            _Instance = ServiceProvider.GetCurrentService<IService>();
                            if (_Instance != null) return _Instance;

                            if (_Instance == null)
                            {
                                String name = Config.GetConfig<String>("XCode.ServiceProvider");
                                if (!String.IsNullOrEmpty(name))
                                {
                                    Type type = TypeX.GetType(name);
                                    if (type != null && typeof(IService).IsAssignableFrom(type))
                                    {
                                        _Instance = TypeX.CreateInstance(type) as IService;
                                    }
                                }
                            }
                            if (_Instance == null) _Instance = new MyProvider();
                        }
                    }
                }
                return _Instance;
            }
        }

        class MyProvider : IService
        {
            #region IXCodeServiceProvider 成员

            public IDataTable CreateTable()
            {
                IDataTable dt = ServiceProvider.GetCurrentService<IDataTable>();
                if (dt != null) return dt;

                return new XTable();
            }

            DictionaryCache<Type, IDataRowEntityAccessor> _cache_dreAccessor = new DictionaryCache<Type, IDataRowEntityAccessor>();
            public IDataRowEntityAccessor CreateDataRowEntityAccessor(Type entityType)
            {
                return _cache_dreAccessor.GetItem(entityType, key => new DataRowEntityAccessor(key));
            }
            #endregion
        }
    }
}