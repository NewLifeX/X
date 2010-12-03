using System.Web;
using NewLife.IO;

namespace NewLife.Web
{
    /// <summary>
    /// HTTP输入输出流
    /// </summary>
    public class HttpStream : ReadWriteStream
    {
        #region 属性
        private HttpContext _Context;
        /// <summary>HTTP上下文</summary>
        public HttpContext Context
        {
            get { return _Context; }
            private set { _Context = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="context"></param>
        public HttpStream(HttpContext context)
            : base(context.Request.InputStream, context.Response.OutputStream)
        {
            Context = context;
        }
        #endregion

        /// <summary>
        /// 已重载。
        /// </summary>
        public override void Flush()
        {
            Context.Response.Flush();
        }
    }
}