using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;

namespace NewLife.Net
{
    /// <summary>发送队列。以队列发送数据包，自动拆分大包，合并小包</summary>
    class SendQueue : DisposeBase
    {
        #region 属性
        public SessionBase Session { get; private set; }

        public Int32 BufferSize { get; private set; }

        private ConcurrentQueue<SocketAsyncEventArgs> _seSendPool = new ConcurrentQueue<SocketAsyncEventArgs>();

        private ConcurrentQueue<QueueItem> _SendQueue = new ConcurrentQueue<QueueItem>();
        #endregion

        #region 构造
        public SendQueue(SessionBase session)
        {
            Session = session;

            BufferSize = session.BufferSize;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            var reason = GetType().Name + (disposing ? "Dispose" : "GC");
            Release(reason);
        }
        #endregion

        #region 主要方法
        internal Boolean Add(Packet pk, IPEndPoint remote)
        {
            var count = pk.Total;
            var ss = Session;
            ss.StatSend.Increment(count);
            if (ss.LogSend) ss.WriteLog("SendAsync [{0}]: {1}", count, pk.ToHex());

            ss.LastTime = DateTime.Now;

            // 打开UDP广播
            if (ss.Local.Type == NetType.Udp && remote != null && Equals(remote.Address, IPAddress.Broadcast)) ss.Client.EnableBroadcast = true;

            // 同时只允许一个异步发送，其它发送放入队列

            // 考虑到超长数据包，拆分为多个包
            if (count <= BufferSize)
            {
                var qi = new QueueItem
                {
                    Packet = pk,
                    Remote = remote
                };

                _SendQueue.Enqueue(qi);
            }
            else
            {
                // 数据包切分，共用数据区，不需要内存拷贝
                var idx = 0;
                while (true)
                {
                    var remain = count - idx;
                    if (remain <= 0) break;

                    var len = Math.Min(remain, BufferSize);

                    var qi = new QueueItem
                    {
                        //qi.Packet = new Packet(pk.Data, pk.Offset + idx, len);
                        Packet = new Packet(pk.ReadBytes(idx, len)),
                        Remote = remote
                    };

                    _SendQueue.Enqueue(qi);

                    idx += len;
                }
            }

            Check(false);

            return true;
        }

        internal void Release(String reason)
        {
            foreach (var item in _seSendPool)
            {
                item.Dispose();
            }
            _seSendPool.TryDispose();
            _seSendPool = null;
            Session.WriteLog("释放SendSA {0} {1}", 1, reason);
        }

        void Check(Boolean io)
        {
            // 如果已销毁，则停止检查发送队列
            if (Session.Client == null || Session.Disposed) return;

            var qu = _SendQueue;
            if (qu.IsEmpty) return;

            // 如果没有在发送，就开始发送
            //if (Interlocked.CompareExchange(ref _Sending, 1, 0) != 0) return;

            QueueItem qi = null;
            if (!qu.TryDequeue(out qi)) return;

            SocketAsyncEventArgs se;
            if (!_seSendPool.TryDequeue(out se))
            {
                var buf = new Byte[BufferSize];
                se = new SocketAsyncEventArgs();
                se.SetBuffer(buf, 0, buf.Length);
                se.Completed += (s, e) => Process(e);
                _seSendPool.Enqueue(se);

                Session.WriteLog("创建SendSA {0}", 1);
            }

            se.RemoteEndPoint = qi.Remote;

            // 拷贝缓冲区，设置长度
            var p = 0;
            var remote = qi.Remote;

            // 为了提高吞吐量，减少数据收发次数，尽可能的把发送队列多个数据包合并成为一个大包发出
            while (true)
            {
                var pk = qi.Packet;
                var len = pk.Total;

                if (pk?.Data == null || se.Buffer == null) break;

                pk.WriteTo(se.Buffer, p);
                p += len;

                // 不足最大长度，试试下一个
                if (!qu.TryPeek(out qi)) break;
                if (qi.Remote + "" != remote + "") break;
                if (p + qi.Packet.Count > BufferSize) break;

                if (!qu.TryDequeue(out qi)) break;
            }

            try
            {
                se.SetBuffer(0, p);
            }
            catch (Exception e)
            {
                Session.WriteLog("SendSAErr {0}", e.Message);
                //未测试下面这句
                //Check(false);
                return;
            }

            if (!Session.OnSendAsync(se))
            {
                if (io)
                    Process(se);
                else
                    Task.Factory.StartNew(s => Process(s as SocketAsyncEventArgs), se);
            }
        }

        void Process(SocketAsyncEventArgs se)
        {
            if (!Session.Active)
            {
                Release("!Active " + se.SocketError);

                return;
            }

            // 判断成功失败
            if (se.SocketError != SocketError.Success)
            {
                // 未被关闭Socket时，可以继续使用
                //if (!se.IsNotClosed())
                {
                    var ex = se.GetException();
                    if (ex != null) Session.OnError("SendAsync", ex);

                    Release("SocketError " + se.SocketError);

                    //if (se.SocketError == SocketError.ConnectionReset) Dispose();
                    if (se.SocketError == SocketError.ConnectionReset) Session.Close("SendAsync " + se.SocketError);

                    return;
                }
            }
            //回收se
            _seSendPool.Enqueue(se);
            // 发送新的数据
            // if (Interlocked.CompareExchange(ref _Sending, 0, 1) == 1) Check(true);
        }
        #endregion

        class QueueItem
        {
            public Packet Packet { get; set; }
            public IPEndPoint Remote { get; set; }
        }
    }
}