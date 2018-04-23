using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NewLife.Collections;
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
        Object Execute(IApiSession session, String action, IDictionary<String, Object> args);
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
        public Object Execute(IApiSession session, String action, IDictionary<String, Object> args)
        {
            var api = session.FindAction(action);
            if (api == null) throw new ApiException(404, "无法找到名为[{0}]的服务！".F(action));

            // 全局共用控制器，或者每次创建对象实例
            var controller = session.CreateController(api);
            if (controller == null) throw new ApiException(403, "无法创建名为[{0}]的服务！".F(api.Name));

            if (controller is IApi) (controller as IApi).Session = session;

            // 服务端需要检查登录授权
            if (Host is ApiServer svrHost && !svrHost.Anonymous)
            {
                if (controller.GetType().GetCustomAttribute<AllowAnonymousAttribute>() == null &&
                    api.Method.GetCustomAttribute<AllowAnonymousAttribute>() == null)
                {
                    if (session.UserSession == null || !session.UserSession.Logined) throw new ApiException(401, "未登录！");
                }
            }

            // 服务设置优先于全局主机
            var svr = session.GetService<IApiServer>();
            var enc = svr?.Encoder ?? Host.Encoder;

            // 不允许参数字典为空
            if (args == null)
                args = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            else
                args = args.ToNullable(StringComparer.OrdinalIgnoreCase);

            // 准备好参数
            var ps = GetParams(api.Method, args, enc);

            // 上下文
            var ctx = new ControllerContext
            {
                Controller = controller,
                Action = api,
                ActionName = action,
                Session = session,
                Parameters = args
            };

            Object rs = null;
            ExceptionContext etx = null;
            try
            {
                // 当前上下文
                var actx = new ActionExecutingContext(ctx) { ActionParameters = ps };
                ControllerContext.Current = actx;

                // 执行动作前的过滤器
                //OnExecuting(actx, fs);
                if (controller is IActionFilter filter)
                {
                    filter.OnActionExecuting(actx);
                    rs = actx.Result;
                }

                // 执行动作
                if (rs == null) rs = controller.InvokeWithParams(api.Method, ps as IDictionary);
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                // 过滤得到内层异常
                ex = ex.GetTrue();

                // 执行异常过滤器
                if (controller is IExceptionFilter filter)
                {
                    etx = new ExceptionContext(ctx) { Exception = ex, Result = rs };
                    filter.OnException(etx);
                    rs = etx.Result;
                }

                Host.WriteLog("执行{0}出错！{1}", action, ex.Message);

                // 如果异常没有被拦截，继续向外抛出
                if (!etx.ExceptionHandled) throw;

                return rs = etx.Result;
            }
            finally
            {
                // 执行动作后的过滤器
                //rs = OnExecuted(ctx, etx, fs, rs);
                if (controller is IActionFilter filter)
                {
                    var atx = new ActionExecutedContext(etx ?? ctx) { Result = rs };
                    filter.OnActionExecuted(atx);
                    rs = atx.Result;
                }
                ControllerContext.Current = null;
            }

            return rs;
        }

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