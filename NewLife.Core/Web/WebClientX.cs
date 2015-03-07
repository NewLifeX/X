using System;
using System.Net;

namespace NewLife.Web
{
    /// <summary>扩展的Web客户端</summary>
    public class WebClientX : WebClient
    {
        static WebClientX()
        {
            // 设置默认最大连接为20，关闭默认代理，提高响应速度
            ServicePointManager.DefaultConnectionLimit = 20;
            WebRequest.DefaultWebProxy = null;
        }

        #region 为了Cookie而重写
        private CookieContainer _Cookie;
        /// <summary>Cookie容器</summary>
        public CookieContainer Cookie { get { return _Cookie ?? (_Cookie = new CookieContainer()); } set { _Cookie = value; } }

        #endregion

        #region 属性
        private String _Accept;
        /// <summary>可接受类型</summary>
        public String Accept { get { return _Accept; } set { _Accept = value; } }

        private String _AcceptLanguage;
        /// <summary>可接受语言</summary>
        public String AcceptLanguage { get { return _AcceptLanguage; } set { _AcceptLanguage = value; } }

        private String _Referer;
        /// <summary>引用页面</summary>
        public String Referer { get { return _Referer; } set { _Referer = value; } }

        private Int32 _Timeout;
        /// <summary>超时，毫秒</summary>
        public Int32 Timeout { get { return _Timeout; } set { _Timeout = value; } }

        private DecompressionMethods _AutomaticDecompression;
        /// <summary>自动解压缩模式。</summary>
        public DecompressionMethods AutomaticDecompression { get { return _AutomaticDecompression; } set { _AutomaticDecompression = value; } }

        private String _UserAgent;
        /// <summary>User-Agent 标头，指定有关客户端代理的信息</summary>
        public String UserAgent { get { return _UserAgent; } set { _UserAgent = value; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public WebClientX() { }

        /// <summary>初始化常用的东西</summary>
        /// <param name="ie">是否模拟ie</param>
        /// <param name="iscompress">是否压缩</param>
        public WebClientX(Boolean ie, Boolean iscompress)
        {
            if (ie)
            {
                Accept = "image/jpeg, image/gif, */*";
                AcceptLanguage = "zh-CN";
                //Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; Trident/5.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E)";
            }
            if (iscompress) AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }
        #endregion

        #region 重载设置属性
        /// <summary>重写获取请求</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            var hr = request as HttpWebRequest;
            if (hr != null)
            {
                hr.CookieContainer = Cookie;
                hr.AutomaticDecompression = AutomaticDecompression;

                if (!String.IsNullOrEmpty(Accept)) hr.Accept = Accept;
                if (!String.IsNullOrEmpty(AcceptLanguage)) hr.Headers[HttpRequestHeader.AcceptLanguage] = AcceptLanguage;
                if (!String.IsNullOrEmpty(UserAgent)) hr.UserAgent = UserAgent;
                if (!String.IsNullOrEmpty(Accept)) hr.Accept = Accept;
            }

            if (Timeout > 0) request.Timeout = Timeout;

            return request;
        }

        /// <summary>重写获取响应</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var response = base.GetWebResponse(request);
            var http = response as HttpWebResponse;
            if (http != null)
            {
                Cookie.Add(http.Cookies);
                if (!String.IsNullOrEmpty(http.CharacterSet)) Encoding = System.Text.Encoding.GetEncoding(http.CharacterSet);
            }

            return response;
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
            //if (ResponseHeaders[HttpResponseHeader.ContentType].Contains("utf-8")) enc = System.Text.Encoding.UTF8;

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
        #endregion
    }
}