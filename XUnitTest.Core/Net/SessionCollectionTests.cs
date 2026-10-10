using System.Net;
using NewLife;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>会话集合的释放语义测试</summary>
/// <remarks>
/// 集合 Dispose 表示停机清空：清理计时器已停、会话已清。此后不得再接受登记——
/// 重建清理计时器会让服务对象图被全局计时器长期钉住（每 10 秒空转），
/// 并产出"服务已停、集合非空"的复活会话（客户端连到已停服务上，等到超时才被清理）。
/// </remarks>
[Collection("Net.C")]
public class SessionCollectionTests
{
    [Fact(DisplayName = "会话集合_已释放后登记_被拒绝")]
    public void Add_AfterDispose_Rejected()
    {
        using var server = new UdpServer { Port = 0 };
        var sessions = (SessionCollection)server.Sessions;

        // 正常登记一个，证明 Add 路径可用
        var first = new UdpSession(server, null, new IPEndPoint(IPAddress.Loopback, 12345));
        Assert.True(sessions.Add(first));
        Assert.Single(sessions);

        // 停机清空：Dispose 停计时器并清空集合
        sessions.Dispose();
        Assert.Empty(sessions);

        // 已释放集合不得再接受会话
        var second = new UdpSession(server, null, new IPEndPoint(IPAddress.Loopback, 12346));
        try
        {
            Assert.False(sessions.Add(second));
            Assert.Empty(sessions);
        }
        finally
        {
            second.TryDispose();
        }
    }
}
