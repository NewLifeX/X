using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net
{
    /// <summary>Http封包</summary>
    [DisplayName("Http封包")]
    public class HttpPacket : IPacket
    {
        #region 属性
        /// <summary>过期时间，超过该时间后按废弃数据处理，默认500ms</summary>
        public Int32 Expire { get; set; } = 500;

        private DateTime _last;
        #endregion

        #region 创建消息
        /// <summary>创建消息</summary>
        /// <param name="pk">数据包</param>
        /// <returns></returns>
        public virtual IMessage CreateMessage(Packet pk)
        {
            // 创建没有头部的消息
            return new Message { Payload = pk };
        }

        /// <summary>加载消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public virtual IMessage LoadMessage(Packet pk)
        {
            if (pk == null || pk.Count == 0) return null;

            // 创建没有头部的消息
            var msg = new Message();
            msg.Read(pk);

            return msg;
        }
        #endregion

        #region 匹配队列
        /// <summary>数据包匹配队列</summary>
        public IPacketQueue Queue { get; set; }

        /// <summary>加入请求队列</summary>
        /// <param name="request">请求的数据</param>
        /// <param name="remote">远程</param>
        /// <param name="msTimeout">超时取消时间</param>
        public virtual Task<Packet> Add(Packet request, IPEndPoint remote, Int32 msTimeout)
        {
            if (Queue == null) Queue = new DefaultPacketQueue();

            return Queue.Add(this, request, remote, msTimeout);
        }

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="response">响应的数据</param>
        /// <param name="remote">远程</param>
        /// <returns></returns>
        public virtual Boolean Match(Packet response, IPEndPoint remote)
        {
            if (Queue == null) return false;

            return Queue.Match(this, response, remote);
        }
        #endregion

        #region 粘包处理
        /// <summary>内部缓存</summary>
        private MemoryStream _ms;

        /// <summary>分析数据流，得到一帧数据</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public virtual Packet[] Parse(Packet pk)
        {
            var nodata = _ms == null || _ms.Position < 0 || _ms.Position >= _ms.Length;

            var list = new List<Packet>();
            // 内部缓存没有数据，直接判断输入数据流是否刚好一帧数据，快速处理，绝大多数是这种场景
            if (nodata)
            {
                if (pk == null) return list.ToArray();

                // 最多只有一个包
                var len = GetLength(pk.GetStream());
                if (len > 0) return new Packet[] { pk };
            }

            if (_ms == null) _ms = new MemoryStream();

            // 加锁，避免多线程冲突
            lock (_ms)
            {
                if (pk != null)
                {
                    // 超过该时间后按废弃数据处理
                    var now = DateTime.Now;
                    if (_last.AddMilliseconds(Expire) < now)
                    {
                        _ms.SetLength(0);
                        _ms.Position = 0;
                    }
                    _last = now;

                    // 拷贝数据到最后面
                    var p = _ms.Position;
                    _ms.Position = _ms.Length;
                    pk.WriteTo(_ms);
                    _ms.Position = p;
                }

                while (_ms.Position < _ms.Length)
                {
                    var len = GetLength(_ms);
                    if (len <= 0) break;

                    var pk2 = new Packet(_ms.ReadBytes(len));
                    list.Add(pk2);
                }

                return list.ToArray();
            }
        }

        /// <summary>从数据流中获取整帧数据长度</summary>
        /// <param name="stream"></param>
        /// <returns>数据帧长度（包含头部长度位）</returns>
        private Int32 GetLength(Stream stream)
        {
            var p = stream.Position;
            // 数据不够，连长度都读取不了
            if (p >= stream.Length) return 0;

            try
            {
                // 读取大小
                var len = 0;
                var body = stream.IndexOf("\r\n\r\n".GetBytes());
                if (body <= 0) return 0;
                body += 4;

                stream.Position = p;
                var idx = stream.IndexOf("Content-Length:".GetBytes());
                if (idx > 0)
                {
                    var idx2 = stream.Position;
                    var idx3 = stream.IndexOf("\r\n".GetBytes());
                    if (idx3 > idx2)
                    {
                        stream.Position = idx2;
                        len = stream.ReadBytes(idx3 - idx2).ToStr().ToInt();
                    }
                }
                else
                {
                    len = (Int32)(stream.Length - body);
                }

                // 判断后续数据是否足够
                if (body + len > stream.Length) return 0;

                // 数据长度加上头部长度
                len += (Int32)body;

                return len;
            }
            finally
            {
                // 恢复位置
                stream.Position = p;
            }
        }
        #endregion
    }

    /// <summary>Http封包工厂</summary>
    [DisplayName("Http封包")]
    public class HttpPacketFactory : IPacketFactory
    {
        /// <summary>服务端多会话共用</summary>
        private IPacketQueue _queue;

        /// <summary>创建粘包处理实例，内含缓冲区，不同会话不能共用</summary>
        /// <returns></returns>
        public virtual IPacket Create()
        {
            if (_queue == null) _queue = new DefaultPacketQueue();

            return new HttpPacket { Queue = _queue };
        }
    }
}