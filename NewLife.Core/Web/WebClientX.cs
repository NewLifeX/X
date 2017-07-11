using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;

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

        /// <summary>请求</summary>
        public HttpRequest Request { get; set; } = new HttpRequest();

        /// <summary>响应</summary>
        public HttpResponse Response { get; set; }

        private HttpClient _client;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public WebClientX()
        {
        }

        /// <summary>初始化常用的东西</summary>
        /// <param name="ie">是否模拟ie</param>
        /// <param name="iscompress">是否压缩</param>
        public WebClientX(Boolean ie, Boolean iscompress)
        {
            if (ie)
            {
                Accept = "text/html, */*";
                AcceptLanguage = "zh-CN";
                //Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
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
                UserAgent = "Mozilla/5.0 (compatible; MSIE 11.0; Windows NT 6.1; Trident/7.0; SLCC2; .NET CLR 2.0.50727; .NET4.0C; .NET4.0E; {0})".F(name);
            }
            if (iscompress) AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _client.TryDispose();
        }
        #endregion

        #region 核心方法
        /// <summary>创建客户端会话</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected virtual HttpClient Create(NetUri uri)
        {
            var http = uri.CreateRemote() as HttpClient;
            http.Log = Log;
            //if (XTrace.Debug)
            //{
            //    http.LogSend = true;
            //    http.LogReceive = true;
            //}

            var req = http.Request = Request;
            req.UserAgent = UserAgent;

            if (AutomaticDecompression != DecompressionMethods.None) req.Compressed = true;

            //if (!String.IsNullOrEmpty(Accept)) req.Headers[HttpRequestHeader.Accept + ""] = Accept;
            //if (!String.IsNullOrEmpty(AcceptLanguage)) req.Headers[HttpRequestHeader.AcceptLanguage + ""] = AcceptLanguage;
            //if (!String.IsNullOrEmpty(Referer)) req.Headers[HttpRequestHeader.Referer + ""] = Referer;
            req.Accept = Accept;
            req.AcceptLanguage = AcceptLanguage;
            req.Referer = Referer;

            return http;
        }

        private HttpClient Check(String address)
        {
            var uri = new NetUri(address);

            if (_client == null || _client.Disposed)
                _client = Create(uri);
            // 远程主机不同，需要重新建立
            else if (_client.Remote + "" != uri + "")
            {
                _client.Dispose();
                _client = Create(uri);
            }

            GetCookie();

            var req = _client.Request;
            req.Url = new Uri(address);
            req.Referer = Referer;

            return _client;
        }

        /// <summary>下载数据</summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual Byte[] Get(String address, Byte[] data)
        {
            var time = Timeout;
            if (time <= 0) time = 3000;
            while (true)
            {
                var http = Check(address);
                http.Request.Method = "GET";

                Log.Info("WebClientX.Get {0}", address);

                // 发送请求
                var task = http.SendAsync(data);
                if (!task.Wait(time)) return null;
                Response = http.Response;
                SetCookie();

                var buf = task.Result?.ToArray();
                //var pk = await http.SendAsync(data);
                //var buf = pk.ToArray();

                // 修改引用地址
                Referer = address;

                // 如果是重定向
                switch (http.Response.StatusCode)
                {
                    case HttpStatusCode.MovedPermanently:
                    case HttpStatusCode.Redirect:
                    case HttpStatusCode.RedirectMethod:
                        var url = http.Response[HttpResponseHeader.Location + ""] + "";
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

                // 解压缩
                if (buf != null)
                {
                    var enc = http.Response[HttpResponseHeader.ContentEncoding + ""] + "";
                    if (enc.EqualIgnoreCase("gzip"))
                    {
                        var ms = new MemoryStream(buf);
                        var ms2 = ms.DecompressGZip();
                        ms2.Position = 0;
                        buf = ms2.ReadBytes();
                    }
                    else if (enc.EqualIgnoreCase("deflate"))
                    {
                        buf = buf.Decompress();
                    }
                }

                return buf;
            }
        }

        /// <summary>异步上传数据</summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual async Task<Byte[]> PostAsync(String address, Byte[] data)
        {
            var http = Check(address);
            http.Request.Method = "POST";
            // 表单编码
            if (!http.Request.Headers.ContainsKey("Content-Type")) http.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";

            Log.Info("WebClientX.PostAsync [{0}] {1}", data?.Length, address);

            // 发送请求
            var rs = await http.SendAsync(data).ConfigureAwait(false);
            Response = http.Response;
            SetCookie();

            // 修改引用地址
            Referer = address;

            // 接收数据
            return rs?.ToArray();
        }

        /// <summary>下载数据</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public virtual Byte[] DownloadData(String address)
        {
            return Get(address, new Byte[0]);
        }

        /// <summary>下载字符串</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public virtual String DownloadString(String address)
        {
            var buf = DownloadData(address);
            if (buf == null || buf.Length == 0) return null;

            return buf.ToStr(Encoding);
        }

        /// <summary>下载文件</summary>
        /// <param name="address"></param>
        /// <param name="fileName"></param>
        public virtual void DownloadFile(String address, String fileName)
        {
            var buf = DownloadData(address);
            if (buf?.Length > 0)
            {
                fileName.EnsureDirectory(true);
                File.WriteAllBytes(fileName, buf);
            }
        }

        /// <summary>异步上传数据</summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public virtual async Task<Byte[]> UploadDataTaskAsync(String address, Byte[] data)
        {
            return await PostAsync(address, data).ConfigureAwait(false);
        }
        #endregion

        #region 方法
        /// <summary>获取指定地址的Html，自动处理文本编码</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public String GetHtml(String url)
        {
            var buf = DownloadData(url);
            Referer = url;
            if (buf == null || buf.Length == 0) return null;

            // 处理编码
            var enc = Encoding;
            //if (ResponseHeaders[HttpResponseHeader.ContentType].Contains("utf-8")) enc = Encoding.UTF8;

            return buf.ToStr(enc);
        }

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
        /// <param name="url">指定页面</param>
        /// <param name="name">页面上指定名称的链接</param>
        /// <param name="destdir">要下载到的目标目录</param>
        /// <returns>返回已下载的文件，无效时返回空</returns>
        public String DownloadLink(String url, String name, String destdir)
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
            if (link == null) return file;

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
                file2 = "{0}_{1:yyyyMMddHHmmss}.{2}".F(file2, DateTime.Now, ext);
                file2 = destdir.CombinePath(file2).EnsureDirectory();
            }

            Log.Info("分析得到文件 {0}，准备下载 {1}", link.Name, link.Url);
            // 开始下载文件，注意要提前建立目录，否则会报错
            file2 = file2.EnsureDirectory();

            var sw = new Stopwatch();
            sw.Start();
            DownloadFile(link.Url, file2);
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
        /// <param name="url">指定页面</param>
        /// <param name="name">页面上指定名称的链接</param>
        /// <param name="destdir">要下载到的目标目录</param>
        /// <param name="overwrite">是否覆盖目标同名文件</param>
        /// <returns></returns>
        public String DownloadLinkAndExtract(String url, String name, String destdir, Boolean overwrite = false)
        {
            var file = "";
            var cachedir = Setting.Current.GetPluginCache();

            // 下载
            try
            {
                file = DownloadLink(url, name, cachedir);
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
        /// <summary>根据Http响应设置本地Cookie</summary>
        private void SetCookie()
        {
            // PSTM=1499740028; expires=Thu, 31-Dec-37 23:55:55 GMT; max-age=2147483647; path=/; domain=.baidu.com
            var excludes = new HashSet<String>(new String[] { "expires", "max-age", "path", "domain" }, StringComparer.OrdinalIgnoreCase);

            var cs = Response?.Headers["Set-Cookie"] + "";
            if (cs.IsNullOrEmpty()) return;

            foreach (var item in cs.SplitAsDictionary())
            {
                if (!excludes.Contains(item.Key))
                {
                    Cookie[item.Key] = item.Value;
                }
            }
        }

        /// <summary>从本地获取Cookie并设置到Http请求头</summary>
        private void GetCookie()
        {
            var req = Request;
            if (req == null) return;
            if (Cookie == null || Cookie.Count == 0) return;

            var sb = new StringBuilder();
            foreach (var item in Cookie)
            {
                if (sb.Length > 0) sb.Append(";");
                sb.AppendFormat("{0}={1}", item.Key, item.Value);
            }
            req.Headers["Cookie"] = sb.ToString();
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = XTrace.Debug ? XTrace.Log : Logger.Null;
        #endregion
    }
}