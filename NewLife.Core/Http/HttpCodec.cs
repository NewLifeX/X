using System;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Http
{
    /// <summary>Http编解码器</summary>
    public class HttpCodec : Handler
    {
        /// <summary>写入数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (message is HttpMessage http)
            {
                message = http.ToPacket();
            }

            return base.Write(context, message);
        }

        /// <summary>读取数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (!(message is Packet pk)) return base.Read(context, message);

            // 解码得到消息

            var msg = new HttpMessage();
            msg.Read(pk);

            // 匹配输入回调，让上层事件收到分包信息
            context.FireRead(msg);

            //if (pk.ToStr(null, 0, 4) == "HTTP")
            //{
            //    var response = new HttpResponse();
            //    if (!response.ParseHeader(pk)) return base.Read(context, message);

            //    // 匹配输入回调，让上层事件收到分包信息
            //    context.FireRead(response);
            //}
            //else
            //{
            //    var request = new HttpRequest();
            //    if (!request.ParseHeader(pk)) return base.Read(context, message);

            //    // 匹配输入回调，让上层事件收到分包信息
            //    context.FireRead(request);
            //}

            return null;
        }
    }

    /// <summary>Http消息</summary>
    public class HttpMessage : IMessage
    {
        /// <summary>是否响应</summary>
        public Boolean Reply { get; set; }

        /// <summary>是否有错</summary>
        public Boolean Error { get; set; }

        /// <summary>单向请求</summary>
        public Boolean OneWay => false;

        /// <summary>头部数据</summary>
        public Packet Header { get; set; }

        /// <summary>负载数据</summary>
        public Packet Payload { get; set; }

        /// <summary>根据请求创建配对的响应消息</summary>
        /// <returns></returns>
        public IMessage CreateReply()
        {
            if (Reply) throw new Exception("不能根据响应消息创建响应消息");

            var msg = new HttpMessage
            {
                Reply = true
            };

            return msg;
        }

        private static readonly Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
        /// <summary>从数据包中读取消息</summary>
        /// <param name="pk"></param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(Packet pk)
        {
            var p = pk.IndexOf(NewLine);
            if (p < 0) return false;

            Header = pk.Slice(0, p);
            Payload = pk.Slice(p + 4);

            return true;
        }

        /// <summary>把消息转为封包</summary>
        /// <returns></returns>
        public virtual Packet ToPacket()
        {
            var pk = Header.Slice(0, -1);
            //pk.Next = NewLine;
            pk.Next = new[] { (Byte)'\r', (Byte)'\n' };

            var pay = Payload;
            if (pay != null && pay.Total > 0) pk.Append(pay);

            return pk;
        }
    }
}