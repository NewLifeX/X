﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Net
{
    /// <summary>网络服务的会话，每个连接一个会话</summary>
    /// <typeparam name="TServer">网络服务类型</typeparam>
    public class NetSession<TServer> : NetSession where TServer : NetServer
    {
        /// <summary>主服务</summary>
        public virtual TServer Host { get => (this as INetSession).Host as TServer; set => (this as INetSession).Host = value; }
    }

    /// <summary>网络服务的会话，每个连接一个会话</summary>
    /// <remarks>
    /// 实际应用可通过重载OnReceive实现收到数据时的业务逻辑。
    /// </remarks>
    public class NetSession : DisposeBase, INetSession, IExtend3
    {
        #region 属性
        /// <summary>唯一会话标识</summary>
        public virtual Int32 ID { get; internal set; }

        /// <summary>主服务</summary>
        NetServer INetSession.Host { get; set; }

        /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpSession/UdpServer</summary>
        public ISocketSession Session { get; set; }

        /// <summary>服务端</summary>
        public ISocketServer Server { get; set; }

        /// <summary>客户端地址</summary>
        public NetUri Remote => Session?.Remote;

        /// <summary>用户会话数据</summary>
        public IDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>();

        /// <summary>获取/设置 用户会话数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Object this[String key] { get => Items[key]; set => Items[key] = value; }

        /// <summary>数据到达事件</summary>
        public event EventHandler<ReceivedEventArgs> Received;
        #endregion

        #region 方法
        /// <summary>开始会话处理。</summary>
        public virtual void Start()
        {
            if (LogSession && Log != null && Log.Enable) WriteLog("新会话 {0}", Session);

            var ns = (this as INetSession).Host;
            using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Connect");

            OnConnected();

            var ss = Session;
            if (ss != null)
            {
                // 网络会话和Socket会话共用用户会话数据
                Items = ss.Items;

                ss.Received += Ss_Received;
                ss.OnDisposed += (s, e2) => Dispose();
                ss.Error += OnError;
            }
        }

        private void Ss_Received(Object sender, ReceivedEventArgs e)
        {
            var ns = (this as INetSession).Host;
            var tracer = ns?.Tracer;
            using var span = tracer?.NewSpan($"net:{ns.Name}:Receive", Remote + "");

            try
            {
                OnReceive(e);
            }
            catch (Exception ex)
            {
                span?.SetError(ex, e.Message ?? e.Packet);
                throw;
            }
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void Dispose(Boolean disposing)
        {
            var ns = (this as INetSession).Host;
            using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Disconnect");

            OnDisconnected();

            if (LogSession && Log != null && Log.Enable) WriteLog("会话结束 {0}", Session);

            base.Dispose(disposing);

            //Session.Dispose();//去掉这句话，因为在释放的时候Session有的时候为null，会出异常报错，导致整个程序退出。去掉后正常。
            Session.TryDispose();

            //Server = null;
            //Session = null;
        }
        #endregion

        #region 业务核心
        /// <summary>新的客户端连接</summary>
        protected virtual void OnConnected() { }

        /// <summary>客户端连接已断开</summary>
        protected virtual void OnDisconnected() { }

        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected virtual void OnReceive(ReceivedEventArgs e) => Received?.Invoke(this, e);

        /// <summary>错误发生，可能是连接断开</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnError(Object sender, ExceptionEventArgs e) { }
        #endregion

        #region 发送数据
        /// <summary>发送数据</summary>
        /// <param name="data">数据包</param>
        public virtual INetSession Send(Packet data)
        {
            var ns = (this as INetSession).Host;
            using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send", data);

            Session.Send(data);

            return this;
        }

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual INetSession Send(Stream stream)
        {
            var ns = (this as INetSession).Host;
            using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send");

            Session.Send(stream);

            return this;
        }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public virtual INetSession Send(String msg, Encoding encoding = null)
        {
            var ns = (this as INetSession).Host;
            using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send", msg);

            Session.Send(msg, encoding);

            return this;
        }

        /// <summary>通过管道发送消息，不等待响应</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Int32 SendMessage(Object message) => Session.SendMessage(message);

        /// <summary>异步发送并等待响应</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Task<Object> SendMessageAsync(Object message) => Session.SendMessageAsync(message);
        #endregion

        #region 日志
        /// <summary>日志提供者</summary>
        public ILog Log { get; set; }

        /// <summary>是否记录会话日志</summary>
        public Boolean LogSession { get; set; }

        private String _LogPrefix;
        /// <summary>日志前缀</summary>
        public virtual String LogPrefix
        {
            get
            {
                if (_LogPrefix == null)
                {
                    var host = (this as INetSession).Host;
                    var name = host == null ? "" : host.Name;
                    _LogPrefix = $"{name}[{ID}] ";
                }
                return _LogPrefix;
            }
            set { _LogPrefix = value; }
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteLog(String format, params Object[] args)
        {
            if (Log != null && Log.Enable) Log.Info(LogPrefix + format, args);
        }

        /// <summary>输出错误日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteError(String format, params Object[] args)
        {
            if (Log != null && Log.Enable) Log.Error(LogPrefix + format, args);
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{ (this as INetSession).Host?.Name}[{ID}] {Session}";
        #endregion
    }
}