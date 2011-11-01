using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;

namespace NewLife.Mvc
{
    class Service : ServiceContainer<Service>
    {
        static Service()
        {
            Container
                .Register<IControllerFactory, GenericControllerFactory>(null, false)
                .Register<ITemplateEngine>(new GenericTemplateEngine(), null, false);
        }
    }
}