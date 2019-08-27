using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Log;
#if !NET4
using System.Net.Http;
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Web
{
    /// <summary>扩展的Web客户端</summary>
    public class WebClientX : DisposeBase
    {
        #region 属性
        /// <summary>超时，默认15000毫秒</summary>
        public Int32 Timeout { get; set; } = 15000;
        #endregion

        #region 构造
        static WebClientX()
        {
#if NET4
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
            }
            catch
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls;
            }
#else
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            }
            catch
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            }
#endif
        }

        /// <summary>实例化</summary>
        public WebClientX() { }

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
        /// <summary>创建客户端会话</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected virtual HttpWebRequest Create(String uri)
        {
            var req = WebRequest.Create(uri) as HttpWebRequest;

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
        public virtual async Task<String> DownloadStringAsync(String address) => (await SendAsync(address)).ToStr();

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

        /// <summary>创建客户端会话</summary>
        /// <returns></returns>
        public virtual HttpClient EnsureCreate()
        {
            var http = _client;
            if (http == null)
            {
                var handler = new HttpClientHandler();
                http = new HttpClient(handler);

                _client = http;
                http.Timeout = new TimeSpan(0, 0, 0, 0, Timeout);
            }

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

            return rs.Content;
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
            Log.Info("下载链接 {0}，目标 {1}", urls, name);

            var names = name.Split(",", ";");

            var file = "";
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
                           .Where(e => e.Name.EqualIgnoreCase(item))
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

            var linkName = link.FullName;
            var file2 = destdir.CombinePath(linkName).EnsureDirectory();

            // 已经提前检查过，这里几乎不可能有文件存在
            if (File.Exists(file2))
            {
                // 如果连接名所表示的文件存在，并且带有时间，那么就智能是它啦
                var p = linkName.LastIndexOf("_");
                if (p > 0 && (p + 8 + 1 == linkName.Length || p + 14 + 1 == linkName.Length))
                {
                    Log.Info("分析得到文件 {0}，目标文件已存在，无需下载 {1}", linkName, link.Url);
                    return file;
                }
            }

            Log.Info("分析得到文件 {0}，准备下载 {1}，保存到 {2}", linkName, link.Url, file2);
            // 开始下载文件，注意要提前建立目录，否则会报错
            file2 = file2.EnsureDirectory();

            var sw = Stopwatch.StartNew();
            TaskEx.Run(() => DownloadFileAsync(link.Url, file2)).Wait();
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
        public String DownloadLinkAndExtract(String urls, String name, String destdir, Boolean overwrite = false)
        {
            var file = "";

            // 下载
            try
            {
                file = DownloadLink(urls, name, destdir);
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

            if (file.IsNullOrEmpty()) return null;

            // 解压缩
            try
            {
                Log.Info("解压缩到 {0}", destdir);
                //ZipFile.ExtractToDirectory(file, destdir);
                file.AsFile().Extract(destdir, overwrite);

                // 删除zip
                File.Delete(file);

                return file;
            }
            catch (Exception ex)
            {
                Log.Error(ex?.GetTrue()?.ToString());

                //// 这个时候出现异常，删除zip
                //if (!file.IsNullOrEmpty() && File.Exists(file))
                //{
                //    try
                //    {
                //        File.Delete(file);
                //    }
                //    catch { }
                //}
            }

            return null;
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