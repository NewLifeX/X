using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Log;

namespace Zero.Server;

static class ClientTest
{
    public static async void TcpClientTest()
    {
        await Task.Delay(1_000);

        var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 12345);
        var ns = client.GetStream();

        // 接收服务端握手
        var buf = new Byte[1024];
        var count = await ns.ReadAsync(buf);
        XTrace.WriteLine("<={0}", buf.ToStr(null, 0, count));

        // 发送数据
        var str = "Hello NewLife";
        XTrace.WriteLine("=>{0}", str);
        await ns.WriteAsync(str.GetBytes());

        // 接收数据
        count = await ns.ReadAsync(buf);
        XTrace.WriteLine("<={0}", buf.ToStr(null, 0, count));
    }

    public static async void UdpClientTest()
    {
        await Task.Delay(1_500);

        var client = new UdpClient();
        client.Connect("127.0.0.1", 12345);

        // 发送数据
        var str = "Hello NewLife";
        XTrace.WriteLine("=>{0}", str);
        await client.SendAsync(str.GetBytes());

        // 接收数据
        var result = await client.ReceiveAsync();
        XTrace.WriteLine("<={0}", result.Buffer.ToStr());
    }
}
