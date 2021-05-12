using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.Http
{
    /// <summary>Http令牌过滤器，请求前加上令牌，请求后拦截401/403</summary>
    public class TokenHttpFilter : IHttpFilter
    {
        #region 属性
        /// <summary>用户</summary>
        public String UserName { get; set; }

        /// <summary>密钥</summary>
        public String Password { get; set; }

        /// <summary>申请令牌动作名，默认 OAuth/Token</summary>
        public String Action { get; set; } = "OAuth/Token";

        /// <summary>令牌信息</summary>
        public TokenModel Token { get; set; }

        /// <summary>令牌有效期</summary>
        public DateTime Expire { get; set; }

        private DateTime _refresh;

        /// <summary>清空令牌的错误码。默认401和403</summary>
        public IList<Int32> ErrorCodes { get; set; } = new List<Int32> { 401, 403 };
        #endregion

        /// <summary>请求前</summary>
        /// <param name="client">客户端</param>
        /// <param name="request">请求消息</param>
        /// <param name="state">状态数据</param>
        /// <returns></returns>
        public virtual async Task OnRequest(HttpClient client, HttpRequestMessage request, Object state)
        {
            if (request.Headers.Authorization != null) return;

            var path = client.BaseAddress == null ? request.RequestUri.AbsoluteUri : request.RequestUri.OriginalString;
            if (path.StartsWithIgnoreCase(Action.EnsureStart("/"))) return;

            // 申请令牌。没有令牌，或者令牌已过期
            if (Token == null || Expire < DateTime.Now)
            {
                Token = client.Post<TokenModel>(Action, new
                {
                    grant_type = "password",
                    username = UserName,
                    password = Password
                });

                // 过期时间和刷新令牌的时间
                Expire = DateTime.Now.AddSeconds(Token.ExpireIn);
                _refresh = DateTime.Now.AddSeconds(Token.ExpireIn / 2);
            }

            // 刷新令牌。要求已有令牌，且未过期，且达到了刷新时间
            if (Token != null && Expire > DateTime.Now && _refresh < DateTime.Now)
            {
                try
                {
                    Token = await client.PostAsync<TokenModel>(Action, new
                    {
                        grant_type = "refresh_token",
                        refresh_token = Token.RefreshToken,
                    });

                    // 过期时间和刷新令牌的时间
                    Expire = DateTime.Now.AddSeconds(Token.ExpireIn);
                    _refresh = DateTime.Now.AddSeconds(Token.ExpireIn / 2);
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine("刷新令牌异常 {0}", Token.ToJson());
                    XTrace.WriteException(ex);
                }
            }

            // 使用令牌。要求已有令牌，且未过期
            if (Token != null && Expire > DateTime.Now)
            {
                var type = Token.TokenType;
                if (type.IsNullOrEmpty() || type.EqualIgnoreCase("Token", "JWT")) type = "Bearer";
                request.Headers.Authorization = new AuthenticationHeaderValue(type, Token.AccessToken);
            }
        }

        /// <summary>获取响应后</summary>
        /// <param name="client">客户端</param>
        /// <param name="response">响应消息</param>
        /// <param name="state">状态数据</param>
        /// <returns></returns>
        public virtual Task OnResponse(HttpClient client, HttpResponseMessage response, Object state)
        {
            var code = (Int32)response.StatusCode;
            if (ErrorCodes.Contains(code))
            {
                // 马上过期
                Expire = DateTime.MinValue;
            }

#if NET40
            return TaskEx.FromResult(0);
#elif NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>发生错误时</summary>
        /// <param name="client">客户端</param>
        /// <param name="exception">异常</param>
        /// <param name="state">状态数据</param>
        /// <returns></returns>
        public virtual Task OnError(HttpClient client, Exception exception, Object state)
        {
            // 识别ApiException
            if (exception is ApiException ae && ErrorCodes.Contains(ae.Code))
            {
                // 马上过期
                Expire = DateTime.MinValue;
            }

#if NET40
            return TaskEx.FromResult(0);
#elif NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}