using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using NewLife.Collections;

namespace NewLife.Web
{
    /// <summary>网页工具类</summary>
    public static class WebHelper
    {
        #region 用户主机
        [ThreadStatic]
        private static String _UserHost;
        /// <summary>用户主机。支持非Web</summary>
        public static String UserHost
        {
            get
            {
#if !__CORE__
                var ctx = HttpContext.Current;
                if (ctx != null)
                {
                    var str = (String)ctx.Items["UserHostAddress"];
                    if (!String.IsNullOrEmpty(str)) return str;

                    var req = ctx.Request;
                    if (req != null)
                    {
                        if (str.IsNullOrEmpty()) str = req.ServerVariables["HTTP_X_FORWARDED_FOR"];
                        if (str.IsNullOrEmpty()) str = req.ServerVariables["X-Real-IP"];
                        if (str.IsNullOrEmpty()) str = req.ServerVariables["X-Forwarded-For"];
                        if (str.IsNullOrEmpty()) str = req.ServerVariables["REMOTE_ADDR"];
                        if (str.IsNullOrEmpty()) str = req.UserHostName;
                        if (str.IsNullOrEmpty()) str = req.UserHostAddress;

                        //// 加上浏览器端口
                        //var port = Request.ServerVariables["REMOTE_PORT"];
                        //if (!port.IsNullOrEmpty()) str += ":" + port;

                        ctx.Items["UserHostAddress"] = str;

                        return str;
                    }
                }
#endif

                return _UserHost;
            }
            set
            {
                _UserHost = value;
#if !__CORE__
                var ctx = HttpContext.Current;
                if (ctx != null) ctx.Items["UserHostAddress"] = value;
#endif
            }
        }
        #endregion

#if !__CORE__
        #region Http请求
        /// <summary>返回请求字符串和表单的名值字段，过滤空值和ViewState，同名时优先表单</summary>
        public static IDictionary<String, String> Params
        {
            get
            {
                var ctx = HttpContext.Current;
                if (ctx.Items["Params"] is IDictionary<String, String> dic) return dic;

                var req = ctx.Request;
                var nvss = new NameValueCollection[] { req.QueryString, req.Form };

                // 这里必须用可空字典，否则直接通过索引查不到数据时会抛出异常
                dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var nvs in nvss)
                {
                    foreach (var item in nvs.AllKeys)
                    {
                        if (item.IsNullOrWhiteSpace()) continue;
                        if (item.StartsWithIgnoreCase("__VIEWSTATE")) continue;

                        // 空值不需要
                        var value = nvs[item];
                        if (value.IsNullOrWhiteSpace())
                        {
                            // 如果请求字符串里面有值而后面表单为空，则抹去
                            if (dic.ContainsKey(item)) dic.Remove(item);
                            continue;
                        }

                        // 同名时优先表单
                        dic[item] = value.Trim();
                    }
                }
                ctx.Items["Params"] = dic;

                return dic;
            }
        }

        /// <summary>获取原始请求Url，支持反向代理</summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static Uri GetRawUrl(this HttpRequest req)
        {
            var uri = req.Url;

            var str = req.RawUrl;
            if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

            str = req.ServerVariables["HTTP_X_REQUEST_URI"];
            if (str.IsNullOrEmpty()) str = req.ServerVariables["X-Request-Uri"];
            if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

            return uri;
        }

        /// <summary>获取原始请求Url，支持反向代理</summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static Uri GetRawUrl(this HttpRequestBase req)
        {
            var uri = req.Url;

            var str = req.RawUrl;
            if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

            str = req.ServerVariables["HTTP_X_REQUEST_URI"];
            if (str.IsNullOrEmpty()) str = req.ServerVariables["X-Request-Uri"];
            if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

            return uri;
        }
        #endregion
#endif

        #region Url扩展
        /// <summary>追加Url参数，不为空时加与符号</summary>
        /// <param name="sb"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static StringBuilder UrlParam(this StringBuilder sb, String str)
        {
            if (str.IsNullOrWhiteSpace()) return sb;

            if (sb.Length > 0)
                sb.Append("&");
            //else
            //    sb.Append("?");

            sb.Append(str);

            return sb;
        }

        /// <summary>追加Url参数，不为空时加与符号</summary>
        /// <param name="sb">字符串构建</param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static StringBuilder UrlParam(this StringBuilder sb, String name, Object value)
        {
            if (name.IsNullOrWhiteSpace()) return sb;

            // 必须注意，value可能是时间类型
            return UrlParam(sb, "{0}={1}".F(name, value));
        }

