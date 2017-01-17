using System;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>Api会话</summary>
    public interface IApiSession : IServiceProvider
    {
        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] AllSessions { get; }

        /// <summary>获取/设置 用户会话数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object this[string key] { get; set; }

        /// <summary>创建消息。低级接口，由框架使用</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        IMessage CreateMessage(Packet pk);

        /// <summary>发送消息。低级接口，由框架使用</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        Task<IMessage> SendAsync(IMessage msg);

        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<TResult> InvokeAsync<TResult>(string action, object args = null);
    }
}