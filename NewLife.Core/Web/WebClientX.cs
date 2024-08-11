using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using NewLife.Http;
using NewLife.Log;
using NewLife.Security;

namespace NewLife.Web;

/// <summary>扩展的Web客户端</summary>
public class WebClientX : DisposeBase
{
    #region 属性
    /// <summary>超时，默认30000毫秒</summary>
    public Int32 Timeout { get; set; } = 30000;

    /// <summary>验证密钥。适配CDN的URL验证，在url后面增加auth_key={timestamp-rand-uid-md5hash}</summary>
    public String? AuthKey { get; set; }

    ///// <summary>验证有效时间。默认1800秒</summary>
    //public TimeSpan AuthExpire { get; set; } = TimeSpan.FromSeconds(1800);

    /// <summary>最后使用的连接名</summary>
    public Link? LastLink { get; set; }
    #endregion

    #region 构造
    static WebClientX()
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }
        catch { }
    }

    /// <summary>实例化</summary>
    public WebClientX() { }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _client.TryDispose();
    }
    #endregion

    #region 核心方法
    private HttpClient? _client;
    private String? _lastAddress;
    private Dictionary<String, String>? _cookies;

    /// <summary>创建客户端会话</summary>
    /// <returns></returns>
    public virtual HttpClient EnsureCreate()
    {
        var http = _client;
        if (http == null)
        {
            http = DefaultTracer.Instance.CreateHttpClient();
            http.Timeout = TimeSpan.FromMilliseconds(Timeout);
            http.SetUserAgent();

            _client = http;
        }

        return http;
    }

    /// <summary>发送请求，获取响应</summary>
    /// <param name="address"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public virtual async Task<HttpContent> SendAsync(String address, HttpContent? content = null)
    {
        var http = EnsureCreate();

        Log.Info("{2}.{1} {0}", address, content != null ? "Post" : "Get", GetType().Name);

        var request = new HttpRequestMessage
        {
            Method = content != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new Uri(address),
            Content = content,
        };

        if (!_lastAddress.IsNullOrEmpty()) request.Headers.Referrer = new Uri(_lastAddress);

        if (_cookies != null && _cookies.Count > 0)
        {
            request.Headers.Add("Cookie", _cookies.Select(e => $"{e.Key}={e.Value}"));
        }

        // 发送请求
        //var task = content != null ? http.PostAsync(address, content) : http.GetAsync(address);
        //var rs = await task;
        var rs = await http.SendAsync(request);

        if (rs.StatusCode < HttpStatusCode.BadRequest)
        {
            // 记录最后一次地址，作为下一次的Referer
            _lastAddress = http.BaseAddress == null ? address : new Uri(http.BaseAddress, address).ToString();

            // 保存Cookie
            if (rs.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var cookie in setCookies)
                {
                    var p1 = cookie.IndexOf('=');
                    if (p1 < 0) continue;
                    var p2 = cookie.IndexOf(';', p1);
                    if (p2 < 0) p2 = cookie.Length;

                    var cs = _cookies ??= [];
                    cs[cookie[..p1]] = cookie.Substring(p1 + 1, p2 - p1 - 1);
                }
            }
        }

        return rs.Content;
    }

    String CheckAuth(String address)
    {
        // 增加CDN的URL验证
        if (!AuthKey.IsNullOrEmpty())
        {
            // http://DomainName/Filename?auth_key={<timestamp>-rand-uid-<md5hash>}
            var uri = new Uri(address);
            var url = uri.PathAndQuery;

            // 如果地址中有中文，需要编码
            var encoding = Encoding.UTF8;
            if (encoding.GetByteCount(url) != url.Length)
            {
                var us = url.Split('/');
                for (var i = 0; i < us.Length; i++)
                {
                    us[i] = HttpUtility.UrlEncode(us[i]);
                }
                url = String.Join("/", us);
            }

            var time = DateTime.UtcNow.ToInt();
            var rand = Rand.Next(100_000, 1_000_000);
            var hash = $"{url}-{time}-{rand}-0-{AuthKey}".MD5().ToLower();
            var key = $"{time}-{rand}-0-{hash}";

            address += address.Contains("?") ? "&" : "?";
            address += "auth_key=" + key;
        }

        return address;
    }

    /// <summary>下载字符串</summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public virtual async Task<String> DownloadStringAsync(String address)
    {
        address = CheckAuth(address);

        var rs = await SendAsync(address);
        return await rs.ReadAsStringAsync();
    }

    /// <summary>下载文件</summary>
    /// <param name="address"></param>
    /// <param name="fileName"></param>
    public virtual async Task DownloadFileAsync(String address, String fileName)
    {
        address = CheckAuth(address);

        var rs = await SendAsync(address);
        fileName.EnsureDirectory(true);
        using var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        await rs.CopyToAsync(fs);
    }
    #endregion

    #region 方法
    /// <summary>获取指定地址的Html，自动处理文本编码</summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public String GetHtml(String url) => Task.Run(() => DownloadStringAsync(url)).Result;

    /// <summary>获取指定地址的Html，分析所有超链接</summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public Link[] GetLinks(String url)
    {
        var html = GetHtml(url);
        if (html.IsNullOrWhiteSpace()) return [];

        return Link.Parse(html, url);
    }

    /// <summary>分析指定页面指定名称的链接，并下载到目标目录，返回目标文件</summary>
    /// <remarks>
    /// 根据版本或时间降序排序选择
    /// </remarks>
    /// <param name="urls">指定页面</param>
    /// <param name="name">页面上指定名称的链接</param>
    /// <param name="destdir">要下载到的目标目录</param>
    /// <returns>返回已下载的文件，无效时返回空</returns>
    public String DownloadLink(String urls, String name, String destdir)
    {
        Log.Info("下载链接 {0}，目标 {1}", urls, name);

        var names = name.Split(",", ";");

        var file = "";
        Link? link = null;
        Exception? lastError = null;
        foreach (var url in urls.Split(",", ";"))
        {
            try
            {
                var ls = GetLinks(url);
                if (ls.Length == 0) continue;

                // 过滤名称后降序排序，多名称时，先确保前面的存在，即使后面名称也存在并且也时间更新都不能用
                //foreach (var item in names)
                //{
                //    link = ls.Where(e => !e.Url.IsNullOrWhiteSpace())
                //       .Where(e => e.Name.EqualIgnoreCase(item) || e.FullName.Equals(item))
                //       .OrderByDescending(e => e.Version)
                //       .ThenByDescending(e => e.Time)
                //       .FirstOrDefault();
                //    if (link != null) break;
                //}
                ls = ls.Where(e => e.Name.EqualIgnoreCase(names) || e.FullName.EqualIgnoreCase(names)).ToArray();
                link = ls.OrderByDescending(e => e.Version).ThenByDescending(e => e.Time).FirstOrDefault();
            }
            catch (WebException ex)
            {
                Log.Error(ex.Message);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
            if (link != null) break;
        }
        if (link == null)
        {
            if (lastError != null) throw lastError;

            return file;
        }

        if (link.Url.IsNullOrEmpty() || link.FullName.IsNullOrEmpty()) throw new InvalidDataException();

        LastLink = link;
        var linkName = link.FullName;
        var file2 = destdir.CombinePath(linkName).EnsureDirectory();

        // 已经提前检查过，这里几乎不可能有文件存在
        if (File.Exists(file2))
        {
            // 如果连接名所表示的文件存在，并且带有时间，那么就只能是它啦
            var p = linkName.LastIndexOf("_");
            if (p > 0 && (p + 8 + 1 == linkName.Length || p + 14 + 1 == linkName.Length))
            {
                Log.Info("分析得到文件：{0}，目标文件已存在，无需下载：{1}", linkName, link.Url);
                return file2;
            }
            // 校验哈希是否一致
            if (!link.Hash.IsNullOrEmpty() && link.Hash.Length == 32)
            {
                var hash = file2.AsFile().MD5().ToHex();
                if (link.Hash.EqualIgnoreCase(hash))
                {
                    Log.Info("分析得到文件：{0}，目标文件已存在，且MD5哈希一致", linkName, link.Url);
                    return file2;
                }
            }
            if (!link.Hash.IsNullOrEmpty() && link.Hash.Length == 128)
            {
                using var fs = file2.AsFile().OpenRead();
                var hash = SHA512.Create().ComputeHash(fs).ToHex();
                if (link.Hash.EqualIgnoreCase(hash))
                {
                    Log.Info("分析得到文件：{0}，目标文件已存在，且SHA512哈希一致", linkName, link.Url);
                    return file2;
                }
            }
        }

        Log.Info("分析得到文件 {0}，准备下载 {1}，保存到 {2}", linkName, link.Url, file2);
        // 开始下载文件，注意要提前建立目录，否则会报错
        file2 = file2.EnsureDirectory();

        var sw = Stopwatch.StartNew();
        Task.Run(() => DownloadFileAsync(link.Url, file2)).Wait(Timeout);
        sw.Stop();

        if (File.Exists(file2))
        {
            Log.Info("下载完成，共{0:n0}字节，耗时{1:n0}毫秒", file2.AsFile().Length, sw.ElapsedMilliseconds);
            file = file2;
        }

        return file;
    }

    /// <summary>分析指定页面指定名称的链接，并下载到目标目录，解压Zip后返回目标文件</summary>
    /// <param name="urls">提供下载地址的多个目标页面</param>
    /// <param name="name">页面上指定名称的链接</param>
    /// <param name="destdir">要下载到的目标目录</param>
    /// <param name="overwrite">是否覆盖目标同名文件</param>
    /// <returns></returns>
    public String? DownloadLinkAndExtract(String urls, String name, String destdir, Boolean overwrite = false)
    {
        var file = "";

        // 下载
        try
        {
            file = DownloadLink(urls, name, destdir);
        }
        catch (Exception ex)
        {
            var err = ex?.GetTrue()?.ToString();
            if (!err.IsNullOrEmpty()) Log.Error(err);

            // 这个时候出现异常，删除zip
            if (!file.IsNullOrEmpty() && File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        if (file.IsNullOrEmpty()) return null;

        // 解压缩
        try
        {
            Log.Info("解压缩到 {0}", destdir);
            file.AsFile().Extract(destdir, overwrite);

            // 删除zip
            File.Delete(file);

            return file;
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }

        return null;
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;
    #endregion
}