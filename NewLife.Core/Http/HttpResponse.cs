using System;
using System.Linq;
using System.Net;
using System.Text;

namespace NewLife.Http
{
    /// <summary>Http响应</summary>
    public class HttpResponse : HttpBase
    {
        #region 属性
        ///// <summary>是否WebSocket</summary>
        //public Boolean IsWebSocket { get; set; }

        /// <summary>是否启用SSL</summary>
        public Boolean IsSSL { get; set; }

        /// <summary>状态码</summary>
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        /// <summary>状态描述</summary>
        public String StatusDescription { get; set; }
        #endregion

        /// <summary>分析第一行</summary>
        /// <param name="firstLine"></param>
        protected override Boolean OnParse(String firstLine)
        {
            if (firstLine.IsNullOrEmpty()) return false;

            // HTTP/1.1 502 Bad Gateway

            var ss = firstLine.Split(" ");
            //if (ss.Length < 3) throw new Exception("非法响应头 {0}".F(firstLine));
            if (ss.Length < 3) return false;

            // 分析响应码
            var code = ss[1].ToInt();
            if (code > 0) StatusCode = (HttpStatusCode)code;

            StatusDescription = ss.Skip(2).Join(" ");
            //ContentLength = Headers["Content-Length"].ToInt();

            return true;
        }

        /// <summary>创建头部</summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected override String BuildHeader(Int32 length)
        {
            // 构建头部
            var sb = new StringBuilder();
            sb.AppendFormat("HTTP/1.1 {0} {1}\r\n", (Int32)StatusCode, StatusCode);

            // 内容长度
            if (length > 0) sb.AppendFormat("Content-Length:{0}\r\n", length);
            if (!ContentType.IsNullOrEmpty()) sb.AppendFormat("Content-Type:{0}\r\n", ContentType);

            foreach (var item in Headers)
            {
                sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
            }

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>验证，如果失败则抛出异常</summary>
        public void Valid()
        {
            if (StatusCode != HttpStatusCode.OK) throw new Exception(StatusDescription ?? (StatusCode + ""));
        }
    }
}