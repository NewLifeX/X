using System;
using NewLife.Configuration;
using NewLife.Exceptions;
using NewLife.Reflection;
using NewLife.Collections;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 实体资格
    /// </summary>
    public static class EntityShip
    {
        #region 提供者
        private static IEntityShipProvider _Provider;
        /// <summary>提供者</summary>
        public static IEntityShipProvider Provider
        {
            get
            {
                if (_Provider == null)
                {
                    String str = Config.GetConfig<String>("NewLife.CommonEntity.EntityShipProvier");
                    if (!String.IsNullOrEmpty(str))
                    {
                        Type type = TypeX.GetType(str, true);
                        if (type == null) throw new XException("无法找到实体资格提供者" + str);

                        _Provider = Activator.CreateInstance(type) as IEntityShipProvider;
                    }

                    if (_Provider == null) _Provider = new EntityShipProvider();
                }
                return _Provider;
            }
            set
            {
                if (_Provider != value)
                {
                    _Provider = value;

                    entityTypeCache.Clear();
                }
            }
        }
        #endregion

        #region 方法
        static DictionaryCache<Type, Type> entityTypeCache = new DictionaryCache<Type, Type>();
        /// <summary>
        /// 获取实体类型。默认扫描所有程序集，包括未加载程序集，优先返回非默认类型的实体类型。
        /// </summary>
        /// <param name="pluginType">约束插件类型</param>
        /// <param name="defaultType">默认类型</param>
        /// <returns></returns>
        public static Type GetEntityType(Type pluginType, Type defaultType)
        {
            return entityTypeCache.GetItem<Type>(pluginType, defaultType, Provider.GetEntityType);
            //return Provider.GetEntityType(pluginType, defaultType);
        }

        /// <summary>
        /// 取得指定实体类型的静态属性值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        public static T GetEntityPropertyValue<T>(String propertyName)
        {
            Type type = EntityShip.GetEntityType(typeof(T), null);
            if (type == null) throw new XException("无法找到实体" + typeof(T).FullName + "的实现者！");

            return (T)PropertyInfoX.Create(type, propertyName).GetValue();
        }

        /// <summary>
        /// 调用指定实体类型的静态方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Object Invoke<T>(String methodName, params Object[] parameters)
        {
            Type type = EntityShip.GetEntityType(typeof(T), null);
            if (type == null) throw new XException("无法找到实体" + typeof(T).FullName + "的实现者！");

            return MethodInfoX.Create(type, methodName).Invoke(null, parameters);
        }
        #endregion
    }
}