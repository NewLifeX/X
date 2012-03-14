using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Log;

namespace NewLife.Net.Sockets
{
    /// <summary>网络事件参数</summary>
    public class NetEventArgs : SocketAsyncEventArgs, IDisposable
    {
        #region 属性
        static Int32 _gid;

        private Int32 _ID = ++_gid;
        /// <summary>编号</summary>
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private Int32 _Times;
        /// <summary>使用次数</summary>
        public Int32 Times { get { return _Times; } set { _Times = value; } }

        private Boolean _Used;
        /// <summary>使用中</summary>
        /// <remarks>
        /// 尽管能够通过该属性判断参数是否在使用中，然后避免线程池操作失误，但是并没有那么做。
        /// 一个正确的架构，是不会出现事件参数丢失或者被重用的情况。
        /// 所以，该属性主要作为对设计人员的限制，提醒设计人员架构上可能出现了问题。
        /// </remarks>
        public Boolean Used { get { return _Used; } set { _Used = value; } }

        private ISocket _Socket;
        /// <summary>当前对象的使用者，默认就是从对象池<see cref="SocketBase.Pool"/>中借出当前网络事件参数的那个SocketBase。
        /// 比如，如果是Server程序，那么它往往就是与客户端通讯的那个Socket(TcpSession)。
        /// 在TcpServer中，它就是TcpSession。
        /// </summary>
        public ISocket Socket { get { return _Socket; } set { _Socket = value; } }

        private ISocketSession _Session;
        /// <summary>Socket会话</summary>
        public ISocketSession Session { get { return _Session; } set { _Session = value; } }

        private Exception _Error;
        /// <summary>异常信息</summary>
        public Exception Error { get { return _Error; } set { _Error = value; } }

        /// <summary>远程IP终结点</summary>
        public IPEndPoint RemoteIPEndPoint { get { return base.RemoteEndPoint as IPEndPoint; } set { base.RemoteEndPoint = value; } }

        private Boolean _Cancel;
        /// <summary>是否取消后续操作</summary>
        public Boolean Cancel { get { return _Cancel; } set { _Cancel = value; } }
        #endregion

        #region 构造
        /// <summary>析构</summary>
        ~NetEventArgs()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        Boolean disposed;
        void Dispose(Boolean disposing)
        {
            if (disposed) return;
            disposed = true;

            if (disposing) GC.SuppressFinalize(this);

            XTrace.WriteLine("{0}被抛弃！{1} {2}", ID, LastOperation, RemoteIPEndPoint);

            //! 清空缓冲区，这一点非常非常重要，内部有个重叠数据对象，挂在一个全局对象池上，它会Pinned住数据缓冲区，这里必须清空被Pinned住的缓冲区
            SetBuffer(0);

            // 断开所有资源的链接
            _buffer = null;
            _Completed = null;

            _Socket = null;
            _Session = null;
            _Error = null;

            base.Dispose();
        }
        #endregion

        #region 事件
        //private Boolean hasEvent;
        private WeakEventHandler<NetEventArgs> _Completed;
        /// <summary>完成事件。该事件只能设置一个。</summary>
        public new event EventHandler<NetEventArgs> Completed
        {
            add
            {
                if (_Completed != null) throw new Exception("重复设置了事件！");
                //_Completed = value;
                // 使用弱引用事件
                _Completed = new WeakEventHandler<NetEventArgs>(value, null, false);
                // 考虑到该对象将会作为池对象使用，不需要频繁的add和remove事件，所以一次性挂接即可
                //if (!hasEvent)
                {
                    //hasEvent = true;
                    // 必须采用挂接事件的方式，因为只有这样才能改变基类的m_CompletedChanged，从而清除/更新执行上下文
                    base.Completed += OnCompleted;
                }
            }
            remove
            {
                _Completed = null;

                // 必须每次清除事件，在下次使用的时候重新设置，保证每次重新设置事件，清楚/更新执行上下文m_Context
                base.Completed -= OnCompleted;
            }
        }

        private void OnCompleted(Object sender, SocketAsyncEventArgs e)
        {
            EventHandler<NetEventArgs> handler = _Completed;
            if (handler != null) handler(sender, e as NetEventArgs);
        }

        //protected override void OnCompleted(SocketAsyncEventArgs e)
        //{
        //    //base.OnCompleted(e);
        //    if (_Completed != null) _Completed(Socket, e as NetEventArgs);
        //}
        #endregion

        #region 缓冲区
        /// <summary>采用弱引用，及时清理不再使用的内存，避免内存泄漏。</summary>
        private WeakReference<Byte[]> _buffer;

        /// <summary>设置缓冲区。
        /// 为了避免频繁分配内存，可以为每一个事件参数固定挂载一个缓冲区，达到重用的效果。
        /// 但是对于TcpServer的Accept来说，不能设置缓冲区，否则客户端连接的时候不会触发Accept事件，
        /// 而必须等到第一个数据包的到来才触发，那时缓冲区里面同时带有第一个数据包的数据。
        /// 
        /// 所以，考虑把缓冲内置一份到外部，进行控制。
        /// </summary>
        /// <param name="size"></param>
        internal void SetBuffer(Int32 size)
        {
            if (size > 0)
            {
                // 没有缓冲区，或者大小不相同时，重新设置
                if (Buffer == null || Count != size)
                {
                    // 没有缓冲区，或者大小不够时，重新分配
                    Byte[] _buf = _buffer == null ? null : _buffer.Target;
                    if (_buf == null || _buf.Length < size) _buffer = _buf = new Byte[size];

                    SetBuffer(_buf, 0, size);
                }
            }
            else
            {
                // 事件内有缓冲区时才清空，不管它多长，必须清空
                if (Buffer != null) SetBuffer(null, 0, 0);
            }
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

        ///// <summary>Socket数据流。每个网络事件参数带有一个，防止多次声明流对象</summary>
        //private SocketStream socketStream;

        /// <summary>从接收缓冲区获取一个流，该流可用于读取已接收数据，写入数据时向远端发送数据。该流应该在持有事件参数期内使用，否则可能产生冲突。</summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            if (Buffer == null || Buffer.Length < 1 || BytesTransferred < 1) return null;

            Stream ms = new MemoryStream(Buffer, Offset, BytesTransferred);
            return new SocketStream(AcceptSocket, ms, RemoteEndPoint);

            //if (socketStream == null)
            //{
            //    socketStream = new SocketStream(AcceptSocket, ms, RemoteEndPoint);
            //}
            //else
            //{
            //    socketStream.Reset(AcceptSocket, ms, RemoteEndPoint);
            //}

            //return socketStream;
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
                return String.Format("[{0}]{1} {2}", ID, LastOperation, Error.Message);
            else if (SocketError != SocketError.Success)
                return String.Format("[{0}]{1} {2}", ID, LastOperation, SocketError);
            else
                return String.Format("[{0}]{1} BytesTransferred={2}", ID, LastOperation, BytesTransferred);
        }
        #endregion
    }
}