using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using NewLife.Data;
using NewLife.IO;
using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.Yun;

/// <summary>阿里云文件存储</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/oss
/// </remarks>
public class OssClient : IObjectStorage
{
    #region 属性
    /// <summary>访问域名。Endpoint</summary>
    public String? Server { get; set; } = "http://oss-cn-shanghai.aliyuncs.com";

    /// <summary>访问密钥。AccessKeyId</summary>
    public String? AppId { get; set; }

    /// <summary>访问密钥。AccessKeySecret</summary>
    public String? Secret { get; set; }

    /// <summary>存储空间</summary>
    public String? BucketName { get; set; }

    /// <summary>是否支持获取文件直接访问Url</summary>
    public Boolean CanGetUrl => false;

    /// <summary>是否支持删除</summary>
    public Boolean CanDelete => true;

    /// <summary>是否支持搜索</summary>
    public Boolean CanSearch => true;

    /// <summary>是否支持复制</summary>
    public Boolean CanCopy => true;

    private String? _bucketName;
    private String? _baseAddress;
    private HttpClient? _Client;
    #endregion

    #region 远程操作
    private HttpClient GetClient()
    {
        if (_Client != null) return _Client;

        var addr = _baseAddress ?? Server;
        if (addr.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Server), "OSS service address not specified");

