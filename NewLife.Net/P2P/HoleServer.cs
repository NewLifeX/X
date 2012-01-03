using System;
using NewLife.Linq;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net;

namespace NewLife.Net.P2P
{
    /// <summary>打洞服务器</summary>
    public class HoleServer : NetServer
    {
        #region 属性
        private Dictionary<String, INetSession> _Clients;
        /// <summary>客户端集合</summary>
        public IDictionary<String, INetSession> Clients { get { return _Clients ?? (_Clients = new Dictionary<String, INetSession>(StringComparer.OrdinalIgnoreCase)); } }
        #endregion

        #region 方法
        ///// <summary>接受连接时，对于Udp是收到数据时（同时触发OnReceived）</summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //protected override void OnAccepted(object sender, NetEventArgs e)
        //{
        //    base.OnAccepted(sender, e);
        //}

        /// <summary>收到数据时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            base.OnReceived(sender, e);

            var session = e.Socket as ISocketSession;
            var client = e.RemoteIPEndPoint;

            var str = e.GetString();
            WriteLog(client + " " + str);

            var ss = str.Split(":");
            ss[0] = ss[0].ToLower();
            if (ss[0] == "reg")
            {
                // 如果连续注册两次可能会有问题，这里要处理一下
                if (Clients.ContainsKey(ss[1]))
                {
                    session.Send("Has Register!", null, client);
                }
                else
                {
                    var ns = NetService.Resolve<INetSession>();
                    ns.Server = sender as ISocketServer;
                    ns.Session = session;
                    ns.ClientEndPoint = client;
                    Clients[ss[1]] = ns;
                    ns.OnDisposed += (s, e2) => Clients.Remove(ss[1]);

                    session.Send("Success!", null, client);
                }
            }
            else if (ss[0] == "invite")
            {
                INetSession ns = null;
                if (Clients.TryGetValue(ss[1], out ns))
                {
                    session.Send(ns.ClientEndPoint.ToString(), null, client);

                    // 同时还要通知对方
                    ns.Session.Send(client.ToString(), null, ns.ClientEndPoint);
                }
                else
                    session.Send("Not Found!", null, client);
            }
        }
        #endregion
    }
}