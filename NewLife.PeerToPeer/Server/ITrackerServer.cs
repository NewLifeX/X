using System;
using System.IO;
using NewLife.PeerToPeer.Messages;

namespace NewLife.PeerToPeer.Server
{
    /// <summary>
    /// 跟踪信息
    /// </summary>
    public interface ITrackerServer
    {
        /// <summary>
        /// 消息到达时触发
        /// </summary>
        event EventHandler<EventArgs<P2PMessage, Stream>> MessageArrived;

        /// <summary>
        /// 开始
        /// </summary>
        void Start();

        /// <summary>
        /// 停止
        /// </summary>
        void Stop();

        ///// <summary>
        ///// 发送数据
        ///// </summary>
        ///// <param name="buffer"></param>
        ///// <param name="remoteEP"></param>
        //void Send(Byte[] buffer, EndPoint remoteEP);
    }
}
