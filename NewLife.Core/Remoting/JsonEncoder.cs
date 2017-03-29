using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
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
            if (json.IsNullOrWhiteSpace()) return new NullableDictionary<String, Object>();

            var jp = new JsonParser(json);
            try
            {
                return jp.Decode() as IDictionary<String, Object>;
            }
            catch (Exception)
            {
                if (XTrace.Debug) XTrace.WriteLine("Json解码错误！" + json);
                throw;
            }
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