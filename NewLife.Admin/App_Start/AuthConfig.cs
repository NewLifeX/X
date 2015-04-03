using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.WebPages.OAuth;
using NewLife.Admin.Models;

namespace NewLife.Admin
{
    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            // 若要允许此站点的用户使用他们在其他站点(例如 Microsoft、Facebook 和 Twitter)上拥有的帐户登录，
            // 必须更新此站点。有关详细信息，请访问 http://go.microsoft.com/fwlink/?LinkID=252166

            //OAuthWebSecurity.RegisterMicrosoftClient(
            //    clientId: "",
            //    clientSecret: "");

            //OAuthWebSecurity.RegisterTwitterClient(
            //    consumerKey: "",
            //    consumerSecret: "");

            //OAuthWebSecurity.RegisterFacebookClient(
            //    appId: "",
            //    appSecret: "");

            //OAuthWebSecurity.RegisterGoogleClient();
        }
    }
}
