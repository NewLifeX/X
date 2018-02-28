using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>身份验证提供者</summary>
    public class GithubClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public GithubClient()
        {
            Server = "https://github.com/login/oauth/";

            AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = "access_token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            UserUrl = "https://api.github.com/user?access_token={token}";
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            if (dic.ContainsKey("id")) UserID = dic["id"].Trim('\"').ToLong();
            if (dic.ContainsKey("login")) UserName = dic["login"].Trim();
            if (dic.ContainsKey("name")) NickName = dic["name"].Trim();
            if (dic.ContainsKey("avatar_url")) Avatar = dic["avatar_url"].Trim();
        }

        private WebClientX _Client;

        /// <summary>创建客户端</summary>
        /// <param name="url">路径</param>
        /// <returns></returns>
        protected override String Request(String url)
        {
            if (_Client == null)
            {
                // 允许宽松头部
                WebClientX.SetAllowUnsafeHeaderParsing(true);

                // 必须指定中文编码
                _Client = new WebClientX(true, true);
            }
            return LastHtml = _Client.GetHtml(url);
        }
    }
}