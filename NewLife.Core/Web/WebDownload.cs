using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;

namespace NewLife.Web
{
    /// <summary>提供网页下载支持，在服务端把一个数据流作为附件传给浏览器，带有断点续传和限速的功能</summary>
    public class WebDownload
    {
        #region 属性
        private Stream _Stream;
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        private String _FileName;
        /// <summary>文件名</summary>
        public String FileName { get { return _FileName; } set { _FileName = value; } }

        private String _ContentType = "application/octet-stream";
        /// <summary>内容类型</summary>
        public String ContentType { get { return _ContentType; } set { _ContentType = value; } }

        private DispositionMode _Mode = DispositionMode.None;
        /// <summary>附件配置模式，是在浏览器直接打开，还是提示另存为</summary>
        public DispositionMode Mode { get { return _Mode; } set { _Mode = value; } }

        private Int64 _Speed;
        /// <summary>速度，每秒传输字节数，根据包大小，每响应一个包后睡眠指定毫秒数，0表示不限制</summary>
        public Int64 Speed { get { return _Speed; } set { _Speed = value; } }

        /// <summary>是否启用浏览器缓存 默认禁用</summary>
        public bool BrowserCache { get; set; }

        private TimeSpan _browserCacheMaxAge = new TimeSpan(30, 0, 0, 0);
        /// <summary>浏览器最大缓存时间 默认30天。通过Cache-Control头控制max-age，直接使用浏览器缓存，不会发出Http请求，对F5无效</summary>
        public TimeSpan BrowserCacheMaxAge { get { return _browserCacheMaxAge; } set { _browserCacheMaxAge = value; } }

        private DateTime _ModifyTime;
        /// <summary>文件数据最后修改时间，浏览器缓存时用</summary>
        public DateTime ModifyTime { get { return _ModifyTime; } set { _ModifyTime = value; } }
        #endregion

        #region 枚举
        /// <summary>附件配置模式</summary>
        public enum DispositionMode
        {
            /// <summary>不设置</summary>
            None,

            /// <summary>内联模式，在浏览器直接打开</summary>
            Inline,

            /// <summary>附件模式，提示另存为</summary>
            Attachment
        }

        /// <summary>分析模式</summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static DispositionMode ParseMode(String mode)
        {
            return (DispositionMode)Enum.Parse(typeof(DispositionMode), mode);
        }
        #endregion

        #region 构造
        /// <summary>构造函数</summary>
        public WebDownload() { }

        /// <summary>构造函数</summary>
        /// <param name="stream"></param>
        public WebDownload(Stream stream) { Stream = stream; }

        /// <summary>构造函数</summary>
        /// <param name="html"></param>
        /// <param name="encoding"></param>
        public WebDownload(String html, Encoding encoding)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms, encoding);
            writer.Write(html);
            ms.Position = 0;

            Stream = ms;
        }
        #endregion

        /// <summary>检查浏览器缓存是否依然有效，如果有效则跳过Render</summary>
        /// <returns></returns>
        public virtual Boolean CheckCache()
        {
            //增加 浏览器缓存 304缓存
            //if (BrowserCache)
            {
                var Request = HttpContext.Current.Request;
                var Response = HttpContext.Current.Response;

                var since = Request.ServerVariables["HTTP_IF_MODIFIED_SINCE"];
                if (!String.IsNullOrEmpty(since))
                {
                    DateTime dt;
                    //if (DateTime.TryParse(since, out dt) && dt >= attachment.UploadTime)
                    //!!! 注意：本地修改时间精确到毫秒，而HTTP_IF_MODIFIED_SINCE只能到秒
                    if (DateTime.TryParse(since, out dt) && (dt - ModifyTime).TotalSeconds > -1)
                    {
                        Response.StatusCode = 304;
                        return true;
                    }
                }
                // WebDev不支持HTTP_IF_MODIFIED_SINCE，但是可以用HTTP_IF_NONE_MATCH
                var etag = Request.ServerVariables["HTTP_IF_NONE_MATCH"];
                if (!String.IsNullOrEmpty(etag))
                {
                    Int64 ticks = 0;
                    if (Int64.TryParse(etag, out ticks) && (new DateTime(ticks) - ModifyTime).TotalSeconds > -1)
                    {
                        Response.StatusCode = 304;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>输出数据流</summary>
        public virtual void Render()
        {
            var Request = HttpContext.Current.Request;
            var Response = HttpContext.Current.Response;

            var stream = Stream;

            // 速度
            long speed = Speed;
            // 包大小
            int pack = 1024000;
            // 计算睡眠时间
            int sleep = speed > 0 ? (int)Math.Floor(1000 * (double)pack / speed) + 1 : 0;

            // 输出Accept-Ranges，表示支持断点
            Response.AddHeader("Accept-Ranges", "bytes");
            Response.Buffer = false;
            long fileLength = stream.Length;
            long startBytes = 0;

            // 如果请求里面指定范围，表示需要断点
            if (Request.Headers["Range"] != null)
            {
                Response.StatusCode = 206;
                string[] range = Request.Headers["Range"].Split(new char[] { '=', '-' });
                startBytes = Convert.ToInt64(range[1]);
            }
            // 计算真正的长度
            Response.AddHeader("Content-Length", (fileLength - startBytes).ToString());
            if (startBytes != 0)
            {
                // 指定数据范围
                Response.AddHeader("Content-Range", string.Format(" bytes {0}-{1}/{2}", startBytes, fileLength - 1, fileLength));
            }
            Response.AddHeader("Connection", "Keep-Alive");
            Response.ContentType = ContentType;
            if (Mode != DispositionMode.None)
                Response.AddHeader("Content-Disposition", Mode + ";filename=" + HttpUtility.UrlEncode(FileName, Encoding.UTF8));

            //增加 浏览器缓存 304缓存
            if (BrowserCache)
            {
                //Response.ExpiresAbsolute = DateTime.Now.Add(BrowserCacheMaxAge);
                //Response.Cache.SetMaxAge(BrowserCacheMaxAge);
                //Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
                //Response.Cache.SetCacheability(HttpCacheability.Public);
                //Response.Cache.SetLastModified(ModifyTime);

                Response.Cache.SetETag(ModifyTime.Ticks.ToString());
                Response.Cache.SetLastModified(ModifyTime);
                Response.Cache.SetCacheability(HttpCacheability.Public);
                Response.Cache.SetMaxAge(BrowserCacheMaxAge);
                Response.Cache.SetSlidingExpiration(true);
            }

            //stream.Seek(startBytes, SeekOrigin.Begin);
            int maxCount = (int)Math.Floor((fileLength - startBytes) / (double)pack) + 1;
            // 如果不足一个包，则缩小缓冲区，避免浪费内存
            if (pack > stream.Length) pack = (Int32)stream.Length;
            var buffer = new Byte[pack];
            for (int i = 0; i < maxCount; i++)
            {
                if (!Response.IsClientConnected) break;

                Int32 count = stream.Read(buffer, 0, buffer.Length);
                if (count == pack)
                    Response.BinaryWrite(buffer);
                else
                {
                    var data = new Byte[count];
                    Buffer.BlockCopy(buffer, 0, data, 0, count);
                    Response.BinaryWrite(data);
                }
                if (sleep > 0) Thread.Sleep(sleep);
            }
        }
    }
}