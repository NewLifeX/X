using System;
using System.Collections.Generic;
using System.IO;
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

        private Packet Encode2(String action, Object args)
        {
            var ms = new MemoryStream();
            ms.Seek(4, SeekOrigin.Begin);
            var writer = new BinaryWriter(ms);
            writer.Write(action);

            var str = Log.Enable ? action + "=>" : null;
            if (args != null)
            {
                var json = args.ToJson();
                if (Log.Enable) str += json;

                writer.Write(json);
            }

            var pk = new Packet(ms.GetBuffer(), 4, (Int32)ms.Length - 4);

            //WriteLog("=>{0}", pk.ToStr());
            if (Log.Enable) WriteLog(str);

            return pk;
        }

        /// <summary>编码</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override Packet OnEncode(String action, Int32 code, Object value)
        {
            // 不支持序列化异常
            if (value is Exception ex) value = ex.GetTrue()?.Message;
            if (value is String err) value = new { Code = code, Error = err };

            var json = value.ToJson();
            WriteLog("{0}=>{1}", action, json);

            return json.GetBytes();
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