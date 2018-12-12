using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;

namespace NewLife.Http
{
    /// <summary>Http编码器</summary>
    public class HttpEncoder : EncoderBase, IEncoder
    {
        /// <summary>编码</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Packet Encode(String action, Int32 code, Object value)
        {
            if (value == null) return null;

            // 不支持序列化异常
            if (value is Exception ex) value = ex.GetTrue()?.Message;

            var json = value.ToJson();
            WriteLog("{0}=>{1}", action, json);

            return json.GetBytes();
        }

        /// <summary>解码参数</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public IDictionary<String, Object> DecodeParameters(String action, Packet data)
        {
            var str = data.ToStr();
            WriteLog("{0}<={1}", action, str);
            if (!str.IsNullOrEmpty()) return str.SplitAsDictionary("=", "&").ToDictionary(e => e.Key, e => (Object)e.Value);

            return null;
        }

        /// <summary>解码结果</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Object DecodeResult(String action, Packet data)
        {
            var json = data.ToStr();
            WriteLog("{0}<={1}", action, json);

            return new JsonParser(json).Decode();
        }

        /// <summary>转换为目标类型</summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public Object Convert(Object obj, Type targetType) => JsonHelper.Default.Convert(obj, targetType);

        #region 编码/解码
        ///// <summary>编码 请求/响应</summary>
        ///// <param name="action"></param>
        ///// <param name="code"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public virtual Packet Encode(String action, Int32 code, Packet value)
        //{
        //    return null;
        //}

        /// <summary>创建请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual IMessage CreateRequest(String action, Object args)
        {
            // 请求方法 GET / HTTP/1.1
            var req = new HttpMessage();
            var sb = Pool.StringBuilder.Get();
            sb.Append("GET ");
            sb.Append(action);

            // 准备参数，二进制优先
            if (args is Packet pk)
            {
            }
            else if (args is Byte[] buf)
                pk = new Packet(buf);
            else
            {
                pk = null;

                // url参数
                sb.Append("?");
                if (args.GetType().GetTypeCode() != TypeCode.Object)
                {
                    sb.Append(args);
                }
                else
                {
                    foreach (var item in args.ToDictionary())
                    {
                        sb.AppendFormat("{0}={1}", item.Key, item.Value);
                    }
                }
            }
            sb.AppendLine(" HTTP/1.1");

            if (pk != null && pk.Total > 0)
            {
                sb.AppendFormat("Content-Length:{0}\r\n", pk.Total);
                sb.AppendLine("Content-Type:application/json");
            }
            sb.AppendLine("Connection:keep-alive");

            req.Header = sb.Put(true).GetBytes();

            return req;
        }

        /// <summary>创建响应</summary>
        /// <param name="msg"></param>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IMessage CreateResponse(IMessage msg, String action, Int32 code, Object value)
        {
            if (code <= 0) code = 200;

            // 编码响应数据包，二进制优先
            if (!(value is Packet pk)) pk = Encode(action, code, value);

            // 构造响应消息
            var rs = new HttpMessage
            {
                Payload = pk
            };
            if (code >= 500) rs.Error = true;

            // HTTP/1.1 502 Bad Gateway
            var sb = Pool.StringBuilder.Get();
            sb.Append("HTTP/1.1 ");
            sb.Append(code);
            if (code < 500)
                sb.AppendLine(" OK");
            else
                sb.AppendLine(" Error");

            sb.AppendFormat("Content-Length:{0}\r\n", pk.Total);
            sb.AppendLine("Content-Type:application/json");
            sb.AppendLine("Connection:keep-alive");

            rs.Header = sb.Put(true).GetBytes();

            return rs;
        }

        /// <summary>解码 请求/响应</summary>
        /// <param name="msg">消息</param>
        /// <param name="action">服务动作</param>
        /// <param name="code">错误码</param>
        /// <param name="value">参数或结果</param>
        /// <returns></returns>
        public override Boolean Decode(IMessage msg, out String action, out Int32 code, out Packet value)
        {
            action = null;
            code = 0;
            value = null;

            if (!(msg is HttpMessage http)) return false;

            // 分析请求方法 GET / HTTP/1.1
            var p = http.Header.IndexOf(new[] { (Byte)'\r', (Byte)'\n' });
            if (p <= 0) return false;

            var line = http.Header.ToStr(null, 0, p);

            var ss = line.Split(" ");
            if (ss.Length < 3) return false;

            // 第二段是地址
            var url = ss[1];
            p = url.IndexOf('?');
            if (p > 0)
            {
                action = url.Substring(1, p - 1);
                value = url.Substring(p + 1).GetBytes();
            }
            else
            {
                action = url.Substring(1);
                value = http.Payload;
            }

            return true;
        }
        #endregion
    }
}