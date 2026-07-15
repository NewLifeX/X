using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

public class ConsoleLogTests
{
    [Fact]
    public void Create_Default()
    {
        var log = new ConsoleLog();

        Assert.NotNull(log);
        Assert.True(log.UseColor);
        Assert.True(log.Enable);
    }

    [Fact]
    public void UseColor_Property()
    {
        var log = new ConsoleLog { UseColor = false };
        Assert.False(log.UseColor);

        log.UseColor = true;
        Assert.True(log.UseColor);
    }

    [Fact]
    public void Enable_Property()
    {
        var log = new ConsoleLog { Enable = false };
        Assert.False(log.Enable);
    }

    [Fact]
    public void WriteLog_NoException()
    {
        var log = new ConsoleLog();
        var ex = Record.Exception(() => log.Info("测试控制台日志"));

        Assert.Null(ex);
    }

    [Fact]
    public void WriteLog_WithFormat()
    {
        var log = new ConsoleLog();
        var ex = Record.Exception(() => log.Info("格式化 {0} {1}", "arg1", 42));

        Assert.Null(ex);
    }

    [Fact]
    public void WriteLog_AllLevels()
    {
        var log = new ConsoleLog();

        var ex = Record.Exception(() =>
        {
            log.Debug("debug");
            log.Info("info");
            log.Warn("warn");
            log.Error("error");
            log.Fatal("fatal");
        });

        Assert.Null(ex);
    }

}
