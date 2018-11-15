using System;
using System.Threading.Tasks;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>Api会话</summary>
    public interface IApiSession
    {
        /// <summary>主机</summary>
        IApiHost Host { get; }

        /// <summary>最后活跃时间</summary>
        DateTime LastActive { get; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] AllSessions { get; }

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

        ///// <summary>创建消息。低级接口，由框架使用</summary>
        ///// <param name="pk"></param>
        ///// <returns></returns>
        //IMessage CreateMessage(Packet pk);

        /// <summary>发送消息。低级接口，由框架使用</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        Task<Tuple<IMessage, Object>> SendAsync(IMessage msg);

        /// <summary>发送消息。低级接口，由框架使用</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        Boolean Send(IMessage msg);

        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        Task<TResult> InvokeAsync<TResult>(String action, Object args = null, Byte flag = 0);
    }
}