using System;
using System.Collections.Generic;
using System.Web;
using NewLife.Mvc;

/// <summary>
///Urls 的摘要说明
/// </summary>
public class Urls : IRouteConfig
{

    public void Config(RouteConfigManager cfg)
    {
        cfg
            .RouteToFactory<RouteFactory>("/Module")
            .Route<TestController>("/Test")
            .Route(
                "/foo.aspx$", typeof(GenericControllerFactory),
                "/Test1$", typeof(TestController1),
                "/Test2", typeof(TestController2),
                "/Factory1", typeof(TestFactory),
                "/Error", typeof(TestError),
                "/Module", typeof(TestModuleRoute),
                ""
            );

        /*
         * TODO
         * ##RouteConfigManager使用稳定的排序算法
         * 开放Mvc路由控制相关的类,可以使用自定义的针对域名的路由控制
         *   RouteContext需要支持多级上下文
         *      传统的结构是 / -> Module -> Module/Factory/Controller
         *      需要支持的新结构是 DomainName -> Module/Factory/Controller -> / -> Module/Factory/Controller
         *      实际需要实现的是允许 Module/Factory 中进一步路由到Module/Factory/Controller 并保持RouteContext相关信息
         *   在运行时 修改某路径下的模块路由配置 以及运行时重新加载
         *   在运行时 限制特定路径只有在特定条件下由指定模块 工厂 控制器处理 比如以特定域名访问的请求
         *   增加忽略的路由项
         *   尝试实现从实体类集合读取路由配置 提供缓存访问(及跟踪变化 刷新缓存)
         * 
         */
    }
}