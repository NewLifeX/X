using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.IO;
using Xunit;

namespace XUnitTest.IO;

public class AsyncEnumerableTests
{
    #region 辅助类型

    /// <summary>测试用可异步释放资源</summary>
    private class AsyncResource : IAsyncDisposable
    {
        /// <summary>是否已释放</summary>
        public Boolean Disposed { get; private set; }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>生成指定数量整数的异步迭代器，支持取消</summary>
    private static async IAsyncEnumerable<Int32> GenerateAsync(Int32 count, [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (var i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return i;
        }
    }

    #endregion

    #region IAsyncDisposable 测试

    [Fact]
    [DisplayName("await using 触发 DisposeAsync")]
    public async Task DisposeAsync_Called_On_AwaitUsing()
    {
        var res = new AsyncResource();
        await using (res)
        {
            Assert.False(res.Disposed);
        }
        Assert.True(res.Disposed);
    }

    [Fact]
    [DisplayName("ConfigureAwait(false) 后 DisposeAsync 正常工作")]
    public async Task DisposeAsync_ConfigureAwait()
    {
        var res = new AsyncResource();
        await using var d = res.ConfigureAwait(false);
        Assert.False(res.Disposed);
        // d 超出作用域后 DisposeAsync 被调用
        _ = d; // 确保编译器不优化掉
    }

    [Fact]
    [DisplayName("多个资源 await using 均被正确释放")]
    public async Task DisposeAsync_MultipleResources()
    {
        var r1 = new AsyncResource();
        var r2 = new AsyncResource();

        await using (r1)
        await using (r2)
        {
            Assert.False(r1.Disposed);
            Assert.False(r2.Disposed);
        }

        Assert.True(r1.Disposed);
        Assert.True(r2.Disposed);
    }

    #endregion

    #region IAsyncEnumerable 测试

    [Fact]
    [DisplayName("await foreach 按序产出所有元素")]
    public async Task AsyncEnumerable_AwaitForeach_AllElements()
    {
        var results = new List<Int32>();
        await foreach (var item in GenerateAsync(5))
        {
            results.Add(item);
        }

        Assert.Equal([0, 1, 2, 3, 4], results);
    }

    [Fact]
    [DisplayName("空迭代器不产出任何元素")]
    public async Task AsyncEnumerable_Empty()
    {
        var count = 0;
        await foreach (var _ in GenerateAsync(0))
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    [DisplayName("WithCancellation 在取消后抛出 OperationCanceledException")]
    public async Task AsyncEnumerable_WithCancellation()
    {
        using var cts = new CancellationTokenSource();
        var results = new List<Int32>();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in GenerateAsync(10).WithCancellation(cts.Token))
            {
                results.Add(item);
                if (item == 2) cts.Cancel();
            }
        });

        // 至少收到了 0,1,2 三个元素
        Assert.Equal([0, 1, 2], results);
    }

    [Fact]
    [DisplayName("ConfigureAwait(false) 正常迭代所有元素")]
    public async Task AsyncEnumerable_ConfigureAwait()
    {
        var results = new List<Int32>();
        await foreach (var item in GenerateAsync(3).ConfigureAwait(false))
        {
            results.Add(item);
        }

        Assert.Equal([0, 1, 2], results);
    }

    [Fact]
    [DisplayName("ConfigureAwait 与 WithCancellation 链式调用正常工作")]
    public async Task AsyncEnumerable_ConfigureAwait_WithCancellation_Chain()
    {
        using var cts = new CancellationTokenSource();
        var results = new List<Int32>();

        // 先 ConfigureAwait 再 WithCancellation
        await foreach (var item in GenerateAsync(5).ConfigureAwait(false).WithCancellation(cts.Token))
        {
            results.Add(item);
        }

        Assert.Equal([0, 1, 2, 3, 4], results);
    }

    [Fact]
    [DisplayName("默认 CancellationToken 不影响正常迭代")]
    public async Task AsyncEnumerable_WithCancellation_Default()
    {
        var results = new List<Int32>();
        await foreach (var item in GenerateAsync(3).WithCancellation(CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal([0, 1, 2], results);
    }

    #endregion

    #region CsvFile 集成测试

    [Fact]
    [DisplayName("CsvFile.ReadAllAsync 通过 await foreach 读出所有行")]
    public async Task CsvFile_ReadAllAsync_Lines()
    {
        var ms = new MemoryStream();
        var csv = new CsvFile(ms, true);
        await csv.WriteLineAsync(new Object[] { "Alice", "30" });
        await csv.WriteLineAsync(new Object[] { "Bob", "25" });
        await csv.DisposeAsync();
        ms.Position = 0;

        var readCsv = new CsvFile(ms);
        var rows = new List<String[]>();
        await foreach (var row in readCsv.ReadAllAsync())
        {
            rows.Add(row);
        }

        Assert.Equal(2, rows.Count);
        Assert.Equal("Alice", rows[0][0]);
        Assert.Equal("30", rows[0][1]);
        Assert.Equal("Bob", rows[1][0]);
        Assert.Equal("25", rows[1][1]);
    }

    [Fact]
    [DisplayName("CsvFile.ReadAllAsync 支持 ConfigureAwait(false) 迭代")]
    public async Task CsvFile_ReadAllAsync_ConfigureAwait()
    {
        var ms = new MemoryStream();
        var csv = new CsvFile(ms, true);
        await csv.WriteLineAsync(new Object[] { "X", "1" });
        await csv.DisposeAsync();
        ms.Position = 0;

        var readCsv = new CsvFile(ms);
        var rows = new List<String[]>();
        await foreach (var row in readCsv.ReadAllAsync().ConfigureAwait(false))
        {
            rows.Add(row);
        }

        Assert.Single(rows);
        Assert.Equal("X", rows[0][0]);
    }

    [Fact]
    [DisplayName("await using CsvFile 正确触发 DisposeAsync")]
    public async Task CsvFile_AwaitUsing_DisposeAsync()
    {
        var ms = new MemoryStream(Encoding.UTF8.GetBytes("a,b\r\nc,d\r\n"));
        await using var csv = new CsvFile(ms);
        var count = 0;
        await foreach (var _ in csv.ReadAllAsync())
        {
            count++;
        }

        Assert.Equal(2, count);
        // DisposeAsync 在 await using 块退出后被调用，流已关闭
    }

    #endregion
}
