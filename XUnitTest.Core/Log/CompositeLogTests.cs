using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

public class CompositeLogTests
{
    [Fact(DisplayName = "默认构造空日志列表")]
    public void Ctor_Default()
    {
        var log = new CompositeLog();
        Assert.NotNull(log.Logs);
        Assert.Empty(log.Logs);
    }

    [Fact(DisplayName = "单日志构造")]
    public void Ctor_SingleLog()
    {
        var inner = new MemoryLog();
        var log = new CompositeLog(inner);

        Assert.Single(log.Logs);
        Assert.Same(inner, log.Logs[0]);
    }

    [Fact(DisplayName = "双日志构造")]
    public void Ctor_TwoLogs()
    {
        var log1 = new MemoryLog();
        var log2 = new MemoryLog();
        var log = new CompositeLog(log1, log2);

        Assert.Equal(2, log.Logs.Count);
    }

    [Fact(DisplayName = "Add添加日志")]
    public void AddTest()
    {
        var log = new CompositeLog();
        var inner = new MemoryLog();

        log.Add(inner);

        Assert.Single(log.Logs);
    }

    [Fact(DisplayName = "Remove移除日志")]
    public void RemoveTest()
    {
        var inner = new MemoryLog();
        var log = new CompositeLog(inner);

        log.Remove(inner);

        Assert.Empty(log.Logs);
    }

    [Fact(DisplayName = "Remove不存在的不报错")]
    public void Remove_NotExist()
    {
        var log = new CompositeLog();
        var inner = new MemoryLog();

        log.Remove(inner); // 不应抛异常
        Assert.Empty(log.Logs);
    }

    [Fact(DisplayName = "写日志转发到所有内部日志")]
    public void Write_ForwardsToAll()
    {
        var log1 = new MemoryLog();
        var log2 = new MemoryLog();
        var log = new CompositeLog(log1, log2);
        log.Level = LogLevel.All;

        log.Info("Test message {0}", 42);

        Assert.True(log1.Messages.Count > 0);
        Assert.True(log2.Messages.Count > 0);
    }

    [Fact(DisplayName = "Level设置传播到子日志")]
    public void Level_Propagates()
    {
        var log1 = new MemoryLog();
        var log2 = new MemoryLog();
        var log = new CompositeLog(log1, log2);

        log.Level = LogLevel.Error;

        Assert.Equal(LogLevel.Error, log1.Level);
        Assert.Equal(LogLevel.Error, log2.Level);
    }

    [Fact(DisplayName = "Get获取指定类型日志")]
    public void Get_ByType()
    {
        var memLog = new MemoryLog();
        var log = new CompositeLog(memLog);

        var found = log.Get<MemoryLog>();

        Assert.NotNull(found);
        Assert.Same(memLog, found);
    }

    [Fact(DisplayName = "Get获取不存在类型返回null")]
    public void Get_NotFound()
    {
        var log = new CompositeLog(new MemoryLog());

        var found = log.Get<TextFileLog>();

        Assert.Null(found);
    }

    [Fact(DisplayName = "Get递归获取嵌套日志")]
    public void Get_Recursive()
    {
        var memLog = new MemoryLog();
        var inner = new CompositeLog(memLog);
        var outer = new CompositeLog(inner);

        var found = outer.Get<MemoryLog>();

        Assert.NotNull(found);
        Assert.Same(memLog, found);
    }

    [Fact(DisplayName = "ToString包含类名")]
    public void ToStringTest()
    {
        var log = new CompositeLog(new MemoryLog());
        var str = log.ToString();

        Assert.Contains("CompositeLog", str);
    }

    #region 辅助类
    class MemoryLog : Logger
    {
        public List<String> Messages { get; } = [];

        protected override void OnWrite(LogLevel level, String format, params Object?[] args)
        {
            if (args?.Length > 0)
                Messages.Add(String.Format(format, args));
            else
                Messages.Add(format);
        }
    }
    #endregion
}
