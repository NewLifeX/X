using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="cookie">附加参数，位于顶级</param>
        /// <returns></returns>
        public static async Task<TResult> InvokeAsync<TResult>(IApiHost host, IApiSession session, String action, Object args, IDictionary<String, Object> cookie)
        {
            if (session == null) return default(TResult);

            var enc = host.Encoder;
            var data = enc.Encode(action, args, cookie);
            //var data = enc.Encode(new { action, args }.ToDictionary().Merge(cookie));

            var msg = session.CreateMessage(data);

            var rs = await session.SendAsync(msg);
            if (rs == null) return default(TResult);

            // 特殊返回类型
            if (typeof(TResult) == typeof(Packet)) return (TResult)(Object)rs.Payload;

            var dic = enc.Decode(rs.Payload);
            if (typeof(TResult) == typeof(IDictionary<String, Object>)) return (TResult)(Object)dic;

            //return enc.Decode<TResult>(dic);
            var code = 0;
            //enc.TryGet(dic, out code, out result);
            dic.TryGetValue("code", out var cod);

            // 参数可能不存在
            dic.TryGetValue("result", out var result);
            code = cod.ToInt();

            // 是否成功
            if (code != 0)
            {
                var aex = new ApiException(code, result + "");
                // 支持自定义错误
                if (result is IDictionary<String, Object> errdata)
                {
                    foreach (var item in errdata)
                    {
                        aex.Data[item.Key] = item.Value;
                    }
                }
                throw aex;
            }

            if (result == null) return default(TResult);
            if (typeof(TResult) == typeof(Object)) return (TResult)result;

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