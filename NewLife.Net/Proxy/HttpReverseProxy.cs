using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Proxy
{
    /// <summary>Http反向代理。把所有收到的Http请求转发到目标服务器。</summary>
    /// <remarks>
    /// 主要是修改Http请求头为正确的主机，还有可能修改Http响应。
    /// 
    /// 经典用途：
    /// 1，缓存。代理缓存某些静态资源的请求结果，减少对服务器的请求压力
    /// 2，拦截。禁止访问某些资源，返回空白页或者连接重置
    /// 3，修改请求或响应。更多的可能是修改响应的页面内容
    /// 4，记录统计。记录并统计请求的网址。
    /// 
    /// 修改Http响应的一般做法：
    /// 1，反向映射888端口到目标abc.com
    /// 2，abc.com页面响应时，所有http://abc.com/的连接都修改为http://IP:888
    /// 3，注意在内网的反向代理需要使用公网IP，而不是本机IP
    /// 4，子域名也可以修改，比如http://pic.abc.com/修改为http://IP:888/http_pic.abc.com/
    /// </remarks>
    public class HttpReverseProxy : NATProxy
    {
    }
}