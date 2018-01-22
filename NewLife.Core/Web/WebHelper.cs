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
        #region 辅助
#if !__CORE__
        /// <summary>输出脚本</summary>
        /// <param name="script"></param>
        public static void WriteScript(String script)
        {
            Js.WriteScript(script, true);
        }
#endif
        #endregion

        #region 弹出信息
#if !__CORE__
        /// <summary>弹出页面提示</summary>
        /// <param name="msg"></param>
        public static void Alert(String msg)
        {
            Js.Alert(msg);
        }

        /// <summary>弹出页面提示并停止输出后退一步！</summary>
        /// <param name="msg"></param>
        public static void AlertAndEnd(String msg)
        {
            Js.Alert(msg).End();
        }

        /// <summary>弹出页面提示，并刷新该页面</summary>
        /// <param name="msg"></param>
        public static void AlertAndRefresh(String msg)
        {
            Js.Alert(msg).Refresh().End();
        }

        /// <summary>弹出页面提示并重定向到另外的页面</summary>
        /// <param name="msg"></param>
        /// <param name="url"></param>
        public static void AlertAndRedirect(String msg, String url)
        {
            Js.Alert(msg).Redirect(url).End();
        }

        /// <summary>弹出页面提示并关闭当前页面</summary>
        /// <param name="msg"></param>
        public static void AlertAndClose(String msg)
        {
            Js.Alert(msg).Close().End();
        }
#endif
        #endregion

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
        #endregion
    }
}