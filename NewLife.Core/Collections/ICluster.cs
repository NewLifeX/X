using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLife.Collections
{
    /// <summary>集群管理</summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface ICluster<TKey, TValue>
    {
        /// <summary>资源列表</summary>
        Func<IEnumerable<TKey>> GetItems { get; set; }

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

    /// <summary>集群异常</summary>
    public class ClusterException : Exception
    {
        /// <summary>资源</summary>
        public String Resource { get; set; }

        /// <summary>实例化</summary>
        /// <param name="res"></param>
        /// <param name="inner"></param>
        public ClusterException(String res, Exception inner) : base($"[{res}]异常.{inner.Message}", inner) => Resource = res;
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
            var item = default(TValue);
            try
            {
                item = cluster.Get();
                return func(item);
            }
            finally
            {
                cluster.Put(item);
            }
        }

        /// <summary>对集群进行多次调用</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="cluster"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static TResult InvokeAll<TKey, TValue, TResult>(this ICluster<TKey, TValue> cluster, Func<TValue, TResult> func)
        {
            Exception error = null;
            var item = default(TValue);
            var count = cluster.GetItems().Count();
            for (var i = 0; i < count; i++)
            {
                try
                {
                    item = cluster.Get();
                    return func(item);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    cluster.Put(item);
                }
            }

            //throw error;
            throw new ClusterException(item + "", error);
        }
    }
}