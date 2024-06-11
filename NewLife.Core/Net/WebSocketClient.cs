using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net.Handlers;
using NewLife.Security;
using NewLife.Threading;
#if !NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Net;

/// <summary>WebSocket客户端</summary>
public class WebSocketClient : TcpSession
{
    #region 属性
    /// <summary>资源地址</summary>
    public Uri Uri { get; set; } = null!;

    /// <summary>WebSocket心跳间隔。默认60秒</summary>
    public TimeSpan KeepAlive { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>请求头。ws握手时可以传递Token</summary>
    public IDictionary<String, String?>? RequestHeaders { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public WebSocketClient()
    {
        // 加入WebSocket编码器，实现报文编解码
        this.Add<WebSocketCodec>();
    }

    /// <summary>实例化</summary>
    /// <param name="uri"></param>
    public WebSocketClient(Uri uri) : this()
    {
        Uri = uri;

        Remote = new NetUri(uri.ToString());
    }

    /// <summary>实例化</summary>
    /// <param name="url"></param>
    public WebSocketClient(String url) : this(new Uri(url)) { }
    #endregion

    /// <summary>打开连接，建立WebSocket请求</summary>
    /// <returns></returns>
    protected override Boolean OnOpen()
    {
        var remote = Remote;
        if (remote == null || remote.Address.IsAny() || remote.Port == 0)
        {
            remote = Remote = new NetUri(Uri.ToString());
        }

        if (!base.OnOpen()) return false;

        //// 连接必须是ws/wss协议
        //if (remote.Type != NetType.WebSocket) return false;

        //// 设置为激活
        //Active = true;

        //var rs = Handshake(this, Uri);

        //Active = false;

        var p = (Int32)KeepAlive.TotalMilliseconds;
        if (p > 0)
            _timer = new TimerX(DoPing, null, 5_000, p) { Async = true };

        return true;
    }

    /// <summary>关闭连接</summary>
    /// <param name="reason"></param>
    /// <returns></returns>
    protected override Boolean OnClose(String reason)
    {
        _timer.TryDispose();
        _timer = null;

        return base.OnClose(reason);
    }

    /// <summary>设置请求头。ws握手时可以传递Token</summary>
    /// <param name="headerName"></param>
    /// <param name="headerValue"></param>
    public void SetRequestHeader(String headerName, String? headerValue)
    {
        RequestHeaders ??= new Dictionary<String, String?>();

        RequestHeaders[headerName] = headerValue;
    }

    #region 消息收发
    /// <summary>接收WebSocket消息</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<WebSocketMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default)
    {
        var rs = await base.ReceiveAsync(cancellationToken);
        if (rs == null) return null;

        var msg = new WebSocketMessage();
        if (!msg.Read(rs)) return null;

        return msg;
    }

    /// <summary>发送消息</summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default)
    {
        //var pk = message.ToPacket();
        //Send(pk);
        SendMessage(message);

        return TaskEx.CompletedTask;
    }

    /// <summary>发送文本</summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendTextAsync(Packet data, CancellationToken cancellationToken = default)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Text,
            Payload = data,
        };

        return SendMessageAsync(msg, cancellationToken);
    }

    /// <summary>发送文本</summary>
    /// <param name="text"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendTextAsync(String text, CancellationToken cancellationToken = default) => SendTextAsync(text.GetBytes(), cancellationToken);

    /// <summary>发送二进制数据</summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendBinaryAsync(Packet data, CancellationToken cancellationToken = default)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = data,
        };

        return SendMessageAsync(msg, cancellationToken);
    }

    /// <summary>发送关闭</summary>
    /// <param name="closeStatus"></param>
    /// <param name="statusDescription"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task CloseAsync(Int32 closeStatus, String? statusDescription = null, CancellationToken cancellationToken = default)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Close,
            CloseStatus = closeStatus,
            StatusDescription = statusDescription,
        };

        return SendMessageAsync(msg, cancellationToken);
    }
    #endregion

    #region 心跳
    private TimerX? _timer;
    private void DoPing(Object? state)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Ping,
            Payload = $"Ping {DateTime.UtcNow.ToFullString()}",
        };

        SendMessage(msg);

        var p = (Int32)KeepAlive.TotalMilliseconds;
        if (_timer != null) _timer.Period = p;
    }
    #endregion

    #region 辅助
    /// <summary>握手</summary>
    /// <param name="client"></param>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static Boolean Handshake(ISocketClient client, Uri uri)
    {
        // 建立WebSocket请求
        var request = new HttpRequest
        {
            Method = "GET",
            RequestUri = uri
        };

        if (client is WebSocketClient ws && ws.RequestHeaders != null)
        {
            foreach (var item in ws.RequestHeaders)
            {
                request.Headers[item.Key] = item.Value!;
            }
        }

        request.Headers["Connection"] = "Upgrade";
        request.Headers["Upgrade"] = "websocket";
        request.Headers["Sec-WebSocket-Version"] = "13";

        var key = Rand.NextBytes(16).ToBase64();
        request.Headers["Sec-WebSocket-Key"] = key;

        // 注入链路跟踪标记
        DefaultSpan.Current?.Attach(request.Headers);

        using var span = client.Tracer?.NewSpan($"net:{client.Name}:WebSocket", uri + "");
        try
        {
            // 发送请求
            var req = request.Build();
            client.Send(req);

            // 接收响应
            var rs = client.Receive();
            if (rs == null || rs.Count == 0) return false;

            // 解析响应
            var res = new HttpResponse();
            if (!res.Parse(rs)) return false;

            //if (res.StatusCode != HttpStatusCode.OK) throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");
            if (res.StatusCode != HttpStatusCode.SwitchingProtocols) throw new Exception("WebSocket握手失败！" + res.StatusDescription);

            // 检查响应头
            if (!res.Headers.TryGetValue("Sec-WebSocket-Accept", out var accept) ||
                accept != SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes()).ToBase64())
                throw new Exception("WebSocket握手失败！");
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            client.WriteLog("WebSocket握手失败！" + ex.Message);

            client.Close("WebSocket");
            client.Dispose();

            return false;
        }

        return true;
    }
    #endregion
}
