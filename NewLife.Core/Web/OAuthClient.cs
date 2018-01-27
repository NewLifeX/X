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
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>应用Key</summary>
        public String Key { get; set; }

        /// <summary>安全码</summary>
        public String Secret { get; set; }

        /// <summary>验证地址</summary>
        public String AuthUrl { get; set; }

        /// <summary>访问令牌地址</summary>
        public String AccessUrl { get; set; }

        /// <summary>基础地址。用于相对路径生成完整绝对路径</summary>
        /// <remarks>
        /// 为了解决反向代理问题，可调用WebHelper.GetRawUrl取得原始访问地址作为基础地址。
        /// </remarks>
        public String BaseUrl { get; set; }

        /// <summary>重定向地址</summary>
        /// <remarks>
        /// 某些提供商（如百度）会在获取AccessToken时要求传递与前面一致的重定向地址
        /// </remarks>
        public String RedirectUri { get; set; }

        /// <summary>响应类型</summary>
        /// <remarks>
        /// 验证服务器跳转回来子系统时的类型，默认code，此时还需要子系统服务端请求验证服务器换取AccessToken；
        /// 可选token，此时验证服务器直接返回AccessToken，子系统不需要再次请求。
        /// </remarks>
        public String ResponseType { get; set; } = "code";

        /// <summary>作用域</summary>
        public String Scope { get; set; }
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

        #region 方法
        /// <summary>应用参数设置</summary>
        /// <param name="name"></param>
        /// <param name="baseUrl"></param>
        public void Apply(String name, String baseUrl = null)
        {
            var set = OAuthConfig.Current;
            var ms = set.Items;
            if (ms == null || ms.Length == 0) throw new InvalidOperationException("未设置OAuth服务端");

            //var mi = ms.FirstOrDefault(e => e.Enable && (name.IsNullOrEmpty() || e.Name.EqualIgnoreCase(name)));
            var mi = set.GetOrAdd(name);
            if (name.IsNullOrEmpty()) mi = ms.FirstOrDefault(e => e.Enable);
            if (mi == null) throw new InvalidOperationException($"未找到有效的OAuth服务端设置[{name}]");

            name = mi.Name;

            if (set.Debug) Log = XTrace.Log;
            BaseUrl = baseUrl;

            Apply(mi);
        }

        /// <summary>应用参数设置</summary>
        /// <param name="mi"></param>
        public virtual void Apply(OAuthItem mi)
        {
            Name = mi.Name;
            Key = mi.AppID;
            Secret = mi.Secret;
            Scope = mi.Scope;

            switch (mi.Name.ToLower())
            {
                case "qq": SetQQ(); break;
                case "baidu": SetBaidu(); break;
                case "taobao": SetTaobao(); break;
                case "github": SetGithub(); break;
            }
        }
        #endregion

        #region 默认提供者
        /// <summary>设置为QQ专属地址</summary>
        public void SetQQ()
        {
            var url = "https://graph.qq.com/oauth2.0/";
            AuthUrl = url + "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = url + "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";

            var set = OAuthConfig.Current;
            var mi = set.GetOrAdd("QQ");
            mi.Enable = true;
            if (mi.Server.IsNullOrEmpty()) mi.Server = url;

            set.SaveAsync();
        }

        /// <summary>设置百度</summary>
        public void SetBaidu()
        {
            var url = "http://openapi.baidu.com/oauth/2.0/";
            AuthUrl = url + "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = url + "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            UserUrl = "https://openapi.baidu.com/rest/2.0/passport/users/getLoggedInUser?access_token={token}";

            _OnGetUserInfo = dic =>
            {
                if (dic.ContainsKey("uid")) UserID = dic["uid"].Trim().ToLong();
                if (dic.ContainsKey("uname")) UserName = dic["uname"].Trim();

                // small image: http://tb.himg.baidu.com/sys/portraitn/item/{$portrait}
                // large image: http://tb.himg.baidu.com/sys/portrait/item/{$portrait}
                if (dic.ContainsKey("portrait")) Avatar = "http://tb.himg.baidu.com/sys/portrait/item/" + dic["portrait"].Trim();
            };

            var set = OAuthConfig.Current;
            var mi = set.GetOrAdd("Baidu");
            mi.Enable = true;
            if (mi.Server.IsNullOrEmpty()) mi.Server = url;

            set.SaveAsync();
        }

        /// <summary>设置淘宝</summary>
        public void SetTaobao()
        {
            var url = "https://oauth.taobao.com/";
            AuthUrl = url + "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = url + "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            //UserUrl = "https://openapi.baidu.com/rest/2.0/passport/users/getLoggedInUser?access_token={token}";

            _OnGetUserInfo = dic =>
            {
                if (dic.ContainsKey("taobao_user_id")) UserID = dic["taobao_user_id"].Trim('\"').ToLong();
                if (dic.ContainsKey("taobao_user_nick")) UserName = dic["taobao_user_nick"].Trim();
            };

            var set = OAuthConfig.Current;
            var mi = set.GetOrAdd("Taobao");
            mi.Enable = true;
            if (mi.Server.IsNullOrEmpty()) mi.Server = url;

            set.SaveAsync();
        }

        /// <summary>设置Github</summary>
        public void SetGithub()
        {
            var url = "https://github.com/login/oauth/";
            AuthUrl = url + "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = url + "access_token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            UserUrl = "https://api.github.com/user?access_token={token}";

            _OnGetUserInfo = dic =>
            {
                if (dic.ContainsKey("id")) UserID = dic["id"].Trim('\"').ToLong();
                if (dic.ContainsKey("login")) UserName = dic["login"].Trim();
                if (dic.ContainsKey("name")) NickName = dic["name"].Trim();
            };

            var set = OAuthConfig.Current;
            var mi = set.GetOrAdd("Github");
            mi.Enable = true;
            if (mi.Server.IsNullOrEmpty()) mi.Server = url;

            set.SaveAsync();

            // 允许宽松头部
            WebClientX.SetAllowUnsafeHeaderParsing(true);

            if (_Client == null) _Client = new WebClientX(true, true);
        }
        #endregion

        #region 跳转验证
        private String _state;

        /// <summary>跳转验证</summary>
        /// <param name="redirect">验证完成后调整的目标地址</param>
        /// <param name="state">用户状态数据</param>
        public virtual String Authorize(String redirect, String state = null)
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
                var burl = BaseUrl;
                if (burl.IsNullOrEmpty()) throw new ArgumentNullException(nameof(BaseUrl), "使用相对跳转地址时，需要设置BaseUrl");
                // 从Http请求头中取出原始主机名和端口
                //var request = HttpContext.Current.Request;
                //var uri = request.GetRawUrl();

                var uri = new Uri(new Uri(BaseUrl), redirect);
                redirect = uri.ToString();
            }

            //if (redirect.Contains("/")) redirect = HttpUtility.UrlEncode(redirect);
