using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Threading;

namespace NewLife.Net.Handlers
{
    /// <summary>消息封包</summary>
    public class MessageCodec<T> : Handler
    {
        /// <summary>消息队列。用于匹配请求响应包</summary>
        public IMatchQueue Queue { get; set; } = new DefaultMatchQueue();

        /// <summary>调用超时时间。默认30_000ms</summary>
        public Int32 Timeout { get; set; } = 30_000;

        /// <summary>使用数据包，写入时数据包转消息，读取时消息自动解包返回数据负载。默认true</summary>
        public Boolean UserPacket { get; set; } = true;

        /// <summary>写入数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (message is T msg)
            {
                message = Encode(context, msg);
                if (message == null) return null;

                // 加入队列
                AddToQueue(context, msg);
            }

            return base.Write(context, message);
        }

        /// <summary>编码</summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual Object Encode(IHandlerContext context, T msg)
        {
            if (msg is IMessage msg2) return msg2.ToPacket();

            return null;
        }

        /// <summary>加入队列</summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual void AddToQueue(IHandlerContext context, T msg)
        {
            if (msg != null && context["TaskSource"] is TaskCompletionSource<Object> source)
            {
                var timeout = Timeout;
                //if (context.Session is ISocketClient client) timeout = client.Timeout;
                Queue.Add(context.Owner, msg, timeout, source);
            }
        }

        /// <summary>读取数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (!(message is Packet pk)) return base.Read(context, message);

            // 解码得到多个消息
            var list = Decode(context, pk);
            if (list == null) return null;

            foreach (var msg in list)
            {
                if (UserPacket && msg is IMessage msg2)
                    message = msg2.Payload;
                else
                    message = msg;

                // 后续处理器，得到最终结果，匹配请求队列
                var rs = base.Read(context, message);

                if (msg is IMessage msg3)
                {
                    // 匹配
                    if (msg3.Reply)
                    {
                        //!!! 处理结果的Packet需要拷贝一份，否交给另一个线程使用会有冲突
                        if (rs is IMessage msg4 && msg4.Payload != null && msg4.Payload == msg3.Payload) msg4.Payload = msg4.Payload.Clone();
                        Queue.Match(context.Owner, msg, rs, IsMatch);
                    }
                }
                else if (rs != null)
                {
                    // 其它消息不考虑响应
                    Queue.Match(context.Owner, msg, rs, IsMatch);
                }

                // 匹配输入回调，让上层事件收到分包信息
                context.FireRead(rs);
            }

            return null;
        }

        /// <summary>解码</summary>
        /// <param name="context"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected virtual IList<T> Decode(IHandlerContext context, Packet pk) => null;

        /// <summary>是否匹配响应</summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        protected virtual Boolean IsMatch(Object request, Object response) => true;

        #region 粘包处理
        /// <summary>分析数据流，得到一帧数据</summary>
        /// <param name="pk">待分析数据包</param>
        /// <param name="codec">参数</param>
        /// <param name="getLength">获取长度</param>
        /// <param name="expire">缓存有效期</param>
        /// <returns></returns>
        protected virtual IList<Packet> Parse(Packet pk, CodecItem codec, Func<Packet, Int32> getLength, Int32 expire = 5000)
        {
            var _ms = codec.Stream;
            var nodata = _ms == null || _ms.Position < 0 || _ms.Position >= _ms.Length;

            var list = new List<Packet>();
            // 内部缓存没有数据，直接判断输入数据流是否刚好一帧数据，快速处理，绝大多数是这种场景
            if (nodata)
            {
                if (pk == null) return list.ToArray();

                var idx = 0;
                while (idx < pk.Total)
                {
                    //var pk2 = new Packet(pk.Data, pk.Offset + idx, pk.Total - idx);
                    var pk2 = pk.Slice(idx);
                    var len = getLength(pk2);
                    if (len <= 0 || len > pk2.Count) break;

                    pk2.Set(pk2.Data, pk2.Offset, len);
                    //pk2.SetSub(0, len);
                    list.Add(pk2);
                    idx += len;
                }
                // 如果没有剩余，可以返回
                if (idx == pk.Total) return list.ToArray();

                // 剩下的
                //pk = new Packet(pk.Data, pk.Offset + idx, pk.Total - idx);
                pk = pk.Slice(idx);
            }

            if (_ms == null) codec.Stream = _ms = new MemoryStream();

            // 加锁，避免多线程冲突
            lock (_ms)
            {
                // 超过该时间后按废弃数据处理
                var now = TimerX.Now;
                if (_ms.Length > _ms.Position && codec.Last.AddMilliseconds(expire) < now)
                {
                    _ms.SetLength(0);
                    _ms.Position = 0;
                }
                codec.Last = now;

                // 合并数据到最后面
                if (pk != null && pk.Total > 0)
                {
                    var p = _ms.Position;
                    _ms.Position = _ms.Length;
                    pk.WriteTo(_ms);
                    _ms.Position = p;
                }

                // 尝试解包
                while (_ms.Position < _ms.Length)
                {
                    //var pk2 = new Packet(_ms.GetBuffer(), (Int32)_ms.Position, (Int32)_ms.Length);
                    var pk2 = new Packet(_ms);
                    var len = getLength(pk2);

                    // 资源不足一包
                    if (len <= 0 || len > pk2.Total) break;

                    // 解包成功
                    pk2.Set(pk2.Data, pk2.Offset, len);
                    //pk2.SetSub(0, len);
                    list.Add(pk2);

                    _ms.Seek(len, SeekOrigin.Current);
                }

                // 如果读完了数据，需要重置缓冲区
                if (_ms.Position >= _ms.Length)
                {
                    _ms.SetLength(0);
                    _ms.Position = 0;
                }

                return list;
            }
        }

        /// <summary>从数据流中获取整帧数据长度</summary>
        /// <param name="pk"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns>数据帧长度（包含头部长度位）</returns>
        protected static Int32 GetLength(Packet pk, Int32 offset, Int32 size)
        {
            if (offset < 0) return pk.Total - pk.Offset;

            var p = pk.Offset;
            // 数据不够，连长度都读取不了
            if (offset >= pk.Total) return 0;

            // 读取大小
            var len = 0;
            switch (size)
            {
                case 0:
                    var ms = pk.GetStream();
                    if (offset > 0) ms.Seek(offset, SeekOrigin.Current);
                    len = ms.ReadEncodedInt();
                    len += (Int32)(ms.Position - offset);
                    break;
                case 1:
                    len = pk[offset];
                    break;
                case 2:
                    len = pk.ReadBytes(offset, 2).ToUInt16();
                    break;
                case 4:
                    len = (Int32)pk.ReadBytes(offset, 4).ToUInt32();
                    break;
                case -2:
                    len = pk.ReadBytes(offset, 2).ToUInt16(0, false);
                    break;
                case -4:
                    len = (Int32)pk.ReadBytes(offset, 4).ToUInt32(0, false);
                    break;
                default:
                    throw new NotSupportedException();
            }

            // 判断后续数据是否足够
            if (len > pk.Total) return 0;

            // 数据长度加上头部长度
            len += Math.Abs(size);

            return len;
        }
        #endregion
    }

    /// <summary>消息编码参数</summary>
    public class CodecItem
    {
        /// <summary>缓存流</summary>
        public MemoryStream Stream;

        /// <summary>最后一次接收</summary>
        public DateTime Last;
    }
}