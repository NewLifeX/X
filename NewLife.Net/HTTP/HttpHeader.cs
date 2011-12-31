using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.IO;

namespace NewLife.Net.Protocols.Http
{
    /// <summary>Http头部</summary>
    public class HttpHeader
    {
        #region 属性
        private HttpVerb _Verb;
        /// <summary>谓语</summary>
        public HttpVerb Verb { get { return _Verb; } set { _Verb = value; } }

        private String _Url;
        /// <summary>请求文档</summary>
        public String Url { get { return _Url; } set { _Url = value; } }

        private String _Version;
        /// <summary>协议版本</summary>
        public String Version { get { return _Version; } set { _Version = value; } }

        private IDictionary<String, String> _Headers;
        /// <summary>头部</summary>
        public IDictionary<String, String> Headers { get { return _Headers ?? (_Headers = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)); } }

        const String VersionPrefix = "HTTP/";
        #endregion

        #region 扩展属性
        /// <summary>原始地址。直接代理会包括全路径</summary>
        public String RawUrl { get { return Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? Url : String.Format("http://{0}{1}", Headers["Host"], Url); } }
        #endregion

        #region 方法
        /// <summary>从流中读取Http头部对象</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static HttpHeader Read(Stream stream)
        {
            var entity = new HttpHeader();
            using (StreamReader reader = new StreamReader(stream))
            {
                String line = reader.ReadLine();
                var ss = line.Split(" ");
                entity.Verb = (HttpVerb)Enum.Parse(typeof(HttpVerb), ss[0], true);
                entity.Url = ss[1];
                entity.Version = ss[2];
                if (entity.Version.StartsWith(VersionPrefix, StringComparison.OrdinalIgnoreCase)) entity.Version = entity.Version.Substring(VersionPrefix.Length);

                while (!String.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    Int32 p = line.IndexOf(":");
                    entity.Headers.Add(line.Substring(0, p).Trim(), line.Substring(p + 1).Trim());
                }
            }

            return entity;
        }

        /// <summary>往流中写入Http头</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            // StreamWriter太恶心了，自动把流给关闭了，还没有地方设置
            //using (StreamWriter writer = new StreamWriter(stream))
            //{
            //    var ver = Version;
            //    if (!ver.StartsWith(VersionPrefix, StringComparison.OrdinalIgnoreCase)) ver = VersionPrefix + ver;
            //    writer.WriteLine("{0} {1} {2}", Verb, Url, ver);
            //    foreach (var item in Headers)
            //    {
            //        writer.WriteLine("{0}: {1}", item.Key, item.Value);
            //    }
            //    writer.WriteLine();
            //}

            Byte[] buffer = Encoding.ASCII.GetBytes(this.ToString());
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>已重载。以文本形式呈现整个头部</summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var ver = Version;
            if (!ver.StartsWith(VersionPrefix, StringComparison.OrdinalIgnoreCase)) ver = VersionPrefix + ver;
            sb.AppendFormat("{0} {1} {2}", Verb, Url, ver);
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
    }

    /// <summary>Http谓语</summary>
    public enum HttpVerb
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