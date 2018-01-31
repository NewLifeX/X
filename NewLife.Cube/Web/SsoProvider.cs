using System;
using System.Web;
using NewLife.Cube.Entity;
using NewLife.Model;
using NewLife.Security;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Web
{
    /// <summary>单点登录提供者</summary>
    public class SsoProvider
    {
        #region 属性
        /// <summary>用户管理提供者</summary>
        public IManageProvider Provider { get; set; }

        /// <summary>重定向地址。~/Sso/LoginInfo</summary>
        public String RedirectUrl { get; set; }

        /// <summary>登录成功后跳转地址。~/Admin</summary>
        public String SuccessUrl { get; set; }

        /// <summary>已登录用户</summary>
        public IManageUser Current => Provider.Current;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public SsoProvider()
        {
            Provider = ManageProvider.Provider;
            RedirectUrl = "~/Sso/LoginInfo";
            SuccessUrl = "~/Admin";
        }
        #endregion

        #region 方法
        /// <summary>获取OAuth客户端</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual OAuthClient GetClient(String name) => OAuthClient.Create(name);

        /// <summary>获取返回地址</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual String GetReturnUrl(HttpRequestBase request)
        {
            var returnUrl = request["r"];
            if (!returnUrl.IsNullOrEmpty() && returnUrl.StartsWithIgnoreCase("http"))
            {
                var baseUri = request.GetRawUrl();

                var uri = new Uri(returnUrl);
                if (uri != null && uri.Host.EqualIgnoreCase(baseUri.Host)) returnUrl = uri.PathAndQuery;
            }

            return returnUrl;
        }

        /// <summary>获取回调地址</summary>
        /// <param name="request"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public virtual String GetRedirect(HttpRequestBase request, String returnUrl = null)
        {
            var baseUri = request.GetRawUrl();

            var url = RedirectUrl;
            if (url.StartsWith("~/")) url = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + url.Substring(2);
            if (url.StartsWith("/"))
            {
                var uri = new Uri(baseUri, url);
                url = uri.ToString();
            }

            if (returnUrl.IsNullOrEmpty()) returnUrl = GetReturnUrl(request);
            if (!returnUrl.IsNullOrEmpty() && returnUrl != "/")
            {
                if (url.Contains("?"))
                    url += "&";
                else
                    url += "?";

                url += "r=" + HttpUtility.UrlEncode(returnUrl);
            }

            return url;
        }

        /// <summary>登录成功</summary>
        /// <param name="client">OAuth客户端</param>
        /// <param name="service">服务提供者。可用于获取HttpContext成员</param>
        /// <returns></returns>
        public virtual String OnLogin(OAuthClient client, IServiceProvider service)
        {
            var openid = client.OpenID;
            if (openid.IsNullOrEmpty()) openid = client.UserName;

            // 根据OpenID找到用户绑定信息
            var uc = UserConnect.FindByProviderAndOpenID(client.Name, openid);
            if (uc == null) uc = new UserConnect { Provider = client.Name, OpenID = openid };

            uc.Fill(client);

            // 检查绑定
            var user = Provider.FindByID(uc.UserID);
            if (user == null || !uc.Enable) user = OnBind(uc, client);

            // 填充昵称等数据
            client.Fill(user);
            var dic = client.Items;
            if (dic != null && user is UserX user2)
            {
                if (user2.Mail.IsNullOrEmpty() && dic.TryGetValue("email", out var email)) user2.Mail = email;
            }

            user.Save();
            uc.Save();

            if (!user.Enable) throw new InvalidOperationException("用户已禁用！");

            Provider.Current = user;

            return SuccessUrl;
        }

        /// <summary>绑定用户</summary>
        /// <param name="uc"></param>
        /// <param name="client"></param>
        public virtual IManageUser OnBind(UserConnect uc, OAuthClient client)
        {
            var prv = Provider;

            // 如果未登录，需要注册一个
            var user = prv.Current;

            if (user == null)
            {
                // 如果用户名不能用，则考虑OpenID
                var name = client.UserName;
                if (name.IsNullOrEmpty() || prv.FindByName(name) != null)
                {
                    var openid = client.OpenID ?? client.AccessToken;
                    name = openid;

                    // 过长，需要随机一个较短的
                    if (name.Length > 12)
                    {
                        var num = name.GetBytes().Crc16();
                        while (true)
                        {
                            name = uc.Provider + "_" + num.ToString("X4");
                            user = prv.FindByName(name);
                            if (user == null) break;

                            if (num >= UInt16.MaxValue) throw new InvalidOperationException("不可能的设计错误！");
                            num++;
                        }
                    }
                }

                // 注册用户，随机密码
                user = prv.Register(name, Rand.NextString(16), null, true);
            }

            uc.UserID = user.ID;
            uc.Enable = true;

            return user;
        }

        /// <summary>注销</summary>
        /// <returns></returns>
        public virtual void Logout()
        {
            Provider.Current = null;
        }
        #endregion
    }
}