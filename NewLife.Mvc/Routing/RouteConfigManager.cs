using System;
using System.Collections.Generic;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>
    /// 路由配置管理器
    /// </summary>
    public class RouteConfigManager
    {
        #region 公共

        /// <summary>
        /// 指定路径路由到指定控制器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public RouteConfigManager Route<T>(string path) where T : IController, new()
        {
            return Route(path, typeof(T), typeof(IController));
        }

        /// <summary>
        /// 指定路径路由到控制器工厂,工厂是单例的
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public RouteConfigManager RouteToFactory<T>(string path) where T : IControllerFactory, new()
        {
            return Route(path, typeof(T), typeof(IControllerFactory));
        }

        /// <summary>
        /// 指定路径路由到控制器工厂,自定义工厂的初始化
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newFunc">工厂实例化方式</param>
        /// <returns></returns>
        public RouteConfigManager RouteToFactory(string path, Func<IControllerFactory> newFunc)
        {
            return Route(path, typeof(IControllerFactory), typeof(IControllerFactory), delegate(Rule r)
            {
                (r as FactoryRule).NewFactoryFunc = newFunc;
            });
        }

        /// <summary>
        /// 指定路径路由到模块,模块是一个独立的路由配置,可以相对于自身所路由的路径,进一步路由到具体的控制器或者工厂
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public RouteConfigManager RouteToModule<T>(string path) where T : IRouteConfigModule, new()
        {
            return Route(path, typeof(T), typeof(IRouteConfigModule));
        }

        /// <summary>
        /// 指定路径路由到指定名称的类型,目标类型需要是IController,IControllerFactory,IRouteConfigMoudule其中之一
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public RouteConfigManager Route(string path, string type)
        {
            return Route(path, TypeX.GetType(type));
        }

        /// <summary>
        /// 指定路径路由到指定类型,类型需要是IController,IControllerFactory,IRouteConfigMoudule其中之一
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public RouteConfigManager Route(string path, Type type)
        {
            return Route(path, type, typeof(object));
        }

        /// <summary>
        /// 指定多个路径路由到指定的目标,目标需要是IController,IControllerFactory,IRouteConfigMoudule其中之一
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="args">多个路由规则,其中type可以是具体的Type或者字符串指定的类型名称</param>
        /// <returns></returns>
        public RouteConfigManager Route(string path, Type type, params object[] args)
        {
            // TODO 
            Route(path, type, typeof(object));
            int n = args.Length & ~1;

            for (int i = 0; i < n; i += 2)
            {
                path = args[i].ToString();
                object t = args[i + 1];
                if (t != null)
                {
                    if (t is string)
                    {
                        t = TypeX.GetType(t as string);
                    }
                    if (t is Type)
                    {
                        Route(path, t as Type, typeof(object));
                    }
                }
            }
            return this;
        }

        #endregion 公共

        #region 内部

        List<Rule> rules;

        internal virtual RouteConfigManager Route(string path, Type type, Type ruleType, Action<Rule> onCreatedRule = null)
        {
            if (rules == null) rules = new List<Rule>();
            Rule r = null;
            try
            {
                r = Rule.Create(path, type, ruleType);
            }
            catch (RouteConfigException ex)
            {
                ex.RoutePath = path;
                throw;
            }
            if (onCreatedRule != null) onCreatedRule(r);
            rules.Add(r);
            return this;
        }

        /// <summary>
        /// 加载指定类型的路由配置模块,返回创建的模块实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal IRouteConfigModule Load(Type type)
        {
            IRouteConfigModule cfg = TypeX.CreateInstance(type) as IRouteConfigModule;
            try
            {
                cfg.Config(this);
            }
            catch (RouteConfigException ex)
            {
                ex.Module = cfg;
                throw;
            }
            return cfg;
        }

        /// <summary>
        /// 按照路由配置的路径可能的匹配程度倒序排列,目前的实现是路由路径的长度
        /// </summary>
        internal void SortConfigRule()
        {
            rules.Sort((a, b) => b.Path.Length - a.Path.Length);
        }

        /// <summary>
        /// 返回当前路由配置的路由目标HttpHandler,如果无法匹配任何路由目标则返回null
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal IController GetRouteHandler(string path)
        {
            IController c = null;
            foreach (var r in rules)
            {
                c = r.GetRouteHandler(path);
                if (c != null)
                {
                    break;
                }
            }
            return c;
        }

        #endregion 内部
    }

    /// <summary>
    /// 路由配置异常
    /// </summary>
    public class RouteConfigException : ArgumentException
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="message"></param>
        public RouteConfigException(string message)
            : base(message) { }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="message"></param>
        /// <param name="paramName"></param>
        public RouteConfigException(string message, string paramName)
            : base(message, paramName) { }

        /// <summary>
        /// 路由配置异常消息
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format("在路由配置 {0} 中的 {1} 路由项配置发生异常:{2}", Module != null ? Module.GetType().AssemblyQualifiedName : "", RoutePath, base.Message);
            }
        }
        internal IRouteConfigModule Module { get; set; }
        internal string RoutePath { get; set; }
    }
}