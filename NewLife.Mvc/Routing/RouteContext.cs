using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using NewLife.Reflection;

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
            Path = RoutePath = r.Path.Substring(r.ApplicationPath.TrimEnd('/').Length);
        }

        #region 公共属性

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

        Stack<RouteFrag> _Frags = new Stack<RouteFrag>();

        /// <summary>
        /// 当前路由的片段,先匹配的在数组的开始
        /// </summary>
        public RouteFrag[] Frags
        {
            get
            {
                RouteFrag[] ret = _Frags.ToArray();
                Array.Reverse(ret);
                return ret;
            }
        }

        /// <summary>
        /// 当前路由最近的一个路由配置
        /// </summary>
        public RouteFrag? Config
        {
            get
            {
                foreach (var f in _Frags)
                {
                    if (f.Type == RouteFragType.Config) return f;
                }
                return null;
            }
        }

        /// <summary>
        /// 当前路由最近的一个模块
        /// </summary>
        public RouteFrag? Module
        {
            get
            {
                foreach (var f in _Frags)
                {
                    if (f.Type == RouteFragType.Module) return f;
                }
                return null;
            }
        }

        /// <summary>
        /// 路由最近的一个控制器工厂,如果没有路由进工厂则返回null
        /// </summary>
        public RouteFrag? Factory
        {
            get
            {
                foreach (var f in _Frags)
                {
                    if (f.Type == RouteFragType.Controller) continue;
                    if (f.Type == RouteFragType.Factory) return f;
                    break;
                }
                return null;
            }
        }

        /// <summary>
        /// 路由最近的一个控制器,如果没有路由进控制器则返回null
        /// </summary>
        public RouteFrag? Controller
        {
            get
            {
                if (_Frags.Count > 0)
                {
                    RouteFrag f = _Frags.Peek();
                    if (f.Type == RouteFragType.Controller) return f;
                }
                return null;
            }
        }

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

        #endregion 公共属性

        #region 公共方法

        /// <summary>
        /// 在Frags中查找第一个符合指定条件的RouteFrag
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public RouteFrag? FindFrag(Func<RouteFrag, bool> filter)
        {
            foreach (var f in _Frags)
            {
                if (filter(f)) return f;
            }
            return null;
        }

        /// <summary>
        /// 在Frags中查找符合指定条件的RouteFrag
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public List<RouteFrag> FindAllFrags(Func<RouteFrag, bool> filter)
        {
            List<RouteFrag> ret = new List<RouteFrag>();
            foreach (var f in _Frags)
            {
                if (filter(f)) ret.Add(f);
            }
            return ret;
        }

        /// <summary>
        /// 路由当前路径到指定模块路由配置
        ///
        /// 适用于临时路由,使用 RouteTo(RouteConfigManager cfg) 能避免重复的对象实例化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IController RouteTo<T>() where T : IRouteConfigModule
        {
            return RouteTo((IRouteConfigModule)TypeX.CreateInstance<T>());
        }

        /// <summary>
        /// 路由当前路径到指定的模块路由配置
        ///
        /// 适用于临时路由,使用 RouteTo(RouteConfigManager cfg) 能避免重复的对象实例化.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public IController RouteTo(IRouteConfigModule module)
        {
            RouteConfigManager cfg = new RouteConfigManager();
            module.Config(cfg);
            // TODO 模块上下文进出
            return RouteTo(cfg);
        }

        /// <summary>
        /// 路由当前路径到指定的路由配置
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public IController RouteTo(RouteConfigManager cfg)
        {
            cfg.Sort();
            // TODO 是否有必要所有的路由配置都要进出
            EnterConfigManager("", Path, null, cfg);
            IController c = null;
            try
            {
                foreach (var r in cfg.Rules)
                {
                    c = r.RouteTo(this);
                    if (c != null) break;
                }
            }
            finally
            {
                if (c != null)
                {
                }
                else
                {
                    ExitConfigManager("", Path, null, cfg);
                }
            }
            return c;
        }

        #endregion 公共方法

        #region 上下文状态进出

        /// <summary>
        /// 上下文进入特定路由配置
        /// </summary>
        /// <param name="match"></param>
        /// <param name="path"></param>
        /// <param name="r"></param>
        /// <param name="related"></param>
        internal void EnterConfigManager(string match, string path, Rule r, RouteConfigManager related)
        {
            Path = path.Substring(match.Length);
            _Frags.Push(new RouteFrag()
            {
                Type = RouteFragType.Config,
                Path = match,
                Rule = r,
                Related = related
            });
        }
        /// <summary>
        /// 上下文退出特定路由配置
        /// </summary>
        /// <param name="match"></param>
        /// <param name="path"></param>
        /// <param name="r"></param>
        /// <param name="related"></param>
        internal void ExitConfigManager(string match, string path, Rule r, RouteConfigManager related)
        {
#if DEBUG
            Debug.Assert(path.StartsWith(match));
            RouteFrag m = _Frags.Peek();
            Debug.Assert(m.Path == match);
            Debug.Assert(m.Rule == r);
            Debug.Assert(m.Related == related);
#endif
            Path = path;
            _Frags.Pop();
        }

        /// <summary>
        /// 上下文进入模块
        /// </summary>
        /// <param name="match">匹配到的路径,需要是Path参数的开始部分</param>
        /// <param name="path">进入模块前的路径</param>
        /// <param name="r">当前匹配的路由规则</param>
        /// <param name="related">模块实例</param>
        internal void EnterModule(string match, string path, Rule r, IRouteConfigModule related)
        {
            Path = path.Substring(match.Length);
            _Frags.Push(new RouteFrag()
            {
                Type = RouteFragType.Module,
                Path = match,
                Rule = r,
                Related = related
            });
        }

        /// <summary>
        /// 上下文退出模块
        /// </summary>
        /// <param name="match"></param>
        /// <param name="path"></param>
        /// <param name="r"></param>
        /// <param name="related"></param>
        internal void ExitModule(string match, string path, Rule r, IRouteConfigModule related)
        {
#if DEBUG
            Debug.Assert(path.StartsWith(match));
            RouteFrag m = _Frags.Peek();
            Debug.Assert(m.Path == match);
            Debug.Assert(m.Rule == r);
            Debug.Assert(m.Related == related);
#endif
            Path = path;
            _Frags.Pop();
        }

        /// <summary>
        /// 上下文进入工厂
        /// </summary>
        /// <param name="match"></param>
        /// <param name="path"></param>
        /// <param name="r"></param>
        /// <param name="related"></param>
        internal void EnterFactory(string match, string path, Rule r, IControllerFactory related)
        {
            Path = path.Substring(match.Length);
            _Frags.Push(new RouteFrag()
            {
                Type = RouteFragType.Factory,
                Path = match,
                Rule = r,
                Related = related
            });
        }
        /// <summary>
        /// 上下文退出工厂
        /// </summary>
        /// <param name="match"></param>
        /// <param name="path"></param>
        /// <param name="r"></param>
        /// <param name="related"></param>
        internal void ExitFactory(string match, string path, Rule r, IControllerFactory related)
        {
#if DEBUG
            Debug.Assert(path.StartsWith(match));
            RouteFrag m = _Frags.Peek();
            Debug.Assert(m.Type == RouteFragType.Factory);
            Debug.Assert(m.Path == match);
            Debug.Assert(m.Rule == r);
            Debug.Assert(m.Related == related);
#endif
            Path = path;
            _Frags.Pop();
        }

        /// <summary>
        /// 上下文进入控制器
        /// </summary>
        /// <param name="match"></param>
        /// <param name="path"></param>
        /// <param name="r"></param>
        /// <param name="related"></param>
        internal void EnterController(string match, string path, Rule r, IController related)
        {
#if DEBUG
            Debug.Assert(path.StartsWith(match));
            RouteFrag? f = Factory;
            if (f != null)
            {
                Debug.Assert(f.Value.Path == match);
                Debug.Assert(f.Value.Rule == r);
            }
#endif
            Path = path.Substring(match.Length);
            _Frags.Push(new RouteFrag()
            {
                Type = RouteFragType.Controller,
                Path = match,
                Rule = r,
                Related = related
            });
        }
        #endregion

        public override string ToString()
        {
            return base.ToString();

            // TODO 输出路由上下文信息
        }
    }

    /// <summary>
    /// 路由片段结构体,表示当前请求路径每个匹配的路径信息
    /// </summary>
    public struct RouteFrag
    {
        /// <summary>
        /// 片段类型
        /// </summary>
        public RouteFragType Type { get; internal set; }

        /// <summary>
        /// 片段匹配的实际路径
        /// </summary>
        public string Path { get; internal set; }

        /// <summary>
        /// 片段匹配的路由规则实例
        /// </summary>
        public Rule Rule { get; internal set; }

        /// <summary>
        /// 相关的对象,和Type关联
        /// </summary>
        public object Related { get; internal set; }

        /// <summary>
        /// 重写
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{{RouteFrag {0} -> {1} [{2}] {3}}}", Path, Rule, Type, Related);
        }

        /// <summary>
        /// 返回当前片段关联对象的强类型实例,如果和指定类型不符则返回default(T)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetRelated<T>()
        {
            switch (Type)
            {
                case RouteFragType.Controller:
                    if (typeof(T) == typeof(IController) && Related is IController) return (T)Related;
                    break;
                case RouteFragType.Factory:
                    if (typeof(T) == typeof(IControllerFactory) && Related is IControllerFactory) return (T)Related;
                    break;
                case RouteFragType.Module:
                    if (typeof(T) == typeof(IRouteConfigModule) && Related is IRouteConfigModule) return (T)Related;
                    break;
                case RouteFragType.Config:
                    if (typeof(RouteConfigManager).IsAssignableFrom(typeof(T)) && Related is RouteConfigManager) return (T)Related;
                    break;
            }
            return default(T);
        }
    }

    /// <summary>
    /// 路由片段类型
    /// </summary>
    public enum RouteFragType
    {
        /// <summary>
        /// 控制器,Related是IController类型
        /// </summary>
        Controller,
        /// <summary>
        /// 控制器工厂,Related是IControllerFactory类型
        /// </summary>
        Factory,
        /// <summary>
        /// 模块,Related是IRouteConfigModule类型
        /// </summary>
        Module,
        /// <summary>
        /// 路由配置,Related是RouteConfigManager类型
        /// </summary>
        Config
    }
}