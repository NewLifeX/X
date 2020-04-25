using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NewLife.Remoting;
using NewLife.Serialization;

namespace NewLife.Yun
{
    /// <summary>阿里云文件存储</summary>
    public class OssClient
    {
        #region 属性
        /// <summary>访问域名</summary>
        public String Endpoint { get; set; }

        /// <summary>访问密钥</summary>
        public String AccessKeyId { get; set; }

        /// <summary>访问密钥</summary>
        public String AccessKeySecret { get; set; }

        private HttpClient _Client;
        #endregion

        #region 远程操作
        private HttpClient GetClient()
        {
            if (_Client != null) return _Client;

            var http = new HttpClient(new HttpClientHandler { UseProxy = false });
            http.BaseAddress = new Uri(Endpoint);

            return _Client = http;
        }

        public async Task<IDictionary<String, Object>> InvokeAsync(HttpMethod method, String action, Object args = null)
        {
            var request = ApiHelper.BuildRequest(method, action, args);

            // 时间
            request.Headers.Date = DateTimeOffset.UtcNow;
            //request.Headers.Add("Date", DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss \\G\\M\\T"));

            // 签名
            var headers = request.Headers.ToDictionary(e => e.Key, e => e.Value?.FirstOrDefault());
            var canonicalString = BuildCanonicalString(method.Method, action, headers, null);
            var signature = canonicalString.GetBytes().SHA1(AccessKeySecret.GetBytes()).ToBase64();
            request.Headers.Authorization = new AuthenticationHeaderValue("OSS", AccessKeyId + ":" + signature);

            var http = GetClient();
            var rs = await http.SendAsync(request);

            return await ApiHelper.ProcessResponse<IDictionary<String, Object>>(rs);
        }

        private void OnRequest(HttpRequestMessage request)
        {

        }
        #endregion

        #region Bucket操作
        public async Task<String[]> ListBuckets(String prefix = null, String marker = null, Int32 maxKeys = 100)
        {
            var rs = (prefix.IsNullOrEmpty() && marker.IsNullOrEmpty() && maxKeys == 100) ?
                await InvokeAsync(HttpMethod.Get, "/") :
                await InvokeAsync(HttpMethod.Get, "/", new { prefix, marker, maxKeys });

            var bs = rs?["Buckets"] as IDictionary<String, Object>;
            var bk = bs?["Bucket"] as IList<Object>;

            return bk?.Select(e => (e as IDictionary<String, Object>)["Name"] + "").ToArray();
        }
        #endregion

        #region Object操作
        #endregion

        #region 辅助
        private const String NewLineMarker = "\n";

        private static readonly IList<String> ParamtersToSign = new List<String> {
            "acl", "uploadId", "partNumber", "uploads", "cors", "logging",
            "website", "delete", "referer", "lifecycle", "security-token","append",
            "position", "x-oss-process", "restore", "bucketInfo", "stat", "symlink",
            "location", "qos", "policy", "tagging", "requestPayment", "x-oss-traffic-limit",
            "objectMeta", "encryption", "versioning", "versionId", "versions",
            "response-cache-control",
            "response-content-disposition",
            "response-content-encoding",
            "response-content-language",
            "response-content-type",
            "response-expires"
        };

        private static String BuildCanonicalString(String method, String resourcePath, IDictionary<String, String> headers, IDictionary<String, String> parameters)
        {
            var sb = new StringBuilder();

            sb.Append(method).Append(NewLineMarker);

            var headersToSign = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (header.Key.EqualIgnoreCase("Content-Type", "Content-MD5", "Date") ||
                        header.Key.StartsWith("x-oss-"))
                        headersToSign.Add(header.Key, header.Value);
                }
            }

            if (!headersToSign.ContainsKey("Content-Type")) headersToSign.Add("Content-Type", "");
            if (!headersToSign.ContainsKey("Content-MD5")) headersToSign.Add("Content-MD5", "");

            var sortedHeaders = headersToSign.Keys.OrderBy(e => e).ToList();
            foreach (var key in sortedHeaders)
            {
                var value = headersToSign[key];
                if (key.StartsWith("x-oss-"))
                    sb.Append(key.ToLowerInvariant()).Append(':').Append(value);
                else
                    sb.Append(value);

                sb.Append(NewLineMarker);
            }

            sb.Append(resourcePath);

            if (parameters != null)
            {
                var separator = '?';
                foreach (var paramName in parameters.Keys.OrderBy(e => e))
                {
                    if (!ParamtersToSign.Contains(paramName)) continue;

                    sb.Append(separator);
                    sb.Append(paramName);
                    var paramValue = parameters[paramName];
                    if (!String.IsNullOrEmpty(paramValue))
                        sb.Append("=").Append(paramValue);

                    separator = '&';
                }
            }

            return sb.ToString();
        }
        #endregion
    }
}