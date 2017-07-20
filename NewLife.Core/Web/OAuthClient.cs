using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if __CORE__
#else
using System.Web;
#endif
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
        /// <param name="redirect"></param>
        /// <param name="state"></param>
        public void Authorize(String redirect, String state = null)
        {
            if (Key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Key));
            if (Secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Secret));
            if (redirect.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect));
            if (state.IsNullOrEmpty()) state = Rand.Next().ToString();
            if (redirect.Contains("/")) redirect = HttpUtility.UrlEncode(redirect);

            _redirect = redirect;
            _state = state;

            var url = GetUrl(AuthUrl);

#if !__CORE__
            HttpContext.Current.Response.Redirect(url);
#endif
        }

        /// <summary>根据授权码获取访问令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public String GetAccessToken(String code)
        {
            Code = code;

            var url = GetUrl(AccessUrl);

            var html = Request(url);
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
        String GetUrl(String url)
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

        private WebClientX _Client;
        /// <summary>创建客户端</summary>
        /// <param name="url">路径</param>
        /// <returns></returns>
        protected virtual String Request(String url)
        {
            if (_Client == null) _Client = new WebClientX();

            return _Client.DownloadString(url);
        }
        #endregion
    }
}