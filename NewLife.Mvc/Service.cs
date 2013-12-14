using System;
using NewLife.Model;

namespace NewLife.Mvc
{
    internal class Service //: ServiceContainer<Service>
    {
        #region 当前静态服务容器
        /// <summary>当前对象容器</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        static Service()
        {
            Container
                .Register<IControllerFactory, GenericControllerFactory>()
                .Register<ITemplateEngine>(new GenericTemplateEngine());
        }
    }
}