namespace NewLife.Remoting;

/// <summary>Api接口响应代码</summary>
public class ApiCode
{
    /// <summary>成功</summary>
    public const Int32 Ok = 0;

    /// <summary>200成功。一般用于Http响应，也有部分JsonRpc使用该响应码表示成功</summary>
    public const Int32 Ok200 = 200;

    /// <summary>未经许可。一般是指未登录</summary>
    public const Int32 Unauthorized = 401;

    /// <summary>禁止访问。一般是只已登录但无权访问</summary>
    public const Int32 Forbidden = 403;

    /// <summary>找不到</summary>
    public const Int32 NotFound = 404;

    /// <summary>内部服务错误。通用错误</summary>
    public const Int32 InternalServerError = 500;
}