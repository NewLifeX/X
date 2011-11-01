using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace NewLife.Mvc
{
    /// <summary>一般控制器</summary>
    public class GenericController : IController
    {
        #region 属性
        /// <summary>Http上下文</summary>
        public HttpContext Context { get { return HttpContext.Current; } }

        private HttpRequest _Request;
        /// <summary>Http请求</summary>
        public HttpRequest Request { get { return HttpContext.Current == null ? null : HttpContext.Current.Request; } }

        private HttpResponse _Response;
        /// <summary>Http响应</summary>
        public HttpResponse Response { get { return HttpContext.Current == null ? null : HttpContext.Current.Response; } }
        #endregion

        #region IController 成员
        /// <summary>
        /// 执行
        /// </summary>
        public virtual void Execute()
        {
            // 根据Url，计算模版，调用模版引擎
            Uri uri = Request.Url;
            //String path = uri.AbsolutePath;
            String templateName = String.Join(@"/", uri.Segments, 1, uri.Segments.Length - 1);

            ITemplateEngine engine = Service.Resolve<ITemplateEngine>();
            Dictionary<String, Object> data = new Dictionary<String, Object>();
            data.Add("Controller", this);

            String html = engine.Render(templateName, data);

            Response.Write(html);
        }
        #endregion
    }
}