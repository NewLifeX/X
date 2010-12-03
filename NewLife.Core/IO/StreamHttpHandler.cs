using System;
using System.Web;
using NewLife.Web;
using System.IO;

namespace NewLife.IO
{
    /// <summary>
    /// 数据流Http处理器。可以在web.config中配置一个处理器指向该类。
    /// </summary>
    public class StreamHttpHandler : IHttpHandler
    {
        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            // 以文件名（没有后缀）作为数据流工厂总线名称
            String name = Path.GetFileNameWithoutExtension(context.Request.FilePath);
            StreamHandlerFactory.Process(name, new HttpStream(context));
        }

        /// <summary>
        /// 是否可以重用
        /// </summary>
        public Boolean IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}
