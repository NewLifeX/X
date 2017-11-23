using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>OAuth 2.0</summary>
    public class OAuthClient
    {
        #region 属性
        /// <summary>应用Key</summary>
        public String Key { get; set; }

        /// <summary>安全码</summary>
        public String Secret { get; set; }

        /// <summary>验证地址</summary>
        public String AuthUrl { get; set; }

        /// <summary>访问令牌地址</summary>
        public String AccessUrl { get; set; }
        #endregion

        #region 返回参数
        /// <summary>授权码</summary>
        public String Code { get; private set; }

        /// <summary>访问令牌</summary>
        public String AccessToken { get; private set; }

        /// <summary>刷新令牌</summary>
        public String RefreshToken { get; private set; }

        /// <summary>统一标识</summary>
        public String OpenID { get; private set; }

        /// <summary>过期时间</summary>
        public DateTime Expire { get; private set; }

        /// <summary>访问项</summary>
        public IDictionary<String, String> Items { get; private set; }
        #endregion

        #region QQ专属
        /// <summary>设置为QQ专属地址</summary>
        public void SetQQ()
        {
            var url = "https://graph.qq.com/oauth2.0/";
            AuthUrl = url + "authorize?response_type=code&client_id={key}&state={state}&redirect_uri={redirect}";
            AccessUrl = url + "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
        }
        #endregion

        #region 跳转验证
        private String _redirect;
        private String _state;

        /// <summary>跳转验证</summary>
        /// <param name="redirect">验证完成后调整的目标地址</param>
        /// <param name="state">用户状态数据</param>
        public virtual void Authorize(String redirect, String state = null)
        {
            if (redirect.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect));

            if (Key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Key), "未设置应用标识");
            if (Secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Secret), "未设置应用密钥");

            if (state.IsNullOrEmpty()) state = Rand.Next().ToString();

#if !__CORE__
            // 如果是相对路径，自动加上前缀。需要考虑反向代理的可能，不能直接使用Request.Url
            if (redirect.StartsWith("~/")) redirect = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + redirect.Substring(2);
            if (redirect.StartsWith("/"))
            {
                // 从Http请求头中取出原始主机名和端口
                var req = HttpContext.Current.Request;
                var uri = req.GetRawUrl();

                uri = new Uri(uri, redirect);
                redirect = uri.ToString();
            }

            if (redirect.Contains("/")) redirect = HttpUtility.UrlEncode(redirect);
#endif
            _redirect = redirect;
            _state = state;

            var url = GetUrl(AuthUrl);
            WriteLog("Authorize {0}", url);

#if !__CORE__
            HttpContext.Current.Response.Redirect(url);
#endif
        }

        /// <summary>根据授权码获取访问令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public virtual async Task<String> GetAccessToken(String code)
        {
            if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code), "未设置授权码");

            Code = code;

            var url = GetUrl(AccessUrl);
            WriteLog("GetAccessToken {0}", url);

            var html = await Request(url);
            if (html.IsNullOrEmpty()) return null;

            html = html.Trim();

            IDictionary<String, String> dic = null;
            if (html.StartsWith("{") && html.EndsWith("}"))
            {
                dic = new JsonParser(html).Decode().ToDictionary().ToDictionary(e => e.Key.ToLower(), e => e.Value + "");
            }
            else if (html.Contains("=") && html.Contains("&"))
            {
                dic = html.SplitAsDictionary("=", "&").ToDictionary(e => e.Key.ToLower(), e => e.Value);
            }
            if (dic != null)
            {
                if (dic.ContainsKey("access_token")) AccessToken = dic["access_token"].Trim();
                if (dic.ContainsKey("expires_in")) Expire = DateTime.Now.AddSeconds(dic["expires_in"].Trim().ToInt());
                if (dic.ContainsKey("refresh_token")) RefreshToken = dic["refresh_token"].Trim();
                if (dic.ContainsKey("openid")) OpenID = dic["openid"].Trim();
            }
            Items = dic;

            return html;
        }
        #endregion

        #region 辅助
        /// <summary>替换地址模版参数</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual String GetUrl(String url)
        {
            url = url
               .Replace("{key}", Key)
               .Replace("{secret}", Secret)
               .Replace("{token}", AccessToken)
               .Replace("{code}", Code)
               .Replace("{redirect}", _redirect)
               .Replace("{state}", _state);

            return url;
        }

        /// <summary>最后一次请求的响应内容</summary>
        public String LastHtml { get; set; }

        /// <summary>创建客户端</summary>
        /// <param name="url">路径</param>
        /// <returns></returns>
        protected virtual async Task<String> Request(String url)
        {
            return LastHtml = await WebClientX.GetStringAsync(url);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}