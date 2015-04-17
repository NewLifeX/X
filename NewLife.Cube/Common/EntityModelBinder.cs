using System;
using System.Web.Mvc;
using NewLife.Log;
using XCode;

namespace NewLife.Cube
{
    /// <summary>实体模型绑定器。特殊处理XCode实体类</summary>
    public class EntityModelBinder : DefaultModelBinder
    {
        /// <summary>创建模型。对于有Key的请求，使用FindByKeyForEdit方法先查出来数据，而不是直接反射实例化实体对象</summary>
        /// <param name="controllerContext"></param>
        /// <param name="bindingContext"></param>
        /// <param name="modelType"></param>
        /// <returns></returns>
        protected override object CreateModel(ControllerContext controllerContext, ModelBindingContext bindingContext, Type modelType)
        {
            if (typeof(IEntity).IsAssignableFrom(modelType))
            {
                var fact = EntityFactory.CreateOperate(modelType);
                if (fact != null)
                {
                    var rvs = controllerContext.RouteData.Values;
                    var uk = fact.Unique;
                    if (uk != null && rvs[uk.Name] != null) return fact.FindByKeyForEdit(rvs[uk.Name]) ?? fact.Create();

                    return fact.Create();
                }
            }

            return base.CreateModel(controllerContext, bindingContext, modelType);
        }
    }

    /// <summary>实体模型绑定器提供者，为所有XCode实体类提供实体模型绑定器</summary>
    public class EntityModelBinderProvider : IModelBinderProvider
    {
        /// <summary>获取绑定器</summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        public IModelBinder GetBinder(Type modelType)
        {
            if (typeof(IEntity).IsAssignableFrom(modelType)) return new EntityModelBinder();

            return null;
        }

        static EntityModelBinderProvider()
        {
            XTrace.WriteLine("注册实体模型绑定器：{0}", typeof(EntityModelBinderProvider).FullName);
            ModelBinderProviders.BinderProviders.Add(new EntityModelBinderProvider());
        }

        /// <summary>注册到全局模型绑定器提供者集合</summary>
        public static void Register()
        {
            // 引发静态构造，只执行一次
        }
    }
}