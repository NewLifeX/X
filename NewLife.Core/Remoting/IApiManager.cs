using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        ApiAction Find(string action);
    }

    class ApiManager : IApiManager
    {
        /// <summary>可提供服务的方法</summary>
        public IDictionary<string, ApiAction> Services { get; } = new Dictionary<string, ApiAction>();

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        public void Register<TService>() where TService : class, new()
        {
            var type = typeof(TService);
            //var name = type.Name.TrimEnd("Controller");

            foreach (var mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.IsSpecialName) continue;
                if (mi.DeclaringType == typeof(object)) continue;

                var act = new ApiAction(mi);

                Services[act.Name] = act;
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