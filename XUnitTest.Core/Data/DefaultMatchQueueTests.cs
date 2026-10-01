using System.ComponentModel;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Data;

public class DefaultMatchQueueTests
{
    [Fact]
    [DisplayName("Add_Match_正常请求响应配对")]
    public void Add_Match_RequestResponseMatch()
    {
        var queue = new DefaultMatchQueue(16);
        var tcs = new TaskCompletionSource<Object>();
        var request = "req1";
        var response = "resp1";

        queue.Add("owner", request, 5000, tcs);

        var matched = queue.Match("owner", response, tcs, (req, resp) =>
        {
            return req?.ToString() == "req1" && resp?.ToString() == "resp1";
        });

        Assert.True(matched);
    }

    [Fact]
    [DisplayName("Add_Match_不匹配回调_返回False")]
    public void Add_Match_NoMatch_ReturnsFalse()
    {
        var queue = new DefaultMatchQueue(16);
        var tcs = new TaskCompletionSource<Object>();
        var request = "req1";
        var response = "resp2";

        queue.Add("owner", request, 5000, tcs);

        var matched = queue.Match("owner", response, tcs, (req, resp) =>
        {
            return req?.ToString() == "req1" && resp?.ToString() == "resp1";
        });

        Assert.False(matched);
    }

    [Fact]
    [DisplayName("Add_多个请求_按顺序匹配")]
    public void Add_MultipleRequests_MatchInOrder()
    {
        var queue = new DefaultMatchQueue(16);
        var tcs1 = new TaskCompletionSource<Object>();
        var tcs2 = new TaskCompletionSource<Object>();

        queue.Add("owner", "req1", 5000, tcs1);
        queue.Add("owner", "req2", 5000, tcs2);

        // 匹配第二个先
        var matched2 = queue.Match("owner", "resp2", tcs2, (req, resp) =>
        {
            return req?.ToString() == "req2" && resp?.ToString() == "resp2";
        });
        Assert.True(matched2);

        // 再匹配第一个
        var matched1 = queue.Match("owner", "resp1", tcs1, (req, resp) =>
        {
            return req?.ToString() == "req1" && resp?.ToString() == "resp1";
        });
        Assert.True(matched1);
    }

    [Fact]
    [DisplayName("Clear_清空后不再匹配")]
    public void Clear_ThenNoMatch()
    {
        var queue = new DefaultMatchQueue(16);
        var tcs = new TaskCompletionSource<Object>();

        queue.Add("owner", "req1", 5000, tcs);
        queue.Clear();

        var matched = queue.Match("owner", "resp1", tcs, (req, resp) => true);
        Assert.False(matched);
    }

    [Fact]
    [DisplayName("队列满时_抛出异常")]
    public void QueueFull_ThrowsException()
    {
        // 使用小队列（3个槽位）
        var queue = new DefaultMatchQueue(3);
        var tasks = new TaskCompletionSource<Object>[4];

        for (var i = 0; i < 3; i++)
        {
            tasks[i] = new TaskCompletionSource<Object>();
            queue.Add("owner", $"req{i}", 5000, tasks[i]);
        }

        // 第四个插入应抛出异常
        tasks[3] = new TaskCompletionSource<Object>();
        Assert.Throws<XException>(() => queue.Add("owner", "req3", 5000, tasks[3]));
    }

    [Fact]
    [DisplayName("Clear_清除所有项")]
    public void Clear_RemovesAllItems()
    {
        var queue = new DefaultMatchQueue(16);
        var tcs = new TaskCompletionSource<Object>();

        queue.Add("owner", "req1", 5000, tcs);
        queue.Clear();

        // Clear 后再 Match 应返回 false
        var matched = queue.Match("owner", "resp1", tcs, (req, resp) => true);
        Assert.False(matched);
    }

    [Fact]
    [DisplayName("不同Owner_分别匹配")]
    public void DifferentOwners_MatchSeparately()
    {
        var queue = new DefaultMatchQueue(16);
        var tcs1 = new TaskCompletionSource<Object>();
        var tcs2 = new TaskCompletionSource<Object>();

        queue.Add("owner1", "req1", 5000, tcs1);
        queue.Add("owner2", "req2", 5000, tcs2);

        // owner1 只匹配自己的请求
        var matched1 = queue.Match("owner1", "resp1", tcs1, (req, resp) =>
            req?.ToString() == "req1");
        Assert.True(matched1);

        // owner2 只匹配自己的请求
        var matched2 = queue.Match("owner2", "resp2", tcs2, (req, resp) =>
            req?.ToString() == "req2");
        Assert.True(matched2);
    }

    [Fact]
    [DisplayName("队列清空后停止检查定时器_再次入队重新启动")]
    public async Task Timer_StopsWhenEmpty_AndRestarts()
    {
        var queue = new DefaultMatchQueue(16);

        // 入队即启动周期检查定时器（请求超时取消依赖它）
        var first = new TaskCompletionSource<Object>(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.Add(null, "req1", 11, first);
        Assert.NotNull(queue.Timer);

        // 首轮请求超时被清理后队列归空：实现应停掉定时器，空闲队列不再每秒空转占用调度器
        await Task.Delay(1_500);
        Assert.True(first.Task.IsCanceled, "首次请求未被超时取消");
        Assert.Null(queue.Timer);

        // 停表后再次入队必须重新启动，否则该请求永远等不到超时取消，等待方悬挂
        var second = new TaskCompletionSource<Object>(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.Add(null, "req2", 11, second);
        Assert.NotNull(queue.Timer);

        await Task.Delay(1_500);
        Assert.True(second.Task.IsCanceled, "停表后未重新启动定时器：第二次请求未被超时取消");
    }
}
