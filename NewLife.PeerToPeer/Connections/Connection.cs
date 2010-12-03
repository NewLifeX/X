using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Net.Sockets;
using System.Net.Sockets;
using System.Net;

namespace NewLife.PeerToPeer.Connections
{
    /// <summary>
    /// 网络连接
    /// </summary>
    public abstract class Connection
    {
        #region 属性
        private IPAddress _Address = IPAddress.Any;
        /// <summary>地址</summary>
        public IPAddress Address
        {
            get { return _Address; }
            set { _Address = value; }
        }

        private Int32 _Port;
        /// <summary>端口</summary>
        public Int32 Port
        {
            get { return _Port; }
            set { _Port = value; }
        }

        private ProtocolType _ProtocolType;
        /// <summary>连接类型　TCP/UDP</summary>
        public ProtocolType ProtocolType
        {
            get { return _ProtocolType; }
            set { _ProtocolType = value; }
        }
        #endregion

        /// <summary>
        /// 连接远程地址
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public abstract void Connect(IPAddress address, Int32 port);

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="stream"></param>
        public virtual void Send(Stream stream)
        {
        }

        /// <summary>
        /// 数据到达触发事件
        /// </summary>
        public virtual event EventHandler<NetEventArgs> DataArrived;

        /// <summary>
        /// 数据到达时
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="state"></param>
        /// <param name="stream"></param>
        protected virtual void OnDataArrived(Socket socket, Object state, Stream stream)
        {
            if (DataArrived == null) return;

            NetEventArgs e = new NetEventArgs();
            //e.Socket = socket;
            //e.State = state;
            //e.Stream = stream;

            DataArrived(this, e);
        }
    }
}
