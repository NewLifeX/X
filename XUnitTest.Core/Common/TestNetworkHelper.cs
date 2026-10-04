using System.Net.Sockets;

namespace XUnitTest;

/// <summary>测试用网络辅助</summary>
internal static class TestNetworkHelper
{
    /// <summary>检查目标服务器是否可达</summary>
    /// <remarks>
    /// 访问公网站点的用例必须先过本判断：断网或代理异常时，真实请求（HTTP 默认 100 秒超时 + DNS 解析）
    /// 会把用例挂住很久，全量运行表现为“卡住不动”。探测本身限时 2 秒，不可达即静默跳过。
    /// </remarks>
    /// <param name="host">主机名</param>
    /// <param name="port">端口</param>
    /// <param name="timeoutMs">连接超时，毫秒</param>
    /// <returns>是否可达</returns>
    public static Boolean IsReachable(String host, Int32 port, Int32 timeoutMs = 2000)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs))) return false;

            client.EndConnect(result);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
