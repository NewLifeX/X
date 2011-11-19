using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Mvc
{
    /// <summary>路由上下文</summary>
    public interface IRouteContext
    {
        /// <summary>
        /// 当前的路径,在不同的上下文环境中有不同的含义
        ///  在模块路由中:路由路径中,匹配当前模块后剩下的路径
        ///  在控制器工厂中,路由路径中,匹配当前控制器工厂后剩下的路径
        ///  在控制器中,路由路径中,匹配当前控制器后剩下的路径
        /// </summary>
        string Path { get; }
    }
}