using System;
using System.Threading.Tasks;
using NewLife.Net;

namespace NewLife.Remoting
{
    /// <summary>Api会话</summary>
    public interface IApiSession : IServiceProvider
    {
        ///// <summary>远程地址</summary>
        //NetUri Remote { get; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] AllSessions { get; }

        /// <summary>获取/设置 用户会话数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object this[string key] { get; set; }

        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<TResult> InvokeAsync<TResult>(string action, object args = null);
    }
}