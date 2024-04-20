using System.Net.WebSockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;

namespace Zero.HttpServer;

static class ClientTest
{
    public static async Task HttpClientTest()
    {
        await Task.Delay(1_000);

        // 基础请求
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var html = await client.GetStringAsync("/");
        XTrace.WriteLine(html);

        // Api接口请求
        var http = new ApiHttpClient("http://localhost:8080");

        // 请求接口，返回data部分
        var rs = await http.GetAsync<String>("/user", new { act = "Delete", uid = 1234 });
        XTrace.WriteLine(rs);

        var rs2 = await http.GetAsync<Object>("/api/info", new { state = "test" });
        XTrace.WriteLine(rs2.ToJson(true));
    }

    public static async Task WebSocketClientTest()
    {
        await Task.Delay(5_000);

        var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("ws://127.0.0.1:8080/ws"), default);
        await client.SendAsync("Hello NewLife".GetBytes(), System.Net.WebSockets.WebSocketMessageType.Text, true, default);

        var buf = new Byte[1024];
        var rs = await client.ReceiveAsync(buf, default);
        XTrace.WriteLine(new Packet(buf, 0, rs.Count).ToStr());

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "通信完成", default);
        XTrace.WriteLine("Close [{0}] {1}", client.CloseStatus, client.CloseStatusDescription);
    }
}
