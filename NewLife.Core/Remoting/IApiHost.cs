using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Reflection;

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

        /// <summary>发送统计</summary>
        ICounter StatSend { get; set; }

        /// <summary>接收统计</summary>
        ICounter StatReceive { get; set; }

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
        /// <param name="host"></param>
        /// <param name="session"></param>
        /// <param name="resultType">结果类型</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public static async Task<Object> InvokeAsync(IApiHost host, IApiSession session, Type resultType, String action, Object args, Byte flag)
        {
            if (session == null) return null;

            // 性能计数器，次数、TPS、平均耗时
            //host.StatSend?.Increment();
            var st = host.StatSend;
            var sw = st.StartCount();

            // 编码请求
            var enc = host.Encoder;
            var pk = EncodeArgs(enc, action, args);

            // 构造消息
            var msg = new DefaultMessage { Payload = pk, };
            if (flag > 0) msg.Flag = flag;

            IMessage rs = null;
            try
            {
                rs = await session.SendAsync(msg);
                if (rs == null) return null;
            }
            catch (AggregateException aggex)
            {
                var ex = aggex.GetTrue();
                if (ex is TaskCanceledException)
                {
                    throw new TimeoutException($"请求[{action}]超时！", ex);
                }
                throw aggex;
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException($"请求[{action}]超时！", ex);
            }
            finally
            {
                st.StopCount(sw);
            }

            // 特殊返回类型
            if (resultType == typeof(IMessage)) return rs;
            //if (resultType == typeof(Packet)) return rs.Payload;

            if (!Decode(rs, out var act, out var code, out var data)) throw new InvalidOperationException();

            // 是否成功
            if (code != 0) throw new ApiException(code, data.ToStr());

            if (data == null) return null;
            if (resultType == typeof(Packet)) return data;

            // 解码结果
            var result = enc.Decode(action, data);
            if (resultType == typeof(Object)) return result;

            // 返回
            return enc.Convert(result, resultType);
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

            // 性能计数器，次数、TPS、平均耗时
            //host.StatSend?.Increment();
            var st = host.StatSend;

            // 编码请求
            var pk = EncodeArgs(host.Encoder, action, args);

            // 构造消息
            var msg = new DefaultMessage
            {
                OneWay = true,
                Payload = pk,
            };
            if (flag > 0) msg.Flag = flag;

            var sw = st.StartCount();
            try
            {
                return session.Send(msg);
            }
            finally
            {
                st.StopCount(sw);
            }
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
            ms.Seek(8, SeekOrigin.Begin);

            // 请求：action + args
            // 响应：action + code + result
            var writer = new BinaryWriter(ms);
            writer.Write(action);
            if (code != 0) writer.Write(code);

            // 参数或结果
            var pk2 = value as Packet;
            if (pk2 != null && pk2.Data != null)
            {
                var len = pk2.Total;

                // 不管有没有附加数据，都会写入长度
                writer.Write(len);
            }

            var pk = new Packet(ms.GetBuffer(), 8, (Int32)ms.Length - 8);
            if (pk2 != null && pk2.Data != null) pk.Next = pk2;

            return pk;
        }

        private static Packet EncodeArgs(IEncoder enc, String action, Object args)
        {
            // 二进制优先
            if (args is Packet pk)
            {
            }
            else if (args is Byte[] buf)
                pk = new Packet(buf);
            else
                pk = enc.Encode(action, 0, args);
            pk = Encode(action, 0, pk);

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
            // 响应：action + code + result
            var ms = msg.Payload.GetStream();
            var reader = new BinaryReader(ms);

            action = reader.ReadString();
            if (msg.Reply && msg is DefaultMessage dm && dm.Error) code = reader.ReadInt32();
            if (action.IsNullOrEmpty()) throw new Exception("解码错误，无法找到服务名！");

            // 参数或结果
            if (ms.Length > ms.Position)
            {
                var len = reader.ReadInt32();
                if (len > 0) value = msg.Payload.Slice((Int32)ms.Position, len);
            }

            return true;
        }
        #endregion

        #region 统计
        /// <summary>获取统计信息</summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static String GetStat(this IApiHost host)
        {
            if (host == null) return null;

            var sb = new StringBuilder();
            var pf1 = host.StatSend;
            var pf2 = host.StatReceive;
            if (pf1 != null && pf1.Value > 0) sb.AppendFormat("请求：{0} ", pf1);
            if (pf2 != null && pf2.Value > 0) sb.AppendFormat("处理：{0} ", pf2);

            if (host is ApiServer svr && svr.Server is NetServer ns)
                sb.Append(ns.GetStat());
            else if (host is ApiClient ac && ac.Client != null)
                sb.Append(ac.Client.GetStat());

            return sb.ToString();
        }
        #endregion
    }
}