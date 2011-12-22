using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Net;

namespace NewLife.Net.Sockets
{
    /// <summary>网络事件参数</summary>
    public class NetEventArgs : SocketAsyncEventArgs
    {
        #region 属性
        private Int32 _Times;
        /// <summary>使用次数</summary>
        public Int32 Times { get { return _Times; } set { _Times = value; } }

        private Boolean _Used;
        /// <summary>使用中</summary>
        public Boolean Used { get { return _Used; } set { _Used = value; } }

        private ISocket _Socket;
        /// <summary>当前对象的使用者，默认就是从对象池<see cref="SocketBase.Pool"/>中借出当前网络事件参数的那个SocketBase。
        /// 比如，如果是Server程序，那么它往往就是与客户端通讯的那个Socket(TcpSession)。
        /// 在TcpServer中，它就是TcpSession。
        /// </summary>
        public ISocket Socket { get { return _Socket; } set { _Socket = value; } }

        private Exception _Error;
        /// <summary>异常信息</summary>
        public Exception Error { get { return _Error; } set { _Error = value; } }

        /// <summary>远程IP终结点</summary>
        public IPEndPoint RemoteIPEndPoint { get { return base.RemoteEndPoint as IPEndPoint; } set { base.RemoteEndPoint = value; } }
        #endregion

        #region 事件
        private Boolean hasEvent;
        private Delegate _Completed;
        /// <summary>完成事件。该事件只能设置一个。</summary>
        public new event EventHandler<NetEventArgs> Completed
        {
            add
            {
                if (_Completed != null) throw new Exception("重复设置了事件！");
                _Completed = value;
                // 考虑到该对象将会作为池对象使用，不需要频繁的add和remove事件，所以一次性挂接即可
                if (!hasEvent)
                {
                    hasEvent = true;
                    base.Completed += OnCompleted;
                }
            }
            remove { _Completed = null; }
        }

        private void OnCompleted(Object sender, SocketAsyncEventArgs e)
        {
            if (_Completed != null) (_Completed as EventHandler<NetEventArgs>)(sender, e as NetEventArgs);
        }
        #endregion

        #region 辅助
        /// <summary>从接收缓冲区拿字符串，UTF-8编码</summary>
        /// <returns></returns>
        public String GetString(Encoding encoding = null)
        {
            if (Buffer == null || Buffer.Length < 1 || BytesTransferred < 1) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(Buffer, Offset, BytesTransferred);
        }

        /// <summary>Socket数据流。每个网络事件参数带有一个，防止多次声明流对象</summary>
        private SocketStream socketStream;

        /// <summary>从接收缓冲区获取一个流，该流可用于读取已接收数据，写入数据时向远端发送数据</summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            if (Buffer == null || Buffer.Length < 1 || BytesTransferred < 1) return null;

            Stream ms = new MemoryStream(Buffer, Offset, BytesTransferred);
            if (socketStream == null)
            {
                socketStream = new SocketStream(AcceptSocket, ms, RemoteEndPoint);
            }
            else
            {
                socketStream.Reset(AcceptSocket, ms, RemoteEndPoint);
            }

            return socketStream;
        }

        /// <summary>将接收缓冲区中的数据写入流</summary>
        /// <param name="stream"></param>
        public void WriteTo(Stream stream)
        {
            if (Buffer == null || Buffer.Length < 1 || BytesTransferred < 1) return;

            stream.Write(Buffer, Offset, BytesTransferred);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            // 不要取字符串，那样影响效率
            //return String.Format("[{0}]{1}", LastOperation, GetString());

            if (Error != null)
                return String.Format("[{0}]{1}", LastOperation, Error.Message);
            else if (SocketError != SocketError.Success)
                return String.Format("[{0}]{1}", LastOperation, SocketError);
            else
                return String.Format("[{0}]BytesTransferred={1}", LastOperation, BytesTransferred);
        }
        #endregion
    }
}