using NewLife.Net;

namespace Zero.EchoServer;

/// <summary>定义服务端，用于管理所有网络会话</summary>
class EchoNetServer : NetServer<EchoSession>
{
}

/// <summary>定义会话。每一个远程连接唯一对应一个网络会话，再次重复收发信息</summary>
class EchoSession : NetSession<EchoNetServer>
{
    /// <summary>收到客户端数据</summary>
    /// <param name="e"></param>
    protected override void OnReceive(ReceivedEventArgs e)
    {
        if (e.Packet.Length == 0) return;

        // 把收到的数据发回去
        Send(e.Packet);
    }
}