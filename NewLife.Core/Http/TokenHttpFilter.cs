using System;
using System.Net.Http;
using System.Net.Http.Headers;
using NewLife.Remoting;
using NewLife.Web;

namespace NewLife.Http
{
    /// <summary>Http令牌过滤器，请求前加上令牌，请求后拦截401/403</summary>
    public class TokenHttpFilter : IHttpFilter
    {
        /// <summary>用户</summary>
        public String UserName { get; set; }

        /// <summary>密钥</summary>
        public String Password { get; set; }

        /// <summary>令牌信息</summary>
        public TokenModel Token { get; set; }

        /// <summary>令牌有效期</summary>
        public DateTime Expire { get; set; }

        /// <summary>清空令牌的错误码</summary>
        public Int32 ErrorCode { get; set; } = 401;

        /// <summary>请求前</summary>
        /// <param name="client">客户端</param>
        /// <param name="request">请求消息</param>
        /// <param name="state">状态数据</param>
        /// <returns></returns>
        public virtual void OnRequest(HttpClient client, HttpRequestMessage request, Object state)
        {
            if (request.Headers.Authorization != null) return;

            var path = client.BaseAddress == null ? request.RequestUri.AbsoluteUri : request.RequestUri.OriginalString;
            if (path.StartsWithIgnoreCase("/OAuth/Token")) return;

            if (Token == null)
            {
                // 申请令牌
                Token = client.Post<TokenModel>("OAuth/Token", new
                {
                    grant_type = "password",
                    username = UserName,
                    password = Password
                });

                // 提前一分钟过期
                Expire = DateTime.Now.AddSeconds(Token.ExpireIn);
            }
            else if (Expire.AddSeconds(600) < DateTime.Now)
            {
                // 刷新令牌
                Token = client.Post<TokenModel>("OAuth/Token", new
                {
                    grant_type = "refresh_token",
                    refresh_token = Token.RefreshToken,
                });
            }

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
        public virtual void OnResponse(HttpClient client, HttpResponseMessage response, Object state)
        {
            if ((Int32)response.StatusCode == ErrorCode)
            {
                // 马上过期
                Expire = DateTime.MinValue;
            }
        }

        /// <summary>发生错误时</summary>
        /// <param name="client">客户端</param>
        /// <param name="exception">异常</param>
        /// <param name="state">状态数据</param>
        /// <returns></returns>
        public virtual void OnError(HttpClient client, Exception exception, Object state)
        {
            // 识别ApiException
            if (exception is ApiException ae && ae.Code == ErrorCode)
            {
                // 马上过期
                Expire = DateTime.MinValue;
            }
        }
    }
}