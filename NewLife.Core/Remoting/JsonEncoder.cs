using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>Json编码器</summary>
    public class JsonEncoder : EncoderBase
    {
        /// <summary>编码</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>编码对象</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override Byte[] Encode(Object obj)
        {
            var json = obj.ToJson();

            WriteLog("=>{0}", json);

            return json.GetBytes(Encoding);
        }

        /// <summary>解码成为字典</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public override IDictionary<String, Object> Decode(Packet pk)
        {
            var json = pk.ToStr(Encoding);

            WriteLog("<={0}", json);

            var jp = new JsonParser(json);
            var dic = jp.Decode() as IDictionary<String, Object>;

            return dic;
        }

        /// <summary>转换为目标类型</summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public override Object Convert(Object obj, Type targetType)
        {
            var reader = new JsonReader();

            return reader.ToObject(obj, targetType);
        }
    }
}