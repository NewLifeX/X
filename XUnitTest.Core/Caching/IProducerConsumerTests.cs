using NewLife.Caching;
using Xunit;

namespace XUnitTest.Caching;

/// <summary>IProducerConsumer 接口契约测试，以 MemoryQueue 为实现验证生产者消费者语义</summary>
public class IProducerConsumerTests : IDisposable
{
    private readonly MemoryQueue<String> _queue;

    public IProducerConsumerTests() => _queue = new MemoryQueue<String>();

    public void Dispose() => _queue.Dispose();

    [Fact(DisplayName = "初始状态 Count=0 IsEmpty=true")]
    public void InitialState()
    {
        Assert.Equal(0, _queue.Count);
        Assert.True(_queue.IsEmpty);
    }

    [Fact(DisplayName = "Add/TakeOne 单条生产消费")]
    public void Add_TakeOne()
    {
        _queue.Add("hello");

        Assert.Equal(1, _queue.Count);
        Assert.False(_queue.IsEmpty);

        var val = _queue.TakeOne(1);
        Assert.Equal("hello", val);

        // 消费后应为空
        Assert.True(_queue.IsEmpty);
    }

    [Fact(DisplayName = "Add 批量生产")]
    public void Add_Batch()
    {
        var n = _queue.Add("a", "b", "c");

        Assert.Equal(3, n);
        Assert.Equal(3, _queue.Count);
    }

    [Fact(DisplayName = "Take 批量消费")]
    public void Take_Batch()
    {
        _queue.Add("x", "y", "z");

        var items = _queue.Take(2).ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains("x", items);
        Assert.Contains("y", items);

        // 队列剩余 1 个
        Assert.Equal(1, _queue.Count);
    }

    [Fact(DisplayName = "TakeOneAsync 异步消费")]
    public async Task TakeOneAsync()
    {
        _queue.Add("async_test");

        var val = await _queue.TakeOneAsync(1);
        Assert.Equal("async_test", val);
    }

    [Fact(DisplayName = "TakeOne 空队列不等待返回默认值")]
    public void TakeOne_Timeout()
    {
        // 空队列 timeout < 0 不等待直接返回默认值
        var val = _queue.TakeOne(-1);
        Assert.Null(val);
    }

    [Fact(DisplayName = "Acknowledge 确认消费")]
    public void Acknowledge()
    {
        _queue.Add("ack_msg");

        var val = _queue.TakeOne(1);
        Assert.Equal("ack_msg", val);

        // 确认消费（MemoryQueue 的 Acknowledge 始终返回 0）
        var n = _queue.Acknowledge("ack_msg");
        Assert.Equal(0, n);
    }
}
