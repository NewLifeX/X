using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net
{
    /// <summary>Socket服务器接口</summary>
    public interface ISocketServer : ISocket, IServer
    {
        /// <summary>是否活动</summary>
        Boolean Active { get; }

        /// <summary>会话超时时间。默认20*60秒</summary>
        /// <remarks>
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// </remarks>
        Int32 SessionTimeout { get; set; }

        /// <summary>会话统计</summary>
        ICounter StatSession { get; set; }

        /// <summary>会话集合。用地址端口作为标识，业务应用自己维持地址端口与业务主键的对应关系。</summary>
        IDictionary<String, ISocketSession> Sessions { get; }

        /// <summary>新会话时触发</summary>
        event EventHandler<SessionEventArgs> NewSession;
    }

    /// <summary>服务端通信Socket扩展</summary>
    public static class SocketServerHelper
    {
        #region 统计
        /// <summary>获取统计信息</summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static String GetStat(this ISocketServer socket)
        {
            if (socket == null) return null;

            var sb = Pool.StringBuilder.Get();
            //sb.AppendFormat("在线：{0:n0}/{1:n0} ", socket.SessionCount, socket.MaxSessionCount);
            if (socket.StatSend.Value > 0) sb.AppendFormat("发送：{0} ", socket.StatSend);
            if (socket.StatReceive.Value > 0) sb.AppendFormat("接收：{0} ", socket.StatReceive);
            if (socket.StatSession.Value > 0) sb.AppendFormat("会话：{0} ", socket.StatSession);

            return sb.Put(true);
        }
        #endregion
    }
}