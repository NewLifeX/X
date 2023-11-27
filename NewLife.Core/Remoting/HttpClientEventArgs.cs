namespace NewLife.Remoting;

/// <summary>Http客户端事件参数</summary>
public class HttpClientEventArgs : EventArgs
{
    /// <summary>客户端</summary>
    public HttpClient Client { get; set; } = null!;
}
