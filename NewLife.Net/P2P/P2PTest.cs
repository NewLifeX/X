using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.P2P
{
    /// <summary>P2P测试</summary>
    public static class P2PTest
    {
        /// <summary>开始</summary>
        public static void Start()
        {
            var server = new HoleServer();
            server.Port = 15;
            server.Start();
        }
    }
}