using System;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>Json编码器</summary>
    public class JsonEncoder : EncoderBase, IEncoder
    {
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

        /// <summary>解码</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Object Decode(String action, Packet data)
        {
            var json = data.ToStr();
            WriteLog("{0}<={1}", action, json);

            return new JsonParser(json).Decode();
        }

        /// <summary>转换为对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public T Convert<T>(Object obj) => (T)Convert(obj, typeof(T));

        /// <summary>转换为目标类型</summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public Object Convert(Object obj, Type targetType) => JsonHelper.Default.Convert(obj, targetType);

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