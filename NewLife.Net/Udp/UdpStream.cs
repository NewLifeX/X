//using System;
//using System.Collections.Generic;
//using System.Text;
//using NewLife.Net.Sockets;
//using System.Net;
//using System.Net.Sockets;
//using System.IO;

//namespace NewLife.Net.Udp
//{
//    /// <summary>
//    /// Udp网络流。Udp的数据发送需要指定远程地址
//    /// </summary>
//    public class UdpStream : SocketStream
//    {
//        #region 属性
//        private IPEndPoint _RemoteEndPoint;
//        /// <summary>远程地址</summary>
//        public IPEndPoint RemoteEndPoint
//        {
//            get { return _RemoteEndPoint; }
//            set { _RemoteEndPoint = value; }
//        }
//        #endregion

//        #region 构造
//        /// <summary>
//        /// 初始化
//        /// </summary>
//        /// <param name="socket"></param>
//        /// <param name="remote"></param>
//        public UdpStream(Socket socket, EndPoint remote) : this(socket, Stream.Null, remote) { }

//        /// <summary>
//        /// 使用Socket和输入流初始化一个Socket流，该流将从输入流中读取数据，并把输出的数据写入到Socket中
//        /// </summary>
//        /// <param name="socket"></param>
//        /// <param name="inputStream"></param>
//        /// <param name="remote"></param>
//        public UdpStream(Socket socket, Stream inputStream, EndPoint remote)
//            : base(socket, inputStream)
//        {
//            RemoteEndPoint = remote as IPEndPoint;
//        }
//        #endregion

//        #region 方法
//        /// <summary>
//        /// 把数据发送到网络中
//        /// </summary>
//        /// <param name="buffer"></param>
//        /// <param name="offset"></param>
//        /// <param name="count"></param>
//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            if (RemoteEndPoint.Address != IPAddress.Any && RemoteEndPoint.Port != 0)
//                Socket.SendTo(buffer, offset, count, SocketFlags.None, RemoteEndPoint);
//            else
//                base.Write(buffer, offset, count);
//        }
//        #endregion
//    }
//}