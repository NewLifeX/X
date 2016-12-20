using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        object Execute(IApiSession session, String action, IDictionary<String, Object> args);
    }

    class ApiHandler : IApiHandler
    {
        public ApiServer Server { get; set; }

        /// <summary>执行</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Object Execute(IApiSession session, string action, IDictionary<string, object> args)
        {
            var api = Server.FindAction(action);
            if (api == null) throw new Exception("无法找到名为[{0}]的服务！".F(action));

            var controller = api.Method.DeclaringType.CreateInstance();
            if (controller is IApi) (controller as IApi).Session = session;

            var ps = args as IDictionary<string, Object>;
            var fs = api.Filters;

            // 上下文
            var ctx = new ControllerContext { Controller = controller };
            ctx.Action = api;
            ctx.Session = session;

            try
            {
                if (fs.Length > 0)
                {
                    var actx = new ActionExecutingContext(ctx);
                    actx.ActionParameters = ps;
                    foreach (var filter in fs)
                    {
                        filter.OnActionExecuting(actx);
                    }
                }

                var rs = controller.InvokeWithParams(api.Method, args as IDictionary);

                if (fs.Length > 0)
                {
                    // 倒序
                    fs = fs.Reverse().ToArray();

                    var actx = new ActionExecutedContext(ctx);
                    actx.Result = rs;
                    foreach (var filter in fs)
                    {
                        filter.OnActionExecuted(actx);
                    }
                    rs = actx.Result;
                }

                return rs;
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                var exctx = new ExceptionContext(ctx);
                exctx.Exception = ex;

                // 如果异常没有被拦截，继续向外抛出
                if (!exctx.ExceptionHandled) throw;

                return exctx.Result;
            }
        }
    }
}