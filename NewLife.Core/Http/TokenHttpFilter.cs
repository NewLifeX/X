using System.Diagnostics;
using System.Net.Http.Headers;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.Http;

/// <summary>Http令牌过滤器，请求前加上令牌，请求后拦截401/403</summary>
public class TokenHttpFilter : IHttpFilter
{
    #region 属性
    /// <summary>用户</summary>
    public String UserName { get; set; }

    /// <summary>密钥</summary>
    public String Password { get; set; }

    /// <summary>客户端唯一标识。一般是IP@进程</summary>
    public String ClientId { get; set; }

    /// <summary>安全密钥。keyName$keyValue</summary>
    /// <remarks>
    /// 公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，可以写死在代码里面。
    /// 密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。
    /// </remarks>
    public String SecurityKey { get; set; }

    /// <summary>申请令牌动作名，默认 OAuth/Token</summary>
    public String Action { get; set; } = "OAuth/Token";

    /// <summary>令牌信息</summary>
    public TokenModel Token { get; set; }

    /// <summary>令牌有效期</summary>
    public DateTime Expire { get; set; }

    private DateTime _refresh;

    /// <summary>清空令牌的错误码。默认401和403</summary>
    public IList<Int32> ErrorCodes { get; set; } = new List<Int32> { ApiCode.Unauthorized, ApiCode.Forbidden };
    #endregion

    /// <summary>实例化令牌过滤器</summary>
    public TokenHttpFilter() => ValidClientId();

    private void ValidClientId()
    {
        try
        {
            // 刚启动时可能还没有拿到本地IP
            if (ClientId.IsNullOrEmpty() || ClientId[0] == '@')
                ClientId = $"{NetHelper.MyIP()}@{Process.GetCurrentProcess().Id}";
        }
        catch { }
    }

    /// <summary>请求前</summary>
    /// <param name="client">客户端</param>
    /// <param name="request">请求消息</param>
    /// <param name="state">状态数据</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task OnRequest(HttpClient client, HttpRequestMessage request, Object state, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization != null) return;

        var path = client.BaseAddress == null ? request.RequestUri.AbsoluteUri : request.RequestUri.OriginalString;
        if (path.StartsWithIgnoreCase(Action.EnsureStart("/"))) return;

        // 申请令牌。没有令牌，或者令牌已过期
        if (Token == null || Expire < DateTime.Now)
        {
            Token = await SendAuth(client, cancellationToken);

            // 过期时间和刷新令牌的时间
            Expire = DateTime.Now.AddSeconds(Token.ExpireIn);
            _refresh = DateTime.Now.AddSeconds(Token.ExpireIn / 2);
        }

        // 刷新令牌。要求已有令牌，且未过期，且达到了刷新时间
        if (Token != null && Expire > DateTime.Now && _refresh < DateTime.Now)
        {
            try
            {
                Token = await SendRefresh(client, cancellationToken);

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

    /// <summary>发起密码认证请求</summary>
    /// <param name="client"></param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected virtual async Task<TokenModel> SendAuth(HttpClient client, CancellationToken cancellationToken)
    {
        ValidClientId();

        var pass = EncodePassword(UserName, Password);
        return await client.PostAsync<TokenModel>(Action, new
        {
            grant_type = "password",
            username = UserName,
            password = pass,
            clientId = ClientId,
        }, cancellationToken);
    }

    /// <summary>发起刷新令牌请求</summary>
    /// <param name="client"></param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected virtual async Task<TokenModel> SendRefresh(HttpClient client, CancellationToken cancellationToken)
    {
        ValidClientId();

        return await client.PostAsync<TokenModel>(Action, new
        {
            grant_type = "refresh_token",
            refresh_token = Token.RefreshToken,
            clientId = ClientId,
        }, cancellationToken);
    }

    /// <summary>编码密码，在传输中保护安全，一般使用RSA加密</summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    protected virtual String EncodePassword(String username, String password)
    {
        if (password.IsNullOrEmpty()) return password;

        var key = SecurityKey;
        if (!key.IsNullOrEmpty())
        {
            var name = "";
            var p = key.IndexOf('$');
            if (p >= 0)
            {
                name = key[..p];
                key = key[(p + 1)..];
            }

            // RSA公钥加密
            var pass = RSAHelper.Encrypt(password.GetBytes(), key).ToBase64();
            password = $"$rsa${name}${pass}";
        }

        return password;
    }

    /// <summary>获取响应后</summary>
    /// <param name="client">客户端</param>
    /// <param name="response">响应消息</param>
    /// <param name="state">状态数据</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual Task OnResponse(HttpClient client, HttpResponseMessage response, Object state, CancellationToken cancellationToken)
    {
        var code = (Int32)response.StatusCode;
        if (ErrorCodes.Contains(code))
        {
            // 马上过期
            Expire = DateTime.MinValue;
        }

#if NET45
        return Task.FromResult(0);
#else
        return Task.CompletedTask;
#endif
    }

    /// <summary>发生错误时</summary>
    /// <param name="client">客户端</param>
    /// <param name="exception">异常</param>
    /// <param name="state">状态数据</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual Task OnError(HttpClient client, Exception exception, Object state, CancellationToken cancellationToken)
    {
        // 识别ApiException
        if (exception is ApiException ae && ErrorCodes.Contains(ae.Code))
        {
            // 马上过期
            Expire = DateTime.MinValue;
        }

#if NET45
        return Task.FromResult(0);
#else
        return Task.CompletedTask;
#endif
    }
}