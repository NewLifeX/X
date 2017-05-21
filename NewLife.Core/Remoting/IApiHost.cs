using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
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

        /// <summary>过滤器</summary>
        IList<IFilter> Filters { get; }

        /// <summary>接口动作管理器</summary>
        IApiManager Manager { get; }

        ///// <summary>是否默认匿名访问</summary>
        //Boolean Anonymous { get; set; }

        /// <summary>是否加密</summary>
        Boolean Encrypted { get; set; }

        /// <summary>是否压缩</summary>
        Boolean Compressed { get; set; }

        /// <summary>收到请求</summary>
        event EventHandler<ApiMessageEventArgs> Received;

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

    /// <summary>消息事件参数</summary>
    public class ApiMessageEventArgs : EventArgs
    {
        /// <summary>会话</summary>
        public IApiSession Session { get; internal set; }

        /// <summary>负载数据</summary>
        public IMessage Message { get; internal set; }

        /// <summary>是否已处理</summary>
        public Boolean Handled { get; set; }
    }

    /// <summary>Api主机助手</summary>
    public static class ApiHostHelper
    {
        /// <summary>调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="host"></param>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public static async Task<TResult> InvokeAsync<TResult>(IApiHost host, IApiSession session, String action, Object args = null, IDictionary<String, Object> cookie = null)
        {
            if (session == null) return default(TResult);

            var enc = host.Encoder;
            var data = enc.Encode(action, args, cookie);
            //var data = cookie != null && cookie.Count > 0 ? enc.Encode(new { action, args, cookie }) : enc.Encode(new { action, args });

            var msg = session.CreateMessage(data);

            // 过滤器
            host.ExecuteFilter(session, msg, true);

            var rs = await session.SendAsync(msg).ConfigureAwait(false);
            if (rs == null) return default(TResult);

            // 过滤器
            host.ExecuteFilter(session, rs, false);

            // 特殊返回类型
            if (typeof(TResult) == typeof(Packet)) return (TResult)(Object)rs.Payload;

            var dic = enc.Decode(rs.Payload);
            if (typeof(TResult) == typeof(IDictionary<String, Object>)) return (TResult)(Object)dic;

            //return enc.Decode<TResult>(dic);
            var code = 0;
            Object result = null;
            //enc.TryGet(dic, out code, out result);
            Object cod = null;
            dic.TryGetValue("code", out cod);

            // 参数可能不存在
            dic.TryGetValue("result", out result);
            code = cod.ToInt();

            // 是否成功
            if (code != 0) throw new ApiException(code, result + "");

            if (result == null) return default(TResult);
            if (typeof(TResult) == typeof(Object)) return (TResult)result;

            // 返回
            return enc.Convert<TResult>(result);
        }

        /// <summary>执行过滤器</summary>
        /// <param name="host"></param>
        /// <param name="session"></param>
        /// <param name="msg"></param>
        /// <param name="issend"></param>
        internal static void ExecuteFilter(this IApiHost host, IApiSession session, IMessage msg, Boolean issend)
        {
            var fs = host.Filters;
            if (fs.Count == 0) return;

            // 接收时需要倒序
            if (!issend) fs = fs.Reverse().ToList();

            var ctx = new ApiFilterContext { Session = session, Packet = msg.Payload, Message = msg, IsSend = issend };
            foreach (var item in fs)
            {
                item.Execute(ctx);
                //Log.Debug("{0}:{1}", item.GetType().Name, ctx.Packet.ToHex());
            }
            msg.Payload = ctx.Packet;
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

            var att = api.Type?.GetCustomAttribute<ApiAttribute>(true);
            if (att != null && att.IsReusable)
            {
                var ts = session["Controller"] as IDictionary<Type, Object>;
                if (ts == null)
                {
                    session["Controller"] = ts = new NullableDictionary<Type, Object>();

                    // 析构时销毁所有从属控制器
                    var sd = session as IDisposable2;
                    if (sd != null) sd.OnDisposed += (s, e) =>
                    {
                        foreach (var item in ts)
                        {
                            item.Value.TryDispose();
                        }
                    };
                }

                controller = ts[api.Type];
                if (controller == null) controller = ts[api.Type] = api.Type.CreateInstance();

                return controller;
            }

            controller = api.Type.CreateInstance();

            return controller;
        }
    }
}