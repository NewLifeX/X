using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;
using NewLife.Net.Proxy;
using NewLife.Reflection;

namespace NewLife.Net
{
    /// <summary>网络服务对象提供者</summary>
    class NetService : ServiceContainer<NetService>
    {
        static NetService()
        {
            IObjectContainer container = Container;
            container.Register<IProxySession, ProxySession>();
        }

        #region 方法
        public static Type ResolveType<TInterface>(Func<IObjectMap, Boolean> func)
        {
            foreach (IObjectMap item in Container.ResolveAllMaps(typeof(TInterface)))
            {
                if (func(item)) return item.ImplementType;
            }

            return null;
        }
        #endregion
    }
}