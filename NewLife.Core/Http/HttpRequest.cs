using System;
using System.Text;
using NewLife.Collections;

namespace NewLife.Http
{
    /// <summary>Http请求</summary>
    public class HttpRequest : HttpBase
    {
        #region 属性
        /// <summary>Http方法</summary>
        public String Method { get; set; }

        /// <summary>资源路径</summary>
        public Uri Url { get; set; }

        /// <summary>用户代理</summary>
        public String UserAgent { get; set; }

        /// <summary>是否压缩</summary>
        public Boolean Compressed { get; set; }

        /// <summary>保持连接</summary>
        public Boolean KeepAlive { get; set; }

        /// <summary>可接受内容</summary>
        public String Accept { get; set; }

        /// <summary>接受语言</summary>
        public String AcceptLanguage { get; set; }

        /// <summary>引用路径</summary>
        public String Referer { get; set; }
        #endregion

        /// <summary>分析第一行</summary>
        /// <param name="firstLine"></param>
        protected override Boolean OnParse(String firstLine)
        {
            if (firstLine.IsNullOrEmpty()) return false;

            var ss = firstLine.Split(" ");
            if (ss.Length < 3) return false;

            // 分析请求方法 GET / HTTP/1.1
            if (ss.Length >= 3 && ss[2].StartsWithIgnoreCase("HTTP/"))
            {
                Method = ss[0];

                // 构造资源路径
                var sch = Headers["Sec-WebSocket-Key"] + "" != "" ? "ws" : "http";
                var host = Headers["Host"] + "";
                var uri = "{0}://{1}".F(sch, host);
                //var uri = "{0}://{1}".F(IsSSL ? "https" : "http", host);
                //if (host.IsNullOrEmpty() || !host.Contains(":"))
                //{
                //    var port = Local.Port;
                //    if (IsSSL && port != 443 || !IsSSL && port != 80) uri += ":" + port;
                //}
                uri += ss[1];
                Url = new Uri(uri);
            }

            UserAgent = Headers["User-Agent"] + "";
            Compressed = (Headers["Accept-Encoding"] + "").Contains("deflate");
            KeepAlive = (Headers["Connection"] + "").EqualIgnoreCase("keep-alive");
            Accept = Headers["Accept"] + "";
            AcceptLanguage = Headers["Accept-Language"] + "";
            Referer = Headers["Referer"] + "";

            return true;
        }

        /// <summary>创建头部</summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected override String BuildHeader(Int32 length)
        {
            if (Method.IsNullOrEmpty()) Method = length > 0 ? "POST" : "GET";

            // 分解主机和资源
            var host = "";
            var uri = Url;
            if (uri == null) uri = new Uri("/");

            if (uri.Scheme.EqualIgnoreCase("http", "ws"))
            {
                if (uri.Port == 80)
                    host = uri.Host;
                else
                    host = "{0}:{1}".F(uri.Host, uri.Port);
            }
            else if (uri.Scheme.EqualIgnoreCase("https"))
            {
                if (uri.Port == 443)
                    host = uri.Host;
                else
                    host = "{0}:{1}".F(uri.Host, uri.Port);
            }

            // 构建头部
            var sb = Pool.StringBuilder.Get();
            sb.AppendFormat("{0} {1} HTTP/1.1\r\n", Method, uri.PathAndQuery);
            sb.AppendFormat("Host:{0}\r\n", host);

            if (!Accept.IsNullOrEmpty()) sb.AppendFormat("Accept:{0}\r\n", Accept);
            if (Compressed) sb.AppendLine("Accept-Encoding:gzip, deflate");
            if (!AcceptLanguage.IsNullOrEmpty()) sb.AppendFormat("AcceptLanguage:{0}\r\n", AcceptLanguage);
            if (!UserAgent.IsNullOrEmpty()) sb.AppendFormat("User-Agent:{0}\r\n", UserAgent);

            // 内容长度
            if (length > 0) sb.AppendFormat("Content-Length:{0}\r\n", length);
            if (!ContentType.IsNullOrEmpty()) sb.AppendFormat("Content-Type:{0}\r\n", ContentType);

            if (KeepAlive) sb.AppendLine("Connection:keep-alive");
            if (!Referer.IsNullOrEmpty()) sb.AppendFormat("Referer:{0}\r\n", Referer);

            foreach (var item in Headers)
            {
                sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
            }

            sb.AppendLine();

            return sb.Put(true);
        }
    }
}