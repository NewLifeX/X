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

        /// <summary>编码请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override Byte[] Encode(String action, Object args)
        {
            var obj = new { action, args };
            var json = obj.ToJson();

            WriteLog("=>{0}", json);

            return json.GetBytes(Encoding);
        }

        /// <summary>编码响应</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override Byte[] Encode(String action, Int32 code, Object result)
        {
            // 不支持序列化异常
            if (result is Exception ex) result = ex.GetTrue()?.Message;

            var obj = new { action, code, result };
            var json = obj.ToJson();

            WriteLog("=>{0}", json);

            return json.GetBytes(Encoding);
        }

        /// <summary>解码成为字典</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public override IDictionary<String, Object> Decode(Packet pk)
        {
            if (pk.Count <= 2) return new NullableDictionary<String, Object>();

            if (pk[0] != '{') throw new Exception("非法Json字符串");

            var json = pk.ToStr(Encoding);

            WriteLog("<={0}", json);
            if (json.IsNullOrWhiteSpace()) return new NullableDictionary<String, Object>();

            var jp = new JsonParser(json);
            try
            {
                return jp.Decode() as IDictionary<String, Object>;
            }
            catch
            {
                if (XTrace.Debug) XTrace.WriteLine("Json解码错误！" + json);
                throw;
            }
        }

        /// <summary>转换为目标类型</summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public override Object Convert(Object obj, Type targetType) => JsonHelper.Default.Convert(obj, targetType);
    }
}