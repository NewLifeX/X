using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

public class LoggerTests
{
    [Fact]
    public void FormatDateTime()
    {
        var dt = DateTime.Now;
        var log = new ConsoleLog();
        var str = log.Format("[{0:MM-dd}] {1} Test", [dt, Environment.MachineName]);
        var str2 = $"[{dt.Month:00}-{dt.Day:00}] {Environment.MachineName} Test";
        Assert.Equal(str2, str);
    }
}
