using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

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

        /// <summary>过期时间</summary>
        public DateTime Expire { get; private set; }
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
            if (state.IsNullOrEmpty()) state = new Random().Next().ToString();

            _redirect = redirect;
            _state = state;

            var url = GetUrl(AuthUrl);

            HttpContext.Current.Response.Redirect(url);
        }

        /// <summary>根据授权码获取访问令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public String GetAccessToken(String code)
        {
            Code = code;

            var url = GetUrl(AccessUrl);

            var wc = new WebClientX();
            var html = wc.DownloadString(url);
            if (html.IsNullOrEmpty()) return null;

            var dic = html.SplitAsDictionary("=", "&");
            if (dic.ContainsKey("access_token")) AccessToken = dic["access_token"].Trim();
            if (dic.ContainsKey("expires_in")) Expire = DateTime.Now.AddSeconds(dic["expires_in"].Trim().ToInt());
            if (dic.ContainsKey("refresh_token")) RefreshToken = dic["refresh_token"].Trim();

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
        #endregion
    }
}