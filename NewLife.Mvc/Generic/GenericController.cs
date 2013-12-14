using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;

namespace NewLife.Mvc
{
    /// <summary>一般控制器</summary>
    public class GenericController : IController
    {
        #region 属性

        /// <summary>Http上下文</summary>
        public HttpContext Context { get { return HttpContext.Current; } }

        /// <summary>Http请求</summary>
        public HttpRequest Request { get { return HttpContext.Current == null ? null : HttpContext.Current.Request; } }

        /// <summary>Http响应</summary>
        public HttpResponse Response { get { return HttpContext.Current == null ? null : HttpContext.Current.Response; } }

        /// <summary>HttpServer</summary>
        public HttpServerUtility Server { get { return HttpContext.Current == null ? null : HttpContext.Current.Server; } }

        /// <summary>会话</summary>
        public HttpSessionState Session { get { return HttpContext.Current == null ? null : HttpContext.Current.Session; } }

        #endregion 属性

        #region 方法

        /// <summary>使用指定的参数产生模版中使用的数据</summary>
        /// <example>
        /// 一般用法:
        /// <code>
        /// Data(
        ///     "keyname1", "value1", // 第一个必须是字符串
        ///     "keyname2", "value2",
        ///     "" // 会忽略没有成对出现的
        /// );
        /// </code>
        /// </example>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual IDictionary<string, object> Data(params object[] args)
        {
            return Data(null, args);
        }

        /// <summary>向指定模版数据添加额外的,如果参数data为null会自动创建一个新的data,返回添加了数据的data</summary>
        /// <example>
        /// 一般用法:
        /// <code>
        /// Data(dict,
        ///     "newkeyname1", "value1", // 如果dict中已经包含newkeyname1则会覆盖
        ///     ""
        /// );
        /// </code>
        /// </example>
        /// <param name="data"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual IDictionary<string, object> Data(IDictionary<string, object> data, params object[] args)
        {
            int n = args.Length & ~1;
            if (data == null) data = new Dictionary<string, object>(n);
            for (int i = 0; i < n; i += 2)
            {
                data[args[i].ToString()] = args[i + 1];
            }
            return data;
        }

        /// <summary>使用默认的模版生成页面</summary>
        /// <param name="data"></param>
        public virtual void Render(IDictionary<string, object> data)
        {
            data = Data(data, "Controller", this);
            Render(RouteContext.Current.RoutePath, data);
        }

        /// <summary>使用指定模版生成页面</summary>
        /// <param name="path">相对于模版路径的</param>
        /// <param name="data"></param>
        public virtual void Render(string path, IDictionary<string, object> data)
        {
            var engine = Service.Container.Resolve<ITemplateEngine>();
            path = GenericControllerFactory.ResolveTempletePath(path);
            String html = engine.Render(path, data);
            Response.Write(html);
        }

        #endregion 方法

        #region IController 成员

        /// <summary>通过实现 <see cref="T:NewLife.Mvc.IController" /> 接口的自定义 Controller 启用 HTTP Web 请求的处理。</summary>
        /// <param name="context"><see cref="T:NewLife.Mvc.IRouteContext" /> 对象，它提供对用于为 HTTP 请求提供服务的内部服务器对象的引用。</param>
        public virtual void ProcessRequest(IRouteContext context)
        {
            Render(null);
        }

        /// <summary>获取一个值，该值指示其他请求是否可以使用 <see cref="T:NewLife.Mvc.IController" /> 实例。</summary>
        /// <returns>如果 <see cref="T:NewLife.Mvc.IController" /> 实例可再次使用，则为 true；否则为 false。</returns>
        public virtual bool IsReusable
        {
            get { return false; }
        }

        #endregion IController 成员
    }
}