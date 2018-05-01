using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>Json编码器</summary>
    public class JsonEncoder : EncoderBase, IEncoder
    {
        /// <summary>编码</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>编码请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public IMessage Encode(String action, Object args)
        {
            var obj = new { action, args };
            var json = obj.ToJson();

            WriteLog("=>{0}", json);

            var msg = new DefaultMessage
            {
                Payload = json.GetBytes(Encoding)
            };

            return msg;
        }

        /// <summary>编码响应</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Packet Encode(String action, Int32 code, Object result)
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
        public IDictionary<String, Object> Decode(Packet pk)
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

        /// <summary>解码请求</summary>
        /// <param name="msg"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Boolean TryGetRequest(IMessage msg, out String action, out IDictionary<String, Object> args)
        {
            action = null;
            args = null;

            if (msg.Reply) return false;

            var dic = Decode(msg.Payload);
            action = dic["action"] as String;
            if (action.IsNullOrEmpty()) return false;

            args = dic["args"] as IDictionary<String, Object>;

            return true;
        }

        /// <summary>解码响应</summary>
        /// <param name="msg"></param>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Boolean TryGetResponse(IMessage msg, out Int32 code, out Object result)
        {
            code = 0;
            result = null;

            if (!msg.Reply) return false;

            try
            {
                var dic = Decode(msg.Payload);

                code = dic["code"].ToInt();
                result = dic["result"];
            }
            catch (Exception ex)
            {
                //XTrace.WriteException(ex);
                //XTrace.WriteLine("{0} {1}", msg, msg.Payload.ToStr());

                throw;
            }

            return true;
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