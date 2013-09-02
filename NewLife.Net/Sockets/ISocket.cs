using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Common;

namespace NewLife.Net.Sockets
{
    /// <summary>基础Socket接口</summary>
    /// <remarks>
    /// 主要是对Socket封装一层，把所有异步操作结果转移到事件中去。
    /// </remarks>
    public interface ISocket : ISocketAddress, IDisposable2
    {
        #region 属性
        /// <summary>基础Socket对象</summary>
        Socket Socket { get; set; }

        /// <summary>监听本地地址</summary>
        IPAddress Address { get; set; }

        /// <summary>监听端口</summary>
        Int32 Port { get; set; }

        /// <summary>地址族</summary>
        AddressFamily AddressFamily { get; set; }

        ///// <summary>是否使用线程池处理事件。建议仅在事件处理非常耗时时使用线程池来处理。</summary>
        //bool UseThreadPool { get; set; }

        ///// <summary>禁用接收延迟，收到数据后马上建立异步读取再处理本次数据</summary>
        //bool NoDelay { get; set; }

        ///// <summary>允许将套接字绑定到已在使用中的地址。</summary>
        //bool ReuseAddress { get; set; }

        ///// <summary>缓冲区大小</summary>
        //int BufferSize { get; set; }

        ///// <summary>数据字典</summary>
        //IDictionary Items { get; }

        /// <summary>接收数据包统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        IStatistics Statistics { get; }

        /// <summary>异步操作计数</summary>
        Int32 AsyncCount { get; }
        #endregion

        #region 方法
        /// <summary>绑定本地终结点</summary>
        void Bind();

        /// <summary>关闭网络操作</summary>
        void Close();
        #endregion

        #region 事件
        /// <summary>错误发生/断开连接时</summary>
        event EventHandler<ExceptionEventArgs> Error;
        #endregion
    }
}