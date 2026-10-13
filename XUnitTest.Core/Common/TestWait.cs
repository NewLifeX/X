using System.Diagnostics;

namespace XUnitTest;

/// <summary>测试等待助手。以条件轮询取代固定 Sleep，超时仅作防挂起兜底，不作为性能判定</summary>
/// <remarks>
/// <para>全量并行跑测时线程池与调度存在秒级抖动，用固定 Sleep 等待异步副作用必然偶发误判：睡少了失败，睡多了拖慢。</para>
/// <para>等待一律"条件成立即通过、超时兜底失败"，超时默认取足够宽松（10 秒），只负责防止用例永久挂起。</para>
/// </remarks>
internal static class TestWait
{
    /// <summary>轮询等待条件成立</summary>
    /// <param name="condition">条件委托</param>
    /// <param name="timeoutMs">超时毫秒，仅作防挂起兜底</param>
    /// <param name="intervalMs">轮询间隔毫秒</param>
    /// <returns>是否在超时前成立</returns>
    public static Boolean Until(Func<Boolean> condition, Int32 timeoutMs = 10_000, Int32 intervalMs = 20)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            if (condition()) return true;
            if (sw.ElapsedMilliseconds >= timeoutMs) return false;

            Thread.Sleep(intervalMs);
        }
    }

    /// <summary>异步轮询等待条件成立</summary>
    /// <param name="condition">条件委托</param>
    /// <param name="timeoutMs">超时毫秒，仅作防挂起兜底</param>
    /// <param name="intervalMs">轮询间隔毫秒</param>
    /// <returns>是否在超时前成立</returns>
    public static async Task<Boolean> UntilAsync(Func<Boolean> condition, Int32 timeoutMs = 10_000, Int32 intervalMs = 20)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            if (condition()) return true;
            if (sw.ElapsedMilliseconds >= timeoutMs) return false;

            await Task.Delay(intervalMs).ConfigureAwait(false);
        }
    }

    /// <summary>等待任务在超时内完成</summary>
    /// <param name="task">任务</param>
    /// <param name="timeoutMs">超时毫秒</param>
    /// <returns>是否在超时前完成</returns>
    public static async Task<Boolean> CompletedAsync(Task task, Int32 timeoutMs)
    {
        var finished = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);

        return ReferenceEquals(finished, task);
    }
}
