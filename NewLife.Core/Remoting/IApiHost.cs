using System;
using System.Threading.Tasks;
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
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task<TResult> InvokeAsync<TResult>(IApiHost host, IApiSession session, String action, Object args)
        {
            if (session == null) return default(TResult);

            var enc = host.Encoder;
            var msg = enc.Encode(action, args);

            var rs = await session.SendAsync(msg);
            if (rs == null) return default(TResult);

            // 特殊返回类型
            var rtype = typeof(TResult);
            if (rtype == typeof(IMessage)) return (TResult)rs;
            if (rtype == typeof(Packet)) return (TResult)(Object)rs.Payload;

            if (!enc.TryGetResponse(rs, out var code, out var result)) throw new InvalidOperationException();

            // 是否成功
            if (code != 0) throw new ApiException(code, result + "");

            if (result == null) return default(TResult);
            if (result is TResult || rtype == typeof(Object)) return (TResult)result;

            // 返回
            return enc.Convert<TResult>(result);
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
    }
}