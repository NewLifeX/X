using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NewLife.Cube.Web
{
    /// <summary>开放验证助手</summary>
    public static class OAuthHelper
    {
        /// <summary>获取登录地址</summary>
        /// <param name="name"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public static String GetLoginUrl(String name, String returnUrl)
        {
            var url = "Sso/Login?name=" + name;
            if (!returnUrl.IsNullOrEmpty()) url += "&returnUrl=" + HttpUtility.UrlEncode(returnUrl);

            url = HttpRuntime.AppDomainAppVirtualPath + url;

            return url;
        }

        /// <summary>合并Url</summary>
        /// <param name="baseUrl"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public static String GetUrl(String baseUrl, String returnUrl = null)
        {
            var url = baseUrl;

            //if (returnUrl.IsNullOrEmpty()) returnUrl = Request["returnUrl"];

            if (!returnUrl.IsNullOrEmpty())
            {
                if (url.Contains("?"))
                    url += "&";
                else
                    url += "?";

                url += "returnUrl=" + HttpUtility.UrlEncode(returnUrl);
            }

            return url;
        }
    }
}