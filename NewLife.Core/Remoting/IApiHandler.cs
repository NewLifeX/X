using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>Api处理器</summary>
    public interface IApiHandler
    {
        /// <summary>执行</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Object Execute(IApiSession session, String action, Packet args);
    }

    /// <summary>默认处理器</summary>
    public class ApiHandler : IApiHandler
    {
        #region 属性
        /// <summary>Api接口主机</summary>
        public IApiHost Host { get; set; }
        #endregion

        #region 执行
        /// <summary>执行</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Object Execute(IApiSession session, String action, Packet args)
        {
            var api = session.FindAction(action);
            if (api == null) throw new ApiException(404, "无法找到名为[{0}]的服务！".F(action));

            // 全局共用控制器，或者每次创建对象实例
            var controller = session.CreateController(api);
            if (controller == null) throw new ApiException(403, "无法创建名为[{0}]的服务！".F(api.Name));

            if (controller is IApi capi) capi.Session = session;

            var ctx = Prepare(session, action, args, api);
            ctx.Controller = controller;

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
            }

            return rs;
        }

        /// <summary>准备上下文</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        protected virtual ControllerContext Prepare(IApiSession session, String action, Packet args, ApiAction api)
        {
            var enc = Host.Encoder;

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
                    enc.DecodeParameters(action, args);
                ctx.Parameters = dic;

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
            if (pis == null || pis.Length < 1) return null;

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
                        if (v == null) v = args;
                        //if (v is IDictionary<String, Object>)
                        ps[name] = encoder.Convert(v, pi.ParameterType);
                    }
                }
            }

            return ps;
        }
        #endregion
    }
}