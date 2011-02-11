using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Exceptions;

namespace NewLife.IO
{
    /// <summary>
    /// 数据流客户端，用于与服务端的数据流处理器通讯
    /// </summary>
    public abstract class StreamClient
    {
        #region 属性
        private Uri _Uri;
        /// <summary>服务端地址</summary>
        public Uri Uri
        {
            get { return _Uri; }
            set { _Uri = value; }
        }

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
        /// <summary>
        /// 实例化
        /// </summary>
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
        public abstract Byte[] Send(Byte[] data);

        /// <summary>
        /// 异步发送数据，服务端响应数据将由数据流总线处理
        /// </summary>
        /// <param name="data">待发送数据</param>
        public abstract void SendAsync(Byte[] data);
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
    }
}