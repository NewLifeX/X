using System;
using System.Web.Mvc;
using XCode;

namespace NewLife.Cube
{
    public class EntityModelBinder : DefaultModelBinder
    {
        protected override object CreateModel(ControllerContext controllerContext, ModelBindingContext bindingContext, Type modelType)
        {
            if (typeof(IEntity).IsAssignableFrom(modelType))
            {
                var fact = EntityFactory.CreateOperate(modelType);
                if (fact != null)
                {
                    var rvs = controllerContext.RouteData.Values;
                    var uk = fact.Unique;
                    if (uk != null && rvs[uk.Name] != null) return fact.FindByKeyForEdit(rvs[uk.Name]);

                    return fact.Create();
                }
            }

            return base.CreateModel(controllerContext, bindingContext, modelType);
        }
    }

    public class EntityModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(Type modelType)
        {
            if (typeof(IEntity).IsAssignableFrom(modelType)) return new EntityModelBinder();

            return null;
        }

        static EntityModelBinderProvider()
        {
            ModelBinderProviders.BinderProviders.Add(new EntityModelBinderProvider());
        }

        public static void Register()
        {
            // 引发静态构造，只执行一次
        }
    }
}