using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>身份验证提供者</summary>
    public class WeixinClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public WeixinClient()
        {
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);
        }
    }
}