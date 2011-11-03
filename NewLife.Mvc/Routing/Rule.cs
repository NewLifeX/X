using System;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    internal class Rule
    {
        #region 公共

        private string _Path;

        /// <summary>
        /// 路径,赋值时如果以$符号结尾,表示是完整匹配(只会匹配Path部分,不包括Url中Query部分),而不是
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
        /// 路由的目标类型
        /// </summary>
        public Type Type
        {
            get;
            set;
        }

        /// <summary>
        /// 重写
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "{Rule} " + Path + " " + Type.ToString();
        }

        #endregion 公共

        private bool IsCompleteMatch;

        internal static Rule Create(string path, bool checkType, bool isFactory, Type type)
        {
            if (checkType)
            {
                isFactory = typeof(IControllerFactory).IsAssignableFrom(type);
            }
            if (!isFactory && !typeof(IController).IsAssignableFrom(type))
            {
                throw new ArgumentException(string.Format("NewLife.Mvc初始化异常 路径:{0} 所路由的目标:{1} 并未实现IControllerFactory或IController接口", path, type.FullName), "type");
            }

            // TODO Module类型
            Rule r = isFactory ? new FactoryRule() : new Rule();
            r.Path = path;
            r.Type = type;
            return r;
        }

        internal virtual IController GetRouteHandler(string path)
        {
            if (IsMatchPath(path))
            {
                IController c = GetRouteHandler();
                if (c != null)
                {
                    RouteContext.Current.EnterController(path, Path, c);
                }
                return c;
            }
            return null;
        }

        internal virtual bool IsMatchPath(string path)
        {
            bool isMatch = false;
            if (IsCompleteMatch)
            {
                isMatch = path == Path;
            }
            else // TODO 考虑忽略大小写
            {
                isMatch = path.StartsWith(Path);
            }
            return isMatch;
        }

        internal virtual IController GetRouteHandler()
        {
            return TypeX.CreateInstance(Type) as IController;
        }
    }

    internal class FactoryRule : Rule
    {
        private IControllerFactory factory;

        internal override bool IsMatchPath(string path)
        {
            bool isMatch = base.IsMatchPath(path);
            if (isMatch)
            {
                if (factory == null)
                {
                    lock (this)
                    {
                        if (factory == null)
                        {
                            factory = TypeX.CreateInstance(Type) as IControllerFactory;
                        }
                    }
                }
                RouteContext.Current.EnterFactory(path, Path, factory);
                isMatch = false;
                try
                {
                    isMatch = factory.Support(path);
                }
                finally
                {
                    if (!isMatch)
                    {
                        RouteContext.Current.ExitFactory();
                    }
                }
            }
            return isMatch;
        }

        internal override IController GetRouteHandler()
        {
            return factory.Create();
        }
    }

    // TODO IRouteConfigModule类型的规则
}