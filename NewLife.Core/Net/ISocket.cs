using System;
using System.Net.Sockets;
using NewLife.Exceptions;

namespace NewLife.Net
{
    /// <summary>基础Socket接口</summary>
    /// <remarks>
    /// 封装所有基础接口的共有特性！
    /// 
    /// 核心设计理念：事件驱动，接口统一，简单易用！
    /// 异常处理理念：确保主流程简单易用，特殊情况的异常通过事件处理！
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

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        Boolean ThrowException { get; set; }

        /// <summary>接收数据包统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        IStatistics Statistics { get; }

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