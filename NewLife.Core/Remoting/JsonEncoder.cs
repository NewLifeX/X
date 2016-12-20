using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>
    /// 
    /// </summary>
    public class JsonEncoder : EncoderBase
    {
        /// <summary>编码</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>编码对象</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Byte[] Encode(Object obj)
        {
            var json = obj.ToJson();

            WriteLog("=>{0}", json);

            return json.GetBytes(Encoding);
        }

        /// <summary>解码成为字典</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override IDictionary<String, Object> Decode(Byte[] data)
        {
            var json = data.ToStr(Encoding);

            WriteLog("<={0}", json);

            //return json.ToJsonEntity<T>();

            var jp = new JsonParser(data.ToStr(Encoding));
            var dic = jp.Decode() as IDictionary<string, object>;

            return dic;
        }

        ///// <summary>解码响应</summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="dic"></param>
        ///// <returns></returns>
        //public override T Decode<T>(IDictionary<String, Object> dic)
        //{
        //    if (dic == null) return default(T);

        //    // 是否成功
        //    var success = dic["success"].ToBoolean();
        //    var result = dic["result"];
        //    if (!success) throw new Exception(result + "");

        //    // 返回
        //    var reader = new JsonReader();

        //    return (T)reader.ToObject(result, typeof(T));
        //}

        ///// <summary>解码请求</summary>
        ///// <param name="data"></param>
        ///// <param name="action"></param>
        ///// <param name="args"></param>
        ///// <returns></returns>
        //public override bool Decode(byte[] data, out string action, out IDictionary<string, object> args)
        //{
        //    action = null;
        //    args = null;

        //    var jp = new JsonParser(data.ToStr(Encoding));
        //    var dic = jp.Decode() as IDictionary<string, object>;
        //    if (dic == null) return false;

        //    action = dic["action"] + "";
        //    args = dic["args"] as IDictionary<string, object>;

        //    return true;
        //}

        ///// <summary>转换为对象</summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //public override T Convert<T>(Object obj)
        //{
        //    var reader = new JsonReader();

        //    return (T)reader.ToObject(obj, typeof(T));
        //}

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