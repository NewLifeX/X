using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.Yun
{
    /// <summary>阿里云文件存储</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/oss
    /// </remarks>
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

            var handler = new HttpClientHandler { UseProxy = false };
            var http = DefaultTracer.Instance?.CreateHttpClient(handler) ?? new HttpClient(handler);
            http.BaseAddress = new Uri(_baseAddress ?? Endpoint);

            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var asmName = asm?.GetName();
            if (asmName != null)
            {
                //var userAgent = $"{asmName.Name}/{asmName.Version}({Environment.OSVersion};{Environment.Version})";
                http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(asmName.Name, asmName.Version + ""));
            }

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
        protected async Task<TResult> InvokeAsync<TResult>(HttpMethod method, String action, Object args = null)
        {
            var request = ApiHelper.BuildRequest(method, action, args);

            // 资源路径
            var resourcePath = action;
            if (!_bucketName.IsNullOrEmpty()) resourcePath = "/" + _bucketName + resourcePath;

            // 时间
            request.Headers.Date = DateTimeOffset.UtcNow;
            //request.Headers.Add("Date", DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss \\G\\M\\T"));

            // 签名
            var canonicalString = BuildCanonicalString(method.Method, resourcePath, request);
            var signature = canonicalString.GetBytes().SHA1(AccessKeySecret.GetBytes()).ToBase64();
            request.Headers.Authorization = new AuthenticationHeaderValue("OSS", AccessKeyId + ":" + signature);

            var http = GetClient();
            var rs = await http.SendAsync(request);

            return await ApiHelper.ProcessResponse<TResult>(rs);
        }

        private async Task<IDictionary<String, Object>> GetAsync(String action, Object args = null) => await InvokeAsync<IDictionary<String, Object>>(HttpMethod.Get, action, args);
        #endregion

        #region Bucket操作
        /// <summary>列出所有存储空间名称</summary>
        /// <returns></returns>
        public async Task<String[]> ListBuckets()
        {
            SetBucket(null);

            var rs = await GetAsync("/");

            var bs = rs?["Buckets"] as IDictionary<String, Object>;
            var bk = bs?["Bucket"];

            if (bk is IList<Object> list) return list.Select(e => (e as IDictionary<String, Object>)["Name"] + "").ToArray();
            if (bk is IDictionary<String, Object> dic) return new[] { dic["Name"] + "" };

            return null;
        }

        /// <summary>列出所有存储空间明细，支持过滤</summary>
        /// <param name="prefix"></param>
        /// <param name="marker"></param>
        /// <param name="maxKeys"></param>
        /// <returns></returns>
        public async Task<IList<Object>> ListBuckets(String prefix, String marker, Int32 maxKeys = 100)
        {
            SetBucket(null);

            var rs = await GetAsync("/", new { prefix, marker, maxKeys });

            var bs = rs?["Buckets"] as IDictionary<String, Object>;
            var bk = bs?["Bucket"] as IList<Object>;
            if (bk is IList<Object> list) return list;
            return new[] { bk };
        }
        #endregion

        #region Object操作
        /// <summary>列出所有文件名称</summary>
        /// <returns></returns>
        public async Task<String[]> ListObjects()
        {
            SetBucket(BucketName);

            var rs = await GetAsync("/");

            var contents = rs?["Contents"];
            if (contents is IList<Object> list) return list?.Select(e => (e as IDictionary<String, Object>)["Key"] + "").ToArray();
            if (contents is IDictionary<String, Object> dic) return new[] { dic["Key"] + "" };

            return null;
        }

        /// <summary>列出所有文件明细，支持过滤</summary>
        /// <param name="prefix"></param>
        /// <param name="marker"></param>
        /// <param name="maxKeys"></param>
        /// <returns></returns>
        public async Task<IList<Object>> ListObjects(String prefix, String marker, Int32 maxKeys = 100)
        {
            SetBucket(BucketName);

            var rs = await GetAsync("/", new { prefix, marker, maxKeys });

            var contents = rs?["Contents"];
            if (contents is IList<Object> list) return list;
            return new[] { contents };
        }

        /// <summary>上传文件</summary>
        /// <param name="objectName">对象文件名</param>
        /// <param name="data">数据内容</param>
        /// <returns></returns>
        public async Task PutObject(String objectName, Byte[] data)
        {
            SetBucket(BucketName);

            await InvokeAsync<Object>(HttpMethod.Put, "/" + objectName, data);
        }

        /// <summary>获取文件</summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public async Task<Byte[]> GetObject(String objectName)
        {
            SetBucket(BucketName);

            var rs = await InvokeAsync<Byte[]>(HttpMethod.Get, "/" + objectName);

            return rs;
        }

        /// <summary>删除文件</summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public async Task DeleteObject(String objectName)
        {
            SetBucket(BucketName);

            await InvokeAsync<Object>(HttpMethod.Delete, "/" + objectName);
        }
        #endregion

        #region 辅助
        private const Char NewLineMarker = '\n';

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

        private static String BuildCanonicalString(String method, String resourcePath, HttpRequestMessage request)
        {
            var sb = new StringBuilder();

            sb.Append(method).Append(NewLineMarker);

            var headersToSign = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            var headers = request.Headers.ToDictionary(e => e.Key, e => e.Value?.FirstOrDefault());
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (header.Key.EqualIgnoreCase("Content-Type", "Content-MD5", "Date") ||
                        header.Key.StartsWith("x-oss-"))
                        headersToSign.Add(header.Key, header.Value);
                }
            }

            var contentHeaders = request.Content?.Headers;
            if (!headersToSign.ContainsKey("Content-Type")) headersToSign.Add("Content-Type", contentHeaders?.ContentType + "");
            if (!headersToSign.ContainsKey("Content-MD5")) headersToSign.Add("Content-MD5", contentHeaders?.ContentMD5?.ToHex() + "");

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

#if NET50
            var parameters = request.Options;
#else
            var parameters = request.Properties;
#endif
            if (parameters != null)
            {
                var separator = '?';
                foreach (var item in parameters.OrderBy(e => e.Key))
                {
                    if (!ParamtersToSign.Contains(item.Key)) continue;

                    sb.Append(separator);
                    sb.Append(item.Key);
                    var paramValue = item.Value;
                    if (!String.IsNullOrEmpty(paramValue + ""))
                        sb.Append('=').Append(paramValue);

                    separator = '&';
                }
            }

            return sb.ToString();
        }
        #endregion
    }
}