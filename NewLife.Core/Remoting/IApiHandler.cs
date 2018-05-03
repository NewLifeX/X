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

    class ApiHandler : IApiHandler
    {
        /// <summary>Api接口主机</summary>
        public IApiHost Host { get; set; }

        /// <summary>执行</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Object Execute(IApiSession session, String action, Packet args)
        {
            var api = session.FindAction(action);
            if (api == null) throw new ApiException(404, "无法找到名为[{0}]的服务！".F(action));

            // 全局共用控制器，或者每次创建对象实例
            var controller = session.CreateController(api);
            if (controller == null) throw new ApiException(403, "无法创建名为[{0}]的服务！".F(api.Name));

            if (controller is IApi capi) capi.Session = session;

            var enc = Host.Encoder;
            IDictionary<String, Object> ps = null;

            // 上下文
            var ctx = new ControllerContext
            {
                Controller = controller,
                Action = api,
                ActionName = action,
                Session = session,
                Request = args,
                //Parameters = dic,
            };
            // 当前上下文
            ControllerContext.Current = ctx;

            // 如果服务只有一个二进制参数，则走快速通道
            var fast = api.IsPacketParameter && api.IsPacketReturn;
            if (!fast)
            {
                // 不允许参数字典为空
                var dic = args == null || args.Total == 0 ?
                    new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase) :
                    enc.Decode(action, args) as IDictionary<String, Object>;
                //dic = dic.ToNullable(StringComparer.OrdinalIgnoreCase);
                ctx.Parameters = dic;

                // 准备好参数
                ps = GetParams(api.Method, dic, enc);
            }

            Object rs = null;
            //ExceptionContext etx = null;
            try
            {
                //// 当前上下文
                //var actx = new ActionExecutingContext(ctx) { ActionParameters = ps };
                //ControllerContext.Current = actx;

                // 执行动作前的过滤器
                if (controller is IActionFilter filter)
                {
                    filter.OnActionExecuting(ctx);
                    rs = ctx.Result;
                }

                // 执行动作
                if (rs == null)
                {
                    if (fast)
                    {
                        var func = api.Method.As<Func<Packet, Packet>>(controller);
                        rs = func(args);
                    }
                    else
                    {
                        // 特殊处理参数和返回类型都是Packet的服务
                        rs = controller.InvokeWithParams(api.Method, ps as IDictionary);
                    }
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                //rs = OnException(ctx, ex);
                ctx.Exception = ex.GetTrue();
            }
            finally
            {
                // 执行动作后的过滤器
                if (controller is IActionFilter filter)
                {
                    //var atx = new ActionExecutedContext(etx ?? ctx) { Result = rs };
                    filter.OnActionExecuted(ctx);
                    rs = ctx.Result;
                }
                ControllerContext.Current = null;

                if (ctx.Exception != null && !ctx.ExceptionHandled) throw ctx.Exception;
            }

            // 二进制优先通道
            if (api.IsPacketReturn && rs is Packet pk) return pk;

            return rs;
        }

        //private Object OnException(ControllerContext ctx, Exception ex)
        //{
        //    // 过滤得到内层异常
        //    ex = ex.GetTrue();

        //    // 执行异常过滤器
        //    if (ctx.Controller is IExceptionFilter filter)
        //    {
        //        var etx = new ExceptionContext(ctx) { Exception = ex };
        //        filter.OnException(etx);
        //        var rs = etx.Result ?? etx.Exception ?? ex;

        //        // 如果异常没有被拦截，继续向外抛出
        //        if (etx.ExceptionHandled) return rs;
        //    }

        //    Host.WriteLog("执行{0}出错！{1}", ctx.ActionName, ex.Message);

        //    // 如果异常没有被拦截，继续向外抛出
        //    throw ex;
        //}

        private IDictionary<String, Object> GetParams(MethodInfo method, IDictionary<String, Object> args, IEncoder encoder)
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
    }
}