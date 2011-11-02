using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    class Rule
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
        #endregion

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

            Rule r = isFactory ? new FactoryRule() : new Rule();
            r.Path = path;
            r.Type = type;
            return r;
        }

        internal virtual IHttpHandler GetRouteHandler(string path)
        {
            if (IsMatchPath(path))
            {
                return GetRouteHandler();
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
            else
            {
                isMatch = path.StartsWith(Path);
            }
            return isMatch;
        }

        internal virtual IHttpHandler GetRouteHandler()
        {
            return new HttpHandlerWrap(TypeX.CreateInstance(Type) as IController);
        }
    }

    class FactoryRule : Rule
    {
        private IControllerFactory factory;

        internal override bool IsMatchPath(string path)
        {
            if (base.IsMatchPath(path))
            {
                if (factory == null)
                {
                    factory = TypeX.CreateInstance(Type) as IControllerFactory;
                }
                return factory.Support(path);
            }
            return false;
        }

        internal override IHttpHandler GetRouteHandler()
        {
            return new HttpHandlerWrap(factory.Create());
        }
    }

    class HttpHandlerWrap : IHttpHandler
    {
        private IController Controller;
        public HttpHandlerWrap(IController controller)
        {
            Controller = controller;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            Controller.Execute();
        }
    }

}
