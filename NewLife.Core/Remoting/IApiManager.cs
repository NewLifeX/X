using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>接口管理器</summary>
    public interface IApiManager
    {
        /// <summary>可提供服务的方法</summary>
        IDictionary<string, ApiAction> Services { get; }

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        void Register<TService>() where TService : class, new();

        /// <summary>注册服务</summary>
        /// <param name="controller">控制器对象或类型</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        void Register(Object controller, String method);

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        ApiAction Find(string action);
    }

    class ApiManager : IApiManager
    {
        /// <summary>可提供服务的方法</summary>
        public IDictionary<string, ApiAction> Services { get; } = new Dictionary<string, ApiAction>();

        private void Register(Object controller, Type type)
        {
            foreach (var mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.IsSpecialName) continue;
                if (mi.DeclaringType == typeof(object)) continue;

                var act = new ApiAction(mi);
                act.Controller = controller;

                Services[act.Name] = act;
            }
        }

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        public void Register<TService>() where TService : class, new()
        {
            Register(null, typeof(TService));
        }

        /// <summary>注册服务</summary>
        /// <param name="controller">控制器对象或类型</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        public void Register(Object controller, String method)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            var type = controller is Type ? controller as Type : controller.GetType();

            if (!method.IsNullOrEmpty())
            {
                var mi = type.GetMethodEx(method);
                var act = new ApiAction(mi);

                Services[act.Name] = act;
            }
            else
            {
                Register(controller, type);
            }
        }

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ApiAction Find(string action)
        {
            ApiAction mi;
            return Services.TryGetValue(action, out mi) ? mi : null;
        }
    }
}