using System;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端接口</summary>
    public interface IApiClient
    {
        /// <summary>服务提供者</summary>
        IServiceProvider Provider { get; set; }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        Boolean Init(Object config);

        /// <summary>打开</summary>
        void Open();

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        void Close(String reason);

        /// <summary>打开后触发。</summary>
        event EventHandler Opened;

        /// <summary>创建消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        IMessage CreateMessage(Packet pk);

        /// <summary>远程调用</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        Task<IMessage> SendAsync(IMessage msg);

        /// <summary>日志</summary>
        ILog Log { get; set; }
    }
}