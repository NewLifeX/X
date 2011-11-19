using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;

namespace NewLife.Mvc
{
    /// <summary>
    /// 路由的上下文信息
    ///
    /// 上下文信息包括
    ///   模块匹配的路径,路由到模块时会增加一个模块路径
    ///   控制器工厂匹配的路径,路由到控制器工厂时会提供的值
    ///   控制器匹配的路径,路由到控制器时会提供的值,如果是通过工厂路由到控制器的,那么和上一个工厂匹配的路径相同
    ///   剩余的路径,其它情况下的路径
    /// </summary>
    public class RouteContext : IRouteContext
    {
        internal RouteContext(HttpApplication app)
        {
            HttpRequest r = app.Context.Request;
            RoutePath = r.Path.Substring(r.ApplicationPath.TrimEnd('/').Length);
        }

        #region 公共

        [ThreadStatic]
        private static RouteContext _Current;
        /// <summary>当前请求路由上下文信息</summary>
        public static RouteContext Current { get { return _Current; } set { _Current = value; } }

        /// <summary>
        /// 当前请求的路由路径,即url排除掉当前应用部署的路径后,以/开始的路径,不包括url中?及其后面的
        ///
        /// 路由操作主要是基于这个路径
        ///
        /// 在当前请求初始化后不会改变
        /// </summary>
        public string RoutePath { get; private set; }

        private List<RouteMatchInfo<IRouteConfigModule>> _Modules;

        /// <summary>
        /// 当前路由经过的模块,在模块路由配置中可以获取到这个信息
        /// </summary>
        public List<RouteMatchInfo<IRouteConfigModule>> Modules
        {
            get
            {
                if (_Modules == null)
                {
                    _Modules = new List<RouteMatchInfo<IRouteConfigModule>>();
                }
                return _Modules;
            }
        }

        /// <summary>
        /// 当前路由最近一次经过的模块,如果没有将返回null
        /// </summary>
        public RouteMatchInfo<IRouteConfigModule> Module
        {
            get
            {
                if (Modules.Count > 0)
                {
                    return Modules[Modules.Count - 1];
                }
                return null;
            }
        }

        /// <summary>
        /// 当前路由经过的控制器工厂,如果没有经过将返回null
        /// </summary>
        public RouteMatchInfo<IControllerFactory> Factory { get; private set; }

        /// <summary>
        /// 当前路由经过的控制器,如果还未路由到控制器将返回null
        /// </summary>
        public RouteMatchInfo<IController> Controller { get; private set; }

        private string _Path;

        /// <summary>
        /// 当前的路径,在不同的上下文环境中有不同的含义
        ///  在模块路由中:路由路径中,匹配当前模块后剩下的路径
        ///  在控制器工厂中,路由路径中,匹配当前控制器工厂后剩下的路径
        ///  在控制器中,路由路径中,匹配当前控制器后剩下的路径
        /// </summary>
        public string Path
        {
            get
            {
                return _Path;
            }
            private set
            {
                _Path = value;
                PathFragments = null;
            }
        }

        private string[] _PathFragments = null;

        /// <summary>
        /// 当前路径使用/分割后的片段,不包含空白的,对于/foo/bar.foo这样的路径,将会返回["foo","bar.foo"]
        /// </summary>
        public string[] PathFragments
        {
            get
            {
                if (_PathFragments == null)
                {
                    _PathFragments = Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                }
                return _PathFragments;
            }
            private set
            {
                _PathFragments = value;
            }
        }

        #endregion 公共

        #region 上下文信息切换

        /// <summary>
        /// 进入指定的模块
        /// </summary>
        /// <param name="pattern">路由规则的路径</param>
        /// <param name="match">匹配pattern的实际路径部分</param>
        /// <param name="path">当前请求的路径,当前模块路径尚未提取</param>
        /// <param name="module"></param>
        internal void EnterModule(string pattern, string match, string path, IRouteConfigModule module)
        {
            Path = path.Substring(match.Length);
            Modules.Add(new RouteMatchInfo<IRouteConfigModule>(pattern, match, module));
        }

        /// <summary>
        /// 退出指定的模块
        /// </summary>
        /// <param name="pattern">路由规则的路径</param>
        /// <param name="match">匹配pattern的实际路径部分</param>
        /// <param name="path">当前请求的路径,当前模块路径尚未提取</param>
        /// <param name="module"></param>
        internal void ExitModule(string pattern, string match, string path, IRouteConfigModule module)
        {
#if DEBUG
            Debug.Assert(Module.Pattern == pattern);
            Debug.Assert(Module.Path == match);
            Debug.Assert(path.StartsWith(match));
            Debug.Assert(Module.RelatedObject == module);
#endif
            Modules.RemoveAt(Modules.Count - 1);
        }

        /// <summary>
        /// 进入指定的工厂
        /// </summary>
        /// <param name="pattern">路由规则的路径</param>
        /// <param name="match">匹配pattern的实际路径部分</param>
        /// <param name="path">当前请求的路径,当前控制器工厂路径尚未提取</param>
        /// <param name="factory"></param>
        internal void EnterFactory(string pattern, string match, string path, IControllerFactory factory)
        {
            Path = path.Substring(match.Length);
            Factory = new RouteMatchInfo<IControllerFactory>(pattern, match, factory);
        }

        /// <summary>
        /// 退出指定的工厂
        /// </summary>
        /// <param name="pattern">路由规则的路径</param>
        /// <param name="match">匹配pattern的实际路径部分</param>
        /// <param name="path">当前请求的路径,当前控制器工厂路径尚未提取</param>
        /// <param name="factory"></param>
        internal void ExitFactory(string pattern, string match, string path, IControllerFactory factory)
        {
#if DEBUG
            Debug.Assert(Factory.Pattern == pattern);
            Debug.Assert(Factory.Path == match);
            Debug.Assert(path.StartsWith(match));
            Debug.Assert(Factory.RelatedObject == factory);
#endif
            Factory = null;
        }

        /// <summary>
        /// 进入指定的控制器
        /// </summary>
        /// <param name="pattern">路由规则的路径</param>
        /// <param name="match">匹配pattern的实际路径部分</param>
        /// <param name="path">当前请求的路径,当前控制器路径尚未提取</param>
        /// <param name="controller"></param>
        internal void EnterController(string pattern, string match, string path, IController controller)
        {
#if DEBUG
            RouteMatchInfo<IControllerFactory> f = Factory;
            if (f != null)
            {
                Debug.Assert(f.Pattern == pattern);
                Debug.Assert(f.Path == match);
                Debug.Assert(path.StartsWith(match));
            }
#endif
            Path = path.Substring(match.Length);
            Controller = new RouteMatchInfo<IController>(pattern, match, controller);
        }

        #endregion 上下文信息切换
    }

    /// <summary>
    /// 路由上下文中使用的,用于表示当前路径中匹配路由规则的信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RouteMatchInfo<T>
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="path"></param>
        /// <param name="related"></param>
        public RouteMatchInfo(string pattern, string path, T related)
        {
            Pattern = pattern;
            Path = path;
            RelatedObject = related;
        }

        /// <summary>
        /// 匹配路径时使用的模式
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// 实际匹配到的路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 相关的处理对象,一般是IController,IControllerFactory,IRouteConfigMoudule
        /// </summary>
        public T RelatedObject { get; set; }

        /// <summary>
        /// 重载
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("RouteMatchInfo<{0}> {2}({1}) -> {3}", typeof(T).Name, Path, Pattern, RelatedObject);
        }
    }
}