using System;
using System.IO;
using System.Text;
using System.Web;
using NewLife.Web;

namespace NewLife.IO
{
    /// <summary>数据流Http处理器。可以在web.config中配置一个处理器指向该类。</summary>
    public class StreamHttpHandler : IHttpHandler
    {
        #region 方法
        /// <summary>处理请求</summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            OnProcess(context);
        }

        /// <summary>处理请求</summary>
        /// <param name="context"></param>
        public virtual void OnProcess(HttpContext context)
        {
            // 以文件名（没有后缀）作为数据流总线名称
            String name = GetName(context);

            Stream stream = GetStream(context);
            if (stream != null)
                StreamHandler.Process(name, stream);
            else
            {
                //context.Response.Write("不支持的HTTP数据传输方法！");
                var sb = new StringBuilder(200);
                sb.Append("请使用POST方式提交二进制数据到：");
                sb.AppendFormat("<font color='red'>{0}</font>", context.Request.Url);
                sb.Append("<br />");
                sb.Append("或者把二进制数据转为十六进制字符串，使用GET方式提交，比如：");
                sb.AppendFormat("<font color='red'>{0}?0123456789ABCDEF</font>", context.Request.Url);
                context.Response.Write(sb.ToString());
            }
        }

        /// <summary>从Http上下文获取数据流总线名称</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual String GetName(HttpContext context)
        {
            return Path.GetFileNameWithoutExtension(context.Request.FilePath);
        }

        /// <summary>从Http上下文获取数据流</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual Stream GetStream(HttpContext context)
        {
            HttpStream stream = null;
            String method = context.Request.HttpMethod;
            if (method == "POST")
            {
                if (context.Request.ContentLength > 0)
                {
                    stream = new HttpStream(context);
                    return stream;
                }
            }
            //else if (method == "GET")
            {
                if (context.Request.QueryString != null && context.Request.QueryString.Count == 1)
                {
                    String queryText = context.Request.QueryString[0];
                    if (!String.IsNullOrEmpty(queryText))
                    {
                        Byte[] data = FromHex(queryText);
                        if (data != null && data.Length > 0)
                        {
                            stream = new HttpStream(context);
                            stream.InputStream = new MemoryStream(data);
                        }
                    }
                }
            }
            return stream;
        }

        /// <summary>是否可以重用</summary>
        public Boolean IsReusable
        {
            get
            {
                return true;
            }
        }
        #endregion

        #region 编码
        /// <summary>加密</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static String ToHex(Byte[] data)
        {
            if (data == null || data.Length < 1) return null;

            return BitConverter.ToString(data).Replace("-", null);
        }

        /// <summary>解密</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] FromHex(String data)
        {
            if (String.IsNullOrEmpty(data)) return null;

            Byte[] bts = new Byte[data.Length / 2];
            for (int i = 0; i < data.Length / 2; i++)
            {
                bts[i] = (Byte)Convert.ToInt32(data.Substring(2 * i, 2), 16);
            }
            return bts;
        }
        #endregion
    }
}