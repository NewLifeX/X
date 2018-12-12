using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>Json编码器</summary>
    public class JsonEncoder : EncoderBase, IEncoder
    {
        /// <summary>编码 请求/响应</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Packet Encode(String action, Int32 code, Packet value)
        {
            var ms = new MemoryStream();
            ms.Seek(8, SeekOrigin.Begin);

            // 请求：action + args
            // 响应：action + code + result
            var writer = new BinaryWriter(ms);
            writer.Write(action);
            if (code != 0) writer.Write(code);

            // 参数或结果
            var pk2 = value as Packet;
            if (pk2 != null && pk2.Data != null)
            {
                var len = pk2.Total;

                // 不管有没有附加数据，都会写入长度
                writer.Write(len);
            }

            var pk = new Packet(ms.GetBuffer(), 8, (Int32)ms.Length - 8);
            if (pk2 != null && pk2.Data != null) pk.Next = pk2;

            return pk;
        }

        /// <summary>编码</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Packet Encode(String action, Int32 code, Object value)
        {
            if (value == null) return null;

            // 不支持序列化异常
            if (value is Exception ex) value = ex.GetTrue()?.Message;

            var json = value.ToJson();
            WriteLog("{0}=>{1}", action, json);

            return json.GetBytes();
        }

        /// <summary>解码参数</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public IDictionary<String, Object> DecodeParameters(String action, Packet data)
        {
            var json = data.ToStr();
            WriteLog("{0}<={1}", action, json);

            return new JsonParser(json).Decode() as IDictionary<String, Object>;
        }

        /// <summary>解码结果</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Object DecodeResult(String action, Packet data)
        {
            var json = data.ToStr();
            WriteLog("{0}<={1}", action, json);

            return new JsonParser(json).Decode();
        }

        /// <summary>转换为目标类型</summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public Object Convert(Object obj, Type targetType) => JsonHelper.Default.Convert(obj, targetType);

        /// <summary>创建请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual IMessage CreateRequest(String action, Object args)
        {
            // 二进制优先
            if (args is Packet pk)
            {
            }
            else if (args is Byte[] buf)
                pk = new Packet(buf);
            else
                pk = Encode(action, 0, args);
            pk = Encode(action, 0, pk);

            var msg = new DefaultMessage { Payload = pk, };
            return msg;
        }

        /// <summary>创建响应</summary>
        /// <param name="msg"></param>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IMessage CreateResponse(IMessage msg, String action, Int32 code, Object value)
        {
            // 编码响应数据包，二进制优先
            if (!(value is Packet pk)) pk = Encode(action, code, value);
            pk = Encode(action, code, pk);

            // 构造响应消息
            var rs = msg.CreateReply();
            rs.Payload = pk;
            if (code > 0) rs.Error = true;

            return rs;
        }
    }
}