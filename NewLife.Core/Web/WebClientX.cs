using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Net;
using NewLife.Serialization;
using NewLife.Threading;
#if NET4
#else
using System.Net.Http;
using System.Net.Http.Headers;
//using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Web
{
    /// <summary>扩展的Web客户端</summary>
    public class WebClientX : DisposeBase
    {
        #region 属性
        /// <summary>Cookie容器</summary>
        public IDictionary<String, String> Cookie { get; set; } = new NullableDictionary<String, String>();

        /// <summary>可接受类型</summary>
        public String Accept { get; set; }

        /// <summary>可接受语言</summary>
        public String AcceptLanguage { get; set; }

        /// <summary>引用页面</summary>
        public String Referer { get; set; }

        /// <summary>超时，默认15000毫秒</summary>
        public Int32 Timeout { get; set; } = 15000;

        /// <summary>自动解压缩模式。</summary>
        public DecompressionMethods AutomaticDecompression { get; set; }

        /// <summary>User-Agent 标头，指定有关客户端代理的信息</summary>
        public String UserAgent { get; set; }

        /// <summary>编码。网络时代，绝大部分使用utf8编码</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>保持连接</summary>
        public Boolean KeepAlive { get; set; }

        /// <summary>代理服务器地址</summary>
        public String ProxyAddress { get; set; }

        /// <summary>网页代理</summary>
        public IWebProxy Proxy { get; set; }
        #endregion

        #region 构造
        static WebClientX()
        {
#if NET4
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
#elif __CORE__
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#else
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif
        }

        /// <summary>实例化</summary>
        public WebClientX() { }

        /// <summary>初始化常用的东西</summary>
        /// <param name="ie">是否模拟ie</param>
        /// <param name="iscompress">是否压缩</param>
        public WebClientX(Boolean ie, Boolean iscompress) : this()
        {
            if (ie)
            {
                Accept = "text/html, */*";
                AcceptLanguage = "zh-CN";
                var name = "";
                var asm = Assembly.GetEntryAssembly();
                if (asm != null) name = asm.GetName().Name;
                if (String.IsNullOrEmpty(name))
                {
                    try
                    {
                        name = Process.GetCurrentProcess().ProcessName;
                    }
                    catch { }
                }
                UserAgent = "Mozilla/5.0 (compatible; MSIE 11.0; Windows NT 10.0; {0})".F(name);
            }
            if (iscompress) AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

#if !NET4
            _client.TryDispose();
#endif
        }
        #endregion

        #region 核心方法
#if NET4
        /// <summary>请求</summary>
        public HttpWebRequest Request { get; private set; }

        /// <summary>响应</summary>
        public HttpWebResponse Response { get; private set; }

        /// <summary>创建客户端会话</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected virtual HttpWebRequest Create(String uri)
        {
            var req = WebRequest.Create(uri) as HttpWebRequest;
            req.UserAgent = UserAgent;

            if (AutomaticDecompression != DecompressionMethods.None) req.AutomaticDecompression = AutomaticDecompression;

            if (!Accept.IsNullOrEmpty()) req.Accept = Accept;
            if (!AcceptLanguage.IsNullOrEmpty()) req.Headers[HttpRequestHeader.AcceptLanguage + ""] = AcceptLanguage;
            if (!Referer.IsNullOrEmpty()) req.Referer = Referer;

            return req;
        }

        /// <summary>下载数据</summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual async Task<Byte[]> SendAsync(String address, Byte[] data = null)
        {
            var time = Timeout;
            if (time <= 0) time = 3000;
            while (true)
            {
                var http = Create(address);
                http.Timeout = time;
                http.Method = data == null || data.Length == 0 ? "GET" : "POST";

                Log.Info("WebClientX.SendAsync {0}", address);

                // 发送请求
                var rs = (await Task.Factory.FromAsync(http.BeginGetResponse, http.EndGetResponse, null)) as HttpWebResponse;

                // 修改引用地址
                Referer = address;

                // 如果是重定向
                switch (rs.StatusCode)
                {
                    case HttpStatusCode.MovedPermanently:
                    case HttpStatusCode.Redirect:
                    case HttpStatusCode.RedirectMethod:
                        var url = rs.Headers[HttpResponseHeader.Location + ""] + "";
                        if (!url.IsNullOrEmpty())
                        {
                            address = url;
                            data = null;
                            continue;
                        }
                        break;
                    default:
                        break;
                }

                var ms = NewLife.Collections.Pool.MemoryStream.Get();
                var ns = rs.GetResponseStream();

                ns.CopyTo(ms);
                while (rs.ContentLength > 0 && ms.Length < rs.ContentLength)
                {
                    Thread.Sleep(10);
                    ns.CopyTo(ms);
                }

                return ms.Put(true);
            }
        }

        /// <summary>下载数据</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public virtual async Task<Byte[]> DownloadDataAsync(String address) => await SendAsync(address);

        /// <summary>下载字符串</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public virtual async Task<String> DownloadStringAsync(String address) => (await SendAsync(address)).ToStr(Encoding);

        /// <summary>下载文件</summary>
        /// <param name="address"></param>
        /// <param name="fileName"></param>
        public virtual async Task DownloadFileAsync(String address, String fileName)
        {
            var rs = await SendAsync(address);
            fileName.EnsureDirectory(true);
            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                await fs.WriteAsync(rs, 0, rs.Length);
            }
        }
#else
        private HttpClient _client;

        /// <summary>请求</summary>
        public HttpRequestHeaders Request { get; private set; }

        /// <summary>响应</summary>
        public HttpResponseMessage Response { get; private set; }

        /// <summary>创建客户端会话</summary>
        /// <returns></returns>
        public virtual HttpClient EnsureCreate()
        {
            var http = _client;
            if (http == null)
            {
                var p = Proxy;
                if (p == null && !ProxyAddress.IsNullOrEmpty()) Proxy = p = new WebProxy(ProxyAddress);

                var handler = new HttpClientHandler();
                if (p != null)
                {
                    handler.UseProxy = true;
                    handler.Proxy = p;
                }
                else
                {
                    handler.UseProxy = false;
                    handler.Proxy = null;
                }
                if (AutomaticDecompression != DecompressionMethods.None) handler.AutomaticDecompression = AutomaticDecompression;

                http = new HttpClient(handler);

                _client = http;
                Request = http.DefaultRequestHeaders;
                http.Timeout = new TimeSpan(0, 0, 0, 0, Timeout);
            }

            var req = http.DefaultRequestHeaders;
            if (!UserAgent.IsNullOrEmpty()) req.UserAgent.ParseAdd(UserAgent);
            if (!Accept.IsNullOrEmpty()) req.Accept.ParseAdd(Accept);
            if (!AcceptLanguage.IsNullOrEmpty()) req.AcceptLanguage.ParseAdd(AcceptLanguage);
            if (AutomaticDecompression != DecompressionMethods.None) req.AcceptEncoding.ParseAdd("gzip, deflate");
            if (!Referer.IsNullOrEmpty()) req.Referrer = new Uri(Referer);
            if (KeepAlive) req.Connection.ParseAdd("keep-alive");

            GetCookie(http);

            return http;
        }

        /// <summary>发送请求，获取响应</summary>
        /// <param name="address"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public virtual async Task<HttpContent> SendAsync(String address, HttpContent content = null)
        {
            var time = Timeout;
            if (time <= 0) time = 3000;

            var http = EnsureCreate();

            Log.Info("{2}.{1} {0}", address, content != null ? "Post" : "Get", GetType().Name);

            // 发送请求
            var source = new CancellationTokenSource(time);
            var task = content != null ? http.PostAsync(address, content, source.Token) : http.GetAsync(address, source.Token);
            var rs = await task;
            //Response = rs.EnsureSuccessStatusCode();
            Response = rs;
            SetCookie();

            // 修改引用地址
            Referer = address;

            return rs.Content;
        }

        /// <summary>下载数据</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public virtual async Task<Byte[]> DownloadDataAsync(String address)
        {
            var rs = await SendAsync(address);
            return await rs.ReadAsByteArrayAsync();
        }

        /// <summary>下载字符串</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public virtual async Task<String> DownloadStringAsync(String address)
        {
            var rs = await SendAsync(address);
            return await rs.ReadAsStringAsync();
        }

        /// <summary>下载文件</summary>
        /// <param name="address"></param>
        /// <param name="fileName"></param>
        public virtual async Task DownloadFileAsync(String address, String fileName)
        {
            var rs = await SendAsync(address);
            fileName.EnsureDirectory(true);
            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                await rs.CopyToAsync(fs);
            }
        }

        /// <summary>异步上传数据</summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public virtual async Task<Byte[]> UploadDataTaskAsync(String address, Byte[] data)
        {
            var ctx = new ByteArrayContent(data);
            var rs = await SendAsync(address, ctx);
            return await rs.ReadAsByteArrayAsync();
        }

        /// <summary>异步上传表单</summary>
        /// <param name="address"></param>
        /// <param name="collection"></param>
        public virtual async Task<String> UploadValuesAsync(String address, IEnumerable<KeyValuePair<String, String>> collection)
        {
            var ctx = new FormUrlEncodedContent(collection);
            var rs = await SendAsync(address, ctx);
            return await rs.ReadAsStringAsync();
        }

        /// <summary>异步上传字符串</summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public virtual async Task<String> UploadStringAsync(String address, String data)
        {
            var ctx = new StringContent(data, Encoding, "application/x-www-form-urlencoded");

            var rs = await SendAsync(address, ctx);
            return await rs.ReadAsStringAsync();
        }

        /// <summary>异步上传Json对象</summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public virtual async Task<String> UploadJsonAsync(String address, Object data)
        {
            if (!(data is String str)) str = data.ToJson();

            var ctx = new StringContent(str, Encoding, "application/json");

            var rs = await SendAsync(address, ctx);
            return await rs.ReadAsStringAsync();
        }
