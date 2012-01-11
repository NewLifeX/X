using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using NewLife.IO;

namespace NewLife.Net.Protocols.Http
{
    /// <summary>Http头部</summary>
    public class HttpHeader
    {
        #region 属性
        /// <summary>是否响应。以Method是否为空作为一句。</summary>
        public Boolean IsResponse { get { return String.IsNullOrEmpty(Method); } }

        private String _Method;
        /// <summary>请求方法</summary>
        public String Method { get { return _Method; } set { _Method = value; } }

        private Uri _Url;
        /// <summary>请求文档</summary>
        public Uri Url { get { return _Url; } set { _Url = value; } }

        private String _Version;
        /// <summary>协议版本</summary>
        public String Version { get { return _Version; } set { _Version = value; } }

        private Int32 _StatusCode;
        /// <summary>状态码</summary>
        public Int32 StatusCode { get { return _StatusCode; } set { _StatusCode = value; } }

        private String _StatusDescription;
        /// <summary>状态描述</summary>
        public String StatusDescription { get { return _StatusDescription; } set { _StatusDescription = value; } }

        private IDictionary<String, String> _Headers;
        /// <summary>头部</summary>
        public IDictionary<String, String> Headers { get { return _Headers ?? (_Headers = new HeaderCollection()); } }

        //const String VersionPrefix = "HTTP/";
        #endregion

        #region 扩展属性
        /// <summary>主机头</summary>
        public String Host { get { return Headers["Host"]; } set { Headers["Host"] = value; } }

        /// <summary>引用页</summary>
        public String Referer { get { return Headers["Referer"]; } set { Headers["Referer"] = value; } }

        /// <summary>内容长度</summary>
        public Int32 ContentLength { get { return Headers["Content-Length"].IsNullOrWhiteSpace() ? 0 : Int32.Parse(Headers["Content-Length"]); } set { Headers["Content-Length"] = value.ToString(); } }

        /// <summary>原始地址。直接代理会包括全路径</summary>
        public String RawUrl { get { return Url != null && Url.IsAbsoluteUri ? Url.ToString() : String.Format("http://{0}{1}", Host, Url); } }
        #endregion

        #region 方法
        /// <summary>从流中读取Http头部对象。如果不是Http头部，指针要回到原来位置</summary>
        /// <param name="stream"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static HttpHeader Read(Stream stream, HttpHeaderReadMode mode = HttpHeaderReadMode.RequestOrResponse)
        {
            HttpHeader entity = null;
            var p = stream.Position;
            using (var reader = new StreamReaderX(stream) { Closable = false })
            {
                entity = ReadFirst(reader);
                if (entity == null) return null;

                switch (mode)
                {
                    case HttpHeaderReadMode.Request:
                        if (entity.IsResponse) { stream.Position = p; return null; }
                        break;
                    case HttpHeaderReadMode.Response:
                        if (!entity.IsResponse) { stream.Position = p; return null; }
                        break;
                    default:
                        break;
                }

                entity.ReadHeaders(reader);

                // 因为涉及字符编码，所以跟流位置可能不同。对于ASCII编码没有问题。
                stream.Position = p + reader.CharPosition;
            }

            return entity;
        }

        /// <summary>仅读取第一行。如果不是Http头部，指针要回到原来位置</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static HttpHeader ReadFirst(StreamReader reader)
        {
            // 如果不是Http头部，指针要回到原来位置
            var stream = reader.BaseStream;
            var p = stream.Position;

            String line = reader.ReadLine();
            if (line.IsNullOrWhiteSpace()) { stream.Position = p; return null; }
            var ss = line.Split(new Char[] { ' ' }, 3);
            if (ss == null || ss.Length < 3 || ss[0].IsNullOrWhiteSpace() || ss[0].Length > 10) { stream.Position = p; return null; }

            var entity = new HttpHeader();
            if (ss[0].StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
            {
                entity.Version = ss[0];
                entity.StatusCode = Int32.Parse(ss[1]);
                entity.StatusDescription = ss[2];
            }
            else
            {
                entity.Method = ss[0];
                entity.Url = new Uri(ss[1], UriKind.RelativeOrAbsolute);
                entity.Version = ss[2];
            }

            return entity;
        }

        /// <summary>读取头部键值</summary>
        /// <param name="reader"></param>
        public void ReadHeaders(StreamReader reader)
        {
            String line;
            while (!String.IsNullOrEmpty(line = reader.ReadLine()))
            {
                Int32 p = line.IndexOf(":");
                Headers.Add(line.Substring(0, p).Trim(), line.Substring(p + 1).Trim());
            }
        }

        /// <summary>往流中写入Http头</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            // StreamWriter太恶心了，自动把流给关闭了，还没有地方设置
            using (var writer = new StreamWriterX(stream) { Closable = false })
            {
                if (!IsResponse)
                    writer.WriteLine("{0} {1} {2}", Method, Url, Version);
                else
                    writer.WriteLine("{0} {1} {2}", Version, StatusCode, StatusDescription);
                foreach (var item in Headers)
                {
                    writer.WriteLine("{0}: {1}", item.Key, item.Value);
                }
                writer.WriteLine();
            }
        }

        /// <summary>已重载。以文本形式呈现整个头部</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!IsResponse)
                return String.Format("{0} {1} {2}", Method, RawUrl, Version);
            else
                return String.Format("{0} {1} {2}", Version, StatusCode, StatusDescription);
        }

        /// <summary>文本形式</summary>
        /// <returns></returns>
        public string ToText()
        {
            StringBuilder sb = new StringBuilder();
            if (!IsResponse)
                sb.AppendFormat("{0} {1} {2}", Method, Url, Version);
            else
                sb.AppendFormat("{0} {1} {2}", Version, StatusCode, StatusDescription);
            sb.AppendLine();
            foreach (var item in Headers)
            {
                sb.AppendFormat("{0}: {1}", item.Key, item.Value);
                sb.AppendLine();
            }
            sb.AppendLine();

            return sb.ToString();
        }
        #endregion

        #region 辅助
        class HeaderCollection : Dictionary<String, String>, IDictionary<String, String>
        {
            public HeaderCollection() : base(StringComparer.OrdinalIgnoreCase) { }

            public new String this[String key]
            {
                get
                {
                    String v = null;
                    return TryGetValue(key, out v) ? v : null;
                }
                set { base[key] = value; }
            }
        }
        #endregion
    }

    /// <summary>读取模式</summary>
    public enum HttpHeaderReadMode
    {
        /// <summary>请求或响应</summary>
        RequestOrResponse,

        /// <summary>请求</summary>
        Request,

        /// <summary>响应</summary>
        Response
    }

    /// <summary>Http谓语</summary>
    enum HttpVerb
    {
        /// <summary>未解析</summary>
        Unparsed,

        /// <summary>未知</summary>
        Unknown,

        /// <summary>获取</summary>
        GET,

        /// <summary>推送</summary>
        PUT,

        /// <summary>跟GET一样，只不过响应包只包括头部而没有内容</summary>
        HEAD,

        /// <summary>提交</summary>
        POST,

        /// <summary>调试</summary>
        DEBUG,

        /// <summary>跟踪</summary>
        TRACE,

        /// <summary>连接</summary>
        CONNECT,

        /// <summary>选项</summary>
        OPTIONS,

        /// <summary>删除</summary>
        DELETE
    }
}