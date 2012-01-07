//using System;
//using System.Collections.Generic;
//using System.Text;
//using NewLife.Net.Sockets;
//using System.IO;

//namespace NewLife.Net.Proxy
//{
//    /// <summary>代理过滤器集合</summary>
//    class ProxyFilterCollection : List<IProxyFilter>, IProxyFilter
//    {
//        #region 属性
//        /// <summary>代理对象</summary>
//        public IProxy Proxy { get; set; }
//        #endregion

//        #region 构造
//        public ProxyFilterCollection(IProxy proxy) { Proxy = proxy; }
//        #endregion

//        #region 方法
//        /// <summary>为会话创建与远程服务器通讯的Socket</summary>
//        /// <param name="session"></param>
//        /// <param name="e"></param>
//        /// <returns></returns>
//        public ISocketClient CreateRemote(IProxySession session, NetEventArgs e)
//        {
//            // 转发给过滤器，由过滤器负责创建
//            foreach (var item in this)
//            {
//                var client = item.CreateRemote(session, e);
//                if (client != null) return client;
//            }

//            // return null;
//            throw new NetException("没有任何过滤器为会话创建远程Socket！");
//        }

//        /// <summary>客户端发数据往服务端时</summary>
//        /// <param name="session"></param>
//        /// <param name="stream"></param>
//        /// <param name="e"></param>
//        /// <returns></returns>
//        public Stream OnClientToServer(IProxySession session, Stream stream, NetEventArgs e)
//        {
//            foreach (var item in this)
//            {
//                if ((stream = item.OnClientToServer(session, stream, e)) == null) return null;
//            }
//            return stream;
//        }

//        /// <summary>服务端发数据往客户端时</summary>
//        /// <param name="session"></param>
//        /// <param name="stream"></param>
//        /// <param name="e"></param>
//        /// <returns></returns>
//        public Stream OnServerToClient(IProxySession session, Stream stream, NetEventArgs e)
//        {
//            foreach (var item in this)
//            {
//                if ((stream = item.OnServerToClient(session, stream, e)) == null) return null;
//            }
//            return stream;
//        }
//        #endregion

//        #region IDisposable 成员
//        public void Dispose()
//        {
//            foreach (var item in this)
//            {
//                item.Dispose();
//            }
//            this.Clear();
//        }
//        #endregion
//    }
//}