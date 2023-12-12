namespace NewLife.Remoting;

/// <summary>Http请求事件参数</summary>
public class HttpRequestEventArgs : EventArgs
{
    /// <summary>客户端</summary>
    public HttpRequestMessage Request { get; set; } = null!;
}
