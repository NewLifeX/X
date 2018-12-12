using System;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
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

        /// <summary>获取消息编码器。重载以指定不同的封包协议</summary>
        /// <returns></returns>
        IHandler GetMessageCodec();

        /// <summary>处理消息</summary>
        /// <param name="session"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        IMessage Process(IApiSession session, IMessage msg);

        /// <summary>发送统计</summary>
        ICounter StatInvoke { get; set; }

        /// <summary>接收统计</summary>
        ICounter StatProcess { get; set; }

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
        public static async Task<Object> InvokeAsync(IApiHost host, Object session, Type resultType, String action, Object args, Byte flag)
        {
            if (session == null) return null;

            // 性能计数器，次数、TPS、平均耗时
            //host.StatSend?.Increment();
            var st = host.StatInvoke;
            var sw = st.StartCount();

            // 编码请求，构造消息
            var enc = host.Encoder;
            var msg = enc.CreateRequest(action, args);
            if (flag > 0 && msg is DefaultMessage dm) dm.Flag = flag;

            var invoker = session;
            IMessage rs = null;
            try
            {
                if (session is IApiSession ss)
                {
                    var rs2 = await ss.SendAsync(msg);
                    rs = rs2.Item1;
                    if (rs2.Item2 != null) invoker = rs2.Item2;
                }
                else if (session is ISocketRemote client)
                    rs = (await client.SendMessageAsync(msg)) as IMessage;
                else
                    throw new InvalidOperationException();
                //rs = await session.SendAsync(msg);
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

            if (!enc.Decode(rs, out var act, out var code, out var data)) throw new InvalidOperationException();

            // 是否成功
            if (code != 0) throw new ApiException(code, $"远程[{invoker}]错误！ {data.ToStr()}");

            if (data == null) return null;
            if (resultType == typeof(Packet)) return data;

            // 解码结果
            var result = enc.DecodeResult(action, data);
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
        public static Boolean Invoke(IApiHost host, Object session, String action, Object args, Byte flag = 0)
        {
            if (session == null) return false;

            // 性能计数器，次数、TPS、平均耗时
            //host.StatSend?.Increment();
            var st = host.StatInvoke;

            // 编码请求
            var msg = host.Encoder.CreateRequest(action, args);

            if (msg is DefaultMessage dm)
            {
                dm.OneWay = true;
                if (flag > 0) dm.Flag = flag;
            }

            var sw = st.StartCount();
            try
            {
                if (session is IApiSession ss)
                    return ss.Send(msg);
                else if (session is ISocketRemote client)
                    return client.SendMessage(msg);
                else
                    throw new InvalidOperationException();
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

        #region 统计
        /// <summary>获取统计信息</summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static String GetStat(this IApiHost host)
        {
            if (host == null) return null;

            var sb = Pool.StringBuilder.Get();
            var pf1 = host.StatInvoke;
            var pf2 = host.StatProcess;
            if (pf1 != null && pf1.Value > 0) sb.AppendFormat("请求：{0} ", pf1);
            if (pf2 != null && pf2.Value > 0) sb.AppendFormat("处理：{0} ", pf2);

            if (host is ApiServer svr && svr.Server is NetServer ns)
                sb.Append(ns.GetStat());
            else if (host is ApiClient ac)
            {
                var st1 = ac.StatSend;
                var st2 = ac.StatReceive;
                if (st1 != null && st1.Value > 0) sb.AppendFormat("发送：{0} ", st1);
                if (st2 != null && st2.Value > 0) sb.AppendFormat("接收：{0} ", st2);
            }

            return sb.Put(true);
        }
        #endregion
    }
}