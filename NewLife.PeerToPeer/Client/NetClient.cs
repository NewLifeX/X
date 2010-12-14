using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;

namespace NewLife.PeerToPeer.Client
{
    /// <summary>
    /// 网络客户端
    /// </summary>
    public class NetClient : NetServer
    {
        #region 属性
        #endregion

        #region 构造
        #endregion

        #region 方法
        #endregion

        #region 开始/停止
        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            Name = "P2P节点";

            // 允许同时处理多个数据包
            Server.NoDelay = true;
            // 使用线程池来处理事件
            Server.UseThreadPool = true;
        }
        #endregion
    }
}