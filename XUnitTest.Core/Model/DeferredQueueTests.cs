using NewLife.Model;
using Xunit;

namespace XUnitTest.Model;

public class DeferredQueueTests
{
    [Fact(DisplayName = "基本TryAdd")]
    public void BasicTryAdd()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };

        Assert.True(queue.TryAdd("key1", "value1"));
        Assert.Equal(1, queue.Count);
    }

    [Fact(DisplayName = "重复key不添加")]
    public void TryAdd_DuplicateKey()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };

        Assert.True(queue.TryAdd("key1", "value1"));
        Assert.False(queue.TryAdd("key1", "value2"));
        Assert.Equal(1, queue.Count);
    }

    [Fact(DisplayName = "TryRemove移除")]
    public void TryRemoveTest()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };
        queue.TryAdd("key1", "value1");

        Assert.True(queue.TryRemove("key1"));
        Assert.Equal(0, queue.Count);
    }

    [Fact(DisplayName = "TryRemove不存在返回false")]
    public void TryRemove_NotExist()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };

        Assert.False(queue.TryRemove("nonexist"));
    }

    [Fact(DisplayName = "GetOrAdd获取或创建")]
    public void GetOrAddTest()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };

        var obj = queue.GetOrAdd<TestObj>("key1");
        Assert.NotNull(obj);

        // 再次获取应返回同一对象
        var obj2 = queue.GetOrAdd<TestObj>("key1");
        Assert.Same(obj, obj2);
    }

    [Fact(DisplayName = "GetOrAdd使用工厂方法")]
    public void GetOrAdd_WithFactory()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };

        var obj = queue.GetOrAdd<TestObj>("key1", k => new TestObj { Name = k });
        Assert.NotNull(obj);
        Assert.Equal("key1", obj!.Name);
    }

    [Fact(DisplayName = "Commit提交")]
    public void CommitTest()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };

        queue.GetOrAdd<TestObj>("key1");
        queue.Commit("key1"); // 不应抛异常
    }

    [Fact(DisplayName = "Flush同步处理")]
    public void FlushTest()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };
        queue.TryAdd("a", "1");
        queue.TryAdd("b", "2");

        queue.Flush();

        Assert.True(queue.ProcessedCount > 0);
        Assert.Equal(0, queue.Count);
    }

    [Fact(DisplayName = "Times记录操作次数")]
    public void TimesTest()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };

        queue.TryAdd("a", "1");
        queue.TryAdd("b", "2");
        queue.GetOrAdd<TestObj>("c");

        Assert.True(queue.Times >= 3);
    }

    [Fact(DisplayName = "Name默认值")]
    public void NameTest()
    {
        using var queue = new TestDeferredQueue();
        Assert.Equal("TestDeferred", queue.Name);
    }

    [Fact(DisplayName = "Entities字典可访问")]
    public void EntitiesTest()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };
        queue.TryAdd("key1", "value1");

        Assert.True(queue.Entities.ContainsKey("key1"));
    }

    [Fact(DisplayName = "Finish回调触发")]
    public void FinishCallbackTest()
    {
        using var queue = new TestDeferredQueue { Period = 60_000 };
        var finished = false;
        queue.Finish = list => finished = true;

        queue.TryAdd("a", "1");
        queue.Flush();

        Assert.True(finished);
    }

    [Fact(DisplayName = "Error回调触发")]
    public void ErrorCallbackTest()
    {
        using var queue = new ErrorDeferredQueue { Period = 60_000 };
        Exception? caught = null;
        queue.Error = (list, ex) => caught = ex;

        queue.TryAdd("a", "1");
        queue.Flush();

        Assert.NotNull(caught);
    }

    [Fact(DisplayName = "Dispose后清空")]
    public void DisposeTest()
    {
        var queue = new TestDeferredQueue { Period = 60_000 };
        queue.TryAdd("a", "1");
        queue.Dispose();
        // Dispose成功不抛异常
    }

    #region 辅助类
    class TestObj
    {
        public String? Name { get; set; }
    }

    class TestDeferredQueue : DeferredQueue
    {
        public Int32 ProcessedCount { get; private set; }

        public override Int32 Process(IList<Object> list)
        {
            ProcessedCount += list.Count;
            return list.Count;
        }
    }

    class ErrorDeferredQueue : DeferredQueue
    {
        public override Int32 Process(IList<Object> list)
        {
            throw new InvalidOperationException("Test error");
        }
    }
    #endregion
}
