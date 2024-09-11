using System.Net.WebSockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;
using NewLife.Serialization;

namespace Zero.HttpServer;

static class ClientTest
{
    public static async Task HttpClientTest()
    {
        await Task.Delay(1_000);
        XTrace.WriteLine("");
        XTrace.WriteLine("Http客户端开始连接！");

        // 基础请求
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.4:8080")
        };

        var html = await client.GetStringAsync("/");
        XTrace.WriteLine(html);

        // Api接口请求
        var http = new ApiHttpClient("http://127.0.0.5:8080");
        http.Log = XTrace.Log;

        // 请求接口，返回data部分
        var rs = await http.GetAsync<String>("/user", new { act = "Delete", uid = 1234 });
        XTrace.WriteLine(rs);

        var rs2 = await http.GetAsync<Object>("/api/info", new { state = "test" });
        XTrace.WriteLine(rs2.ToJson(true));

        // 关闭连接
        client.Dispose();
    }

    public static async Task WebSocketTest()
    {
        await Task.Delay(2_000);
        XTrace.WriteLine("");
        XTrace.WriteLine("WebSocket客户端开始连接！");

        var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("ws://127.0.0.6:8080/ws"), default);
        await client.SendAsync("Hello NewLife".GetBytes(), System.Net.WebSockets.WebSocketMessageType.Text, true, default);

        var buf = new Byte[1024];
        var rs = await client.ReceiveAsync(buf, default);
        XTrace.WriteLine(new Span<Byte>(buf, 0, rs.Count).ToStr());

        // 关闭连接
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "通信完成", default);
        XTrace.WriteLine("Close [{0}] {1}", client.CloseStatus, client.CloseStatusDescription);

        client.Dispose();
    }

    public static async Task WebSocketClientTest()
    {
        await Task.Delay(3_000);
        XTrace.WriteLine("");
        XTrace.WriteLine("WebSocketClient开始连接！");

        var client = new WebSocketClient("ws://127.0.0.7:8080/ws")
        {
            Name = "小ws客户",
            Log = XTrace.Log
        };
        if (client is TcpSession tcp) tcp.MaxAsync = 0;

        await client.SendTextAsync("Hello NewLife");

        using var rs = await client.ReceiveMessageAsync(default);
        client.WriteLog(rs.Payload.ToStr());

        await Task.Delay(6_000);

        // 关闭连接
        await client.CloseAsync(1000, "通信完成", default);
        client.WriteLog("Close");

        using var rs2 = await client.ReceiveMessageAsync(default);
        client.WriteLog("Close [{0}] {1}", rs2.CloseStatus, rs2.StatusDescription);

        client.Dispose();
    }
}
