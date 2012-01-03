using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;
using System.Threading;
using System.Net;

namespace NewLife.Net.P2P
{
    /// <summary>P2P测试</summary>
    public static class P2PTest
    {
        /// <summary>开始</summary>
        /// <param name="port"></param>
        public static void StartHole(Int32 port = 15)
        {
            var server = new HoleServer();
            server.Port = port;
            server.Start();
        }

        /// <summary>开始客户端</summary>
        /// <param name="name"></param>
        /// <param name="server"></param>
        /// <param name="serverport"></param>
        public static void StartClient(String name, String server = "127.0.0.1", Int32 serverport = 15)
        {
            var client = new P2PClient();
            client.HoleServer = new IPEndPoint(NetHelper.ParseAddress(server), serverport);
            client.Start(name);
        }
    }
}