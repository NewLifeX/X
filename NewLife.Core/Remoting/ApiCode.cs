namespace NewLife.Remoting;

/// <summary>Api接口响应代码</summary>
public class ApiCode
{
    /// <summary>成功</summary>
    public const Int32 Ok = 0;

    /// <summary>200成功。一般用于Http响应，也有部分JsonRpc使用该响应码表示成功</summary>
    public const Int32 Ok200 = 200;

    /// <summary>错误请求。客户端请求语法错误或参数无效时使用</summary>
    public const Int32 BadRequest = 400;

    /// <summary>未提供凭证。凭证无效/需要重新认证，当服务器认为客户端的认证信息（如用户名或密码）无效时，会返回401状态码，提示用户需要重新认证</summary>
    public const Int32 Unauthorized = 401;

    /// <summary>禁止访问。权限不足，用户虽然已登录，但没有被授予访问该特定资源的权限</summary>
    public const Int32 Forbidden = 403;

    /// <summary>服务器找不到请求的资源</summary>
    public const Int32 NotFound = 404;

    /// <summary>内部服务错误。通用错误</summary>
    public const Int32 InternalServerError = 500;
}