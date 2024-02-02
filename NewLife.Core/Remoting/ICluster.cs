namespace NewLife.Remoting;

/// <summary>集群管理</summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface ICluster<TKey, TValue>
{
    /// <summary>最后使用资源</summary>
    KeyValuePair<TKey, TValue> Current { get; }

    /// <summary>资源列表</summary>
    Func<IEnumerable<TKey>>? GetItems { get; set; }

    /// <summary>打开</summary>
    Boolean Open();

    /// <summary>关闭</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <returns>是否成功</returns>
    Boolean Close(String reason);

    /// <summary>从集群中获取资源</summary>
    /// <returns></returns>
    TValue Get();

    /// <summary>归还</summary>
    /// <param name="value"></param>
    Boolean Put(TValue value);
}

/// <summary>集群助手</summary>
public static class ClusterHelper
{
    /// <summary>借助集群资源处理事务</summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="cluster"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static TResult Invoke<TKey, TValue, TResult>(this ICluster<TKey, TValue> cluster, Func<TValue, TResult> func)
    {
        var item = cluster.Get();
        try
        {
            return func(item);
        }
        finally
        {
            cluster.Put(item);
        }
    }

    /// <summary>借助集群资源处理事务</summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="cluster"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static async Task<TResult> InvokeAsync<TKey, TValue, TResult>(this ICluster<TKey, TValue> cluster, Func<TValue, Task<TResult>> func)
    {
        var item = cluster.Get();
        try
        {
            return await func(item).ConfigureAwait(false);
        }
        finally
        {
            cluster.Put(item);
        }
    }
}