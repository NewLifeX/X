using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Threading;

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
        Socket Client { get; /*set;*/ }

        /// <summary>本地地址</summary>
        NetUri Local { get; set; }

        /// <summary>端口</summary>
        Int32 Port { get; set; }

        /// <summary>管道</summary>
        IPipeline Pipeline { get; set; }

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        Boolean ThrowException { get; set; }

        /// <summary>异步处理接收到的数据。</summary>
        /// <remarks>异步处理有可能造成数据包乱序，特别是Tcp。true利于提升网络吞吐量。false避免拷贝，提升处理速度</remarks>
        Boolean ProcessAsync { get; set; }

        /// <summary>发送统计</summary>
        ICounter StatSend { get; set; }

        /// <summary>接收统计</summary>
        ICounter StatReceive { get; set; }

        /// <summary>日志提供者</summary>
        ILog Log { get; set; }

        /// <summary>是否输出发送日志。默认false</summary>
        Boolean LogSend { get; set; }

        /// <summary>是否输出接收日志。默认false</summary>
        Boolean LogReceive { get; set; }
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
    public interface ISocketRemote : ISocket, IExtend
    {
        #region 属性
        /// <summary>远程地址</summary>
        NetUri Remote { get; set; }

        /// <summary>通信开始时间</summary>
        DateTime StartTime { get; }

        /// <summary>最后一次通信时间，主要表示会话活跃时间，包括收发</summary>
        DateTime LastTime { get; }

        /// <summary>缓冲区大小</summary>
        Int32 BufferSize { get; set; }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定
        /// </remarks>
        /// <param name="pk">数据包</param>
        /// <returns>是否成功</returns>
        Boolean Send(Packet pk);
        #endregion

        #region 接收
        /// <summary>接收数据。阻塞当前线程等待返回</summary>
        /// <returns></returns>
        Packet Receive();

        /// <summary>数据到达事件</summary>
        event EventHandler<ReceivedEventArgs> Received;
        #endregion

        #region 消息包
        /// <summary>异步发送数据并等待响应</summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        Task<Object> SendMessageAsync(Object message);

        /// <summary>发送消息</summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        Boolean SendMessage(Object message);

        /// <summary>处理数据帧</summary>
        /// <param name="data">数据帧</param>
        void Process(IData data);
        #endregion
    }

    /// <summary>远程通信Socket扩展</summary>
    public static class SocketRemoteHelper
    {
        #region 统计
        /// <summary>获取统计信息</summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static String GetStat(this ISocketRemote socket)
        {
            if (socket == null) return null;

            var st1 = socket.StatSend;
            var st2 = socket.StatReceive;
            if (st1 == null && st2 == null) return null;

            var sb = Pool.StringBuilder.Get();
            if (st1 != null && st1.Value > 0) sb.AppendFormat("发送：{0} ", st1);
            if (st2 != null && st2.Value > 0) sb.AppendFormat("接收：{0} ", st2);

            return sb.Put(true);
        }
        #endregion

        #region 发送
        /// <summary>发送数据流</summary>
        /// <param name="session">会话</param>
        /// <param name="stream">数据流</param>
        /// <returns>返回是否成功</returns>
        public static Boolean Send(this ISocketRemote session, Stream stream)
        {
            // 空数据直接发出
            var remain = stream.Length - stream.Position;
            if (remain == 0) return session.Send(new Byte[0]);

            var buffer = new Byte[session.BufferSize];
            while (true)
            {
                var count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;

                var pk = new Packet(buffer, 0, count);
                if (!session.Send(pk)) return false;

                if (count < buffer.Length) break;
            }
            return true;
        }

        /// <summary>发送字符串</summary>
        /// <param name="session">会话</param>
        /// <param name="msg">要发送的字符串</param>
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
        /// <returns>返回自身，用于链式写法</returns>
        public static Boolean Send(this ISocketRemote session, String msg, Encoding encoding = null)
        {
            if (String.IsNullOrEmpty(msg)) return session.Send(new Byte[0]);

            if (encoding == null) encoding = Encoding.UTF8;
            return session.Send(encoding.GetBytes(msg));
        }

        /// <summary>异步多次发送数据</summary>
        /// <param name="session">会话</param>
        /// <param name="pk">数据包</param>
        /// <param name="times">次数</param>
        /// <param name="msInterval">间隔</param>
        /// <returns></returns>
        public static Boolean SendMulti(this ISocketRemote session, Packet pk, Int32 times, Int32 msInterval)
        {
            if (times <= 1)
            {
                session.Send(pk);
                return true;
            }

            if (msInterval < 10)
            {
                for (var i = 0; i < times; i++)
                {
                    session.Send(pk);
                }
                return true;
            }

            var timer = new TimerX(s =>
            {
                session.Send(pk);

                // 如果次数足够，则把定时器周期置空，内部会删除
                var t = s as TimerX;
                if (--times <= 0)
                {
                    t.Period = 0;
                }
            }, null, 0, msInterval);

            return true;
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
        public static void Add<THandler>(this ISocket session) where THandler : IHandler, new()
        {
            if (session.Pipeline == null) session.Pipeline = new Pipeline();

            session.Pipeline.AddLast(new THandler());
        }

        /// <summary>添加处理器</summary>
        /// <param name="session">会话</param>
        /// <param name="handler">处理器</param>
        public static void Add(this ISocket session, IHandler handler)
        {
            if (session.Pipeline == null) session.Pipeline = new Pipeline();

            session.Pipeline.AddLast(handler);
        }
        #endregion
    }
}