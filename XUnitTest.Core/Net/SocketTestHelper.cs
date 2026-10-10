using System.Net.Sockets;

namespace XUnitTest.Net;

/// <summary>网络测试的套接字辅助</summary>
internal static class SocketTestHelper
{
    /// <summary>限时等待连接</summary>
    /// <remarks>
    /// 裸套接字的 <c>Accept</c> 没有超时：一旦客户端建连失败（端口占用、绑定失败等），用例会永久挂起，
    /// 把整个测试进程拖住直到人工终止（全量运行中曾实际发生）。测试内一律走本方法，让失败变成可诊断的异常。
    /// </remarks>
    /// <param name="listener">监听套接字</param>
    /// <param name="timeoutMs">等待超时，毫秒</param>
    /// <param name="detail">超时时附加的诊断信息（如入队任务状态），便于定位“为什么没连上”</param>
    /// <returns>已接受的连接</returns>
    public static async Task<Socket> AcceptWithinAsync(this Socket listener, Int32 timeoutMs = 10_000, Func<String>? detail = null)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            return await listener.AcceptAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            var extra = detail?.Invoke();
            var message = $"等待客户端连接超时 {timeoutMs}ms，监听端点 {listener.LocalEndPoint}";
            if (!String.IsNullOrEmpty(extra)) message += "；" + extra;

            throw new TimeoutException(message);
        }
    }
}
