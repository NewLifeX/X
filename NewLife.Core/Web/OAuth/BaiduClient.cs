using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>身份验证提供者</summary>
    public class BaiduClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public BaiduClient()
        {
            var url = "https://openapi.baidu.com/oauth/2.0/";

            AuthUrl = url + "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = url + "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            UserUrl = "https://openapi.baidu.com/rest/2.0/passport/users/getLoggedInUser?access_token={token}";
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            if (dic.ContainsKey("uid")) UserID = dic["uid"].Trim().ToLong();
            if (dic.ContainsKey("uname")) UserName = dic["uname"].Trim();

            // small image: http://tb.himg.baidu.com/sys/portraitn/item/{$portrait}
            // large image: http://tb.himg.baidu.com/sys/portrait/item/{$portrait}
            if (dic.ContainsKey("portrait")) Avatar = "http://tb.himg.baidu.com/sys/portrait/item/" + dic["portrait"].Trim();
        }
    }
}