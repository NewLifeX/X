using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>Json编码器</summary>
    public class JsonEncoder : EncoderBase, IEncoder
    {
        /// <summary>编码</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>编码</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override Packet OnEncode(String action, Int32 code, Object value)
        {
            if (code > 0)
            {
                // 不支持序列化异常
                if (value is Exception ex) value = ex.GetTrue()?.Message;
                if (value is String err) value = new { Code = code, Error = err };
            }

            var json = value.ToJson();
            WriteLog("{0}=>{1}", action, json);

            return json.GetBytes();
        }

        /// <summary>解码</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override Object OnDecode(String action, Packet data)
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
    }
}