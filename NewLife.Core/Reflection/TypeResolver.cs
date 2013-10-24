//using System;
//using NewLife.Configuration;
//using NewLife.Exceptions;

//namespace NewLife.Reflection
//{
//    /// <summary>类型解析器</summary>
//    [Obsolete("该类在后续版本中将不再被支持！")]
//    public static class TypeResolver
//    {
//        #region 提供者
//        /// <summary>内部提供者</summary>
//        private static TypeResolverProvider provider = new TypeResolverProvider();

//        private static ITypeResolverProvider _Provider;
//        /// <summary>提供者</summary>
//        public static ITypeResolverProvider Provider
//        {
//            get
//            {
//                if (_Provider == null)
//                {
//                    Type type = null;

//                    String str = Config.GetConfig<String>("NewLife.Reflection.TypeResolverProvier");
//                    if (!String.IsNullOrEmpty(str))
//                    {
//                        type = TypeX.GetType(str, true);
//                        if (type == null) throw new XException("无法找到实体资格提供者" + str);
//                    }

//                    // 使用默认的提供者找提供者
//                    if (type == null) type = provider.Resolve(typeof(ITypeResolverProvider), new Type[] { typeof(TypeResolverProvider) });

//                    if (type != null) _Provider = Activator.CreateInstance(type) as ITypeResolverProvider;

//                    if (_Provider == null) _Provider = provider;
//                }
//                return _Provider;
//            }
//            set
//            {
//                if (_Provider != value)
//                {
//                    _Provider = value;

//                    //entityTypeCache.Clear();
//                }
//            }
//        }
//        #endregion

//        #region 注册
//        /// <summary>
//        /// 注册类型，同一基类，获取时以最后一个类为准
//        /// </summary>
//        /// <param name="baseType">基类</param>
//        /// <param name="type">要注册的类型</param>
//        /// <returns>返回自身，以便于连续注册</returns>
//        public static ITypeResolverProvider Register(Type baseType, Type type)
//        {
//            return Provider.Register(baseType, type);
//        }

//        /// <summary>
//        /// 注册类型
//        /// </summary>
//        /// <typeparam name="TBase">基类</typeparam>
//        /// <typeparam name="T">要注册的类型</typeparam>
//        /// <returns>返回自身，以便于连续注册</returns>
//        public static ITypeResolverProvider Register<TBase, T>()
//        {
//            return Register(typeof(TBase), typeof(T));
//        }

//        #endregion

//        #region 解析
//        /// <summary>
//        /// 解析类型。
//        /// </summary>
//        /// <param name="baseType">基类</param>
//        /// <param name="excludeTypes">排除类型</param>
//        /// <returns></returns>
//        public static Type Resolve(Type baseType, Type[] excludeTypes)
//        {
//            return Provider.Resolve(baseType, excludeTypes);
//        }

//        /// <summary>
//        /// 解析类型
//        /// </summary>
//        /// <typeparam name="TBase"></typeparam>
//        /// <typeparam name="TExclude"></typeparam>
//        /// <returns></returns>
//        public static Type Resolve<TBase, TExclude>()
//        {
//            return Resolve(typeof(TBase), new Type[] { typeof(TExclude) });
//        }

//        /// <summary>
//        /// 解析类型
//        /// </summary>
//        /// <typeparam name="TBase"></typeparam>
//        /// <returns></returns>
//        public static Type Resolve<TBase>()
//        {
//            return Resolve(typeof(TBase), null);
//        }

//        /// <summary>
//        /// 解析类型
//        /// </summary>
//        /// <param name="baseType"></param>
//        /// <returns></returns>
//        public static Type[] ResolveAll(Type baseType)
//        {
//            return Provider.ResolveAll(baseType);
//        }

//        /// <summary>
//        /// 解析类型
//        /// </summary>
//        /// <typeparam name="TBase"></typeparam>
//        /// <returns></returns>
//        public static Type[] ResolveAll<TBase>()
//        {
//            return ResolveAll(typeof(TBase));
//        }
//        #endregion

//        #region 方法
//        /// <summary>
//        /// 获取对象
//        /// </summary>
//        /// <param name="baseType"></param>
//        /// <returns></returns>
//        public static Object GetObject(Type baseType)
//        {
//            Type type = TypeResolver.Resolve(baseType, null);
//            if (type == null) throw new XException("无法找到" + baseType.FullName + "的实现者！");

//            return TypeX.CreateInstance(type, null);
//        }

//        /// <summary>
//        /// 取得指定类型的静态属性值
//        /// </summary>
//        /// <param name="baseType"></param>
//        /// <param name="propertyName">属性名</param>
//        /// <returns></returns>
//        public static Object GetPropertyValue(Type baseType, String propertyName)
//        {
//            Type type = TypeResolver.Resolve(baseType, null);
//            if (type == null) throw new XException("无法找到" + baseType.FullName + "的实现者！");

//            return PropertyInfoX.Create(type, propertyName).GetValue();
//        }

//        /// <summary>
//        /// 调用指定类型的静态方法
//        /// </summary>
//        /// <param name="baseType"></param>
//        /// <param name="methodName"></param>
//        /// <param name="parameters">参数数组</param>
//        /// <returns></returns>
//        public static Object Invoke(Type baseType, String methodName, params Object[] parameters)
//        {
//            Type type = TypeResolver.Resolve(baseType, null);
//            if (type == null) throw new XException("无法找到" + baseType.FullName + "的实现者！");

//            return MethodInfoX.Create(type, methodName).Invoke(null, parameters);
//        }
//        #endregion
//    }
//}