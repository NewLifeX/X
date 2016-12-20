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
        Task<Object> Execute(IApiSession session, String action, IDictionary<String, Object> args);
    }

    class ApiHandler : IApiHandler
    {
        public ApiServer Server { get; set; }

        /// <summary>执行</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<Object> Execute(IApiSession session, string action, IDictionary<string, object> args)
        {
            var api = Server.FindAction(action);
            if (api == null) throw new Exception("无法找到名为[{0}]的服务！".F(action));

            var controller = api.Method.DeclaringType.CreateInstance();
            if (controller is IApi) (controller as IApi).Session = session;

            var fs = api.ActionFilters;

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
                OnExecuting(ctx, fs, args);

                // 执行动作
                rs = await Task.Run(() => controller.InvokeWithParams(api.Method, args as IDictionary));
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                // 执行异常过滤器
                etx = OnException(ctx, ex, api.ExceptionFilters, rs);

                // 如果异常没有被拦截，继续向外抛出
                if (!etx.ExceptionHandled) throw;

                return etx.Result;
            }
            finally
            {
                // 执行动作后的过滤器
                rs = OnExecuted(ctx, etx, fs, rs);
            }

            return rs;
        }

        protected virtual void OnExecuting(ControllerContext ctx, IActionFilter[] fs, IDictionary<string, object> args)
        {
            if (fs.Length == 0) return;

            var actx = new ActionExecutingContext(ctx) { ActionParameters = args };
            foreach (var filter in fs)
            {
                filter.OnActionExecuting(actx);
            }
        }

        protected virtual Object OnExecuted(ControllerContext ctx, ExceptionContext etx, IActionFilter[] fs, Object rs)
        {
            if (fs.Length == 0) return rs;

            // 倒序
            fs = fs.Reverse().ToArray();

            var atx = new ActionExecutedContext(etx ?? ctx) { Result = rs };
            foreach (var filter in fs)
            {
                filter.OnActionExecuted(atx);
            }
            return atx.Result;
        }

        protected virtual ExceptionContext OnException(ControllerContext ctx, Exception ex, IExceptionFilter[] fs, Object rs)
        {
            //if (fs.Length == 0) return null;

            var etx = new ExceptionContext(ctx) { Exception = ex, Result = rs };

            foreach (var filter in fs)
            {
                filter.OnException(etx);
            }

            return etx;
        }
    }
}