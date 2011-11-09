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

        #region IController 成员

        /// <summary>
        /// 执行
        /// </summary>
        public virtual void Execute()
        {
            Render(null);
        }

        /// <summary>
        /// 使用指定的参数产生模版中使用的数据
        /// </summary>
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

        /// <summary>
        /// 向指定模版数据添加额外的,如果参数data为null会自动创建一个新的data,返回添加了数据的data
        /// </summary>
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

        /// <summary>
        /// 使用默认的模版生成页面
        /// </summary>
        /// <param name="data"></param>
        public virtual void Render(IDictionary<string, object> data)
        {
            data = Data(data, "Controller", this);
            Render(RouteContext.Current.RoutePath, data);
        }

        /// <summary>
        /// 使用指定模版生成页面
        /// </summary>
        /// <param name="path">相对于模版路径的</param>
        /// <param name="data"></param>
        public virtual void Render(string path, IDictionary<string, object> data)
        {
            ITemplateEngine engine = Service.Resolve<ITemplateEngine>();
            path = GenericControllerFactory.ResolveTempletePath(path);
            String html = engine.Render(path, data);
            Response.Write(html);
        }

        #endregion IController 成员
    }
}