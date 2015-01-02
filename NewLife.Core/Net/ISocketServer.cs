using System;
using System.Collections.Generic;
using System.Net.Sockets;
using NewLife.Model;

namespace NewLife.Net
{
    /// <summary>Socket服务器接口</summary>
    public interface ISocketServer : ISocket, IServer
    {
        /// <summary>是否活动</summary>
        Boolean Active { get; }

        ///// <summary>基础Socket对象</summary>
        //Socket Server { get; set; }

        /// <summary>最大不活动时间。
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// 单位秒，默认30秒。时间不是太准确，建议15秒的倍数。为0表示不检查。</summary>
        Int32 MaxNotActive { get; set; }

        /// <summary>会话集合。用地址端口作为标识，业务应用自己维持地址端口与业务主键的对应关系。</summary>
        IDictionary<String, ISocketSession> Sessions { get; }

        /// <summary>新会话时触发</summary>
        event EventHandler<SessionEventArgs> NewSession;
    }
}