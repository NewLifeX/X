using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Net
{
    /// <summary>Http帮助类</summary>
    public static class HttpHelper
    {
        #region Http封包解包
        /// <summary>创建请求包</summary>
        /// <param name="method"></param>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Packet MakeRequest(String method, Uri uri, IDictionary<String, Object> headers, Packet pk)
        {
            if (method.IsNullOrEmpty()) method = pk?.Count > 0 ? "POST" : "GET";

            // 分解主机和资源
            var host = "";
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
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} HTTP/1.1\r\n", method, uri.PathAndQuery);
            sb.AppendFormat("Host:{0}\r\n", host);

            //if (Compressed) sb.AppendLine("Accept-Encoding:gzip, deflate");
            //if (KeepAlive) sb.AppendLine("Connection:keep-alive");
            //if (!UserAgent.IsNullOrEmpty()) sb.AppendFormat("User-Agent:{0}\r\n", UserAgent);

            // 内容长度
            if (pk?.Count > 0) sb.AppendFormat("Content-Length:{0}\r\n", pk.Count);

            foreach (var item in headers)
            {
                sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
            }

            sb.AppendLine();

            //return sb.ToString();
            var rs = new Packet(sb.ToString().GetBytes());
            rs.Next = pk;
            return rs;
        }

        /// <summary>创建响应包</summary>
        /// <param name="code"></param>
        /// <param name="headers"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Packet MakeResponse(HttpStatusCode code, IDictionary<String, Object> headers, Packet pk)
        {
            // 构建头部
            var sb = new StringBuilder();
            sb.AppendFormat("HTTP/1.1 {0} {1}\r\n", (Int32)code, code);

            // 内容长度
            if (pk?.Count > 0) sb.AppendFormat("Content-Length:{0}\r\n", pk.Count);

            foreach (var item in headers)
            {
                sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
            }

            sb.AppendLine();

            //return sb.ToString();
            var rs = new Packet(sb.ToString().GetBytes());
            rs.Next = pk;
            return rs;
        }

        /// <summary>分析头部</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static IDictionary<String, Object> ParseHeader(Packet pk)
        {
            // 客户端收到响应，服务端收到请求
            var headers = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

            var p = (Int32)pk.Data.IndexOf(pk.Offset, pk.Count, "\r\n\r\n".GetBytes());
            if (p < 0) return headers;

#if DEBUG
            //WriteLog(pk.ToStr());
#endif

            // 截取
            var lines = pk.ReadBytes(0, p).ToStr().Split("\r\n");
            // 重构
            p += 4;
            pk.Set(pk.Data, pk.Offset + p, pk.Count - p);

            // 分析头部
            headers.Clear();
            var line = lines[0];
            for (var i = 1; i < lines.Length; i++)
            {
                line = lines[i];
                p = line.IndexOf(':');
                if (p > 0) headers[line.Substring(0, p)] = line.Substring(p + 1).Trim();
            }

            line = lines[0];
            var ss = line.Split(" ");
            // 分析请求方法 GET / HTTP/1.1
            if (ss.Length >= 3 && ss[2].StartsWithIgnoreCase("HTTP/"))
            {
                headers["Method"] = ss[0];

                // 构造资源路径
                var host = headers["Host"] + "";
                var uri = "{0}://{1}".F("http", host);
                //var uri = "{0}://{1}".F(IsSSL ? "https" : "http", host);
                //if (host.IsNullOrEmpty() || !host.Contains(":"))
                //{
                //    var port = Local.Port;
                //    if (IsSSL && port != 443 || !IsSSL && port != 80) uri += ":" + port;
                //}
                uri += ss[1];
                headers["Url"] = new Uri(uri);
            }
            else
            {
                // 分析响应码
                var code = ss[1].ToInt();
                if (code > 0) headers["StatusCode"] = (HttpStatusCode)code;
            }

            return headers;
        }
        #endregion
    }
}