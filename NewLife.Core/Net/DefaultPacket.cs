using System;
using System.Net;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net
{
    /// <summary>新生命标准网络封包协议</summary>
    public class DefaultPacket : PacketProvider
    {
        /// <summary>实例化标准封包</summary>
        public DefaultPacket()
        {
            Offset = 2;
            Size = 2;
        }

        /// <summary>创建消息</summary>
        /// <param name="pk">数据包</param>
        /// <returns></returns>
        public override IMessage CreateMessage(Packet pk)
        {
            var msg = new DefaultMessage();
            msg.Read(pk);

            return msg;
        }
    }

    /// <summary>标准封包工厂</summary>
    public class Default : IPacketFactory
    {
        /// <summary>服务端多会话共用</summary>
        private IPacketQueue _queue;

        /// <summary>创建粘包处理实例，内含缓冲区，不同会话不能共用</summary>
        /// <returns></returns>
        public virtual IPacket Create()
        {
            if (_queue == null) _queue = new MyQueue();

            return new DefaultPacket { Queue = _queue };
        }

        class MyQueue : DefaultPacketQueue
        {
            /// <summary>请求和响应是否匹配</summary>
            /// <param name="owner">拥有者</param>
            /// <param name="remote">远程</param>
            /// <param name="request">请求的数据</param>
            /// <param name="response">响应的数据</param>
            /// <returns></returns>
            protected override Boolean IsMatch(Object owner, IPEndPoint remote, Packet request, Packet response)
            {
                if (request.Count < 4 || response.Count < 4) return false;

                // 序号相等
                if (request[1] != response[1]) return false;

                return true;
            }
        }
    }
}