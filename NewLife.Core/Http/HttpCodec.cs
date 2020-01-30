using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Http
{
    /// <summary>Http编解码器</summary>
    public class HttpCodec : Handler
    {
        #region 属性
        /// <summary>允许分析头部。默认false</summary>
        /// <remarks>
        /// 分析头部对性能有一定损耗
        /// </remarks>
        public Boolean AllowParseHeader { get; set; }
        #endregion

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

            // 是否Http请求
            var isGet = pk.Count >= 4 && pk[0] == 'G' && pk[1] == 'E' && pk[2] == 'T' && pk[3] == ' ';
            var isPost = pk.Count >= 5 && pk[0] == 'P' && pk[1] == 'O' && pk[2] == 'S' && pk[3] == 'T' && pk[4] == ' ';

            // 该连接第一包检查是否Http
            var ext = context.Owner as IExtend;
            if (!(ext["Encoder"] is HttpEncoder))
            {
                // 第一个请求必须是GET/POST，才执行后续操作
                if (!isGet && !isPost) return base.Read(context, message);

                ext["Encoder"] = new HttpEncoder();
            }

            // 检查是否有未完成消息
            if (!(ext["Message"] is HttpMessage msg))
            {
                // 解码得到消息
                msg = new HttpMessage();
                if (!msg.Read(pk)) throw new XException("Http请求头不完整");

                if (AllowParseHeader && !msg.ParseHeaders()) throw new XException("Http头部解码失败");

                // GET请求一次性过来，暂时不支持头部被拆为多包的场景
                if (isGet)
                {
                    // 匹配输入回调，让上层事件收到分包信息
                    context.FireRead(msg);
                }
                // POST可能多次，最典型的是头部和主体分离
                else
                {
                    // 消息完整才允许上报
                    if (msg.ContentLength == 0 || msg.ContentLength > 0 && msg.Payload != null && msg.Payload.Total >= msg.ContentLength)
                    {
                        // 匹配输入回调，让上层事件收到分包信息
                        context.FireRead(msg);
                    }
                    else
                    {
                        // 请求不完整，拷贝一份，避免缓冲区重用
                        if (msg.Header != null) msg.Header = msg.Header.Clone();
                        if (msg.Payload != null) msg.Payload = msg.Payload.Clone();

                        ext["Message"] = msg;
                    }
                }
            }
            else
            {
                // 数据包拼接到上一个未完整消息中
                if (msg.Payload == null)
                    msg.Payload = pk;
                else
                    msg.Payload.Append(pk);

                // 消息完整才允许上报
                if (msg.ContentLength == 0 || msg.ContentLength > 0 && msg.Payload != null && msg.Payload.Total >= msg.ContentLength)
                {
                    // 匹配输入回调，让上层事件收到分包信息
                    context.FireRead(msg);

                    // 移除消息
                    //ext.Items.Remove("Message");
                    ext["Message"] = null;
                }
            }

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
        #region 属性
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

        /// <summary>请求方法</summary>
        public String Method { get; set; }

        /// <summary>请求资源</summary>
        public String Uri { get; set; }

        /// <summary>内容长度</summary>
        public Int32 ContentLength { get; set; } = -1;

        /// <summary>头部集合</summary>
        public IDictionary<String, String> Headers { get; set; }
        #endregion

        #region 方法
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

            //var isGet = pk.Count >= 4 && pk[0] == 'G' && pk[1] == 'E' && pk[2] == 'T' && pk[3] == ' ';
            //var isPost = pk.Count >= 5 && pk[0] == 'P' && pk[1] == 'O' && pk[2] == 'S' && pk[3] == 'T' && pk[4] == ' ';
            //if (isGet)
            //    Method = "GET";
            //else if (isPost)
            //    Method = "POST";

            return true;
        }

        /// <summary>解码头部</summary>
        public virtual Boolean ParseHeaders()
        {
            var pk = Header;
            if (pk == null || pk.Total == 0) return false;

            // 请求方法 GET / HTTP/1.1
            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            var ss = pk.ToStr().Split(Environment.NewLine);
            {
                var kv = ss[0].Split(" ");
                if (kv != null && kv.Length >= 3)
                {
                    Method = kv[0].Trim();
                    Uri = kv[1].Trim();
                }
            }
            for (var i = 1; i < ss.Length; i++)
            {
                var kv = ss[i].Split(":");
                if (kv != null && kv.Length >= 2)
                {
                    dic[kv[0].Trim()] = kv[1].Trim();
                }
            }
            Headers = dic;

            // 内容长度
            if (dic.TryGetValue("Content-Length", out var str))
                ContentLength = str.ToInt();

            return true;
        }

        /// <summary>把消息转为封包</summary>
        /// <returns></returns>
        public virtual Packet ToPacket()
        {
            // 使用子数据区，不改变原来的头部对象
            var pk = Header.Slice(0, -1);
            pk.Next = NewLine;
            //pk.Next = new[] { (Byte)'\r', (Byte)'\n' };

            var pay = Payload;
            if (pay != null && pay.Total > 0) pk.Append(pay);

            return pk;
        }
        #endregion
    }
}