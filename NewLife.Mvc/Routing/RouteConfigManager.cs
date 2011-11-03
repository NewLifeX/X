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
        public RouteConfigManager Route<T>(string path) where T : IController
        {
            return Route(path, false, false, typeof(T));
        }

        /// <summary>
        /// 指定路径路由到指定名称的类型,可以是控制器IController,或者工厂IControllerFactory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public RouteConfigManager Route(string path, string type)
        {
            return Route(path, true, false, TypeX.GetType(type));
        }

        /// <summary>
        /// 指定路径路由到指定名称的控制器工厂根据需要产生控制器实例来处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public RouteConfigManager RouteToFactory<T>(string path) where T : IControllerFactory
        {
            return Route(path, false, true, typeof(T));
        }

        /// <summary>
        /// 指定多个路径路由到指定的目标,目标可以是IController,IControllerFactory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public RouteConfigManager Route(string path, Type type, params object[] args)
        {
            Route(path, true, false, type);
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
                        type = t as Type;
                        Route(path, true, false, type);
                    }
                }
            }
            return this;
        }

        #endregion 公共

        #region 内部

        List<Rule> rules;

        /// <summary>
        /// 路由指定路径到指定类型
        /// </summary>
        /// <param name="path"></param>
        /// <param name="checkType"></param>
        /// <param name="isFactory"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal virtual RouteConfigManager Route(string path, bool checkType, bool isFactory, Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (path == null) throw new ArgumentNullException("path");
            if (rules == null)
            {
                rules = new List<Rule>();
            }
            rules.Add(Rule.Create(path, checkType, isFactory, type));
            return this;
        }

        internal void Load(Type type)
        {
            IRouteConfigMoudule cfg = TypeX.CreateInstance(type) as IRouteConfigMoudule;
            cfg.Config(this);
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
}