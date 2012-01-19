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

        /// <summary>重写获取请求</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);

            //if (request is HttpWebRequest) (request as HttpWebRequest).CookieContainer = Cookie;
            var hr = request as HttpWebRequest;
            if (hr != null)
            {
                hr.CookieContainer = Cookie;
                hr.AutomaticDecompression = AutomaticDecompression;
            }

            if (Timeout > 0) request.Timeout = Timeout;

            return request;
        }

        /// <summary>重写获取响应</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);

            if (response is HttpWebResponse) Cookie.Add((response as HttpWebResponse).Cookies);

            return response;
        }
        #endregion

        #region 属性
        ///// <summary>可接受类型</summary>
        //public String Accept
        //{
        //    get { return Headers[HttpRequestHeader.Accept]; }
        //    set { Headers[HttpRequestHeader.Accept] = value; }
        //}

        private Int32 _Timeout;
        /// <summary>超时，毫秒</summary>
        public Int32 Timeout { get { return _Timeout; } set { _Timeout = value; } }

        private DecompressionMethods _AutomaticDecompression;
        /// <summary>自动解压缩模式。</summary>
        public DecompressionMethods AutomaticDecompression { get { return _AutomaticDecompression; } set { _AutomaticDecompression = value; } }
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
                Headers[HttpRequestHeader.Accept] = "image/jpeg, image/gif, */*";
                Headers[HttpRequestHeader.AcceptLanguage] = "zh-CN";
                //Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                Headers[HttpRequestHeader.UserAgent] = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; Trident/5.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E)";
            }
            if (iscompress) AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }
        #endregion
    }
}