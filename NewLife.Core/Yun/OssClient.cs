using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Remoting;

namespace NewLife.Yun
{
    /// <summary>阿里云文件存储</summary>
    public class OssClient
    {
        #region 属性
        /// <summary>访问域名</summary>
        public String Endpoint { get; set; } = "http://oss-cn-shanghai.aliyuncs.com";

        /// <summary>访问密钥</summary>
        public String AccessKeyId { get; set; }

        /// <summary>访问密钥</summary>
        public String AccessKeySecret { get; set; }

        /// <summary>存储空间</summary>
        public String BucketName { get; set; }

        private String _bucketName;
        private String _baseAddress;
        private HttpClient _Client;
        #endregion

        #region 远程操作
        private HttpClient GetClient()
        {
            if (_Client != null) return _Client;

            var http = new HttpClient(new HttpClientHandler { UseProxy = false })
            {
                BaseAddress = new Uri(_baseAddress ?? Endpoint)
            };

            return _Client = http;
        }

        private void SetBucket(String bucketName)
        {
            var url = Endpoint;
            if (!bucketName.IsNullOrEmpty())
            {
                var ss = Endpoint.Split("://");
                url = $"{ss[0]}://{bucketName}.{ss[1]}";
            }

            // 判断是否有改变
            if (_baseAddress != url)
            {
                _baseAddress = url;
                _Client = null;
            }

            _bucketName = bucketName;
        }

        /// <summary>异步调用命令</summary>
        /// <param name="method"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected async Task<IDictionary<String, Object>> InvokeAsync(HttpMethod method, String action, Object args = null)
        {
            var request = ApiHelper.BuildRequest(method, action, args);

            // 资源路径
            var resourcePath = action;
            if (!_bucketName.IsNullOrEmpty()) resourcePath = "/" + _bucketName + resourcePath;

            // 时间
            request.Headers.Date = DateTimeOffset.UtcNow;
            //request.Headers.Add("Date", DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss \\G\\M\\T"));

            // 签名
            var headers = request.Headers.ToDictionary(e => e.Key, e => e.Value?.FirstOrDefault());
            //var parameters = args?.ToDictionary().ToDictionary(e => e.Key, e => e.Value + "");
            var canonicalString = BuildCanonicalString(method.Method, resourcePath, headers, null);
            var signature = canonicalString.GetBytes().SHA1(AccessKeySecret.GetBytes()).ToBase64();
            request.Headers.Authorization = new AuthenticationHeaderValue("OSS", AccessKeyId + ":" + signature);

            var http = GetClient();
            var rs = await http.SendAsync(request);

            return await ApiHelper.ProcessResponse<IDictionary<String, Object>>(rs);
        }
        #endregion

        #region Bucket操作
        /// <summary>列出所有存储空间名称</summary>
        /// <returns></returns>
        public async Task<String[]> ListBuckets()
        {
            SetBucket(null);

            var rs = await InvokeAsync(HttpMethod.Get, "/");

            var bs = rs?["Buckets"] as IDictionary<String, Object>;
            var bk = bs?["Bucket"] as IList<Object>;

            return bk?.Select(e => (e as IDictionary<String, Object>)["Name"] + "").ToArray();
        }

        /// <summary>列出所有存储空间明细，支持过滤</summary>
        /// <param name="prefix"></param>
        /// <param name="marker"></param>
        /// <param name="maxKeys"></param>
        /// <returns></returns>
        public async Task<IList<Object>> ListBuckets(String prefix, String marker, Int32 maxKeys = 100)
        {
            SetBucket(null);

            var rs = await InvokeAsync(HttpMethod.Get, "/", new { prefix, marker, maxKeys });

            var bs = rs?["Buckets"] as IDictionary<String, Object>;
            var bk = bs?["Bucket"] as IList<Object>;

            return bk;
        }
        #endregion

        #region Object操作
        /// <summary>列出所有文件名称</summary>
        /// <returns></returns>
        public async Task<String[]> ListObjects()
        {
            SetBucket(BucketName);

            var rs = await InvokeAsync(HttpMethod.Get, "/");

            var contents = rs?["Contents"] as IList<Object>;

            return contents?.Select(e => (e as IDictionary<String, Object>)["Key"] + "").ToArray();
        }

        /// <summary>列出所有文件明细，支持过滤</summary>
        /// <param name="prefix"></param>
        /// <param name="marker"></param>
        /// <param name="maxKeys"></param>
        /// <returns></returns>
        public async Task<IList<Object>> ListObjects(String prefix, String marker, Int32 maxKeys = 100)
        {
            SetBucket(BucketName);

            var rs = await InvokeAsync(HttpMethod.Get, "/", new { prefix, marker, maxKeys });

            var contents = rs?["Contents"] as IList<Object>;

            return contents;
        }

        /// <summary>上传文件</summary>
        /// <param name="objectName">对象文件名</param>
        /// <param name="data">数据内容</param>
        /// <returns></returns>
        public async Task<String[]> PutObject(String objectName, Byte[] data)
        {
            SetBucket(BucketName);

            var rs = await InvokeAsync(HttpMethod.Put, "/" + objectName, data);

            var contents = rs?["Contents"] as IList<Object>;

            return contents?.Select(e => (e as IDictionary<String, Object>)["Key"] + "").ToArray();
        }
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