        var http = DefaultTracer.Instance.CreateHttpClient();
        http.BaseAddress = new Uri(addr);

        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var asmName = asm?.GetName();
        if (asmName != null && !asmName.Name.IsNullOrEmpty())
        {
            //var userAgent = $"{asmName.Name}/{asmName.Version}({Environment.OSVersion};{Environment.Version})";
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(asmName.Name, asmName.Version + ""));
        }

        return _Client = http;
    }

    private void SetBucket(String? bucketName)
    {
        var url = Server;
        if (!bucketName.IsNullOrEmpty() && !url.IsNullOrEmpty())
        {
            var ss = url.Split("://");
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
    /// <param name="method">HTTP方法</param>
    /// <param name="action">操作路径</param>
    /// <param name="args">参数</param>
    /// <returns>响应结果</returns>
    protected async Task<TResult?> InvokeAsync<TResult>(HttpMethod method, String action, Object? args = null)
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
        var signature = canonicalString.GetBytes().SHA1(Secret.GetBytes()).ToBase64();
        request.Headers.Authorization = new AuthenticationHeaderValue("OSS", AppId + ":" + signature);

        var http = GetClient();
        var rs = await http.SendAsync(request).ConfigureAwait(false);

        return await ApiHelper.ProcessResponse<TResult>(rs).ConfigureAwait(false);
    }

    private Task<IDictionary<String, Object?>?> GetDictAsync(String action, Object? args = null) => InvokeAsync<IDictionary<String, Object?>>(HttpMethod.Get, action, args);
    #endregion

    #region Bucket操作
    /// <summary>列出所有存储空间名称</summary>
    /// <returns>存储空间名称数组</returns>
    public async Task<String[]?> ListBuckets()
    {
        SetBucket(null);

        var rs = await GetDictAsync("/").ConfigureAwait(false);

        var bs = rs?["Buckets"] as IDictionary<String, Object>;
        var bk = bs?["Bucket"];

        if (bk is IList<Object> list) return list.Select(e => (e as IDictionary<String, Object?>)!["Name"] + "").ToArray();
        if (bk is IDictionary<String, Object> dic) return [dic["Name"] + ""];

        return null;
    }

    /// <summary>列出所有存储空间明细，支持过滤</summary>
    /// <param name="prefix">前缀</param>
    /// <param name="marker">标记</param>
    /// <param name="maxKeys">最大返回数</param>
    /// <returns>存储空间信息列表</returns>
    public async Task<IList<ObjectInfo>?> ListBuckets(String prefix, String marker, Int32 maxKeys = 100)
    {
        SetBucket(null);

        var rs = await GetDictAsync("/", new { prefix, marker, maxKeys }).ConfigureAwait(false);

        var bs = rs?["Buckets"] as IDictionary<String, Object>;
        var bk = bs?["Bucket"] as IList<Object>;
        if (bk is not IList<Object> list) return null;

        var infos = new List<ObjectInfo>();
        foreach (var item in list.Cast<IDictionary<String, Object>>())
        {

        }

        return infos;
    }
    #endregion

    #region Object操作
    /// <summary>列出所有文件名称</summary>
    /// <returns>文件名称数组</returns>
    public async Task<String[]?> ListObjects()
    {
        SetBucket(BucketName);

        var rs = await GetDictAsync("/").ConfigureAwait(false);

        var contents = rs?["Contents"];
        if (contents is IList<Object> list) return list?.Select(e => (e as IDictionary<String, Object?>)!["Key"] + "").ToArray();
        if (contents is IDictionary<String, Object> dic) return [dic["Key"] + ""];

        return null;
    }

    /// <summary>列出所有文件明细，支持过滤</summary>
    /// <param name="prefix">前缀</param>
    /// <param name="marker">标记</param>
    /// <param name="maxKeys">最大返回数</param>
    /// <returns>文件对象信息列表</returns>
    public async Task<IList<ObjectInfo>?> ListObjects(String prefix, String marker, Int32 maxKeys = 100)
    {
        SetBucket(BucketName);

        var rs = await GetDictAsync("/", new { prefix, marker, maxKeys }).ConfigureAwait(false);

        var contents = rs?["Contents"];
        if (contents is not IList<Object> list) return null;

        var infos = new List<ObjectInfo>();
        foreach (var item in list.Cast<IDictionary<String, Object>>())
        {

        }

        return infos;
    }

    /// <summary>上传文件</summary>
    /// <param name="objectName">对象文件名</param>
    /// <param name="data">数据内容</param>
    /// <returns>文件对象信息</returns>
    public async Task<IObjectInfo?> PutAsync(String objectName, IPacket data)
    {
        SetBucket(BucketName);

        var content = data.Next == null && data.TryGetArray(out var segment) ?
            new ByteArrayContent(segment.Array!, segment.Offset, segment.Count) :
            new ByteArrayContent(data.ReadBytes());
        var rs = await InvokeAsync<IPacket>(HttpMethod.Put, "/" + objectName, content).ConfigureAwait(false);

        return new ObjectInfo { Name = objectName, Data = rs, Length = data.Length };
    }

    /// <summary>获取文件</summary>
    /// <param name="objectName">对象文件名</param>
    /// <returns>文件对象信息</returns>
    public async Task<IObjectInfo?> GetAsync(String objectName)
    {
        SetBucket(BucketName);

        var rs = await InvokeAsync<IPacket>(HttpMethod.Get, "/" + objectName).ConfigureAwait(false);

        return new ObjectInfo { Name = objectName, Data = rs, Length = rs?.Length ?? 0 };
    }

    /// <summary>获取文件直接访问Url</summary>
    /// <param name="id">对象文件名</param>
    /// <returns>可直接访问的Url地址</returns>
    public Task<String?> GetUrlAsync(String id) => throw new NotSupportedException();

    /// <summary>检查文件是否存在</summary>
    /// <param name="id">对象文件名</param>
    /// <returns>存在返回true，不存在返回false</returns>
    public async Task<Boolean> ExistsAsync(String id)
    {
        SetBucket(BucketName);

        try
        {
            var rs = await InvokeAsync<Object>(HttpMethod.Head, "/" + id).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>删除文件</summary>
    /// <param name="objectName">对象文件名</param>
    /// <returns>删除成功的数量</returns>
    public async Task<Int32> DeleteAsync(String objectName)
    {
        SetBucket(BucketName);

        var rs = await InvokeAsync<Object>(HttpMethod.Delete, "/" + objectName).ConfigureAwait(false);

        return rs != null ? 1 : 0;
    }

    /// <summary>批量删除文件对象</summary>
    /// <param name="ids">对象文件名列表</param>
    /// <returns>删除成功的数量</returns>
    public async Task<Int32> DeleteAsync(String[] ids)
    {
        if (ids == null || ids.Length == 0) throw new ArgumentNullException(nameof(ids));

        var count = 0;
        foreach (var id in ids)
        {
            count += await DeleteAsync(id).ConfigureAwait(false);
        }
        return count;
    }

    /// <summary>复制文件对象</summary>
    /// <param name="sourceId">源对象文件名</param>
    /// <param name="destId">目标对象文件名</param>
    /// <returns>复制后的文件对象信息</returns>
    public async Task<IObjectInfo?> CopyAsync(String sourceId, String destId)
    {
        SetBucket(BucketName);

        // OSS 复制需要在请求头中指定 x-oss-copy-source
        var request = new HttpRequestMessage(HttpMethod.Put, "/" + destId);
        request.Headers.Add("x-oss-copy-source", $"/{BucketName}/{sourceId}");

        // 资源路径
        var resourcePath = "/" + destId;
        if (!_bucketName.IsNullOrEmpty()) resourcePath = "/" + _bucketName + resourcePath;

        // 时间
        request.Headers.Date = DateTimeOffset.UtcNow;

        // 签名
        var canonicalString = BuildCanonicalString(HttpMethod.Put.Method, resourcePath, request);
        var signature = canonicalString.GetBytes().SHA1(Secret.GetBytes()).ToBase64();
        request.Headers.Authorization = new AuthenticationHeaderValue("OSS", AppId + ":" + signature);

        var http = GetClient();
        var rs = await http.SendAsync(request).ConfigureAwait(false);

        if (!rs.IsSuccessStatusCode) return null;

        return new ObjectInfo { Name = destId };
    }

    /// <summary>搜索文件</summary>
    /// <param name="pattern">匹配模式。如/202304/*.jpg</param>
    /// <param name="start">开始序号。0开始</param>
    /// <param name="count">最大个数</param>
    /// <returns>文件对象信息列表</returns>
    public Task<IList<IObjectInfo>?> SearchAsync(String? pattern, Int32 start, Int32 count) => throw new NotSupportedException();
    #endregion

    #region 兼容旧版
    /// <summary>上传文件</summary>
    /// <param name="objectName">对象文件名</param>
    /// <param name="data">数据内容</param>
    /// <returns>文件对象信息</returns>
    [Obsolete("请使用 PutAsync")]
    public Task<IObjectInfo?> Put(String objectName, IPacket data) => PutAsync(objectName, data);

    /// <summary>获取文件</summary>
    /// <param name="objectName">对象文件名</param>
    /// <returns>文件对象信息</returns>
    [Obsolete("请使用 GetAsync")]
    public Task<IObjectInfo?> Get(String objectName) => GetAsync(objectName);

    /// <summary>获取文件直接访问Url</summary>
    /// <param name="id">对象文件名</param>
    /// <returns>可直接访问的Url地址</returns>
    [Obsolete("请使用 GetUrlAsync")]
    public Task<String?> GetUrl(String id) => GetUrlAsync(id);

    /// <summary>删除文件</summary>
    /// <param name="objectName">对象文件名</param>
    /// <returns>删除成功的数量</returns>
    [Obsolete("请使用 DeleteAsync")]
    public Task<Int32> Delete(String objectName) => DeleteAsync(objectName);

    /// <summary>搜索文件</summary>
    /// <param name="pattern">匹配模式。如/202304/*.jpg</param>
    /// <param name="start">开始序号。0开始</param>
    /// <param name="count">最大个数</param>
    /// <returns>文件对象信息列表</returns>
    [Obsolete("请使用 SearchAsync")]
    public Task<IList<IObjectInfo>?> Search(String? pattern, Int32 start, Int32 count) => SearchAsync(pattern, start, count);
    #endregion

    #region 辅助
    private const Char NewLineMarker = '\n';

    private static readonly IList<String> ParamtersToSign = new List<String> {
        "acl", "uploadId", "partNumber", "uploads", "cors", "logging",
        "website", "delete", "referer", "lifecycle", "security-token","append",
        "position", "x-oss-process", "restore", "bucketInfo", "stat", "symlink",
        "location", "qos", "policy", "tagging", "requestPayment", "x-oss-traffic-limit",
        "objectMeta", "encryption", "versioning", "versionId", "versions",
        "live", "status", "comp", "vod", "startTime", "endTime",
        "inventory","continuation-token","inventoryId",
        "callback", "callback-var","x-oss-request-payer",
        "worm","wormId","wormExtend",
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

        var headersToSign = new Dictionary<String, String?>(StringComparer.OrdinalIgnoreCase);
        var headers = request.Headers;
        if (headers != null)
        {
            foreach (var header in headers)
            {
                if (header.Key.EqualIgnoreCase("Content-Type", "Content-MD5", "Date") ||
                    header.Key.StartsWithIgnoreCase("x-oss-"))
                    headersToSign.Add(header.Key, header.Value?.Join());
            }
        }

        if (!headersToSign.ContainsKey("Content-Type")) headersToSign.Add("Content-Type", "");
        if (!headersToSign.ContainsKey("Content-MD5")) headersToSign.Add("Content-MD5", "");

        var sortedHeaders = headersToSign.Keys.OrderBy(e => e).ToList();
        foreach (var key in sortedHeaders)
        {
            var value = headersToSign[key];
            if (key.StartsWithIgnoreCase("x-oss-"))
                sb.Append(key.ToLowerInvariant()).Append(':').Append(value);
            else
                sb.Append(value);

            sb.Append(NewLineMarker);
        }

        sb.Append(resourcePath);

#if NET5_0_OR_GREATER
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