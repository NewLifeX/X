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

            var ps = args as IDictionary<string, object>;
            var fs = api.Filters;

            // 上下文
            var ctx = new ControllerContext
            {
                Controller = controller,
                Action = api,
                Session = session
            };

            Object rs = null;
            ExceptionContext etx = null;
            try
            {
                // 执行动作前的过滤器
                if (fs.Length > 0)
                {
                    var actx = new ActionExecutingContext(ctx) { ActionParameters = ps };
                    foreach (var filter in fs)
                    {
                        filter.OnActionExecuting(actx);
                    }
                }

                // 执行动作
                rs = controller.InvokeWithParams(api.Method, args as IDictionary);
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                // 执行异常过滤器
                etx = new ExceptionContext(ctx) { Exception = ex, Result = rs };

                foreach (var filter in fs)
                {
                    var ef = filter as IExceptionFilter;
                    if (ef != null) ef.OnException(etx);
                }

                // 如果异常没有被拦截，继续向外抛出
                if (!etx.ExceptionHandled) throw;

                return etx.Result;
            }
            finally
            {
                // 执行动作后的过滤器
                if (fs.Length > 0)
                {
                    // 倒序
                    fs = fs.Reverse().ToArray();

                    var atx = new ActionExecutedContext(ctx) { Result = rs };
                    // 可能发生了异常
                    if (etx != null)
                    {
                        atx.Exception = etx.Exception;
                        atx.ExceptionHandled = etx.ExceptionHandled;
                        atx.Result = etx.Result;
                    }
                    foreach (var filter in fs)
                    {
                        filter.OnActionExecuted(atx);
                    }
                    rs = atx.Result;
                }
            }

            return rs;
        }
    }
}