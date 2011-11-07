using System;
using System.Collections.Generic;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>
    /// 路由规则
    /// </summary>
    internal class Rule
    {
        private bool IsCompleteMatch;

        private string _Path;

        /// <summary>
        /// 路由路径,赋值时如果以$符号结尾,表示是完整匹配(只会匹配Path部分,不包括Url中Query部分),而不是StartsWith匹配
        /// </summary>
        public string Path
        {
            get
            {
                return _Path;
            }
            set
            {
                int n = value.Length;
                bool f1 = false, f2 = false;
                if (n >= 1)
                {
                    f1 = value[n - 1] == '$';
                    if (n >= 2)
                    {
                        f2 = value[n - 2] == '$';
                    }
                }
                if (f1 && f2 || f1)
                {
                    value = value.Substring(0, n - 1);
                    IsCompleteMatch = f1 != f2;
                }
                _Path = value != "" ? value : "/";
            }
        }

        /// <summary>
        /// 路由的目标类型,需要实现了IController,IControllerFactory,IRouteConfigMoudule任意一个
        /// </summary>
        public Type Type { get; set; }

        #region 创建路由规则实例

        static List<RuleType>[] _RuleTypeList = new List<RuleType>[] { null };

        /// <summary>
        /// 路由规则类型列表,提供 具体的路由类型 创建Rule类或子类实例的方法
        /// </summary>
        private static List<RuleType> RuleTypeList
        {
            get
            {
                if (_RuleTypeList[0] == null)
                {
                    lock (_RuleTypeList)
                    {
                        if (_RuleTypeList[0] == null)
                        {
                            _RuleTypeList[0] = new List<RuleType>();
                            _RuleTypeList[0].Add(RuleType.Create<IController>(() => new Rule()));
                            _RuleTypeList[0].Add(RuleType.Create<IControllerFactory>(() => new FactoryRule()));
                            _RuleTypeList[0].Add(RuleType.Create<IRouteConfigMoudule>(() => new ModuleRule()));
                        }
                    }
                }
                return _RuleTypeList[0];
            }
        }

        /// <summary>
        /// 路由规则类型,及其对应的创建方法
        /// </summary>
        struct RuleType
        {
            public static RuleType Create<T>(Func<Rule> func)
            {
                return new RuleType()
                {
                    Type = typeof(T),
                    New = func
                };
            }

            /// <summary>
            /// 路由规则类型
            /// </summary>
            public Type Type;
            /// <summary>
            /// 对应规则的Rule实例创建方法
            /// </summary>
            public Func<Rule> New;
        }

        /// <summary>
        /// 创建指定路径到指定类型的路由,路由类型由ruleType指定,如果未指定则会自动检测
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="ruleType"></param>
        /// <returns></returns>
        internal static Rule Create(string path, Type type, Type ruleType)
        {
            if (path == null) throw new RouteConfigException("路由路径为Null");
            if (type == null) throw new RouteConfigException("路由目标未找到");
            if (ruleType == typeof(object)) ruleType = null;

            Rule r = null;
            foreach (var item in RuleTypeList)
            {
                if (ruleType == item.Type || ruleType == null && item.Type.IsAssignableFrom(type))
                {
                    r = item.New();
                    break;
                }
            }
            if (r == null) throw new RouteConfigException("无效的路由目标类型");
            r.Path = path;
            r.Type = type;
            return r;
        }

        #endregion 创建路由规则实例

        #region 获取路由的目标,普通类型的将直接返回控制器

        /// <summary>
        /// 使用当前的路由配置获取指定路径的控制器
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal virtual IController GetRouteHandler(string path)
        {
            if (IsMatch(path))
            {
                IController c = TypeX.CreateInstance(Type) as IController;
                if (c != null)
                {
                    RouteContext.Current.EnterController(Path, path, c);
                    return c;
                }
            }
            return null;
        }

        /// <summary>
        /// 使用当前路由规则的路径匹配指定的路径,返回是否匹配
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal bool IsMatch(string path)
        {
            return IsCompleteMatch ?
                String.Equals(path, Path, StringComparison.OrdinalIgnoreCase) :
                path.StartsWith(Path, StringComparison.OrdinalIgnoreCase);
        }

        #endregion 获取路由的目标,普通类型的将直接返回控制器

        /// <summary>
        /// 重写
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "{" + GetType().Name + "} " + Path + " " + Type.ToString();
        }
    }

    /// <summary>
    /// 工厂路由规则,会使用工厂的创建方法获取控制器,以及使用工厂的Support检查是否支持
    /// </summary>
    internal class FactoryRule : Rule
    {
        /// <summary>
        /// 工厂的创建方式,默认为直接创建Type指定的类型
        /// </summary>
        public Func<IControllerFactory> NewFactoryFunc { get; set; }

        IControllerFactory[] _Factory = new IControllerFactory[] { null };

        /// <summary>
        /// 当前路由规则对应的控制器工厂实例
        /// </summary>
        private IControllerFactory Factory
        {
            get
            {
                if (_Factory[0] == null)
                {
                    lock (_Factory)
                    {
                        if (_Factory[0] == null)
                        {
                            if (NewFactoryFunc != null)
                            {
                                _Factory[0] = NewFactoryFunc();
                            }
                            else
                            {
                                _Factory[0] = TypeX.CreateInstance(Type) as IControllerFactory;
                            }
                        }
                    }
                }
                return _Factory[0];
            }
        }

        internal override IController GetRouteHandler(string path)
        {
            if (base.IsMatch(path))
            {
                RouteContext rctx = RouteContext.Current;
                rctx.EnterFactory(Path, path, Factory);
                IController c = null;
                try
                {
                    if (Factory.Support(rctx.Path))
                    {
                        c = Factory.Create();
                        if (c != null)
                        {
                            rctx.EnterController(Path, path, c);
                            return c;
                        }
                    }
                }
                finally
                {
                    if (c == null)
                    {
                        rctx.ExitFactory(Path, path, Factory);
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 模块路由规则,按需要初始化模块的路由配置,使用对应的RouteConfigManager路由请求
    /// </summary>
    internal class ModuleRule : Rule
    {
        IRouteConfigMoudule _Module;

        /// <summary>
        /// 当前模块路由规则对应的模块
        /// </summary>
        private IRouteConfigMoudule Module
        {
            get
            {
                if (_Module == null)
                {
                    Config.ToString();
                }
                return _Module;
            }
            set
            {
                _Module = value;
            }
        }

        RouteConfigManager[] _Config = new RouteConfigManager[] { null };

        /// <summary>
        /// 当前模块路由规则对应的路由配置
        /// </summary>
        private RouteConfigManager Config
        {
            get
            {
                if (_Config[0] == null)
                {
                    lock (_Config)
                    {
                        if (_Config[0] == null)
                        {
                            _Config[0] = new RouteConfigManager();
                            Module = _Config[0].Load(Type);
                            Config.SortConfigRule();
                        }
                    }
                }
                return _Config[0];
            }
        }

        internal override IController GetRouteHandler(string path)
        {
            if (base.IsMatch(path))
            {
                RouteContext rctx = RouteContext.Current;
                rctx.EnterModule(Path, path, Module);
                IController r = null;
                try
                {
                    r = Config.GetRouteHandler(rctx.Path);
                    // 因为最终会路由到控制器或者控制器工厂,所以模块不需要调用EnterController
                }
                finally
                {
                    if (r == null)
                    {
                        rctx.ExitModule(Path, path, Module);
                    }
                }
                return r;
            }
            return null;
        }
    }
}