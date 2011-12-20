using System.IO;
using System.Net;
using System.Net.Sockets;
using NewLife.IO;
using NewLife.Net.Common;

namespace NewLife.Net.Sockets
{
    /// <summary>Socket数据流</summary>
    public class SocketStream : ReadWriteStream
    {
        #region 属性
        private Socket _Socket;
        /// <summary>套接字</summary>
        public Socket Socket { get { return _Socket; } internal set { _Socket = value; } }

        private IPEndPoint _RemoteEndPoint;
        /// <summary>远程地址</summary>
        public IPEndPoint RemoteEndPoint { get { return _RemoteEndPoint; } private set { _RemoteEndPoint = value; } }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="socket"></param>
        public SocketStream(Socket socket) : this(socket, Stream.Null) { }

        /// <summary>使用Socket和输入流初始化一个Socket流，该流将从输入流中读取数据，并把输出的数据写入到Socket中</summary>
        /// <param name="socket"></param>
        /// <param name="inputStream"></param>
        public SocketStream(Socket socket, Stream inputStream)
            : base(inputStream, Stream.Null)
        {
            Socket = socket;
        }

        /// <summary>初始化</summary>
        /// <param name="socket"></param>
        /// <param name="remote"></param>
        public SocketStream(Socket socket, EndPoint remote) : this(socket, Stream.Null, remote) { }

        /// <summary>使用Socket和输入流初始化一个Socket流，该流将从输入流中读取数据，并把输出的数据写入到Socket中</summary>
        /// <param name="socket"></param>
        /// <param name="inputStream"></param>
        /// <param name="remote"></param>
        public SocketStream(Socket socket, Stream inputStream, EndPoint remote)
            : base(inputStream, Stream.Null)
        {
            Socket = socket;
            RemoteEndPoint = remote as IPEndPoint;
        }
        #endregion

        #region 重载
        /// <summary>读取数据，如果初始化时指定了输入流，则从输入流读取数据，否则从Socket中读取数据</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (InputStream == Stream.Null)
                return Socket.Receive(buffer, offset, count, SocketFlags.None);
            else
                return base.Read(buffer, offset, count);
        }

        /// <summary>写入数据，经Socket向网络发送</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // 兼容IPV6
            if (!RemoteEndPoint.Address.IsAny() && RemoteEndPoint.Port != 0)
                Socket.SendTo(buffer, offset, count, SocketFlags.None, RemoteEndPoint);
            else
                Socket.Send(buffer, offset, count, SocketFlags.None);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 重新设置流
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="input"></param>
        /// <param name="remote"></param>
        public void Reset(Socket socket, Stream input, EndPoint remote)
        {
            Socket = socket;
            RemoteEndPoint = remote as IPEndPoint;

            InputStream = input;
        }
        #endregion
    }
}