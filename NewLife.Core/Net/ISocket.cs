﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

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
        /// <summary>名称。主要用于日志输出</summary>
        String Name { get; set; }

        /// <summary>基础Socket对象</summary>
        Socket Client { get; }

        /// <summary>本地地址</summary>
        NetUri Local { get; set; }

        /// <summary>端口</summary>
        Int32 Port { get; set; }

        /// <summary>消息管道。收发消息都经过管道处理器，进行协议编码解码</summary>
        /// <remarks>
        /// 1，接收数据解码时，从前向后通过管道处理器；
        /// 2，发送数据编码时，从后向前通过管道处理器；
        /// </remarks>
        IPipeline Pipeline { get; set; }

        /// <summary>日志提供者</summary>
        ILog Log { get; set; }

        /// <summary>是否输出发送日志。默认false</summary>
        Boolean LogSend { get; set; }

        /// <summary>是否输出接收日志。默认false</summary>
        Boolean LogReceive { get; set; }

        /// <summary>APM性能追踪器</summary>
        ITracer Tracer { get; set; }
        #endregion

        #region 方法
        /// <summary>已重载。日志加上前缀</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void WriteLog(String format, params Object[] args);
        #endregion

        #region 事件
        /// <summary>错误发生/断开连接时</summary>
        event EventHandler<ExceptionEventArgs> Error;
        #endregion
    }

    /// <summary>远程通信Socket，仅具有收发功能</summary>
    public interface ISocketRemote : ISocket, IExtend3
    {
        #region 属性
        /// <summary>标识</summary>
        Int32 ID { get; }

        /// <summary>远程地址</summary>
        NetUri Remote { get; set; }

        /// <summary>最后一次通信时间，主要表示会话活跃时间，包括收发</summary>
        DateTime LastTime { get; }
        #endregion

        #region 发送
        /// <summary>发送原始数据包</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定
        /// </remarks>
        /// <param name="data">数据包</param>
        /// <returns>是否成功</returns>
        Int32 Send(Packet data);
        #endregion

        #region 接收
        /// <summary>接收数据。阻塞当前线程等待返回</summary>
        /// <returns></returns>
        Packet Receive();

        /// <summary>数据到达事件</summary>
        event EventHandler<ReceivedEventArgs> Received;
        #endregion

        #region 消息包
        /// <summary>异步发送消息并等待响应</summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        Task<Object> SendMessageAsync(Object message);

        /// <summary>发送消息，不等待响应</summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        Int32 SendMessage(Object message);

        /// <summary>处理消息数据帧</summary>
        /// <param name="data">数据帧</param>
        void Process(IData data);
        #endregion
    }

    /// <summary>远程通信Socket扩展</summary>
    public static class SocketRemoteHelper
    {
        #region 发送
        /// <summary>发送数据流</summary>
        /// <param name="session">会话</param>
        /// <param name="stream">数据流</param>
        /// <returns>返回是否成功</returns>
        public static Int32 Send(this ISocketRemote session, Stream stream)
        {
            // 空数据直接发出
            var remain = stream.Length - stream.Position;
            if (remain == 0) return session.Send(new Byte[0]);

            var rs = 0;
            var buffer = new Byte[8192];
            while (true)
            {
                var count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;

                var pk = new Packet(buffer, 0, count);
                var count2 = session.Send(pk);
                if (count2 < 0) break;
                rs += count2;

                if (count < buffer.Length) break;
            }
            return rs;
        }

        /// <summary>发送字符串</summary>
        /// <param name="session">会话</param>
        /// <param name="msg">要发送的字符串</param>
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
        /// <returns>返回自身，用于链式写法</returns>
        public static Int32 Send(this ISocketRemote session, String msg, Encoding encoding = null)
        {
            if (String.IsNullOrEmpty(msg)) return session.Send(new Byte[0]);

            if (encoding == null) encoding = Encoding.UTF8;
            return session.Send(encoding.GetBytes(msg));
        }
        #endregion

        #region 接收
        /// <summary>接收字符串</summary>
        /// <param name="session">会话</param>
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
        /// <returns></returns>
        public static String ReceiveString(this ISocketRemote session, Encoding encoding = null)
        {
            var pk = session.Receive();
            if (pk == null || pk.Count == 0) return null;

            return pk.ToStr(encoding ?? Encoding.UTF8);
        }
        #endregion

        #region 消息包
        /// <summary>添加处理器</summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="session">会话</param>
        public static void Add<THandler>(this ISocket session) where THandler : IHandler, new() => GetPipe(session).Add(new THandler());

        /// <summary>添加处理器</summary>
        /// <param name="session">会话</param>
        /// <param name="handler">处理器</param>
        public static void Add(this ISocket session, IHandler handler) => GetPipe(session).Add(handler);

        private static IPipeline GetPipe(ISocket session) => session.Pipeline ??= new Pipeline();
        #endregion
    }
}