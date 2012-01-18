using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Exceptions;

namespace NewLife.IO
{
    /// <summary>数据流客户端，用于与服务端的数据流处理器通讯</summary>
    public abstract class StreamClient
    {
        #region 属性
        private Uri _Uri;
        /// <summary>服务端地址</summary>
        public Uri Uri { get { return _Uri; } set { _Uri = value; } }

        private String _StreamHandlerName;
        /// <summary>数据流总线名称</summary>
        public String StreamHandlerName
        {
            get
            {
                if (_StreamHandlerName == null && Uri != null)
                {
                    _StreamHandlerName = String.Empty;
                    if (!String.IsNullOrEmpty(Uri.AbsolutePath)) _StreamHandlerName = Path.GetFileNameWithoutExtension(Uri.AbsolutePath);
                }
                return _StreamHandlerName;
            }
            set { _StreamHandlerName = value; }
        }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public StreamClient() { }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="uri"></param>
        public StreamClient(Uri uri) { Uri = uri; }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="url"></param>
        public StreamClient(String url) { Uri = new Uri(url); }
        #endregion

        #region 发送数据
        /// <summary>
        /// 同步发送数据
        /// </summary>
        /// <param name="data">待发送数据</param>
        /// <returns>服务端响应数据</returns>
        protected abstract Byte[] Send(Byte[] data);

        /// <summary>
        /// 异步发送数据，服务端响应数据将由数据流总线处理
        /// </summary>
        /// <param name="data">待发送数据</param>
        protected abstract void SendAsync(Byte[] data);
        #endregion

        #region 数据流处理
        /// <summary>
        /// 处理数据流
        /// </summary>
        /// <param name="stream"></param>
        protected virtual void Process(Stream stream)
        {
            String name = StreamHandlerName;
            if (String.IsNullOrEmpty(name)) throw new XException("未指定数据流总线名称StreamHandlerName！");

            StreamHandler.Process(name, stream);
        }
        #endregion

        #region 内部数据流
        /// <summary>内部数据流</summary>
        private InternalStream _Stream;

        /// <summary>
        /// 获取用于收发数据的数据流
        /// </summary>
        /// <returns></returns>
        public virtual Stream GetStream()
        {
            if (_Stream == null) _Stream = new InternalStream(this);
            return _Stream;
        }

        /// <summary>
        /// 内部数据流。重写输入行为，然后使用一个内存流作为输出。
        /// </summary>
        class InternalStream : ReadWriteStream
        {
            private StreamClient _Client;
            /// <summary>数据流客户端</summary>
            public StreamClient Client
            {
                get { return _Client; }
                set { _Client = value; }
            }

            public InternalStream(StreamClient client)
                : base(new MemoryStream(), Stream.Null)
            {
                Client = client;
            }

            #region 重载
            public override void Write(byte[] buffer, int offset, int count)
            {
                CheckArgument(buffer, offset, count);

                Byte[] result = null;
                // 发送数据
                if (offset == 0 && count == buffer.Length)
                {
                    result = Client.Send(buffer);
                }
                else
                {
                    Byte[] bts = new Byte[count];
                    Buffer.BlockCopy(buffer, offset, bts, 0, count);
                    result = Client.Send(bts);
                }

                // 把响应数据写入输入流，供上层应用读取
                if (result != null && result.Length > 0)
                {
                    InputStream.Write(result, 0, result.Length);
                    // 后退
                    InputStream.Seek(-1 * result.Length, SeekOrigin.Current);
                }
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                CheckArgument(buffer, offset, count);

                // 发送数据
                if (offset == 0 && count == buffer.Length)
                {
                    Client.SendAsync(buffer);
                }
                else
                {
                    Byte[] bts = new Byte[count];
                    Buffer.BlockCopy(buffer, offset, bts, 0, count);
                    Client.SendAsync(bts);
                }

                return null;
            }
            #endregion
        }
        #endregion
    }
}