using System.Globalization;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Log;

[Collection("GlobalState")]
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

    [Fact(DisplayName = "日志消息格式化辅助：按日志语言翻译并保留参数")]
    public void FormatMessage_translates()
    {
        var log = new ConsoleLog();
        var old = Localization.LogCulture;
        try
        {
            Localization.LogCulture = CultureInfo.GetCultureInfo("zh-Hans");
            var text = Logger.FormatMessage(log, "Message body is incomplete; expected {0} bytes, read {1} bytes.", [5, 3]);
            Assert.Equal("消息体未读满：期望 5 字节，实读 3 字节", text);
        }
        finally
        {
            Localization.LogCulture = old;
        }
    }

    [Fact(DisplayName = "带前缀日志：先翻译消息再拼接前缀")]
    public void PrefixedLog_translates()
    {
        var messages = new List<String>();
        var log = new ActionLog((format, args) => messages.Add(format));
        var server = new NetServer { Log = log, LogPrefix = "Test" };
        var old = Localization.LogCulture;
        try
        {
            Localization.LogCulture = CultureInfo.GetCultureInfo("zh-Hans");
            server.WriteLog("Ready.");
        }
        finally
        {
            Localization.LogCulture = old;
        }

        Assert.Contains("Test 准备就绪！", messages);
    }

    [Fact(DisplayName = "日志扩展：InfoPrefixed 先翻译再拼前缀")]
    public void InfoPrefixed_translates()
    {
        var messages = new List<String>();
        var log = new ActionLog((format, args) => messages.Add(format));
        var old = Localization.LogCulture;
        try
        {
            Localization.LogCulture = CultureInfo.GetCultureInfo("zh-Hans");
            log.InfoPrefixed("[Test] ", "Ready.");
        }
        finally
        {
            Localization.LogCulture = old;
        }

        Assert.Equal("[Test] 准备就绪！", Assert.Single(messages));
    }

    [Fact(DisplayName = "日志扩展：级别不足时不格式化")]
    public void PrefixedLog_respects_level()
    {
        var messages = new List<String>();
        var log = new ActionLog((format, args) => messages.Add(format)) { Level = LogLevel.Warn };
        log.InfoPrefixed("[Test] ", "Ready.");
        Assert.Empty(messages);
    }
}
