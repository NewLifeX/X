using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Log;
using NewLife.Net;

namespace Zero.Server;

static class ClientTest
{
    /// <summary>TcpClient连接NetServer</summary>
    public static async void TcpClientTest()
    {
        await Task.Delay(1_000);
        XTrace.WriteLine("");
        XTrace.WriteLine("Tcp客户端开始连接！");

        // 连接服务端
        var client = new TcpClient();
        await client.ConnectAsync("127.0.0.2", 12345);
        var ns = client.GetStream();

        // 接收服务端握手。连接服务端后，服务端会主动发送数据
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

        // 关闭连接
        client.Close();
    }

    /// <summary>UdpClient连接NetServer</summary>
    public static async void UdpClientTest()
    {
        await Task.Delay(2_000);
        XTrace.WriteLine("");
        XTrace.WriteLine("Udp客户端开始连接！");

        // 无需连接服务端
        var uri = new NetUri("udp://127.0.0.3:12345");
        var client = new UdpClient();
        //client.Connect(uri.EndPoint);

        // 发送数据。服务端收到第一个包才建立会话
        var str = "Hello NewLife";
        XTrace.WriteLine("=>{0}", str);
        var buf = str.GetBytes();
        await client.SendAsync(buf, buf.Length, uri.EndPoint);

        // 接收数据。建立会话后，服务端会主动发送握手数据
        var result = await client.ReceiveAsync();
        XTrace.WriteLine("<={0}", result.Buffer.ToStr());

        result = await client.ReceiveAsync();
        XTrace.WriteLine("<={0}", result.Buffer.ToStr());

        // 发送空包。服务端收到空包后，会关闭连接
        buf = new Byte[0];
        await client.SendAsync(buf, buf.Length, uri.EndPoint);

        // 关闭连接
        client.Close();
    }

    /// <summary>ISocketClient(TCP)连接NetServer</summary>
    public static async void TcpSessionTest()
    {
        await Task.Delay(3_000);
        XTrace.WriteLine("");
        XTrace.WriteLine("Tcp会话开始连接！");

        // 创建客户端，关闭默认的异步模式（MaxAsync=0）
        var uri = new NetUri("tcp://127.0.0.4:12345");
        var client = uri.CreateRemote();
        client.Name = "小tcp客户";
        client.Log = XTrace.Log;
        if (client is TcpSession tcp) tcp.MaxAsync = 0;

        // 接收服务端握手。内部自动建立连接
        var rs = await client.ReceiveAsync(default);
        client.WriteLog("<={0}", rs.ToStr());

        // 发送数据
        var str = "Hello NewLife";
        client.WriteLog("=>{0}", str);
        client.Send(str);

        // 接收数据
        rs = await client.ReceiveAsync(default);
        client.WriteLog("<={0}", rs.ToStr());

        // 关闭连接
        client.Close("测试完成");
    }

    /// <summary>ISocketClient(UDP)连接NetServer</summary>
    public static async void UdpSessionTest()
    {
        await Task.Delay(4_000);
        XTrace.WriteLine("");
        XTrace.WriteLine("Udp会话开始连接！");

        // 创建客户端，关闭默认的异步模式（MaxAsync=0）
        var uri = new NetUri("udp://127.0.0.4:12345");
        var client = uri.CreateRemote();
        client.Name = "小udp客户";
        client.Log = XTrace.Log;
        if (client is UdpServer udp) udp.MaxAsync = 0;

        // 发送数据。服务端收到第一个包才建立会话
        var str = "Hello NewLife";
        client.WriteLog("=>{0}", str);
        client.Send(str);

        // 接收服务端握手
        var rs = await client.ReceiveAsync(default);
        client.WriteLog("<={0}", rs.ToStr());

        // 接收数据
        rs = await client.ReceiveAsync(default);
        client.WriteLog("<={0}", rs.ToStr());

        // 关闭连接
        client.Close("测试完成");
    }
}
