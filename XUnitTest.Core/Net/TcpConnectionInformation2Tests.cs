using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net
{
    public class TcpConnectionInformation2Tests
    {
        [Fact]
        public void GetAllTcp()
        {
            var tcps = TcpConnectionInformation2.GetAllTcpConnections();
            Assert.NotNull(tcps);
            Assert.True(tcps.Length > 0);
            Assert.Contains(tcps, e => e.ProcessId > 0);

            foreach (var item in tcps)
            {
                XTrace.WriteLine("{0}\t{1}\t{2}\t{3}", item.LocalEndPoint, item.RemoteEndPoint, item.State, item.ProcessId);
            }
        }
    }
}