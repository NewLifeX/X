//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.ComponentModel.Design;
//using NewLife.Linq;
//using NewLife.Reflection;

//namespace NewLife.Model
//{
//    /// <summary>
//    /// 发现可用类型
//    /// </summary>
//    public class TypeDiscoveryService : ITypeDiscoveryService
//    {
//        IServiceProvider _provider;
//        ITypeDiscoveryService _baseservice;

//        /// <summary>
//        /// 实例化
//        /// </summary>
//        public TypeDiscoveryService() { }

//        /// <summary>
//        /// 实例化
//        /// </summary>
//        /// <param name="provider"></param>
//        /// <param name="baseservice"></param>
//        public TypeDiscoveryService(IServiceProvider provider, ITypeDiscoveryService baseservice)
//        {
//            _provider = provider;
//            _baseservice = baseservice;
//        }

//        /// <summary>
//        /// 检索可用类型的列表
//        /// </summary>
//        /// <param name="baseType">要匹配的基类型</param>
//        /// <param name="excludeGlobalTypes">指示是否应检查来自所有引用程序集的类型</param>
//        /// <returns>与 baseType 和 excludeGlobalTypes 指定的条件相匹配的类型的集合</returns>
//        public virtual List<Type> GetReferencedTypes(Type baseType, bool excludeGlobalTypes)
//        {
//            List<Type> list = new List<Type>();

//            // 从上一个发现服务中获取
//            if (_baseservice != null && _baseservice != this)
//            {
//                ICollection ts = _baseservice.GetTypes(baseType, excludeGlobalTypes);
//                if (ts != null)
//                {
//                    foreach (Type item in ts)
//                    {
//                        if (!list.Contains(item)) list.Add(item);
//                    }
//                }
//            }

//            // 尝试从提供者里面获取
//            if (_provider != null)
//            {
//                ITypeDiscoveryService service = ServiceProvider.GetService<ITypeDiscoveryService>(_provider);
//                if (service != null & service != this)
//                {
//                    ICollection ts = service.GetTypes(baseType, excludeGlobalTypes);
//                    if (ts != null)
//                    {
//                        foreach (Type item in ts)
//                        {
//                            if (!list.Contains(item)) list.Add(item);
//                        }
//                    }
//                }
//            }

//            // 尝试遍历所有已加载和未加载的程序集
//            {
//                List<Type> ts = AssemblyX.FindAllPlugins(baseType, true, excludeGlobalTypes).ToList();
//                if (ts != null)
//                {
//                    foreach (Type item in ts)
//                    {
//                        if (!list.Contains(item)) list.Add(item);
//                    }
//                }
//            }

//            return list.Count < 1 ? null : list;
//        }

//        #region ITypeDiscoveryService 成员
//        /// <summary>
//        /// 检索可用类型的列表
//        /// </summary>
//        /// <param name="baseType">要匹配的基类型</param>
//        /// <param name="excludeGlobalTypes">指示是否应检查来自所有引用程序集的类型。如果为 false，则检查来自所有引用程序集的类型。 否则，只检查来自非全局程序集缓存 (GAC) 引用的程序集的类型。</param>
//        /// <returns>与 baseType 和 excludeGlobalTypes 指定的条件相匹配的类型的集合</returns>
//        ICollection ITypeDiscoveryService.GetTypes(Type baseType, bool excludeGlobalTypes)
//        {
//            return GetReferencedTypes(baseType, excludeGlobalTypes);
//        }
//        #endregion
//    }
//}