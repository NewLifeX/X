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
        IDictionary<String, ApiAction> Services { get; }

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        void Register<TService>() where TService : class, new();

        /// <summary>注册服务</summary>
        /// <param name="controller">控制器对象</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        void Register(Object controller, String method);

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        ApiAction Find(String action);
    }

    class ApiManager : IApiManager
    {
        /// <summary>可提供服务的方法</summary>
        public IDictionary<String, ApiAction> Services { get; } = new Dictionary<String, ApiAction>(StringComparer.OrdinalIgnoreCase);

        private void RegisterAll(Object controller, Type type)
        {
            // 是否要求Api特性
            var requireApi = type.GetCustomAttribute<ApiAttribute>() != null;

            var flag = BindingFlags.Public | BindingFlags.Instance;
            // 如果要求Api特性，则还需要遍历私有方法和静态方法
            if (requireApi) flag |= BindingFlags.NonPublic | BindingFlags.Static;
            foreach (var mi in type.GetMethods(flag))
            {
                if (mi.IsSpecialName) continue;
                if (mi.DeclaringType == typeof(Object)) continue;
                if (requireApi && mi.GetCustomAttribute<ApiAttribute>() == null) continue;

                var act = new ApiAction(mi, type)
                {
                    Controller = controller
                };

                Services[act.Name] = act;
            }
        }

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        public void Register<TService>() where TService : class, new() => RegisterAll(null, typeof(TService));

        /// <summary>注册服务</summary>
        /// <param name="controller">控制器对象</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        public void Register(Object controller, String method)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            var type = controller is Type ? controller as Type : controller.GetType();

            if (!method.IsNullOrEmpty())
            {
                var mi = type.GetMethodEx(method);
                var act = new ApiAction(mi, type)
                {
                    Controller = controller
                };

                Services[act.Name] = act;
            }
            else
            {
                RegisterAll(controller, type);
            }
        }

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ApiAction Find(String action)
        {
            if (Services.TryGetValue(action, out var mi)) return mi;

            // 局部模糊匹配
            if (action.Contains("/"))
            {
                var ctrl = action.Substring(null, "/");
                if (Services.TryGetValue(ctrl + "/*", out mi)) return mi;
            }

            // 全局模糊匹配
            if (Services.TryGetValue("*", out mi)) return mi;

            return null;
        }
    }
}