using System;
using System.Threading.Tasks;

namespace NewLife.Remoting
{
    /// <summary>Api会话</summary>
    public interface IApiSession : IServiceProvider
    {
        /// <summary>正在连接的所有会话，包含自己</summary>
        IApiSession[] AllSessions { get; }

        /// <summary>获取/设置 用户会话数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get; set; }

        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<TResult> InvokeAsync<TResult>(string action, object args = null);
    }
}