#endif
        #endregion

        #region 方法
        /// <summary>获取指定地址的Html，自动处理文本编码</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public String GetHtml(String url) => TaskEx.Run(() => DownloadStringAsync(url)).Result;

        /// <summary>获取指定地址的Html，分析所有超链接</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public Link[] GetLinks(String url)
        {
            var html = GetHtml(url);
            if (html.IsNullOrWhiteSpace()) return new Link[0];

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
            // 一定时间之内，不再重复下载
            var cacheTime = DateTime.Now.AddDays(-1);
            var cachedir = Setting.Current.GetPluginCache();
            var names = name.Split(",", ";");

            var file = "";
            foreach (var item in names)
            {
                // 猜测本地可能存在的文件
                var fi = CheckCache(item, destdir);
                if (fi != null && fi.LastWriteTime > cacheTime) return fi.FullName;
                // 检查缓存目录
                if (!destdir.EqualIgnoreCase(cachedir))
                {
                    fi = CheckCache(item, cachedir) ?? fi;
                    if (fi != null && fi.LastWriteTime > cacheTime) return fi.FullName;
                }

                // 确保即使联网下载失败，也返回较旧版本
                if (fi != null) file = fi.FullName;
            }

            // 确保即使联网下载失败，也返回较旧版本
            Link link = null;
            Exception lastError = null;
            foreach (var url in urls.Split(",", ";"))
            {
                try
                {
                    var ls = GetLinks(url);
                    if (ls.Length == 0) return file;

                    // 过滤名称后降序排序，多名称时，先确保前面的存在，即使后面名称也存在并且也时间更新都不能用
                    foreach (var item in names)
                    {
                        link = ls.Where(e => !e.Url.IsNullOrWhiteSpace())
                           .Where(e => e.Name.EqualIgnoreCase(item) || e.Name.StartsWithIgnoreCase(item + ".") || e.Name.StartsWithIgnoreCase(item + "_"))
                           .OrderByDescending(e => e.Version)
                           .OrderByDescending(e => e.Time)
                           .FirstOrDefault();
                        if (link != null) break;
                    }
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

            var file2 = destdir.CombinePath(link.Name).EnsureDirectory();

            // 已经提前检查过，这里几乎不可能有文件存在
            if (File.Exists(file2))
            {
                // 如果连接名所表示的文件存在，并且带有时间，那么就智能是它啦
                var p = link.Name.LastIndexOf("_");
                if (p > 0 && (p + 8 + 1 == link.Name.Length || p + 14 + 1 == link.Name.Length))
                {
                    Log.Info("分析得到文件 {0}，目标文件已存在，无需下载 {1}", link.Name, link.Url);
                    return file;
                }

                // 如果文件存在，另外改一个名字吧
                var ext = Path.GetExtension(link.Name);
                file2 = Path.GetFileNameWithoutExtension(link.Name);
                file2 = "{0}_{1:yyyyMMddHHmmss}{2}".F(file2, DateTime.Now, ext);
                file2 = destdir.CombinePath(file2).EnsureDirectory();
            }

            Log.Info("分析得到文件 {0}，准备下载 {1}", link.Name, link.Url);
            // 开始下载文件，注意要提前建立目录，否则会报错
            file2 = file2.EnsureDirectory();

            var sw = Stopwatch.StartNew();
            TaskEx.Run(() => DownloadFileAsync(link.Url, file2)).Wait();
            //ThreadPoolX.QueueUserWorkItem(() => DownloadFileAsync(link.Url, file2).Wait());
            //ThreadPoolX.QueueTask(() => DownloadFileAsync(link.Url, file2)).Wait();
            sw.Stop();

            if (File.Exists(file2))
            {
                Log.Info("下载完成，共{0:n0}字节，耗时{1:n0}毫秒", file2.AsFile().Length, sw.ElapsedMilliseconds);
                // 缓存文件
                if (!destdir.EqualIgnoreCase(cachedir))
                {
                    var cachefile = cachedir.CombinePath(link.Name);
                    Log.Info("缓存到 {0}", cachefile);
                    try
                    {
                        File.Copy(file2, cachefile.EnsureDirectory(), true);
                    }
                    catch { }
                }
                file = file2;
            }

            return file;
        }

        FileInfo CheckCache(String name, String dir)
        {
            var di = dir.AsDirectory();
            if (di != null && di.Exists)
            {
                var fi = di.GetFiles(name + ".*").FirstOrDefault();
                if (fi == null || !fi.Exists) fi = di.GetFiles(name + "_*.*").FirstOrDefault();
                if (fi != null && fi.Exists)
                {
                    Log.Info("目标文件{0}已存在，更新于{1}", fi.FullName, fi.LastWriteTime);
                    return fi;
                }
            }

            return null;
        }

        /// <summary>分析指定页面指定名称的链接，并下载到目标目录，解压Zip后返回目标文件</summary>
        /// <param name="urls">提供下载地址的多个目标页面</param>
        /// <param name="name">页面上指定名称的链接</param>
        /// <param name="destdir">要下载到的目标目录</param>
        /// <param name="overwrite">是否覆盖目标同名文件</param>
        /// <returns></returns>
        public String DownloadLinkAndExtract(String urls, String name, String destdir, Boolean overwrite = false)
        {
            var file = "";
            var cachedir = Setting.Current.GetPluginCache();

            // 下载
            try
            {
                file = DownloadLink(urls, name, cachedir);
            }
            catch (Exception ex)
            {
                Log.Error(ex?.GetTrue()?.ToString());

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

            // 如果下载失败，尝试缓存
            if (file.IsNullOrEmpty())
            {
                try
                {
                    var fi = CheckCache(name, cachedir);
                    file = fi?.FullName;
                }
                catch { }
            }
            if (file.IsNullOrEmpty()) return null;

            // 解压缩
            try
            {
                Log.Info("解压缩到 {0}", destdir);
                //ZipFile.ExtractToDirectory(file, destdir);
                file.AsFile().Extract(destdir, overwrite);

                return file;
            }
            catch (Exception ex)
            {
                Log.Error(ex?.GetTrue()?.ToString());

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

            return null;
        }
        #endregion

        #region Cookie处理
#if !NET4
        /// <summary>根据Http响应设置本地Cookie</summary>
        private void SetCookie()
        {
            var rs = Response;
            if (rs == null) return;

            // PSTM=1499740028; expires=Thu, 31-Dec-37 23:55:55 GMT; max-age=2147483647; path=/; domain=.baidu.com
            var excludes = new HashSet<String>(new String[] { "expires", "max-age", "path", "domain" }, StringComparer.OrdinalIgnoreCase);

            if (!rs.Headers.TryGetValues("Set-Cookie", out var cs) || !cs.Any()) return;

            foreach (var item in cs.FirstOrDefault().SplitAsDictionary())
            {
                if (!excludes.Contains(item.Key))
                {
                    Cookie[item.Key] = item.Value;
                }
            }
        }

        /// <summary>从本地获取Cookie并设置到Http请求头</summary>
        private void GetCookie(HttpClient http)
        {
            var req = http.DefaultRequestHeaders;
            if (req == null) return;

            if (Cookie == null || Cookie.Count == 0) return;

            var sb = new StringBuilder();
            foreach (var item in Cookie)
            {
                if (sb.Length > 0) sb.Append(";");
                sb.AppendFormat("{0}={1}", item.Key, item.Value);
            }
            req.Add("Cookie", sb.ToString());
        }
#endif
        #endregion

        #region 连接池
        private static readonly Object SyncRoot = new Object();
        private static WebClientPool _Pool;
        /// <summary>默认连接池</summary>
        public static IPool<WebClientX> Pool
        {
            get
            {
                if (_Pool != null) return _Pool;
                lock (SyncRoot)
                {
                    if (_Pool != null) return _Pool;

                    var pool = new WebClientPool
                    {
                        Name = "WebClientPool",
                        Min = 2,
                        AllIdleTime = 60
                    };

                    return _Pool = pool;
                }
            }
        }

        /// <summary>访问地址获取字符串</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static async Task<String> GetStringAsync(String address)
        {
            var client = Pool.Get();
            try
            {
                return await client.DownloadStringAsync(address);
            }
            finally
            {
                Pool.Put(client);
            }
        }

        class WebClientPool : ObjectPool<WebClientX>
        {
            protected override WebClientX OnCreate() => new WebClientX();
        }
        #endregion

        #region 辅助
        private static Boolean? _useUnsafeHeaderParsing;
        /// <summary>设置是否允许不安全头部</summary>
        /// <remarks>
        /// 微软WebClient默认要求严格的Http头部，否则报错
        /// </remarks>
        /// <param name="useUnsafe"></param>
        /// <returns></returns>
        public static Boolean SetAllowUnsafeHeaderParsing(Boolean useUnsafe)
        {
            if (_useUnsafeHeaderParsing != null && _useUnsafeHeaderParsing.Value == useUnsafe) return true;

#if __CORE__
            _useUnsafeHeaderParsing = true;
#else
            //Get the assembly that contains the internal class
            var aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
            if (aNetAssembly == null) return false;

            //Use the assembly in order to get the internal type for the internal class
            var type = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
            if (type == null) return false;

            //Use the internal static property to get an instance of the internal settings class.
            //If the static instance isn't created allready the property will create it for us.
            var section = type.InvokeMember("Section", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new Object[] { });

            if (section != null)
            {
                //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                var useUnsafeHeaderParsing = type.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                if (useUnsafeHeaderParsing != null)
                {
                    useUnsafeHeaderParsing.SetValue(section, useUnsafe);
                    _useUnsafeHeaderParsing = useUnsafe;
                    return true;
                }
            }
#endif

            return false;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}