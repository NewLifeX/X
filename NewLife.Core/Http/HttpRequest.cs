using System;
using NewLife.Collections;
using NewLife.Data;

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

        /// <summary>目标主机</summary>
        public String Host { get; set; }

        /// <summary>保持连接</summary>
        public Boolean KeepAlive { get; set; }
        #endregion

        /// <summary>分析第一行</summary>
        /// <param name="firstLine"></param>
        protected override Boolean OnParse(String firstLine)
        {
            if (firstLine.IsNullOrEmpty()) return false;

            var ss = firstLine.Split(' ');
            if (ss.Length < 3) return false;

            // 分析请求方法 GET / HTTP/1.1
            if (ss.Length >= 3 && ss[2].StartsWithIgnoreCase("HTTP/"))
            {
                Method = ss[0];
                Url = new Uri(ss[1], UriKind.RelativeOrAbsolute);
                Version = ss[2].TrimStart("HTTP/");
            }

            Host = Headers["Host"];
            KeepAlive = Headers["Connection"].EqualIgnoreCase("keep-alive");

            return true;
        }

        private static readonly Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n' };
        /// <summary>快速分析请求头，只分析第一行</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public Boolean FastParse(Packet pk)
        {
            if (!FastValidHeader(pk)) return false;

            var p = pk.IndexOf(NewLine);
            if (p < 0) return false;

            var line = pk.ReadBytes(0, p).ToStr();

            Body = pk.Slice(p + 2);

            // 分析第一行
            if (!OnParse(line)) return false;

            return true;
        }

        /// <summary>创建头部</summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected override String BuildHeader(Int32 length)
        {
            if (Method.IsNullOrEmpty()) Method = length > 0 ? "POST" : "GET";

            // 分解主机和资源
            var uri = Url;
            if (uri == null) uri = new Uri("/");

            if (Host.IsNullOrEmpty())
            {
                var host = "";
                if (uri.Scheme.EqualIgnoreCase("http", "ws"))
                {
                    if (uri.Port == 80)
                        host = uri.Host;
                    else
                        host = $"{uri.Host}:{uri.Port}";
                }
                else if (uri.Scheme.EqualIgnoreCase("https", "wss"))
                {
                    if (uri.Port == 443)
                        host = uri.Host;
                    else
                        host = $"{uri.Host}:{uri.Port}";
                }
                Host = host;
            }

            // 构建头部
            var sb = Pool.StringBuilder.Get();
            sb.AppendFormat("{0} {1} HTTP/{2}\r\n", Method, uri, Version);
            sb.AppendFormat("Host:{0}\r\n", Host);

            // 内容长度
            if (length > 0) Headers["Content-Length"] = length + "";
            if (!ContentType.IsNullOrEmpty()) Headers["Content-Type"] = ContentType;

            if (KeepAlive) Headers["Connection"] = "keep-alive";

            foreach (var item in Headers)
            {
                if (!item.Key.EqualIgnoreCase("Host"))
                    sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
            }

            sb.AppendLine();

            return sb.Put(true);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{Method} {Url}";
    }
}