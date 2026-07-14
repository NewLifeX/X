namespace NewLife.Http;

/// <summary>Http控制器接口</summary>
/// <remarks>
/// 实现此接口的控制器可自动获取当前 <see cref="IHttpContext"/> 上下文。
/// 在 <see cref="ControllerHandler"/> 处理请求时，若控制器实现了此接口，
/// 会自动将当前上下文注入到 <see cref="Context"/> 属性中。
/// 
/// 使用方式：
/// <code>
/// public class MyController : IHttpController
/// {
///     public IHttpContext? Context { get; set; }
///     
///     public String Info()
///     {
///         var ua = Context?.Request.Headers["User-Agent"].FirstOrDefault();
///         return $"Hello, your UA is {ua}";
///     }
/// }
/// </code>
/// </remarks>
public interface IHttpController
{
    /// <summary>当前Http上下文</summary>
    IHttpContext? Context { get; set; }
}
