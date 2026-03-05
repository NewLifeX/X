using NewLife.Collections;
using Xunit;

namespace XUnitTest.Collections;

public class ConcurrentHashSetTests
{
    [Fact(DisplayName = "新建集合为空")]
    public void NewSetIsEmpty()
    {
        var set = new ConcurrentHashSet<Int32>();

        Assert.True(set.IsEmpty);
        Assert.Equal(0, set.Count);
    }

    [Fact(DisplayName = "添加元素")]
    public void TryAdd()
    {
        var set = new ConcurrentHashSet<String>();

        Assert.True(set.TryAdd("Hello"));
        Assert.Equal(1, set.Count);
        Assert.False(set.IsEmpty);
    }

    [Fact(DisplayName = "重复添加返回false")]
    public void TryAdd_Duplicate()
    {
        var set = new ConcurrentHashSet<Int32>();

        Assert.True(set.TryAdd(1));
        Assert.False(set.TryAdd(1));
        Assert.Equal(1, set.Count);
    }

    [Fact(DisplayName = "Contains检查")]
    public void Contains()
    {
        var set = new ConcurrentHashSet<String>();
        set.TryAdd("test");

        Assert.True(set.Contains("test"));
        Assert.False(set.Contains("other"));
    }

    [Fact(DisplayName = "移除元素")]
    public void TryRemove()
    {
        var set = new ConcurrentHashSet<Int32>();
        set.TryAdd(42);

        Assert.True(set.TryRemove(42));
        Assert.Equal(0, set.Count);
        Assert.False(set.Contains(42));
    }

    [Fact(DisplayName = "移除不存在元素返回false")]
    public void TryRemove_NotExist()
    {
        var set = new ConcurrentHashSet<Int32>();

        Assert.False(set.TryRemove(99));
    }

    [Fact(DisplayName = "枚举所有元素")]
    public void Enumeration()
    {
        var set = new ConcurrentHashSet<Int32>();
        set.TryAdd(1);
        set.TryAdd(2);
        set.TryAdd(3);

        var list = new List<Int32>();
        foreach (var item in set)
        {
            list.Add(item);
        }

        Assert.Equal(3, list.Count);
        Assert.Contains(1, list);
        Assert.Contains(2, list);
        Assert.Contains(3, list);
    }

    [Fact(DisplayName = "并发添加安全")]
    public void ConcurrentAdd()
    {
        var set = new ConcurrentHashSet<Int32>();
        var addedCount = 0;

        Parallel.For(0, 1000, i =>
        {
            if (set.TryAdd(i))
                Interlocked.Increment(ref addedCount);
        });

        Assert.Equal(1000, set.Count);
        Assert.Equal(1000, addedCount);
    }

    [Fact(DisplayName = "并发添加和删除安全")]
    public void ConcurrentAddAndRemove()
    {
        var set = new ConcurrentHashSet<Int32>();

        // 先添加一批
        for (var i = 0; i < 100; i++)
        {
            set.TryAdd(i);
        }

        // 并发删除和添加
        Parallel.For(0, 200, i =>
        {
            if (i < 100)
                set.TryRemove(i);
            else
                set.TryAdd(i);
        });

        // 最终应该只有100-199
        Assert.Equal(100, set.Count);
    }

#pragma warning disable CS0618
    [Fact(DisplayName = "Contain旧接口兼容")]
    public void Contain_Obsolete()
    {
        var set = new ConcurrentHashSet<String>();
        set.TryAdd("key");

        Assert.True(set.Contain("key"));
    }
#pragma warning restore CS0618
}
