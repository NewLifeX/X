using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>OAuth 2.0 客户端</summary>
    public class OAuthClient
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>验证服务器地址</summary>
        public String Server { get; set; }

        /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
        public String AccessServer { get; set; }

        /// <summary>应用Key</summary>
        public String Key { get; set; }

        /// <summary>安全码</summary>
        public String Secret { get; set; }

        /// <summary>验证地址</summary>
        public String AuthUrl { get; set; }

        /// <summary>访问令牌地址</summary>
        public String AccessUrl { get; set; }

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

        #region 构造
        /// <summary>实例化</summary>
        public OAuthClient()
        {
            Name = GetType().Name.TrimEnd("Client");

            // 标准地址格式
            AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = "access_token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
        }
        #endregion

        #region 静态创建
        private static IDictionary<String, Type> _map;
        /// <summary>根据名称创建客户端</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static OAuthClient Create(String name)
        {
            //if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));

            // 初始化映射表
            if (_map == null)
            {
                var dic = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in typeof(OAuthClient).GetAllSubclasses(true))
                {
                    var key = item.Name.TrimEnd("Client");
                    var ct = item.CreateInstance() as OAuthClient;
                    if (!ct.Name.IsNullOrEmpty()) key = ct.Name;

                    dic[key] = item;
                }

                _map = dic;
            }

            if (name.IsNullOrEmpty())
            {
                var set = OAuthConfig.Current;
                var mi = set.Items.FirstOrDefault(e => !e.AppID.IsNullOrEmpty());
                if (mi != null) name = mi.Name;
            }
            if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name), "未正确配置OAuth");

            //if (!_map.TryGetValue(name, out var type)) throw new Exception($"找不到[{name}]的OAuth客户端");
            // 找不到就用默认
            _map.TryGetValue(name, out var type);

            var client = type?.CreateInstance() as OAuthClient ?? new OAuthClient();
            client.Apply(name);

            return client;
        }
        #endregion

        #region 方法
        /// <summary>应用参数设置</summary>
        /// <param name="name"></param>
        public void Apply(String name)
        {
            var set = OAuthConfig.Current;
            var ms = set.Items;
            if (ms == null || ms.Length == 0) throw new InvalidOperationException("未设置OAuth服务端");

            var mi = set.GetOrAdd(name);
            if (name.IsNullOrEmpty()) mi = ms.FirstOrDefault(e => !e.AppID.IsNullOrEmpty());
            if (mi == null) throw new InvalidOperationException($"未找到有效的OAuth服务端设置[{name}]");

            Name = mi.Name;

            if (set.Debug) Log = XTrace.Log;

            Apply(mi);
        }

        /// <summary>应用参数设置</summary>
        /// <param name="mi"></param>
        public virtual void Apply(OAuthItem mi)
        {
            Name = mi.Name;
            if (!mi.Server.IsNullOrEmpty()) Server = mi.Server;
            if (!mi.AccessServer.IsNullOrEmpty()) AccessServer = mi.AccessServer;
            if (!mi.AppID.IsNullOrEmpty()) Key = mi.AppID;
            if (!mi.Secret.IsNullOrEmpty()) Secret = mi.Secret;
            if (!mi.Scope.IsNullOrEmpty()) Scope = mi.Scope;
        }
        #endregion

        #region 1-跳转验证
        private String _redirect;
        private String _state;

        /// <summary>构建跳转验证地址</summary>
        /// <param name="redirect">验证完成后调整的目标地址</param>
        /// <param name="state">用户状态数据</param>
        /// <param name="baseUri">相对地址的基地址</param>
        public virtual String Authorize(String redirect, String state = null, Uri baseUri = null)
        {
            if (redirect.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect));

            if (Key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Key), "未设置应用标识");
            if (Secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Secret), "未设置应用密钥");

            //if (state.IsNullOrEmpty()) state = Rand.Next().ToString();

            // 如果是相对路径，自动加上前缀。需要考虑反向代理的可能，不能直接使用Request.Url
            //redirect = redirect.AsUri(baseUri) + "";
            _redirect = redirect;
            _state = state;

            var url = GetUrl(AuthUrl);
            if (!state.IsNullOrEmpty()) WriteLog("Authorize {0}", url);

            return url;
        }
        #endregion

        #region 2-获取访问令牌
        /// <summary>根据授权码获取访问令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public virtual String GetAccessToken(String code)
        {
            if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code), "未设置授权码");

            Code = code;

            var url = GetUrl(AccessUrl);
            WriteLog("GetAccessToken {0}", url);

            var html = Request(url);
            if (html.IsNullOrEmpty()) return null;

            html = html.Trim();
            if (Log != null && Log.Enable) WriteLog(html);

            var dic = GetNameValues(html);
            if (dic != null)
            {
                if (dic.ContainsKey("access_token")) AccessToken = dic["access_token"].Trim();
                if (dic.ContainsKey("expires_in")) Expire = DateTime.Now.AddSeconds(dic["expires_in"].Trim().ToInt());
                if (dic.ContainsKey("refresh_token")) RefreshToken = dic["refresh_token"].Trim();

                // 如果响应区域包含用户信息，则增加用户地址
                if (UserUrl.IsNullOrEmpty() && dic.ContainsKey("scope"))
                {
                    var ss = dic["scope"].Trim().Split(",");
                    if (ss.Contains("UserInfo"))
                    {
                        UserUrl = "userinfo?access_token={token}";
                        LogoutUrl = "logout?client_id={key}&redirect_uri={redirect}&state={state}";
                    }
                }

                OnGetInfo(dic);
            }
            Items = dic;

            return html;
        }
        #endregion

        #region 3-获取OpenID
        /// <summary>OpenID地址</summary>
        public String OpenIDUrl { get; set; }

        /// <summary>根据授权码获取访问令牌</summary>
        /// <returns></returns>
        public virtual String GetOpenID()
        {
            if (AccessToken.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AccessToken), "未设置授权码");

            var url = GetUrl(OpenIDUrl);
            WriteLog("GetOpenID {0}", url);

            var html = Request(url);
            if (html.IsNullOrEmpty()) return null;

            html = html.Trim();
            if (Log != null && Log.Enable) WriteLog(html);

            var dic = GetNameValues(html);
            if (dic != null)
            {
                if (dic.ContainsKey("expires_in")) Expire = DateTime.Now.AddSeconds(dic["expires_in"].Trim().ToInt());
                if (dic.ContainsKey("openid")) OpenID = dic["openid"].Trim();

                OnGetInfo(dic);
            }
            Items = dic;

            return html;
        }
        #endregion

        #region 4-用户信息
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

        /// <summary>获取用户信息</summary>
        /// <returns></returns>
        public virtual String GetUserInfo()
        {
            var url = UserUrl;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(UserUrl), "未设置用户信息地址");

            url = GetUrl(url);
            WriteLog("GetUserInfo {0}", url);

            var html = Request(url);
            if (html.IsNullOrEmpty()) return null;

            html = html.Trim();
            if (Log != null && Log.Enable) WriteLog(html);

            var dic = GetNameValues(html);
            if (dic != null)
            {

                OnGetInfo(dic);

                // 合并字典
                if (Items == null)
                    Items = dic;
                else
                {
                    foreach (var item in dic)
                    {
                        Items[item.Key] = item.Value;
                    }
                }
            }

            return html;
        }

        /// <summary>填充用户，登录成功并获取用户信息之后</summary>
        /// <param name="user"></param>
        public virtual void Fill(IManageUser user)
        {
            if (user.Name.IsNullOrEmpty()) user.Name = UserName ?? OpenID;
            if (user.NickName.IsNullOrEmpty()) user.NickName = NickName;

            //// 头像
            //if (!Avatar.IsNullOrEmpty()) user.SetValue(nameof(Avatar), Avatar);
        }
        #endregion

        #region 5-注销
        /// <summary>注销地址</summary>
        public String LogoutUrl { get; set; }

        /// <summary>注销</summary>
        /// <param name="redirect">完成后调整的目标地址</param>
        /// <param name="state">用户状态数据</param>
        /// <param name="baseUri">相对地址的基地址</param>
        /// <returns></returns>
        public virtual String Logout(String redirect = null, String state = null, Uri baseUri = null)
        {
            var url = LogoutUrl;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(LogoutUrl), "未设置注销地址");

            // 如果是相对路径，自动加上前缀。需要考虑反向代理的可能，不能直接使用Request.Url
            //redirect = redirect.AsUri(baseUri) + "";
            _redirect = redirect;
            _state = state;

            url = GetUrl(url);
            WriteLog("Logout {0}", url);

            return url;
        }
        #endregion

        #region 辅助
        /// <summary>替换地址模版参数</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual String GetUrl(String url)
        {
            if (!url.StartsWithIgnoreCase("http"))
            {
                // 授权以外的连接，使用令牌服务地址
                if (!AccessServer.IsNullOrEmpty() && !url.StartsWithIgnoreCase("auth"))
                    url = AccessServer.EnsureEnd("/") + url.TrimStart('/');
                else
                    url = Server.EnsureEnd("/") + url.TrimStart('/');
            }

            url = url
               .Replace("{key}", Key)
               .Replace("{secret}", Secret)
               .Replace("{response_type}", ResponseType)
               .Replace("{token}", AccessToken)
               .Replace("{code}", Code)
               .Replace("{openid}", OpenID)
               .Replace("{redirect}", HttpUtility.UrlEncode(_redirect + ""))
               .Replace("{scope}", Scope)
               .Replace("{state}", _state);

            return url;
        }

        /// <summary>获取名值字典</summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected virtual IDictionary<String, String> GetNameValues(String html)
        {
            // 部分提供者的返回Json不是{开头，比如QQ
            var p1 = html.IndexOf('{');
            var p2 = html.LastIndexOf('}');
            if (p1 > 0 && p2 > p1) html = html.Substring(p1, p2 - p1 + 1);

            IDictionary<String, String> dic = null;
            // Json格式转为名值字典
            if (p1 >= 0 && p2 > p1)
            {
                dic = new JsonParser(html).Decode().ToDictionary().ToDictionary(e => e.Key.ToLower(), e => e.Value + "", StringComparer.OrdinalIgnoreCase);
            }
            // Url格式转为名值字典
            else if (html.Contains("=") && html.Contains("&"))
            {
                dic = html.SplitAsDictionary("=", "&").ToDictionary(e => e.Key.ToLower(), e => e.Value + "", StringComparer.OrdinalIgnoreCase);
            }

            return dic.ToNullable(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>最后一次请求的响应内容</summary>
        public String LastHtml { get; set; }

        private WebClientX _Client;
        /// <summary>创建客户端</summary>
        /// <param name="url">路径</param>
        /// <returns></returns>
        protected virtual String Request(String url)
        {
            if (_Client == null) _Client = new WebClientX();

            return LastHtml = _Client.GetHtml(url);
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected virtual void OnGetInfo(IDictionary<String, String> dic)
        {
            if (dic.ContainsKey("openid")) OpenID = dic["openid"].Trim();
            if (dic.TryGetValue("openid", out var str)) OpenID = str.Trim();

            if (dic.TryGetValue("uid", out str)) UserID = str.ToLong();
            if (dic.TryGetValue("userid", out str)) UserID = str.ToLong();
            if (dic.TryGetValue("user_id", out str)) UserID = str.ToLong();

            if (dic.TryGetValue("name", out str)) UserName = str.Trim();
            if (dic.TryGetValue("username", out str)) UserName = str.Trim();
            if (dic.TryGetValue("user_name", out str)) UserName = str.Trim();

            if (dic.TryGetValue("nickname", out str)) NickName = str.Trim();
            if (dic.TryGetValue("nick_name", out str)) NickName = str.Trim();

            if (dic.TryGetValue("Avatar", out str)) Avatar = str.Trim();

            // 获取用户信息出错时抛出异常
            if (dic.TryGetValue("error", out str)) throw new InvalidOperationException(str);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; }

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