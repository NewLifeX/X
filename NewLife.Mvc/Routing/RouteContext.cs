using System;
using System.Web;

namespace NewLife.Mvc
{
    /// <summary>
    /// 路由的上下文信息
    /// </summary>
    public class RouteContext
    {
        internal RouteContext(HttpApplication app)
        {
            HttpRequest r = app.Context.Request;
            RoutePath = r.Path.Substring(r.ApplicationPath.TrimEnd('/').Length);

            //Modules = new string[] { };
            //Path =
        }

        [ThreadStatic]
        private static RouteContext _Current;
        /// <summary>
        /// 当前请求路由上下文信息
        /// </summary>
        public static RouteContext Current
        {
            get
            {
                return _Current;
            }
            set
            {
                _Current = value;
            }
        }

        ///// <summary>
        ///// 当前的模块路径,最近一次的
        ///// </summary>
        //public string Module { get; private set; }

        ///// <summary>
        ///// 当前所有模块的路径,按照模块层次顺序,第一个是顶级模块
        ///// </summary>
        //public string[] Modules { get; private set; }

        ///// <summary>
        ///// 当前的路径,Url中模块路径之后的部分
        ///// </summary>
        //public string Path { get; private set; }

        ///// <summary>
        ///// 获取应用程序根的虚拟路径,以~/开头的
        ///// 
        ///// 在当前请求初始化后不会改变
        ///// 
        ///// 用于替代Request.AppRelativeCurrentExecutionFilePath,因为这个方法在Url对应的文件不存在时会返回空白
        ///// </summary>
        //public string AppRelativePath { get; private set; }

        /// <summary>
        /// 当前请求的路由路径,即url排除掉当前应用部署的路径后,以/开始的路径,不包括url中?及其后面的
        /// 
        /// 路由操作主要是基于这个路径
        /// 
        /// 在当前请求初始化后不会改变
        /// </summary>
        public string RoutePath { get; private set; }
    }
}
