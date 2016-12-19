using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    class JsonEncoder : IEncoder
    {
        /// <summary>编码</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public T Decode<T>(Byte[] data)
        {
            var json = data.ToStr(Encoding);

            XTrace.WriteLine("<={0}", json);

            return json.ToJsonEntity<T>();
        }

        public Byte[] Encode(Object obj)
        {
            var json = obj.ToJson();

            XTrace.WriteLine("=>{0}", json);

            return json.GetBytes(Encoding);
        }

        public IDictionary<String, Object> Decode2(Byte[] data)
        {
            var jp = new JsonParser(data.ToStr(Encoding));
            return jp.Decode() as IDictionary<String, Object>;
        }
    }
}