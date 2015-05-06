using System;
using System.Web.Mvc;

namespace NewLife.Cube.Precompiled
{
    /// <summary>视图页注册器</summary>
    internal class DefaultViewPageActivator : IViewPageActivator
    {
        private static class Nested
        {
            internal static readonly DefaultViewPageActivator Instance;
            static Nested()
            {
                Instance = new DefaultViewPageActivator();
            }
        }

        private readonly Func<IDependencyResolver> _resolverThunk;

        /// <summary>当前注册器</summary>
        public static DefaultViewPageActivator Current { get { return Nested.Instance; } }

        public DefaultViewPageActivator() : this(null) { }

        public DefaultViewPageActivator(IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                _resolverThunk = (() => DependencyResolver.Current);
            }
            else
            {
                _resolverThunk = (() => resolver);
            }
        }

        /// <summary>创建视图实例</summary>
        /// <param name="controllerContext"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Create(ControllerContext controllerContext, Type type)
        {
            return _resolverThunk().GetService(type) ?? Activator.CreateInstance(type);
        }
    }
}