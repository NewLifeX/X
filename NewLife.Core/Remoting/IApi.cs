namespace NewLife.Remoting;

/// <summary>Api接口</summary>
/// <remarks>
/// 在基于令牌Token的无状态验证模式中，可以借助Token重写IApiHandler.Prepare，来达到同一个Token共用相同的IApiSession.Items
/// </remarks>
public interface IApi
{
    /// <summary>会话</summary>
    IApiSession Session { get; set; }
}