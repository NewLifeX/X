using System;
using System.Threading.Tasks;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端接口</summary>
    public interface IApiClient
    {
        /// <summary>令牌。每次请求携带</summary>
        String Token { get; set; }

        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        TResult Invoke<TResult>(String action, Object args = null);

        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        Task<TResult> InvokeAsync<TResult>(String action, Object args = null);
    }
}