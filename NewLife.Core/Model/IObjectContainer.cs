using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Model
{
    /// <summary>对象容器</summary>
    interface IObjectContainer
    {
        IObjectContainer Parent { get; }

        IObjectContainer RegisterType(Type type);
        IObjectContainer RegisterType(Type type, String name);
        IObjectContainer RegisterType<T>();
        IObjectContainer RegisterType<T>(String name);
        IObjectContainer RegisterInstance(Type type, Object instance);
        IObjectContainer RegisterInstance(Type type, String name, Object instance);
        IObjectContainer RegisterInstance<TInterface>(Object instance);
        IObjectContainer RegisterInstance<TInterface>(String name, Object instance);

        Object Resolve(Type type);
        Object Resolve(Type type, String name);
        T Resolve<T>();
        T Resolve<T>(String name);

        IEnumerable<Object> ResolveAll(Type type);
        IEnumerable<T> ResolveAll<T>();

        IObjectContainer RemoveAllChildContainers();
        IObjectContainer CreateChildContainer();
    }
}