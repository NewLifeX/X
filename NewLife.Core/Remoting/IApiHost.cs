using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>Api主机</summary>
    public interface IApiHost
    {
        /// <summary>编码器</summary>
        IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        IApiHandler Handler { get; set; }

        /// <summary>接口动作管理器</summary>
        IApiManager Manager { get; }

        /// <summary>处理消息</summary>
        /// <param name="session"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        IMessage Process(IApiSession session, IMessage msg);

        /// <summary>日志</summary>
        ILog Log { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void WriteLog(String format, params Object[] args);
    }

    /// <summary>Api主机助手</summary>
    public static class ApiHostHelper
    {
        /// <summary>调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="host"></param>
        /// <param name="session"></param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public static async Task<TResult> InvokeAsync<TResult>(IApiHost host, IApiSession session, String action, Object args, Byte flag)
        {
            if (session == null) return default(TResult);

            // 编码请求
            var enc = host.Encoder;
            var pk = enc.Encode(action, 0, args);
            pk = Encode(action, 0, pk);

            // 构造消息
            var msg = new DefaultMessage { Payload = pk, };
            if (flag > 0) msg.Flag = flag;

            var rs = await session.SendAsync(msg);
            if (rs == null) return default(TResult);

            // 特殊返回类型
            var rtype = typeof(TResult);
            if (rtype == typeof(IMessage)) return (TResult)rs;
            if (rtype == typeof(Packet)) return (TResult)(Object)rs.Payload;

            if (!Decode(rs, out var act, out var code, out var data)) throw new InvalidOperationException();

            // 是否成功
            if (code != 0) throw new ApiException(code, data.ToStr());

            if (data == null) return default(TResult);
            if (typeof(TResult) == typeof(Packet)) return (TResult)(Object)data;

            var result = enc.Decode(action, data);
            if (result is TResult || rtype == typeof(Object)) return (TResult)(Object)result;

            // 返回
            return enc.Convert<TResult>(result);
        }

        /// <summary>调用</summary>
        /// <param name="host"></param>
        /// <param name="session"></param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public static Boolean Invoke(IApiHost host, IApiSession session, String action, Object args, Byte flag = 0)
        {
            if (session == null) return false;

            // 编码请求
            var enc = host.Encoder;
            var pk = enc.Encode(action, 0, args);
            pk = Encode(action, 0, pk);

            // 构造消息
            var msg = new DefaultMessage
            {
                OneWay = true,
                Payload = pk,
            };
            if (flag > 0) msg.Flag = flag;

            return session.Send(msg);
        }

        /// <summary>创建控制器实例</summary>
        /// <param name="host"></param>
        /// <param name="session"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        public static Object CreateController(this IApiHost host, IApiSession session, ApiAction api)
        {
            var controller = api.Controller;
            if (controller != null) return controller;

            controller = api.Type.CreateInstance();

            return controller;
        }

        #region 编码/解码
        /// <summary>编码 请求/响应</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Packet Encode(String action, Int32 code, Packet value)
        {
            var ms = new MemoryStream();
            ms.Seek(4, SeekOrigin.Begin);

            // 请求：action + args
            // 响应：code + action + result
            var writer = new BinaryWriter(ms);
            if (code != 0) writer.Write(code);
            writer.Write(action);

            // 参数或结果
            var pk2 = value as Packet;
            var len = pk2.Total;

            // 不管有没有附加数据，都会写入长度
            ms.WriteEncodedInt(len);

            var pk = new Packet(ms.GetBuffer(), 4, (Int32)ms.Length - 4);
            if (pk2 != null) pk.Next = pk2;

            return pk;
        }

        /// <summary>解码 请求/响应</summary>
        /// <param name="msg">消息</param>
        /// <param name="action">服务动作</param>
        /// <param name="code">错误码</param>
        /// <param name="value">参数或结果</param>
        /// <returns></returns>
        public static Boolean Decode(IMessage msg, out String action, out Int32 code, out Packet value)
        {
            action = null;
            code = 0;
            value = null;

            // 请求：action + args
            // 响应：code + action + result
            var ms = msg.Payload.GetStream();
            var reader = new BinaryReader(ms);

            if (msg.Reply && msg is DefaultMessage dm && dm.Error) code = reader.ReadInt32();
            action = reader.ReadString();
            if (action.IsNullOrEmpty()) return false;

            // 参数或结果
            if (ms.Length > ms.Position)
            {
                var len = ms.ReadEncodedInt();
                if (len > 0) value = msg.Payload.Sub((Int32)ms.Position, len);
            }

            return true;
        }
        #endregion
    }
}