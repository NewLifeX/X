using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

/// <summary>ActionLog委托日志测试</summary>
public class ActionLogTests
{
    [Fact(DisplayName = "写日志调用委托")]
    public void WriteInvokesDelegate()
    {
        String? captured = null;
        Object?[]? capturedArgs = null;
        var log = new ActionLog((fmt, args) =>
        {
            captured = fmt;
            capturedArgs = args;
        });

        log.Info("Hello {0}", "World");

        Assert.NotNull(captured);
        Assert.Contains("Hello", captured);
    }

    [Fact(DisplayName = "Method属性")]
    public void MethodProperty()
    {
        Action<String, Object?[]> action = (_, _) => { };
        var log = new ActionLog(action);
        Assert.Same(action, log.Method);
    }

    [Fact(DisplayName = "ToString返回委托信息")]
    public void ToStringReturnsMethodInfo()
    {
        var log = new ActionLog((_, _) => { });
        var str = log.ToString();
        Assert.NotNull(str);
    }

    [Fact(DisplayName = "Level过滤")]
    public void LevelFiltering()
    {
        var messages = new List<String>();
        var log = new ActionLog((fmt, _) => messages.Add(fmt));
        log.Level = LogLevel.Warn;

        log.Debug("debug");
        log.Info("info");
        log.Warn("warn");
        log.Error("error");

        // Debug和Info应被过滤
        Assert.DoesNotContain("debug", messages);
        Assert.DoesNotContain("info", messages);
        Assert.Contains(messages, m => m.Contains("warn"));
        Assert.Contains(messages, m => m.Contains("error"));
    }
}
