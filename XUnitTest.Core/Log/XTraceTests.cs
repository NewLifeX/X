using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

public class XTraceTests
{
    [Fact]
    public void WriteLine_NoException()
    {
        var ex = Record.Exception(() =>
        {
            XTrace.WriteLine("测试日志消息");
            XTrace.WriteLine("格式化 {0} {1}", "arg1", 42);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void WriteLine_Null()
    {
        var ex = Record.Exception(() =>
        {
            XTrace.WriteLine((String?)null);
            XTrace.WriteLine((String?)null, null);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void WriteException_NoException()
    {
        var ex = Record.Exception(() =>
        {
            XTrace.WriteException(new InvalidOperationException("测试异常"));
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Log_Property()
    {
        var original = XTrace.Log;

        try
        {
            var customLog = new ActionLog(XTrace.WriteLine);
            XTrace.Log = customLog;
            Assert.Same(customLog, XTrace.Log);
        }
        finally
        {
            XTrace.Log = original;
        }
    }

    [Fact]
    public void Log_DefaultNotNull()
    {
        Assert.NotNull(XTrace.Log);
    }

    [Fact]
    public void Debug_Property()
    {
        var original = XTrace.Debug;
        try
        {
            XTrace.Debug = true;
            Assert.True(XTrace.Debug);

            XTrace.Debug = false;
            Assert.False(XTrace.Debug);
        }
        finally
        {
            XTrace.Debug = original;
        }
    }
}
