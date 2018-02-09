using System;
using NewLife.Caching;
using NewLife.Model;
using NewLife.Security;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>单点登录服务端</summary>
    public class OAuthServer
    {
        #region 属性
        private ICache Cache { get; } = NewLife.Caching.Cache.Default;

        /// <summary>令牌提供者</summary>
        public TokenProvider TokenProvider { get; set; }

        /// <summary>令牌有效期。默认24小时</summary>
        public Int32 Expire { get; set; } = 24 * 3600;
        #endregion

        #region 静态
        /// <summary>实例</summary>
        public static OAuthServer Instance { get; set; } = new OAuthServer();
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>验证用户身份</summary>
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
        public virtual Int32 Authorize(String appid, String redirect_uri, String response_type = null, String scope = null, String state = null)
        {
            //if (appid.IsNullOrEmpty()) throw new ArgumentNullException(nameof(appid));
            //if (redirect_uri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect_uri));
            //if (response_type.IsNullOrEmpty()) response_type = "code";

            if (!response_type.EqualIgnoreCase("code")) throw new NotSupportedException(nameof(response_type));

            // 用缓存把数据存下来，避免本站登录期间，跳转地址很长
            var model = new Model
            {
                AppID = appid,
                Uri = redirect_uri,
                Type = response_type,
                Scope = scope,
                State = state,
            };

            // 随机key，并尝试加入缓存
            var key = 0;
            do
            {
                key = Rand.Next();
            }
            while (!Cache.Add("Model:" + key, model, 20 * 60));

            if (Log != null) WriteLog("Authorize {2} key={0} {1}", key, model.ToJson(false), appid);

            return key;
        }

        /// <summary>根据验证结果获取跳转回子系统的Url</summary>
        /// <param name="key"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual String GetResult(Int32 key, IManageUser user)
        {
            var k = "Model:" + key;
            var model = Cache.Get<Model>(k);
            if (model == null) throw new ArgumentOutOfRangeException(nameof(key));

            Cache.Remove(k);

            //// 保存用户信息
            //model.User = user;
            var prv = TokenProvider;
            if (prv == null) prv = TokenProvider = new TokenProvider();
            if (prv.Key.IsNullOrEmpty()) prv.ReadKey("..\\Keys\\OAuth.prvkey", true);

            // 建立令牌
            model.Token = prv.Encode(user.Name, DateTime.Now.AddSeconds(Expire));

            // 随机code，并尝试加入缓存
            var code = "";
            do
            {
                code = Rand.NextString(16);
            }
            while (!Cache.Add("Code:" + code, model, 20 * 60));

            if (Log != null) WriteLog("{2} key={0} code={1}", key, code, model.AppID);

            var url = model.Uri;
            if (url.Contains("?"))
                url += "&";
            else
                url += "?";
            url += "code=" + code;
            if (!model.State.IsNullOrEmpty()) url += "&state=" + model.State;

            return url;
        }

        /// <summary>根据Code获取令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public virtual String GetToken(String code)
        {
            var k = "Code:" + code;
            var model = Cache.Get<Model>(k);
            if (model == null) throw new ArgumentOutOfRangeException(nameof(code));

            if (Log != null) WriteLog("Token {0} code={1} token={2}", model.AppID, code, model.Token);

            Cache.Remove(k);

            return model.Token;
        }

        ///// <summary>根据Token获取用户信息</summary>
        ///// <param name="token"></param>
        ///// <returns></returns>
        //public virtual IManageUser GetUser(String token)
        //{
        //    var k = "Code:" + token;
        //    var model = Cache.Get<Model>(k);
        //    if (model == null) throw new ArgumentOutOfRangeException(nameof(token));

        //    if (Log != null) WriteLog("GetUser code={0} user={1}", token, model.User);

        //    Cache.Remove(k);

        //    return model.User;
        //}
        #endregion

        #region 内嵌
        class Model
        {
            public String AppID { get; set; }
            public String Uri { get; set; }
            public String Type { get; set; }
            public String Scope { get; set; }
            public String State { get; set; }

            public String Token { get; set; }

            //public IManageUser User { get; set; }
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public NewLife.Log.ILog Log { get; set; }

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