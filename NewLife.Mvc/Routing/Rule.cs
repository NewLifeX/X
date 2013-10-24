using System;
using System.Collections.Generic;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>路由规则</summary>
    public class Rule
    {
        private bool IsCompleteMatch;

        #region 公共属性

        private string _Path;

        /// <summary>
        /// 路由路径,赋值时如果以$符号结尾,表示是完整匹配(只会匹配Path部分,不包括Url中Query部分),而不是StartsWith匹配
        ///
        /// $$表示原始的$符号
        /// </summary>
        public string Path
        {
            get
            {
                return _Path;
            }
            set
            {
                IsCompleteMatch = false;
                if (value != null)
                {
                    if (value.EndsWith("$") && !value.EndsWith("$$"))
                    {
                        IsCompleteMatch = true;
                    }
                }
                _Path = value;
            }
        }

        /// <summary>路由的目标类型,需要实现了IController,IControllerFactory,IRouteConfigMoudule任意一个</summary>
        public virtual Type Type { get; set; }

        #endregion 公共属性

        #region 路由规则的实例化

        static List<RuleType>[] _RuleTypeList = new List<RuleType>[] { null };

        /// <summary>规则类型到具体类型的Rule子类映射</summary>
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
                            _RuleTypeList[0].Add(RuleType.Create<IRouteConfigModule>(() => new ModuleRule()));
                        }
                    }
                }
                return _RuleTypeList[0];
            }
        }

        /// <summary>创建指定路径到指定类型的路由,路由类型由ruleType指定,如果未指定则会自动检测</summary>
        /// <param name="path"></param>
        /// <param name="type">类型</param>
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
            if (r == null)
            {
                throw new RouteConfigException(string.Format("无效的路由目标类型,目标需要是{0}其中一种类型", RuleTypeNames));
            }
            r.Path = path;
            r.Type = type;
            return r;
        }

        /// <summary>路由规则类型,及其对应的创建方法</summary>
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

            /// <summary>路由规则类型</summary>
            public Type Type;
            /// <summary>对应规则的Rule实例创建方法</summary>
            public Func<Rule> New;
        }

        static string _RuleTypeNames;

        /// <summary>RuleTypeList中所有规则类型名称,逗号分割的</summary>
        private static string RuleTypeNames
        {
            get
            {
                if (_RuleTypeNames == null)
                {
                    _RuleTypeNames = string.Join(",", RuleTypeList.ConvertAll<string>(a => a.Type.Name).ToArray());
                }
                return _RuleTypeNames;
            }
        }

        #endregion 路由规则的实例化

        #region 路由

        /// <summary>路由当前上下文,子类根据自己的匹配逻辑重写</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        internal virtual IController RouteTo(RouteContext ctx)
        {
            string match, path = ctx.Path;
            if (TryMatch(path, out match))
            {
                var c = Type.CreateInstance() as IController;
                if (c != null)
                {
                    ctx.EnterController(match, path, this, c);
                    return c;
                }
            }
            return null;
        }

        /// <summary>使用当前路由规则的路径匹配指定的路径,返回是否匹配</summary>
        /// <param name="path"></param>
        /// <param name="match">返回匹配到的路径片段</param>
        /// <returns></returns>
        internal virtual bool TryMatch(string path, out string match)
        {
            bool ret;
            match = null;
            if (IsCompleteMatch)
            {
                ret = Path.Length - 1 == path.Length && Path.StartsWith(path, StringComparison.OrdinalIgnoreCase); // 因为IsCompleteMatch时Path末尾包含一个$符号
                if (ret)
                {
                    match = path;
                }
            }
            else
            {
                ret = path.StartsWith(Path, StringComparison.OrdinalIgnoreCase);
                if (ret)
                {
                    match = path.Substring(0, Path.Length);
                }
            }
            return ret;
        }

        #endregion 路由

        /// <summary>
        /// 重写,将输出
        /// {Rule 规则配置的路径 -> 规则配置的目标类型}
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{{\"{0}\" -> {1}}}", Path, Type.FullName);
        }
    }

    /// <summary>工厂路由规则,会使用工厂的创建方法获取控制器,以及使用工厂的Support检查是否支持</summary>
    public class FactoryRule : Rule
    {
        /// <summary>工厂的创建方式,默认为直接创建Type指定的类型</summary>
        public Func<IControllerFactory> NewFactoryFunc { get; set; }

        IControllerFactory[] _Factory = new IControllerFactory[] { null };

        /// <summary>当前路由规则对应的控制器工厂实例</summary>
        public IControllerFactory Factory
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
                                _Factory[0] = Type.CreateInstance() as IControllerFactory;
                            }
                        }
                    }
                }
                return _Factory[0];
            }
            set
            {
                _Factory[0] = value;
            }
        }

        internal override IController RouteTo(RouteContext ctx)
        {
            string match, path = ctx.Path;
            if (base.TryMatch(path, out match))
            {
                ctx.EnterFactory(match, path, this, Factory);
                IController c = null;
                try
                {
                    c = Factory.GetController(ctx);
                }
                finally
                {
                    if (c != null)
                    {
                        RouteFrag f = ctx.Controller;
                        if (f == null)
                        {
                            // 控制器工厂内部有可能通过RouteContext.RouteTo已经调用了进入控制器 所以退出时需要检查是否有需要进入控制器工厂
                            // 由于控制器工厂已经匹配了路径,所以控制器工厂返回的控制器不应该匹配任何路径
                            ctx.EnterController("", path, this, c);
                        }
                    }
                    else
                    {
                        ctx.ExitFactory(match, path, this, Factory);
                    }
                }
                return c;
            }
            return null;
        }

        /// <summary>
        /// 重写,将输出
        /// {FactoryRule 规则配置的路径 -> 规则配置的目标类型 目标工厂类型实例}
        /// 其中"目标工厂类型实例"只有在不为null是输出
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{{\"{0}\" -> {1}}}", Path, Factory);
        }
    }

    /// <summary>模块路由规则,按需要初始化模块的路由配置,使用对应的RouteConfigManager路由请求</summary>
    public class ModuleRule : Rule
    {
        /// <summary>重写,路由目标类型</summary>
        public override Type Type
        {
            get
            {
                return base.Type;
            }
            set
            {
                if (base.Type != null && base.Type != value)
                {
                    Module = null;
                }
                base.Type = value;
            }
        }

        IRouteConfigModule[] _Module = { null };

        /// <summary>当前模块路由规则对应的模块</summary>
        public IRouteConfigModule Module
        {
            get
            {
                if (Type != null && _Module[0] == null)
                {
                    lock (_Module)
                    {
                        if (Type != null && _Module[0] == null)
                        {
                            _Module[0] = (IRouteConfigModule)Type.CreateInstance();
                        }
                    }
                }
                return _Module[0];
            }
            set
            {
                if (_Module[0] != null && _Module[0] != value)
                {
                    reloadConfig = true;
                }
                _Module[0] = value;
            }
        }

        private bool reloadConfig;

        RouteConfigManager[] _Config = new RouteConfigManager[] { null };

        /// <summary>当前模块路由规则对应的路由配置</summary>
        public RouteConfigManager Config
        {
            get
            {
                if ((_Config[0] == null || reloadConfig) && Module != null)
                {
                    lock (_Config)
                    {
                        if ((_Config[0] == null || reloadConfig) && Module != null)
                        {
                            var cfg = _Config[0];
                            if (cfg == null) cfg = new RouteConfigManager();
                            else cfg.Clear();
                            _Config[0] = cfg.AcquireWriterLock<RouteConfigManager>(() =>
                            {
                                reloadConfig = false;
                                return cfg.Load(Module).Sort();
                            });
                        }
                    }
                }
                return _Config[0];
            }
            set
            {
                _Config[0] = value;
            }
        }

        internal override IController RouteTo(RouteContext ctx)
        {
            string match, path = ctx.Path;
            if (base.TryMatch(path, out match))
            {
                ctx.EnterModule(match, path, this, Module);
                IController r = null;
                try
                {
                    r = ctx.RouteTo("", this, f => null);
                }
                finally
                {
                    if (r != null)
                    {
                        // 模块不需要负责调用进出上下文 内部会有Rule或者FactoryRule负责
                    }
                    else
                    {
                        ctx.ExitModule(match, path, this, Module);
                    }
                }
                return r;
            }
            return null;
        }

        /// <summary>
        /// 重写,将输出
        /// {ModuleRule 规则配置的路径 -> 规则配置的目标类型 目标模块类型实例 模块产生的路由配置数量}
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{{\"{0}\" -> {1} Contains {2} rules}}", Path, Module, Config.Count);
        }
    }
}