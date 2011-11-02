using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Mvc
{
    /// <summary>
    /// 路由的上下文信息
    /// </summary>
    public class RouteContext
    {
        internal RouteContext(string path)
        {
            ModulePath = "";
            AllModulePaths = new string[] { };
            Path = path;
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
        /// <summary>
        /// 当前的模块路径,最近一次的
        /// </summary>
        public string ModulePath { get; private set; }

        /// <summary>
        /// 当前所有模块的路径,按照模块层次顺序的
        /// </summary>
        public string[] AllModulePaths { get; private set; }

        /// <summary>
        /// 当前的路径,Url中模块路径之后的部分
        /// </summary>
        public string Path { get; private set; }
    }
}
