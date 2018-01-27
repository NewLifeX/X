using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Model;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Controllers
{
    /// <summary>单点登录控制器</summary>
    public class SsoController : Controller
    {
        /// <summary>当前提供者</summary>
        public static IManageProvider Provider { get; set; } = ManageProvider.Provider;

        static SsoController()
        {
            OAuthServer.Instance.Log = XTrace.Log;
        }

        /// <summary>首页</summary>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Index()
        {
            return View("Index");
        }

        #region 单点登录客户端
        /// <summary>第三方登录</summary>
        /// <param name="name"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Login(String name, String returnUrl)
        {
            var set = OAuthConfig.Current;
            var ms = set.Items;
            if (ms == null || ms.Length == 0) throw new InvalidOperationException("未设置OAuth服务端");

            var mi = ms.FirstOrDefault(e => e.Enable && (name.IsNullOrEmpty() || e.Name.EqualIgnoreCase(name)));
            if (mi == null) throw new InvalidOperationException($"未找到有效的OAuth服务端设置[{name}]");

            var sso = new OAuthClient()
            {
                Log = XTrace.Log,
                BaseUrl = Request.GetRawUrl() + "",
            };

            sso.Key = mi.AppID;
            sso.Secret = mi.Secret;
            sso.Scope = mi.Scope;

            switch (mi.Name)
            {
                case "Baidu": sso.SetBaidu(); break;
            }

            var redirect = "~/Sso/LoginInfo";

            if (!returnUrl.IsNullOrEmpty() && returnUrl.StartsWithIgnoreCase("http"))
            {
                var uri = new Uri(returnUrl);
                if (uri != null && uri.Host.EqualIgnoreCase(Request.Url.Host)) returnUrl = uri.PathAndQuery;
            }

            redirect = OAuthHelper.GetUrl(redirect, returnUrl);

            var url = sso.Authorize(redirect, null);

            return Redirect(url);
        }

        /// <summary>第三方登录完成后跳转到此</summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<ActionResult> LoginInfo(String code, String state)
        {
            var ss = Session;

            var sso = new OAuthClient() { Log = XTrace.Log };
            await sso.GetAccessToken(code);
            // 如果拿不到访问令牌，则重新跳转
            if (sso.AccessToken.IsNullOrEmpty())
            {
                XTrace.WriteLine("拿不到访问令牌，重新跳转 code={0} state={1}", code, state);

                return Redirect(OAuthHelper.GetUrl("Login"));
                //return RedirectToAction("Login");
            }

            //// 拿到OpenID
            //var inf = await sso.GetUserInfo();
            //var uid = inf["user_id"].ToInt();

            //// 用户登录
            //var user = UserX.Login(uid, sso.OpenID);
            //if (user == null) return RedirectToAction("Login", "Account");

            //// 异步会导致HttpContext.Current.Session为空无法赋值
            //ss["User"] = user;

            //// 保存信息
            //user.SaveAsync();

            var returnUrl = Request["returnUrl"];
            return Login("", returnUrl);
        }
        #endregion

        #region 单点登录服务端
        /// <summary>1，验证用户身份</summary>
        /// <remarks>
        /// 子系统需要验证访问者身份时，引导用户跳转到这里。
        /// 用户登录完成后，得到一个独一无二的code，并跳转回去子系统。
        /// </remarks>
        /// <param name="appid">应用标识</param>
        /// <param name="redirect_uri">回调地址</param>
        /// <param name="response_type">响应类型。默认code</param>
        /// <param name="scope">授权域</param>
        /// <param name="state">用户状态数据</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Authorize(String appid, String redirect_uri, String response_type = null, String scope = null, String state = null)
        {
            if (appid.IsNullOrEmpty()) throw new ArgumentNullException(nameof(appid));
            if (redirect_uri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect_uri));
            if (response_type.IsNullOrEmpty()) response_type = "code";

            // 判断合法性，然后跳转到登录页面，登录完成后跳转回来
            var key = OAuthServer.Instance.Authorize(appid, redirect_uri, response_type, scope, state);

            var url = GetUrl("~/Account/Login", "~/sso/auth2/" + key);

            return Redirect(url);
        }

        /// <summary>2，用户登录成功后返回这里</summary>
        /// <remarks>
        /// 构建身份验证结构，返回code给子系统
        /// </remarks>
        /// <returns></returns>
        public virtual ActionResult Auth2(Int32 id)
        {
            if (id <= 0) throw new ArgumentNullException(nameof(id));

            var sso = OAuthServer.Instance;

            //var provider = FrontManagerProvider.Provider ?? ManageProvider.Provider;
            var provider = Provider;
            var user = provider?.Current;
            if (user == null) throw new InvalidOperationException("未登录！");

            // 返回给子系统的数据：
            // code 授权码，子系统凭借该代码来索取用户信息
            // state 子系统传过来的用户状态数据，原样返回

            var url = sso.GetResult(id, user as IManageUser);

            return Redirect(url);
        }

        /// <summary>3，根据code获取令牌</summary>
        /// <remarks>
        /// 子系统根据验证用户身份时得到的code，直接在服务器间请求本系统。
        /// 传递应用标识和密钥，主要是为了向本系统表明其合法身份。
        /// </remarks>
        /// <param name="appid">应用标识</param>
        /// <param name="secret">密钥</param>
        /// <param name="code">代码</param>
        /// <param name="grant_type">授权类型。</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Token(String appid, String secret, String code, String grant_type = null)
        {
            if (appid.IsNullOrEmpty()) throw new ArgumentNullException(nameof(appid));
            if (secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(secret));
            if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code));
            if (grant_type.IsNullOrEmpty()) grant_type = "authorization_code";

            // 返回给子系统的数据：
            // access_token 访问令牌
            // expires_in 有效期
            // refresh_token 刷新令牌
            // openid 用户唯一标识

            var user = OAuthServer.Instance.GetUser(code);
            var user2 = user as IUser;

            return Json(new
            {
                result = true,
                data = new
                {
                    user.ID,
                    user.Name,
                    user.NickName,
                    user2?.RoleID,
                    user2?.RoleName,
                }
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        /// <summary>4，注销登录</summary>
        /// <remarks>
        /// 子系统引导用户跳转到这里注销登录。
        /// </remarks>
        /// <param name="redirect_uri">回调地址</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Logout(String redirect_uri)
        {
            if (redirect_uri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect_uri));

            var url = GetUrl("~/Account/Logout", redirect_uri);

            return Redirect(url);
        }

        private String GetUrl(String baseUrl, String returnUrl = null)
        {
            var url = baseUrl;
            //if (url.StartsWith("~/")) url = Server.UrlPathEncode(url);

            if (returnUrl.IsNullOrEmpty()) returnUrl = Request["returnUrl"];

            if (!returnUrl.IsNullOrEmpty())
            {
                if (url.Contains("?"))
                    url += "&";
                else
                    url += "?";

                if (returnUrl.StartsWith("~/")) returnUrl = HttpRuntime.AppDomainAppVirtualPath + returnUrl.Substring(2);
                url += "returnUrl=" + HttpUtility.UrlEncode(returnUrl);
            }

            return url;
        }
    }
}