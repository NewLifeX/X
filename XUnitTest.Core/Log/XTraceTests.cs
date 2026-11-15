using System.Reflection;
using NewLife;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

/// <summary>改写进程级全局状态（XTrace.Log），与其它用例串行执行</summary>
[Collection("GlobalState")]
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

    [Fact]
    public void LogPath_NotSuppressDebugFromConfig()
    {
        var debug = XTrace.Debug;
        var logPath = XTrace.LogPath;

        try
        {
            ResetSetting();

            // 先显式设置日志目录，再读 Debug，仍应取到配置值
            XTrace.LogPath = Path.Combine(Path.GetTempPath(), "xtrace-test");
            Assert.Equal(Setting.Current.Debug, XTrace.Debug);
        }
        finally
        {
            ResetSetting();
            XTrace.Debug = debug;
            XTrace.LogPath = logPath;
        }
    }

    [Fact]
    public void Debug_NotSuppressLogPathFromConfig()
    {
        var debug = XTrace.Debug;
        var logPath = XTrace.LogPath;

        try
        {
            ResetSetting();

            // 先显式设置调试开关，再读日志目录，仍应取到配置值
            XTrace.Debug = !Setting.Current.Debug;
            Assert.Equal(Setting.Current.LogPath, XTrace.LogPath);
        }
        finally
        {
            ResetSetting();
            XTrace.Debug = debug;
            XTrace.LogPath = logPath;
        }
    }

    /// <summary>复位延迟加载状态，模拟进程内首次访问。字段改名时用例会失败提醒同步</summary>
    private static void ResetSetting()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var type = typeof(XTrace);

        type.GetField("_settingLoaded", flags)!.SetValue(null, 0);
        type.GetField("_setting", flags)!.SetValue(null, null);
        type.GetField("_debug", flags)!.SetValue(null, null);
        type.GetField("_logPath", flags)!.SetValue(null, null);
    }
}
