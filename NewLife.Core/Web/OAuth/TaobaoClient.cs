using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>淘宝身份验证提供者</summary>
    public class TaobaoClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public TaobaoClient()
        {
            var url = "https://oauth.taobao.com/";

            AuthUrl = url + "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = url + "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            //UserUrl = "https://openapi.baidu.com/rest/2.0/passport/users/getLoggedInUser?access_token={token}";
            LogoutUrl = url + "logoff?client_id={key}&view=web";
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            if (dic.ContainsKey("taobao_user_id")) UserID = dic["taobao_user_id"].Trim('\"').ToLong();
            if (dic.ContainsKey("taobao_user_nick")) UserName = dic["taobao_user_nick"].Trim();
        }
    }
}