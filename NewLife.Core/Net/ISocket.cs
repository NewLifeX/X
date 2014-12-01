using System;
using System.Net.Sockets;

namespace NewLife.Net
{
    /// <summary>基础Socket接口</summary>
    /// <remarks>
    /// 主要是对Socket封装一层，把所有异步操作结果转移到事件中去。
    /// </remarks>
    public interface ISocket : IDisposable2
    {
        #region 属性
        /// <summary>基础Socket对象</summary>
        Socket Socket { get; /*set;*/ }

        /// <summary>本地地址</summary>
        NetUri Local { get; set; }

        /// <summary>端口</summary>
        Int32 Port { get; set; }

        ///// <summary>接收数据包统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        //IStatistics Statistics { get; }

        ///// <summary>异步操作计数</summary>
        //Int32 AsyncCount { get; }
        #endregion

        #region 方法
        ///// <summary>绑定本地终结点</summary>
        //void Bind();

        ///// <summary>关闭网络操作</summary>
        //void Close();
        #endregion

        #region 事件
        /// <summary>错误发生/断开连接时</summary>
        event EventHandler<ExceptionEventArgs> Error;
        #endregion
    }
}