using NewLife;
using Xunit;

namespace XUnitTest.Common;

/// <summary>DisposeBase可销毁基类测试</summary>
public class DisposeBaseTests
{
    /// <summary>测试用的可销毁子类</summary>
    private class TestDisposable : DisposeBase
    {
        public Boolean ManagedDisposed { get; private set; }
        public Int32 DisposeCallCount { get; private set; }

        protected override void Dispose(Boolean disposing)
        {
            if (Disposed) return; // 基类只执行一次，子类也应遵循

            base.Dispose(disposing);

            DisposeCallCount++;
            if (disposing) ManagedDisposed = true;
        }
    }

    [Fact(DisplayName = "初始未释放")]
    public void InitialNotDisposed()
    {
        var obj = new TestDisposable();
        Assert.False(obj.Disposed);
        obj.Dispose();
    }

    [Fact(DisplayName = "Dispose后标记已释放")]
    public void DisposeSetsFlag()
    {
        var obj = new TestDisposable();
        obj.Dispose();
        Assert.True(obj.Disposed);
        Assert.True(obj.ManagedDisposed);
    }

    [Fact(DisplayName = "多次Dispose只执行一次")]
    public void DoubleDisposeOnlyOnce()
    {
        var obj = new TestDisposable();
        obj.Dispose();
        obj.Dispose();
        obj.Dispose();

        Assert.Equal(1, obj.DisposeCallCount);
    }

    [Fact(DisplayName = "OnDisposed事件触发")]
    public void OnDisposedEventFires()
    {
        var obj = new TestDisposable();
        var fired = false;
        obj.OnDisposed += (s, e) => fired = true;

        obj.Dispose();

        Assert.True(fired);
    }

    [Fact(DisplayName = "OnDisposed事件发送者正确")]
    public void OnDisposedSenderCorrect()
    {
        var obj = new TestDisposable();
        Object? sender = null;
        obj.OnDisposed += (s, e) => sender = s;

        obj.Dispose();

        Assert.Same(obj, sender);
    }

    [Fact(DisplayName = "IDisposable2接口实现")]
    public void ImplementsIDisposable2()
    {
        IDisposable2 obj = new TestDisposable();
        Assert.False(obj.Disposed);
        obj.Dispose();
        Assert.True(obj.Disposed);
    }

    [Fact(DisplayName = "TryDispose扩展方法")]
    public void TryDisposeExtension()
    {
        var obj = new TestDisposable();
        var result = obj.TryDispose();

        Assert.True(obj.Disposed);
        Assert.Same(obj, result);
    }

    [Fact(DisplayName = "TryDispose对null安全")]
    public void TryDisposeNullSafe()
    {
        Object? obj = null;
        var result = obj.TryDispose();
        Assert.Null(result);
    }

    [Fact(DisplayName = "TryDispose处理列表")]
    public void TryDisposeList()
    {
        var items = new List<TestDisposable>
        {
            new(),
            new(),
            new()
        };

        ((Object)items).TryDispose();

        foreach (var item in items)
        {
            Assert.True(item.Disposed);
        }
    }
}
