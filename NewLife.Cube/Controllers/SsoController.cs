using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Model;
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
 *          显示登录框，输入密码登录后绑定（暂不支持）
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
        public static SsoProvider Provider { get; set; }

        static SsoController()
        {
            // 注册单点登录
            var oc = ObjectContainer.Current;
            oc.Register<SsoProvider, SsoProvider>();

            Provider = ObjectContainer.Current.ResolveInstance<SsoProvider>();

            //OAuthServer.Instance.Log = XTrace.Log;
            OAuthServer.Instance.Log = LogProvider.Provider.AsLog("OAuth");
        }

        /// <summary>首页</summary>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Index() => Redirect("~/");

        /// <summary>发生错误时</summary>
        /// <param name="filterContext"></param>
        protected override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                var vr = new ViewResult
                {
                    ViewName = "CubeError"
                };
                vr.ViewBag.Context = filterContext;

                filterContext.Result = vr;
                filterContext.ExceptionHandled = true;
            }

            base.OnException(filterContext);
        }

        #region 单点登录客户端
        /// <summary>第三方登录</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Login(String name)
        {
            var prov = Provider;
            var client = prov.GetClient(name);
            var rurl = prov.GetReturnUrl(Request, true);
            var redirect = prov.GetRedirect(Request, rurl);

            var state = Request["state"];
            if (!state.IsNullOrEmpty())
                state = client.Name + "_" + state;
            else
                state = client.Name;

            var url = client.Authorize(redirect, state);

            return Redirect(url);
        }

        /// <summary>第三方登录完成后跳转到此</summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult LoginInfo(String code, String state)
        {
            var name = state + "";
            var p = name.IndexOf('_');
            if (p > 0)
            {
                name = state.Substring(0, p);
                state = state.Substring(p + 1);
            }

            var prov = Provider;
            var client = prov.GetClient(name);

            client.WriteLog("LoginInfo name={0} code={1} state={2}", name, code, state);

            // 构造redirect_uri，部分提供商（百度）要求获取AccessToken的时候也要传递
            var redirect = prov.GetRedirect(Request);
            client.Authorize(redirect);

            var returnUrl = prov.GetReturnUrl(Request, false);

            try
            {
                // 获取访问令牌
                var html = client.GetAccessToken(code);

                // 如果拿不到访问令牌或用户信息，则重新跳转
                if (client.AccessToken.IsNullOrEmpty() && client.OpenID.IsNullOrEmpty() && client.UserID == 0)
                {
                    // 如果拿不到访问令牌，刷新一次，然后报错
                    if (state.EqualIgnoreCase("refresh"))
                    {
                        if (client.Log == null) XTrace.WriteLine(html);

                        throw new InvalidOperationException("内部错误，无法获取令牌");
                    }

                    XTrace.WriteLine("拿不到访问令牌，重新跳转 code={0} state={1}", code, state);

                    return RedirectToAction("Login", new { name = client.Name, r = returnUrl, state = "refresh" });
                }

                // 获取OpenID。部分提供商不需要
                if (!client.OpenIDUrl.IsNullOrEmpty()) client.GetOpenID();
                // 获取用户信息
                if (!client.UserUrl.IsNullOrEmpty()) client.GetUserInfo();

                var url = prov.OnLogin(client, HttpContext);

                // 标记登录提供商
                Session["Cube_Sso"] = client.Name;
                Session["Cube_Sso_Client"] = client;

                if (!returnUrl.IsNullOrEmpty()) url = returnUrl;

                return Redirect(url);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex.GetTrue());
                //throw;

                if (!state.EqualIgnoreCase("refresh")) return RedirectToAction("Login", new { name = client.Name, r = returnUrl, state = "refresh" });

                var inf = new HandleErrorInfo(ex, "Sso", nameof(LoginInfo));
                return View("CubeError", inf);
            }
        }

        /// <summary>注销登录</summary>
        /// <remarks>
        /// 子系统引导用户跳转到这里注销登录。
        /// </remarks>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Logout()
        {
            // 先读Session，待会会清空
            var client = Session["Cube_Sso_Client"] as OAuthClient;

            var prv = Provider;
            prv?.Logout();

            var url = "";

            // 准备跳转到验证中心
            if (client != null)
            {
                if (!client.LogoutUrl.IsNullOrEmpty())
                {
                    // 准备返回地址
                    url = Request["r"];
                    if (url.IsNullOrEmpty()) url = prv.SuccessUrl;

                    var state = Request["state"];
                    if (!state.IsNullOrEmpty())
                        state = client.Name + "_" + state;
                    else
                        state = client.Name;

                    // 跳转到验证中心注销地址
                    url = client.Logout(url, state, Request.GetRawUrl());

                    return Redirect(url);
                }
            }

            url = Provider?.GetReturnUrl(Request, false);
            if (url.IsNullOrEmpty()) url = "~/";

            return Redirect(url);
        }

        /// <summary>绑定</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual ActionResult Bind(String id)
        {
            var prov = Provider;

            var user = prov.Current;
            if (user == null) throw new Exception("未登录！");

            var client = prov.GetClient(id);
            var redirect = prov.GetRedirect(Request, Request.UrlReferrer + "");
            // 附加绑定动作
            redirect += "&sso_action=bind";
            var url = client.Authorize(redirect, client.Name);

            return Redirect(url);
        }

        /// <summary>取消绑定</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual ActionResult UnBind(String id)
        {
            var user = Provider.Current;
            if (user == null) throw new Exception("未登录！");

            var binds = UserConnect.FindAllByUserID(user.ID);

            var uc = binds.FirstOrDefault(e => e.Provider.EqualIgnoreCase(id));
            if (uc != null)
            {
                uc.Enable = false;
                uc.Save();
            }

            var url = Request.UrlReferrer + "";
            if (url.IsNullOrEmpty()) url = "/";

            return Redirect(url);
        }
        #endregion

        #region 单点登录服务端
        /// <summary>1，验证用户身份</summary>
        /// <remarks>
        /// 子系统需要验证访问者身份时，引导用户跳转到这里。
        /// 用户登录完成后，得到一个独一无二的code，并跳转回去子系统。
        /// </remarks>
        /// <param name="client_id">应用标识</param>
        /// <param name="redirect_uri">回调地址</param>
        /// <param name="response_type">响应类型。默认code</param>
        /// <param name="scope">授权域</param>
        /// <param name="state">用户状态数据</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Authorize(String client_id, String redirect_uri, String response_type = null, String scope = null, String state = null)
        {
            if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
            if (redirect_uri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect_uri));
            if (response_type.IsNullOrEmpty()) response_type = "code";

            // 判断合法性，然后跳转到登录页面，登录完成后跳转回来
            var sso = OAuthServer.Instance;
            var key = sso.Authorize(client_id, redirect_uri, response_type, scope, state);

            var prov = Provider;
            var url = "";

            // 如果已经登录，直接返回。否则跳到登录页面
            var user = prov?.Current;
            if (user != null)
                url = sso.GetResult(key, user);
            else
                url = prov.LoginUrl.AppendReturn("~/Sso/Auth2/" + key);

            return Redirect(url);
        }

        /// <summary>2，用户登录成功后返回这里</summary>
        /// <remarks>
        /// 构建身份验证结构，返回code给子系统
        /// </remarks>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Auth2(Int32 id)
        {
            if (id <= 0) throw new ArgumentNullException(nameof(id));

            var sso = OAuthServer.Instance;

            var user = Provider?.Current;
            if (user == null) throw new InvalidOperationException("未登录！");

            // 返回给子系统的数据：
            // code 授权码，子系统凭借该代码来索取用户信息
            // state 子系统传过来的用户状态数据，原样返回

            var url = sso.GetResult(id, user);

            return Redirect(url);
        }

        /// <summary>3，根据code获取令牌</summary>
        /// <remarks>
        /// 子系统根据验证用户身份时得到的code，直接在服务器间请求本系统。
        /// 传递应用标识和密钥，主要是为了向本系统表明其合法身份。
        /// </remarks>
        /// <param name="client_id">应用标识</param>
        /// <param name="client_secret">密钥</param>
        /// <param name="code">代码</param>
        /// <param name="grant_type">授权类型。</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Access_Token(String client_id, String client_secret, String code, String grant_type = null)
        {
            if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
            if (client_secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_secret));
            if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code));
            if (grant_type.IsNullOrEmpty()) grant_type = "authorization_code";

            // 返回给子系统的数据：
            // access_token 访问令牌
            // expires_in 有效期
            // refresh_token 刷新令牌
            // openid 用户唯一标识

            var sso = OAuthServer.Instance;
            try
            {
                var rs = Provider.GetAccessToken(sso, code);

                // 返回UserInfo告知客户端可以请求用户信息
                return Json(rs, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"Access_Token client_id={client_id} client_secret={client_secret} code={code}");
                XTrace.WriteException(ex);
                return Json(new { error = ex.GetTrue().Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>3，根据token获取用户信息</summary>
        /// <param name="access_token">访问令牌</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult UserInfo(String access_token)
        {
            if (access_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(access_token));

            var sso = OAuthServer.Instance;
            IManageUser user = null;

            var msg = "";
            try
            {
                user = Provider?.GetUser(sso, access_token);
                if (user == null) throw new Exception("用户不存在");

                var rs = Provider.GetUserInfo(sso, access_token, user);
                return Json(rs, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                msg = ex.GetTrue().Message;

                XTrace.WriteLine($"UserInfo {access_token}");
                XTrace.WriteException(ex);
                return Json(new { error = ex.GetTrue().Message }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                sso.WriteLog("UserInfo {0} access_token={1} msg={2}", user, access_token, msg);
            }
        }
        #endregion

        #region 辅助
        /// <summary>获取用户头像</summary>
        /// <param name="id">用户编号</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Avatar(Int32 id)
        {
            if (id <= 0) throw new ArgumentNullException(nameof(id));

            var prv = Provider;
            if (prv == null) throw new ArgumentNullException(nameof(Provider));

            var set = Setting.Current;
            var av = set.AvatarPath.CombinePath(id + "").GetFullPath();
            if (!System.IO.File.Exists(av))
            {
                var user = prv.Provider?.FindByID(id);
                if (user == null) throw new Exception("用户不存在 " + id);

                prv.FetchAvatar(user);
            }
            if (!System.IO.File.Exists(av)) throw new Exception("用户头像不存在 " + id);

            return File(av, "image");
        }
        #endregion
    }
}