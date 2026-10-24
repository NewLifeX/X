using System.Collections.Concurrent;
using NewLife.Collections;
using Xunit;

namespace XUnitTest.Collections;

public class PoolTests
{
    [Fact(DisplayName = "Pool 并发压力测试")]
    public void ConcurrentGetReturn()
    {
        var pool = new Pool<Object>(64);
        var bag = new ConcurrentBag<Object>();
        var threadCount = 8;
        var loop = 10_000;
        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < loop; i++)
            {
                var obj = pool.Get();
                bag.Add(obj);
                Assert.NotNull(obj);
            }
        });

        // 归还所有对象
        foreach (var obj in bag) pool.Return(obj);

        // 池容量不超限
        var count = pool.Clear();
        Assert.True(count <= pool.Max);
    }

    [Fact(DisplayName = "基本创建与归还复用")]
    public void Get_Return_Reuse()
    {
        var pool = new Pool<PoolTestItem>(8);

        var a = pool.Get();
        Assert.NotNull(a);

        pool.Return(a);
        var b = pool.Get();
        Assert.Same(a, b);
    }

    [Fact(DisplayName = "子类重写OnCreate优先于默认工厂")]
    public void OnCreate_Override()
    {
        var pool = new CustomPool(8);
        Assert.Equal(42, pool.Get().Value);
    }

    [Fact(DisplayName = "无公开无参构造类型_Get抛出而非静默返回null")]
    public void Get_NoParameterlessCtor_Throws()
    {
        var pool = new Pool<NoDefaultCtorItem>(8);
        Assert.ThrowsAny<Exception>(() => pool.Get());
    }

    class PoolTestItem
    {
        public Int32 Value { get; set; }
    }

    class CustomPool : Pool<PoolTestItem>
    {
        public CustomPool(Int32 max) : base(max) { }

        protected override PoolTestItem? OnCreate() => new() { Value = 42 };
    }

    class NoDefaultCtorItem
    {
        public NoDefaultCtorItem(Int32 value) => Value = value;

        public Int32 Value { get; }
    }
}
