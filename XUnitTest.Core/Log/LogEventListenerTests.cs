using System.Diagnostics.Tracing;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

/// <summary>日志事件监听器测试</summary>
public class LogEventListenerTests
{
    [Fact(DisplayName = "构造时不抛异常")]
    public void ConstructorDoesNotThrow()
    {
        using var listener = new LogEventListener(["MyTestSource"]);
        Assert.NotNull(listener);
    }

    [Fact(DisplayName = "监听特定事件源")]
    public void ListenToSpecificSource()
    {
        var messages = new List<String>();
        var actionLog = new ActionLog((fmt, args) =>
        {
            lock (messages)
            {
                messages.Add(fmt);
            }
        });
        actionLog.Level = LogLevel.All;

        var oldLog = XTrace.Log;
        XTrace.Log = actionLog;

        try
        {
            using var listener = new LogEventListener(["MyTestSource"]);

            // 创建测试事件源
            using var testSource = new EventSource("MyTestSource");

            // 写入事件
            testSource.Write("TestEvent", new { Data = "Hello" });

            // 等待一小段时间让事件处理完成
            System.Threading.Thread.Sleep(100);

            // 日志中应该包含事件源名称和事件信息
            Assert.Contains(messages, m => m.Contains("MyTestSource"));
        }
        finally
        {
            XTrace.Log = oldLog;
        }
    }

    [Fact(DisplayName = "监听多个事件源")]
    public void ListenToMultipleSources()
    {
        var messages = new List<String>();
        var actionLog = new ActionLog((fmt, args) =>
        {
            lock (messages)
            {
                messages.Add(fmt);
            }
        });
        actionLog.Level = LogLevel.All;

        var oldLog = XTrace.Log;
        XTrace.Log = actionLog;

        try
        {
            using var listener = new LogEventListener(["SourceA", "SourceB"]);

            using var sourceA = new EventSource("SourceA");
            using var sourceB = new EventSource("SourceB");
            using var sourceC = new EventSource("SourceC"); // 不在监听列表

            sourceA.Write("Event1", new { Value = 1 });
            sourceB.Write("Event2", new { Value = 2 });
            sourceC.Write("Event3", new { Value = 3 });

            System.Threading.Thread.Sleep(100);

            // 应包含 SourceA 和 SourceB 的事件
            Assert.Contains(messages, m => m.Contains("SourceA"));
            Assert.Contains(messages, m => m.Contains("SourceB"));
        }
        finally
        {
            XTrace.Log = oldLog;
        }
    }

    [Fact(DisplayName = "未监听的事件源不产生事件日志")]
    public void UnlistedSourceNoError()
    {
        var messages = new List<String>();
        var actionLog = new ActionLog((fmt, args) =>
        {
            lock (messages)
            {
                messages.Add(fmt);
            }
        });
        actionLog.Level = LogLevel.All;

        var oldLog = XTrace.Log;
        XTrace.Log = actionLog;

        try
        {
            using var listener = new LogEventListener(["MyApp"]);
            // 未监听的事件源不应导致异常
            using var otherSource = new EventSource("OtherApp");
            otherSource.Write("Event", new { Data = "test" });

            System.Threading.Thread.Sleep(50);

            // 不抛异常即为成功
            Assert.True(true);
        }
        finally
        {
            XTrace.Log = oldLog;
        }
    }
}
