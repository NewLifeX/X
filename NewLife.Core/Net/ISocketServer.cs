using System;
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
    }
}