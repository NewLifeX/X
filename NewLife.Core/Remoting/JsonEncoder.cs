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
        /// <summary>编码。请求/响应</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Packet Encode(String action, Int32 code, Packet value)
        {
            // 内存流，前面留空8字节用于协议头4字节（超长8字节）
            var ms = new MemoryStream();
            ms.Seek(8, SeekOrigin.Begin);

            // 请求：action + args
            // 响应：action + code + result
            var writer = new BinaryWriter(ms);
            writer.Write(action);

            // 异常响应才有code
            if (code != 0) writer.Write(code);

            // 参数或结果
            var pk = value;
            if (pk != null) writer.Write(pk.Total);

            var rs = new Packet(ms.GetBuffer(), 8, (Int32)ms.Length - 8)
            {
                Next = pk
            };

            return rs;
        }

        /// <summary>解码参数</summary>
        /// <param name="action">动作</param>
        /// <param name="data">数据</param>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        public IDictionary<String, Object> DecodeParameters(String action, Packet data, IMessage msg)
        {
            var json = data.ToStr();
            WriteLog("{0}[{2:X2}]<={1}", action, json, msg is DefaultMessage dm ? dm.Sequence : 0);

            return JsonParser.Decode(json);
        }

        /// <summary>解码结果</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        public Object DecodeResult(String action, Packet data, IMessage msg)
        {
            var json = data.ToStr();
            WriteLog("{0}[{2:X2}]<={1}", action, json, msg is DefaultMessage dm ? dm.Sequence : 0);

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
            var str = "";
            if (args is Packet pk)
            {
            }
            else if (args is IAccessor acc)
                pk = acc.ToPacket();
            else if (args is Byte[] buf)
                pk = new Packet(buf);
            else if (args != null)
            {
                str = args.ToJson(false, false, false);

                pk = str.GetBytes();
            }
            else
                pk = null;

            if (Log != null && str.IsNullOrEmpty() && pk != null) str = $"[{pk?.Total}]";
            WriteLog("{0}=>{1}", action, str);

            var payload = Encode(action, 0, pk);

            return new DefaultMessage { Payload = payload, };
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
            var str = "";
            if (value is Packet pk)
            {
            }
            else if (value is IAccessor acc)
                pk = acc.ToPacket();
            else if (value is Byte[] buf)
                pk = new Packet(buf);
            else if (value != null)
            {
                // 不支持序列化异常
                if (value is Exception ex) value = ex.GetTrue()?.Message;

                str = value.ToJson(false, false, false);

                pk = str.GetBytes();
            }
            else
                pk = null;

            if (Log != null && str.IsNullOrEmpty() && pk != null) str = $"[{pk?.Total}]";
            WriteLog("{0}[{2:X2}]=>{1}", action, str, msg is DefaultMessage dm ? dm.Sequence : 0);

            var payload = Encode(action, code, pk);

            // 构造响应消息
            var rs = msg.CreateReply();
            rs.Payload = payload;
            if (code > 0) rs.Error = true;

            return rs;
        }
    }
}