using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>Api会话</summary>
    public interface IApiSession : IServiceProvider
    {
        /// <summary>用户对象。一般用于共享用户信息对象</summary>
        Object UserState { get; set; }

        ///// <summary>用户状态会话</summary>
        //IUserSession UserSession { get; set; }

        /// <summary>主机</summary>
        IApiHost Host { get; }

        /// <summary>最后活跃时间</summary>
        DateTime LastActive { get; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] AllSessions { get; }

        /// <summary>附加参数，每次请求都携带</summary>
        IDictionary<String, Object> Cookie { get; set; }

        /// <summary>获取/设置 用户会话数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get; set; }

        /// <summary>查找Api动作</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        ApiAction FindAction(String action);

        /// <summary>创建控制器实例</summary>
        /// <param name="api"></param>
        /// <returns></returns>
        Object CreateController(ApiAction api);

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
        /// <param name="cookie">附加参数，位于顶级</param>
        /// <returns></returns>
        Task<TResult> InvokeAsync<TResult>(String action, Object args = null, IDictionary<String, Object> cookie = null);
    }
}