        /// <summary>把一个参数字典追加Url参数，指定包含的参数</summary>
        /// <param name="sb">字符串构建</param>
        /// <param name="pms">参数字典</param>
        /// <param name="includes">包含的参数</param>
        /// <returns></returns>
        public static StringBuilder UrlParams(this StringBuilder sb, IDictionary<String, String> pms, params String[] includes)
        {
            foreach (var item in pms)
            {
                if (item.Key.EqualIgnoreCase(includes))
                    sb.UrlParam(item.Key, item.Value);
            }
            return sb;
        }

        /// <summary>把一个参数字典追加Url参数，排除一些参数</summary>
        /// <param name="sb">字符串构建</param>
        /// <param name="pms">参数字典</param>
        /// <param name="excludes">要排除的参数</param>
        /// <returns></returns>
        public static StringBuilder UrlParamsExcept(this StringBuilder sb, IDictionary<String, String> pms, params String[] excludes)
        {
            foreach (var item in pms)
            {
                if (!item.Key.EqualIgnoreCase(excludes))
                    sb.UrlParam(item.Key, item.Value);
            }
            return sb;
        }

        /// <summary>相对路径转Uri</summary>
        /// <param name="url">相对路径</param>
        /// <param name="baseUri">基础</param>
        /// <returns></returns>
        public static Uri AsUri(this String url, Uri baseUri = null)
        {
            if (url.IsNullOrEmpty()) return null;

#if !__CORE__
            // 虚拟路径
            if (url.StartsWith("~/")) url = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + url.Substring(2);
#endif

            // 绝对路径
            if (!url.StartsWith("/")) return new Uri(url);

            // 相对路径
            if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
            return new Uri(baseUri, url);
        }

        /// <summary>打包返回地址</summary>
        /// <param name="uri"></param>
        /// <param name="returnUrl"></param>
        /// <param name="returnKey"></param>
        /// <returns></returns>
        public static Uri AppendReturn(this Uri uri, String returnUrl, String returnKey = null)
        {
            if (uri == null || returnUrl.IsNullOrEmpty()) return uri;

            if (returnKey.IsNullOrEmpty()) returnKey = "r";

            // 如果协议和主机相同，则削减为只要路径查询部分
            if (returnUrl.StartsWithIgnoreCase("http"))
            {
                var ruri = new Uri(returnUrl);
                if (ruri.Scheme.EqualIgnoreCase(uri.Scheme) && ruri.Host.EqualIgnoreCase(uri.Host)) returnUrl = ruri.PathAndQuery;
            }
#if !__CORE__
            else if (returnUrl.StartsWith("~/"))
                returnUrl = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + returnUrl.Substring(2);
#endif

            var url = uri + "";
            if (url.Contains("?"))
                url += "&";
            else
                url += "?";
            url += returnKey + "=" + HttpUtility.UrlEncode(returnUrl);

            return new Uri(url);
        }

        /// <summary>打包返回地址</summary>
        /// <param name="url"></param>
        /// <param name="returnUrl"></param>
        /// <param name="returnKey"></param>
        /// <returns></returns>
        public static String AppendReturn(this String url, String returnUrl, String returnKey = null)
        {
            if (url.IsNullOrEmpty() || returnUrl.IsNullOrEmpty()) return url;

            if (returnKey.IsNullOrEmpty()) returnKey = "r";

            // 如果协议和主机相同，则削减为只要路径查询部分
            if (url.StartsWithIgnoreCase("http") && returnUrl.StartsWithIgnoreCase("http"))
            {
                var uri = new Uri(url);
                var ruri = new Uri(returnUrl);
                if (ruri.Scheme.EqualIgnoreCase(uri.Scheme) && ruri.Host.EqualIgnoreCase(uri.Host)) returnUrl = ruri.PathAndQuery;
            }
#if !__CORE__
            else if (returnUrl.StartsWith("~/"))
                returnUrl = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + returnUrl.Substring(2);
#endif

            if (url.Contains("?"))
                url += "&";
            else
                url += "?";
            //url += returnKey + "=" + returnUrl;
            url += returnKey + "=" + HttpUtility.UrlEncode(returnUrl);

            return url;
        }
        #endregion
    }
}