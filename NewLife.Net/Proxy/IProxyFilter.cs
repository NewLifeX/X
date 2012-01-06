//using System;
//using System.Collections.Generic;
//using System.Text;
//using NewLife.Net.Sockets;
//using System.IO;

//namespace NewLife.Net.Proxy
//{
//    /// <summary>代理过滤器接口。</summary>
//    /// <remarks>
//    /// 这里是二次开发的核心所在，使用者所需要做的就是实现各种过滤器来控制数据转发。
//    /// </remarks>
//    public interface IProxyFilter : IDisposable
//    {
//        #region 属性
//        /// <summary>代理对象</summary>
//        IProxy Proxy { get; set; }
//        #endregion

//        #region 方法
//        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。</summary>
//        /// <param name="session"></param>
//        /// <param name="e"></param>
//        /// <returns></returns>
//        ISocketClient CreateRemote(IProxySession session, NetEventArgs e);

//        /// <summary>客户端发数据往服务端时</summary>
//        /// <param name="session"></param>
//        /// <param name="stream"></param>
//        /// <param name="e"></param>
//        /// <returns></returns>
//        Stream OnClientToServer(IProxySession session, Stream stream, NetEventArgs e);

//        /// <summary>服务端发数据往客户端时</summary>
//        /// <param name="session"></param>
//        /// <param name="stream"></param>
//        /// <param name="e"></param>
//        /// <returns></returns>
//        Stream OnServerToClient(IProxySession session, Stream stream, NetEventArgs e);
//        #endregion
//    }
//}