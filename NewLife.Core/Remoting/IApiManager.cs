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
        /// <param name="requireApi">是否要求Api特性</param>
        /// <typeparam name="TService"></typeparam>
        void Register<TService>(Boolean requireApi = false) where TService : class, new();

        /// <summary>注册服务</summary>
        /// <param name="controller">控制器对象</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        /// <param name="requireApi">是否要求Api特性</param>
        void Register(Object controller, String method, Boolean requireApi);

        /// <summary>注册服务</summary>
        /// <param name="type">控制器类型</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        /// <param name="requireApi">是否要求Api特性</param>
        void Register(Type type, String method, Boolean requireApi);

        /// <summary>注册服务</summary>
        /// <param name="method">动作</param>
        void Register(MethodInfo method);

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        ApiAction Find(String action);
    }

    class ApiManager : IApiManager
    {
        /// <summary>可提供服务的方法</summary>
        public IDictionary<String, ApiAction> Services { get; } = new Dictionary<String, ApiAction>();

        private void RegisterAll(Object controller, Type type, Boolean requireApi)
        {
            var flag = BindingFlags.Public | BindingFlags.Instance;
            // 如果要求Api特性，则还需要遍历私有方法和静态方法
            if (requireApi) flag |= BindingFlags.NonPublic | BindingFlags.Static;
            foreach (var mi in type.GetMethods(flag))
            {
                if (mi.IsSpecialName) continue;
                if (mi.DeclaringType == typeof(Object)) continue;
                if (requireApi && mi.GetCustomAttribute<ApiAttribute>() == null) continue;

                var act = new ApiAction(mi, type);
                act.Controller = controller;

                Services[act.Name] = act;
            }
        }

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="requireApi">是否要求Api特性</param>
        public void Register<TService>(Boolean requireApi = false) where TService : class, new()
        {
            RegisterAll(null, typeof(TService), requireApi);
        }

        /// <summary>注册服务</summary>
        /// <param name="controller">控制器对象</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        /// <param name="requireApi">是否要求Api特性</param>
        public void Register(Object controller, String method, Boolean requireApi)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            var type = controller is Type ? controller as Type : controller.GetType();

            if (!method.IsNullOrEmpty())
            {
                var mi = type.GetMethodEx(method);
                var act = new ApiAction(mi, type);
                act.Controller = controller;

                Services[act.Name] = act;
            }
            else
            {
                RegisterAll(controller, type, requireApi);
            }
        }

        /// <summary>注册服务</summary>
        /// <param name="type">控制器类型</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        /// <param name="requireApi">是否要求Api特性</param>
        public void Register(Type type, String method, Boolean requireApi)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (!method.IsNullOrEmpty())
            {
                var mi = type.GetMethodEx(method);
                var act = new ApiAction(mi, type);

                Services[act.Name] = act;
            }
            else
            {
                RegisterAll(null, type, requireApi);
            }
        }

        /// <summary>注册服务</summary>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        public void Register(MethodInfo method)
        {
            var act = new ApiAction(method, null);

            Services[act.Name] = act;
        }

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ApiAction Find(String action)
        {
            ApiAction mi;
            return Services.TryGetValue(action, out mi) ? mi : null;
        }
    }
}