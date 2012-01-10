//using System;
//using System.Collections.Generic;
//using NewLife.Linq;
//using NewLife.Collections;

//namespace NewLife.Reflection
//{
//    /// <summary>
//    /// 类型解析器提供者接口
//    /// </summary>
//    [Obsolete("该成员在后续版本中将不再被支持！")]
//    public interface ITypeResolverProvider
//    {
//        /// <summary>
//        /// 注册类型，同一基类，解析时优先使用最后注册者
//        /// </summary>
//        /// <param name="baseType">基类</param>
//        /// <param name="type">要注册的类型</param>
//        /// <returns>返回自身，以便于连续注册</returns>
//        ITypeResolverProvider Register(Type baseType, Type type);

//        /// <summary>
//        /// 解析指定基类所注册的类型，排除指定类型，优先返回最后注册的类型
//        /// </summary>
//        /// <param name="baseType">基类</param>
//        /// <param name="excludeTypes">排除类型，当没有排除类型以外的类型时，忽略排除类型返回最后注册的类型</param>
//        /// <returns></returns>
//        Type Resolve(Type baseType, Type[] excludeTypes);

//        /// <summary>
//        /// 解析指定基类所注册的所有类型
//        /// </summary>
//        /// <param name="baseType"></param>
//        /// <returns></returns>
//        Type[] ResolveAll(Type baseType);
//    }

//    /// <summary>
//    /// 类型解析器提供者
//    /// </summary>
//    [Obsolete("该成员在后续版本中将不再被支持！")]
//    public class TypeResolverProvider : ITypeResolverProvider
//    {
//        #region 业务
//        private DictionaryCache<Type, List<Type>> _Maps = new DictionaryCache<Type, List<Type>>();
//        /// <summary>映射</summary>
//        protected virtual DictionaryCache<Type, List<Type>> Maps
//        {
//            get { return _Maps; }
//            set { _Maps = value; }
//        }

//        List<Type> FindTypes(Type baseType)
//        {
//            return Maps.GetItem(baseType, delegate(Type type)
//            {
//                List<Type> list = AssemblyX.FindAllPlugins(baseType, true).ToList();
//                if (list == null) list = new List<Type>();
//                return list;
//            });
//        }

//        /// <summary>
//        /// 注册类型，同一基类，解析时优先使用最后注册者
//        /// </summary>
//        /// <param name="baseType">基类</param>
//        /// <param name="type">要注册的类型</param>
//        /// <returns>返回自身，以便于连续注册</returns>
//        public virtual ITypeResolverProvider Register(Type baseType, Type type)
//        {
//            // 先找是否已注册，同时也是为了触发AssemblyX.FindAllPlugins扫描所有类
//            List<Type> list = FindTypes(baseType);
//            if (list.Contains(type))
//            {
//                // 如果已注册，将其调整到最后
//                lock (Maps)
//                {
//                    list.Remove(type);
//                    list.Add(type);
//                }
//                return this;
//            }

//            lock (Maps)
//            {
//                if (!Maps.TryGetValue(baseType, out list))
//                {
//                    list = new List<Type>();
//                    Maps.Add(baseType, list);
//                }
//                // 如果已注册，将其调整到最后
//                if (list.Contains(type)) list.Remove(type);
//                list.Add(type);
//            }
//            return this;
//        }

//        /// <summary>
//        /// 解析指定基类所注册的类型，排除指定类型，优先返回最后注册的类型
//        /// </summary>
//        /// <param name="baseType">基类</param>
//        /// <param name="excludeTypes">排除类型，当没有排除类型以外的类型时，忽略排除类型返回最后注册的类型</param>
//        /// <returns></returns>
//        public virtual Type Resolve(Type baseType, Type[] excludeTypes)
//        {
//            List<Type> list = FindTypes(baseType);
//            if (list == null || list.Count < 1) return null;

//            // 一个，直接返回
//            if (list.Count == 1) return list[0];

//            Type last = list[list.Count - 1];

//            // 多个，且设置了默认值，排除掉默认值
//            //if (excludeTypes != null && excludeTypes.Length > 0) list.RemoveAll(item => Array.IndexOf(excludeTypes, item) >= 0);
//            if (excludeTypes != null && excludeTypes.Length > 0)
//                list = list.FindAll(item => Array.IndexOf(excludeTypes, item) < 0);

//            // 没有排除类型以外的类型，返回最后注册的
//            if (list == null || list.Count < 1) return last;

//            // 一个，直接返回
//            if (list.Count == 1) return list[0];

//            // 还有多个，尝试排除同名者
//            foreach (Type type in list)
//            {
//                // 实体类和基类名字相同
//                String name = type.BaseType.Name;
//                Int32 p = name.IndexOf('`');
//                if (p > 0 && type.Name == name.Substring(0, p)) continue;

//                return type;
//            }

//            return list[list.Count - 1];
//        }

//        /// <summary>
//        /// 解析指定基类所注册的所有类型
//        /// </summary>
//        /// <param name="baseType"></param>
//        /// <returns></returns>
//        public virtual Type[] ResolveAll(Type baseType)
//        {
//            if (baseType == null) throw new ArgumentNullException("baseType");

//            List<Type> list = FindTypes(baseType);
//            if (list == null || list.Count < 1) return null;

//            return list.ToArray();
//        }
//        #endregion
//    }
//}