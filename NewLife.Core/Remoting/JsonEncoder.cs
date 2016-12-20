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

        /// <summary>把对象转换为字节数组</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public byte[] Encode(string action, object args)
        {
            var obj = new { action, args };
            var json = obj.ToJson();

            XTrace.WriteLine("=>{0}", json);

            return json.GetBytes(Encoding);
        }

        /// <summary>把对象转换为字节数组</summary>
        /// <param name="success"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public byte[] Encode(bool success, object result)
        {
            // 不支持序列化异常
            var ex = result as Exception;
            if (ex != null) result = ex.GetTrue()?.Message;

            var obj = new { success, result };
            var json = obj.ToJson();

            XTrace.WriteLine("=>{0}", json);

            return json.GetBytes(Encoding);
        }

        public T Decode<T>(byte[] data)
        {
            var json = data.ToStr(Encoding);

            XTrace.WriteLine("<={0}", json);

            //return json.ToJsonEntity<T>();

            var jp = new JsonParser(data.ToStr(Encoding));
            var dic = jp.Decode() as IDictionary<string, object>;
            if (dic == null) return default(T);

            // 是否成功
            var success = dic["success"].ToBoolean();
            var result = dic["result"];
            if (!success) throw new Exception(result + "");

            // 返回
            var reader = new JsonReader();

            return (T)reader.ToObject(result, typeof(T));
        }

        public bool Decode(byte[] data, out string action, out IDictionary<string, object> args)
        {
            action = null;
            args = null;

            var jp = new JsonParser(data.ToStr(Encoding));
            var dic = jp.Decode() as IDictionary<string, object>;
            if (dic == null) return false;

            action = dic["action"] + "";
            args = dic["args"] as IDictionary<string, object>;

            return true;
        }
    }
}