#endif
            RedirectUri = redirect;
            _state = state;

            var url = GetUrl(AuthUrl);
            WriteLog("Authorize {0}", url);

#if !__CORE__
            //HttpContext.Current.Response.Redirect(url);
#endif
            return url;
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

            if (Log != null && Log.Enable) WriteLog(html);

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

                _OnGetUserInfo?.Invoke(dic);
            }
            Items = dic;

            return html;
        }
        #endregion

        #region 用户信息
        /// <summary>用户信息地址</summary>
        public String UserUrl { get; set; }

        /// <summary>用户ID</summary>
        public Int64 UserID { get; set; }

        /// <summary>用户名</summary>
        public String UserName { get; set; }

        /// <summary>昵称</summary>
        public String NickName { get; set; }

        /// <summary>头像</summary>
        public String Avatar { get; set; }

        private Action<IDictionary<String, String>> _OnGetUserInfo;

        /// <summary>获取用户信息</summary>
        /// <returns></returns>
        public virtual async Task<String> GetUserInfo()
        {
            var url = UserUrl;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(UserUrl), "未设置用户信息地址");

            url = GetUrl(url);
            WriteLog("GetUserInfo {0}", url);

            var html = await Request(url);
            if (html.IsNullOrEmpty()) return null;

            html = html.Trim();

            if (Log != null && Log.Enable) WriteLog(html);

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
                if (dic.ContainsKey("uid")) UserID = dic["uid"].Trim().ToLong();
                if (dic.ContainsKey("userid")) UserID = dic["userid"].Trim().ToLong();
                if (dic.ContainsKey("user_id")) UserID = dic["user_id"].Trim().ToLong();
                if (dic.ContainsKey("username")) UserName = dic["username"].Trim();
                if (dic.ContainsKey("user_name")) UserName = dic["user_name"].Trim();
                if (dic.ContainsKey("openid")) OpenID = dic["openid"].Trim();

                _OnGetUserInfo?.Invoke(dic);
            }
            //Items = dic;
            // 合并字典
            foreach (var item in dic)
            {
                Items[item.Key] = item.Value;
            }

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
               .Replace("{response_type}", ResponseType)
               .Replace("{token}", AccessToken)
               .Replace("{code}", Code)
               .Replace("{redirect}", HttpUtility.UrlEncode(RedirectUri + ""))
               .Replace("{scope}", Scope)
               .Replace("{state}", _state);

            return url;
        }

        /// <summary>最后一次请求的响应内容</summary>
        public String LastHtml { get; set; }

        private WebClientX _Client;

        /// <summary>创建客户端</summary>
        /// <param name="url">路径</param>
        /// <returns></returns>
        protected virtual async Task<String> Request(String url)
        {
            if (_Client != null) return LastHtml = await _Client.DownloadStringAsync(url);

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