using NewLife.Model;

namespace NewLife.Mvc
{
    internal class Service : ServiceContainer<Service>
    {
        static Service()
        {
            Container
                .Register<IControllerFactory, GenericControllerFactory>()
                .Register<ITemplateEngine>(new GenericTemplateEngine());
        }
    }
}