using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Common;

namespace NewLife.Net.Sockets
{
    /// <summary>基础Socket接口</summary>
    /// <remarks>
    /// 主要是对Socket封装一层，把所有异步操作结果转移到事件中<see cref="Completed"/>去。
    /// </remarks>
    public interface ISocket : IDisposable2
    {
        #region 属性
        /// <summary>协议类型</summary>
        ProtocolType ProtocolType { get; }

        /// <summary>监听本地地址</summary>
        IPAddress Address { get; set; }

        /// <summary>监听端口</summary>
        int Port { get; set; }

        /// <summary>地址族</summary>
        AddressFamily AddressFamily { get; set; }

        /// <summary>本地终结点</summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>远程终结点</summary>
        IPEndPoint RemoteEndPoint { get; }

        /// <summary>是否使用线程池处理事件。建议仅在事件处理非常耗时时使用线程池来处理。</summary>
        bool UseThreadPool { get; set; }

        /// <summary>禁用接收延迟，收到数据后马上建立异步读取再处理本次数据</summary>
        bool NoDelay { get; set; }

        ///// <summary>允许将套接字绑定到已在使用中的地址。</summary>
        //bool ReuseAddress { get; set; }

        ///// <summary>缓冲区大小</summary>
        //int BufferSize { get; set; }

        ///// <summary>数据字典</summary>
        //IDictionary Items { get; }

        ///// <summary>是否已经释放</summary>
        //Boolean Disposed { get; }

        /// <summary>接收数据包统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        IStatistics Statistics { get; }
        #endregion

        #region 方法
        /// <summary>绑定本地终结点</summary>
        void Bind();

        /// <summary>关闭网络操作</summary>
        void Close();

        ///// <summary>从池里拿一个对象</summary>
        //NetEventArgs Pop();

        ///// <summary>把对象归还到池里</summary>
        //void Push(NetEventArgs e);

        /// <summary>获取相对于指定远程地址的本地地址</summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        IPAddress GetRelativeAddress(IPAddress remote);

        /// <summary>获取相对于指定远程地址的本地地址</summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        IPEndPoint GetRelativeEndPoint(IPAddress remote);
        #endregion

        #region 事件
        /// <summary>完成事件，将在工作线程中被调用，不要占用太多时间。</summary>
        event EventHandler<NetEventArgs> Completed;

        /// <summary>错误发生/断开连接时</summary>
        event EventHandler<NetEventArgs> Error;
        #endregion
    }
}