using NewLife;
using NewLife.Model;
using NewLife.Net;
using Zero.TcpServer.Handlers;

namespace Zero.TcpServer;

/// <summary>定义服务端，用于管理所有网络会话</summary>
class MyNetServer : NetServer<MyNetSession>
{
}

/// <summary>定义会话。每一个远程连接唯一对应一个网络会话，再次重复收发信息</summary>
class MyNetSession : NetSession<MyNetServer>
{
    private IList<IMsgHandler> _handlers;

    /// <summary>客户端连接</summary>
    protected override void OnConnected()
    {
        _handlers = ServiceProvider.GetServices<IMsgHandler>().ToList();

        // 发送欢迎语
        Send($"Welcome to visit {Environment.MachineName}!  [{Remote}]\r\n");

        base.OnConnected();
    }

    /// <summary>客户端断开连接</summary>
    protected override void OnDisconnected(String reason)
    {
        WriteLog("客户端{0}已经断开连接啦。{1}", Remote, reason);

        base.OnDisconnected(reason);
    }

    /// <summary>收到客户端数据</summary>
    /// <param name="e"></param>
    protected override void OnReceive(ReceivedEventArgs e)
    {
        WriteLog("收到：{0}", e.Packet.ToStr());

        //todo 这里是业务处理核心，解开数据包e.Packet并进行业务处理

        // 把收到的数据发回去
        Send(e.Packet);
    }

    /// <summary>出错</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void OnError(Object sender, ExceptionEventArgs e)
    {
        WriteLog("[{0}]错误：{1}", e.Action, e.Exception?.GetTrue().Message);

        base.OnError(sender, e);
    }
}