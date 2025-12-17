using NewLife;
using Xunit;

namespace XUnitTest.Common;

public class RuntimeTests
{
    [Fact(DisplayName = "ClientId 基本格式")]
    public void ClientId_HasProcessSuffix()
    {
        var cid = Runtime.ClientId;
        Assert.False(String.IsNullOrWhiteSpace(cid));
        var pid = Runtime.ProcessId.ToString();
        Assert.EndsWith("@" + pid, cid);
    }

    [Fact(DisplayName = "ClientId 使用本地IPv4或仅@pid")]
    public void ClientId_UsesIPv4IfAvailable()
    {
        var ip = NetHelper.MyIP();
        var cid = Runtime.ClientId;

        if (ip != null)
        {
            Assert.StartsWith(ip.ToString() + "@", cid);
        }
        else
        {
            // 网络未就绪或无IPv4时，返回 @pid
            Assert.Equal("@" + Runtime.ProcessId, cid);
        }
    }
}
