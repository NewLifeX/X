using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;
using XCode.Membership;

/*
 * 魔方OAuth在禁用本地登录，且只设置一个第三方登录时，形成单点登录。
 * 
 * 验证流程：
 *      进入登录页~/Admin/User/Login
 *      if 允许本地登录
 *          输入密码登录 或 选择第三方登录
 *      else if 多个第三方登录
 *          选择第三方登录
 *      else
 *          直接跳转唯一的第三方登录
 *      登录完成
 *      if 有绑定用户
 *          登录完成，跳转来源页
 *      else
 *          进入绑定流程
 * 
 * 绑定流程：
 *      if 本地已登录
 *          第三方绑定到当前已登录本地用户
 *      else 允许本地登录
 *          显示登录框，输入密码登录后绑定
 *          或 直接进入，自动注册本地用户
 *      else
 *          直接进入，自动注册本地用户
 */

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
        /// <summary>获取OAuth客户端</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected virtual OAuthClient GetClient(String name)
        {
            var client = OAuthClient.Create(name);
            //client.Apply(name, Request.GetRawUrl() + "");

            return client;
        }

        /// <summary>第三方登录</summary>
        /// <param name="name"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Login(String name, String returnUrl)
        {
            var client = GetClient(name);

            var redirect = "~/Sso/LoginInfo";

            var buri = Request.GetRawUrl();
            if (!returnUrl.IsNullOrEmpty() && returnUrl.StartsWithIgnoreCase("http"))
            {
                var uri = new Uri(returnUrl);
                if (uri != null && uri.Host.EqualIgnoreCase(buri.Host)) returnUrl = uri.PathAndQuery;
            }

            redirect = OAuthHelper.GetUrl(redirect, returnUrl);
            var url = client.Authorize(redirect, client.Name, buri);

            return Redirect(url);
        }

        /// <summary>第三方登录完成后跳转到此</summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult LoginInfo(String code, String state)
        {
            // 异步会导致HttpContext.Current.Session为空无法赋值
            var ss = Session;
            var returnUrl = Request["returnUrl"];

            var client = GetClient(state + "");

            var buri = Request.GetRawUrl();
            var redirect = "~/Sso/LoginInfo";
            redirect = OAuthHelper.GetUrl(redirect, returnUrl);
            client.Authorize(redirect, client.Name, buri);

            Task.Run(() => client.GetAccessToken(code)).Wait();

            // 如果拿不到访问令牌，则重新跳转
            if (client.AccessToken.IsNullOrEmpty())
            {
                XTrace.WriteLine("拿不到访问令牌，重新跳转 code={0} state={1}", code, state);

                //return Redirect(OAuthHelper.GetUrl("Login"));
                return RedirectToAction("Login", new { name = client.Name, returnUrl });
            }

            // 获取OpenID。部分提供商不需要
            if (!client.OpenIDUrl.IsNullOrEmpty()) Task.Run(() => client.GetOpenID()).Wait();

            // 获取用户信息
            if (!client.UserUrl.IsNullOrEmpty()) Task.Run(() => client.GetUserInfo()).Wait();

            var url = OnLogin(client);

            if (!url.IsNullOrEmpty()) return Redirect(url);

            return RedirectToAction("Index", "Index", new { page = returnUrl });
            //var msg = $"{client.Name} 登录成功！<br/>{client.UserID} {client.UserName} {client.NickName} OpenID={client.OpenID} AccessToken={client.AccessToken} <br/><img src=\"{client.Avatar}\"/> ";
            //return Content(msg);
        }

        /// <summary>登录成功</summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected virtual String OnLogin(OAuthClient client)
        {
            //// 用户登录
            //var user = Provider.Login(client.UserName ?? (client.UserID + ""), client.OpenID);
            //if (user == null) return RedirectToAction("Login", "Account");

            //// 异步会导致HttpContext.Current.Session为空无法赋值
            //ss["User"] = user;

            //// 保存信息
            //user.SaveAsync();

            var openid = client.OpenID;
            if (openid.IsNullOrEmpty()) openid = client.UserName;

            // 根据OpenID找到用户绑定信息
            var uc = UserConnect.FindByProviderAndOpenID(client.Name, openid);
            if (uc == null) uc = new UserConnect { Provider = client.Name, OpenID = openid };

            if (!client.NickName.IsNullOrEmpty()) uc.NickName = client.NickName;
            if (!client.Avatar.IsNullOrEmpty()) uc.Avatar = client.Avatar;

            uc.LinkID = client.UserID;
            //ub.OpenID = client.OpenID;
            uc.AccessToken = client.AccessToken;
            uc.RefreshToken = client.RefreshToken;
            uc.Expire = client.Expire;

            if (client.Items != null) uc.Remark = client.Items.ToJson();

            var user = Provider.FindByID(uc.UserID);

            // 如果未绑定
            if (user == null || !uc.Enable) user = OnBind(uc);

            // 修正昵称
            if (!uc.NickName.IsNullOrEmpty() && user.NickName.IsNullOrEmpty())
            {
                user.NickName = uc.NickName;
                user.Save();
            }

            uc.Save();

            if (!user.Enable) throw new InvalidOperationException("用户已禁用！");

            Provider.Current = user;

            XTrace.WriteLine("{0} {1} {2} {3} 登录成功", client.UserID, client.UserName, client.NickName, client.OpenID);

            var returnUrl = Request["returnUrl"];
            if (Url.IsLocalUrl(returnUrl)) return returnUrl;

            //return null;
            return "~/Admin";
        }

        /// <summary>绑定用户</summary>
        /// <param name="uc"></param>
        protected virtual IManageUser OnBind(UserConnect uc)
        {
            var username = uc.Provider + "_" + uc.OpenID;
            // 如果未登录，需要注册一个
            var user = Provider.Current;
            if (user == null) user = Provider.FindByName(username);
            if (user == null) user = Provider.Register(username, Rand.NextString(16), null, true);

            uc.UserID = user.ID;
            uc.Enable = true;

            return user;
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