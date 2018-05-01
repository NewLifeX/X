using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>二进制编码器</summary>
    public class BinaryEncoder : EncoderBase, IEncoder
    {
        /// <summary>编码</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>编码请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public IMessage Encode(String action, Object args)
        {
            var ms = new MemoryStream();
            ms.Seek(4, SeekOrigin.Begin);

            // 写入服务名
            var writer = new BinaryWriter(ms);
            writer.Write(action);

            // 写入参数
            if (args != null)
            {
                if (args is Packet pk)
                    pk.WriteTo(ms);
                else
                {
                    var bn = new Binary { Stream = ms };
                    bn.Write(args);
                }
            }

            // 构造消息
            var msg = new DefaultMessage
            {
                Payload = new Packet(ms.GetBuffer(), 4, (Int32)ms.Length - 4)
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

            var ms = new MemoryStream();
            ms.Seek(4, SeekOrigin.Begin);

            // 写入服务名
            var writer = new BinaryWriter(ms);
            //writer.Write(action);
            writer.Write(code);

            // 写入参数
            if (result != null)
            {
                if (result is Packet pk)
                    pk.WriteTo(ms);
                else
                {
                    var bn = new Binary { Stream = ms };
                    bn.Write(result);
                }
            }

            return new Packet(ms.GetBuffer(), 4, (Int32)ms.Length - 4);
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

            // 读取服务名
            var ms = msg.Payload.GetStream();
            var reader = new BinaryReader(ms);
            action = reader.ReadString();
            if (action.IsNullOrEmpty()) return false;

            // 读取参数
            args = ms as IDictionary<String, Object>;

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

            // 读取服务名
            var ms = msg.Payload.GetStream();
            var reader = new BinaryReader(ms);
            code = reader.ReadInt32();

            // 读取结果
            result = ms;

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
        public Object Convert(Object obj, Type targetType)
        {
            if (obj is Stream ms)
            {
                var bn = new Binary { Stream = ms, EncodeInt = true };
                return bn.Read(targetType);
            }

            return JsonHelper.Default.Convert(obj, targetType);
        }

        //class Request
        //{
        //    public String Action;
        //    public Object Args;
        //}

        //class Response
        //{
        //    public String Action;
        //    public Int32 Code;
        //    public Object Result;
        //}
    }
}