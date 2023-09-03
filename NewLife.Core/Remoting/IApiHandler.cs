using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>Api处理器</summary>
    public interface IApiHandler
    {
        /// <summary>执行</summary>
        /// <param name="session">会话</param>
        /// <param name="action">动作</param>
        /// <param name="args">参数</param>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        Object Execute(IApiSession session, String action, Packet args, IMessage msg);
    }

    /// <summary>默认处理器</summary>
    /// <remarks>
    /// 在基于令牌Token的无状态验证模式中，可以借助Token重写IApiHandler.Prepare，来达到同一个Token共用相同的IApiSession.Items
    /// </remarks>
    public class ApiHandler : IApiHandler
    {
        #region 属性
        /// <summary>Api接口主机</summary>
        public IApiHost Host { get; set; }
        #endregion

        #region 执行
        /// <summary>执行</summary>
        /// <param name="session">会话</param>
        /// <param name="action">动作</param>
        /// <param name="args">参数</param>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        public virtual Object Execute(IApiSession session, String action, Packet args, IMessage msg)
        {
            if (action.IsNullOrEmpty()) action = "Api/Info";

            var api = session.FindAction(action) ?? throw new ApiException(404, $"无法找到名为[{action}]的服务！");

            // 全局共用控制器，或者每次创建对象实例
            var controller = session.CreateController(api) ?? throw new ApiException(403, $"无法创建名为[{api.Name}]的服务！");
            if (controller is IApi capi) capi.Session = session;
            if (session is INetSession ss)
                api.LastSession = ss.Remote + "";
            else
                api.LastSession = session + "";

            var st = api.StatProcess;
            var sw = st.StartCount();

            var ctx = Prepare(session, action, args, api, msg);
            ctx.Controller = controller;

            // 释放参数到跟踪片段
            DefaultSpan.Current.Detach(ctx.Parameters);

            Object rs = null;
            try
            {
                // 执行动作前的过滤器
                if (controller is IActionFilter filter)
                {
                    filter.OnActionExecuting(ctx);
                    rs = ctx.Result;
                }

                // 执行动作
                if (rs == null)
                {
                    // 特殊处理参数和返回类型都是Packet的服务
                    if (api.IsPacketParameter && api.IsPacketReturn)
                    {
                        var func = api.Method.As<Func<Packet, Packet>>(controller);
                        rs = func(args);
                    }
                    else if (api.IsPacketParameter)
                    {
                        rs = controller.Invoke(api.Method, args);
                    }
                    else
                    {
                        var ps = ctx.ActionParameters;
                        rs = controller.InvokeWithParams(api.Method, ps as IDictionary);
                    }
                    ctx.Result = rs;
                }

                // 执行动作后的过滤器
                if (controller is IActionFilter filter2)
                {
                    filter2.OnActionExecuted(ctx);
                    rs = ctx.Result;
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                ctx.Exception = ex.GetTrue();

                // 执行动作后的过滤器
                if (controller is IActionFilter filter)
                {
                    filter.OnActionExecuted(ctx);
                    rs = ctx.Result;
                }
                if (ctx.Exception != null && !ctx.ExceptionHandled) throw;
            }
            finally
            {
                // 重置上下文，待下次重用对象
                ctx.Reset();

                st.StopCount(sw);
            }

            return rs;
        }

        /// <summary>准备上下文，可以借助Token重写Session会话集合</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="api"></param>
        /// <param name="msg">消息内容，辅助数据解析</param>
        /// <returns></returns>
        protected virtual ControllerContext Prepare(IApiSession session, String action, Packet args, ApiAction api, IMessage msg)
        {
            //var enc = Host.Encoder;
            var enc = session["Encoder"] as IEncoder ?? Host.Encoder;

            // 当前上下文
            var ctx = ControllerContext.Current;
            if (ctx == null)
            {
                ctx = new ControllerContext();
                ControllerContext.Current = ctx;
            }
            ctx.Action = api;
            ctx.ActionName = action;
            ctx.Session = session;
            ctx.Request = args;

            // 如果服务只有一个二进制参数，则走快速通道
            if (!api.IsPacketParameter)
            {
                // 不允许参数字典为空
                var dic = args == null || args.Total == 0 ?
                    new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase) :
                    enc.DecodeParameters(action, args, msg);
                ctx.Parameters = dic;
                session.Parameters = dic;

                // 令牌，作为参数或者http头传递
                if (dic.TryGetValue("Token", out var token)) session.Token = token + "";
                if (session.Token.IsNullOrEmpty() && msg is HttpMessage hmsg && hmsg.Headers != null)
                {
                    // post、package、byte三种情况将token 写入请求头
                    if (hmsg.Headers.TryGetValue("x-token", out var token2))
                        session.Token = token2;
                    else if (hmsg.Headers.TryGetValue("Authorization", out token2))
                        session.Token = token2.TrimStart("Bearer ");
                }

                // 准备好参数
                var ps = GetParams(api.Method, dic, enc);
                ctx.ActionParameters = ps;
            }

            return ctx;
        }

        /// <summary>获取参数</summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <param name="encoder"></param>
        /// <returns></returns>
        protected virtual IDictionary<String, Object> GetParams(MethodInfo method, IDictionary<String, Object> args, IEncoder encoder)
        {
            // 该方法没有参数，无视外部传入参数
            var pis = method.GetParameters();
            if (pis == null || pis.Length <= 0) return null;

            var ps = new Dictionary<String, Object>();
            foreach (var pi in pis)
            {
                var name = pi.Name;

                Object v = null;
                if (args != null && args.ContainsKey(name)) v = args[name];

                // 基本类型
                if (pi.ParameterType.GetTypeCode() != TypeCode.Object)
                {
                    ps[name] = v.ChangeType(pi.ParameterType);
                }
                // 复杂对象填充，各个参数填充到一个模型参数里面去
                else
                {
                    // 特殊处理字节数组
                    if (pi.ParameterType == typeof(Byte[]))
                        ps[name] = Convert.FromBase64String(v + "");
                    else
                    {
                        v ??= args;
                        //if (v is IDictionary<String, Object>)
                        ps[name] = encoder.Convert(v, pi.ParameterType);
                    }
                }
            }

            return ps;
        }
        #endregion
    }

    /// <summary>带令牌会话的处理器</summary>
    /// <remarks>
    /// 在基于令牌Token的无状态验证模式中，可以借助Token重写IApiHandler.Prepare，来达到同一个Token共用相同的IApiSession.Items。
    /// 支持内存缓存和Redis缓存。
    /// </remarks>
    public class TokenApiHandler : ApiHandler
    {
        /// <summary>会话存储</summary>
        public ICache Cache { get; set; } = new MemoryCache { Expire = 20 * 60 };

        /// <summary>准备上下文，可以借助Token重写Session会话集合</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="api"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected override ControllerContext Prepare(IApiSession session, String action, Packet args, ApiAction api, IMessage msg)
        {
            var ctx = base.Prepare(session, action, args, api, msg);

            var token = session.Token;
            if (!token.IsNullOrEmpty())
            {
                // 第一用户数据是本地字典，用于记录是否启用了第二数据
                if (session is ApiNetSession ns && ns.Items["Token"] + "" != token)
                {
                    var key = GetKey(token);
                    // 采用哈希结构。内存缓存用并行字段，Redis用Set
                    ns.Items2 = Cache.GetDictionary<Object>(key);
                    ns.Items["Token"] = token;
                }
            }

            return ctx;
        }

        /// <summary>根据令牌活期缓存Key</summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected virtual String GetKey(String token) => (!token.IsNullOrEmpty() && token.Length > 16) ? token.MD5() : token;
    }
}