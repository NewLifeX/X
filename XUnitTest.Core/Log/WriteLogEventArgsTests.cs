using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

/// <summary>写日志事件参数测试</summary>
public class WriteLogEventArgsTests
{
    [Fact(DisplayName = "默认构造函数")]
    public void DefaultConstructor()
    {
        var args = new WriteLogEventArgs();
        Assert.Null(args.Message);
        Assert.Null(args.Exception);
        Assert.Equal(0, args.ThreadID);
    }

    [Fact(DisplayName = "Set设置级别")]
    public void SetLevel()
    {
        var args = new WriteLogEventArgs();
        var result = args.Set(LogLevel.Warn);

        Assert.Same(args, result);
        Assert.Equal(LogLevel.Warn, args.Level);
    }

    [Fact(DisplayName = "Set设置消息和异常")]
    public void SetMessageAndException()
    {
        var args = new WriteLogEventArgs();
        var ex = new InvalidOperationException("test");
        var result = args.Set("hello {0}", ex);

        Assert.Same(args, result);
        Assert.Equal("hello {0}", args.Message);
        Assert.Same(ex, args.Exception);
    }

    [Fact(DisplayName = "Current线程静态实例")]
    public void CurrentThreadStatic()
    {
        var current = WriteLogEventArgs.Current;
        Assert.NotNull(current);

        // 同一线程应返回同一实例
        var current2 = WriteLogEventArgs.Current;
        Assert.Same(current, current2);
    }

    [Fact(DisplayName = "不同线程不同实例")]
    public void DifferentThreadDifferentInstance()
    {
        var current1 = WriteLogEventArgs.Current;
        WriteLogEventArgs? current2 = null;

        var t = new Thread(() => current2 = WriteLogEventArgs.Current);
        t.Start();
        t.Join();

        Assert.NotNull(current2);
        Assert.NotSame(current1, current2);
    }

    [Fact(DisplayName = "属性可读写")]
    public void PropertiesReadWrite()
    {
        var args = new WriteLogEventArgs
        {
            Level = LogLevel.Error,
            Message = "test message",
            ThreadID = 42,
            IsPool = true,
            IsWeb = false,
            ThreadName = "Worker",
            TaskID = 100,
            Time = new DateTime(2025, 1, 1)
        };

        Assert.Equal(LogLevel.Error, args.Level);
        Assert.Equal("test message", args.Message);
        Assert.Equal(42, args.ThreadID);
        Assert.True(args.IsPool);
        Assert.False(args.IsWeb);
        Assert.Equal("Worker", args.ThreadName);
        Assert.Equal(100, args.TaskID);
        Assert.Equal(new DateTime(2025, 1, 1), args.Time);
    }
}
